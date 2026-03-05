# Frozen Frontier

Unity project for the **Frozen Frontier** prototype/MVP.

## Requirements
- Unity Editor (project version is defined in `ProjectSettings/ProjectVersion.txt`)

## Quick Start
1. Open the project in Unity Hub.
2. In Unity, run `Tools > Frozen Frontier > Generate Main Scene (One Click)`.
3. Open `Assets/_Project/Scenes/Main.unity` if it is not opened automatically.
4. Press Play.

Detailed setup notes live in `Assets/_Project/README_Setup.md`.

## Repository Layout
- `Assets/` gameplay scripts, scenes, ScriptableObjects, prefabs
- `Packages/` Unity package manifest and lock file
- `ProjectSettings/` Unity project settings

## Git Notes
This repository ignores Unity-generated cache/build folders like `Library/`, `Temp/`, `Logs/`, and `UserSettings/` via `.gitignore`.