using System;
using UnityEngine;

namespace FrozenFrontier.Data
{
    [CreateAssetMenu(menuName = "Frozen Frontier/Event Definition", fileName = "EventDef")]
    public class EventDef : ScriptableObject
    {
        public string id = "event_id";
        public string title = "Event";
        [TextArea] public string description = "Event description.";
        public EventChoiceDef[] choices;
    }

    [Serializable]
    public class EventChoiceDef
    {
        public string id = "choice_id";
        public string label = "Choice";
        public ResourceAmount[] costs;
        public EventOutcomeDef outcome;
    }

    [Serializable]
    public class EventOutcomeDef
    {
        public ResourceAmount[] resourceDeltas;
        public int heatDelta;
        public int moraleDelta;
        public int healthDelta;
        [TextArea] public string resultText = "Outcome applied.";
    }
}
