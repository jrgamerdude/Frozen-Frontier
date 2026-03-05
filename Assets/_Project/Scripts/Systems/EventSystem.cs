using System;
using System.Collections.Generic;
using FrozenFrontier.Data;
using UnityEngine;

namespace FrozenFrontier.Systems
{
    public class EventSystem : MonoBehaviour
    {
        [SerializeField] private List<EventDef> eventDefinitions = new List<EventDef>();
        [SerializeField, Min(10)] private int minEventIntervalTicks = 90;
        [SerializeField, Min(10)] private int maxEventIntervalTicks = 150;

        private readonly List<EventDef> fallbackEvents = new List<EventDef>();
        private readonly Dictionary<string, EventDef> eventsById = new Dictionary<string, EventDef>();

        private ResourceSystem resourceSystem;
        private SurvivorSystem survivorSystem;
        private EventDef activeEvent;
        private int ticksUntilNextEvent;

        public event Action<EventDef> EventOpened;
        public event Action EventClosed;
        public event Action<string> ToastRequested;

        public EventDef ActiveEvent => activeEvent;

        public void Initialize(ResourceSystem resources, SurvivorSystem survivors)
        {
            resourceSystem = resources;
            survivorSystem = survivors;
            EnsureEvents();
            if (ticksUntilNextEvent <= 0)
            {
                ScheduleNextEvent();
            }
        }

        public void Tick(bool offlineMode)
        {
            if (activeEvent != null)
            {
                if (offlineMode)
                {
                    ResolveChoice(0, true);
                }

                return;
            }

            ticksUntilNextEvent--;
            if (ticksUntilNextEvent > 0)
            {
                return;
            }

            TriggerRandomEvent();
            if (offlineMode && activeEvent != null)
            {
                ResolveChoice(0, true);
            }
        }

        public bool ResolveChoice(int choiceIndex, bool silent = false)
        {
            if (activeEvent == null || activeEvent.choices == null || activeEvent.choices.Length == 0)
            {
                return false;
            }

            if (resourceSystem == null || survivorSystem == null)
            {
                return false;
            }

            int safeIndex = Mathf.Clamp(choiceIndex, 0, activeEvent.choices.Length - 1);
            EventChoiceDef choice = activeEvent.choices[safeIndex];
            if (choice == null)
            {
                return false;
            }

            if (!resourceSystem.TrySpend(choice.costs))
            {
                if (!silent)
                {
                    ToastRequested?.Invoke("Not enough resources for that event choice.");
                }

                return false;
            }

            ApplyOutcome(choice.outcome);
            if (!silent && choice.outcome != null && !string.IsNullOrWhiteSpace(choice.outcome.resultText))
            {
                ToastRequested?.Invoke(choice.outcome.resultText);
            }

            activeEvent = null;
            NotifyEventClosed();
            ScheduleNextEvent();
            return true;
        }

        public EventSystemSaveData ExportState()
        {
            return new EventSystemSaveData
            {
                ticksUntilNextEvent = ticksUntilNextEvent,
                activeEventId = activeEvent != null ? activeEvent.id : ""
            };
        }

        public void ImportState(EventSystemSaveData data)
        {
            EnsureEvents();
            if (data == null)
            {
                activeEvent = null;
                ScheduleNextEvent();
                return;
            }

            ticksUntilNextEvent = Mathf.Max(1, data.ticksUntilNextEvent);
            activeEvent = null;
            if (!string.IsNullOrWhiteSpace(data.activeEventId))
            {
                eventsById.TryGetValue(data.activeEventId, out activeEvent);
                if (IsTriggerableEvent(activeEvent))
                {
                    NotifyEventOpened(activeEvent);
                }
                else
                {
                    activeEvent = null;
                }
            }

            if (ticksUntilNextEvent <= 0)
            {
                ScheduleNextEvent();
            }
        }

        private void ApplyOutcome(EventOutcomeDef outcome)
        {
            if (outcome == null)
            {
                return;
            }

            resourceSystem.ApplyDeltas(outcome.resourceDeltas);
            resourceSystem.ModifyHeat(outcome.heatDelta);
            survivorSystem.ApplyMoraleDelta(outcome.moraleDelta);
            survivorSystem.ApplyHealthDelta(outcome.healthDelta);
        }

