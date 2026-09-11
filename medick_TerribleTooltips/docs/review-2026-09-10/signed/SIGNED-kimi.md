# 🟣➤🌙 Kimi (kimi-k3-high via Cursor pool, 💸 CREDITS 96977 in / 7190 out) — top-to-bottom review (2026-09-10, read-only, code-quality lens)
sessionId ff0004fc-35f4-4998-a40f-7f48b31b9ee4 · manifest hashes UNAVAILABLE (seat could not run a hash tool; not fabricated)

DONE_WITH_CONCERNS

| Rank | Claim | Failure mechanism | Verify-by / repair |
|---|---|---|---|
| MATERIAL | Dead public method Colors.BadgeTextColor + stale design comment | Colors.cs:66-76, zero callers; its comment describes an abandoned luminance-flip design; shipped badges use fixed Ink #FFF6E6 at TooltipRecolor.cs:731. A maintainer "fixing" contrast edits the dead function. | Delete or wire in; needs a decision on which design is canon. |
| MATERIAL | KG-bracket format defined by 4 regexes in 2 files | AffixInjector.cs:79-81 vs TooltipRecolor.cs:270-285 — same grammar, subtly different expressions. Writer/reader drift → lines silently pass through uncomposed (the raw-[1B] symptom class). | One shared regex set (BracketFormat helper). Mechanical. |
| MATERIAL | Filter-match logic duplicated; CheckFilter missing lower-bound check | TerribleTooltipsApi.cs:34-36 checks only `>= Count`; matchingRuleNumber > Count → negative index → IndexOutOfRange swallowed (:43). FilterRuleTooltip.cs:162-163 has the correct `idx >= 0`; :184 repeats the math a third time. | Extract one TryMatchRule; both callers use it. Real latent-defect fix. |
| MINOR | s_suppressedRanges never pruned while ranges pinned on | Prune lives inside `!DeepRange` (TooltipRecolor.cs:179); with AlwaysShowRanges=true entries are never reaped. Bounded by pool churn. | Move the prune outside the gate. |
| MINOR | Grade threshold ladder duplicated | 50/70/80/95 twice in Colors.cs:40-59; roll×100 rounding redone in AffixInjector.cs:35 and GroundLabels.cs:162. | Derive colour from letter. |
| MINOR | Swallowed catch{} hiding state corruption | DriveNativeRangeSwitch (TooltipRecolor.cs:142): a failed restore leaves the native range mode flipped with no log. Prefs.Save (Prefs.cs:126): silent non-persist. FilterRuleTooltip.MonitorUpdate outer catch (:139): a per-frame always-throw is invisible. | Dbg.Log in all three. |
| MINOR | Stale docs/config vs code | SPEC.md:205 says Range widgets hidden via SetActive — code empties text. cfg has orphan keys GroundLabels="FilterOnly" and SignalText="Spelled" with no Prefs entries. ARCHAEOLOGY.md:197 still asks to audit RollColor (removed). | Doc/cfg sweep. |
| MINOR | Alt-key poll + 'requires' scan duplicated | TooltipRecolor.cs:155, GroundLabels.cs:49,134; FilterRuleTooltip.cs:96-107 and :125-137. | Tiny shared helpers. Low value. |
| NOT PROVEN | Version string duplication BuildInfo.cs:13 vs csproj:8 | No drift today. Process hazard. | Read assembly version at runtime or build-time check. |

Top three cleanups (value/risk): 1. Delete Colors.BadgeTextColor + reconcile its comment. 2. Unify the KG-bracket regexes into one definition. 3. Extract one filter-match helper for CheckFilter + TryGetMatchedRule (fixes the missing `< 0` guard in the public ABI).

🟣➤🌙 Kimi
