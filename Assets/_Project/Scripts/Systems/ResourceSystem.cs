using System;
using System.Collections.Generic;
using FrozenFrontier.Data;
using UnityEngine;

namespace FrozenFrontier.Systems
{
    public class ResourceSystem : MonoBehaviour
    {
        [Header("Starting Resources")]
        [SerializeField, Min(0)] private int startingWood = 80;
        [SerializeField, Min(0)] private int startingFood = 80;
        [SerializeField, Min(0)] private int startingFuel = 50;
        [SerializeField, Min(0)] private int startingScrap = 30;
        [SerializeField, Min(0)] private int startingHeat = 20;
        [SerializeField, Min(0)] private int startingPower = 0;

        [Header("Base Caps")]
        [SerializeField, Min(1)] private int baseWoodCap = 200;
        [SerializeField, Min(1)] private int baseFoodCap = 200;
        [SerializeField, Min(1)] private int baseFuelCap = 200;
        [SerializeField, Min(1)] private int baseScrapCap = 200;
        [SerializeField, Min(1)] private int maxHeat = 200;
        [SerializeField, Min(1)] private int maxPower = 300;

        private readonly Dictionary<ResourceType, int> amounts = new Dictionary<ResourceType, int>();
        private readonly Dictionary<ResourceType, int> baseCaps = new Dictionary<ResourceType, int>();
        private int storageBonus;
        private int heat;
        private int power;

        public event Action Changed;

        public int Heat => heat;
        public int Power => power;
        public int StorageBonus => storageBonus;

        public void InitializeDefaults()
        {
            baseCaps[ResourceType.Wood] = baseWoodCap;
            baseCaps[ResourceType.Food] = baseFoodCap;
            baseCaps[ResourceType.Fuel] = baseFuelCap;
            baseCaps[ResourceType.Scrap] = baseScrapCap;

            amounts[ResourceType.Wood] = startingWood;
            amounts[ResourceType.Food] = startingFood;
            amounts[ResourceType.Fuel] = startingFuel;
            amounts[ResourceType.Scrap] = startingScrap;

            heat = startingHeat;
            power = startingPower;
            storageBonus = 0;
            ClampAll();
            NotifyChanged();
        }

        public int GetAmount(ResourceType type)
        {
            if (!amounts.TryGetValue(type, out int value))
            {
                value = 0;
            }

            return value;
        }

        public int GetCap(ResourceType type)
        {
            if (!baseCaps.TryGetValue(type, out int cap))
            {
                cap = 0;
            }

            return Mathf.Max(1, cap + storageBonus);
        }

        public void SetStorageBonus(int bonus)
        {
            storageBonus = Mathf.Max(0, bonus);
            ClampAll();
            NotifyChanged();
        }

