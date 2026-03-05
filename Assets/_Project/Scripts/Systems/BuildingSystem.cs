using System;
using System.Collections.Generic;
using FrozenFrontier.Data;
using UnityEngine;

namespace FrozenFrontier.Systems
{
    public class BuildingSystem : MonoBehaviour
    {
        [SerializeField] private List<BuildingDef> buildingDefinitions = new List<BuildingDef>();
        [SerializeField, Min(1)] private int baseGridWidth = 8;
        [SerializeField, Min(1)] private int baseGridHeight = 8;

        [Header("Base Grid World")]
        [SerializeField] private Transform baseAreaRoot;
        [SerializeField] private Vector2 baseGridOrigin = new Vector2(-4f, -4f);
        [SerializeField, Min(0.2f)] private float cellWorldSize = 1f;

        private readonly List<PlacedBuildingRuntimeData> placedBuildings = new List<PlacedBuildingRuntimeData>();
        private readonly Dictionary<string, BuildingDef> defsById = new Dictionary<string, BuildingDef>();
        private readonly List<BuildingDef> fallbackDefs = new List<BuildingDef>();

        private ResourceSystem resourceSystem;
        private SurvivorSystem survivorSystem;
        private string pendingPlacementDefId = "";

        public event Action Changed;
        public event Action<string> ToastRequested;
        public event Action<BuildingDef> PlacementModeChanged;

        public IReadOnlyList<PlacedBuildingRuntimeData> PlacedBuildings => placedBuildings;
        public IReadOnlyList<BuildingDef> Definitions => buildingDefinitions;
        public int GridWidth => baseGridWidth;
        public int GridHeight => baseGridHeight;
        public float CellWorldSize => cellWorldSize;
        public Transform BaseAreaRoot => baseAreaRoot;
        public bool IsPlacementMode => !string.IsNullOrWhiteSpace(pendingPlacementDefId);
        public string PendingPlacementDefId => pendingPlacementDefId;

        public void Initialize(ResourceSystem resources, SurvivorSystem survivors)
        {
            resourceSystem = resources;
            survivorSystem = survivors;

            EnsureDefinitions();
            if (placedBuildings.Count == 0)
            {
                SpawnStartingShelter();
            }

            RecalculateGlobalBonuses();
            NotifyChanged();
        }

        public void SetBaseAreaRoot(Transform root)
        {
            baseAreaRoot = root;
        }

        public bool BeginPlacement(string defId)
        {
            EnsureDefinitions();
            BuildingDef def = GetDef(defId);
            if (def == null)
            {
                ToastRequested?.Invoke($"Unknown building id: {defId}");
                return false;
            }

            pendingPlacementDefId = def.id;
            PlacementModeChanged?.Invoke(def);
            ToastRequested?.Invoke($"Placement mode: click on base grid to place {def.displayName}. Press Esc to cancel.");
            NotifyChanged();
            return true;
        }

        public void CancelPlacement(bool silent = false)
        {
            if (!IsPlacementMode)
            {
                return;
            }

            pendingPlacementDefId = "";
            PlacementModeChanged?.Invoke(null);
            if (!silent)
            {
                ToastRequested?.Invoke("Placement cancelled.");
            }

            NotifyChanged();
        }

        public bool TryPlacePendingAtGrid(int x, int y)
        {
            if (!IsPlacementMode)
            {
                return false;
            }

            bool placed = TryPlaceBuildingAtGridInternal(pendingPlacementDefId, x, y, true, out _);
            if (placed)
            {
                pendingPlacementDefId = "";
                PlacementModeChanged?.Invoke(null);
                NotifyChanged();
            }

            return placed;
        }

        public bool CanPlacePendingAtGrid(int x, int y, out string reason)
        {
            reason = "No building selected.";
            if (!IsPlacementMode)
            {
                return false;
            }

            return ValidatePlacement(pendingPlacementDefId, x, y, true, out reason);
        }

        public bool PlaceBuilding(string defId)
        {
            EnsureDefinitions();
            BuildingDef def = GetDef(defId);
            if (def == null)
            {
                ToastRequested?.Invoke($"Unknown building id: {defId}");
                return false;
            }

            if (!TryFindPlacement(def.size, out int x, out int y))
            {
                ToastRequested?.Invoke("No free building slot in base grid.");
                return false;
            }

            return TryPlaceBuildingAtGridInternal(def.id, x, y, true, out _);
        }