        private void TriggerRandomEvent()
        {
            List<EventDef> usableEvents = GetUsableEvents();
            if (usableEvents.Count == 0)
            {
                Debug.LogWarning("EventSystem: no valid event definitions available.");
                ScheduleNextEvent();
                return;
            }

            EventDef def = usableEvents[UnityEngine.Random.Range(0, usableEvents.Count)];
            if (def == null)
            {
                ScheduleNextEvent();
                return;
            }

            activeEvent = def;
            NotifyEventOpened(activeEvent);
            string title = string.IsNullOrWhiteSpace(activeEvent.title) ? activeEvent.id : activeEvent.title;
            ToastRequested?.Invoke($"Event: {title}");
        }

        private void ScheduleNextEvent()
        {
            int min = Mathf.Max(10, minEventIntervalTicks);
            int max = Mathf.Max(min + 1, maxEventIntervalTicks + 1);
            ticksUntilNextEvent = UnityEngine.Random.Range(min, max);
        }

        private void EnsureEvents()
        {
            eventsById.Clear();
            if (eventDefinitions == null || eventDefinitions.Count == 0)
            {
                BuildFallbackEvents();
                eventDefinitions = new List<EventDef>(fallbackEvents);
            }

            for (int i = 0; i < eventDefinitions.Count; i++)
            {
                EventDef def = eventDefinitions[i];
                if (!IsTriggerableEvent(def))
                {
                    continue;
                }

                eventsById[def.id] = def;
            }

            if (eventsById.Count == 0)
            {
                BuildFallbackEvents();
                eventDefinitions = new List<EventDef>(fallbackEvents);
                for (int i = 0; i < eventDefinitions.Count; i++)
                {
                    EventDef def = eventDefinitions[i];
                    if (IsTriggerableEvent(def))
                    {
                        eventsById[def.id] = def;
                    }
                }
            }
        }

        private void BuildFallbackEvents()
        {
            if (fallbackEvents.Count > 0)
            {
                return;
            }

            fallbackEvents.Add(CreateEvent(
                "event_cold_snap",
                "Cold Snap",
                "A brutal cold front arrives overnight.",
                new[]
                {
                    Choice("burn_extra_fuel", "Burn extra fuel", Cost(ResourceType.Fuel, 6), Outcome(heatDelta: 10, moraleDelta: 2, resultText: "The settlement stays warm.")),
                    Choice("hold_position", "Conserve supplies", null, Outcome(heatDelta: -6, moraleDelta: -4, healthDelta: -2, resultText: "The cold bites hard."))
                }));

            fallbackEvents.Add(CreateEvent(
                "event_wreckage",
                "Wreckage Found",
                "Scouts locate a half-buried transport.",
                new[]
                {
                    Choice("salvage", "Send a salvage team", null, Outcome(new[] { Delta(ResourceType.Scrap, 20), Delta(ResourceType.Wood, 8) }, resultText: "Useful parts recovered.")),
                    Choice("careful_salvage", "Bring medics too", Cost(ResourceType.Food, 4), Outcome(new[] { Delta(ResourceType.Scrap, 14) }, healthDelta: 2, moraleDelta: 1, resultText: "Safe but slower salvage.")),
                    Choice("ignore", "Leave it", null, Outcome(moraleDelta: -1, resultText: "You move on."))
                }));

            fallbackEvents.Add(CreateEvent(
                "event_hungry_strangers",
                "Hungry Strangers",
                "Travelers ask for food and shelter.",
                new[]
                {
                    Choice("share_food", "Share supplies", Cost(ResourceType.Food, 10), Outcome(moraleDelta: 6, resultText: "Compassion lifts morale.")),
                    Choice("turn_away", "Turn them away", null, Outcome(moraleDelta: -5, resultText: "The camp feels colder.")),
                    Choice("trade", "Trade for scrap", Cost(ResourceType.Food, 6), Outcome(new[] { Delta(ResourceType.Scrap, 10) }, moraleDelta: 1, resultText: "A hard but fair deal."))
                }));

            fallbackEvents.Add(CreateEvent(
                "event_generator_fault",
                "Generator Fault",
                "The generator sputters and sparks.",
                new[]
                {
                    Choice("repair_now", "Repair immediately", Cost(ResourceType.Scrap, 12), Outcome(heatDelta: 4, resultText: "Repairs completed.")),
                    Choice("patch", "Temporary patch", Cost(ResourceType.Scrap, 4), Outcome(heatDelta: -2, moraleDelta: -1, resultText: "The patch barely holds.")),
                    Choice("shutdown", "Shut it down", null, Outcome(heatDelta: -6, moraleDelta: -3, resultText: "Power is conserved but spirits dip."))
                }));

            fallbackEvents.Add(CreateEvent(
                "event_medical_need",
                "Medical Emergency",
                "A disease risk is spreading through the shelter.",
                new[]
                {
                    Choice("intensive_care", "Use medicine stock", Cost(ResourceType.Food, 8), Outcome(healthDelta: 4, moraleDelta: 2, resultText: "Treatment succeeds.")),
                    Choice("quarantine", "Set strict quarantine", null, Outcome(healthDelta: -1, moraleDelta: -3, resultText: "Containment works, but morale suffers.")),
                    Choice("warm_rest", "Prioritize warmth and rest", Cost(ResourceType.Fuel, 4), Outcome(heatDelta: 3, healthDelta: 2, resultText: "Recovery improves in warmer tents."))
                }));
        }