        public bool CanAfford(ResourceAmount[] costs)
        {
            if (costs == null)
            {
                return true;
            }

            for (int i = 0; i < costs.Length; i++)
            {
                ResourceAmount cost = costs[i];
                if (cost == null || cost.amount <= 0)
                {
                    continue;
                }

                if (GetAmount(cost.type) < cost.amount)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TrySpend(ResourceAmount[] costs)
        {
            if (costs == null || costs.Length == 0)
            {
                return true;
            }

            if (!CanAfford(costs))
            {
                return false;
            }

            for (int i = 0; i < costs.Length; i++)
            {
                ResourceAmount cost = costs[i];
                if (cost == null || cost.amount <= 0)
                {
                    continue;
                }

                amounts[cost.type] = Mathf.Max(0, GetAmount(cost.type) - cost.amount);
            }

            NotifyChanged();
            return true;
        }

        public int SpendUpTo(ResourceType type, int requested)
        {
            if (requested <= 0)
            {
                return 0;
            }

            int current = GetAmount(type);
            int spent = Mathf.Min(current, requested);
            amounts[type] = current - spent;
            if (spent > 0)
            {
                NotifyChanged();
            }

            return spent;
        }

        public void Add(ResourceType type, int amount)
        {
            if (amount == 0)
            {
                return;
            }

            int newValue = GetAmount(type) + amount;
            amounts[type] = Mathf.Clamp(newValue, 0, GetCap(type));
            NotifyChanged();
        }

        public void ApplyDeltas(ResourceAmount[] deltas)
        {
            if (deltas == null)
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < deltas.Length; i++)
            {
                ResourceAmount delta = deltas[i];
                if (delta == null || delta.amount == 0)
                {
                    continue;
                }

                int current = GetAmount(delta.type);
                amounts[delta.type] = Mathf.Clamp(current + delta.amount, 0, GetCap(delta.type));
                changed = true;
            }

            if (changed)
            {
                NotifyChanged();
            }
        }

        public void ModifyHeat(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            heat = Mathf.Clamp(heat + delta, 0, maxHeat);
            NotifyChanged();
        }

        public void ModifyPower(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            power = Mathf.Clamp(power + delta, 0, maxPower);
            NotifyChanged();
        }

        public float GetHeatProductivityMultiplier()
        {
            if (heat <= 0)
            {
                return 0.3f;
            }

            if (heat <= 30)
            {
                float t = heat / 30f;
                return Mathf.Lerp(0.3f, 1f, t);
            }

            return 1f;
        }

        public ResourceSystemSaveData ExportState()
        {
            ResourceSystemSaveData data = new ResourceSystemSaveData
            {
                heat = heat,
                power = power,
                storageBonus = storageBonus
            };

            foreach (KeyValuePair<ResourceType, int> pair in amounts)
            {
                data.resources.Add(new ResourceEntrySaveData
                {
                    type = pair.Key,
                    amount = pair.Value
                });
            }

            return data;
        }

        public void ImportState(ResourceSystemSaveData data)
        {
            if (data == null)
            {
                InitializeDefaults();
                return;
            }

            amounts.Clear();
            baseCaps[ResourceType.Wood] = baseWoodCap;
            baseCaps[ResourceType.Food] = baseFoodCap;
            baseCaps[ResourceType.Fuel] = baseFuelCap;
            baseCaps[ResourceType.Scrap] = baseScrapCap;

            if (data.resources != null)
            {
                for (int i = 0; i < data.resources.Count; i++)
                {
                    ResourceEntrySaveData entry = data.resources[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    amounts[entry.type] = Mathf.Max(0, entry.amount);
                }
            }

            EnsureAllResourceKeys();
            storageBonus = Mathf.Max(0, data.storageBonus);
            heat = Mathf.Clamp(data.heat, 0, maxHeat);
            power = Mathf.Clamp(data.power, 0, maxPower);
            ClampAll();
            NotifyChanged();
        }

        private void EnsureAllResourceKeys()
        {
            if (!amounts.ContainsKey(ResourceType.Wood))
            {
                amounts[ResourceType.Wood] = 0;
            }

            if (!amounts.ContainsKey(ResourceType.Food))
            {
                amounts[ResourceType.Food] = 0;
            }

            if (!amounts.ContainsKey(ResourceType.Fuel))
            {
                amounts[ResourceType.Fuel] = 0;
            }

            if (!amounts.ContainsKey(ResourceType.Scrap))
            {
                amounts[ResourceType.Scrap] = 0;
            }
        }

        private void ClampAll()
        {
            EnsureAllResourceKeys();
            amounts[ResourceType.Wood] = Mathf.Clamp(amounts[ResourceType.Wood], 0, GetCap(ResourceType.Wood));
            amounts[ResourceType.Food] = Mathf.Clamp(amounts[ResourceType.Food], 0, GetCap(ResourceType.Food));
            amounts[ResourceType.Fuel] = Mathf.Clamp(amounts[ResourceType.Fuel], 0, GetCap(ResourceType.Fuel));
            amounts[ResourceType.Scrap] = Mathf.Clamp(amounts[ResourceType.Scrap], 0, GetCap(ResourceType.Scrap));
            heat = Mathf.Clamp(heat, 0, maxHeat);
            power = Mathf.Clamp(power, 0, maxPower);
        }

        public void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