        public bool TryPlaceBuildingAtGrid(string defId, int x, int y)
        {
            return TryPlaceBuildingAtGridInternal(defId, x, y, true, out _);
        }

        public bool UpgradeBuilding(string instanceId)
        {
            if (resourceSystem == null)
            {
                return false;
            }

            PlacedBuildingRuntimeData placed = FindByInstanceId(instanceId);
            if (placed == null)
            {
                return false;
            }

            BuildingDef def = GetDef(placed.defId);
            if (def == null)
            {
                return false;
            }

            int targetLevel = placed.level + 1;
            if (targetLevel >= Mathf.Max(def.maxLevel, def.levels != null ? def.levels.Length : 1))
            {
                ToastRequested?.Invoke($"{def.displayName} is already max level.");
                return false;
            }

            BuildingLevelDef levelDef = def.GetLevel(targetLevel);
            if (!resourceSystem.TrySpend(levelDef.buildCosts))
            {
                ToastRequested?.Invoke($"Not enough resources to upgrade {def.displayName}");
                return false;
            }

            placed.level = targetLevel;
            RecalculateGlobalBonuses();
            NotifyChanged();
            ToastRequested?.Invoke($"{def.displayName} upgraded to Lv {placed.level + 1}");
            return true;
        }

        public bool UpgradeFirstByDef(string defId)
        {
            for (int i = 0; i < placedBuildings.Count; i++)
            {
                if (placedBuildings[i].defId == defId)
                {
                    return UpgradeBuilding(placedBuildings[i].instanceId);
                }
            }

            ToastRequested?.Invoke($"No {defId} found to upgrade.");
            return false;
        }

        public void Tick(bool offlineMode)
        {
            if (resourceSystem == null || survivorSystem == null)
            {
                return;
            }

            Dictionary<SurvivorJob, int> availableByJob = BuildWorkerPool();
            for (int i = 0; i < placedBuildings.Count; i++)
            {
                PlacedBuildingRuntimeData building = placedBuildings[i];
                BuildingDef def = GetDef(building.defId);
                if (def == null)
                {
                    continue;
                }

                SurvivorJob requiredJob = GetRequiredJob(def.id);
                if (requiredJob == SurvivorJob.Idle || def.workerSlots <= 0)
                {
                    building.assignedWorkers = 0;
                    continue;
                }

                int available = availableByJob.TryGetValue(requiredJob, out int count) ? count : 0;
                int assigned = Mathf.Min(def.workerSlots, available);
                building.assignedWorkers = assigned;
                availableByJob[requiredJob] = Mathf.Max(0, available - assigned);
            }

            float heatProductivity = resourceSystem.GetHeatProductivityMultiplier();
            float survivorProductivity = survivorSystem.GetGlobalProductivityModifier();
            for (int i = 0; i < placedBuildings.Count; i++)
            {
                PlacedBuildingRuntimeData building = placedBuildings[i];
                BuildingDef def = GetDef(building.defId);
                if (def == null)
                {
                    continue;
                }

                BuildingLevelDef levelDef = def.GetLevel(building.level);
                float workerMultiplier = building.assignedWorkers > 0
                    ? 1f + building.assignedWorkers * 0.25f
                    : Mathf.Clamp(levelDef.noWorkerOutputMultiplier, 0.05f, 1f);
                float productivity = heatProductivity * survivorProductivity * workerMultiplier;

                ResourceAmount[] perTickConsumption = Scale(levelDef.perTickConsumption, workerMultiplier);
                bool canOperate = resourceSystem.CanAfford(perTickConsumption);
                if (!canOperate)
                {
                    if (def.id == "heater")
                    {
                        resourceSystem.ModifyHeat(-1);
                    }

                    continue;
                }

                resourceSystem.TrySpend(perTickConsumption);
                resourceSystem.ApplyDeltas(Scale(levelDef.perTickProduction, productivity));
                resourceSystem.ModifyHeat(ScaleAmount(levelDef.heatDeltaPerTick, workerMultiplier));
                resourceSystem.ModifyPower(ScaleAmount(levelDef.powerDeltaPerTick, workerMultiplier));

                if (!offlineMode && def.id == "kitchen" && resourceSystem.Heat > 30)
                {
                    survivorSystem.ApplyMoraleDelta(1);
                }
            }

            NotifyChanged();
        }

