using System;
using UnityEngine;

namespace FrozenFrontier.Data
{
    [CreateAssetMenu(menuName = "Frozen Frontier/Building Definition", fileName = "BuildingDef")]
    public class BuildingDef : ScriptableObject
    {
        public string id = "building_id";
        public string displayName = "Building";
        public Sprite icon;
        public Vector2Int size = Vector2Int.one;
        [Min(1)] public int workerSlots = 1;
        [Min(1)] public int maxLevel = 1;
        public BuildingLevelDef[] levels;

        public BuildingLevelDef GetLevel(int levelIndex)
        {
            if (levels == null || levels.Length == 0)
            {
                return new BuildingLevelDef();
            }

            if (levelIndex < 0)
            {
                levelIndex = 0;
            }

            if (levelIndex >= levels.Length)
            {
                levelIndex = levels.Length - 1;
            }

            return levels[levelIndex] ?? new BuildingLevelDef();
        }
    }

    [Serializable]
    public class BuildingLevelDef
    {
        public ResourceAmount[] buildCosts;
        public ResourceAmount[] perTickProduction;
        public ResourceAmount[] perTickConsumption;
        public int heatDeltaPerTick;
        public int powerDeltaPerTick;
        public int survivorCapBonus;
        public int storageCapBonus;
        [Range(0.05f, 1f)] public float noWorkerOutputMultiplier = 0.2f;
    }
}
