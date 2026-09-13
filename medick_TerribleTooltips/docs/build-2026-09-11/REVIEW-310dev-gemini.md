# 🟢 Gemini — cross-vendor review of TICKET-3.1.0-dev, round 1 (2026-09-11 14:22)
conversationId ce7cbcd6-f749-4482-8125-a1b0db8bc846 · status SUCCESS · brain UNREPORTED · builder 🔵 Codex thread 01a0924e… (OpenAI) → cross-vendor holds · packet C:\Sync\Projects\tt-design-2026-09-11\310dev (DELTA-310dev.patch 239 lines)

# Code Review Report: Ticket 3.1.0-dev

**Reviewer:** 🟢 Gemini
**Builder:** 🔵 Codex (OpenAI)
**Scope:** `DELTA-310dev.patch` (6 files: `TooltipRecolor.cs`, `Prefs.cs`, `FilterRuleTooltip.cs`, `Colors.cs`, `CHANGELOG.md`, `README.md`)
**Verdict:** **APPROVE_WITH_NOTES**

### Findings Summary

#### BLOCKER Findings
*None.*

#### MATERIAL Findings

1. **Unbracketed Multi-Stat Affix Continuation Lines Miss GreaterAffix Tint**
   * **Location:** src/TooltipRecolor.cs:797-802
   * **Failure Mechanism:** In `ComposeUnbracketed`, line 790 processes line 0 via `ComposeCleanLine`, which applies `GreaterAffixTint` for Tier 6/7 affixes. However, for continuation lines (`nameLines[i]` where `i > 0`), the logic only checks `Prefs.NameColorMode.Value == AffixNameColorMode.TierColor`. It lacks a branch for `AffixNameColorMode.GreaterAffix`. Consequently, on unbracketed multi-stat affixes (e.g., hybrid or sealed multi-stat lines at T6/T7), continuation stat lines remain game-default white instead of following line 1's rule (violating Spec Item A).
   * **Repair Direction:** In `ComposeUnbracketed`, add a check for `AffixNameColorMode.GreaterAffix` when `(tier == 6 || tier == 7)` and `Prefs.TooltipTierColors.Value` is true, wrapping `nameLines[i]` in `<color={greaterAffixHex}>`.

2. **Missing Keys in `WarnOrphanedKeys()` Register Set Causes False Warnings**
   * **Location:** src/Prefs.cs:163-170
   * **Failure Mechanism:** `WarnOrphanedKeys()` checks `.cfg` lines against a hardcoded `HashSet<string>` (`registered`). The preference keys `PillProbe`, `PillTiers`, and `GreaterAffixTint` are initialized in `Prefs.Init()` (lines 126–130) but were omitted from `registered`. On startup, any `.cfg` file containing these valid keys triggers a false warning: `MelonLogger.Warning("unregistered preference keys in cfg: GreaterAffixTint, PillProbe, PillTiers")`.
   * **Repair Direction:** Add `"PillProbe"`, `"PillTiers"`, and `"GreaterAffixTint"` to the `registered` HashSet in `WarnOrphanedKeys()`.

#### MINOR Findings

3. **In-Memory Mutation of Config Value During Render & Unvalidated Continuation Usage**
   * **Location:** src/TooltipRecolor.cs:711, src/TooltipRecolor.cs:833-841
   * **Failure Mechanism:** In `ComposeCleanLine`, invalid hex strings trigger `Prefs.GreaterAffixTint.Value = greaterAffixHex;`. Mutating `Prefs.GreaterAffixTint.Value` inside a UI render/composition helper creates side effects in memory, causing any subsequent config save to overwrite minor user typos in `.cfg`. Additionally, in `Compose` line 711, continuation lines read `Prefs.GreaterAffixTint.Value` directly without validation.
   * **Repair Direction:** Extract a helper method `GetValidGreaterAffixTint()` that validates the hex regex, logs a warning once using a static flag (`s_tintWarned`), and returns the valid hex or default without writing back to `Prefs.GreaterAffixTint.Value`.

#### NOT PROVEN Findings
*None.*

