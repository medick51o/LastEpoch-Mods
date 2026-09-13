# Terrible Cooldowns v1.0.0 — council synthesis (2026-09-11, overnight auto mode)
Seats (blind, read-only, packet C:\Sync\Projects\cooldown-review-2026-09-11, mirror commit 5e9e045): 🔵 Astra (gpt-6-astra, architecture) · 🟢 Gemini (Antigravity, robustness / player experience; brain UNREPORTED) · 🟣➤🌙 Kimi k3-high (code quality, 💸) · 🟣➤🔷 GLM 5.2-high (adversarial correctness, 💸). ⚫ Grok benched. All four returned DONE_WITH_CONCERNS. Mirror `git status --porcelain` empty after all four seats; repo unchanged.
Provenance: NO gap. The DLL in the game's Mods folder (40,448 B, 2026-07-01 20:58, md5 c4210da03fef2976a49e0f7bad636b2a) is byte-identical to the repo's Release build of this source. Astra and Gemini computed MD5s that match the mirror for every file; Kimi and GLM could not run a shell in the Cursor read-only sandbox and list files read without hashes (both said so rather than fabricate).
Astra additionally inspected Il2CppLE.dll (md5 5FEF0752…) and confirmed the three Harmony targets and the five `AbilityBarIcon` members plus `EpochInputManager.instance` / `forceDisableInput` exist with the expected types.

