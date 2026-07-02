# Changelog — MedicK's Terrible Cooldowns

## v1.0.0 — 2026-07-01

Ground-up engineering pass and rebrand: **Cooldown Tracker** is now
**MedicK's Terrible Cooldowns** ("Terrible Cooldowns" for short), joining
Terrible Tooltips and Terrible Inventory. Internal name, DLL, preferences and
the Nexus upgrade path are unchanged (`medick_CooldownTracker`). The version
resets to **1.0.0** — the Terrible era starts here (succeeds CooldownTracker
v4.5).

### Added
- **Console button badges** — short button labels render as real console-style
  buttons under each skill icon: round face buttons in authentic pad colours
  (red B, gold Y, green △, blue ✕…) and capsule keycaps for bumpers/triggers/
  sticks/keys (RB, LT, L3, Q…). Drawn procedurally from a runtime-generated
  anti-aliased disc — no image assets shipped. Spell-name labels keep the
  classic text strip. Toggleable in DISPLAY ("Console button badges", on by
  default).
- **Move mode** — press *Move* next to the offset sliders, drag the floating
  icon cluster in the game view to wherever you want it, press *Lock*. The
  sliders follow live and remain available for precision nudges. Game input
  is blocked while moving so your character doesn't wander.
- **Redesigned settings panel** — a real design system (dark Last-Epoch-native
  palette, single gold accent, segmented controls, custom switches and
  sliders, hover/focus states, branded title bar and footer) replacing the
  grab-bag of coloured boxes and default Unity widgets.
- Per-slot tracking toggles now persist between sessions.
- `DebugLog` preference — registration logging is opt-in; default output is
  one startup line.

### Fixed
- **Real skill icons.** Sprites packed into the game's UI atlases were being
  sampled with original-texture coordinates (`Sprite.rect`) instead of packed
  atlas coordinates (`Sprite.textureRect`) — reading a random neighbouring
  region of the atlas sheet. That is why earlier versions showed emblem and
  portrait art instead of your actual skills. Icons now also refresh when you
  swap a skill into an existing slot, and respect override sprites.
- **No more ghost icons on the login screen.** Slots are pruned the moment their
  game objects die, destroyed-object checks now use Unity's real null semantics
  (the old `?.` checks passed dead references through), and the fallback that
  drew icons at a guessed screen position when no player was found is gone —
  no living player on screen, nothing drawn. Menus stay clean.
- **Settings panel position persists.** Drag it once; it opens there every
  session. New "Reset panel position" button if you ever lose it.
- **Removed all seven global Harmony patches on `UnityEngine.Input`.** Last
  Epoch routes input through its own `EpochInputManager`, so those patches
  never affected the game — they only fed false input to other mods and to
  this mod's own controller detector (input-mode detection used to freeze
  while movement lock was active). `EpochInputManager.forceDisableInput`
  remains the sole blocking mechanism, now written only on state change so
  other mods using the same flag aren't stomped.
- Panel header version no longer hardcoded (read v4.4 while the mod was 4.5).
- Text-field focus flag can no longer latch on and permanently block input
  when the slot list empties mid-edit.

### Changed
- Source restructured from one 1,050-line file into `src/` modules
  (registry / input / blocker / renderer / panel / picker / styles).
- Render path no longer allocates per frame (cached GUI styles, cached
  two-line label splits, no per-frame LINQ).
- In-game keybind probe now retries with 2 s backoff and gives up after 6
  attempts instead of sweeping reflection 20× per second forever.
- Settings carry over from v4.x unchanged (same preference category and keys).

---

## v4.5 — 2026-04-10
- Removed `Patch_Start` (`AbilityBarIcon.Start` no longer exists in Season 4
  builds) — mod loads cleanly alongside other mods such as Fallenstar's
  Improved Tooltips. `Awake` already handles slot registration.

## v4.4 — 2026-04
- Backspace / Delete work in label text fields (removed the `ev.Use()` event
  swallow that blocked IMGUI fields).
- Movement lock switched to `EpochInputManager.forceDisableInput` — the
  game's own input flag.
- Game hotkeys no longer fire while typing in a label field.

## v4.3
- Corrected default button arrays; movement lock toggle added.

## v4.2
- [▼] Button Picker for Xbox and PS5 modes.

## v4.1
- Per-mode custom labels (Keyboard / Xbox / PS5 fully independent).
- Slot 6 (evade/dodge) fully supported and customisable.

## v4.0 and earlier
- Floating overhead cooldown icons, input-mode auto-detection, opacity/size/
  offset sliders, two-line label stacking, initial Nexus release.
