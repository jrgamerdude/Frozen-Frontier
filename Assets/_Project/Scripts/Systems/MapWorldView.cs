using System.Collections.Generic;
using FrozenFrontier.Data;
using UnityEngine;

namespace FrozenFrontier.Systems
{
    public class MapWorldView : MonoBehaviour
    {
        [SerializeField] private MapSystem mapSystem;
        [SerializeField] private Transform mapRoot;
        [SerializeField] private Vector2 mapOrigin = new Vector2(-24f, -24f);
        [SerializeField, Min(0.1f)] private float cellWorldSize = 0.75f;
        [SerializeField, Range(0.4f, 1f)] private float tileScale = 0.92f;
        [SerializeField] private int tileSortingOrder = 10;

        [Header("State Colors")]
        [SerializeField] private Color lockedColor = new Color(0.09f, 0.14f, 0.2f, 0.88f);
        [SerializeField] private Color unlockedColor = new Color(0.22f, 0.4f, 0.56f, 0.92f);
        [SerializeField] private Color clearedColor = new Color(0.58f, 0.78f, 0.92f, 0.96f);
        [SerializeField] private Color exploringColor = new Color(0.73f, 0.92f, 1f, 1f);
        [SerializeField, Min(0.1f)] private float explorePulseSpeed = 2.5f;

        private readonly Dictionary<string, SpriteRenderer> tileRenderers = new Dictionary<string, SpriteRenderer>();
        private readonly Dictionary<string, Color> tileBaseColors = new Dictionary<string, Color>();
        private readonly List<string> exploringTileIds = new List<string>();
        private readonly List<string> toRemove = new List<string>();
        private Sprite squareSprite;

        public MapSystem MapSystem => mapSystem;
        public float CellWorldSize => cellWorldSize;

        private void Awake()
        {
            if (mapSystem == null)
            {
                mapSystem = FindFirstObjectByType<MapSystem>();
            }

            if (mapRoot == null)
            {
                mapRoot = transform;
            }

            EnsureSprite();
        }

