# Changelog — MedicK's Terrible Tooltips

## v3.0.0 — THE CLEAN LINE
One line per affix. The essay dies.

### Changed
- **Affix lines composed clean**: each affix is now ONE line — the affix
  text plus a `Tier 5·A` signal (tier in its tier colour, grade in its
  grade colour). EHG's separate "Tier: 5 (max craftable)" and
  "Range: 40% to 60%" lines are folded away. A 4-affix exalted drops from
  ~16 lines of tooltip to ~5.
- **Hold Alt = deep view**: while hovering, hold Alt and the full EHG
  detail (ranges, craft info) returns live under each affix. Release Alt
  and it's clean again.

### Added — the layout toy box
- **Tooltip Layout** (dropdown): `BadgeLeft` — "Tier 5·A  affix text"
  (default) · `SignalRight` — signal at the right edge · `Trailing` —
  "affix text — Tier 5 A". Build it how you want.
- **Signal Style** (dropdown): `Badge` (default) — Tier/Grade render as
  colored chips: the chip wears the tier colour, the text inside renders
  in one bright ink that stays readable on every chip, so the signal
  reads as a *label* while the affix text keeps the color story ·
  `PlainText` — colored text, no chips.
- **Affix Name Color** (dropdown): `TierColor` (default — the text wears
  its tier colour) or `GameDefault` (only the signal is coloured).
- **Show Grade Letters** (toggle): want the S/A/B/C/F gone? It's gone.
- **Always Show Ranges / Always Show Tier Details** (toggles): pin the
  deep-view lines permanently if Alt isn't your style. Both default OFF.

### Fixed — release-night fleet pass (2026-07-01)
- **Ground labels are truly untouched again**: the clean-line composer
  could capture a single-affix ground bracket (its wake gate had no
  ground-label exclusion) and permanently mangle it into chip format.
  The composer now excludes anything carrying the ground-label marker.
- **Hold-Alt ground labels**: an item dropped while Alt was already held
  now shows its brackets immediately (the swap previously fired only on
  an Alt state *change*).

### Unchanged on purpose
- Ground labels — the KG bracket is beloved; the bracket format renders
  exactly as it always has (tonight's fixes only shield the labels from
  the composer and make Alt-mode brackets appear instantly).
- Standalone set/unique Tier widgets keep the v2 recolor treatment
  (they're separate game widgets, not our essay to kill). Standalone
  Range rows now follow the same ranges-hidden / Hold-Alt rule as the
  rest of the tooltip — the lean pass superseded the keep-as-is plan.
- All v1/v2 settings, the cfg path, every ecosystem treaty (LeHud truce,
  Fallen Star), and every crash law.

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
