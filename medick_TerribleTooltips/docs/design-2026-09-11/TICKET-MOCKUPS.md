# TICKET — design mockups 2026-09-11 (builder: 🔵 Codex, workspace-write)

WRITE SET (absolute, nothing else): `mockups\design-2026-09-11-*.html` under C:\Users\andre\Downloads\LastEpoch-Mods\medick_TerribleTooltips. Do NOT touch src\, docs\, README, CHANGELOG, or the two existing mockups. No build, no game, no git.

## Goal
Six self-contained HTML files (no external assets, no JS needed) that reproduce the Last Epoch item tooltip at REAL SIZE on a dark game-like background so Playwright can screenshot them at 1x and 2x. Every file shows the SAME three items so variants compare fairly:
- Item A: "PLATED BELT" (Belt), implicits `+48 Armor` (grade F) and `+3 Potion Slots` (grade C), forging potential 0, affixes: `58% increased Lightning Damage` (T5, grade A), `28% increased Mana Regeneration` (T4, grade C), `30% increased Cooldown Recovery Speed` (T7, grade C), a sealed affix `13% of Potion Health Converted to Ward` (Sealed, T1, grade B); filter line "Filter Rule: SHOW Wanted Tier 7 (Can Edit Affixes & Item Type)", footer "Rule#69" in gold #FA9E3D and "Level 63".
- Item B: "ORNATE IDOL" (Grand Idol) with FOUR affixes: `+15% Fire Damage` (T5, grade S), `+3% Physical Penetration` (T1, grade F), `+3% Minion Physical Penetration` (T1, grade F — this is the second stat of the SAME affix as the previous line; 3.0.2 gives each stat its own signal), and a WEAVER affix `+2 to Level of Fire Skills` (T3, grade B) which sits on the game's own translucent purple highlight bar (draw a full-width bar rgba(120,60,180,0.35) behind that row — this is EHG's, not ours).
- Item A again under HOLD-ALT deep view: under each affix add the dim secondary lines `max craftable` and `Range: 40% to 60%` (the range in the grade colour), size 90%.