### Detailed Check Item Verification
1. **Containment:** Verified. Exactly the 6 write-set files were touched. `src/PillProbe.cs` and all other source files remain untouched. In `src/TooltipRecolor.cs`, no hooks (`Patch_UpdateLayout`), scan-gate (`ShouldScan`), marker definitions (`Marker`), or re-entrancy latches (`s_relayouting`) were modified.
2. **Item A (GreaterAffix Tint):** Verified on bracketed path. T1–T5 receive no `<color>` tag; T6–T7 receive `<color=#C990FF>`; untiered (tier 0) receive no tag. `TierColor` and `GameDefault` remain unchanged. Continuation lines on bracketed multi-stat affixes follow line 1 (unbracketed path noted in Finding 1).
3. **Tint Validation:** Regex `^#[0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?$` correctly validates `#RRGGBB` and `#RRGGBBAA`. (In-memory pref write-back noted in Finding 3).
4. **Item B (Signal Unit & PlainText Default):** Verified. `PlainText` default outputs T4/C: `<color=#77ACFF>Tier 4</color><color=#8a8478>·</color><color=#77ACFF>C</color>  +50% Lightning Damage` · T7/A: `<color=#FF44FF>Tier 7</color><color=#8a8478>·</color><color=#FA9E3D>A</color>  <color=#C990FF>+100% Lightning Damage</color>`. Two spaces separate the signal unit from the sentence. `Badge` mode continues emitting `<mark>` chips.
5. **Item C (Sealed Label Dimming):** Verified. `PlainText` emits `<size=90%><color=#8a8478>Sealed</color></size>`. `Badge` path remains `<mark=#8a847866><color=#FFF6E6>Sealed</color></mark>`.
6. **Item D (Alt Details Dimming & Output Marker):** Verified. Alt detail lines (Range rows, Tier annotation tail, sealed tags) are wrapped in `<size=90%>`. The composer does not touch game-owned text outside its pipeline. The 4-ZWSP `Marker` is preserved on all output paths.
7. **Item E (Filter Rule Number):** Verified. `src/FilterRuleTooltip.cs:118` updated to `<size=120%><b><color=#FA9E3D>Rule#{displayNum}</color></b></size>`. No other lines in that file were touched.
8. **Item F (Dead Code Removal):** Verified. `Colors.BadgeTextColor` was deleted and grep confirms 0 callers remain in `src/`.
9. **Config ABI:** Verified. All key names, enum member names, and enum order are preserved. `AffixNameColorMode.GreaterAffix` (value 2) is appended last. Default values correctly updated to `SignalStyle.PlainText` and `AffixNameColorMode.GreaterAffix`.
10. **Existing User Regression Check:** Verified. Configs containing `TierColor` + `Badge` render identically to v3.0.2.
11. **IL2CPP/TMP Pitfalls:** Verified. Tags are correctly structured and balanced in strict LIFO order. No `<mark>` tags exist on the `PlainText` path.
12. **CHANGELOG / README:** Verified.

🟢 Gemini

---
Conductor: 2 MATERIAL + 1 MINOR accepted → REPAIR ROUND 1 (of 2) ordered 14:23 to the same Codex thread. Finding 2 is user-visible on every launch (false orphan-key warning); finding 1 is a correctness gap on unbracketed T6/T7 multi-stat lines; finding 3 is the write-back. Round-2 verdict appended below when it lands.

---
# 🟢 Gemini — ROUND 2 verification of the three repairs (2026-09-11 14:26)
conversationId 5de7022e-1627-400a-b6a0-50445b86bb5b · status SUCCESS · brain UNREPORTED · reviewed DELTA-310dev.patch (post-repair, 259 lines) + TooltipRecolor.cs + Prefs.cs

Verdict: **APPROVE**

All Round 1 findings and sanity checks have been verified against DELTA-310dev.patch, TooltipRecolor.cs, and Prefs.cs:

1. **Unbracketed Continuations**: `ComposeUnbracketed` continuation lines (TooltipRecolor.cs:798-804) now check `AffixNameColorMode.GreaterAffix` and `(tier == 6 || tier == 7)`, tinting T6/T7 via `GreaterAffixHex()`, leaving T0–T5 untagged, and leaving `TierColor`/`GameDefault` logic intact.
2. **Orphan Key Registration**: `"PillProbe"`, `"PillTiers"`, and `"GreaterAffixTint"` are registered in `WarnOrphanedKeys` (Prefs.cs:170).
3. **Hex Validator**: `GreaterAffixHex()` (TooltipRecolor.cs:829-843) validates `^#[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$`, warns once per session via `s_greaterAffixTintWarned`, returns fallback `Colors.GreaterAffixTintDefault` on failure, and does not mutate `Prefs.GreaterAffixTint.Value`. It is consumed in `ComposeCleanLine` (:908), bracketed continuations (:713), and `ComposeUnbracketed` (:803).
4. **Integrity Verification**: no changes to hook, scan-gate, marker, or latch code; RichText tags across all modified strings are strictly balanced; 4-ZWSP `Marker` is appended across all composition output paths.

🟢 Gemini

---
Conductor: repair rounds used 1 of 2. Final staged DLL = 83,456 B, md5 5670b8181f54bcd757b53929679ed71c (BUILD-310dev.md). Deploy = team lead; in-hand = Andrew.