## Agreement across seats (blind → counts)
| # | Finding | Seats | Rank | Fix |
|---|---|---|---|---|
| 1 | InputBlocker release stomps a sibling's lock: `_applied = true` is set even when the flag was already true (Terrible Zoom), and close writes `false` unconditionally (InputBlocker.cs:35-41). Header comment says "written only on change" and contradicts the re-assert loop (InputBlocker.cs:10-11) | Astra M, Kimi M, GLM M, Gemini MINOR | MATERIAL (Zoom is installed on the boss's box) | set `_applied` only when this mod flipped the flag; rewrite header |
| 2 | Registry keyed by `abilityNumber` only: any second `AbilityBarIcon` set (minion bar, spectator, second action bar) replaces the player's slots, last-wins (SlotRegistry.cs:52-67, 120-124) | Astra M, Gemini M, GLM M · Kimi NOT PROVEN (no second bar known in 1.4.7) | MATERIAL if such a bar exists; NOT PROVEN that it does | Tier A: one-per-session warning when a live slot is replaced by a different instance (evidence); Tier B: owner/instance identity |
| 3 | NaN / Infinity prefs survive `Mathf.Clamp` into GUI geometry; NaN PanelX/Y parks the panel off-screen with no in-game recovery; InputMode outside 0-3 falls to Auto with no segment highlighted (Prefs.cs:39-62, SettingsPanel.cs:34-36,54, OverheadRenderer.cs:34-42, InputDetection.cs:115-127) | Astra M, Kimi MINOR, GLM MINOR | MATERIAL for the panel (unrecoverable), MINOR elsewhere | sanitize on Init: non-finite floats → default, InputMode/CtrlLayout clamped |
| 4 | `Camera.main` looked up on every OnGUI event (2-5×/frame) and outside any catch (OverheadRenderer.cs:24) | Astra MINOR, Kimi MINOR, GLM M, Gemini NOT PROVEN | MINOR | cache once per frame (Time.frameCount) inside try/catch |
| 5 | Stale "v4.x"/"v5" narration in a v1.0.0 codebase (InputBlocker.cs:5-12, InputDetection.cs:13,144-148, OverheadRenderer.cs:9-10, Theme.cs:254-255) | Astra, Kimi, GLM, Gemini (Q5) | MINOR | comment-only rewrite |
| 6 | README install line says MelonLoader 0.6.x; tonight's log runs 0.7.2 Open-Beta (README.md:42) | Astra, GLM | MINOR | "0.7.x (tested on 0.7.2 Open-Beta, game 1.4.7)" |
| 7 | 12 orphan v4.x keys in the live cfg (ShowWindow, ShowOverhead, WindowScale, WindowX/Y, IconAlpha, IconSize, OverheadY, EnabledKeys, Controller, SlotLabel0-5) — unregistered, inert | all four | MINOR (harmless) | document as inert; deletion is a Tier B decision (Astra: don't silently delete history) |
| 8 | csproj has no `<Version>`; version literal lives only in BuildInfo.cs | Gemini, Kimi, GLM | MINOR | add `<Version>`; NO copy-to-Mods target (see disagreements) |
| 9 | Live replacement can keep a stale `cooldownBar` / `SlotIndex` if the game swaps the bar Image without destroying it or mutates `abilityNumber` after Awake (SlotRegistry.cs:55,147-153) | Astra M · GLM NOT PROVEN | NOT PROVEN | Tier B in-game spec-swap test with DebugLog on |
| 10 | Dead-but-not-destroyed player keeps icons anchored at the corpse; no scene/character callback resets registry or panel (OverheadRenderer.cs:96-104, SlotRegistry.cs:139) | Astra M (retention NOT PROVEN), GLM NOT PROVEN | NOT PROVEN | Tier B in-game death test |
| 11 | Active icon cluster re-centres when a cooldown ends, so remaining icons jump sideways (OverheadRenderer.cs:39-41) | Gemini M | design choice, not a defect | Tier B: boss decides fixed grid vs centred cluster |
| 12 | Home does not close the panel while a label field has focus (CooldownTrackerMod.cs:32) | Gemini MINOR | deliberate per code comment | Tier B: boss decides |
| 13 | Panel height is fixed (~757 × scale px with 7 slots); scale 2.0 exceeds 1080p, bottom controls unreachable (SettingsPanel.cs:73-88) | Astra MINOR | MINOR | Tier B: cap scale to screen height or scroll whole panel |
| 14 | Theme `_ready` latched before init completes; textures/font never destroyed; `_rows`/`_buf` keep old SlotData until next snapshot (Theme.cs:52, SettingsPanel.cs:20, OverheadRenderer.cs:17) | Astra MINOR | MINOR | Tier B |
| 15 | GUIContent / glyph-array allocations per GUI pass; duplicated field-column layout math (Widgets.cs:17, SettingsPanel.cs:199,246-250 vs 307-311, ButtonPicker.cs:89-106) | Astra MINOR, Kimi MINOR | MINOR | Tier C cleanup |
| 16 | Harmony targets have no capability gate / single compatibility diagnostic; field-read catches don't prove safety under stub/native mismatch | Astra NOT PROVEN | NOT PROVEN | Tier C |

Everything else: all four seats confirm the settings panel is self-drawn IMGUI with NO native template lookup — the "Toogle - Minion Health Bars" failure that killed the sibling mods on 1.4.7 does not apply here. All four confirm no unbounded growth (style caches are keyed by quantized sizes), 20 Hz tick, liveness pruning, and clean degradation on Harmony target loss (Tick re-reads `cooldownBarActive` as a fallback).

## Disagreements — named, not smoothed
- **Camera.main rank.** GLM ranks it MATERIAL on the claim that `Camera.main` is "deprecated post-Unity 2023 and throws on removal". That premise is false: Unity 6000 keeps `Camera.main` (cached internally since 2020.2). Conductor ranks MINOR; the cheap per-frame cache + catch is still worth doing.
- **Second action bar.** Three seats MATERIAL, Kimi NOT PROVEN. Nobody produced evidence that 1.4.7 instantiates a second `AbilityBarIcon` set. Conductor: ship the diagnostic (Tier A), not the structural fix (Tier B).
- **InputBlocker rank.** Gemini MINOR vs three MATERIAL. Conductor sides with MATERIAL because Terrible Zoom runs the same flag on this box tonight.
- **Home while typing / icon re-centering.** Gemini calls both bugs; the code comments call the first deliberate and the second is a layout choice. Neither is agreed; both go to the boss.
- **Copy-to-Mods target.** Gemini, Kimi and GLM say the csproj "needs" a copy target. Astra: "absent deployment targets are not a defect". Conductor: NO target is added — tonight's law is nothing deploys, and the sibling mods needed a `DeployToMods` condition precisely because they had one.
- **Verdicts.** All four: PATCH every file; nothing REBUILD or RETIRE (Astra's "REBUILD" on BuildInfo/csproj means "keep unchanged", per its own legend).

## Tier A → v1.0.1 (one builder ticket, one cross-vendor review, one in-hand pass) — TICKET-A.md
Findings 1, 2 (diagnostic only), 3, 4, 5, 6, 7 (document only), 8. All cheap, agreed by ≥2 seats or comment/doc-only, none changes what the player sees except the panel becoming recoverable from a bad cfg.
## Tier B (needs a decision or an in-game test)
2 (owner/instance identity — after the diagnostic shows whether a second bar exists), 9, 10 (spec-swap and death tests with DebugLog=true), 11, 12 (boss decisions), 13, 14, orphan-key deletion.
## Tier C (rebuilds)
15 (allocation hoisting / layout dedupe), 16 (Harmony capability gating), and a cross-mod input-lock ownership service shared with Terrible Zoom (Astra's real fix for finding 1; Tier A's `_applied` correction is the cheap version).
