using System;
using System.Collections.Generic;
using FrozenFrontier.Data;
using UnityEngine;

namespace FrozenFrontier.Systems
{
    public class MapSystem : MonoBehaviour
    {
        [SerializeField, Min(3)] private int mapWidth = 64;
        [SerializeField, Min(3)] private int mapHeight = 64;
        [SerializeField] private List<TileBiomeDef> biomeDefinitions = new List<TileBiomeDef>();

        [Header("Unlock Cost")]
        [SerializeField, Min(0)] private int unlockWoodCost = 12;
        [SerializeField, Min(0)] private int unlockScrapCost = 8;

        private readonly List<MapTileRuntimeData> tiles = new List<MapTileRuntimeData>();
        private readonly Dictionary<string, MapTileRuntimeData> tilesById = new Dictionary<string, MapTileRuntimeData>();
        private readonly Dictionary<string, TileBiomeDef> biomeById = new Dictionary<string, TileBiomeDef>();
        private readonly List<TileBiomeDef> fallbackBiomes = new List<TileBiomeDef>();
        private readonly List<MapTileRuntimeData> exploringTiles = new List<MapTileRuntimeData>();
        private System.Random rng;

        private ResourceSystem resourceSystem;
        private SurvivorSystem survivorSystem;

        public event Action Changed;
        public event Action<string> ToastRequested;

        public IReadOnlyList<MapTileRuntimeData> Tiles => tiles;
        public int Width => mapWidth;
        public int Height => mapHeight;

        public void Initialize(ResourceSystem resources, SurvivorSystem survivors)
        {
            resourceSystem = resources;
            survivorSystem = survivors;
            if (rng == null)
            {
                rng = new System.Random(1337);
            }

            EnsureBiomes();
            if (tiles.Count == 0)
            {
                GenerateDefaultMap();
            }

            NotifyChanged();
        }

        public bool TryUnlockTile(string tileId)
        {
            if (resourceSystem == null)
            {
                return false;
            }

            MapTileRuntimeData tile = FindTile(tileId);
            if (tile == null || tile.state != TileState.Locked)
            {
                return false;
            }

            if (!HasUnlockedAdjacent(tile.x, tile.y))
            {
                ToastRequested?.Invoke("Can only unlock tiles adjacent to explored territory.");
                return false;
            }

            ResourceAmount[] cost =
            {
                new ResourceAmount { type = ResourceType.Wood, amount = unlockWoodCost },
                new ResourceAmount { type = ResourceType.Scrap, amount = unlockScrapCost }
            };
            if (!resourceSystem.TrySpend(cost))
            {
                ToastRequested?.Invoke("Not enough resources to unlock tile.");
                return false;
            }

            tile.state = TileState.Unlocked;
            NotifyChanged();
            ToastRequested?.Invoke($"Tile {tile.x},{tile.y} unlocked.");
            return true;
        }

        public bool TryStartExploration(string tileId)
        {
            if (survivorSystem == null)
            {
                return false;
            }

            MapTileRuntimeData tile = FindTile(tileId);
            if (tile == null || tile.state != TileState.Unlocked || tile.isExploring)
            {
                return false;
            }

            if (survivorSystem.GetCountByJob(SurvivorJob.Explorer) <= 0)
            {
                ToastRequested?.Invoke("Assign at least one Explorer before scouting.");
                return false;
            }

            TileBiomeDef biome = GetBiome(tile.biomeId);
            tile.exploreTicksRemaining = Mathf.Max(4, biome != null ? biome.exploreDurationTicks : 8);
            tile.isExploring = true;
            AddExploringTile(tile);
            NotifyChanged();
            ToastRequested?.Invoke($"Exploration started on tile {tile.x},{tile.y}");
            return true;
        }

        public bool TryGetTileAt(int x, int y, out MapTileRuntimeData tile)
        {
            tile = null;
            if (!IsInsideBounds(x, y))
            {
                return false;
            }

            return tilesById.TryGetValue(TileId(x, y), out tile) && tile != null;
        }

        public bool TryUnlockTileAt(int x, int y)
        {
            return TryGetTileAt(x, y, out MapTileRuntimeData tile) && TryUnlockTile(tile.id);
        }