        public BuildingSystemSaveData ExportState()
        {
            BuildingSystemSaveData data = new BuildingSystemSaveData();
            for (int i = 0; i < placedBuildings.Count; i++)
            {
                PlacedBuildingRuntimeData building = placedBuildings[i];
                data.placedBuildings.Add(new PlacedBuildingSaveData
                {
                    instanceId = building.instanceId,
                    defId = building.defId,
                    level = building.level,
                    gridX = building.gridX,
                    gridY = building.gridY,
                    assignedWorkers = building.assignedWorkers
                });
            }

            return data;
        }

        public void ImportState(BuildingSystemSaveData data)
        {
            placedBuildings.Clear();
            pendingPlacementDefId = "";

            if (data != null && data.placedBuildings != null)
            {
                for (int i = 0; i < data.placedBuildings.Count; i++)
                {
                    PlacedBuildingSaveData source = data.placedBuildings[i];
                    if (source == null || string.IsNullOrWhiteSpace(source.defId))
                    {
                        continue;
                    }

                    placedBuildings.Add(new PlacedBuildingRuntimeData
                    {
                        instanceId = string.IsNullOrWhiteSpace(source.instanceId) ? $"building_{Guid.NewGuid():N}" : source.instanceId,
                        defId = source.defId,
                        level = Mathf.Max(0, source.level),
                        gridX = source.gridX,
                        gridY = source.gridY,
                        assignedWorkers = Mathf.Max(0, source.assignedWorkers)
                    });
                }
            }

            EnsureDefinitions();
            if (placedBuildings.Count == 0)
            {
                SpawnStartingShelter();
            }

            RecalculateGlobalBonuses();
            PlacementModeChanged?.Invoke(null);
            NotifyChanged();
        }

        public BuildingDef GetDefinitionById(string defId)
        {
            EnsureDefinitions();
            return GetDef(defId);
        }

        public BuildingDef GetPendingDefinition()
        {
            return GetDefinitionById(pendingPlacementDefId);
        }

        public Vector2Int GetPendingSize()
        {
            BuildingDef def = GetPendingDefinition();
            return def != null ? ClampSize(def.size) : Vector2Int.one;
        }

        public Vector3 GetGridOriginWorld()
        {
            Vector3 rootOffset = baseAreaRoot != null ? baseAreaRoot.position : Vector3.zero;
            return rootOffset + new Vector3(baseGridOrigin.x, baseGridOrigin.y, 0f);
        }

        public bool WorldToGrid(Vector3 worldPos, out int x, out int y)
        {
            Vector3 local = worldPos - GetGridOriginWorld();
            x = Mathf.FloorToInt(local.x / Mathf.Max(0.01f, cellWorldSize));
            y = Mathf.FloorToInt(local.y / Mathf.Max(0.01f, cellWorldSize));
            return IsInsideGrid(x, y);
        }

        public Vector3 GridToWorldCenter(int x, int y, Vector2Int size)
        {
            Vector2Int clamped = ClampSize(size);
            Vector3 origin = GetGridOriginWorld();
            float width = clamped.x * cellWorldSize;
            float height = clamped.y * cellWorldSize;
            return origin + new Vector3(
                x * cellWorldSize + width * 0.5f,
                y * cellWorldSize + height * 0.5f,
                0f);
        }

        public Vector3 GridToWorldCenter(PlacedBuildingRuntimeData placed)
        {
            BuildingDef def = GetDefinitionById(placed != null ? placed.defId : "");
            Vector2Int size = def != null ? def.size : Vector2Int.one;
            return GridToWorldCenter(placed != null ? placed.gridX : 0, placed != null ? placed.gridY : 0, size);
        }

