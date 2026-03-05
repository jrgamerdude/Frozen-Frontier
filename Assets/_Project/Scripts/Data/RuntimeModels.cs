using System;

namespace FrozenFrontier.Data
{
    [Serializable]
    public class PlacedBuildingRuntimeData
    {
        public string instanceId = "";
        public string defId = "";
        public int level;
        public int gridX;
        public int gridY;
        public int assignedWorkers;
    }

    [Serializable]
    public class MapTileRuntimeData
    {
        public string id = "";
        public int x;
        public int y;
        public string biomeId = "";
        public TileState state;
        public bool isExploring;
        public int exploreTicksRemaining;
    }

    [Serializable]
    public class SurvivorRuntimeData
    {
        public string id = "";
        public string displayName = "";
        public SurvivorJob job;
        public int health;
        public int morale;
        public int hunger;
        public int warmth;
    }
}
