# Independent Cross-Vendor Code Review: beta3 Scope & Health Scan Changes

**Reviewer:** Gemini (Google Seat)  
**Target Version:** v3.1.1-beta3  
**Repository State:** `C:\Sync\Projects\tt-review-2026-09-18`  
**Verdict:** **APPROVE**  

---

## Executive Summary

The changes in `v3.1.1-beta3` replace the previous per-layout whole-scene `FindObjectsOfType<TextMeshProUGUI>()` scan with a scoped collection over active tooltip hierarchies, explicitly including comparison panels (both content-bound and secondary instances) and validating affix/range sibling relationships. Rate limiting (0.5s fallback latch) bounds emergency whole-scene fallbacks, and formatter health tracking (`TrackFormatterHealth`) has been refined to eliminate false warnings on untiered items.

All seven evaluation criteria pass with zero BLOCKER, MATERIAL, or MINOR findings. All protected ABI/configuration/patch invariants remain intact.

---

## Detailed Evaluation by Criteria

### 1. Comparison Tooltip Scope
- **Rank:** NOT PROVEN (No defect found)
- **Analysis:** `Patch_UpdateLayout.Postfix` registers every active `UITooltipItem` instance into `s_tooltips` via `RememberTooltip`. `CollectTooltipTMPs` enumerates `s_tooltips.Values` and includes `ui.transform`, `ui.content`, `ui.compareContent`, `ui.blessingContent`, `ui.blessingCompareContent`, and `ui.resonanceContent` as roots. In addition, `CollectTooltipTMPs` validates active affix TMPs (e.g. `comparePrefixes`, `compareSuffixes`, `compareSealedAffix`) via `RequireTMP`. If an active comparison affix TMP resides outside the panel roots, `RequireTMP` throws an `InvalidOperationException`, safely triggering the whole-scene fallback. Whether comparison panels use the primary `UITooltipItem`'s `compareContent` or a secondary `UITooltipItem` instance, the TMPs are reached.

### 2. Grade Colours & Sibling Scoping
- **Rank:** NOT PROVEN (No defect found)
- **Analysis:** `CollectTooltipTMPs` gathers all `TextMeshProUGUI` descendants under active panel roots using `root.GetComponentsInChildren<TextMeshProUGUI>(true)`. Affix TMPs (holding grade brackets) and standalone Range TMPs (inheriting grade colors) share container transforms under the same panel root. `CollectTooltipTMPs` explicitly validates active standalone Range TMPs with `RequireRoot(tmp.transform.parent)` and `RequireRoot(tmp.transform.parent?.parent)`. If a parent/grandparent lies outside the roots, it throws an `InvalidOperationException` and falls back to whole-scene scan. All required siblings are guaranteed in scope.

### 3. Fallback Bounding & Telemetry
- **Rank:** NOT PROVEN (No defect found)
- **Analysis:** The `catch (Exception ex)` block in `RunScan` latches `s_lastFullSceneTime = now` and checks `now - s_lastFullSceneTime < FallbackScanInterval` (0.5s). Even under continuous per-frame scope failures, whole-scene scans run at most once every 0.5s (capped at 2/sec), preventing runaway per-frame loops. If permanent fallback occurs (e.g., third-party mod hierarchy), `TooltipPerf` increments `s_scans` and `s_scanErrors` on every attempt while `s_scoped` remains 0 and `s_fullScene` counts only the throttled fallbacks. The state is clearly visible in `[perf]` telemetry.

### 4. Formatter Health Checks (`TrackFormatterHealth`)
- **Rank:** NOT PROVEN (No defect found)
- **Analysis:** `TrackFormatterHealth` in beta3 sets `expected = true` only when native tier-bearing text (`Tier: N`) is present (`s_tierRegex.IsMatch(text)`). For untiered items, crafting shards, idols, or lore, `expected` remains false, resetting `s_emptyBracketScans = 0` and suppressing false warnings. For composed TMPs marked with `Marker`, beta3 unwraps `saved.original` and inspects pre-composed text for brackets. Hovering untiered items no longer generates false "dead hook" log entries.

### 5. Ground Labels & Unit Border Subsystems
- **Rank:** NOT PROVEN (No defect found)
- **Analysis:** Code inspection confirms `GroundLabels.cs` (postfix on `GroundItemLabel.SetGroundTooltipText`) and `UnitBorder.cs` (postfix on `UITooltipItem.UpdateLayout`) are untouched by `DELTA-beta3.patch`. `TooltipRecolor` explicitly ignores texts containing `GroundLabels.Marker`. Both subsystems operate independently without regression.

### 6. Protected Invariants
- **Rank:** NOT PROVEN (No invariant violation found)
- **Analysis:**
  - `BuildInfo.Name` is frozen at `"Terrible Tooltips"` in `src/BuildInfo.cs` (line 10).
  - Normal tooltip behavior is preserved with no changes to rendering format or ZWSP markers.
  - No new configuration keys were added to `src/Prefs.cs`.
  - No new hooks added on `OpenItemTooltip` or `UpdatePrefixAndSuffixesText`.

### 7. Delta Containment
- **Rank:** NOT PROVEN (No containment violation found)
- **Analysis:** `DELTA-beta3.patch` modifies strictly `CHANGELOG.md`, `src/BuildInfo.cs`, `src/FilterRuleTooltip.cs` (removing dead field `s_startFrame`), and `src/TooltipRecolor.cs`.

---

## Verification Limitations (Could Not Verify)

1. **Real In-Game Hardware Frametimes:** Static code analysis and clean build compilation verify hash parity and logic correctness, but cannot measure exact in-game FPS delta or micro-stutter reduction on the reporter's specific hardware during heavy inventory combat.
2. **Third-Party Mod Assemblies:** Interaction with unmapped third-party mods that dynamically reparent Unity Canvas elements at runtime can only be verified up to the 0.5s fallback barrier, not for visual rendering of those specific third-party elements.

---

*Effective Model:* Gemini 3.6 Flash (High)
