#if UNITY_EDITOR
using System.Collections.Generic;
using FrozenFrontier.Data;
using UnityEditor;
using UnityEngine;

namespace FrozenFrontier.Util.Editor
{
    public sealed class FrozenFrontierDataRefs
    {
        public readonly List<BuildingDef> buildingDefs = new List<BuildingDef>();
        public readonly List<TileBiomeDef> biomeDefs = new List<TileBiomeDef>();
        public readonly List<EventDef> eventDefs = new List<EventDef>();
    }

    public static class FrozenFrontierDataGenerator
    {
        private const string RootPath = "Assets/_Project/Data/ScriptableObjects";
        private const string BuildingsPath = RootPath + "/Buildings";
        private const string BiomesPath = RootPath + "/Biomes";
        private const string EventsPath = RootPath + "/Events";

        [MenuItem("Tools/Frozen Frontier/Generate Data Assets")]
        public static void GenerateDataAssetsMenu()
        {
            EnsureDataAssets(true);
            EditorUtility.DisplayDialog("Frozen Frontier", "Data assets generated/updated.", "OK");
        }

        public static FrozenFrontierDataRefs EnsureDataAssets(bool forceRewrite)
        {
            EnsureFolders();
            FrozenFrontierDataRefs refs = new FrozenFrontierDataRefs();

            refs.buildingDefs.Add(CreateOrLoadBuilding(
                "Building_Shelter.asset",
                forceRewrite,
                def =>
                {
                    def.id = "shelter";
                    def.displayName = "Shelter (HQ)";
                    def.workerSlots = 0;
                    def.size = Vector2Int.one;
                    def.levels = new[]
                    {
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 0),
                            survivorCapBonus = 2
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 60, ResourceType.Scrap, 25),
                            survivorCapBonus = 4
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 90, ResourceType.Scrap, 45),
                            survivorCapBonus = 7
                        }
                    };
                    def.maxLevel = def.levels.Length;
                }));

            refs.buildingDefs.Add(CreateOrLoadBuilding(
                "Building_Heater.asset",
                forceRewrite,
                def =>
                {
                    def.id = "heater";
                    def.displayName = "Heater";
                    def.workerSlots = 1;
                    def.size = Vector2Int.one;
                    def.levels = new[]
                    {
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 25, ResourceType.Scrap, 20, ResourceType.Fuel, 5),
                            perTickConsumption = Amounts(ResourceType.Fuel, 1),
                            heatDeltaPerTick = 4
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 35, ResourceType.Scrap, 25),
                            perTickConsumption = Amounts(ResourceType.Fuel, 1),
                            heatDeltaPerTick = 6
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 55, ResourceType.Scrap, 35),
                            perTickConsumption = Amounts(ResourceType.Fuel, 2),
                            heatDeltaPerTick = 10
                        }
                    };
                    def.maxLevel = def.levels.Length;
                }));

            refs.buildingDefs.Add(CreateOrLoadBuilding(
                "Building_LumberMill.asset",
                forceRewrite,
                def =>
                {
                    def.id = "lumber_mill";
                    def.displayName = "Lumber Mill";
                    def.workerSlots = 2;
                    def.size = Vector2Int.one;
                    def.levels = new[]
                    {
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 35, ResourceType.Scrap, 10),
                            perTickProduction = Amounts(ResourceType.Wood, 3)
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 40, ResourceType.Scrap, 20),
                            perTickProduction = Amounts(ResourceType.Wood, 5)
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 55, ResourceType.Scrap, 30),
                            perTickProduction = Amounts(ResourceType.Wood, 8)
                        }
                    };
                    def.maxLevel = def.levels.Length;
                }));

            refs.buildingDefs.Add(CreateOrLoadBuilding(
                "Building_Kitchen.asset",
                forceRewrite,
                def =>
                {
                    def.id = "kitchen";
                    def.displayName = "Kitchen";
                    def.workerSlots = 2;
                    def.size = Vector2Int.one;
                    def.levels = new[]
                    {
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 30, ResourceType.Scrap, 15),
                            perTickConsumption = Amounts(ResourceType.Wood, 1, ResourceType.Food, 1),
                            perTickProduction = Amounts(ResourceType.Food, 3)
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 45, ResourceType.Scrap, 25),
                            perTickConsumption = Amounts(ResourceType.Wood, 1, ResourceType.Food, 1),
                            perTickProduction = Amounts(ResourceType.Food, 5)
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 60, ResourceType.Scrap, 35),
                            perTickConsumption = Amounts(ResourceType.Wood, 2, ResourceType.Food, 1),
                            perTickProduction = Amounts(ResourceType.Food, 8)
                        }
                    };
                    def.maxLevel = def.levels.Length;
                }));

            refs.buildingDefs.Add(CreateOrLoadBuilding(
                "Building_Storage.asset",
                forceRewrite,
                def =>
                {
                    def.id = "storage";
                    def.displayName = "Storage";
                    def.workerSlots = 0;
                    def.size = Vector2Int.one;
                    def.levels = new[]
                    {
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 30, ResourceType.Scrap, 15),
                            storageCapBonus = 150
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 45, ResourceType.Scrap, 25),
                            storageCapBonus = 260
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 70, ResourceType.Scrap, 35),
                            storageCapBonus = 400
                        }
                    };
                    def.maxLevel = def.levels.Length;
                }));

            refs.buildingDefs.Add(CreateOrLoadBuilding(
                "Building_Generator.asset",
                forceRewrite,
                def =>
                {
                    def.id = "generator";
                    def.displayName = "Generator";
                    def.workerSlots = 1;
                    def.size = Vector2Int.one;
                    def.levels = new[]
                    {
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 30, ResourceType.Scrap, 30),
                            perTickConsumption = Amounts(ResourceType.Fuel, 1),
                            powerDeltaPerTick = 3
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 45, ResourceType.Scrap, 40),
                            perTickConsumption = Amounts(ResourceType.Fuel, 1),
                            powerDeltaPerTick = 5
                        },
                        new BuildingLevelDef
                        {
                            buildCosts = Amounts(ResourceType.Wood, 60, ResourceType.Scrap, 55),
                            perTickConsumption = Amounts(ResourceType.Fuel, 2),
                            powerDeltaPerTick = 8
                        }
                    };
                    def.maxLevel = def.levels.Length;
                }));

            refs.biomeDefs.Add(CreateOrLoadBiome(
                "Biome_Forest.asset",
                forceRewrite,
                biome =>
                {
                    biome.id = "forest";
                    biome.displayName = "Frozen Forest";
                    biome.hazardChancePercent = 18;
                    biome.exploreDurationTicks = 7;
                    biome.baseRewards = Amounts(ResourceType.Wood, 30, ResourceType.Food, 12, ResourceType.Scrap, 6);
                }));

            refs.biomeDefs.Add(CreateOrLoadBiome(
                "Biome_Ruins.asset",
                forceRewrite,
                biome =>
                {
                    biome.id = "ruins";
                    biome.displayName = "Buried Ruins";
                    biome.hazardChancePercent = 28;
                    biome.exploreDurationTicks = 9;
                    biome.baseRewards = Amounts(ResourceType.Scrap, 24, ResourceType.Wood, 10, ResourceType.Fuel, 5);
                }));

            refs.biomeDefs.Add(CreateOrLoadBiome(
                "Biome_Tundra.asset",
                forceRewrite,
                biome =>
                {
                    biome.id = "tundra";
                    biome.displayName = "Icy Tundra";
                    biome.hazardChancePercent = 35;
                    biome.exploreDurationTicks = 10;
                    biome.baseRewards = Amounts(ResourceType.Food, 20, ResourceType.Fuel, 10);
                }));

            refs.biomeDefs.Add(CreateOrLoadBiome(
                "Biome_FuelCache.asset",
                forceRewrite,
                biome =>
                {
                    biome.id = "fuel_cache";
                    biome.displayName = "Fuel Cache";
                    biome.hazardChancePercent = 22;
                    biome.exploreDurationTicks = 8;
                    biome.baseRewards = Amounts(ResourceType.Fuel, 24, ResourceType.Scrap, 9);
                }));

            refs.eventDefs.Add(CreateOrLoadEvent(
                "Event_ColdSnap.asset",
                forceRewrite,
                def =>
                {
                    def.id = "event_cold_snap";
                    def.title = "Cold Snap";
                    def.description = "A brutal cold front arrives overnight.";
                    def.choices = new[]
                    {
                        Choice("burn_extra_fuel", "Burn extra fuel", Amounts(ResourceType.Fuel, 6), Outcome(null, 10, 2, 0, "The settlement stays warm.")),
                        Choice("hold_position", "Conserve supplies", null, Outcome(null, -6, -4, -2, "The cold bites hard."))
                    };
                }));

            refs.eventDefs.Add(CreateOrLoadEvent(
                "Event_Wreckage.asset",
                forceRewrite,
                def =>
                {
                    def.id = "event_wreckage";
                    def.title = "Wreckage Found";
                    def.description = "Scouts locate a half-buried transport.";
                    def.choices = new[]
                    {
                        Choice("salvage", "Send a salvage team", null, Outcome(Amounts(ResourceType.Scrap, 20, ResourceType.Wood, 8), 0, 0, 0, "Useful parts recovered.")),
                        Choice("careful_salvage", "Bring medics too", Amounts(ResourceType.Food, 4), Outcome(Amounts(ResourceType.Scrap, 14), 0, 1, 2, "Safe but slower salvage.")),
                        Choice("ignore", "Leave it", null, Outcome(null, 0, -1, 0, "You move on."))
                    };
                }));

            refs.eventDefs.Add(CreateOrLoadEvent(
                "Event_HungryStrangers.asset",
                forceRewrite,
                def =>
                {
                    def.id = "event_hungry_strangers";
                    def.title = "Hungry Strangers";
                    def.description = "Travelers ask for food and shelter.";
                    def.choices = new[]
                    {
                        Choice("share_food", "Share supplies", Amounts(ResourceType.Food, 10), Outcome(null, 0, 6, 0, "Compassion lifts morale.")),
                        Choice("turn_away", "Turn them away", null, Outcome(null, 0, -5, 0, "The camp feels colder.")),
                        Choice("trade", "Trade for scrap", Amounts(ResourceType.Food, 6), Outcome(Amounts(ResourceType.Scrap, 10), 0, 1, 0, "A hard but fair deal."))
                    };
                }));

            refs.eventDefs.Add(CreateOrLoadEvent(
                "Event_GeneratorFault.asset",
                forceRewrite,
                def =>
                {
                    def.id = "event_generator_fault";
                    def.title = "Generator Fault";
                    def.description = "The generator sputters and sparks.";
                    def.choices = new[]
                    {
                        Choice("repair_now", "Repair immediately", Amounts(ResourceType.Scrap, 12), Outcome(null, 4, 0, 0, "Repairs completed.")),
                        Choice("patch", "Temporary patch", Amounts(ResourceType.Scrap, 4), Outcome(null, -2, -1, 0, "The patch barely holds.")),
                        Choice("shutdown", "Shut it down", null, Outcome(null, -6, -3, 0, "Power is conserved but spirits dip."))
                    };
                }));

            refs.eventDefs.Add(CreateOrLoadEvent(
                "Event_MedicalNeed.asset",
                forceRewrite,
                def =>
                {
                    def.id = "event_medical_need";
                    def.title = "Medical Emergency";
                    def.description = "A disease risk is spreading through the shelter.";
                    def.choices = new[]
                    {
                        Choice("intensive_care", "Use medicine stock", Amounts(ResourceType.Food, 8), Outcome(null, 0, 2, 4, "Treatment succeeds.")),
                        Choice("quarantine", "Set strict quarantine", null, Outcome(null, 0, -3, -1, "Containment works, but morale suffers.")),
                        Choice("warm_rest", "Prioritize warmth and rest", Amounts(ResourceType.Fuel, 4), Outcome(null, 3, 0, 2, "Recovery improves in warmer tents."))
                    };
                }));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return refs;
        }

        private static BuildingDef CreateOrLoadBuilding(string fileName, bool forceRewrite, System.Action<BuildingDef> applyDefaults)
        {
            string path = $"{BuildingsPath}/{fileName}";
            return CreateOrLoad(path, forceRewrite, applyDefaults);
        }

        private static TileBiomeDef CreateOrLoadBiome(string fileName, bool forceRewrite, System.Action<TileBiomeDef> applyDefaults)
        {
            string path = $"{BiomesPath}/{fileName}";
            return CreateOrLoad(path, forceRewrite, applyDefaults);
        }

        private static EventDef CreateOrLoadEvent(string fileName, bool forceRewrite, System.Action<EventDef> applyDefaults)
        {
            string path = $"{EventsPath}/{fileName}";
            return CreateOrLoad(path, forceRewrite, applyDefaults);
        }

        private static T CreateOrLoad<T>(string path, bool forceRewrite, System.Action<T> applyDefaults)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            bool created = false;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                created = true;
            }

            if (created || forceRewrite)
            {
                applyDefaults?.Invoke(asset);
                EditorUtility.SetDirty(asset);
            }

            return asset;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder("Assets/_Project/Data", "ScriptableObjects");
            EnsureFolder(RootPath, "Buildings");
            EnsureFolder(RootPath, "Biomes");
            EnsureFolder(RootPath, "Events");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string full = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static ResourceAmount[] Amounts(params object[] values)
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
                    amount = Mathf.Max(0, System.Convert.ToInt32(values[i + 1]))
                });
            }

            return list.ToArray();
        }

        private static EventChoiceDef Choice(string id, string label, ResourceAmount[] costs, EventOutcomeDef outcome)
        {
            return new EventChoiceDef
            {
                id = id,
                label = label,
                costs = costs,
                outcome = outcome
            };
        }

        private static EventOutcomeDef Outcome(
            ResourceAmount[] resourceDeltas,
            int heatDelta,
            int moraleDelta,
            int healthDelta,
            string resultText)
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
    }
}
#endif
