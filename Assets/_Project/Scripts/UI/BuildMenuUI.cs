using System.Collections.Generic;
using FrozenFrontier.Core;
using FrozenFrontier.Data;
using FrozenFrontier.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace FrozenFrontier.UI
{
    public class BuildMenuUI : MonoBehaviour
    {
        [Header("Optional Dynamic List")]
        [SerializeField] private Transform buildButtonRoot;
        [SerializeField] private Button buildButtonTemplate;

        [Header("Optional Static Buttons")]
        [SerializeField] private Button buildShelterButton;
        [SerializeField] private Button buildHeaterButton;
        [SerializeField] private Button buildLumberMillButton;
        [SerializeField] private Button buildKitchenButton;
        [SerializeField] private Button buildStorageButton;
        [SerializeField] private Button buildGeneratorButton;

        [Header("Optional Upgrade Buttons")]
        [SerializeField] private Button upgradeShelterButton;
        [SerializeField] private Button upgradeHeaterButton;
        [SerializeField] private Button upgradeLumberMillButton;
        [SerializeField] private Button upgradeKitchenButton;
        [SerializeField] private Button upgradeStorageButton;
        [SerializeField] private Button upgradeGeneratorButton;
        [SerializeField] private Button cancelPlacementButton;

        [Header("Text")]
        [SerializeField] private Text summaryText;

        private readonly List<Button> spawnedButtons = new List<Button>();
        private BuildingSystem buildingSystem;
        private GameManager gameManager;

        private void Awake()
        {
            UiScrollLayoutHelper.EnsureVerticalScroll(transform as RectTransform);
            UiScrollLayoutHelper.ConfigureMultilineText(summaryText);
        }

        public void Bind(BuildingSystem buildings, GameManager manager)
        {
            Unbind();
            buildingSystem = buildings;
            gameManager = manager;

            if (buildingSystem != null)
            {
                buildingSystem.Changed += Refresh;
                buildingSystem.PlacementModeChanged += OnPlacementModeChanged;
            }

            WireStaticButtons();
            RebuildDynamicButtons();
            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void BuildById(string defId)
        {
            if (buildingSystem == null)
            {
                return;
            }

            buildingSystem.BeginPlacement(defId);
        }

        public void UpgradeFirstById(string defId)
        {
            if (buildingSystem == null)
            {
                return;
            }

            buildingSystem.UpgradeFirstByDef(defId);
        }

        private void Refresh()
        {
            if (summaryText == null || buildingSystem == null)
            {
                return;
            }

            Dictionary<string, int> counts = new Dictionary<string, int>();
            IReadOnlyList<PlacedBuildingRuntimeData> placed = buildingSystem.PlacedBuildings;
            for (int i = 0; i < placed.Count; i++)
            {
                string defId = placed[i].defId;
                if (!counts.ContainsKey(defId))
                {
                    counts[defId] = 0;
                }

                counts[defId]++;
            }

            string text = $"Placed: {placed.Count}\n";
            IReadOnlyList<BuildingDef> defs = buildingSystem.Definitions;
            for (int i = 0; i < defs.Count; i++)
            {
                BuildingDef def = defs[i];
                if (def == null)
                {
                    continue;
                }

                int count = counts.TryGetValue(def.id, out int value) ? value : 0;
                text += $"{def.displayName}: {count}\n";
            }

            if (buildingSystem.IsPlacementMode)
            {
                BuildingDef pending = buildingSystem.GetPendingDefinition();
                string pendingName = pending != null ? pending.displayName : buildingSystem.PendingPlacementDefId;
                text += $"\nPlacing: {pendingName}\nClick on base grid to place. Esc cancels.";
            }

            summaryText.text = text.TrimEnd('\n');
        }

        private void RebuildDynamicButtons()
        {
            ClearDynamicButtons();
            if (buildButtonRoot == null || buildButtonTemplate == null || buildingSystem == null)
            {
                return;
            }

            IReadOnlyList<BuildingDef> defs = buildingSystem.Definitions;
            for (int i = 0; i < defs.Count; i++)
            {
                BuildingDef def = defs[i];
                if (def == null)
                {
                    continue;
                }

                Button button = Instantiate(buildButtonTemplate, buildButtonRoot);
                button.gameObject.SetActive(true);
                string defId = def.id;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => BuildById(defId));

                Text buttonText = button.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    UiScrollLayoutHelper.ConfigureButtonLabel(buttonText);
                    buttonText.text = $"Place {def.displayName}";
                }

                spawnedButtons.Add(button);
            }
        }

        private void WireStaticButtons()
        {
            Wire(buildShelterButton, () => BuildById("shelter"));
            Wire(buildHeaterButton, () => BuildById("heater"));
            Wire(buildLumberMillButton, () => BuildById("lumber_mill"));
            Wire(buildKitchenButton, () => BuildById("kitchen"));
            Wire(buildStorageButton, () => BuildById("storage"));
            Wire(buildGeneratorButton, () => BuildById("generator"));

            Wire(upgradeShelterButton, () => UpgradeFirstById("shelter"));
            Wire(upgradeHeaterButton, () => UpgradeFirstById("heater"));
            Wire(upgradeLumberMillButton, () => UpgradeFirstById("lumber_mill"));
            Wire(upgradeKitchenButton, () => UpgradeFirstById("kitchen"));
            Wire(upgradeStorageButton, () => UpgradeFirstById("storage"));
            Wire(upgradeGeneratorButton, () => UpgradeFirstById("generator"));
            Wire(cancelPlacementButton, () => buildingSystem?.CancelPlacement());
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

        private void ClearDynamicButtons()
        {
            for (int i = 0; i < spawnedButtons.Count; i++)
            {
                if (spawnedButtons[i] != null)
                {
                    Destroy(spawnedButtons[i].gameObject);
                }
            }

            spawnedButtons.Clear();
        }

        private void Unbind()
        {
            if (buildingSystem != null)
            {
                buildingSystem.Changed -= Refresh;
                buildingSystem.PlacementModeChanged -= OnPlacementModeChanged;
            }

            ClearDynamicButtons();
        }

        private void OnPlacementModeChanged(BuildingDef _)
        {
            Refresh();
        }
    }

    internal static class UiScrollLayoutHelper
    {
        private const string ViewportName = "__Viewport";
        private const string ContentName = "__Content";

        public static void EnsureVerticalScroll(RectTransform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            ScrollRect existing = panelRoot.GetComponent<ScrollRect>();
            if (existing != null && existing.content != null && existing.viewport != null)
            {
                return;
            }

            List<Transform> childrenToMove = new List<Transform>();
            for (int i = 0; i < panelRoot.childCount; i++)
            {
                Transform child = panelRoot.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                childrenToMove.Add(child);
            }

            GameObject viewportGo = new GameObject(ViewportName, typeof(RectTransform), typeof(RectMask2D));
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.SetParent(panelRoot, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject contentGo = new GameObject(ContentName, typeof(RectTransform));
            RectTransform contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            for (int i = 0; i < childrenToMove.Count; i++)
            {
                Transform child = childrenToMove[i];
                if (child == null)
                {
                    continue;
                }

                if (child == viewportRect)
                {
                    continue;
                }

                child.SetParent(contentRect, false);
            }

            VerticalLayoutGroup sourceLayout = panelRoot.GetComponent<VerticalLayoutGroup>();
            if (sourceLayout != null)
            {
                VerticalLayoutGroup targetLayout = contentGo.AddComponent<VerticalLayoutGroup>();
                targetLayout.padding = sourceLayout.padding;
                targetLayout.spacing = sourceLayout.spacing;
                targetLayout.childAlignment = sourceLayout.childAlignment;
                targetLayout.childControlWidth = sourceLayout.childControlWidth;
                targetLayout.childControlHeight = sourceLayout.childControlHeight;
                targetLayout.childForceExpandWidth = sourceLayout.childForceExpandWidth;
                targetLayout.childForceExpandHeight = sourceLayout.childForceExpandHeight;
                targetLayout.childScaleWidth = sourceLayout.childScaleWidth;
                targetLayout.childScaleHeight = sourceLayout.childScaleHeight;
                targetLayout.reverseArrangement = sourceLayout.reverseArrangement;
                sourceLayout.enabled = false;
            }

            ContentSizeFitter sourceFitter = panelRoot.GetComponent<ContentSizeFitter>();
            if (sourceFitter != null)
            {
                ContentSizeFitter targetFitter = contentGo.AddComponent<ContentSizeFitter>();
                targetFitter.horizontalFit = sourceFitter.horizontalFit;
                targetFitter.verticalFit = sourceFitter.verticalFit;
                sourceFitter.enabled = false;
            }
            else
            {
                ContentSizeFitter targetFitter = contentGo.AddComponent<ContentSizeFitter>();
                targetFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                targetFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            ScrollRect scrollRect = existing != null ? existing : panelRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.inertia = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;
        }

        public static void ConfigureMultilineText(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
        }

        public static void ConfigureButtonLabel(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
        }

        public static void ConfigureSingleLineText(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
        }
    }
}
