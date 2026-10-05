# Terrible Tooltips 3.1.1-beta1 — build handoff

Status: **BUILT, NOT DEPLOYED.** Both confirmed-by-code retry loops are repaired. The reporter's exact crafting-shard scenario and FPS change remain unverified; Andrew cannot run the game and the reporter's log is still unavailable. This is a diagnostic feedback beta, not a claim of an in-hand fix.

## Files and exact behavioral boundaries

- `medick_TerribleTooltips/src/FilterRuleTooltip.cs`: one rule resolution per content generation; one descendant enumeration per discovery attempt, at most once per frame. Discovery is allowed through capture frame + 12 inclusive or capture time + 0.5 unscaled seconds, whichever expires first. At most 13 attempts if the first happens on the capture frame. The first attempt is always allowed even if delayed by disabled preferences; it does not extend the deadline. Missing targets and discovery exceptions terminate. A target appearing at frame 7 was verified in the harness. A target appearing after exhaustion is deliberately not rediscovered until a new generation; this is the bounded settle tradeoff.
- Content identity is the owning UITooltipItem, native target/targetType, and an exact snapshot of item.id bytes. Equal serialized contents in newly allocated wrappers do not reset the budget. ID-less fallback uses item type/subtype/rarity/unique ID/individual ID; it does not use wrapper identity. Pending state is cleared when the observed owner closes/disappears, singleton changes, target/type changes, a null setter arrives, or a different content generation is captured. These owner checks run even while master/rule display is off. Same-content setters still retain the existing composer MarkDirty behavior; a remaining dirty-trigger storm is explicitly visible in the counters and is outside these two repairs.
- Successful/persisted-tag destinations have a separate repair-pending state. A later setter rewrite can repair the known row after reenable or after the discovery deadline, without opening a new descendant-search budget. If the persisted marker came from a previous hover, rule resolution occurs once on repair. Inactive known rows wait without matching/discovery; detached rows are rejected. This preserves two working Rule# sequences caught by the first native self-check.
- `medick_TerribleTooltips/src/TooltipRecolor.cs`: after the full composition pass completes and **before RequestRelayout**, retire originals whose TMP is still active and whose current text lacks the composer marker. This includes empty/noncomposable replacements and failed-to-compose markerless rows. Keep marked originals, including marker-only hidden ranges and successfully composed rows: Alt/master-off still needs their vanilla text. Unknown/exceptional ownership is retained; a failed overall scan never performs this retirement. Because retirement precedes native relayout, a just-composed row is not discarded merely because native relayout later rewrites it. The next scan can reconsider it. Remove any suppression-cache entry for the same retired ID, so later enforcement cannot hide a new range without saved restoration data. This is not broad suppression-cache pruning.
- The five-frame dirty window, 0.5-second fallback, existing scene-wide scan and marker tokens are unchanged. Native range switching, composer formatting and 3.1.0 border code are unchanged. The scan gate returns an exclusive reason instead of a bool so instrumentation counts actual scans, not gate evaluations. Priority remains dirty, then fallback, then marker loss.
- `medick_TerribleTooltips/src/BuildInfo.cs`: Version becomes `3.1.1-beta1`; Name remains exactly `Terrible Tooltips`. Decompiled the built DLL's BuildInfo to verify both constants. The frozen .csproj still declares assembly/package version 3.1.0; MelonInfo/startup uses BuildInfo.Version and identifies this beta correctly.
- `CHANGELOG.md`: adds beta scope, absence of in-hand reproduction, retry bounds and counter definitions. Supporting notes, source-linked test harness, results and `DELTA-311beta.patch` live only in this build directory.

## Diagnostic contract

No new configuration key. With existing `DebugLog=true`, one summary at most every five unscaled seconds, including zero-count heartbeat windows. The actual interval is printed, so a stall is not mislabeled as exactly five seconds. Invariant numeric formatting keeps decimal points stable across locales. Counters reset after each summary; disabling discards the current partial window, and enabling starts a fresh window.

Exact format (N is an integer, elapsed typically 5.0):

```text
[perf] 5.0s: scans=N (fullScene=N, trigger: dirty=N/markerLoss=N/fallback=N) ruleStart=N ruleAttempts=N ruleMatch=N ruleNoTarget=N ruleGiveUp=N ruleInjected=N ruleReuse=N staleRetired=N scanErrors=N
```

| Counter | Meaning |
| --- | --- |
| scans / fullScene | Actual RunScan entries / scene-enumeration attempts, including failed attempts. They currently agree because scoping is deferred. |
| dirty / markerLoss / fallback | Exclusive reason for each accepted scan. Sum equals scans. Due fallback takes precedence over simultaneous marker loss; no double counting. |
| staleRetired | Active markerless originals actually removed after completed passes. Positive counts establish that the stale-original condition occurred; they do not measure milliseconds saved. |
| ruleStart | New content generations captured, including captures while the display is off. Equal-content setter repeats are excluded. |
| ruleAttempts | Descendant discovery attempts, including throws. Known-row repair does not increment this. |
| ruleMatch | Calls to the rule resolver; a resolver call can contain native filter.Match and manual per-rule fallback, as before. It is not the count of individual rule predicates. |
| ruleNoTarget | Discovery attempts with a matched rule but no active destination. |
| ruleGiveUp | Terminal exhausted discovery windows. Can also count bounded discovery exceptions; compare ruleNoTarget. |
| ruleInjected | Successful writes, including repair to a known row. |
| ruleReuse | Pending owner invalidations observed by MonitorUpdate (including closure). Content changes captured by setters are counted in ruleStart instead. |
| scanErrors | Outer scan exceptions. Individual TMP exceptions remain under the existing catches. |

