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
}
