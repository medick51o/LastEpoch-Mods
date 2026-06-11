# Changelog — MedicK's Terrible Tooltips

## v2.0.0 — the ground-up rebuild
The whole mod was re-architected from a single 400-line Core.cs into twelve
focused modules, with every battle-tested behaviour preserved verbatim.

### Fixed
- **Legendary grading bug** (reported by zoundb on Nexus): affixes on
  Legendary Potential items could grade absurdly low — a max-rolled 12% Mana
  graded C instead of S. The grade is now read from the game's own stored
  roll bytes (`uniqueRolls`) instead of being reconstructed from display
  values, so legendaries grade exactly as well as they rolled.
- Off-by-one that could skip the last unique affix when grading.

### Changed
- **Tooltip: Show Filter Rule # now defaults to NumberOnly** (was Off) —
  the matched rule number shows in gold on hover out of the box.
- Colour-legend rows in settings are now honest information rows — the
  decorative non-functional checkbox is gone (no more "this box is not
  clickable" apology).
- One startup log line; everything else behind a new `DebugLog` cfg option.
  (Aaron's House keeps its ♥ lines. Canon.)
- Per-feature patching: a game update that breaks one native signature now
  degrades that one feature with a clear warning instead of taking the mod
  down with it.

### Unchanged on purpose
- All v1 settings, names and the cfg file path — existing configs upgrade
  in place, nothing resets.
- The colour language (T1 gray → T7 mythic pink, F → S grades).
- LeHud truce (face-colour preservation) and the Fallen Star treaty
  (unique/set/legendary ground items stay theirs).
- The 69. The (PoG)/(RiP). Aaron's House and its map ritual. The jank is
  part of the homage.

## v1.5.0
- LeHud compatibility fix (tooltip crash on co-install)
- Tooltip filter Rule # display rebuilt

## v1.4.0
- Ground label rule # positioning (Start / End / EHGDefault)
- Tooltip: Show Filter Rule # display modes

## v1.3.0
- Ground label styles (TierOnly / RankOnly), Filter Only, Hold-Alt mode

## v1.2.0
- Initial public release: tier/grade tooltip colouring, ground label
  brackets, To Aaron's House
