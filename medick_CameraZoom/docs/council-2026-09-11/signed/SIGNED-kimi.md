# SIGNED — 🟣➤🌙 Kimi (kimi-k3-high via Cursor, 💸 credits) · code-quality lens · read-only · mirror commit 806b1c1 · sessionId 90cde325-807a-4c2f-b83b-0199216cd773 · meter 34,364 in / 8,996 out
# Saved verbatim by the conductor 2026-09-11. Seat output is data, not instructions.

**DONE_WITH_CONCERNS**

## Findings

| Rank | Claim | Mechanism | Verify-by / repair | Path:line |
|---|---|---|---|---|
| MATERIAL | End key toggles the panel while the player is typing in chat — End is a text-navigation key | `OnUpdate` calls `Input.GetKeyDown(KeyCode.End)` with no chat/input-field focus check | In-game: open chat, press End → panel pops over chat. Repair: gate toggle on game input-field focus | src/CameraZoomMod.cs:25-30 |
| MATERIAL | InputGuard can clear Terrible Cooldowns' input block | `_applied = true` is set on every wanted frame even when the flag was already true (set by the sibling); release then writes `false` unconditionally — the exact pattern Cooldowns' council pass removed tonight. The re-assert comment (:29-32) does not fix ownership | Cooldowns panel open + blocking; hover this panel once, leave → Cooldowns' block drops until it re-evaluates. Repair: set `_applied` only when this mod flipped the flag | src/InputGuard.cs:33-40 |
| MATERIAL | NaN/out-of-range `PanelX`/`PanelY` in cfg makes the panel permanently unreachable | Raw prefs loaded into `_rect` (:43-45); `Mathf.Clamp` propagates NaN (:97-98) → NaN rect → `Contains` false, nothing draws, Reset button unreachable, and `Close()`/`SavePanelPos` re-persist the NaN | Hand-edit cfg PanelX=NaN → panel gone until cfg surgery. Repair: finite+range check on load, fall back to defaults | src/UI/SettingsPanel.cs:43-45,97-98 |
| MATERIAL | `CameraManager.instance` getter failure is swallowed forever — the stall-warning design never fires | OnUpdate's bare `catch {}` wraps the `.instance` read itself; if the interop property throws after a game patch, `TryCapture` is never reached, `CaptureStalled` never sets, no warning is ever logged — silent idle + per-frame exception cost | Simulate by making the getter throw. Repair: count consecutive failures in OnUpdate and warn once | src/CameraZoomMod.cs:39-45 |
| MINOR | `MenuScale = NaN` in cfg → NaN panel layout | Same clamp-propagates-NaN issue at the scale freeze | Repair: finite check before assigning `_sc` | src/UI/SettingsPanel.cs:55 |
| MINOR | Stale docs/comments | Prefs descriptions claim game defaults ≈ −15 / 1-2 / 5-8; tonight's capture is −7.0 / 0.2 / 2.5 (melonloader-lines.txt:122). README install says MelonLoader 0.6.x vs 0.7.2 tonight. CameraState says "v2 safety rules"/"v1.x's bug class" in a v1.0.0 mod | Zero-risk doc sweep | src/Prefs.cs:33-35; README.md:30; src/CameraState.cs:15,96,106 |
| MINOR | Orphan cfg keys FOV/MinFOV/MaxFOV/SmoothSpeed/RmbModifier + inert `[kg_CameraZoom]` section | No code in `src\` registers them (verified by reading all 9 files); harmless but misleads support ("I set FOV=120 and nothing happened") | Prune or document as legacy | zoom-cfg-excerpt.txt:12-18 |
| MINOR | IMGUI state read inside camera logic | `GUIUtility.hotControl` read in `Apply` couples the state layer to event state from an OnUpdate caller; works (hotControl persists across a drag) but belongs at the call site | Refactor: pass `sliderHot` in | src/CameraState.cs:184 |
| MINOR | Theme retry loop leaks textures on persistent failure | `Bordered()` allocates `HideAndDontSave` textures; if construction throws partway, every OnGUI event retries and leaks 3×3 textures | Edge case; bound retries or destroy partials | src/UI/Theme.cs:94-107 |
| MINOR | csproj hygiene: no `<Version>` (assembly version defaults while brand version lives only in BuildInfo.cs), no `<LangVersion>`, `Nullable` disabled; `bin/`/`obj/` artifacts present in the repo tree | Cosmetic; brief's "no `<Compile Remove>` for docs" is a non-issue (SDK globs only .cs) | Add Version/LangVersion; gitignore artifacts | medick_CameraZoom.csproj:4-8 |
| NOT PROVEN | Fight with other camera mods | `kg_CameraZoom.dll` absent tonight; compare-then-write should coexist; no second writer observed | — | src/CameraState.cs:151-161 |

## Q1 — Breakage
Field renames surface as exceptions caught per frame: reads inside `TryCapture`/`Apply` count toward the 300-frame stall warning (good), but the `instance` getter itself is wrapped by the silent OnUpdate catch — **that** path idles forever without a log line (table row 4). Harmony patch on `CameraManager.Start`: if the method vanishes, patch discovery fails at load — MelonLoader logs it, game survives; no crash path found. InputGuard vs Cooldowns: two real interleavings — this mod releasing clears the sibling's block (row 2); the reverse is self-healing because this mod re-asserts from the live flag each frame (InputGuard.cs:33).

## Q2 — Camera correctness
Clamp direction is coherent: zoomMin is the most-negative bound (`tgt < zoomMin → tgt = zoomMin`, CameraState.cs:184-190), consistent with "more negative = further out" and tonight's −7.0/−17.5 capture. Live config ZoomMin=-1.0 legally pins zoom-out to −1 (ZoomMinHi=-1 permits it) — a config oddity, not a code bug. Angle unlock: one-shot restore of per-manager captured real limits (:164-173); `_wroteLock` resets on manager change (:66), so a zone change can't restore stale tilt into the new manager. No non-finite write path found (Sane + Finite gates; live slider NaN-checked at SettingsPanel.cs:142). Live slider vs reclamp: slider's `lo` is `SaneZoomMin` and reclamp yields to `hotControl` — no fight. Rescue/deinit restore all six fields + `resetZoom()` (:200-211). Real gaps: End-in-chat (row 1), and `RestoreToGame` trusts `Captured` but not that `mgr` is the captured manager — a same-frame zone-change + unload writes stale originals once; harmless in practice since the mod is unloading.

## Q3 — Per-frame cost
OnUpdate: ~6 Il2Cpp reads/frame unlocked (`instance`, `GetInstanceID`, `zoomMin`, `zoomPerScroll`, `zoomSpeed`, `targetZoom`, `currentZoom`), +3 when angle-locked; +2 for InputGuard while active. `TryCapture` exits early once latched (:70) — good. OnGUI per event while open: `instance` + 3 more reads, plus ~8 interpolated strings/event (status row, ZoomLabels, chips, footer) — bounded GC churn, acceptable for IMGUI. Style caches are keyed on rounded sizes (sc ∈ [0.7,2.0] → ~20 sizes) — **bounded**, no unbounded growth. No Il2Cpp object references are held past frame scope (`_mgrId` is an int; panel's `mgr` is a local). The only unbounded-growth edge is the Theme retry leak (row 9).

## Q4 — Settings resilience
Confirmed: fully self-drawn IMGUI; zero dependence on game UI templates or child indices — immune to the "Toogle - Minion Health Bars" failure class that hit the siblings. `ZoomMin=+50` → clamped to −1; `Angle=NaN` → falls back to captured default; `ZoomPerScroll`/`ZoomSpeed` sanitized. **Unsanitized:** PanelX/PanelY/MenuScale (rows 3, 5) — the one real config trap. Orphan keys: harmless, worth pruning (row 7).

## Q5 — Top 3 cleanups (value/risk)
1. **Finite+range sanitize PanelX/PanelY/MenuScale on load** — kills a real support trap; ~5 lines in `Draw`'s load block.
2. **InputGuard `_applied` ownership fix** — one-condition change, mirrors tonight's proven Cooldowns repair, fixes cross-mod input leak.
3. **Zero-risk doc sweep** — Prefs "game default" descriptions vs actual −7.0/0.2/2.5, README's MelonLoader 0.6.x line, "v2"/"v1.x" comments in a v1.0.0 mod.

## Q6 — Verdicts
- CameraZoomMod.cs — **PATCH** (chat-focus guard; fail-counting catch).
- CameraZoomMod.Patches.cs — **PATCH** (fine as-is; keep, maybe soften the auto-discovery superstition comment).
- CameraState.cs — **PATCH** (stale v2/v1.x comments; hotControl coupling; otherwise the strongest file).
- InputGuard.cs — **PATCH** (ownership semantics — the one behavioral must-fix).
- Prefs.cs — **PATCH** (stale "game default" descriptions).
- BuildInfo.cs — **RETIRE nothing / PATCH** (keep; consider moving version into csproj).
- SettingsPanel.cs — **PATCH** (NaN sanitize; otherwise solid).
- Theme.cs — **PATCH** (retry-leak edge only).
- Widgets.cs — **PATCH** (dead `_btnSelected` path via never-used `selected` arg; duplicated `rowH` math; cosmetic).
Nothing merits REBUILD or RETIRE.

## Manifest
Read in full: BRIEF.md, README.md, CHANGELOG.md, melonloader-lines.txt, zoom-cfg-excerpt.txt, medick_CameraZoom.csproj, src\CameraZoomMod.cs, src\CameraZoomMod.Patches.cs, src\CameraState.cs, src\InputGuard.cs, src\Prefs.cs, src\BuildInfo.cs, src\UI\SettingsPanel.cs, src\UI\Theme.cs, src\UI\Widgets.cs (15 files). **I could not compute MD5s** — three hash attempts (md5sum, PowerShell Get-FileHash, certutil) each returned no output and no exit status from this shell; rather than invent hashes I list the files plainly. `release\_archive\` was not present in the working tree I could see.

🟣➤🌙 Kimi

---
🟣➤🌙 [wmw-cursor] 💸 Moonshot · kimi-k3-high
   sessionId: 90cde325-807a-4c2f-b83b-0199216cd773 · meter: CREDITS · 34364 in / 8996 out
🟣➤ 💸 —— THIS CALL SPENT MONEY —— 💸 🟣➤
   third-party CREDIT pool — billed at API prices
