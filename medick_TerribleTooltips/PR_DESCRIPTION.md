# Fix displayed roll grades and matched filter-rule labels

Depends on PR #3 (17d84cd); its API compatibility fixes and original commit are retained.

## Changes
- Grade displayed values against their own ranges using F/C/B/A/S bands (30/60/90/97%), display precision and the short-range near-maximum ladder. Fixed properties are S.
- Read original implicit bounds, use the widest affix component, and apply the native idol multiplier. Hide only the mod-provided tier-1 idol badge, not native ALT details.
- Handle native range macros in unique/legendary descriptions without confusing durations and cooldowns with rolls; preserve native range text.
- Resolve the first enabled matching filter rule with the actual player level. Respect HIDE and native colour/emphasis, while retaining the explanation when ALT temporarily suppresses ground filtering.
- Give the rule label an owned layout row. Restore it after native rebuilds, fence pooled/rebound tooltips and release owned objects on shutdown/unload.

Settings remain in the original GAMEPLAY category. No MODS/SOCIAL experiments, diagnostic probes, audit files, test scaffolding, binaries or unrelated mods are included.

- Add Show Affix Rarity (default OFF), independent of Show Grade Letters. Optional tier/rarity/roll signals such as 8SS use native affix weighting. Common or unknown affixes omit rarity; unique macros and ground labels are unchanged.

## Verification
Release build against installed generated game assemblies: zero warnings/errors, with deployment disabled. External checks against the final source passed: 106 roll-quality checks, 77 display/template checks and 663 label-format assertions. Test SDK reports the .NET 6 end-of-support warning. The test harness is intentionally not part of this change.

Build using the existing project and override ML and GM with local generated-assembly paths; always pass -p:DeployToMods=false for review builds.

## Runtime scope
The source implementation was exercised in a separate 3.3.17 installation. The b27c7fc candidate was installed and accepted by the user. The subsequent expert-review fixes require another manual pass; this is not exhaustive native layout or Harmony coverage. Compilation and pure checks do not establish complete runtime parity. Reverse macro ranges and unknown range-detail localizations fail closed. Signed single-value fixed descriptions are graded only when exactly one hidden typed modifier matches; arbitrary prose is not graded.

Suggested checks: Evolution End fixed minion resistance with/without ALT; Lethal Concentration 116% poison in 80-120%; idol resistance near maximum; composed unique descriptions; rule labels across ALT, comparison tooltips, re-hover and filter edits. Confirm original settings tabs remain unchanged.
Expert-review fixes isolate roll colours from hidden rarity, omit empty signals, reject unknown rolls, restore typed fixed descriptions, avoid caching unavailable definitions and cap normalized rule-name input at 1024 UTF-16 units without splitting surrogate pairs.

ColorAffixesByTier defaults OFF and chooses roll quality rather than tier. Existing AffixNameColor modes remain active. Hidden or disabled source signals leave the game colour untouched; rarity never controls text colour.
