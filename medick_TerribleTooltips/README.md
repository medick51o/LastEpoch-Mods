# Terrible Tooltips
**by medick** — v1.2.0

WoW / Diablo 4 style tier and grade colouring on item tooltips and ground labels.

---

## Features

### Tooltip Colours
Affix names are coloured by **crafting tier** (T1 gray → T7 mythic pink) and a **grade letter** is appended showing how well the affix actually rolled within that tier (F = bottom of the range, S = near-perfect). Same tier, very different power — now you can see it at a glance.

### Ground Labels
Items on the ground show `[5A 3C 7S]` style brackets — tier number, grade letter, or both — so you can evaluate drops without hovering over everything.

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
| F | Bottom of the roll range |
| C | Below average |
| B | Average |
| A | Above average |
| S | Near-perfect roll |

---

## Compatibility
- Works standalone — does **not** require KG's mod
- **Fallen_LE_Mods** compatible — Fallen will detect this mod if you add `|| m.Info.Name == "Terrible Tooltips"` to the KG check

---

## Credits & Inspiration

This mod exists because **KG's Better Item Filter and Tooltips** ([war3i4i/LastEpochImprovements](https://github.com/war3i4i/LastEpochImprovements)) is no longer around — and it was my second favourite Last Epoch mod of all time. There was a void, and someone had to fill it (terribly).

Massive shout out and full credit to **KillingGodVH** for the original inspiration and the open-source code that helped shape how this mod works. Several patterns and approaches in this codebase were learned from and influenced by KG's work. If you haven't seen what he built, go look — it was something else.

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