        private void OnEnable()
        {
            if (mapSystem != null)
            {
                mapSystem.Changed += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (mapSystem != null)
            {
                mapSystem.Changed -= Refresh;
            }
        }

        private void Update()
        {
            if (exploringTileIds.Count == 0)
            {
                return;
            }

            float pulse = 0.5f + Mathf.Sin(Time.time * explorePulseSpeed) * 0.5f;
            for (int i = 0; i < exploringTileIds.Count; i++)
            {
                string tileId = exploringTileIds[i];
                if (!tileRenderers.TryGetValue(tileId, out SpriteRenderer renderer) || renderer == null)
                {
                    continue;
                }

                if (!tileBaseColors.TryGetValue(tileId, out Color baseColor))
                {
                    baseColor = unlockedColor;
                }

                Color pulsed = Color.Lerp(baseColor, exploringColor, 0.4f + pulse * 0.4f);
                pulsed.a = Mathf.Lerp(baseColor.a, exploringColor.a, 0.45f + pulse * 0.4f);
                renderer.color = pulsed;
            }
        }

        public void Refresh()
        {
            if (mapSystem == null)
            {
                return;
            }

            EnsureSprite();
            exploringTileIds.Clear();
            tileBaseColors.Clear();

            IReadOnlyList<MapTileRuntimeData> tiles = mapSystem.Tiles;
            HashSet<string> activeIds = new HashSet<string>();
            for (int i = 0; i < tiles.Count; i++)
            {
                MapTileRuntimeData tile = tiles[i];
                if (tile == null || string.IsNullOrWhiteSpace(tile.id))
                {
                    continue;
                }

                activeIds.Add(tile.id);
                if (!tileRenderers.TryGetValue(tile.id, out SpriteRenderer renderer) || renderer == null)
                {
                    renderer = CreateRenderer(tile.id);
                    tileRenderers[tile.id] = renderer;
                }

                if (renderer == null)
                {
                    continue;
                }

                renderer.transform.position = GridToWorldCenter(tile.x, tile.y);
                renderer.transform.localScale = Vector3.one * cellWorldSize * Mathf.Clamp(tileScale, 0.4f, 1f);
                renderer.sortingOrder = tileSortingOrder;

                Color baseColor = GetTileStateColor(tile);
                tileBaseColors[tile.id] = baseColor;
                if (tile.isExploring)
                {
                    exploringTileIds.Add(tile.id);
                }
                else
                {
                    renderer.color = baseColor;
                }
            }

            toRemove.Clear();
            foreach (KeyValuePair<string, SpriteRenderer> pair in tileRenderers)
            {
                if (!activeIds.Contains(pair.Key))
                {
                    if (pair.Value != null)
                    {
                        Destroy(pair.Value.gameObject);
                    }

                    toRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                string id = toRemove[i];
                tileRenderers.Remove(id);
                tileBaseColors.Remove(id);
            }
        }

        public bool WorldToGrid(Vector3 worldPos, out int x, out int y)
        {
            float size = Mathf.Max(0.1f, cellWorldSize);
            Vector3 local = worldPos - GetGridOriginWorld();
            x = Mathf.FloorToInt(local.x / size);
            y = Mathf.FloorToInt(local.y / size);

            if (mapSystem == null)
            {
                return false;
            }

            return x >= 0 && y >= 0 && x < mapSystem.Width && y < mapSystem.Height;
        }

        public Vector3 GridToWorldCenter(int x, int y)
        {
            Vector3 origin = GetGridOriginWorld();
            float size = Mathf.Max(0.1f, cellWorldSize);
            return origin + new Vector3((x + 0.5f) * size, (y + 0.5f) * size, 0f);
        }

        public Vector3 GetGridOriginWorld()
        {
            Vector3 root = mapRoot != null ? mapRoot.position : transform.position;
            return root + new Vector3(mapOrigin.x, mapOrigin.y, 0f);
        }

        public Bounds GetWorldBounds()
        {
            if (mapSystem == null || mapSystem.Width <= 0 || mapSystem.Height <= 0)
            {
                return new Bounds(GetGridOriginWorld(), new Vector3(4f, 4f, 0f));
            }

            float size = Mathf.Max(0.1f, cellWorldSize);
            float width = mapSystem.Width * size;
            float height = mapSystem.Height * size;
            Vector3 origin = GetGridOriginWorld();
            Vector3 center = origin + new Vector3(width * 0.5f, height * 0.5f, 0f);
            return new Bounds(center, new Vector3(width, height, 0f));
        }

        private SpriteRenderer CreateRenderer(string tileId)
        {
            Transform parent = mapRoot != null ? mapRoot : transform;
            GameObject tile = new GameObject($"MapTile_{tileId}", typeof(SpriteRenderer));
            tile.transform.SetParent(parent, false);
            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.sortingOrder = tileSortingOrder;
            return renderer;
        }

        private Color GetTileStateColor(MapTileRuntimeData tile)
        {
            Color biomeColor = GetBiomeTint(tile != null ? tile.biomeId : "");
            if (tile == null)
            {
                return lockedColor;
            }

            switch (tile.state)
            {
                case TileState.Unlocked:
                    return Color.Lerp(unlockedColor, biomeColor, 0.5f);
                case TileState.Cleared:
                    return Color.Lerp(clearedColor, biomeColor, 0.35f);
                default:
                    return Color.Lerp(lockedColor, biomeColor, 0.2f);
            }
        }

        private Color GetBiomeTint(string biomeId)
        {
            switch (biomeId)
            {
                case "forest":
                    return new Color(0.3f, 0.64f, 0.42f, 1f);
                case "ruins":
                    return new Color(0.56f, 0.52f, 0.48f, 1f);
                case "tundra":
                    return new Color(0.74f, 0.86f, 0.93f, 1f);
                case "fuel_cache":
                    return new Color(0.56f, 0.46f, 0.31f, 1f);
                default:
                    return new Color(0.42f, 0.58f, 0.74f, 1f);
            }
        }

        private void EnsureSprite()
        {
            if (squareSprite != null)
            {
                return;
            }

            Texture2D texture = Texture2D.whiteTexture;
            squareSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
        }
    }
}
