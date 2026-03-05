using System;
using System.Collections.Generic;
using FrozenFrontier.Data;
using UnityEngine;

namespace FrozenFrontier.Systems
{
    public class SurvivorSystem : MonoBehaviour
    {
        [SerializeField, Min(1)] private int startingSurvivorCount = 5;
        [SerializeField, Min(1)] private int baseSurvivorCap = 5;

        private readonly List<SurvivorRuntimeData> survivors = new List<SurvivorRuntimeData>();
        private ResourceSystem resourceSystem;
        private int survivorCapBonus;

        public event Action Changed;

        public int SurvivorCap => baseSurvivorCap + survivorCapBonus;
        public IReadOnlyList<SurvivorRuntimeData> Survivors => survivors;

        public void Initialize(ResourceSystem resources)
        {
            resourceSystem = resources;
            if (survivors.Count == 0)
            {
                CreateDefaultSurvivors();
            }

            NotifyChanged();
        }

        public void SetSurvivorCapBonus(int bonus)
        {
            survivorCapBonus = Mathf.Max(0, bonus);
            NotifyChanged();
        }

        public void Tick(bool offlineMode)
        {
            if (resourceSystem == null || survivors.Count == 0)
            {
                return;
            }

            int requiredFood = survivors.Count;
            int spentFood = resourceSystem.SpendUpTo(ResourceType.Food, requiredFood);
            bool starving = spentFood < requiredFood;
            int heat = resourceSystem.Heat;

            for (int i = 0; i < survivors.Count; i++)
            {
                SurvivorRuntimeData survivor = survivors[i];
                if (starving)
                {
                    survivor.hunger = Mathf.Clamp(survivor.hunger + 8, 0, 100);
                    survivor.morale = Mathf.Clamp(survivor.morale - 2, 0, 100);
                    if (survivor.hunger > 60)
                    {
                        survivor.health = Mathf.Clamp(survivor.health - 1, 0, 100);
                    }
                }
                else
                {
                    survivor.hunger = Mathf.Clamp(survivor.hunger - 5, 0, 100);
                    if (!offlineMode && survivor.health < 100)
                    {
                        survivor.health = Mathf.Clamp(survivor.health + 1, 0, 100);
                    }
                }

                if (heat <= 0)
                {
                    survivor.warmth = Mathf.Clamp(survivor.warmth - 5, 0, 100);
                    survivor.morale = Mathf.Clamp(survivor.morale - 1, 0, 100);
                }
                else if (heat > 30)
                {
                    survivor.warmth = Mathf.Clamp(survivor.warmth + 2, 0, 100);
                    if ((i + Time.frameCount) % 3 == 0)
                    {
                        survivor.morale = Mathf.Clamp(survivor.morale + 1, 0, 100);
                    }
                }
                else
                {
                    survivor.warmth = Mathf.Clamp(survivor.warmth + 1, 0, 100);
                }
            }

            NotifyChanged();
        }

        public int GetAverageMorale()
        {
            return Mathf.RoundToInt(GetAverageStat(s => s.morale));
        }

        public int GetAverageHealth()
        {
            return Mathf.RoundToInt(GetAverageStat(s => s.health));
        }

        public float GetGlobalProductivityModifier()
        {
            if (survivors.Count == 0)
            {
                return 0.5f;
            }

            float avgMorale = GetAverageMorale() / 100f;
            float avgHealth = GetAverageHealth() / 100f;
            float avgWarmth = GetAverageStat(s => s.warmth) / 100f;
            float combined = (avgMorale * 0.4f) + (avgHealth * 0.4f) + (avgWarmth * 0.2f);
            return Mathf.Clamp(0.65f + combined * 0.55f, 0.5f, 1.2f);
        }

