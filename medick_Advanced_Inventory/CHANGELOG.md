# MedicK's Terrible Inventory v2.0.2 — Season 5 compatibility

- Season 5 compatibility update, rebuilt against the current game assemblies.
- Fixes the shared CoreModule startup failure automatically from inside the mod DLL.
- One-DLL installation: replace medick_Terrible_Inventory.dll in Mods. No extra plugin or repair tool is required.
- Keeps an original CoreModule backup and handles regenerated files.
- Existing inventory settings are preserved.

Installation: close the game, then replace medick_Terrible_Inventory.dll in Last Epoch/Mods/. This ZIP contains exactly one DLL. No separate plugin, script, or library download is needed. Requires MelonLoader 0.7.3.


Known issue: Quick Teleport currently does not work reliably in Season 5. The mod can incorrectly treat unlocked destinations as locked. Please use the normal world map for travel while we work on a fix.

# Changelog — MedicK's Terrible Inventory

## v2.0.1 — council pass (2026-09-10)

- A1: Travel now fails closed, with a one-time debug message, when waypoint unlock data is unreadable.
- A2: Removed the unproven `WaypointManager` force-enable so travel uses only the game's waypoint-click path.
- A3: STASH ALL binds queued positions to item-object identity, skips changed slots, and reports move failures and totals.
- A4: Scene loads now reset the STASH ALL re-entrancy guard.
- A5: IL2CPP delegate retention is keyed by live controls, replacing rebinds and pruning destroyed controls.
- A6: Added opt-out deployment, resilient Sort-icon lookup, current compatibility wording, and the 2.0.1 version bump.

## v2.0.0 — 2026-06-11

Ground-up professional rebuild, built to a written spec (SPEC.md) on top of
a full excavation of the original development history (ARCHAEOLOGY.md).

### The native look
- **Every visual element is now a clone of the game's own UI.** v1's teleport
  column was hand-drawn flat rectangles with a scavenged font and a faux
  two-rectangle "gold border"; v2 clones the game's Sort button — native
  9-slice sprite, native font and material, native hover/press behavior —
  for the footer buttons, the teleport buttons, the group headers, and the
  QUICK TELEPORT tab. Faction identity is a slim colored accent bar plus a
  tinted subtitle, not paint over the artwork.
- **The game's own UI is no longer modified.** v1 shrank EHG's Transfer/Sort
  buttons, squeezed the footer layout, and mutated the currency row while
  dumping the whole hierarchy to the log every inventory open
  (`TryCompactCurrency`). All gone.

### Travel engine hardened
- **Unlock gate:** teleports now check the game's unlocked-waypoint list and
  do nothing for locked destinations — exactly like the map's own nodes.
- **The dangerous fallback chain is deleted** (map-flash + era-tab
  text-clicking + searching every button on screen for "VISIT…", which once
  matched the mod's own button and melted the framerate). The silent
  era-controller primer — run once per session, snapshot-and-restore — plus
  one retry replaces all of it.
- Travel concurrency guard retained; never uses raw network calls
  (`SendAttemptWaypoint` disconnects you — v1 lesson, still law).

### Settings (new)
- **Settings → Terrible Inventory**: individual toggles for STASH, STASH ALL,
  VENDOR, and the Quick Teleport menu (the "let users pick what they see"
  request from the original build, finally shipped), plus opt-in debug
  logging. Changes apply live.
- **VENDOR ships disabled by default** — it's there if you want it: flip it
  on in Settings → Terrible Inventory. (Existing users who already enabled
  it keep their saved setting.)

### Engineering
- One 53KB three-file tangle → `src/` modules; single version source (v1
  shipped three disagreeing version strings); Harmony patches applied
  per-class with graceful degradation; quiet by default (one startup line);
  controller-mode keep-alive comments finally say `OnLateUpdate` (the code
  always did — the comments said OnUpdate since v1.2.1).
- Teleport column layout via VerticalLayoutGroup (v1's hand-rolled reflow
  retired); Andrew's hand-tuned geometry preserved exactly.

---

## v1.5.0 — 2026-04-14
- Rebrand: Advanced Inventory → Terrible Inventory; TRADER → VENDOR.

## v1.4.0 — 2026-04
- Teleport menu regrouped by utility: FACTIONS / DUNGEONS / HUBS.

## v1.3.0 — 2026-04
- All teleport buttons work without opening the map (silent era-controller
  priming on first inventory open).

## v1.2.x — 2026-04
- Collapsible QUICK TELEPORT menu; controller/Steam Deck fix (buttons stay
  visible in controller mode via LateUpdate keep-alive).

## v1.0–1.1 — 2026-04
- Initial release: STASH / STASH ALL / TRADER footer buttons, 5-destination
  teleport column; Temporal Sanctum/Lightless Arbor scene swap hotfix.


