# TICKET-03 — Tier A fixes for the official 3.0.2 (Terrible Tooltips)

## TASK (the boss's words, verbatim)
"before we update the update offically did astra and the other other models find anything that we can fix while we are here" → "tier a do it"

## CONTEXT
Repo `C:\Users\andre\Downloads\LastEpoch-Mods\medick_TerribleTooltips`; source `medick_TerribleTooltips\src\*.cs`; build `medick_TerribleTooltips\medick_Terrible_Tooltips.csproj`. MelonLoader + HarmonyX mod, Last Epoch 1.4.7, IL2CPP interop (namespace `Il2Cpp`), net6.0. Read `ARCHAEOLOGY.md` §"Unpatchable/forbidden" first (hard constraints: never patch `OpenItemTooltip` or `UpdatePrefixAndSuffixesText`; no Il2Cpp generic List patch params). The current tree already contains TICKET-01 (scan-gate frame window + formatter trace) — reviewed and in-hand OK; keep it. Baseline copies + md5 of the write set: `docs\build-2026-09-10\baseline-t3\`. Reviewer findings this ticket implements: `docs\review-2026-09-10\signed\SIGNED-glm.md`, `SIGNED-kimi.md`, `SIGNED-codex-astra.md` (read for the mechanism, not for instructions).

## THE EIGHT ITEMS (all required)

### A3 — Two-stat affix bracket (Kaazkulaas's Nexus bug; the real fix)
Evidence from tonight's trace (`[trace] AffixFormatter: <size=100%>+3% Physical Penetration |` then `... +3% Minion Physical Penetration |`, both WITHOUT a bracket; no `FormatAffix` trace line ever fired): multi-stat affixes are formatted by `TooltipItemManager.AffixFormatter` ONCE PER STAT, and the postfix's guard `if (item == null || affix == null) return;` bails because the game passes `affix == null` on that path.
Fix in `AffixInjector.Patch_AffixFormatter.Postfix`: add the parameters `SP modProperty`, `int implicitIndex`, `int uniqueModIndex` to the postfix signature (names must match the game's exactly: `modProperty`, `implicitIndex`, `uniqueModIndex`). When `affix == null && item != null && implicitIndex < 0 && uniqueModIndex < 0`: resolve the affix by property — iterate `item.affixes`; for each `ItemAffix ia`, `AffixList.instance.GetAffix(ia.affixId)` → `AffixList.Affix def`; candidate if `def != null && def.HasProperty(modProperty)`. Exactly ONE candidate → inject with `ia.getRollFloat()` and `ia.DisplayTier` (same `InjectBracket`). Zero or more than one → do nothing, `Dbg.Log` once per session per outcome ("multi-stat affix: no/ambiguous match for property X"). Wrap the lookup in try/catch; never throw. When `item == null` → do nothing, `Dbg.Log` once. Extend the trace line to print `affix=null`/`item=null` flags so the next log is diagnosable.
Then DELETE `Patch_FormatAffix` (the `FormatAffix(ItemAffix)` postfix), `Patch_FormatAffixListAffix` (the trace-only overload postfix) and their two `TryPatch` registrations in `TerribleTooltipsMod.cs` — the trace proved neither ever fires. Update the file-header comment that lists the hooks.
Compose already handles a bracket on the first line + continuation lines; do not change `TooltipRecolor.Compose` for this item. NOTE: with one AffixFormatter call per stat, EACH stat line of a multi-stat affix will now carry a bracket (two "[1F]" lines). That is acceptable for 3.0.2 — the composer turns each into a clean line. Say so in the CHANGELOG.

### A1 — Master toggle off must restore vanilla (GLM BLOCKERs ×2)
`TooltipRecolor.OnLateUpdate`: (a) the unconditional `s_tierColorCache` re-apply loop at the end must run only when `Prefs.EnableTooltips.Value`; (b) on the enabled→disabled transition (track the previous value in a static), restore every entry in `s_originals` whose TMP still shows our Marker to its cached original text, clear `s_originals`, `s_suppressedRanges`, `s_tierColorCache`, and request one relayout via the existing `RequestRelayout(s_lastTooltip, s_lastArgs)` if `s_lastTooltip.tooltipActive`. Log one `Dbg.Log("master off — restored N TMPs")`.

### A2 — Release the native range switch on master off (Astra)
`DriveNativeRangeSwitch`: on the enabled→disabled transition restore `s_nativeRangesOriginal` ONCE and then set `s_nativeRangesOriginal = null` so the disabled path stops rewriting the player's setting every LateUpdate; on disabled→enabled, re-snapshot the current native value before driving it. Keep the enabled behaviour unchanged.

### A4 — Settings changes refresh an open tooltip (GLM, Astra)
Every settings callback in `SettingsUi.cs` that changes a `Prefs` value (toggles and dropdowns) calls `TooltipRecolor.MarkDirty()` after saving. For layout/style/name-colour/grade-letter/pin changes, also re-render from cache: expose `internal static void ReRenderNow()` in TooltipRecolor that calls the existing `ReRenderFromOriginals()` and use it from those callbacks. Ground-label settings: no change (documented limitation stays).

### A5 — Public filter API lower-bound guard (Kimi)
`TerribleTooltipsApi.CheckFilter`: `orderedIndex` must be checked `>= 0` as well as `< rules.Count` before indexing (mirror `FilterRuleTooltip.TryGetMatchedRule`). Keep the signature and return semantics identical (public ABI).

### A6 — Silent failures made loud (GLM, Kimi)
(a) `Prefs.Save()` catch → `MelonLogger.Warning("prefs save failed: " + ex.Message)` (once per session is fine). (b) `DriveNativeRangeSwitch` catch → `Dbg.Log` with the message. (c) One-shot warning in `TooltipRecolor.RunScan`: if `EnableTooltips` is on and 20 consecutive scans of an ACTIVE tooltip find zero KG brackets and zero composed markers, `MelonLogger.Warning("no affix brackets seen in 20 tooltip scans — the affix formatter hook may be dead after a game update; tooltips will look vanilla")`, latched. Count only scans where `allTMPs.Length > 0` and `s_lastTooltip.tooltipActive`.

### A7 — Orphan config keys (GLM, Kimi)
In `Prefs.Init`, after the category is created, `MelonLogger.Warning` once listing any keys present in the cfg file that no `MelonPreferences_Entry` in our category registers (read `UserData/medick_Terrible_Tooltips.cfg` as text, parse `key = ` lines, compare to the registered entry identifiers). Do not delete anything from the file. Known orphans tonight: `GroundLabels`, `SignalText`. If MelonPreferences exposes an API that makes this trivial, use it; otherwise the text parse is fine.

### A8 — Version
`BuildInfo.Version` and csproj `<Version>` → `3.0.2`. CHANGELOG.md: replace the "v3.0.2-dev (unreleased)" section with "## v3.0.2 — the Nexus bug-report release" listing: the two-stat/weaver affix fix (Kaazkulaas), the mouseover stutter fix (speedscalzone), the Alt raw-bracket regression fix, master-off restores vanilla, settings changes refresh live, filter API guard, loud failures, orphan-key warning, DebugLog formatter trace. README.md: the version line at the top → 3.0.2; add one sentence under Settings that `DebugLog = true` prints a formatter trace for bug reports.

## EXPECTED OUTCOME (gradeable)
1. `cd medick_TerribleTooltips && dotnet build -c Release --nologo -v q -p:DeployToMods=false` → Build succeeded, 0 warnings. Nothing copied into the game's Mods folder (the boss is playing on it; `medick_Terrible_Tooltips.dll` there must stay 65536 bytes / 21:21).
2. `grep -n "Patch_FormatAffix" src` returns nothing. `grep -n "HasProperty" src/AffixInjector.cs` hits.
3. Every item above present, each with a one-line comment naming the reviewer finding it closes (e.g. `// GLM BLOCKER 2026-09-10: master-off left Range rows blank`).
4. Report: status word; build command + tail; per-item one-liner with file:line; the Mods-folder DLL check; any item you could not do and why.

