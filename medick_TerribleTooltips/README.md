# MedicK's Terrible Tooltips
**by medick** — v2.0.0

WoW / Diablo 4 style tier and grade colouring on item tooltips and ground labels. If your eyes were trained by twenty years of loot games, they already know how to read this mod — zero deciphering required.

---

## Features

### Tooltip Colours
Affix names are coloured by **crafting tier** (T1 gray → T7 mythic pink) and a **grade letter** is appended showing how well the affix actually rolled within that tier (F = bottom of the range, S = near-perfect). Same tier, very different power — now you can see it at a glance.

**v2.0.0: this finally works correctly on legendaries.** v1 graded legendary affixes off reconstructed display values, which broke on Legendary Potential items (a max-rolled 12% Mana could grade C). v2 reads the game's own stored roll bytes — the grade you see is the roll the game actually gave you.

### Ground Labels
Items on the ground show `[5A 3C 7S]` style brackets — tier number, grade letter, or both — so you can evaluate drops without hovering over everything. Uniques, sets and legendaries are deliberately left alone (Fallen Star's Improved Tooltips owns those, and does it better).

### Filter Rule Number
Hover any item and the tooltip shows **which loot filter rule matched it** — in gold, e.g. `Rule#69`. Switch it to NumberAndName mode and you get the rule's name too ("Rule #69: Maxroll told me to pick this up blah blah"). You can also reposition EHG's native rule number on ground labels (start / end / default).

---

## Settings (in-game Settings panel → scroll to "Terrible Tooltips")

| Setting | Default | What it does |
|---|---|---|
| Terrible Tooltips | ON | Master switch — enables all tooltip colouring |
| Tooltip: Tier Colors | ON | Colours affix names by crafting tier |
| Tooltip: Rank Colors | ON | Colours grade letters by roll quality |
| Ground Label Style | Tier+Rank | Dropdown: None / TierAndRank / TierOnly / RankOnly |
| Ground Labels: Filter Only | OFF | Only show brackets on loot-filter highlighted items |
| Ground Labels: Hold Alt to Show | OFF | Hide brackets until you hold Alt (KG-style) |
| Tooltip: Show Filter Rule # | NumberOnly | Off / NumberOnly / NumberAndName |
| Ground Label: Rule # Position | EHGDefault | Where EHG's rule number sits relative to the brackets |

The panel also includes colour-legend reference rows (the tier ladder and the (PoG) S→F (RiP) grade ladder) so you never have to leave the game to remember what purple means.

Settings persist to `UserData/medick_Terrible_Tooltips.cfg` — editable by hand if the in-game panel ever breaks after a game patch.

---

## Tier Colours
| Tier | Colour |
|---|---|
| T1 | Gray |
| T2 | Light Gray |
| T3 | Green |
| T4 | Blue |
| T5 | Purple |
| T6 | Orange |
| T7 | Mythic Pink |

## Grade Letters
| Letter | Meaning |
|---|---|
| F | Bottom of the roll range (roll sucks bro) |
| C | Below average |
| B | Average |
| A | Above average |
| S | Near-perfect roll |

---

## Compatibility
- Works standalone — does **not** require KG's mod
- **LeHud** — co-exists peacefully; ground label writes preserve LeHud's custom rarity colours
- **Fallen Star's Improved Tooltips (Fallen_LE_Mods)** — fully compatible; unique/set/legendary ground items are deferred to Fallen Star on purpose, and Fallen can detect this mod via `m.Info.Name == "Terrible Tooltips"`
- Survives game patches gracefully: each feature patches independently, so if an update breaks one thing, the rest keeps working and the log tells you exactly what degraded

---

## Credits & Inspiration

This mod exists because **KG's Better Item Filter and Tooltips** ([war3i4i/LastEpochImprovements](https://github.com/war3i4i/LastEpochImprovements)) is no longer around — and it was my second favourite Last Epoch mod of all time. There was a void, and someone had to fill it (terribly).

Massive shout out and full credit to **KillingGodVH** for the original inspiration and the open-source code that helped shape how this mod works. The ground label logic, the settings-injection technique and several core patterns in this codebase were learned from and adapted from KG's work. If you haven't seen what he built, go look — it was something else.

This mod is dedicated to filling that gap, not replacing the legend. ♥

---

## 🥚 Easter Egg: "To Aaron's House"

There is a button at the bottom of the settings panel called **"To Aaron's House"**.

We are not going to tell you what it does.

Fine. It teleports you to the Bazaar. *Shocking.* Revolutionary, even. A button, in a game, that moves your character to a location. Groundbreaking stuff.

**If the button doesn't work:** Open your world map to the Divine Era at least once that session. Yes, really. Just open the map, look at it for half a second, close it, then press the button. We know. We're sorry. The game made us do it this way.

*Dedicated to AaronActionRPG ♥*

---

## Installation
Drop `medick_Terrible_Tooltips.dll` into your `Last Epoch/Mods/` folder.

Requires **MelonLoader 0.7.2+**.