        private EventDef CreateEvent(string id, string title, string description, EventChoiceDef[] choices)
        {
            EventDef def = ScriptableObject.CreateInstance<EventDef>();
            def.id = id;
            def.title = title;
            def.description = description;
            def.choices = choices;
            return def;
        }

        private EventChoiceDef Choice(string id, string label, ResourceAmount[] costs, EventOutcomeDef outcome)
        {
            return new EventChoiceDef
            {
                id = id,
                label = label,
                costs = costs,
                outcome = outcome
            };
        }

        private EventOutcomeDef Outcome(
            ResourceAmount[] resourceDeltas = null,
            int heatDelta = 0,
            int moraleDelta = 0,
            int healthDelta = 0,
            string resultText = "")
        {
            return new EventOutcomeDef
            {
                resourceDeltas = resourceDeltas,
                heatDelta = heatDelta,
                moraleDelta = moraleDelta,
                healthDelta = healthDelta,
                resultText = resultText
            };
        }

        private ResourceAmount[] Cost(params object[] values)
        {
            if (values == null || values.Length == 0)
            {
                return null;
            }

            List<ResourceAmount> list = new List<ResourceAmount>();
            for (int i = 0; i + 1 < values.Length; i += 2)
            {
                list.Add(new ResourceAmount
                {
                    type = (ResourceType)values[i],
                    amount = Mathf.Max(0, Convert.ToInt32(values[i + 1]))
                });
            }

            return list.ToArray();
        }

        private ResourceAmount Delta(ResourceType type, int amount)
        {
            return new ResourceAmount { type = type, amount = amount };
        }

        private List<EventDef> GetUsableEvents()
        {
            List<EventDef> usable = new List<EventDef>();
            if (eventDefinitions != null)
            {
                for (int i = 0; i < eventDefinitions.Count; i++)
                {
                    EventDef def = eventDefinitions[i];
                    if (!IsTriggerableEvent(def))
                    {
                        continue;
                    }

                    usable.Add(def);
                }
            }

            if (usable.Count == 0)
            {
                BuildFallbackEvents();
                for (int i = 0; i < fallbackEvents.Count; i++)
                {
                    EventDef def = fallbackEvents[i];
                    if (!IsTriggerableEvent(def))
                    {
                        continue;
                    }

                    usable.Add(def);
                }
            }

            return usable;
        }

        private bool IsTriggerableEvent(EventDef def)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.id) || def.choices == null || def.choices.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < def.choices.Length; i++)
            {
                if (def.choices[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void NotifyEventOpened(EventDef def)
        {
            if (def == null || EventOpened == null)
            {
                return;
            }

            Delegate[] listeners = EventOpened.GetInvocationList();
            for (int i = 0; i < listeners.Length; i++)
            {
                Action<EventDef> listener = listeners[i] as Action<EventDef>;
                if (listener == null)
                {
                    continue;
                }

                try
                {
                    listener.Invoke(def);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EventSystem: EventOpened listener failed ({listener.Method.DeclaringType?.Name}.{listener.Method.Name}): {ex.Message}");
                }
            }
        }

        private void NotifyEventClosed()
        {
            if (EventClosed == null)
            {
                return;
            }

            Delegate[] listeners = EventClosed.GetInvocationList();
            for (int i = 0; i < listeners.Length; i++)
            {
                Action listener = listeners[i] as Action;
                if (listener == null)
                {
                    continue;
                }

                try
                {
                    listener.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EventSystem: EventClosed listener failed ({listener.Method.DeclaringType?.Name}.{listener.Method.Name}): {ex.Message}");
                }
            }
        }
    }
}