        public bool TryStartExplorationAt(int x, int y)
        {
            return TryGetTileAt(x, y, out MapTileRuntimeData tile) && TryStartExploration(tile.id);
        }

        public void Tick(bool offlineMode)
        {
            bool changed = false;
            for (int i = exploringTiles.Count - 1; i >= 0; i--)
            {
                MapTileRuntimeData tile = exploringTiles[i];
                if (tile == null || !tile.isExploring)
                {
                    exploringTiles.RemoveAt(i);
                    continue;
                }

                tile.exploreTicksRemaining = Mathf.Max(0, tile.exploreTicksRemaining - 1);
                changed = true;
                if (tile.exploreTicksRemaining <= 0)
                {
                    ResolveExploration(tile, offlineMode);
                }
            }

            if (changed)
            {
                NotifyChanged();
            }
        }

        public MapSystemSaveData ExportState()
        {
            MapSystemSaveData data = new MapSystemSaveData
            {
                width = mapWidth,
                height = mapHeight
            };

            for (int i = 0; i < tiles.Count; i++)
            {
                MapTileRuntimeData tile = tiles[i];
                data.tiles.Add(new MapTileSaveData
                {
                    id = tile.id,
                    x = tile.x,
                    y = tile.y,
                    biomeId = tile.biomeId,
                    state = tile.state,
                    isExploring = tile.isExploring,
                    exploreTicksRemaining = tile.exploreTicksRemaining
                });
            }

            return data;
        }

        public void ImportState(MapSystemSaveData data)
        {
            tiles.Clear();
            tilesById.Clear();
            exploringTiles.Clear();
            if (rng == null)
            {
                rng = new System.Random(1337);
            }

            if (data != null && data.tiles != null && data.tiles.Count > 0)
            {
                mapWidth = Mathf.Max(3, data.width);
                mapHeight = Mathf.Max(3, data.height);
                for (int i = 0; i < data.tiles.Count; i++)
                {
                    MapTileSaveData tile = data.tiles[i];
                    if (tile == null || !IsInsideBounds(tile.x, tile.y))
                    {
                        continue;
                    }

                    MapTileRuntimeData runtimeTile = new MapTileRuntimeData
                    {
                        id = TileId(tile.x, tile.y),
                        x = tile.x,
                        y = tile.y,
                        biomeId = tile.biomeId,
                        state = tile.state,
                        isExploring = tile.isExploring,
                        exploreTicksRemaining = Mathf.Max(0, tile.exploreTicksRemaining)
                    };
                    TryAddTile(runtimeTile);
                }

                if (tiles.Count == 0)
                {
                    GenerateDefaultMap();
                }
            }
            else
            {
                GenerateDefaultMap();
            }

            EnsureBiomes();
            RebuildExploringTiles();
            NotifyChanged();
        }

        private void GenerateDefaultMap()
        {
            tiles.Clear();
            tilesById.Clear();
            EnsureBiomes();
            if (rng == null)
            {
                rng = new System.Random(1337);
            }

            List<TileBiomeDef> biomes = GetUsableBiomes();
            if (biomes.Count == 0)
            {
                Debug.LogError("MapSystem: no valid biome definitions available. Cannot generate default map.");
                return;
            }

            int centerX = mapWidth / 2;
            int centerY = mapHeight / 2;
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    TileBiomeDef biome = biomes[rng.Next(0, biomes.Count)];
                    if (biome == null || string.IsNullOrWhiteSpace(biome.id))
                    {
                        biome = biomes[0];
                    }

                    MapTileRuntimeData tile = new MapTileRuntimeData
                    {
                        id = TileId(x, y),
                        x = x,
                        y = y,
                        biomeId = biome.id,
                        state = x == centerX && y == centerY ? TileState.Cleared : TileState.Locked,
                        isExploring = false,
                        exploreTicksRemaining = 0
                    };

                    TryAddTile(tile);
                }
            }

