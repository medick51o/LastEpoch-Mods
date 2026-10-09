# A Terrible Button: The Nexus Button — SPEC (draft 2026-10-08, awaiting Andrew's OK)

## What
One button that opens Last Epoch's own **Nexus** menu (the travel menu EHG added inside the
Monolith timeline screen, bottom-left "THE NEXUS") from anywhere in the game.

## Why
Today the Nexus is buried: walk to Traveler's Rest, click the Monolith pillar, open a timeline,
click THE NEXUS. Andrew wants one click from anywhere. Standalone mod — Terrible Inventory is
left as is.

## Where (Andrew, 2026-10-08)
On the crest at the top centre of the left-hand panel frame: centred ~18.75% from the left, ~3% from
the top on 16:9, ~100x26 at 1920 wide. ALWAYS visible in gameplay (Andrew mock-ups 2026-10-08, panel open + closed).
Must hold on 16:9, 21:9 and 32:9 (Andrew): pinned to the top-left corner at 360,32 (1080p units) and scaled by
screen height only, on the assumption the game pins its left panel the same way — verify on a 21:9 window.

## Placement ruling (Andrew 2026-10-08 20:12, supersedes council point 3)
"i want the nexus button right above in the middle of activites covering that crest ... it should sit flush
lit its just part of the whole UI of the game" — centred over the ACTIVITIES tab, on the crest, flush with
the frame. Approach: attach to the real crest object when a panel is open (follows UI Scale + aspect); same
spot when no panel is open.

## How (reuse, never rebuild)
- The click calls the game's own Nexus path (candidates: `PanelSystemCommands.Nexus()` dev
  command; `MonolithTimelinePanelManager.OpenNexus()`).
- The look is the game's own THE NEXUS button, cloned (sprite, font, glow), like Terrible
  Inventory's native clones.

## Customization (Andrew 2026-10-08 — AFTER the mod works; not in the probes)
Setting "Button Style" (MelonPreferences + an in-game settings row like Terrible Tooltips'):
- **Default** — the game's own THE NEXUS button, cloned, shrunk to the mock-up size (~100x26 @1080p;
  the native one is much bigger).
- **Transparent** — same button, see-through.
- **MEDICK WAS HERE** — same button, label reads "MEDICK WAS HERE" instead of "THE NEXUS". For fun.
Position nudge (Andrew 20:28: "we may need to give players a slider to move its location"): X/Y offset
sliders applied on top of the crest snap, in the same settings section.
Hotkey (Andrew 20:54): "Nexus Hotkey (N)" toggle, OFF by default — players opt in (N is often already bound,
e.g. Andrew's Map). Never fires while chat or a text field has focus.
Tab match (Andrew 20:44): box = the top panel's middle tab size (ACTIVITIES on the Monolith panel), resting
right above it, label at the tab label's size. Follows the top-most left panel (stacked panels, 20:42).
Later, if that lands well: player-typed custom label. Explicitly deferred: "lets get the mod working first".

## Safety
Travel from the Nexus is a server request. Terrible Inventory's history: a waypoint request away
from a waypoint got the client force-disconnected. So:
1. Probe 1 — open the menu only; do NOT click a destination.
2. Probe 2 — travel from it on an OFFLINE character.
3. Only then online.

## Done when
From any zone, one click opens the Nexus menu; a destination click travels; no disconnects;
button looks native; nothing per-frame; failures log and do nothing.

## Out of scope
Replacing or restyling the Nexus menu itself; Terrible Inventory changes.