        private bool ValidatePlacement(string defId, int x, int y, bool checkCost, out string reason)
        {
            reason = "";
            BuildingDef def = GetDef(defId);
            if (def == null)
            {
                reason = $"Unknown building id: {defId}";
                return false;
            }

            Vector2Int size = ClampSize(def.size);
            if (!IsInsideGrid(x, y) || x + size.x > baseGridWidth || y + size.y > baseGridHeight)
            {
                reason = "Placement is outside base grid.";
                return false;
            }

            if (!IsAreaFree(x, y, size))
            {
                reason = "Placement blocked by existing building.";
                return false;
            }

            if (checkCost && resourceSystem != null)
            {
                BuildingLevelDef levelDef = def.GetLevel(0);
                if (!resourceSystem.CanAfford(levelDef.buildCosts))
                {
                    reason = $"Not enough resources for {def.displayName}.";
                    return false;
                }
            }

            return true;
        }

        private bool TryPlaceBuildingAtGridInternal(string defId, int x, int y, bool spendResources, out PlacedBuildingRuntimeData placedBuilding)
        {
            placedBuilding = null;
            EnsureDefinitions();

            if (!ValidatePlacement(defId, x, y, spendResources, out string reason))
            {
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    ToastRequested?.Invoke(reason);
                }

                return false;
            }

            BuildingDef def = GetDef(defId);
            if (def == null)
            {
                ToastRequested?.Invoke($"Unknown building id: {defId}");
                return false;
            }

            BuildingLevelDef levelDef = def.GetLevel(0);
            if (spendResources && resourceSystem != null && !resourceSystem.TrySpend(levelDef.buildCosts))
            {
                ToastRequested?.Invoke($"Not enough resources for {def.displayName}");
                return false;
            }

            placedBuilding = new PlacedBuildingRuntimeData
            {
                instanceId = $"building_{Guid.NewGuid():N}",
                defId = def.id,
                level = 0,
                gridX = x,
                gridY = y,
                assignedWorkers = 0
            };

            placedBuildings.Add(placedBuilding);
            RecalculateGlobalBonuses();
            NotifyChanged();
            ToastRequested?.Invoke($"{def.displayName} constructed at {x},{y}.");
            return true;
        }

        private void EnsureDefinitions()
        {
            defsById.Clear();

            bool hasRealDefs = buildingDefinitions != null && buildingDefinitions.Count > 0;
            if (!hasRealDefs)
            {
                BuildFallbackDefs();
                buildingDefinitions = new List<BuildingDef>(fallbackDefs);
            }

            for (int i = 0; i < buildingDefinitions.Count; i++)
            {
                BuildingDef def = buildingDefinitions[i];
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                {
                    continue;
                }

                defsById[def.id] = def;
            }
        }

