# Changelog — MedicK's Terrible Zoom

## v1.0.0 — 2026-07-01

Ground-up engineering pass and rebrand: **Camera Zoom** is now
**MedicK's Terrible Zoom** ("Terrible Zoom" for short), joining the Terrible
family. Internal name, DLL, preferences and the Nexus upgrade path are
unchanged (`medick_CameraZoom`). The version resets to **1.0.0** — the
Terrible era starts here (succeeds Camera Zoom v1.2).

### Fixed
- **The stuck camera (grizwad's report: "camera angle quit changing and got
  stuck under ground — restarting was the only fix").** Three compounding
  v1.x defects, all eliminated:
  - The originals-captured latch was set *before* the values were read — a
    single failed read froze NaN originals in permanently. The rewrite latches
    only after every value reads back finite, and retries otherwise.
  - The live-zoom slider's range guard (`max <= min`) passes NaN straight
    through (NaN comparisons are always false), letting a drag write NaN
    into `targetZoom` — and a NaN lerp never recovers. The rewrite never
    writes a non-finite value to the camera and self-heals a poisoned zoom
    back to the game default every frame.
  - Unlocking the angle wrote a fabricated ±25° range because the real
    `cameraAngleMin/Max` were never captured. The rewrite captures and
    restores the game's actual limits.
- New **Rescue** button restores every captured game default in one click —
  no restart required, ever.

### Fixed in the release-night fable pass (2026-07-01)
- Panel clicks no longer leak into the game: input pauses while your pointer
  is over the open panel (the Terrible Cooldowns technique), so tuning a
  slider no longer sends your character jogging across the zone.
- Camera originals are captured per zone now, and unlocking the tilt restores
  the game's values once instead of enforcing the first zone's limits
  everywhere forever (the "tilt feels wrong in some zones" finickiness).
- First install no longer changes your scroll feel: sensitivity and glide
  speed adopt the game's own values until you move a slider. The extended
  zoom-out limit stays as shipped, that one is the mod.
- The zoom-out limit slider no longer drags the camera inward with it
  mid-drag.
- Panel styles rebuild after a failed construction instead of latching
  broken.
- Clearer labels: tilt is called tilt, the zoom limit says "lower = farther",
  and the live-zoom row says what it does.

### Added
- Redesigned settings panel on the Terrible-family design system (dark
  LE-native palette, gold accent, custom switches/sliders, branded chrome).
- Panel position persists across sessions (+ reset button).
- Live status row (current/target zoom, angle, game default).
- `DebugLog` preference — per-scene apply logging is opt-in.

### Changed
- Source restructured from one 460-line file into `src/` modules; Harmony
  patch nested in the mod class (MelonLoader auto-discovery guardrail).
- Camera fields are compare-then-write instead of stomped every frame —
  plays nice with per-scene resets and other camera mods.
- The mod restores all game camera defaults if it is unloaded.
- Settings carry over from v1.x unchanged (same preference category/keys).

---

## v1.2 — 2026-04-09
- Initial Nexus release (mod #27): extended zoom-out limit, scroll
  sensitivity and lerp speed sliders, live current-zoom slider, camera
  angle lock, End-key settings panel, live status banner.
- Patches CameraManager fields, not `Camera.main.fieldOfView` (which the
  manager overrides every frame).
