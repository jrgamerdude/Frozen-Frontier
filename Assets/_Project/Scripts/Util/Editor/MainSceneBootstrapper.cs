#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FrozenFrontier.Core;
using FrozenFrontier.Systems;
using FrozenFrontier.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EventSystemRuntime = FrozenFrontier.Systems.EventSystem;

namespace FrozenFrontier.Util.Editor
{
    public static class MainSceneBootstrapper
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private static readonly Color UiTextPrimary = new Color(0.9f, 0.95f, 1f, 1f);
        private static readonly Color UiTextSecondary = new Color(0.74f, 0.84f, 0.93f, 1f);
        private static readonly Color UiTextAccent = new Color(0.54f, 0.86f, 1f, 1f);
        private static readonly Color UiPanelOutline = new Color(0.48f, 0.74f, 0.92f, 0.32f);
        private static readonly Color UiButtonNormal = new Color(0.16f, 0.29f, 0.39f, 0.97f);
        private static readonly Color UiButtonHighlight = new Color(0.24f, 0.41f, 0.53f, 1f);
        private static readonly Color UiButtonPressed = new Color(0.1f, 0.2f, 0.29f, 1f);
        private static readonly Color UiButtonAccent = new Color(0.61f, 0.9f, 1f, 0.9f);

        [MenuItem("Tools/Frozen Frontier/Generate Main Scene (One Click)")]
        public static void GenerateMainScene()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Frozen Frontier Scene Generator",
                "This will create a fresh Main scene and overwrite Assets/_Project/Scenes/Main.unity. Continue?",
                "Generate",
                "Cancel");
            if (!confirmed)
            {
                return;
            }

            FrozenFrontierDataRefs dataRefs = FrozenFrontierDataGenerator.EnsureDataAssets(false);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Font font = GetDefaultFont();

            // Core world objects.
            Camera worldCamera = CreateCamera();
            TryCreateUrp2DLights();
            CreateEventSystem();

            GameObject baseArea = new GameObject("BaseArea");
            GameObject mapArea = new GameObject("MapArea");
            mapArea.transform.position = new Vector3(28f, 0f, 0f);
            Selection.activeObject = baseArea;

            // Game systems.
            GameObject gameRoot = new GameObject("GameRoot");
            GameManager gameManager = gameRoot.AddComponent<GameManager>();
            TimeSystem timeSystem = gameRoot.AddComponent<TimeSystem>();
            ResourceSystem resourceSystem = gameRoot.AddComponent<ResourceSystem>();
            BuildingSystem buildingSystem = gameRoot.AddComponent<BuildingSystem>();
            SurvivorSystem survivorSystem = gameRoot.AddComponent<SurvivorSystem>();
            MapSystem mapSystem = gameRoot.AddComponent<MapSystem>();
            EventSystemRuntime eventSystem = gameRoot.AddComponent<EventSystemRuntime>();
            SaveSystem saveSystem = gameRoot.AddComponent<SaveSystem>();
            BuildingWorldView buildingWorldView = baseArea.AddComponent<BuildingWorldView>();
            BaseGridInput baseGridInput = baseArea.AddComponent<BaseGridInput>();
            MapWorldView mapWorldView = mapArea.AddComponent<MapWorldView>();
            MapWorldInput mapWorldInput = mapArea.AddComponent<MapWorldInput>();
            WorldCameraController worldCameraController = worldCamera.gameObject.AddComponent<WorldCameraController>();

            WireBuildingWorldRefs(buildingSystem, baseArea.transform);
            WireSystemDataAssets(buildingSystem, mapSystem, eventSystem, dataRefs);
            WireBaseGridInput(baseGridInput, buildingSystem, worldCamera);
            WireBuildingWorldView(buildingWorldView, buildingSystem);
            WireMapWorldView(mapWorldView, mapSystem, mapArea.transform, new Vector2(-24f, -24f), 0.75f);
            WireMapWorldInput(mapWorldInput, mapSystem, mapWorldView, worldCamera);
            WireWorldCameraController(worldCameraController, worldCamera, buildingSystem, mapWorldView, baseArea.transform, mapArea.transform, 5f, 28f);

            // Canvas and core UI containers.
            Canvas canvas = CreateCanvas();
            CreateAtmosphereBackdrop(canvas.transform);

            GameObject resourceBar = CreateTopPanel("ResourceBar", canvas.transform, 95f, new Color(0.05f, 0.1f, 0.16f, 0.94f), true);
            GameObject leftMenu = CreateLeftPanel("LeftMenu", canvas.transform, 250f, 95f, 0f, new Color(0.03f, 0.07f, 0.12f, 0.93f), true);

            GameObject buildPanel = CreateContentPanel("BuildMenuPanel", canvas.transform, 260f, 105f, 20f, 20f, new Color(0.06f, 0.11f, 0.17f, 0.94f), false);
            GameObject survivorPanel = CreateContentPanel("SurvivorPanel", canvas.transform, 260f, 105f, 20f, 20f, new Color(0.06f, 0.11f, 0.17f, 0.94f), false);
            GameObject mapPanel = CreateContentPanel("MapPanel", canvas.transform, 260f, 105f, 20f, 20f, new Color(0.06f, 0.11f, 0.17f, 0.94f), false);

            GameObject eventPopup = CreateCenteredPanel("EventPopup", canvas.transform, new Vector2(740f, 460f), new Color(0.08f, 0.13f, 0.2f, 0.97f), true);
            GameObject toasts = CreateBottomCenterPanel("Toasts", canvas.transform, new Vector2(900f, 74f), new Vector2(0f, 24f), new Color(0.03f, 0.08f, 0.14f, 0.82f), true);

            // Resource bar UI.
            HorizontalLayoutGroup resourceLayout = resourceBar.AddComponent<HorizontalLayoutGroup>();
            resourceLayout.childControlWidth = false;
            resourceLayout.childControlHeight = true;
            resourceLayout.childForceExpandWidth = false;
            resourceLayout.childForceExpandHeight = true;
            resourceLayout.spacing = 8f;
            resourceLayout.padding = new RectOffset(14, 14, 10, 10);

            Text woodText = CreateValueText(resourceBar.transform, "WoodText", "Wood 0/0", font, 150);
            Text foodText = CreateValueText(resourceBar.transform, "FoodText", "Food 0/0", font, 150);
            Text fuelText = CreateValueText(resourceBar.transform, "FuelText", "Fuel 0/0", font, 150);
            Text scrapText = CreateValueText(resourceBar.transform, "ScrapText", "Scrap 0/0", font, 150);
            Text heatText = CreateValueText(resourceBar.transform, "HeatText", "Heat 0", font, 120);
            Text powerText = CreateValueText(resourceBar.transform, "PowerText", "Power 0", font, 120);
            Text survivorText = CreateValueText(resourceBar.transform, "SurvivorText", "Survivors 0/0", font, 170);
            Text moraleText = CreateValueText(resourceBar.transform, "MoraleText", "Morale 0%", font, 130);
            Text tickText = CreateValueText(resourceBar.transform, "TickText", "Tick 0", font, 110);
            Button topSaveButton = CreateButton(resourceBar.transform, "TopSaveButton", "Save", font, 110, 44);

            // Left menu UI.
            VerticalLayoutGroup leftLayout = leftMenu.AddComponent<VerticalLayoutGroup>();
            leftLayout.spacing = 12f;
            leftLayout.childControlHeight = false;
            leftLayout.childControlWidth = true;
            leftLayout.childForceExpandHeight = false;
            leftLayout.childForceExpandWidth = true;
            leftLayout.padding = new RectOffset(20, 20, 22, 22);

            CreateHeader(leftMenu.transform, "MenuHeader", "Frozen Frontier", font, 24);
            Button buildButton = CreateButton(leftMenu.transform, "BuildButton", "Build", font, 180, 54);
            Button survivorsButton = CreateButton(leftMenu.transform, "SurvivorsButton", "Survivors", font, 180, 54);
            Button mapButton = CreateButton(leftMenu.transform, "MapButton", "Map", font, 180, 54);
            Button saveButton = CreateButton(leftMenu.transform, "SaveButton", "Save", font, 180, 54);

            // Build panel UI.
            BuildMenuUI buildMenuUI = buildPanel.AddComponent<BuildMenuUI>();
            VerticalLayoutGroup buildLayout = buildPanel.AddComponent<VerticalLayoutGroup>();
            buildLayout.padding = new RectOffset(16, 16, 16, 16);
            buildLayout.spacing = 8f;
            buildLayout.childControlHeight = false;
            buildLayout.childControlWidth = true;
            buildLayout.childForceExpandHeight = false;
            buildLayout.childForceExpandWidth = true;

            CreateHeader(buildPanel.transform, "BuildHeader", "Build & Upgrade", font, 28);
            Button buildShelter = CreateButton(buildPanel.transform, "BuildShelterButton", "Build Shelter (HQ)", font, 0, 44);
            Button buildHeater = CreateButton(buildPanel.transform, "BuildHeaterButton", "Build Heater", font, 0, 44);
            Button buildLumber = CreateButton(buildPanel.transform, "BuildLumberMillButton", "Build Lumber Mill", font, 0, 44);
            Button buildKitchen = CreateButton(buildPanel.transform, "BuildKitchenButton", "Build Kitchen", font, 0, 44);
            Button buildStorage = CreateButton(buildPanel.transform, "BuildStorageButton", "Build Storage", font, 0, 44);
            Button buildGenerator = CreateButton(buildPanel.transform, "BuildGeneratorButton", "Build Generator", font, 0, 44);

            CreateHeader(buildPanel.transform, "UpgradeHeader", "Upgrades", font, 22);
            Button upgradeShelter = CreateButton(buildPanel.transform, "UpgradeShelterButton", "Upgrade Shelter", font, 0, 40);
            Button upgradeHeater = CreateButton(buildPanel.transform, "UpgradeHeaterButton", "Upgrade Heater", font, 0, 40);
            Button upgradeLumber = CreateButton(buildPanel.transform, "UpgradeLumberMillButton", "Upgrade Lumber Mill", font, 0, 40);
            Button upgradeKitchen = CreateButton(buildPanel.transform, "UpgradeKitchenButton", "Upgrade Kitchen", font, 0, 40);
            Button upgradeStorage = CreateButton(buildPanel.transform, "UpgradeStorageButton", "Upgrade Storage", font, 0, 40);
            Button upgradeGenerator = CreateButton(buildPanel.transform, "UpgradeGeneratorButton", "Upgrade Generator", font, 0, 40);
            Button cancelPlacement = CreateButton(buildPanel.transform, "CancelPlacementButton", "Cancel Placement (Esc)", font, 0, 40);
            Text buildSummary = CreateMultilineText(buildPanel.transform, "SummaryText", "Placed: 0", font, 18, 220);

            // Survivor panel UI.
            SurvivorPanelUI survivorPanelUI = survivorPanel.AddComponent<SurvivorPanelUI>();
            VerticalLayoutGroup survivorLayout = survivorPanel.AddComponent<VerticalLayoutGroup>();
            survivorLayout.padding = new RectOffset(16, 16, 16, 16);
            survivorLayout.spacing = 8f;
            survivorLayout.childControlHeight = false;
            survivorLayout.childControlWidth = true;
            survivorLayout.childForceExpandHeight = false;
            survivorLayout.childForceExpandWidth = true;

            CreateHeader(survivorPanel.transform, "SurvivorHeader", "Survivor Management", font, 28);
            Button assignLumber = CreateButton(survivorPanel.transform, "AssignLumberjackButton", "Assign Lumberjack", font, 0, 40);
            Button assignCook = CreateButton(survivorPanel.transform, "AssignCookButton", "Assign Cook", font, 0, 40);
            Button assignBuilder = CreateButton(survivorPanel.transform, "AssignBuilderButton", "Assign Builder", font, 0, 40);
            Button assignExplorer = CreateButton(survivorPanel.transform, "AssignExplorerButton", "Assign Explorer", font, 0, 40);
            Button assignMedic = CreateButton(survivorPanel.transform, "AssignMedicButton", "Assign Medic", font, 0, 40);
            Button assignCollector = CreateButton(survivorPanel.transform, "AssignCollectorButton", "Assign Collector", font, 0, 40);
            Button resetJobs = CreateButton(survivorPanel.transform, "ResetJobsButton", "Reset All Jobs", font, 0, 40);
            Text survivorSummary = CreateMultilineText(survivorPanel.transform, "SummaryText", "Population 0/0", font, 18, 90);
            Text survivorList = CreateMultilineText(survivorPanel.transform, "ListText", "No survivors.", font, 16, 260);

            // Map panel UI.
            MapUI mapUI = mapPanel.AddComponent<MapUI>();
            VerticalLayoutGroup mapLayout = mapPanel.AddComponent<VerticalLayoutGroup>();
            mapLayout.padding = new RectOffset(16, 16, 16, 16);
            mapLayout.spacing = 8f;
            mapLayout.childControlHeight = false;
            mapLayout.childControlWidth = true;
            mapLayout.childForceExpandHeight = false;
            mapLayout.childForceExpandWidth = true;

            CreateHeader(mapPanel.transform, "MapHeader", "Map Exploration", font, 28);
            Text mapSummary = CreateMultilineText(mapPanel.transform, "SummaryText", "Map 64x64", font, 18, 60);
            Button unlockFirst = CreateButton(mapPanel.transform, "UnlockFirstButton", "Unlock First Adjacent Tile", font, 0, 42);
            Button exploreFirst = CreateButton(mapPanel.transform, "ExploreFirstButton", "Explore First Unlocked Tile", font, 0, 42);

            GameObject tileRoot = new GameObject("TileRoot", typeof(RectTransform), typeof(GridLayoutGroup));
            tileRoot.transform.SetParent(mapPanel.transform, false);
            GridLayoutGroup grid = tileRoot.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(118f, 64f);
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            LayoutElement tileRootLayout = tileRoot.AddComponent<LayoutElement>();
            tileRootLayout.preferredHeight = 380f;

            Button tileTemplate = CreateButton(tileRoot.transform, "TileButtonTemplate", "0,0", font, 118, 64);
            tileTemplate.gameObject.SetActive(false);

            // Event popup UI.
            EventPopupUI eventPopupUI = eventPopup.AddComponent<EventPopupUI>();
            VerticalLayoutGroup popupLayout = eventPopup.AddComponent<VerticalLayoutGroup>();
            popupLayout.padding = new RectOffset(16, 16, 16, 16);
            popupLayout.spacing = 10f;
            popupLayout.childControlHeight = false;
            popupLayout.childControlWidth = true;
            popupLayout.childForceExpandHeight = false;
            popupLayout.childForceExpandWidth = true;

            Text eventTitle = CreateHeader(eventPopup.transform, "TitleText", "Event", font, 30);
            Text eventDescription = CreateMultilineText(eventPopup.transform, "DescriptionText", "Event description.", font, 18, 160);
            Button choice1 = CreateButton(eventPopup.transform, "ChoiceButton_1", "Choice 1", font, 0, 48);
            Button choice2 = CreateButton(eventPopup.transform, "ChoiceButton_2", "Choice 2", font, 0, 48);
            Button choice3 = CreateButton(eventPopup.transform, "ChoiceButton_3", "Choice 3", font, 0, 48);

            // Toast UI.
            ToastUI toastUI = toasts.AddComponent<ToastUI>();
            CanvasGroup toastCanvasGroup = toasts.AddComponent<CanvasGroup>();
            Text toastText = CreateCenteredFillText(toasts.transform, "ToastText", "Ready.", font, 22);

            // Attach remaining UI components.
            ResourceBarUI resourceBarUI = resourceBar.AddComponent<ResourceBarUI>();

            // Serialized field wiring.
            WireResourceBarUi(resourceBarUI, woodText, foodText, fuelText, scrapText, heatText, powerText, survivorText, moraleText, tickText, topSaveButton);
            WireBuildMenuUi(buildMenuUI, buildShelter, buildHeater, buildLumber, buildKitchen, buildStorage, buildGenerator,
                upgradeShelter, upgradeHeater, upgradeLumber, upgradeKitchen, upgradeStorage, upgradeGenerator, cancelPlacement, buildSummary);
            WireSurvivorPanelUi(survivorPanelUI, assignLumber, assignCook, assignBuilder, assignExplorer, assignMedic, assignCollector, resetJobs, survivorSummary, survivorList);
            WireMapUi(mapUI, tileRoot.transform, tileTemplate, mapSummary, unlockFirst, exploreFirst);
            WireEventPopupUi(eventPopupUI, eventPopup, eventTitle, eventDescription, choice1, choice2, choice3);
            WireToastUi(toastUI, toastText, toastCanvasGroup);

            WireGameManager(
                gameManager,
                timeSystem,
                resourceSystem,
                buildingSystem,
                survivorSystem,
                mapSystem,
                eventSystem,
                saveSystem,
                resourceBarUI,
                buildMenuUI,
                survivorPanelUI,
                mapUI,
                eventPopupUI,
                toastUI,
                buildPanel,
                survivorPanel,
                mapPanel,
                buildButton,
                survivorsButton,
                mapButton,
                saveButton);

            // Initial panel states.
            buildPanel.SetActive(false);
            survivorPanel.SetActive(false);
            mapPanel.SetActive(false);
            eventPopup.SetActive(false);

            Selection.activeObject = gameRoot;
            EditorSceneManager.MarkSceneDirty(scene);
            SaveSceneToMainPath(scene);
            EditorUtility.DisplayDialog("Frozen Frontier", $"Main scene generated and saved to:\n{MainScenePath}", "OK");
        }

        private static Camera CreateCamera()
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.07f, 0.12f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();

            Type inputSystemUiType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemUiType != null)
            {
                eventSystemGo.AddComponent(inputSystemUiType);
            }
            else
            {
                eventSystemGo.AddComponent<StandaloneInputModule>();
            }
        }

        private static void TryCreateUrp2DLights()
        {
            Type light2DType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (light2DType == null)
            {
                return;
            }

            GameObject globalGo = new GameObject("Global Light 2D");
            Component globalLight = globalGo.AddComponent(light2DType);
            SetLightType(globalLight, "Global");
            SetFloatProperty(globalLight, "intensity", 1f);

            GameObject pointGo = new GameObject("Point Light 2D");
            pointGo.transform.position = new Vector3(0f, 1.5f, 0f);
            Component pointLight = pointGo.AddComponent(light2DType);
            SetLightType(pointLight, "Point");
            SetFloatProperty(pointLight, "intensity", 0.8f);
            SetFloatProperty(pointLight, "pointLightOuterRadius", 5.5f);
            SetFloatProperty(pointLight, "pointLightInnerRadius", 2.5f);
        }

        private static void SetLightType(Component component, string enumName)
        {
            if (component == null)
            {
                return;
            }

            PropertyInfo prop = component.GetType().GetProperty("lightType", BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.PropertyType.IsEnum)
            {
                return;
            }

            try
            {
                object value = Enum.Parse(prop.PropertyType, enumName);
                prop.SetValue(component, value, null);
            }
            catch
            {
                // Best effort only.
            }
        }

        private static void SetFloatProperty(Component component, string propertyName, float value)
        {
            if (component == null)
            {
                return;
            }

            PropertyInfo prop = component.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || prop.PropertyType != typeof(float))
            {
                return;
            }

            try
            {
                prop.SetValue(component, value, null);
            }
            catch
            {
                // Best effort only.
            }
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void CreateAtmosphereBackdrop(Transform canvasRoot)
        {
            GameObject backdrop = new GameObject("UIBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(canvasRoot, false);
            RectTransform bgRt = backdrop.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImage = backdrop.GetComponent<Image>();
            bgImage.color = new Color(0.02f, 0.06f, 0.1f, 0.22f);
            bgImage.raycastTarget = false;
            backdrop.transform.SetAsFirstSibling();

            CreateOverlayBand(backdrop.transform, "BackdropTopGlow", new Vector2(0f, 0.58f), new Vector2(1f, 1f), new Color(0.2f, 0.5f, 0.75f, 0.09f));
            CreateOverlayBand(backdrop.transform, "BackdropBottomFog", new Vector2(0f, 0f), new Vector2(1f, 0.42f), new Color(0.01f, 0.04f, 0.08f, 0.16f));
        }

        private static void CreateOverlayBand(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject band = new GameObject(name, typeof(RectTransform), typeof(Image));
            band.transform.SetParent(parent, false);
            RectTransform rt = band.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image image = band.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static GameObject CreateTopPanel(string name, Transform parent, float height, Color bgColor, bool emphasized)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -height);
            rt.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = bgColor;
            StylePanelGraphic(image, emphasized);
            return panel;
        }

        private static GameObject CreateLeftPanel(string name, Transform parent, float width, float topInset, float bottomInset, Color bgColor, bool emphasized)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(0f, bottomInset);
            rt.offsetMax = new Vector2(width, -topInset);

            Image image = panel.GetComponent<Image>();
            image.color = bgColor;
            StylePanelGraphic(image, emphasized);
            return panel;
        }

        private static GameObject CreateContentPanel(string name, Transform parent, float leftInset, float topInset, float rightInset, float bottomInset, Color bgColor, bool emphasized)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(leftInset, bottomInset);
            rt.offsetMax = new Vector2(-rightInset, -topInset);

            Image image = panel.GetComponent<Image>();
            image.color = bgColor;
            StylePanelGraphic(image, emphasized);
            return panel;
        }

        private static GameObject CreateCenteredPanel(string name, Transform parent, Vector2 size, Color bgColor, bool emphasized)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = bgColor;
            StylePanelGraphic(image, emphasized);
            return panel;
        }

        private static GameObject CreateBottomCenterPanel(string name, Transform parent, Vector2 size, Vector2 offset, Color bgColor, bool emphasized)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;

            Image image = panel.GetComponent<Image>();
            image.color = bgColor;
            StylePanelGraphic(image, emphasized);
            return panel;
        }

        private static void StylePanelGraphic(Image image, bool emphasized)
        {
            if (image == null)
            {
                return;
            }

            Outline outline = image.gameObject.GetComponent<Outline>() ?? image.gameObject.AddComponent<Outline>();
            outline.effectColor = emphasized ? new Color(UiPanelOutline.r, UiPanelOutline.g, UiPanelOutline.b, 0.46f) : UiPanelOutline;
            outline.effectDistance = emphasized ? new Vector2(1.5f, -1.5f) : new Vector2(1f, -1f);

            Shadow shadow = image.gameObject.GetComponent<Shadow>() ?? image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.02f, 0.05f, emphasized ? 0.6f : 0.45f);
            shadow.effectDistance = emphasized ? new Vector2(0f, -4f) : new Vector2(0f, -2f);
        }

        private static Text CreateHeader(Transform parent, string name, string value, Font font, int fontSize)
        {
            Text text = CreateText(parent, name, value, font, fontSize, TextAnchor.MiddleLeft);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = Mathf.Max(40f, fontSize + 12f);
            text.fontStyle = FontStyle.Bold;
            text.color = UiTextAccent;
            Shadow shadow = text.gameObject.GetComponent<Shadow>() ?? text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.08f, 0.14f, 0.7f);
            shadow.effectDistance = new Vector2(0f, -1.4f);
            return text;
        }

        private static Text CreateValueText(Transform parent, string name, string value, Font font, float preferredWidth)
        {
            Text text = CreateText(parent, name, value, font, 20, TextAnchor.MiddleLeft);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            text.fontStyle = FontStyle.Bold;
            text.color = UiTextPrimary;
            return text;
        }

        private static Text CreateMultilineText(Transform parent, string name, string value, Font font, int fontSize, float preferredHeight)
        {
            Text text = CreateText(parent, name, value, font, fontSize, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            text.color = UiTextSecondary;
            return text;
        }

        private static Text CreateCenteredFillText(Transform parent, string name, string value, Font font, int fontSize)
        {
            Text text = CreateText(parent, name, value, font, fontSize, TextAnchor.MiddleCenter);
            RectTransform rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 4f);
            rt.offsetMax = new Vector2(-8f, -4f);
            return text;
        }

        private static Text CreateText(Transform parent, string name, string value, Font font, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = UiTextPrimary;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, float preferredWidth, float preferredHeight)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = UiButtonNormal;
            Outline outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.76f, 0.95f, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);
            Shadow buttonShadow = go.GetComponent<Shadow>() ?? go.AddComponent<Shadow>();
            buttonShadow.effectColor = new Color(0f, 0.03f, 0.08f, 0.5f);
            buttonShadow.effectDistance = new Vector2(0f, -2f);

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = UiButtonNormal;
            colors.highlightedColor = UiButtonHighlight;
            colors.pressedColor = UiButtonPressed;
            colors.selectedColor = UiButtonHighlight;
            colors.disabledColor = new Color(0.12f, 0.19f, 0.24f, 0.65f);
            button.colors = colors;

            LayoutElement layout = go.AddComponent<LayoutElement>();
            if (preferredWidth > 0f)
            {
                layout.preferredWidth = preferredWidth;
            }

            if (preferredHeight > 0f)
            {
                layout.preferredHeight = preferredHeight;
            }

            GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(go.transform, false);
            RectTransform accentRt = accent.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.pivot = new Vector2(0.5f, 1f);
            accentRt.sizeDelta = new Vector2(0f, 4f);
            accentRt.anchoredPosition = Vector2.zero;
            Image accentImage = accent.GetComponent<Image>();
            accentImage.color = UiButtonAccent;
            accentImage.raycastTarget = false;

            Text labelText = CreateText(go.transform, "Text", label, font, 18, TextAnchor.MiddleCenter);
            RectTransform textRt = labelText.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 4f);
            textRt.offsetMax = new Vector2(-8f, -4f);
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = UiTextPrimary;
            Shadow textShadow = labelText.gameObject.GetComponent<Shadow>() ?? labelText.gameObject.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0.03f, 0.08f, 0.7f);
            textShadow.effectDistance = new Vector2(0f, -1f);

            return button;
        }

        private static Font GetDefaultFont()
        {
            Font font = TryGetBuiltinFont("LegacyRuntime.ttf");
            if (font == null)
            {
                font = TryGetBuiltinFont("Fonts/LegacyRuntime.ttf");
            }

            if (font == null)
            {
                font = EditorStyles.label.font;
            }

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Bahnschrift", "Trebuchet MS", "Segoe UI", "Tahoma", "Arial", "Sans Serif" },
                    16);
            }

            return font;
        }

        private static void WireBuildingWorldRefs(BuildingSystem buildingSystem, Transform baseAreaRoot)
        {
            SerializedObject so = new SerializedObject(buildingSystem);
            SetObjectRef(so, "baseAreaRoot", baseAreaRoot);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(buildingSystem);
        }

        private static void WireSystemDataAssets(
            BuildingSystem buildingSystem,
            MapSystem mapSystem,
            EventSystemRuntime eventSystem,
            FrozenFrontierDataRefs dataRefs)
        {
            if (dataRefs == null)
            {
                return;
            }

            SerializedObject buildingSo = new SerializedObject(buildingSystem);
            SetObjectList(buildingSo, "buildingDefinitions", dataRefs.buildingDefs);
            buildingSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(buildingSystem);

            SerializedObject mapSo = new SerializedObject(mapSystem);
            SetObjectList(mapSo, "biomeDefinitions", dataRefs.biomeDefs);
            mapSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mapSystem);

            SerializedObject eventSo = new SerializedObject(eventSystem);
            SetObjectList(eventSo, "eventDefinitions", dataRefs.eventDefs);
            eventSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(eventSystem);
        }

        private static void WireBaseGridInput(BaseGridInput input, BuildingSystem buildingSystem, Camera worldCamera)
        {
            SerializedObject so = new SerializedObject(input);
            SetObjectRef(so, "buildingSystem", buildingSystem);
            SetObjectRef(so, "worldCamera", worldCamera);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(input);
        }

        private static void WireBuildingWorldView(BuildingWorldView view, BuildingSystem buildingSystem)
        {
            SerializedObject so = new SerializedObject(view);
            SetObjectRef(so, "buildingSystem", buildingSystem);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static void WireMapWorldView(MapWorldView view, MapSystem mapSystem, Transform mapRoot, Vector2 mapOrigin, float cellWorldSize)
        {
            SerializedObject so = new SerializedObject(view);
            SetObjectRef(so, "mapSystem", mapSystem);
            SetObjectRef(so, "mapRoot", mapRoot);
            SetVector2(so, "mapOrigin", mapOrigin);
            SetFloat(so, "cellWorldSize", cellWorldSize);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static void WireMapWorldInput(MapWorldInput input, MapSystem mapSystem, MapWorldView mapWorldView, Camera worldCamera)
        {
            SerializedObject so = new SerializedObject(input);
            SetObjectRef(so, "mapSystem", mapSystem);
            SetObjectRef(so, "mapWorldView", mapWorldView);
            SetObjectRef(so, "worldCamera", worldCamera);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(input);
        }

        private static void WireWorldCameraController(
            WorldCameraController controller,
            Camera worldCamera,
            BuildingSystem buildingSystem,
            MapWorldView mapWorldView,
            Transform baseFocusTarget,
            Transform mapFocusTarget,
            float minZoom,
            float maxZoom)
        {
            SerializedObject so = new SerializedObject(controller);
            SetObjectRef(so, "controlledCamera", worldCamera);
            SetObjectRef(so, "buildingSystem", buildingSystem);
            SetObjectRef(so, "mapWorldView", mapWorldView);
            SetObjectRef(so, "baseFocusTarget", baseFocusTarget);
            SetObjectRef(so, "mapFocusTarget", mapFocusTarget);
            SetFloat(so, "minZoom", minZoom);
            SetFloat(so, "maxZoom", maxZoom);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static Font TryGetBuiltinFont(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                Font font = Resources.GetBuiltinResource<Font>(path);
                if (font != null)
                {
                    return font;
                }
            }
            catch
            {
                // Ignore and continue fallback search.
            }

            try
            {
                Font font = AssetDatabase.GetBuiltinExtraResource<Font>(path);
                if (font != null)
                {
                    return font;
                }
            }
            catch
            {
                // Ignore and continue fallback search.
            }

            return null;
        }

        private static void WireGameManager(
            GameManager gameManager,
            TimeSystem timeSystem,
            ResourceSystem resourceSystem,
            BuildingSystem buildingSystem,
            SurvivorSystem survivorSystem,
            MapSystem mapSystem,
            EventSystemRuntime eventSystem,
            SaveSystem saveSystem,
            ResourceBarUI resourceBarUI,
            BuildMenuUI buildMenuUI,
            SurvivorPanelUI survivorPanelUI,
            MapUI mapUI,
            EventPopupUI eventPopupUI,
            ToastUI toastUI,
            GameObject buildPanel,
            GameObject survivorPanel,
            GameObject mapPanel,
            Button buildButton,
            Button survivorButton,
            Button mapButton,
            Button saveButton)
        {
            SerializedObject so = new SerializedObject(gameManager);
            SetObjectRef(so, "timeSystem", timeSystem);
            SetObjectRef(so, "resourceSystem", resourceSystem);
            SetObjectRef(so, "buildingSystem", buildingSystem);
            SetObjectRef(so, "survivorSystem", survivorSystem);
            SetObjectRef(so, "mapSystem", mapSystem);
            SetObjectRef(so, "eventSystem", eventSystem);
            SetObjectRef(so, "saveSystem", saveSystem);

            SetObjectRef(so, "resourceBarUI", resourceBarUI);
            SetObjectRef(so, "buildMenuUI", buildMenuUI);
            SetObjectRef(so, "survivorPanelUI", survivorPanelUI);
            SetObjectRef(so, "mapUI", mapUI);
            SetObjectRef(so, "eventPopupUI", eventPopupUI);
            SetObjectRef(so, "toastUI", toastUI);

            SetObjectRef(so, "buildMenuPanel", buildPanel);
            SetObjectRef(so, "survivorPanel", survivorPanel);
            SetObjectRef(so, "mapPanel", mapPanel);

            SetObjectRef(so, "openBuildButton", buildButton);
            SetObjectRef(so, "openSurvivorButton", survivorButton);
            SetObjectRef(so, "openMapButton", mapButton);
            SetObjectRef(so, "saveButton", saveButton);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameManager);
        }

        private static void WireResourceBarUi(
            ResourceBarUI ui,
            Text wood,
            Text food,
            Text fuel,
            Text scrap,
            Text heat,
            Text power,
            Text survivors,
            Text morale,
            Text tick,
            Button saveButton)
        {
            SerializedObject so = new SerializedObject(ui);
            SetObjectRef(so, "woodText", wood);
            SetObjectRef(so, "foodText", food);
            SetObjectRef(so, "fuelText", fuel);
            SetObjectRef(so, "scrapText", scrap);
            SetObjectRef(so, "heatText", heat);
            SetObjectRef(so, "powerText", power);
            SetObjectRef(so, "survivorText", survivors);
            SetObjectRef(so, "moraleText", morale);
            SetObjectRef(so, "tickText", tick);
            SetObjectRef(so, "saveButton", saveButton);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
        }
        private static void WireBuildMenuUi(
            BuildMenuUI ui,
            Button buildShelter,
            Button buildHeater,
            Button buildLumber,
            Button buildKitchen,
            Button buildStorage,
            Button buildGenerator,
            Button upgradeShelter,
            Button upgradeHeater,
            Button upgradeLumber,
            Button upgradeKitchen,
            Button upgradeStorage,
            Button upgradeGenerator,
            Button cancelPlacement,
            Text summary)
        {
            SerializedObject so = new SerializedObject(ui);
            SetObjectRef(so, "buildShelterButton", buildShelter);
            SetObjectRef(so, "buildHeaterButton", buildHeater);
            SetObjectRef(so, "buildLumberMillButton", buildLumber);
            SetObjectRef(so, "buildKitchenButton", buildKitchen);
            SetObjectRef(so, "buildStorageButton", buildStorage);
            SetObjectRef(so, "buildGeneratorButton", buildGenerator);
            SetObjectRef(so, "upgradeShelterButton", upgradeShelter);
            SetObjectRef(so, "upgradeHeaterButton", upgradeHeater);
            SetObjectRef(so, "upgradeLumberMillButton", upgradeLumber);
            SetObjectRef(so, "upgradeKitchenButton", upgradeKitchen);
            SetObjectRef(so, "upgradeStorageButton", upgradeStorage);
            SetObjectRef(so, "upgradeGeneratorButton", upgradeGenerator);
            SetObjectRef(so, "cancelPlacementButton", cancelPlacement);
            SetObjectRef(so, "summaryText", summary);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
        }

        private static void WireSurvivorPanelUi(
            SurvivorPanelUI ui,
            Button lumber,
            Button cook,
            Button builder,
            Button explorer,
            Button medic,
            Button collector,
            Button reset,
            Text summary,
            Text list)
        {
            SerializedObject so = new SerializedObject(ui);
            SetObjectRef(so, "assignLumberjackButton", lumber);
            SetObjectRef(so, "assignCookButton", cook);
            SetObjectRef(so, "assignBuilderButton", builder);
            SetObjectRef(so, "assignExplorerButton", explorer);
            SetObjectRef(so, "assignMedicButton", medic);
            SetObjectRef(so, "assignCollectorButton", collector);
            SetObjectRef(so, "resetJobsButton", reset);
            SetObjectRef(so, "summaryText", summary);
            SetObjectRef(so, "listText", list);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
        }

        private static void WireMapUi(MapUI ui, Transform tileRoot, Button tileTemplate, Text summary, Button unlock, Button explore)
        {
            SerializedObject so = new SerializedObject(ui);
            SetObjectRef(so, "tileRoot", tileRoot);
            SetObjectRef(so, "tileButtonTemplate", tileTemplate);
            SetObjectRef(so, "summaryText", summary);
            SetObjectRef(so, "unlockFirstButton", unlock);
            SetObjectRef(so, "exploreFirstButton", explore);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
        }

        private static void WireEventPopupUi(EventPopupUI ui, GameObject panelRoot, Text title, Text description, params Button[] choices)
        {
            SerializedObject so = new SerializedObject(ui);
            SetObjectRef(so, "panelRoot", panelRoot);
            SetObjectRef(so, "titleText", title);
            SetObjectRef(so, "descriptionText", description);

            SerializedProperty choicesProp = so.FindProperty("choiceButtons");
            if (choicesProp != null && choices != null)
            {
                choicesProp.arraySize = choices.Length;
                for (int i = 0; i < choices.Length; i++)
                {
                    SerializedProperty element = choicesProp.GetArrayElementAtIndex(i);
                    element.objectReferenceValue = choices[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
        }

        private static void WireToastUi(ToastUI ui, Text toastText, CanvasGroup canvasGroup)
        {
            SerializedObject so = new SerializedObject(ui);
            SetObjectRef(so, "toastText", toastText);
            SetObjectRef(so, "canvasGroup", canvasGroup);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
        }

        private static void SetObjectList<T>(SerializedObject so, string propertyName, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null || !prop.isArray)
            {
                return;
            }

            int count = values != null ? values.Count : 0;
            prop.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(i);
                element.objectReferenceValue = values[i];
            }
        }

        private static void SetObjectRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Float)
            {
                prop.floatValue = value;
            }
        }

        private static void SetVector2(SerializedObject so, string propertyName, Vector2 value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Vector2)
            {
                prop.vector2Value = value;
            }
        }

        private static void SaveSceneToMainPath(Scene scene)
        {
            string directory = Path.GetDirectoryName(MainScenePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            EditorSceneManager.SaveScene(scene, MainScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
