using System.Collections.Generic;
using FrozenFrontier.Data;
using UnityEngine;

namespace FrozenFrontier.Systems
{
    public class BuildingWorldView : MonoBehaviour
    {
        [SerializeField] private BuildingSystem buildingSystem;
        [SerializeField] private bool drawBaseGrid = true;
        [SerializeField] private Color gridColor = new Color(0.45f, 0.5f, 0.58f, 0.14f);
        [SerializeField] private Color emptyOutlineColor = new Color(0.35f, 0.42f, 0.52f, 0.18f);
        [SerializeField] private float gridCellScale = 0.95f;
        [SerializeField] private int gridSortingOrder = 20;
        [SerializeField] private int buildingSortingOrder = 120;

        private readonly Dictionary<string, SpriteRenderer> buildingRenderers = new Dictionary<string, SpriteRenderer>();
        private readonly List<SpriteRenderer> gridRenderers = new List<SpriteRenderer>();
        private Sprite squareSprite;

        private void Awake()
        {
            if (buildingSystem == null)
            {
                buildingSystem = FindFirstObjectByType<BuildingSystem>();
            }

            EnsureSprite();
        }

        private void OnEnable()
        {
            if (buildingSystem != null)
            {
                buildingSystem.Changed += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (buildingSystem != null)
            {
                buildingSystem.Changed -= Refresh;
            }
        }

        public void Refresh()
        {
            if (buildingSystem == null)
            {
                return;
            }

            EnsureSprite();
            SyncGrid();
            SyncBuildings();
        }

        private void SyncGrid()
        {
            if (!drawBaseGrid)
            {
                for (int i = 0; i < gridRenderers.Count; i++)
                {
                    if (gridRenderers[i] != null)
                    {
                        gridRenderers[i].enabled = false;
                    }
                }

                return;
            }

            int targetCount = buildingSystem.GridWidth * buildingSystem.GridHeight;
            while (gridRenderers.Count < targetCount)
            {
                GameObject cell = new GameObject($"GridCell_{gridRenderers.Count:00}", typeof(SpriteRenderer));
                Transform parent = buildingSystem.BaseAreaRoot != null ? buildingSystem.BaseAreaRoot : transform;
                cell.transform.SetParent(parent, false);
                SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
                renderer.sprite = squareSprite;
                renderer.sortingOrder = gridSortingOrder;
                gridRenderers.Add(renderer);
            }

            for (int i = 0; i < gridRenderers.Count; i++)
            {
                bool active = i < targetCount;
                if (gridRenderers[i] != null)
                {
                    gridRenderers[i].enabled = active && drawBaseGrid;
                }
            }

            int index = 0;
            for (int y = 0; y < buildingSystem.GridHeight; y++)
            {
                for (int x = 0; x < buildingSystem.GridWidth; x++)
                {
                    SpriteRenderer renderer = gridRenderers[index++];
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.transform.position = buildingSystem.GridToWorldCenter(x, y, Vector2Int.one);
                    renderer.transform.localScale = Vector3.one * buildingSystem.CellWorldSize * gridCellScale;
                    renderer.color = ((x + y) & 1) == 0 ? gridColor : emptyOutlineColor;
                }
            }
        }

        private void SyncBuildings()
        {
            HashSet<string> activeIds = new HashSet<string>();
            IReadOnlyList<PlacedBuildingRuntimeData> placed = buildingSystem.PlacedBuildings;
            for (int i = 0; i < placed.Count; i++)
            {
                PlacedBuildingRuntimeData building = placed[i];
                if (building == null || string.IsNullOrWhiteSpace(building.instanceId))
                {
                    continue;
                }

                activeIds.Add(building.instanceId);
                if (!buildingRenderers.TryGetValue(building.instanceId, out SpriteRenderer renderer) || renderer == null)
                {
                    GameObject go = new GameObject($"Building_{building.defId}_{building.instanceId}", typeof(SpriteRenderer));
                    Transform parent = buildingSystem.BaseAreaRoot != null ? buildingSystem.BaseAreaRoot : transform;
                    go.transform.SetParent(parent, false);
                    renderer = go.GetComponent<SpriteRenderer>();
                    renderer.sprite = squareSprite;
                    renderer.sortingOrder = buildingSortingOrder;
                    buildingRenderers[building.instanceId] = renderer;
                }

                BuildingDef def = buildingSystem.GetDefinitionById(building.defId);
                Vector2Int size = def != null ? def.size : Vector2Int.one;
                renderer.transform.position = buildingSystem.GridToWorldCenter(building.gridX, building.gridY, size);
                renderer.transform.localScale = new Vector3(
                    Mathf.Max(1, size.x) * buildingSystem.CellWorldSize * 0.9f,
                    Mathf.Max(1, size.y) * buildingSystem.CellWorldSize * 0.9f,
                    1f);
                renderer.color = GetBuildingColor(building.defId, building.level);
            }

            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, SpriteRenderer> pair in buildingRenderers)
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
                buildingRenderers.Remove(toRemove[i]);
            }
        }

        private Color GetBuildingColor(string defId, int level)
        {
            Color baseColor;
            switch (defId)
            {
                case "shelter":
                    baseColor = new Color(0.45f, 0.75f, 0.95f, 0.95f);
                    break;
                case "heater":
                    baseColor = new Color(0.95f, 0.52f, 0.35f, 0.95f);
                    break;
                case "lumber_mill":
                    baseColor = new Color(0.38f, 0.76f, 0.46f, 0.95f);
                    break;
                case "kitchen":
                    baseColor = new Color(0.93f, 0.74f, 0.32f, 0.95f);
                    break;
                case "storage":
                    baseColor = new Color(0.58f, 0.59f, 0.66f, 0.95f);
                    break;
                case "generator":
                    baseColor = new Color(0.83f, 0.5f, 0.95f, 0.95f);
                    break;
                default:
                    baseColor = new Color(0.74f, 0.74f, 0.78f, 0.95f);
                    break;
            }

            float brightness = Mathf.Clamp01(0.85f + level * 0.08f);
            return new Color(baseColor.r * brightness, baseColor.g * brightness, baseColor.b * brightness, baseColor.a);
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
