# 🟣➤🌙 Kimi — Cursor pool, kimi-k3-high, code-quality lens — 2026-09-11 (verbatim; sessionId a4907b31-9218-491e-b6b1-15355f2578a2 · 53457 in / 7247 out, CREDITS)

Now computing MD5s of all files read:DONE_WITH_CONCERNS

**Findings (code-quality lens; all six questions answered below)**

| Rank | Claim | Mechanism | Verify-by / repair | Path:line |
|---|---|---|---|---|
| MATERIAL | InputBlocker can stomp a sibling mod's input lock | `_applied = true` is set even when `forceDisableInput` was already true (set by Terrible Zoom); on panel close the `else if (_applied)` branch writes `false`, clearing a lock we never created | Open zoom-lock, open+close panel, check zoom lock; repair: only set `_applied` when we actually flipped the flag | src/InputBlocker.cs:28-42 |
| MINOR | Header comment contradicts the code it describes | Comment says "only written when our desired state changes", but the re-assert loop writes every frame while locking (added in the fable pass; CHANGELOG documents it, comment doesn't) | Read both; repair: rewrite header to describe re-assert-on-change semantics | src/InputBlocker.cs:10-11 vs 28-34 |
| MINOR | Stale "v5"/"v4.x" comments in a mod versioned 1.0.0 | Version reset to 1.0.0 (BuildInfo.cs:13) but comments narrate v4/v5 history; next maintainer reads wrong lineage | grep "v5"/"v4"; repair: retcon to "1.0.0 (was v4.x)" or delete | src/InputDetection.cs:13,146-148; src/InputBlocker.cs:8-9; src/UI/OverheadRenderer.cs:10; src/UI/Theme.cs:254 |
| MINOR | 12 orphan config keys in live cfg | ShowWindow, ShowOverhead, WindowScale, WindowX/Y, IconAlpha, IconSize, OverheadY, EnabledKeys, Controller, SlotLabel0–5 registered nowhere (Prefs.Init registers only Alpha…ButtonBadges + SlotLabel{Kb,Xb,Ps}n) | Diff cfg keys vs Prefs.cs:42-62; harmless (MelonPreferences ignores unknowns); repair: one-time prune/migration note | cooldown-cfg-excerpt.txt:2-11,20-25 |
| MINOR | `Camera.main` called per OnGUI event | OnGUI fires 2–4×/frame (Layout/Repaint/mouse); each call is a native FindGameObjectsWithTag lookup | Profile; repair: cache per-frame in OnUpdate | src/UI/OverheadRenderer.cs:24 |
| MINOR | Duplicated layout math can drift | Label-field column geometry computed twice (field-band vs row draw); picker Xbox/PS blocks differ only in data | Compare; repair: share a row-layout helper / data-drive picker groups | src/UI/SettingsPanel.cs:246-250 vs 307-311; src/UI/ButtonPicker.cs:89-106 |
| NOT PROVEN | Style-cache growth | `_labels`/`_buttons`/`_fields` keyed by quantized int sizes; sc and icon size are clamped, so key space is finite (~dozens). No unbounded growth shown | Heap snapshot after long session; no repair needed | src/UI/Theme.cs:45-47,160-185 |
| NOT PROVEN | Cross-bar cooldown confusion | `SetCooldown` matches only `abilityNumber`; a second `AbilityBarIcon` set with the same index would flip our slot's state (Register is last-wins per index). No second bar known to exist in LE 1.4.7 | Needs a second action-bar instance to repro | src/SlotRegistry.cs:64-67,120-126 |

**Q1 — Patch resilience.** All three patches are postfixes wrapped in try/catch at the registry boundary (SlotRegistry.cs:50-72,117-127); if EHG renames `AbilityBarIcon` fields or the methods, Harmony fails to patch → MelonLoader logs an error at load, mod degrades to "no slots" (panel shows "Waiting for your skill bar", SettingsPanel.cs:170-173) — clean degradation, no crash, no spam. `EpochInputManager` write is fully guarded (InputBlocker.cs:19-44) → silent no-op if the manager vanishes. `Camera.main`/`Text`/`Image` walks are null- and try/catch-guarded (OverheadRenderer.cs:25,92-107; InputDetection.cs:153-161). Terrible Zoom conflict: see MATERIAL finding above — re-assert direction is fine, release direction is not.

**Q2 — Display correctness.** Icon staleness after skill swap is handled (Tick re-reads sprite every 50 ms, SlotRegistry.cs:146-148). Ghost icons across zone/login are handled by destroyed-object pruning (138-141) and the no-anchor-no-draw rule (OverheadRenderer.cs:32). Duplicate slots prevented by RemoveAll on register (65). Wrong-fill-from-other-skill: NOT PROVEN (table). Fill hysteresis (154-156) is sound.

**Q3 — Per-frame cost.** OnUpdate: InputTracker.Update (joystick-name array alloc every 5 s; 20-iteration GetKeyDown loop only when anyKeyDown), one Il2Cpp field read in InputBlocker.Apply, 20 Hz tick. OnGUI (per event): Theme.Ensure flag check, PreInput, Camera.main (MINOR above), Snapshot under lock, zero per-frame label allocs (cached RawLabel/DisplayLabel, SlotRegistry.cs:20-27,182-188). GUIContent allocs in CalcSize each event (SettingsPanel.cs:199; Widgets.cs:17,24) — trivial. Nothing grows without bound.

**Q4 — Config resilience.** Panel is fully self-drawn IMGUI — no "Toogle - Minion Health Bars"-style template lookup exists anywhere; immune to the sibling mods' 1.4.7 failure. Bad values: InputMode=7 falls to auto (InputDetection.cs:119-126); Size/Alpha/MenuScale/offsets clamped at draw (OverheadRenderer.cs:34-36; SettingsPanel.cs:54); GUI.HorizontalSlider self-clamps. NaN PanelX/Y would defeat the clamp (NaN comparisons fail) and park the panel off-screen with no in-game recovery — MINOR, repair: `float.IsNaN` guard at SettingsPanel.cs:34-36. Orphan keys: harmless, worth pruning (table).

**Q5 — Best value/risk cleanups.** (1) Rewrite the contradictory InputBlocker header — zero risk, prevents a future maintainer "fixing" the re-assert back into the bug. (2) Retcon/delete v4/v5 narrative comments — zero risk. (3) Prune the 12 orphan cfg keys via a one-time migration note — near-zero risk, real hygiene. (Bonus: extract the duplicated row-layout math.)

**Q6 — Verdicts.** CooldownTrackerMod.cs PATCH (fine as-is). Patches.cs PATCH. SlotRegistry.cs PATCH (cross-bar guard optional). InputDetection.cs PATCH (comments). InputBlocker.cs PATCH (the `_applied` fix + comment). Prefs.cs PATCH (orphan-key migration). OverheadRenderer.cs PATCH (cache Camera.main). SettingsPanel.cs PATCH (NaN guard, dedupe layout math). Theme.cs / Widgets.cs / ButtonPicker.cs / UiState.cs / BuildInfo.cs PATCH (comments/data-drive only). csproj PATCH (add `<Version>` + copy target). Nothing merits REBUILD or RETIRE.

**Manifest (all files read in full; MD5s not computed — this read-only session blocked every shell invocation, including `md5sum`; hashes NOT PROVEN rather than fabricated):**
BRIEF.md, README.md, CHANGELOG.md, melonloader-lines.txt, cooldown-cfg-excerpt.txt, medick_CooldownTracker.csproj, src/BuildInfo.cs, src/CooldownTrackerMod.cs, src/CooldownTrackerMod.Patches.cs, src/SlotRegistry.cs, src/InputDetection.cs, src/InputBlocker.cs, src/Prefs.cs, src/UI/Theme.cs, src/UI/Widgets.cs, src/UI/ButtonPicker.cs, src/UI/SettingsPanel.cs, src/UI/OverheadRenderer.cs, src/UI/UiState.cs — md5: unavailable (shell rejected ×5).

🟣➤🌙 Kimi

---
🟣➤🌙 [wmw-cursor] 💸 Moonshot · kimi-k3-high
   sessionId: a4907b31-9218-491e-b6b1-15355f2578a2 · meter: CREDITS · 53457 in / 7247 out
🟣➤ 💸 —— THIS CALL SPENT MONEY —— 💸 🟣➤
   third-party CREDIT pool — billed at API prices