Tooltip frame (same in every file): 520px wide, background linear-gradient(180deg,#241c30,#181222 55%,#15101e), 1px border #4a3a5e, 17px serif (Georgia), line-height 1.85, small-caps title. Take the frame CSS from `mockups\tooltip-belt-v3-badges.html` — it is the established reference — but do NOT copy its chip CSS (see the TMP law below).

FROZEN PALETTE (hex only, never change): T1 #DADADA · T2 #E1E1E1 · T3 #16FF0E · T4 #77ACFF · T5 #A807FF · T6 #FA9E3D · T7 #FF44FF; grades F #DADADA · C #77ACFF · B #A807FF · A #FA9E3D · S #FF44FF; dim #8A8478; neutral affix ink #E8E2D2; chip ink #FFF6E6; rule gold #FA9E3D.

THE TMP LAW (approximate it honestly in D0 and D1 files): in this game TextMeshPro's `<mark>` draws its coloured quad ON TOP of the glyphs, with no padding and square corners. So a "chip" must be built as: the text, then an absolutely positioned overlay quad of the plate colour at the stated alpha covering the text's box (z-index above the text). Not a CSS background behind the text. Plate alpha in hex `66` = 40%, `40` = 25%.

Every file gets: a one-line caption at the top naming the variant and which seat proposed it; under each tooltip a `<pre>` block with the EXACT TMP rich-text string the composer would emit for the first affix line (given in each variant below); a bottom strip showing the 7 tier swatches and 5 grade swatches; and a small "renderer note" line saying whether chips are drawn with the overdraw approximation (D0/D1) or as design intent (D2/D3). Page body padding 24px, total page height under 1400px at 1x is fine; the three tooltips may sit side by side in a flex row that wraps.

## The six files

### design-2026-09-11-D0-current.html — "Current 3.0.2 (BadgeLeft + Badge)"
Faithful to today's composer: two chips per line, both 40% alpha over the ink, then the affix text in the TIER colour. Sealed line = three chips (dim Sealed, Tier 1, B). Implicits = grade chip only, name in grade colour. Idol: each of the 4 lines with its two chips (8 chips), weaver line chips drawn over the purple bar.
First-line string: `<mark=#A807FF66><color=#FFF6E6>Tier 5</color></mark> <mark=#FA9E3D66><color=#FFF6E6>A</color></mark>  <color=#A807FF>58% increased Lightning Damage</color>`

### design-2026-09-11-D1a-plain-signal.html — "D1a: no plate, coloured signal, neutral sentence (Astra)"
No chips. `Tier 5 · A  58% increased Lightning Damage` with "Tier 5" in T5 colour, "·" dim, "A" in grade colour, sentence in #E8E2D2. Sealed: `Sealed` dim at 90% size before the signal. Implicit: `F  +48 Armor` (grade letter only). Show Item A twice: once with neutral sentence, once with the tier-coloured sentence (label the second "name-colour option").
First-line string: `<color=#A807FF>Tier 5</color> <color=#8A8478>·</color> <color=#FA9E3D>A</color>  58% increased Lightning Damage`

### design-2026-09-11-D1b-dark-chip.html — "D1b: one dark plate, WoW colour on the ink (Gemini)"
ONE chip per line: a black plate at 40% (`#00000066`) drawn over the text `T5 · A` where T5 is in the tier colour, · dim, A in the grade colour; sentence neutral. Sealed: `Sealed` dim text before the chip (no plate). Implicit: chip containing only the grade letter. Idol: same chip on every line INCLUDING the weaver line (the dark plate over the purple bar must be visible so the deck can judge it).
First-line string: `<mark=#00000066><color=#A807FF>T5</color> <color=#8A8478>·</color> <color=#FA9E3D>A</color></mark>  58% increased Lightning Damage`

### design-2026-09-11-D1c-tier-plate.html — "D1c: one tier-coloured plate at 25%, grade unplated (Astra #2 / Sonnet #1)"
ONE plate in the TIER colour at 25% alpha over white ink `Tier 5`, then the grade letter unplated in its grade colour, sentence neutral. Add Sonnet's extreme-only decoration: S-grade lines get a single ★ after the letter, in the S colour; F lines get no letter at all (show the idol's F lines that way, and caption it "grade-on-extremes-only option").
First-line string: `<size=90%><mark=#A807FF40><color=#FFF6E6>Tier 5</color></mark></size> <color=#FA9E3D>A</color>  58% increased Lightning Damage`

### design-2026-09-11-D2-native-pill.html — "D2 concept: native cloned pill (design intent, not TMP)"
Rounded pill (border-radius 4px, 1px border #6a5a80, background #2a2238, real padding 0 6px, height matching the line) containing `Tier 5 | A` with Tier 5 in the tier colour, | dim, A in the grade colour; sentence neutral. A real left gutter so the pills form a column and the sentences align. Idol: the two stats of the shared affix get ONE pill spanning both rows (design intent for grouping). Caption must say: "requires a donor sprite in the game's tooltip hierarchy; not proven".
First-line string: `(pill = cloned Image + TMP label; affix TMP text) 58% increased Lightning Damage` with a note that the tier/grade live in the pill's own TMP: `<color=#A807FF>Tier 5</color> <color=#8A8478>|</color> <color=#FA9E3D>A</color>`

### design-2026-09-11-D3-overlay.html — "D3 concept: custom overlay panel (design intent)"
Two-column layout: sentence left (wraps independently), right-aligned signal column `Tier 5   A` in colours; grouped multi-stat affix shares one signal; an Alt detail region under the belt with ranges in a dimmed sub-row; item name in the rarity colour with a WoW-style coloured border glow on the panel (the Sonnet "WoW colours the name" note). Caption: "own canvas over the game's; weeks of work; must prove parity before hiding the native tooltip".
First-line string: `(overlay: two TMPs per row; not a rewrite of EHG's string)`

## Done-when
Six files exist, each opens standalone in a browser, each contains the three items, the caption, the `<pre>` string, the swatch strip, the renderer note. Reply with the file list and, per file, one line on any place you had to deviate from this ticket and why.
