# Terrible Death Log, Assessment and Counter

Experimental v0.1.2 review candidate. Not ready for Nexus Mods or verified in game.

Short description:

A compact, movable death counter with a readable death log, damage colors and advice based on captured defenses. Save gear reassessments without changing the original death, track Season and Legacy history, and browse boss fight notes.

Changelog:

- Redesigned the counter for a smaller idle footprint with hover help and a visible MOVE handle. Added panel and counter size controls.
- Added game death-report hooks for attacker, ability, ailment, damage types and critical-hit context. Preserved the game's death text colors at a readable size.
- Freeze death context before scene transitions. Match late reports carefully and keep missing details unknown.
- Prioritize measured resistance gaps and other supported defensive fixes. Keep advice to three distinct actions; health and ward recommendations require evidence.
- Save separate dated gear reassessments with current stats, comparisons and explained defense grades. Review earlier updates, reassess again, or delete reassessments with confirmation. Original deaths stay saved.
- Preserve Season/Legacy labels and exact scene/area context when available.
- Added a Boss tips tab with five sourced starter profiles, captured damage context and links to the research. Manual browsing does not identify or change a death.
- Added counter reset with the requested Medick warning. Reset keeps the death history and game's counter intact.
- Renamed the display brand to Terrible Death Log, Assessment and Counter. The DLL remains medick_DeathCounter.dll with the embedded Season 5 repair.

214 CoreTests pass. Local and NoGame builds have zero warnings/errors; both InteropGuard checks pass. Native capture, rendering, stats and boss behavior still need testing.