            RebuildExploringTiles();
        }

        private void EnsureBiomes()
        {
            biomeById.Clear();

            if (biomeDefinitions == null || biomeDefinitions.Count == 0)
            {
                BuildFallbackBiomes();
                biomeDefinitions = new List<TileBiomeDef>(fallbackBiomes);
            }

            for (int i = 0; i < biomeDefinitions.Count; i++)
            {
                TileBiomeDef biome = biomeDefinitions[i];
                if (biome == null || string.IsNullOrWhiteSpace(biome.id))
                {
                    continue;
                }

                biomeById[biome.id] = biome;
            }

            if (biomeById.Count == 0)
            {
                BuildFallbackBiomes();
                biomeDefinitions = new List<TileBiomeDef>(fallbackBiomes);
                for (int i = 0; i < biomeDefinitions.Count; i++)
                {
                    TileBiomeDef biome = biomeDefinitions[i];
                    if (biome != null && !string.IsNullOrWhiteSpace(biome.id))
                    {
                        biomeById[biome.id] = biome;
                    }
                }
            }
        }

        private void BuildFallbackBiomes()
        {
            if (fallbackBiomes.Count > 0)
            {
                return;
            }

            fallbackBiomes.Add(CreateBiome(
                "forest",
                "Frozen Forest",
                18,
                7,
                new[]
                {
                    new ResourceAmount { type = ResourceType.Wood, amount = 30 },
                    new ResourceAmount { type = ResourceType.Food, amount = 12 },
                    new ResourceAmount { type = ResourceType.Scrap, amount = 6 }
                }));

            fallbackBiomes.Add(CreateBiome(
                "ruins",
                "Buried Ruins",
                28,
                9,
                new[]
                {
                    new ResourceAmount { type = ResourceType.Scrap, amount = 24 },
                    new ResourceAmount { type = ResourceType.Wood, amount = 10 },
                    new ResourceAmount { type = ResourceType.Fuel, amount = 5 }
                }));

            fallbackBiomes.Add(CreateBiome(
                "tundra",
                "Icy Tundra",
                35,
                10,
                new[]
                {
                    new ResourceAmount { type = ResourceType.Food, amount = 20 },
                    new ResourceAmount { type = ResourceType.Fuel, amount = 10 }
                }));

            fallbackBiomes.Add(CreateBiome(
                "fuel_cache",
                "Fuel Cache",
                22,
                8,
                new[]
                {
                    new ResourceAmount { type = ResourceType.Fuel, amount = 24 },
                    new ResourceAmount { type = ResourceType.Scrap, amount = 9 }
                }));
        }

        private TileBiomeDef CreateBiome(string id, string displayName, int hazardChance, int exploreTicks, ResourceAmount[] rewards)
        {
            TileBiomeDef biome = ScriptableObject.CreateInstance<TileBiomeDef>();
            biome.id = id;
            biome.displayName = displayName;
            biome.hazardChancePercent = hazardChance;
            biome.exploreDurationTicks = exploreTicks;
            biome.baseRewards = rewards;
            return biome;
        }

        private void ResolveExploration(MapTileRuntimeData tile, bool offlineMode)
        {
            TileBiomeDef biome = GetBiome(tile.biomeId);
            tile.isExploring = false;
            tile.exploreTicksRemaining = 0;
            tile.state = TileState.Cleared;
            RemoveExploringTile(tile);

            if (biome != null && biome.baseRewards != null)
            {
                ResourceAmount[] rewards = new ResourceAmount[biome.baseRewards.Length];
                for (int i = 0; i < biome.baseRewards.Length; i++)
                {
                    ResourceAmount reward = biome.baseRewards[i];
                    if (reward == null)
                    {
                        continue;
                    }

                    float spread = UnityEngine.Random.Range(0.8f, 1.2f);
                    rewards[i] = new ResourceAmount
                    {
                        type = reward.type,
                        amount = Mathf.Max(1, Mathf.RoundToInt(reward.amount * spread))
                    };
                }

                if (resourceSystem != null)
                {
                    resourceSystem.ApplyDeltas(rewards);
                }
            }

            int hazardChance = biome != null ? biome.hazardChancePercent : 20;
            bool hazardTriggered = UnityEngine.Random.Range(0, 100) < hazardChance;
            if (hazardTriggered)
            {
                int healthLoss = UnityEngine.Random.Range(1, 4);
                int moraleLoss = UnityEngine.Random.Range(2, 6);
                if (survivorSystem != null)
                {
                    survivorSystem.ApplyHealthDelta(-healthLoss);
                    survivorSystem.ApplyMoraleDelta(-moraleLoss);
                }

                if (resourceSystem != null)
                {
                    resourceSystem.ModifyHeat(-1);
                }

                if (!offlineMode)
                {
                    ToastRequested?.Invoke($"Hazard on tile {tile.x},{tile.y}: morale and health reduced.");
                }
            }
            else if (!offlineMode)
            {
                ToastRequested?.Invoke($"Tile {tile.x},{tile.y} cleared. Rewards collected.");
            }
        }

        private MapTileRuntimeData FindTile(string tileId)
        {
            if (string.IsNullOrWhiteSpace(tileId))
            {
                return null;
            }

            if (tilesById.TryGetValue(tileId, out MapTileRuntimeData cached) && cached != null)
            {
                return cached;
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].id == tileId)
                {
                    tilesById[tileId] = tiles[i];
                    return tiles[i];
                }
            }

            return null;
        }

        private TileBiomeDef GetBiome(string biomeId)
        {
            if (string.IsNullOrWhiteSpace(biomeId))
            {
                return null;
            }

            biomeById.TryGetValue(biomeId, out TileBiomeDef biome);
            return biome;
        }

        private string TileId(int x, int y)
        {
            return $"tile_{x}_{y}";
        }

        private List<TileBiomeDef> GetUsableBiomes()
        {
            List<TileBiomeDef> result = new List<TileBiomeDef>();
            if (biomeDefinitions != null)
            {
                for (int i = 0; i < biomeDefinitions.Count; i++)
                {
                    TileBiomeDef biome = biomeDefinitions[i];
                    if (biome == null || string.IsNullOrWhiteSpace(biome.id))
                    {
                        continue;
                    }

                    result.Add(biome);
                }
            }

            if (result.Count == 0)
            {
                BuildFallbackBiomes();
                for (int i = 0; i < fallbackBiomes.Count; i++)
                {
                    TileBiomeDef biome = fallbackBiomes[i];
                    if (biome == null || string.IsNullOrWhiteSpace(biome.id))
                    {
                        continue;
                    }

                    result.Add(biome);
                }
            }

            return result;
        }

        private bool HasUnlockedAdjacent(int x, int y)
        {
            return IsUnlocked(x - 1, y) || IsUnlocked(x + 1, y) || IsUnlocked(x, y - 1) || IsUnlocked(x, y + 1);
        }

        private bool IsUnlocked(int x, int y)
        {
            if (!TryGetTileAt(x, y, out MapTileRuntimeData tile))
            {
                return false;
            }

            return tile.state == TileState.Unlocked || tile.state == TileState.Cleared;
        }

        private bool TryAddTile(MapTileRuntimeData tile)
        {
            if (tile == null)
            {
                return false;
            }

            if (!IsInsideBounds(tile.x, tile.y))
            {
                return false;
            }

            string id = TileId(tile.x, tile.y);
            if (tilesById.ContainsKey(id))
            {
                return false;
            }

            tile.id = id;
            tiles.Add(tile);
            tilesById[id] = tile;
            if (tile.isExploring)
            {
                AddExploringTile(tile);
            }
            return true;
        }

        private bool IsInsideBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < mapWidth && y < mapHeight;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }

        private void AddExploringTile(MapTileRuntimeData tile)
        {
            if (tile == null || !tile.isExploring)
            {
                return;
            }

            if (!exploringTiles.Contains(tile))
            {
                exploringTiles.Add(tile);
            }
        }

        private void RemoveExploringTile(MapTileRuntimeData tile)
        {
            if (tile == null)
            {
                return;
            }

            exploringTiles.Remove(tile);
        }

        private void RebuildExploringTiles()
        {
            exploringTiles.Clear();
            for (int i = 0; i < tiles.Count; i++)
            {
                MapTileRuntimeData tile = tiles[i];
                if (tile != null && tile.isExploring)
                {
                    exploringTiles.Add(tile);
                }
            }
        }
    }
}
