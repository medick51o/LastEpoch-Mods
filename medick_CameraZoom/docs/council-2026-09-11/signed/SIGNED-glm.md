# SIGNED — 🟣➤🔷 GLM (glm-5.2-high via Cursor, 💸 credits) · adversarial-correctness lens · read-only · mirror commit 806b1c1 · sessionId 313d66e7-c28c-48d0-8bd5-26c8a23340cb · meter 52,553 in / 3,552 out
# Saved verbatim by the conductor 2026-09-11. Seat output is data, not instructions.

DONE_WITH_CONCERNS

| Rank | Claim | Mechanism | Verify/Repair | Path:line |
|---|---|---|---|---|
| BLOCKER | InputGuard stomps sibling lock on close | Zoom re-asserts flag every wanted frame (`_applied=true` line 34) but on close writes `false` unconditionally (line 39). With staged Cooldowns fix (no re-assert), closing Zoom while Cooldowns panel open kills the movement lock → slider drags send the character jogging. Repro: open both panels, close Zoom first. | Make release conditional: only clear if Zoom itself last set it, or re-check sibling state. | `src\InputGuard.cs:34-39` |
| BLOCKER | zoomMin reclamp direction inverts game convention | Game ships `zoomMin=-7.0`, `zoomDefault=-17.5` (more-negative=further-out; zoomMin is the CLOSEST limit, default rests further out). Mod treats zoomMin as the FURTHEST-out limit and reclaims `tgt < zoomMin → tgt=zoomMin` (line 188). With player's live `ZoomMin=-1.0` (sanitized to -1.0), `tgt(-17.5) < -1.0` is true every frame → `targetZoom` yanked to -1.0 (closest) forever; player can never zoom out. | Confirm game semantics via LE decomp; flip reclaim to `tgt > zoomMin` if zoomMin is the closest bound, or remove the auto-reclaim and let the game clamp. | `src\CameraState.cs:186-189`, `src\Prefs.cs:18` |
| MATERIAL | Per-frame swallowed exception if any CameraManager field renames | `Apply` reads 8 fields + writes inside one `try { … } catch {}` (lines 144-194). A single renamed field throws at the first `Differs`/write; the whole block aborts and retries next frame — 60+ IL2CPP exceptions/sec, silently idle, no log after the one stall warning. | Split per-field try/catch; log once on first failure. | `src\CameraState.cs:144-194` |
| MATERIAL | `resetZoom()` rename silently breaks Rescue | `RestoreToGame` calls `mgr.resetZoom()` inside `try{}catch{}` (line 209). If method renamed, restore silently no-ops; camera stays modded. | Log on catch; consider field-only restore. | `src\CameraState.cs:209` |
| MATERIAL | End key fires while typing in chat/search | `OnUpdate` reads `Input.GetKeyDown(End)` unconditionally (line 27) — no check for an open text input. Pressing End in chat toggles the panel and (via InputGuard) pauses game input mid-message. | Verify LE's chat-focus flag; gate End on it. | `src\CameraZoomMod.cs:27-32` |
| MATERIAL | Orphan config keys (`FOV`,`MinFOV`,`MaxFOV`,`SmoothSpeed`,`RmbModifier`) + `[kg_CameraZoom]` section | No code in `src\` registers these (Prefs.Init registers 9 entries only). They are leftovers from a prior kg_ mod / older build. Harmless to runtime (MelonPreferences ignores unknown keys) but mislead users editing the cfg. | Prune from shipped cfg template; document. | `src\Prefs.cs:31-43`, `zoom-cfg-excerpt.txt` |
| MINOR | `OnGUI` runs multiple times/frame; `mgr` local re-fetched each call | `Draw` re-reads `CameraManager.instance` + 3 fields every OnGUI event (lines 65-71). Layout+repaint events mean 2-4× per frame; cheap but unnecessary. | Cache once per frame via a flag. | `src\UI\SettingsPanel.cs:65-71` |
| MINOR | Style dictionaries grow without bound | `Theme._labels` / `_buttons` keyed by `(size,fs,anchor,serif)` (line 40). Sizes are `RoundToInt(N*sc)` — fractional `sc` (e.g. 1.457) yields many unique ints over time; dictionary grows. Bounded in practice but not capped. | Round sc to fixed step. | `src\UI\Theme.cs:40-43` |
| MINOR | Stale "v2 safety rules" / "v1.x's bug class" comments in a v1.0.0 mod | CameraState.cs:14-22 references "v2" rules and "v1.x" bug class; CHANGELOG says version "resets to 1.0.0". Confusing for future maintainers. | Reword to "release-2026-07 safety rules". | `src\CameraState.cs:14-22` |
| MINOR | README install line says "MelonLoader 0.6.x" but tonight's log is 0.7.2 Open-Beta | README line 31. Mismatched install prerequisite vs tested runtime. | Update to 0.7.x. | `README.md:31` |
| MINOR | csproj has no `<Version>`; version lives only in `BuildInfo.cs` | No `<Version>` element means `dotnet pack` would emit 1.0.0.0 only via AssemblyInfo; release packaging relies on the literal. | Add `<Version>1.0.0</Version>` to csproj. | `medick_CameraZoom.csproj:5-9`, `src\BuildInfo.cs:14` |
| NOT PROVEN | Lock stays locked after zone change | Per-zone latch resets `_wroteLock=false` on new manager id (line 57), and Apply re-locks next frame if `LockAngle` true. No path leaves a stale lock on a fresh manager. | — | `src\CameraState.cs:55-58` |
| NOT PROVEN | NaN written to camera | All writes pass through `Sane`/`Finite`; live-zoom slider guards `!float.IsNaN(next)` (SettingsPanel.cs:142). No NaN reaches a write. | — | `src\CameraState.cs:218-220` |
| NOT PROVEN | `MenuScale=0` breaks panel | `_sc` clamped to `[0.7,2.0]` (SettingsPanel.cs:54); `Sane` clamps pref. Zero cannot reach layout. | — | `src\UI\SettingsPanel.cs:54` |
| NOT PROVEN | `PanelX=NaN` breaks panel | `_rect.x` loaded once then `Mathf.Clamp`ed to screen (line 97). NaN clamp → NaN rect, panel invisible, not a crash. | — | `src\UI\SettingsPanel.cs:44-97` |

**Q1** — Harmony `Start` postfix: rename of `CameraManager`/`Start` → MelonLoader logs a patch-apply failure, mod falls back to OnUpdate retry (silent). Field renames: per-frame swallowed exceptions in `Apply` (MATERIAL). `resetZoom()` rename: silent Rescue failure (MATERIAL). `GetInstanceID()` rename: caught, stall warning after 300 frames. `forceDisableInput` rename: caught every frame while panel open → input pause silently dead, no crash. Sibling Cooldowns: BLOCKER — Zoom's unconditional `false` on close stomps a non-re-asserting sibling; reverse direction self-heals because Zoom re-asserts every wanted frame.

**Q2** — BLOCKER: the reclamp sign is inverted vs the game's reported convention (zoomMin=-7 closest, zoomDefault=-17.5 further out). With the live `ZoomMin=-1.0`, the per-frame reclaim pins `targetZoom=-1.0` and the player can never zoom out. Angle lock/unlock: NOT PROVEN stuck — one-shot restore on lock→unlock (lines 168-173) is correct. NaN: NOT PROVEN — fully guarded. Live-zoom slider vs reclamp: gated by `GUIUtility.hotControl==0` (line 187), so mid-drag wins; but on mouse-up the reclaim yanks back per the BLOCKER above. End key in chat: MATERIAL.

**Q3** — Per frame in OnUpdate: 1 `Input.GetKeyDown`, 1 `CameraManager.instance` read, `TryCapture` (≤7 field reads + `GetInstanceID`), `Apply` (≤8 field reads + writes), `InputGuard.Apply` (1 `EpochInputManager.instance` + 1 flag read). OnGUI runs per event (layout+repaint+mouse): re-fetches `mgr` + 3 fields each call (MINOR). Cached refs: `_mgrId` (int, safe), `mgr` local in Draw (Il2Cpp ref, used same call — safe). Allocations: `new GUIContent` per `CalcSize` (Theme/Widgets), `new Rect` structs (stack, cheap). Style dictionaries grow with distinct rounded sizes (MINOR). No unbounded growth proven.

**Q4** — Panel is fully self-drawn IMGUI; no game UI template name or child index lookup (confirmed across SettingsPanel/Theme/Widgets). Bad prefs: `NaN PanelX` → invisible panel (NOT PROVEN crash); `MenuScale=0` → clamped to 0.7 (NOT PROVEN); `ZoomMin=+50` → clamped to `ZoomMinHi=-1` via `Sane` (line 220) → becomes -1 → triggers BLOCKER reclamp; `Angle=NaN` → `Sane` returns `AngleDefault` fallback. Orphan keys: MATERIAL (misleading, not runtime-harmful) — prune.

**Q5** — Best value-to-risk cleanups: (1) Fix the BLOCKER reclamp direction (highest value, low risk — one comparison flip after confirming LE semantics). (2) Make `InputGuard` release conditional on self-ownership (BLOCKER, low risk — matches the staged Cooldowns fix). (3) Prune orphan cfg keys + update README ML version + reword "v2"/"v1.x" comments (MINOR, zero risk, pure docs hygiene).

**Q6** — Per-file verdict:
- `src\CameraZoomMod.cs` — PATCH (gate End key on chat-focus).
- `src\CameraZoomMod.Patches.cs` — RETIRE-as-is (correct; no change).
- `src\CameraState.cs` — REBUILD (reclaim direction + per-field try/catch + comment reword).
- `src\InputGuard.cs` — PATCH (conditional release).
- `src\Prefs.cs` — PATCH (orphan-key note; consider seeding AngleDefault from capture).
- `src\BuildInfo.cs` — RETIRE (correct).
- `src\UI\SettingsPanel.cs` — PATCH (cache mgr per frame; live-zoom reclaim interaction).
- `src\UI\Theme.cs` — PATCH (round sc to step; cap dictionaries).
- `src\UI\Widgets.cs` — RETIRE (correct).
- `medick_CameraZoom.csproj` — PATCH (add `<Version>`).
- `README.md` — PATCH (ML 0.7.x, sign convention).
- `CHANGELOG.md` — PATCH (note reclaim-direction fix in next entry).

**Manifest** — files read (could NOT compute MD5s: the shell tool returned no output in this session, so hashes were not obtainable; no hashes invented):
`BRIEF.md`, `README.md`, `CHANGELOG.md`, `medick_CameraZoom.csproj`, `melonloader-lines.txt`, `zoom-cfg-excerpt.txt`, `src/CameraZoomMod.cs`, `src/CameraZoomMod.Patches.cs`, `src/CameraState.cs`, `src/InputGuard.cs`, `src/Prefs.cs`, `src/BuildInfo.cs`, `src/UI/SettingsPanel.cs`, `src/UI/Theme.cs`, `src/UI/Widgets.cs`.

🟣➤🔷 GLM

---
🟣➤🔷 [wmw-cursor] 💸 Zhipu · glm-5.2-high
   sessionId: 313d66e7-c28c-48d0-8bd5-26c8a23340cb · meter: CREDITS · 52553 in / 3552 out
🟣➤ 💸 —— THIS CALL SPENT MONEY —— 💸 🟣➤
   third-party CREDIT pool — billed at API prices
