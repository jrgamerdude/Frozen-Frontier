using FrozenFrontier.Data;
using FrozenFrontier.Systems;
using FrozenFrontier.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FrozenFrontier.Core
{
    public class GameManager : MonoBehaviour
    {
        public enum ViewState
        {
            Base = 0,
            BuildPanel = 1,
            SurvivorPanel = 2,
            MapPanel = 3
        }

        [Header("Systems")]
        [SerializeField] private TimeSystem timeSystem;
        [SerializeField] private ResourceSystem resourceSystem;
        [SerializeField] private BuildingSystem buildingSystem;
        [SerializeField] private SurvivorSystem survivorSystem;
        [SerializeField] private MapSystem mapSystem;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private SaveSystem saveSystem;

        [Header("UI References")]
        [SerializeField] private ResourceBarUI resourceBarUI;
        [SerializeField] private BuildMenuUI buildMenuUI;
        [SerializeField] private SurvivorPanelUI survivorPanelUI;
        [SerializeField] private MapUI mapUI;
        [SerializeField] private EventPopupUI eventPopupUI;
        [SerializeField] private ToastUI toastUI;

        [Header("Panels")]
        [SerializeField] private GameObject buildMenuPanel;
        [SerializeField] private GameObject survivorPanel;
        [SerializeField] private GameObject mapPanel;

        [Header("Buttons")]
        [SerializeField] private Button openBuildButton;
        [SerializeField] private Button openSurvivorButton;
        [SerializeField] private Button openMapButton;
        [SerializeField] private Button saveButton;

        [Header("Autosave")]
        [SerializeField, Min(5f)] private float autoSaveIntervalSeconds = 30f;

        private float autosaveTimer;
        private bool isBootstrapped;
        private ViewState currentState = ViewState.Base;

        public ViewState CurrentState => currentState;

        private void Awake()
        {
            if (timeSystem == null) timeSystem = GetComponent<TimeSystem>();
            if (resourceSystem == null) resourceSystem = GetComponent<ResourceSystem>();
            if (buildingSystem == null) buildingSystem = GetComponent<BuildingSystem>();
            if (survivorSystem == null) survivorSystem = GetComponent<SurvivorSystem>();
            if (mapSystem == null) mapSystem = GetComponent<MapSystem>();
            if (eventSystem == null) eventSystem = GetComponent<EventSystem>();
            if (saveSystem == null) saveSystem = GetComponent<SaveSystem>();
        }

        private void Start()
        {
            Bootstrap();
        }

        private void Update()
        {
            if (!isBootstrapped)
            {
                return;
            }

            autosaveTimer += Time.deltaTime;
            if (autosaveTimer >= autoSaveIntervalSeconds)
            {
                autosaveTimer = 0f;
                SaveNow(false);
            }
        }

        private void OnDestroy()
        {
            if (timeSystem != null)
            {
                timeSystem.TickRaised -= OnTickRaised;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && isBootstrapped)
            {
                SaveNow(false);
            }
        }

        private void OnApplicationQuit()
        {
            if (isBootstrapped)
            {
                SaveNow(false);
            }
        }

        public void Bootstrap()
        {
            if (isBootstrapped)
            {
                return;
            }

            if (!ValidateCoreReferences())
            {
                return;
            }

            resourceSystem.InitializeDefaults();
            survivorSystem.Initialize(resourceSystem);
            buildingSystem.Initialize(resourceSystem, survivorSystem);
            mapSystem.Initialize(resourceSystem, survivorSystem);
            eventSystem.Initialize(resourceSystem, survivorSystem);

            buildingSystem.ToastRequested += ShowToast;
            mapSystem.ToastRequested += ShowToast;
            eventSystem.ToastRequested += ShowToast;

            TryLoadAndApplySave();
            WireButtons();
            BindUi();
            SetViewState(ViewState.Base);

            timeSystem.TickRaised += OnTickRaised;
            isBootstrapped = true;
            ShowToast("Settlement initialized.");
        }

        public void OpenBuildMenu()
        {
            SetViewState(ViewState.BuildPanel);
        }

        public void OpenSurvivorPanel()
        {
            SetViewState(ViewState.SurvivorPanel);
        }

        public void OpenMapPanel()
        {
            SetViewState(ViewState.MapPanel);
        }

        public void OpenBaseView()
        {
            SetViewState(ViewState.Base);
        }

        public void SaveNow(bool showToast = true)
        {
            if (!isBootstrapped && !ValidateCoreReferences())
            {
                return;
            }

            GameSaveData saveData = new GameSaveData
            {
                lastSaveUtc = timeSystem.GetUtcNowIso(),
                resourceState = resourceSystem.ExportState(),
                survivorState = survivorSystem.ExportState(),
                buildingState = buildingSystem.ExportState(),
                mapState = mapSystem.ExportState(),
                eventState = eventSystem.ExportState(),
                timeState = new TimeSystemSaveData { totalTicks = timeSystem.TotalTicks }
            };

            saveSystem.Save(saveData);
            if (showToast)
            {
                ShowToast("Game saved.");
            }
        }

        private void OnTickRaised(int totalTicks)
        {
            RunTick(false);
        }

        private void RunTick(bool offlineMode)
        {
            if (resourceSystem == null || survivorSystem == null)
            {
                return;
            }

            resourceSystem.BeginBatchChanges();
            survivorSystem.BeginBatchChanges();
            try
            {
                // Natural heat decay forces the player to keep fuel + heaters running.
                resourceSystem.ModifyHeat(-1);
                buildingSystem.Tick(offlineMode);
                survivorSystem.Tick(offlineMode);
                mapSystem.Tick(offlineMode);
                eventSystem.Tick(offlineMode);
            }
            finally
            {
                survivorSystem.EndBatchChanges();
                resourceSystem.EndBatchChanges();
            }
        }

        private void TryLoadAndApplySave()
        {
            GameSaveData loaded = saveSystem.Load();
            if (loaded == null)
            {
                return;
            }

            resourceSystem.ImportState(loaded.resourceState);
            survivorSystem.ImportState(loaded.survivorState);
            buildingSystem.ImportState(loaded.buildingState);
            mapSystem.ImportState(loaded.mapState);
            eventSystem.ImportState(loaded.eventState);
            timeSystem.SetTotalTicks(loaded.timeState != null ? loaded.timeState.totalTicks : 0);

            int offlineTicks = timeSystem.ComputeOfflineTicks(loaded.lastSaveUtc);
            for (int i = 0; i < offlineTicks; i++)
            {
                RunTick(true);
            }

            if (offlineTicks > 0)
            {
                ShowToast($"Offline progress applied: {offlineTicks} ticks.");
            }
        }

        private void BindUi()
        {
            if (resourceBarUI != null)
            {
                resourceBarUI.Bind(resourceSystem, survivorSystem, timeSystem, this);
            }

            if (buildMenuUI != null)
            {
                buildMenuUI.Bind(buildingSystem, this);
            }

            if (survivorPanelUI != null)
            {
                survivorPanelUI.Bind(survivorSystem);
            }

            if (mapUI != null)
            {
                mapUI.Bind(mapSystem);
            }

            if (eventPopupUI != null)
            {
                eventPopupUI.Bind(eventSystem);
            }

            if (toastUI != null)
            {
                // Ready for external toast messages.
            }
        }

        private void WireButtons()
        {
            if (openBuildButton != null)
            {
                openBuildButton.onClick.RemoveAllListeners();
                openBuildButton.onClick.AddListener(OpenBuildMenu);
            }

            if (openSurvivorButton != null)
            {
                openSurvivorButton.onClick.RemoveAllListeners();
                openSurvivorButton.onClick.AddListener(OpenSurvivorPanel);
            }

            if (openMapButton != null)
            {
                openMapButton.onClick.RemoveAllListeners();
                openMapButton.onClick.AddListener(OpenMapPanel);
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(() => SaveNow(true));
            }
        }

        private void SetViewState(ViewState state)
        {
            currentState = state;
            if (buildMenuPanel != null)
            {
                buildMenuPanel.SetActive(state == ViewState.BuildPanel);
            }

            if (survivorPanel != null)
            {
                survivorPanel.SetActive(state == ViewState.SurvivorPanel);
            }

            if (mapPanel != null)
            {
                mapPanel.SetActive(state == ViewState.MapPanel);
            }
        }

        private bool ValidateCoreReferences()
        {
            if (timeSystem == null || resourceSystem == null || buildingSystem == null ||
                survivorSystem == null || mapSystem == null || eventSystem == null || saveSystem == null)
            {
                Debug.LogError("GameManager is missing one or more required system references.");
                return false;
            }

            return true;
        }

        private void ShowToast(string message)
        {
            if (toastUI != null)
            {
                toastUI.Show(message);
            }
        }
    }
}
