# medick_CameraZoom — v1.2

A **MelonLoader** mod for **Last Epoch** that extends the camera's scroll-wheel zoom range and exposes all camera parameters through a live in-game settings panel.

## Why this works (and others don't)

Last Epoch uses its own `CameraManager` class that overwrites `Camera.main` state every frame. Mods that directly set `Camera.main.fieldOfView` have zero lasting effect. This mod writes directly to `CameraManager`'s own fields — the same approach used by other working Last Epoch camera mods.

Fields modified (verified from `Il2CppLE.dll`):

| Field | Effect |
|-------|--------|
| `zoomMin` | Most-zoomed-out limit (default ≈ −15, we set −40) |
| `zoomPerScroll` | Distance change per scroll notch (scroll sensitivity) |
| `zoomSpeed` | How fast the camera lerps to the target zoom |
| `targetZoom` | Current target position — writable for instant repositioning |
| `cameraAngleMin/Max/Default` | Tilt angle controls (used when angle lock is ON) |

Changes are applied both on `CameraManager.Start` (via Harmony postfix) and every frame in `OnUpdate`, so live slider adjustments take effect instantly without a scene reload.

## Features

- Scroll further out — `zoomMin` extended to −40 by default (configurable to −200)
- Adjustable **scroll sensitivity** and **zoom lerp speed**
- Live **Current Zoom** slider — drag to reposition the camera instantly
- Optional **camera angle lock** with adjustable tilt slider
- **Live status banner** showing actual `CameraManager` values each frame
- Captures and displays original game values for easy reset
- Persistent settings via MelonPreferences

## Controls

| Key | Action |
|-----|--------|
| `End` | Toggle settings panel |
| Scroll wheel | Zoom in / out (game-native) |
| Drag title bar | Reposition panel |

## Settings Panel

- **Zoom Out Limit** — how far the scroll wheel lets you go (zoomMin)
- **Scroll Sensitivity** — units of zoom per scroll notch (zoomPerScroll)
- **Zoom Lerp Speed** — how snappy the camera follows its target (zoomSpeed)
- **Current Zoom** — live drag slider; instantly repositions the camera
- **Camera Angle Lock** — toggle + angle slider (cameraAngleMin/Max/Default)
- **Menu Scale** — resize the panel
- **Reset** button — restores all original game values and calls `resetZoom()`

## Installation

1. Install [MelonLoader 0.6.x](https://melonwiki.xyz) into Last Epoch
2. Drop `medick_CameraZoom.dll` into `Last Epoch/Mods/`
3. Launch the game and load into a zone
4. Press `End` to open the settings panel — the green banner confirms the mod is hooked

## Building

```bash
dotnet build -c Release
```

Requires Last Epoch at the default Steam path. Edit `<ML>` and `<GM>` in the `.csproj` if your path differs.
