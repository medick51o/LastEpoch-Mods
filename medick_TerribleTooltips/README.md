# MedicK's Terrible Tooltips
**by medick** — v3.1.0

WoW / Diablo 4 style tier and grade colouring on item tooltips and ground labels. If your eyes were trained by twenty years of loot games, they already know how to read this mod — zero deciphering required.

---

## Features

### The Clean Line (v3)
One line per affix: the affix text plus a **`Tier 5·A`** signal — tier spelled out in its tier colour, grade letter in its grade colour. EHG's per-affix "Tier:" and "Range:" lines are folded away; a 4-affix exalted reads in ~5 lines instead of ~16. **Hold Alt while hovering** and the full detail (ranges, craft info) returns live; release and it's clean again.

**Build it how you want** — three layouts (`BadgeLeft` default / `SignalRight` / `Trailing`), affix names in tier colour or the game's default, grade letters on or off, and pins to keep ranges/tier details permanently visible if Alt isn't your style.

### Tooltip Colours
Affix names are coloured by **crafting tier** (T1 gray → T7 mythic pink) and a **grade letter** shows how well the affix actually rolled within that tier (F = bottom of the range, S = near-perfect). Same tier, very different power — now you can see it at a glance.

**Since v2.0.0 this works correctly on legendaries.** v1 graded legendary affixes off reconstructed display values, which broke on Legendary Potential items (a max-rolled 12% Mana could grade C). v2+ reads the game's own stored roll bytes — the grade you see is the roll the game actually gave you.

### Ground Labels
Items on the ground show `[5A 3C 7S]` style brackets — tier number, grade letter, or both — so you can evaluate drops without hovering over everything. Uniques, sets and legendaries are deliberately left alone (Fallen Star's Improved Tooltips owns those, and does it better).

### Filter Rule Number
Hover any item and the tooltip shows **which loot filter rule matched it** — in orange-gold (T6's shade), e.g. `Rule#69`. Switch it to NumberAndName mode and you get the rule's name too ("Rule #69: Maxroll told me to pick this up blah blah"). You can also reposition EHG's native rule number on ground labels (start / end / default).

---

## Settings (cfg)

**On Last Epoch 1.4.7 the in-game settings rows for this mod do not build.** Edit the
cfg keys below directly in `UserData/medick_Terrible_Tooltips.cfg` with the game
closed, then relaunch.

| Cfg key | Default | What it does |
|---|---|---|
| EnableTooltips | true | Master switch — enables all tooltip colouring |
| TooltipTierColors | true | Colours affix names by crafting tier |
| TooltipRankColors | true | Colours grade letters by roll quality |
| TooltipLayout | BadgeLeft | Where the Tier·Grade signal sits: BadgeLeft / SignalRight / Trailing |
| SignalStyle | PlainText | PlainText = coloured text only (default); Badge = Tier/Grade as coloured chips |
| AffixNameColor | GreaterAffix | GreaterAffix = only Tier 6/7 text wears the greater-affix tint (default); TierColor = text wears its tier colour; GameDefault = the game's own text colour |
| ShowGradeLetters | true | The S/A/B/C/F grades — set false if you only want tiers |
| AlwaysShowRanges | false | Pin EHG's "Range: X to Y" lines permanently (default hidden, hold Alt to peek) |
| AlwaysShowTierDetails | false | Pin EHG's full "Tier: N (max craftable)" line (default folded in, hold Alt to peek) |
| GroundLabelStyle | TierAndRank | None / TierAndRank / TierOnly / RankOnly |
| GroundLabelFilterOnly | false | Only show ground labels on loot-filter highlighted items |
| GroundLabelAltKey | false | Hide ground brackets until you hold Alt (KG-style) |
| ShowFilterRuleNumber | NumberOnly | Off / NumberOnly / NumberAndName |
| LabelRulePosition | EHGDefault | Where EHG's filter rule number sits on the ground label: Start / End / EHGDefault |
| DebugLog | false | Verbose log output — turn on for a formatter trace when filing a bug report |
| GreaterAffixTint | `#C990FF` | The greater-affix tint colour applied to Tier 6/7 affix sentences |
| UnitBorder | true | Draw a thin border around the Tier·Grade unit under the text (PlainText style only) |
| TierWord | Spelled | Spelled ("Tier 7") or Compact ("T7") in the tooltip signal. Ground labels unaffected |
| UnitSeparator | Bar | Bar = "Tier 7 \| A" (default); Dot = "Tier 7·A". Ground labels unaffected |
| DividerStyle | Strip | Strip = full-height divider image (default); Glyph = show the selected \| or · glyph |
| BorderColorMode | Neutral | Neutral = the muted #5A4670 outline; TierColor = the tier colour at 60% alpha |
| BorderThickness | 2 | Border thickness in texels (clamped 1–4) |
| BorderPadX | 6 | Horizontal unit-border padding (clamped 2–12) |
| BorderPadY | 2 | Vertical unit-border padding base (clamped 0–6, plus 0.5 units) |
| BorderDebug | false | Draw the unit border in diagnostic magenta with a translucent centre and log every placement |

The in-game panel, where it does build, also includes colour-legend reference rows
(the tier ladder and the (PoG) S→F (RiP) grade ladder) so you never have to leave
the game to remember what purple means.

Settings persist to `UserData/medick_Terrible_Tooltips.cfg`.

---

## Tier Colours
| Tier | Colour |
|---|---|
| T1 | Gray |
| T2 | Light Gray |
| T3 | Green |
| T4 | Blue |
| T5 | Purple |
| T6 | Orange-Gold |
| T7 | Mythic Pink |
| T6–T7 affix sentence (default) | `#C990FF` Greater-Affix Tint |

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
