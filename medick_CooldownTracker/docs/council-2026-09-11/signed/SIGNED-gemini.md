# 🟢 Gemini — Antigravity seat, robustness / player-experience lens — 2026-09-11 (verbatim; conversationId 4187ac8e-d6f4-4277-9e8b-1ae1e35e5831 · brain: UNREPORTED)

DONE_WITH_CONCERNS

| Rank | Claim | Failure Mechanism | Verify-By / Repair | Path:Line |
| --- | --- | --- | --- | --- |
| MATERIAL | Secondary action bars overwrite player skill slots | `Patch_AbilityBarAwake` registers any `AbilityBarIcon` without checking player ownership; `SlotRegistry.Register` replaces slots 0..6 | Spawn minion with bar or open spectator UI; verify slots hijacked. Repair: filter by local player HUD | `src/CooldownTrackerMod.Patches.cs:16`, `src/SlotRegistry.cs:65` |
| MATERIAL | Active floating icons shift horizontally when cooldowns change | `OverheadRenderer.Draw` calculates cluster width from `_buf.Count` of cooling skills (`SnapshotActive`), recalculating `startX` whenever any skill finishes | Trigger 2 cooldowns with different durations; observe remaining icon snap position on first expiry. Repair: fixed slot grid | `src/UI/OverheadRenderer.cs:39`, `src/SlotRegistry.cs:202` |
| MINOR | `Home` key fails to close panel while editing label text field | `CooldownTrackerMod.OnUpdate` skips `Home` toggle when `UiState.TextFieldActive` is true | Click text field in panel, press `Home`; panel stays open. Repair: allow `Home` key to close panel & drop focus | `src/CooldownTrackerMod.cs:32`, `src/UI/SettingsPanel.cs:181` |
| MINOR | Closing panel can unblock movement for sibling mods | `InputBlocker.Apply(false)` sets `forceDisableInput = false` unconditionally when panel closes, ignoring sibling locks | Open Terrible Zoom menu, open/close CooldownTracker panel; movement unblocks. Repair: check sibling lock state | `src/InputBlocker.cs:40` |
| NOT PROVEN | GC stutter from `Camera.main` calls in `OnGUI` | `OverheadRenderer.Draw` calls `Camera.main` every `OnGUI` event without caching | Profile frame times in Unity Profiler / ML log during combat; no log proof provided. Repair: cache `Camera.main` per frame | `src/UI/OverheadRenderer.cs:24` |

### Manifest of Files Read
- `BRIEF.md` (A18EF131B24D933DB98B67F58AC54D0B)
- `CHANGELOG.md` (D1444708A911C71164597934D8EE6F04)
- `cooldown-cfg-excerpt.txt` (B9F439C286755C45680CBBCAAA6EB500)
- `medick_CooldownTracker.csproj` (A1B3AA00B826116F7701E527D968A6C6)
- `melonloader-lines.txt` (D774DF21F4E34A40883E5994B595A74E)
- `README.md` (54B600649E54F7338101ED7A8B8D180F)
- `src/BuildInfo.cs` (63D22A095E2E81F85B89F9F461EE0481)
- `src/CooldownTrackerMod.cs` (CD31698562476332E754DE9216021939)
- `src/CooldownTrackerMod.Patches.cs` (BBA64CFE93F8715C837F1DBFA2BEC50D)
- `src/InputBlocker.cs` (0BE152AC740D1A3202B6E77024D8B881)
- `src/InputDetection.cs` (C5B2F6492DD341375B63D32C36D76843)
- `src/Prefs.cs` (01DF7052D7BFF469691C06480E9FD071)
- `src/SlotRegistry.cs` (047B4A5BBA100BC7C14FD2D972D4A4A9)
- `src/UI/ButtonPicker.cs` (F0573919665C5D3068B0AEB5DFE7113C)
- `src/UI/OverheadRenderer.cs` (CED8939C78C72E81B76995E5D10E1D48)
- `src/UI/SettingsPanel.cs` (07C157ECD6B01B3AB804290EFA6175A1)
- `src/UI/Theme.cs` (718AE28117513F8909674EB5A69D50FB)
- `src/UI/UiState.cs` (4C7830B7EE4933CEC1E747364994D057)
- `src/UI/Widgets.cs` (572FAE9691C10857F3C67994CEFF3D60)

### Answers to Council Questions
**Q1 (Patches & Mod Safety):** If `activate/deactivate` patches fail, `SlotRegistry.Tick` at 20Hz polls `fillAmount` and `cooldownBarActive` as a fallback. `InputBlocker` re-asserts `forceDisableInput` while open, but closing the panel sets `forceDisableInput = false` unconditionally, which can unblock input for sibling mods (e.g. Terrible Zoom).
**Q2 (Display Correctness & Leaks):** Sprites/fills update live at 20Hz. If player dies/zooms out, `TryGetPlayerAnchor` suppresses drawing. Icons shift horizontally when active counts change due to dynamic cluster sizing (`_buf.Count`). Secondary action bars (minion/spectator) register as slots 0..6 and hijack primary slots.
**Q3 (Per-frame Cost & Lifetimes):** `OnUpdate` handles key tracking and tick timing. `OnGUI` executes multiple times per frame and calls `Camera.main` uncached. `SlotData` holds `AbilityBarIcon` references pruned within 50ms on tick when `Source == null`. `GUIStyle` caches are bounded by discrete font sizes.
**Q4 (Settings & Config Resilience):** The panel is self-drawn via custom IMGUI (independent of native UI templates like "Toogle - Minion Health Bars"). Config values (`InputMode`, `MenuScale`, `Size`, `Alpha`) are safely clamped. 14 legacy v4.x keys remain in `MelonPreferences.cfg` harmlessly.
**Q5 (Top 3 Cleanups):** 1) Prune legacy v4.x keys from `MelonPreferences.cfg`. 2) Allow `Home` key to close panel even when text field is active (`CooldownTrackerMod.cs:32`). 3) Fix stale comments referring to "v5" (`InputBlocker.cs:9`).
**Q6 (Verdict per File):**
- `medick_CooldownTracker.csproj`: PATCH — Needs `<Version>` tag and build copy target.
- `src/BuildInfo.cs`: PATCH — Version is hardcoded literal.
- `src/CooldownTrackerMod.cs`: PATCH — `Home` key toggle blocked during text editing.
- `src/CooldownTrackerMod.Patches.cs`: PATCH — Registers any `AbilityBarIcon` without player check.
- `src/SlotRegistry.cs`: PATCH — `Register` overwrites slots without validating primary bar.
- `src/InputDetection.cs`: PATCH — `HotkeyReader` uses legacy `Text` component lookup.
- `src/InputBlocker.cs`: PATCH — `Restore()` unblocks input unconditionally.
- `src/Prefs.cs`: PATCH — Retains legacy unpruned v4.x config entries.
- `src/UI/OverheadRenderer.cs`: PATCH — Dynamic cluster width causes icon jumping.
- `src/UI/SettingsPanel.cs`: PATCH — Keyboard focus release requires explicit mouse click outside band.
- `src/UI/Theme.cs`: PATCH — Core design system is solid and well-cached.
- `src/UI/Widgets.cs`: PATCH — Reusable IMGUI controls are robust.
- `src/UI/ButtonPicker.cs`: PATCH — Glyph modal picker functions correctly.
- `src/UI/UiState.cs`: PATCH — Clean static GUI state container.

🟢 Gemini

---
[wmw-gemini] conversationId: 4187ac8e-d6f4-4277-9e8b-1ae1e35e5831 · status: SUCCESS · brain: UNREPORTED · turns: 1