        private void BuildFallbackDefs()
        {
            if (fallbackDefs.Count > 0)
            {
                return;
            }

            fallbackDefs.Add(CreateDef(
                "shelter",
                "Shelter (HQ)",
                0,
                new[]
                {
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 0),
                        survivorCapBonus = 2
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 60, ResourceType.Scrap, 25),
                        survivorCapBonus = 4
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 90, ResourceType.Scrap, 45),
                        survivorCapBonus = 7
                    }
                }));

            fallbackDefs.Add(CreateDef(
                "heater",
                "Heater",
                1,
                new[]
                {
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 25, ResourceType.Scrap, 20, ResourceType.Fuel, 5),
                        perTickConsumption = Cost(ResourceType.Fuel, 1),
                        heatDeltaPerTick = 4
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 35, ResourceType.Scrap, 25),
                        perTickConsumption = Cost(ResourceType.Fuel, 1),
                        heatDeltaPerTick = 6
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 55, ResourceType.Scrap, 35),
                        perTickConsumption = Cost(ResourceType.Fuel, 2),
                        heatDeltaPerTick = 10
                    }
                }));

            fallbackDefs.Add(CreateDef(
                "lumber_mill",
                "Lumber Mill",
                2,
                new[]
                {
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 35, ResourceType.Scrap, 10),
                        perTickProduction = Cost(ResourceType.Wood, 3)
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 40, ResourceType.Scrap, 20),
                        perTickProduction = Cost(ResourceType.Wood, 5)
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 55, ResourceType.Scrap, 30),
                        perTickProduction = Cost(ResourceType.Wood, 8)
                    }
                }));

            fallbackDefs.Add(CreateDef(
                "kitchen",
                "Kitchen",
                2,
                new[]
                {
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 30, ResourceType.Scrap, 15),
                        perTickConsumption = Cost(ResourceType.Wood, 1, ResourceType.Food, 1),
                        perTickProduction = Cost(ResourceType.Food, 3)
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 45, ResourceType.Scrap, 25),
                        perTickConsumption = Cost(ResourceType.Wood, 1, ResourceType.Food, 1),
                        perTickProduction = Cost(ResourceType.Food, 5)
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 60, ResourceType.Scrap, 35),
                        perTickConsumption = Cost(ResourceType.Wood, 2, ResourceType.Food, 1),
                        perTickProduction = Cost(ResourceType.Food, 8)
                    }
                }));

            fallbackDefs.Add(CreateDef(
                "storage",
                "Storage",
                0,
                new[]
                {
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 30, ResourceType.Scrap, 15),
                        storageCapBonus = 150
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 45, ResourceType.Scrap, 25),
                        storageCapBonus = 260
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 70, ResourceType.Scrap, 35),
                        storageCapBonus = 400
                    }
                }));

            fallbackDefs.Add(CreateDef(
                "generator",
                "Generator",
                1,
                new[]
                {
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 30, ResourceType.Scrap, 30),
                        perTickConsumption = Cost(ResourceType.Fuel, 1),
                        powerDeltaPerTick = 3
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 45, ResourceType.Scrap, 40),
                        perTickConsumption = Cost(ResourceType.Fuel, 1),
                        powerDeltaPerTick = 5
                    },
                    new BuildingLevelDef
                    {
                        buildCosts = Cost(ResourceType.Wood, 60, ResourceType.Scrap, 55),
                        perTickConsumption = Cost(ResourceType.Fuel, 2),
                        powerDeltaPerTick = 8
                    }
                }));
        }

        private BuildingDef CreateDef(string id, string displayName, int workerSlots, BuildingLevelDef[] levels)
        {
            BuildingDef def = ScriptableObject.CreateInstance<BuildingDef>();
            def.id = id;
            def.displayName = displayName;
            def.workerSlots = workerSlots;
            def.maxLevel = levels != null ? levels.Length : 1;
            def.levels = levels;
            def.size = Vector2Int.one;
            return def;
        }

        private PlacedBuildingRuntimeData FindByInstanceId(string instanceId)
        {
            for (int i = 0; i < placedBuildings.Count; i++)
            {
                if (placedBuildings[i].instanceId == instanceId)
                {
                    return placedBuildings[i];
                }
            }

            return null;
        }

        private BuildingDef GetDef(string defId)
        {
            if (string.IsNullOrWhiteSpace(defId))
            {
                return null;
            }

            defsById.TryGetValue(defId, out BuildingDef def);
            return def;
        }

        private Dictionary<SurvivorJob, int> BuildWorkerPool()
        {
            Dictionary<SurvivorJob, int> pool = new Dictionary<SurvivorJob, int>
            {
                [SurvivorJob.Lumberjack] = survivorSystem.GetCountByJob(SurvivorJob.Lumberjack),
                [SurvivorJob.Cook] = survivorSystem.GetCountByJob(SurvivorJob.Cook),
                [SurvivorJob.Builder] = survivorSystem.GetCountByJob(SurvivorJob.Builder),
                [SurvivorJob.Explorer] = survivorSystem.GetCountByJob(SurvivorJob.Explorer),
                [SurvivorJob.Medic] = survivorSystem.GetCountByJob(SurvivorJob.Medic),
                [SurvivorJob.Collector] = survivorSystem.GetCountByJob(SurvivorJob.Collector)
            };
            return pool;
        }

        private SurvivorJob GetRequiredJob(string buildingDefId)
        {
            switch (buildingDefId)
            {
                case "lumber_mill":
                    return SurvivorJob.Lumberjack;
                case "kitchen":
                    return SurvivorJob.Cook;
                case "heater":
                case "generator":
                    return SurvivorJob.Builder;
                default:
                    return SurvivorJob.Idle;
            }
        }

        private bool TryFindPlacement(Vector2Int size, out int x, out int y)
        {
            for (int py = 0; py < baseGridHeight; py++)
            {
                for (int px = 0; px < baseGridWidth; px++)
                {
                    if (IsAreaFree(px, py, size))
                    {
                        x = px;
                        y = py;
                        return true;
                    }
                }
            }

            x = -1;
            y = -1;
            return false;
        }

        private bool IsAreaFree(int startX, int startY, Vector2Int size)
        {
            Vector2Int clamped = ClampSize(size);
            int w = clamped.x;
            int h = clamped.y;

            if (!IsInsideGrid(startX, startY) || startX + w > baseGridWidth || startY + h > baseGridHeight)
            {
                return false;
            }

            for (int i = 0; i < placedBuildings.Count; i++)
            {
                PlacedBuildingRuntimeData existing = placedBuildings[i];
                BuildingDef existingDef = GetDef(existing.defId);
                Vector2Int existingSize = existingDef != null ? ClampSize(existingDef.size) : Vector2Int.one;
                int exW = existingSize.x;
                int exH = existingSize.y;

                bool overlap =
                    startX < existing.gridX + exW &&
                    startX + w > existing.gridX &&
                    startY < existing.gridY + exH &&
                    startY + h > existing.gridY;
                if (overlap)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsInsideGrid(int x, int y)
        {
            return x >= 0 && y >= 0 && x < baseGridWidth && y < baseGridHeight;
        }

        private Vector2Int ClampSize(Vector2Int size)
        {
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        }

        private void SpawnStartingShelter()
        {
            if (GetDef("shelter") == null)
            {
                return;
            }

            int x = Mathf.Clamp(baseGridWidth / 2, 0, baseGridWidth - 1);
            int y = Mathf.Clamp(baseGridHeight / 2, 0, baseGridHeight - 1);
            if (!IsAreaFree(x, y, Vector2Int.one))
            {
                if (!TryFindPlacement(Vector2Int.one, out x, out y))
                {
                    return;
                }
            }

            placedBuildings.Add(new PlacedBuildingRuntimeData
            {
                instanceId = "building_hq_start",
                defId = "shelter",
                level = 0,
                gridX = x,
                gridY = y,
                assignedWorkers = 0
            });
        }

        private void RecalculateGlobalBonuses()
        {
            int storageBonus = 0;
            int capBonus = 0;
            for (int i = 0; i < placedBuildings.Count; i++)
            {
                BuildingDef def = GetDef(placedBuildings[i].defId);
                if (def == null)
                {
                    continue;
                }

                BuildingLevelDef levelDef = def.GetLevel(placedBuildings[i].level);
                storageBonus += Mathf.Max(0, levelDef.storageCapBonus);
                capBonus += Mathf.Max(0, levelDef.survivorCapBonus);
            }

            if (resourceSystem != null)
            {
                resourceSystem.SetStorageBonus(storageBonus);
            }

            if (survivorSystem != null)
            {
                survivorSystem.SetSurvivorCapBonus(capBonus);
            }
        }

        private ResourceAmount[] Cost(params object[] flat)
        {
            List<ResourceAmount> list = new List<ResourceAmount>();
            for (int i = 0; i + 1 < flat.Length; i += 2)
            {
                ResourceType type = (ResourceType)flat[i];
                int amount = Convert.ToInt32(flat[i + 1]);
                list.Add(new ResourceAmount { type = type, amount = Mathf.Max(0, amount) });
            }

            return list.ToArray();
        }

        private ResourceAmount[] Scale(ResourceAmount[] values, float multiplier)
        {
            if (values == null || values.Length == 0)
            {
                return null;
            }

            ResourceAmount[] scaled = new ResourceAmount[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                ResourceAmount source = values[i];
                if (source == null)
                {
                    continue;
                }

                scaled[i] = new ResourceAmount
                {
                    type = source.type,
                    amount = ScaleAmount(source.amount, multiplier)
                };
            }

            return scaled;
        }

        private int ScaleAmount(int baseAmount, float multiplier)
        {
            if (baseAmount == 0 || Mathf.Approximately(multiplier, 0f))
            {
                return 0;
            }

            int scaled = Mathf.RoundToInt(baseAmount * multiplier);
            if (baseAmount > 0 && scaled <= 0)
            {
                scaled = 1;
            }
            else if (baseAmount < 0 && scaled >= 0)
            {
                scaled = -1;
            }

            return scaled;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
