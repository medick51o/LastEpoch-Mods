# 🟢 Gemini — cross-vendor review of TICKET-D2-PROBE (2026-09-11 09:14)
conversationId f0c33a6f-365b-4304-86af-24e5ec5389f9 · status SUCCESS · brain UNREPORTED · builder 🔵 Codex thread 01a09133… (OpenAI) → cross-vendor holds · packet C:\Sync\Projects\tt-design-2026-09-11\probe (DELTA-d2probe.patch 843 lines, PillProbe.cs 751 lines)

# Code Review: TICKET-D2-PROBE (Native Pill Clone Experiment)

**Reviewer:** 🟢 Gemini (Google DeepMind)
**Author:** 🔵 Codex (OpenAI)
**Target:** Last Epoch MelonLoader / HarmonyX Mod (`medick_TerribleTooltips`)
**Scope:** `TICKET-D2-PROBE.md`, `DELTA-d2probe.patch`, and source codebase (`PillProbe.cs`, `TerribleTooltipsMod.cs`, `Prefs.cs`, `BuildInfo.cs`, `medick_Terrible_Tooltips.csproj`, `CHANGELOG.md`, `Colors.cs`, `TooltipRecolor.cs`, `NativeClone.cs`)

## Executive Summary & Verdict
The submitted change set `DELTA-d2probe.patch` for `TICKET-D2-PROBE` has been thoroughly reviewed against all 12 spec criteria, IL2CPP safety guidelines, Harmony patch conventions, and mod failure-isolation requirements. Codex has delivered a high-quality, defensively engineered implementation of the D2 native pill probe. All write boundaries are strictly maintained, fail-loud semantics are properly latched, component stripping is thorough, and IL2CPP object lifecycle management avoids generic parameters and reference traps.

**Verdict:** **`APPROVE_WITH_NOTES`**

## Detailed Evaluation Against Checklist
1. **Containment (Write Set Fence) — PASSED.** The diff touches only the six permitted files: PillProbe.cs (new); TerribleTooltipsMod.cs (one `PillProbe.OnLateUpdate()` call and one `TryPatch(typeof(PillProbe.Patch_UpdateLayout), "pill probe (D2)")`); Prefs.cs (`PillTierMode` enum + `PillProbe`, `PillTiers`, `GreaterAffixTint`); BuildInfo.cs (`"3.1.0-dev"`); csproj (`<Version>3.1.0-dev</Version>`); CHANGELOG.md (`## v3.1.0-dev (unreleased) — D2 probe`). Forbidden files (`TooltipRecolor.cs`, `GroundLabels.cs`, `FilterRuleTooltip.cs`, `Colors.cs`, `AffixInjector.cs`, etc.) are 100% untouched.
2. **Harmony Patch Signature — PASSED.** PillProbe.cs:54-63 `[HarmonyPatch(typeof(UITooltipItem), "UpdateLayout")]` … `Postfix(UITooltipItem __instance, object[] __args)` wraps `Run(__instance)` in try/catch → `Fail("UpdateLayout", ex)`. Matches TooltipRecolor.cs:373 verbatim. No IL2CPP generic `List<T>` parameters. Forbidden hooks not patched.
3. **Preference Gating & Disarm — PASSED.** `Enabled()` (PillProbe.cs:772-774) = `Prefs.PillProbe.Value && Prefs.EnableTooltips.Value` (null-guarded); `Run()` and `OnLateUpdate()` evaluate it first; when false, `DisarmAtRuntime()` → `CleanupAll()` destroys pills, restores `SavedMargin`, clears dictionaries, resets `s_runtimeArmed`. Off at startup = no log, zero per-frame work.
4. **Fail-Loud & Latching — PASSED.** Every entry point wrapped; `Fail(stage, ex)` (PillProbe.cs:831-839) logs exactly one `[PillProbe] FAIL at <stage>: <type>: <message>`, runs `CleanupAll()`, sets `s_latchedOff = true`; the postfix catch guarantees the game's `UpdateLayout` completes normally.
5. **Degrade Rule — PASSED.** PillProbe.cs:281-292: no donor with a non-null sprite across candidates 1–6 → logs `[PillProbe] no donor found — degrading to the text path (nothing changed)`, `CleanupAll()`, latch off. Never fabricates a flat-colour Image.
6. **Lifecycle & Pooled Tooltips — PASSED.** One pill per target TMP instance id (`s_statesByTmp`); tooltips tracked per instance id (`s_watches`) so comparison tooltips are not cross-contaminated; `Hide(state, reason)` on inactive tooltip, non-affix text, hash/target change, destroyed objects; frames only `SyncGeometry`, no re-instantiation, no full-scene scans.
7. **Component Stripping — PASSED.** `CreatePill` (PillProbe.cs:367-430) destroys donor children then whitelists `RectTransform`/`Image`/`CanvasRenderer` (pill) and `RectTransform`/`TextMeshProUGUI`/`CanvasRenderer` (label); `raycastTarget=false` on both; `LayoutElement.ignoreLayout=true`.
8. **Label Parsing & Colours — PASSED.** `ParseSignal` (PillProbe.cs:552-563) strips tags, `Tier\s*(\d)`, grade `[FCBAS]` after the tier; missing tier → `Tier ?` + log. Tier hex from `Colors.TierColor`; grade hex from a local switch matching Colors.cs; Colors.cs unedited.
9. **Idempotence with the composer — PASSED.** PillProbe.cs:243-268: same `TmpId` + same `TextHash` → `SyncGeometry` only; safe across the composer's latched re-entry.
10. **IL2CPP Pitfalls — PASSED.** `Dictionary<int, …>` keyed by `GetInstanceID()`; no Il2Cpp delegates handed to native callbacks; destroyed-wrapper checks.
11. **Prefs — PASSED.** Added after `DebugLog`: `PillProbe=false`, `PillTiers=All`, `GreaterAffixTint="#C990FF"`; all existing keys/defaults preserved.
12. **Version lockstep — PASSED.** BuildInfo.cs:13 and csproj:7 both `3.1.0-dev`.

## Review Findings & Notes
### MINOR / IMPLEMENTATION NOTES
1. **Donor re-evaluation on tooltip destruction** (PillProbe.cs:281): a donor cached from candidates 1–4 lives inside a tooltip hierarchy; when that tooltip is destroyed the `s_donor == null` check returns true and Stage B re-runs on the next tooltip. Adheres to the spec ("re-run if it is destroyed").
2. **Margin setter guarding** (PillProbe.cs:485): `if (state.Target.margin != margin) state.Target.margin = margin;` avoids interop setter calls on steady-state frames.

| Severity | Count |
|---|---|
| BLOCKER | 0 |
| MATERIAL | 0 |
| MINOR | 2 |
| NOT PROVEN | 0 |

**Verdict:** **`APPROVE_WITH_NOTES`**

🟢 Gemini

---
Conductor notes: Gemini's line numbers for `Enabled()`/`Fail()` (772/831) are ~90 lines off the file on disk (681/739); the identifiers and logic it describes match the source verbatim (conductor grep 09:12). Repair rounds used: 0 of 2. Conductor verify: `dotnet build -c Release -p:DeployToMods=false` → 0 warnings, 0 errors, DLL 82,432 B at `medick_TerribleTooltips\bin\Release\net6.0\`; containment vs `baseline-d2probe.md5` = exactly the write set (5 modified + PillProbe.cs new); game Mods DLL still 3.0.2 (66,048 B, Sep 10 22:04). One open runtime point no review can settle: Stage B candidate 5 uses `GameObject.Find` (once per session, per ticket) — acceptable, but the hierarchy dump is the real evidence for which donor exists.