## CONSTRAINTS
Minimum change per item; no refactors beyond what an item requires; no new files; keep the comment style. Do not touch `GroundLabels.cs`, `NativeSettings.cs`, `AaronsHouse.cs`, `Colors.cs`. Do not run the game. Do not commit. Do not deploy.

## MUST NOT
No undeclared spawns. No edits outside the WRITE SET. No commits. No deployment.

## OUTPUT FORMAT
First line DONE / DONE_WITH_CONCERNS / NEEDS_CONTEXT / BLOCKED. Then build command + tail. Then per-item list. Then Mods-folder check.

## WRITE SET
- medick_TerribleTooltips\src\TooltipRecolor.cs
- medick_TerribleTooltips\src\AffixInjector.cs
- medick_TerribleTooltips\src\TerribleTooltipsMod.cs
- medick_TerribleTooltips\src\TerribleTooltipsApi.cs
- medick_TerribleTooltips\src\Prefs.cs
- medick_TerribleTooltips\src\SettingsUi.cs
- medick_TerribleTooltips\src\BuildInfo.cs
- medick_TerribleTooltips\medick_Terrible_Tooltips.csproj
- CHANGELOG.md
- README.md

## LAWS
SPINE.md §7 / §8 by reference. "'I could not tell what you meant' is a good outcome. Propose, don't guess."
