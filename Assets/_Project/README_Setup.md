# Frozen Frontier MVP Setup

## Quick Start (Recommended)
1. Open Unity.
2. Run `Tools > Frozen Frontier > Generate Main Scene (One Click)`.
   - This also ensures default `ScriptableObject` data assets exist (buildings/biomes/events).
3. (Optional) Run `Tools > Frozen Frontier > Generate Data Assets` if you want to reset/rebuild default balancing assets.
4. (Optional) If tests behave strangely after earlier crashes, run `Tools > Frozen Frontier > Clear Save File`.
5. Open `Assets/_Project/Scenes/Main.unity` if it does not open automatically.
6. Press Play.
7. If a very old save is present, it is automatically ignored in save format v2 and a fresh state is started.

## World Controls
- `W/A/S/D` or arrow keys: pan camera
- Mouse wheel: zoom in/out
- `1`: focus base area
- `2`: focus map area
- Map world click:
  - Click `Locked` tile -> unlock (if adjacent + enough resources)
  - Click `Unlocked` tile -> start exploration

## 1) Folder / File Tree
```text
Assets/
  _Project/
    Scenes/
      Main.unity
    Prefabs/
      UI/
      Buildings/
      Map/
    Art/
      Sprites/
    Scripts/
      Core/
        GameManager.cs
      Systems/
        BaseGridInput.cs
        BuildingWorldView.cs
        MapWorldView.cs
        MapWorldInput.cs
        WorldCameraController.cs
        TimeSystem.cs
        ResourceSystem.cs
        BuildingSystem.cs
        SurvivorSystem.cs
        MapSystem.cs
        EventSystem.cs
        SaveSystem.cs
      Data/
        ResourceType.cs
        SurvivorJob.cs
        TileState.cs
        ResourceAmount.cs
        BuildingDef.cs
        TileBiomeDef.cs
        EventDef.cs
        SaveDataModels.cs
        RuntimeModels.cs
      UI/
        ResourceBarUI.cs
        BuildMenuUI.cs
        SurvivorPanelUI.cs
        MapUI.cs
        EventPopupUI.cs
        ToastUI.cs
      Util/
        Editor/
          FrozenFrontierDataGenerator.cs
          MainSceneBootstrapper.cs
    Data/
      ScriptableObjects/
      Defaults/
    StreamingAssets/
    Resources/
```

## 2) Unity Setup Checklist

### A) URP Path (Universal 2D, preferred)
1. Open project in Unity Hub with template `Universal 2D`.
2. Open `Assets/_Project/Scenes/Main.unity`.
3. Add `GameRoot` empty GameObject.
4. Add components to `GameRoot`:
   - `GameManager`
   - `TimeSystem`
   - `ResourceSystem`
   - `BuildingSystem`
   - `SurvivorSystem`
   - `MapSystem`
   - `EventSystem`
   - `SaveSystem`
5. Add `Light 2D > Global Light 2D` to scene.
6. Add one `Light 2D > Point Light 2D` near base area.
7. Create `Canvas` (Screen Space - Overlay), then add:
   - `ResourceBar`
   - `LeftMenu` (Build, Survivors, Map, Save buttons)
   - Panels:
     - `BuildMenuPanel`
     - `SurvivorPanel`
     - `MapPanel`
     - `EventPopup`
     - `Toasts`
8. Add UI script components to matching panel roots:
   - `ResourceBarUI`, `BuildMenuUI`, `SurvivorPanelUI`, `MapUI`, `EventPopupUI`, `ToastUI`
9. In `GameManager`, assign all system references and all UI references.
10. In each UI script, link required `Text`, `Button`, template, and panel fields.
11. Create empty `BaseArea` and empty `MapArea` GameObjects.
12. Save scene.

### B) Built-in Path (2D Built-in fallback)
1. Create project with `2D (Built-in Render Pipeline)`.
2. Use same scene object hierarchy as above.
3. Skip URP 2D light objects.
4. Use default camera + sprite placeholders.
5. Keep same script assignments and UI wiring.

## 3) Minimal Prefabs (recommended)
1. `Prefabs/UI/TileButton.prefab`
   - `Button` + child `Text`
   - Used as `MapUI.tileButtonTemplate`
2. `Prefabs/UI/BuildButton.prefab`
   - `Button` + child `Text`
   - Used as `BuildMenuUI.buildButtonTemplate`
3. `Prefabs/UI/Toast.prefab`
   - Root with `CanvasGroup`
   - Child `Text`
   - Used by `ToastUI`
4. `Prefabs/Buildings/*` (optional visuals)
   - Simple square sprites for Shelter/Heater/Lumber Mill/Kitchen/Storage/Generator
   - Not required for logic, only for presentation

## 4) How To Run / Test
1. Enter Play Mode.
2. Verify resources update over time and `Heat` changes each tick.
3. Open Build panel, select `Lumber Mill`, then click a base-grid cell in world view to place it.
4. Open Survivors panel and assign a `Lumberjack`; wood growth should improve. (`Shift+Click` assigns all idle survivors to a job.)
5. Pan/zoom camera and verify world map is visible; click locked adjacent tiles to unlock and unlocked tiles to explore.
6. Open Map panel and verify quick actions (`Unlock First Adjacent Tile`, `Explore First Unlocked Tile`) still work.
7. Wait for random event popup and choose an option.
8. Click Save (or wait for autosave), stop Play Mode, restart Play Mode, and confirm state restores with offline progress capped by `TimeSystem`.
9. If testing from an old project save, verify old save versions are ignored and no crash occurs on boot.
