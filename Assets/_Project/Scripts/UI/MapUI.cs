using System.Collections.Generic;
using FrozenFrontier.Data;
using FrozenFrontier.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace FrozenFrontier.UI
{
    public class MapUI : MonoBehaviour
    {
        [SerializeField] private Transform tileRoot;
        [SerializeField] private Button tileButtonTemplate;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button unlockFirstButton;
        [SerializeField] private Button exploreFirstButton;
        [SerializeField, Min(1)] private int maxButtonTilesForGrid = 225;

        private readonly Dictionary<string, Button> buttonsById = new Dictionary<string, Button>();
        private readonly Dictionary<string, Text> labelsById = new Dictionary<string, Text>();
        private MapSystem mapSystem;
        private bool useButtonGrid;

        private void Awake()
        {
            UiScrollLayoutHelper.EnsureVerticalScroll(transform as RectTransform);
            UiScrollLayoutHelper.ConfigureMultilineText(summaryText);
            if (tileButtonTemplate != null)
            {
                Text templateLabel = tileButtonTemplate.GetComponentInChildren<Text>();
                UiScrollLayoutHelper.ConfigureButtonLabel(templateLabel);
            }
        }

        public void Bind(MapSystem map)
        {
            Unbind();
            mapSystem = map;

            if (mapSystem != null)
            {
                mapSystem.Changed += Refresh;
            }

            Wire(unlockFirstButton, UnlockFirstAdjacent);
            Wire(exploreFirstButton, ExploreFirstUnlocked);
            useButtonGrid = ShouldUseButtonGrid();
            RebuildGrid();
            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void UnlockFirstAdjacent()
        {
            if (mapSystem == null)
            {
                return;
            }

            IReadOnlyList<MapTileRuntimeData> tiles = mapSystem.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                MapTileRuntimeData tile = tiles[i];
                if (tile.state == TileState.Locked && HasUnlockedNeighbor(tile, tiles) && mapSystem.TryUnlockTile(tile.id))
                {
                    return;
                }
            }
        }

        public void ExploreFirstUnlocked()
        {
            if (mapSystem == null)
            {
                return;
            }

            IReadOnlyList<MapTileRuntimeData> tiles = mapSystem.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].state == TileState.Unlocked)
                {
                    mapSystem.TryStartExploration(tiles[i].id);
                    return;
                }
            }
        }

        private void Refresh()
        {
            if (mapSystem == null)
            {
                return;
            }

            bool shouldUseButtonGrid = ShouldUseButtonGrid();
            if (shouldUseButtonGrid != useButtonGrid)
            {
                useButtonGrid = shouldUseButtonGrid;
                RebuildGrid();
            }
            else if (useButtonGrid && buttonsById.Count != mapSystem.Tiles.Count)
            {
                RebuildGrid();
            }

            int locked = 0;
            int unlocked = 0;
            int cleared = 0;

            IReadOnlyList<MapTileRuntimeData> tiles = mapSystem.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                MapTileRuntimeData tile = tiles[i];
                switch (tile.state)
                {
                    case TileState.Locked:
                        locked++;
                        break;
                    case TileState.Unlocked:
                        unlocked++;
                        break;
                    case TileState.Cleared:
                        cleared++;
                        break;
                }

                if (useButtonGrid && labelsById.TryGetValue(tile.id, out Text label))
                {
                    string stateText = tile.state.ToString();
                    if (tile.isExploring)
                    {
                        stateText = $"Exploring ({tile.exploreTicksRemaining})";
                    }

                    label.text = $"{tile.x},{tile.y}\n{stateText}";
                }
            }

            if (summaryText != null)
            {
                string largeModeHint = useButtonGrid
                    ? ""
                    : "\nLarge map mode: use world clicks to unlock/explore. Quick actions remain available.";
                summaryText.text = $"Map {mapSystem.Width}x{mapSystem.Height}  Locked:{locked}  Unlocked:{unlocked}  Cleared:{cleared}{largeModeHint}";
            }
        }

        private void RebuildGrid()
        {
            ClearGrid();
            if (mapSystem == null || tileRoot == null || tileButtonTemplate == null)
            {
                return;
            }

            tileRoot.gameObject.SetActive(useButtonGrid);
            if (!useButtonGrid)
            {
                return;
            }

            IReadOnlyList<MapTileRuntimeData> tiles = mapSystem.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                MapTileRuntimeData tile = tiles[i];
                Button button = Instantiate(tileButtonTemplate, tileRoot);
                button.gameObject.SetActive(true);

                string tileId = tile.id;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnTilePressed(tileId));

                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    UiScrollLayoutHelper.ConfigureButtonLabel(label);
                    label.text = $"{tile.x},{tile.y}";
                }

                buttonsById[tileId] = button;
                labelsById[tileId] = label;
            }
        }

        private void OnTilePressed(string tileId)
        {
            if (mapSystem == null)
            {
                return;
            }

            IReadOnlyList<MapTileRuntimeData> tiles = mapSystem.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                MapTileRuntimeData tile = tiles[i];
                if (tile.id != tileId)
                {
                    continue;
                }

                if (tile.state == TileState.Locked)
                {
                    mapSystem.TryUnlockTile(tile.id);
                }
                else if (tile.state == TileState.Unlocked)
                {
                    mapSystem.TryStartExploration(tile.id);
                }

                return;
            }
        }

        private bool HasUnlockedNeighbor(MapTileRuntimeData tile, IReadOnlyList<MapTileRuntimeData> allTiles)
        {
            for (int i = 0; i < allTiles.Count; i++)
            {
                MapTileRuntimeData other = allTiles[i];
                if (other.state != TileState.Unlocked && other.state != TileState.Cleared)
                {
                    continue;
                }

                bool adjacent = Mathf.Abs(other.x - tile.x) + Mathf.Abs(other.y - tile.y) == 1;
                if (adjacent)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearGrid()
        {
            foreach (KeyValuePair<string, Button> pair in buttonsById)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            buttonsById.Clear();
            labelsById.Clear();
        }

        private void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void Unbind()
        {
            if (mapSystem != null)
            {
                mapSystem.Changed -= Refresh;
            }

            ClearGrid();
        }

        private bool ShouldUseButtonGrid()
        {
            if (mapSystem == null)
            {
                return false;
            }

            int threshold = Mathf.Max(1, maxButtonTilesForGrid);
            return mapSystem.Tiles.Count <= threshold;
        }
    }
}
