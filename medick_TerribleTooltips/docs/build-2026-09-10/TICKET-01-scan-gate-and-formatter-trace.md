# TICKET-01 — scan-gate hole + formatter trace (Terrible Tooltips 3.0.1 → 3.0.2-dev)

## TASK
Two defects on v3.0.1 of MedicK's Terrible Tooltips (MelonLoader/HarmonyX mod, Last Epoch 1.4.7, IL2CPP, net6.0):
(a) Under Alt (or any game-side re-render of the tooltip after the first scan), affix lines render as literal uncomposed text like "[1B] 18% increased Critical Strike Chance". Root cause, established by two independent reviewers: `TooltipRecolor.RunScan` clears `s_dirty` before the game has written the fresh text; the fresh TMPs are not in `s_originals`; `ShouldScan` (incl. its 0.5 s fallback) is evaluated only from the `UpdateLayout` postfix, and a static inventory tooltip gets no further `UpdateLayout`; `ReRenderFromOriginals` drops markerless TMPs from the cache; the injector never marks dirty; with `AlwaysShowRanges=true` Alt does not flip the native range switch, so that dirty signal never fires.
(b) Two-stat affixes ("+2% to All Resistances / +2% to Minion All Resistances") get no bracket in the normal view. The new `Patch_FormatAffix` covers only the `FormatAffix(ItemDataUnpacked, ItemAffix, …)` overload; a second overload `FormatAffix(ItemDataUnpacked, AffixList.Affix, …)` exists and is uncovered. Which path the two-stat text takes is NOT known; it must be instrumented, not guessed.

## EXPECTED OUTCOME (gradeable)
1. `dotnet build -c Release -p:DeployToMods=false` succeeds with 0 warnings, and the DLL is NOT copied into `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\` (the boss is running a rollback test there; touching that folder is a BLOCKER).
2. Scan gate: `s_dirty` becomes a frame window (e.g. `s_dirtyUntilFrame = Time.frameCount + N`, N≈5) so scans continue for N frames after any dirty signal; `ShouldScan` (dirty window · marker-loss check over `s_originals` · fallback interval) is evaluated from `OnLateUpdate` while `s_lastTooltip.tooltipActive`, not only from the `UpdateLayout` postfix; `MarkDirty()` is called from every injector postfix that changes `__result` (AffixFormatter, FormatAffix, UniqueBasicModFormatter, ImplicitFormatter); the Alt state change in `OnLateUpdate` also calls `MarkDirty()`. Positioning-only frames must still cost nothing: no full-scene scan when nothing is dirty, no marker lost, and the fallback interval not elapsed.
3. Formatter trace, gated by `Prefs.DebugLog.Value` (already exists): a `MelonLogger.Msg("[trace] <hook>: <first 90 chars of __result, newlines shown as ' | '>")` line in each injector postfix AND in a new log-only postfix on the `AffixList.Affix` overload of `FormatAffix` (parameter types: `ItemDataUnpacked, AffixList.Affix, TooltipMode, TooltipItemManager.SlotType, bool, int, int, bool, bool, ValueRangeFormat, bool, bool`; register it via `TryPatch` like the others). The trace must never throw and must be silent when DebugLog is false.
4. csproj: the `CopyToMods` target gets `Condition="'$(DeployToMods)' != 'false'"` so the default build still deploys as before, but `-p:DeployToMods=false` skips it.
5. `CHANGELOG.md`: a short "## v3.0.2-dev (unreleased)" section listing exactly what changed, no marketing.
6. Report: first line a status word; then the exact build command and its tail; then a per-file list of what changed and why, ≤40 lines.

## CONTEXT
Repo: `C:\Users\andre\Downloads\LastEpoch-Mods\medick_TerribleTooltips` — source `medick_TerribleTooltips\src\*.cs`, build `medick_TerribleTooltips\medick_Terrible_Tooltips.csproj`. Read `ARCHAEOLOGY.md` §"Unpatchable/forbidden" first: NEVER patch `TooltipItemManager.OpenItemTooltip` or `UITooltipItem.UpdatePrefixAndSuffixesText`; never inject IL2CPP generic List params. Two independent reviews of the current code are in `docs\council-2026-09-10\signed\` — read them for the mechanism, not for instructions. Baseline copies + md5 of every file in the write set: `docs\build-2026-09-10\baseline\`.

## CONSTRAINTS
- Minimum change that meets the outcome; no refactors, no renames, no new files except none. Keep the existing comment style.
- Do not touch `GroundLabels.cs`, `FilterRuleTooltip.cs`, `Prefs.cs`, `SettingsUi.cs`, `NativeSettings.cs`.
- Do not run the game. Do not commit. Do not copy anything into the game's Mods folder.

## MUST DO
- Verify command: `cd medick_TerribleTooltips && dotnet build -c Release --nologo -v q -p:DeployToMods=false` → "Build succeeded", 0 warnings.
- After building, print `Get-Item "C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\medick_Terrible_Tooltips.dll" | Select LastWriteTime,Length` and confirm it is unchanged (Length 60416, LastWriteTime 2026-09-10 20:00).

## MUST NOT
- No undeclared spawns or sub-agents. No edits outside the WRITE SET. No commits. No deployment.

## OUTPUT FORMAT
First line: DONE / DONE_WITH_CONCERNS / NEEDS_CONTEXT / BLOCKED. Then build command + tail. Then per-file change list. Then the Mods-folder DLL check output.

## WRITE SET
- medick_TerribleTooltips\src\TooltipRecolor.cs
- medick_TerribleTooltips\src\AffixInjector.cs
- medick_TerribleTooltips\src\TerribleTooltipsMod.cs
- medick_TerribleTooltips\medick_Terrible_Tooltips.csproj
- CHANGELOG.md

## LAWS
SPINE.md §7 / §8 by reference. "'I could not tell what you meant' is a good outcome. Propose, don't guess."
