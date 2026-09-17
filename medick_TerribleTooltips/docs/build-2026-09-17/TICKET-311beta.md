# 3.1.1-beta1 — build contract

Requested by Andrew through the Dispatch Deck on 2026-09-17. Boss's words: "i dont have the means of testing right now ... can we do the assumed fix and put a beta of this 3.1.X and i link the user and i will ask him for feedback".

## Requested result and acceptance criteria

- Fix both confirmed-by-code defects diagnosed in `../diag-2026-09-17/DIAG-shard-stutter.md`. Do not wait for the unavailable in-game A/B or reporter log. Attribution of the reporter's exact crafting-shard scenario remains unproven.
- A: matched filter rule with no active `requires` completes as not applicable after a short bounded settle window. Equipment targets appearing a few frames late must still get Rule#. Clear stale pending ownership on closure/reuse. Repeated same-content setters must not restart the retry budget.
- B: after a completed scan, retire active markerless cached originals that were not successfully composed (empty, repurposed, ineligible). Preserve the five-frame dirty window, 0.5-second fallback, valid Alt/master-off restoration. State the exact retirement boundary in BUILD-311beta.md.
- C: existing DebugLog preference controls counters. At most one summary every roughly five seconds, no per-frame diagnostic messages. Record scene scans, exclusive trigger categories, filter matching/missing destinations and stale retirement, with enough information to distinguish both loops in the reporter's log. Disabled instrumentation must not read clocks, count, format, allocate or log; dynamic preference guards are unavoidable and must not be disguised as literal zero CPU.
- BuildInfo.Version = `3.1.1-beta1`; BuildInfo.Name stays `Terrible Tooltips`. CHANGELOG explains beta status and counters. Do not assert in-game or FPS validation.
- Preserve working tooltip behavior: Rule#, Alt ranges, master-off vanilla restoration, two-stat/sealed affixes, ground labels and 3.1.0 border; no hooks on OpenItemTooltip/UpdatePrefixAndSuffixesText.

## Fence, baseline, validation and rollback

Source write set: `medick_TerribleTooltips/src/TooltipRecolor.cs`, `FilterRuleTooltip.cs`, `BuildInfo.cs`; also `CHANGELOG.md` and this `docs/build-2026-09-17/` directory. Do not edit GroundLabels, UnitBorder, Prefs, the project, config, README, release artifacts, or git state. Build-generated bin/obj artifacts are the necessary output of the explicitly requested build.

Supplied baseline: `baseline-311beta.md5`, all 14 entries verified before edits. Source baseline matches release commit `601a9be` under repository HEAD `6860e65`. The earlier diagnosis and supplied baseline were already untracked; preserve them. Retain unrelated sibling-project changes.

Requested build: `dotnet build -c Release -p:DeployToMods=false` from `medick_TerribleTooltips/`. Require zero compiler warnings/errors. Existing Mods DLL must remain MD5 `cde6ed796a5aeabb93ea9347b5a0c164`. No deployment, commit, zip, or release packaging. Exact command's restore is blocked by inaccessible user NuGet.Config; document any safe no-restore substitute.

Verification: source-linked .NET 6 controlled stub harness for state transitions; release build against installed game references; baseline containment checks; output DLL size/hash and live Mods MD5. No game launch is available. Native self-check can find defects but is not independent approval. A fresh Claude subscription review was attempted and blocked because tool approval is required but session approval policy is never; do not bypass it.

Rollback if requested later: revert only this ticket's four tracked-file hunks using DELTA-311beta.patch and the supplied baseline, preserving unrelated work. Live Mods DLL needs no rollback because it is never replaced. Deployment/release authority remains with Andrew; the reporter's log is the future evidence gate.

Deferred: scene-scan scoping and non-substring marker separation from Tier B, broad range-cache pruning, and all GroundLabels/UnitBorder/Prefs changes.
