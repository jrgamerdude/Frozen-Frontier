using System;
using System.Collections.Generic;

namespace FrozenFrontier.Data
{
    [Serializable]
    public class GameSaveData
    {
        public int saveVersion = 2;
        public string lastSaveUtc = "";
        public ResourceSystemSaveData resourceState = new ResourceSystemSaveData();
        public SurvivorSystemSaveData survivorState = new SurvivorSystemSaveData();
        public BuildingSystemSaveData buildingState = new BuildingSystemSaveData();
        public MapSystemSaveData mapState = new MapSystemSaveData();
        public EventSystemSaveData eventState = new EventSystemSaveData();
        public TimeSystemSaveData timeState = new TimeSystemSaveData();
    }

    [Serializable]
    public class ResourceSystemSaveData
    {
        public List<ResourceEntrySaveData> resources = new List<ResourceEntrySaveData>();
        public int heat;
        public int power;
        public int storageBonus;
    }

    [Serializable]
    public class ResourceEntrySaveData
    {
        public ResourceType type;
        public int amount;
    }

    [Serializable]
    public class SurvivorSystemSaveData
    {
        public List<SurvivorSaveData> survivors = new List<SurvivorSaveData>();
    }

    [Serializable]
    public class SurvivorSaveData
    {
        public string id = "";
        public string displayName = "";
        public SurvivorJob job;
        public int health;
        public int morale;
        public int hunger;
        public int warmth;
    }

    [Serializable]
    public class BuildingSystemSaveData
    {
        public List<PlacedBuildingSaveData> placedBuildings = new List<PlacedBuildingSaveData>();
    }

    [Serializable]
    public class PlacedBuildingSaveData
    {
        public string instanceId = "";
        public string defId = "";
        public int level;
        public int gridX;
        public int gridY;
        public int assignedWorkers;
    }

    [Serializable]
    public class MapSystemSaveData
    {
        public int width;
        public int height;
        public List<MapTileSaveData> tiles = new List<MapTileSaveData>();
    }

    [Serializable]
    public class MapTileSaveData
    {
        public string id = "";
        public int x;
        public int y;
        public string biomeId = "";
        public TileState state;
        public int exploreTicksRemaining;
        public bool isExploring;
    }

    [Serializable]
    public class EventSystemSaveData
    {
        public int ticksUntilNextEvent;
        public string activeEventId = "";
    }

    [Serializable]
    public class TimeSystemSaveData
    {
        public int totalTicks;
    }
}