Disabled instrumentation does no timing, counting, formatting, allocation or I/O: only dynamic preference guards remain, plus one state reset if debug was previously on. Literal zero CPU is impossible for a runtime preference; no extra scene/target enumeration is performed for diagnostics. The harness verifies 10,000 disabled heartbeat calls allocate zero bytes and read no clocks, and disabled event sites leave counters/window untouched. Existing formatter/debug messages outside this counter subsystem remain as before; this beta does not promise that the entire DebugLog stream has only one line per five seconds.

### Reading the reporter's feedback

Ask the reporter to enable DebugLog using the existing preference, confirm startup says `3.1.1-beta1`, and return the MelonLoader log with approximate timestamps for a stationary shard hover and equipment→shard transition. Allow at least 15 seconds of stationary hover so the opening window and two settled summaries are captured. No such request has been sent by this builder.

- **Stale-original lane exercised:** opening/transition reports staleRetired > 0; later stationary windows should have no recurring markerLoss and around 10 fallback scans per five seconds. If markerLoss/staleRetired keep increasing under an unchanged hover, ongoing native/other-mod rewrites or uncaptured identity changes remain; do not call the entire stutter fixed. Large dirty counts point to repeated setters/formatter/native-switch invalidations, not the retired entry alone.
- **Missing-destination lane exercised:** one generation, ruleMatch <= 1, ruleNoTarget > 0, one eventual ruleGiveUp, then ruleAttempts/ruleMatch/ruleNoTarget zero in later stationary windows. Events may straddle summaries: compare adjacent windows rather than requiring all counters in one line. If new generations keep appearing, ruleStart exposes the identity churn; if attempts/matching continue with no new generation, the bound failed. A late target instead produces ruleInjected before exhaustion.
- **Neither condition observed:** zero staleRetired and zero matched missing-target events in a timestamped shard interval means neither repaired condition was observed in that interval. It does not prove the historic 3.0.2 report had another cause. Continued stutter with settled counters points toward remaining scene-scan cost, other dirty producers, border/native work or another mod; counters are evidence of code paths, not a frame-time profiler.

## Gates and artifacts

1. All 14 supplied baseline hashes matched before edits. After edits only the four permitted baseline files differ. GroundLabels, UnitBorder, Prefs and every other baseline source remain byte-identical. No project/config/README/release edits, git mutation, deployment, zip or game launch.
2. Requested `dotnet build -c Release -p:DeployToMods=false` failed during restore because `C:\Users\andre\AppData\Roaming\NuGet\NuGet.Config` is denied by this session's sandbox. An attempted local RestoreConfigFile override hit the same denied user file; its temporary local config was removed. Permissions were not widened.
3. Safe substitute from `medick_TerribleTooltips/`: `dotnet build -c Release -p:DeployToMods=false --no-restore` uses existing restored assets. **Build succeeded: 0 warnings, 0 errors.** Exact output: `build-output.txt`. This is not a claim that fresh restore was validated.
4. `Run-Regression.ps1` compiles the actual two changed behavioral source files plus unchanged Colors against controlled stubs with the installed .NET 6 reference assemblies; it does not edit/create a project file or access NuGet. **43 assertions passed** (`regression-output.txt`), covering retry bounds under same-content/new wrappers and low FPS, late target activation, reuse/closure, known-row repairs across toggles, cached-marker recovery, failed discovery, stale retirement, restoration ordering, Alt, sealed/two-stat composition, ground marker exclusion, trigger accounting and disabled telemetry. These tests do not execute Harmony, native game methods, TMP layout or UnitBorder rendering.
5. Built DLL: `C:\Users\andre\Downloads\LastEpoch-Mods\medick_TerribleTooltips\medick_TerribleTooltips\bin\Release\net6.0\medick_Terrible_Tooltips.dll`, **94,720 bytes**, MD5 **`d5f5687ef1b5805ef371f0bb2d9987b3`**.
6. Live DLL: `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\medick_Terrible_Tooltips.dll`, MD5 before and after **`cde6ed796a5aeabb93ea9347b5a0c164`**, unchanged.
7. During this build, an external conductor status update appeared in CURRENT-WORK.md (timestamp 2026-09-17 11:58 local; its added text describes this ticket). This builder never wrote that file and preserved the concurrent edit. Repository-wide diff therefore includes that external change; the builder's DELTA patch contains only the four allowed tracked files. Existing diagnosis/baseline and unrelated sibling changes are likewise preserved.

## Review and remaining limitations

The [dispatch skill](C:/Users/andre/.codex/skills/dispatch/SKILL.md) was applied with one builder and sequential read-only self-checks. A fresh Claude review tool call was blocked by required approval under policy `never`; no bypass or external write was attempted. **Independent review unavailable in this seat.** The first native OpenAI self-check found two Rule# regressions; both were accepted, repaired and covered by tests (`REVIEW-311beta-round1.md`). The final fresh self-check is recorded separately; native review cannot independently approve OpenAI-produced work. The conductor retains the cross-vendor/release gate.

No in-game validation: late native header/target lifetimes, real shard routing/serialized IDs, comparison pooling, actual filter/native exception costs, and frame-rate improvement remain unproven. Distinct ID-less items with identical fallback identity on the same target cannot be distinguished from this data alone; no undocumented native hook was added. A reuse that changes neither any observed identity nor passes through a sampled closure cannot be inferred from these fields. The reporter's counters will expose repeated generation/discovery churn, but are not a substitute for native timing if the symptom persists.

Still queued: **scene-scan scoping and zero-width marker separation**, broad suppression-cache pruning outside DeepRange, continuation-tint boundaries and ground-label cache/style work. GroundLabels.cs and UnitBorder.cs were deliberately excluded from this beta's source changes. No deploy, zip, commit or publication has been performed.