        public int GetCountByJob(SurvivorJob job)
        {
            int count = 0;
            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i].job == job)
                {
                    count++;
                }
            }

            return count;
        }

        public bool AssignSurvivor(string survivorId, SurvivorJob job)
        {
            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i].id == survivorId)
                {
                    survivors[i].job = job;
                    NotifyChanged();
                    return true;
                }
            }

            return false;
        }

        public bool AssignOneToJob(SurvivorJob job)
        {
            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i].job == SurvivorJob.Idle)
                {
                    survivors[i].job = job;
                    NotifyChanged();
                    return true;
                }
            }

            // If nobody is idle, reassign from the largest existing non-target role.
            int bestIndex = -1;
            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i].job != job)
                {
                    bestIndex = i;
                    break;
                }
            }

            if (bestIndex >= 0)
            {
                survivors[bestIndex].job = job;
                NotifyChanged();
                return true;
            }

            return false;
        }

        public int AssignIdleToJob(SurvivorJob job, int maxCount = int.MaxValue)
        {
            int reassigned = 0;
            int limit = Mathf.Max(1, maxCount);
            for (int i = 0; i < survivors.Count && reassigned < limit; i++)
            {
                if (survivors[i].job != SurvivorJob.Idle)
                {
                    continue;
                }

                survivors[i].job = job;
                reassigned++;
            }

            if (reassigned > 0)
            {
                NotifyChanged();
            }

            return reassigned;
        }

        public void ResetJobs()
        {
            for (int i = 0; i < survivors.Count; i++)
            {
                survivors[i].job = SurvivorJob.Idle;
            }

            NotifyChanged();
        }

        public void ApplyMoraleDelta(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            for (int i = 0; i < survivors.Count; i++)
            {
                survivors[i].morale = Mathf.Clamp(survivors[i].morale + delta, 0, 100);
            }

            NotifyChanged();
        }

        public void ApplyHealthDelta(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            for (int i = 0; i < survivors.Count; i++)
            {
                survivors[i].health = Mathf.Clamp(survivors[i].health + delta, 0, 100);
            }

            NotifyChanged();
        }

        public SurvivorSystemSaveData ExportState()
        {
            SurvivorSystemSaveData data = new SurvivorSystemSaveData();
            for (int i = 0; i < survivors.Count; i++)
            {
                SurvivorRuntimeData survivor = survivors[i];
                data.survivors.Add(new SurvivorSaveData
                {
                    id = survivor.id,
                    displayName = survivor.displayName,
                    job = survivor.job,
                    health = survivor.health,
                    morale = survivor.morale,
                    hunger = survivor.hunger,
                    warmth = survivor.warmth
                });
            }

            return data;
        }

        public void ImportState(SurvivorSystemSaveData data)
        {
            survivors.Clear();
            if (data != null && data.survivors != null && data.survivors.Count > 0)
            {
                for (int i = 0; i < data.survivors.Count; i++)
                {
                    SurvivorSaveData source = data.survivors[i];
                    if (source == null)
                    {
                        continue;
                    }

                    survivors.Add(new SurvivorRuntimeData
                    {
                        id = source.id,
                        displayName = source.displayName,
                        job = source.job,
                        health = Mathf.Clamp(source.health, 0, 100),
                        morale = Mathf.Clamp(source.morale, 0, 100),
                        hunger = Mathf.Clamp(source.hunger, 0, 100),
                        warmth = Mathf.Clamp(source.warmth, 0, 100)
                    });
                }

                if (survivors.Count == 0)
                {
                    CreateDefaultSurvivors();
                }
            }
            else
            {
                CreateDefaultSurvivors();
            }

            NotifyChanged();
        }

        private void CreateDefaultSurvivors()
        {
            survivors.Clear();
            int spawnCount = Mathf.Min(startingSurvivorCount, SurvivorCap);
            for (int i = 0; i < spawnCount; i++)
            {
                survivors.Add(new SurvivorRuntimeData
                {
                    id = $"survivor_{i + 1}",
                    displayName = $"Survivor {i + 1}",
                    job = SurvivorJob.Idle,
                    health = 100,
                    morale = 70,
                    hunger = 25,
                    warmth = 60
                });
            }
        }

        private float GetAverageStat(Func<SurvivorRuntimeData, int> selector)
        {
            if (survivors.Count == 0)
            {
                return 0f;
            }

            int sum = 0;
            for (int i = 0; i < survivors.Count; i++)
            {
                sum += selector(survivors[i]);
            }

            return (float)sum / survivors.Count;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
