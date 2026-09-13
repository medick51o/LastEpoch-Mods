# Terrible Tooltips — design council synthesis v3 (2026-09-11 08:43, round three 08:47, conductor: Fable 5.1)

> **RULED 08:41 (before round two finished; verbatim in RULINGS-andrew.md, recorded by the team lead):** Andrew approved **D2 native-cloned pill** as the target look (bounded in-game PROBE next, staged only) with the **GREATER-AFFIX TINT**: T1–T5 sentences plain white, T6/T7 sentences `#C990FF` light purple (final 08:42), pill ink on the palette. D1 is the fallback if the probe fails. That settles Q1/Q2/Q5/Q8 below; the council's round-two convergence on the no-plate unit now stands as its recommendation for the FALLBACK's shape (the ruling's own wording suggests D1b/D1c "to approximate the pill"; the votes say those carry attacks and the no-plate unit does not). Still open for Andrew: Q4 spelled/compact, Q6 S★, Q7 rule size; Q5 grouping depends on the probe. **Round three 08:47** (signed\ROUND3-pill-on-low-tiers.md): pill reserved to T6/T7 vs pills everywhere = 3–1 (Composer, Kimi, GLM vs Cursor-Grok); same in-game A/B settles it; the probe carries a `PillTiers` switch.
v1 08:05 (three seats) → v2 08:40 (four late blind Cursor seats added on the boss's 07:44 order) → **v3: round two folded in** (every Cursor seat read v2 + the renders and voted/refuted; Kimi re-read the mockups; GLM audited colour load; Composer did the three-second player read). Signed reads verbatim in signed/ (SIGNED-* = blind round one, VOTE-* = round two). No file under src\ was touched; no game run.

## Seats and spend
| Seat | Model | Lens | Round 1 | Round 2 | Meter |
|---|---|---|---|---|---|
| 🔵 Astra (LEAD) | gpt-6-astra · thread 01a090ed… | code + constraints + all directions | blind | — | subscription |
| 🟢 Gemini | Antigravity (brain UNREPORTED) | what's on screen · mockup review | blind · 6/6 MATCH | — | subscription |
| 🟠 Claude Sonnet | sonnet subagents | player familiarity · Playwright render gate | blind · gate | — | subscription, DISCOUNTED (conductor's lineage) |
| 🔵 Codex (builder) | default · thread 01a090fa… | six mockups | — | — | subscription |
| 🟣➤⚫ Cursor-Grok | cursor-grok-4.6-high · 0d9350eb… | adversarial | blind (given the likely D1 as target) · 67523/12138 | vote, viewed 6 PNGs · 24100/4923 | ♾️ included |
| 🟣➤🎼 Composer | composer-2.5 · 81062378… / b2ebfa40… | fresh eyes · three-second read | blind, BRIEF only · 11174/3889 | three-second read on PNGs · 16933/3702; vote (3 PNGs viewed) · 18867/3649 | ♾️ included |
| 🟣➤🌙 Kimi | kimi-k3-high · 5f7233ec… / bb4e5abf… | typography · mockup review | blind · 43092/6668 | mockup review 6/6 MATCH (1 note) · 44842/5584; vote, viewed 6 PNGs · 29434/5019 | 💸 credits |
| 🟣➤🔷 GLM | glm-5.2-high · f84a29d9… (council) / 02c0ae6f… (audit) | colour-blind + overload · colour-load audit | blind · 19577/3645 | audit from markup · 4665/702; vote, HTML only (did not find the PNGs) · 21717/1627 | 💸 credits |
⚫ native Grok benched (hung twice last night). Cursor totals this mission: ♾️ included 138,597 in / 28,301 out · 💸 credits 163,247 in / 22,545 out. On-demand spending is disabled on the account, so no overage is possible. Packet: C:\Sync\Projects\tt-design-2026-09-11 (BRIEF.md md5 f37eef54…).

## Round-two tally on the ruling queue (Cursor seats voted; round-one positions of the other seats shown where they were explicit)
| Q | Question | Votes | Read |
|---|---|---|---|
| 1 | Plate or no plate | NO PLATE: Cursor-Grok, Composer, Kimi, GLM, Astra · PLATE: Gemini (dark), Sonnet (pill); render seat and Composer's three-second read liked D1c's look | **Strong no-plate.** Every seat that voted after seeing the renders chose no plate, two of them reversing their own blind proposal (Composer's merged chip, Kimi's merged plate) because a CSS render cannot show TMP overdraw |
| 2 | Sentence: tier colour or neutral | TIER COLOUR: Cursor-Grok, Composer, Kimi (its blind "neutral" was conditional on a plate) · NEUTRAL: GLM, Gemini, Astra (as default, colour kept as option) · VALUE ONLY: Sonnet | **3–3. Andrew's ruling.** ARCHAEOLOGY records the tier-coloured NAME as his capitalised genesis direction and `TierColor` is the shipped default; the default stands until he rules |
| 3 | If a plate: dark or tier-coloured | dark: Cursor-Grok, Gemini · tier: Composer, Kimi, GLM | moot while Q1 = no plate; decided only by the in-game alpha strip |
| 4 | "Tier 5" or "T5" | SPELLED: Cursor-Grok, Kimi, GLM, Astra · COMPACT: Composer, Gemini, Sonnet | spelled out by default, T5 as the wrap reserve (already parked in the June mockup) |
| 5 | Grade letter colour | GRADE COLOUR: Cursor-Grok, Composer, Kimi, Astra, Gemini, Sonnet · NEUTRAL: GLM | grade colour; GLM's one-hue rule recorded as the accessibility option |
| 6 | S ★ | NO: Cursor-Grok, Composer, Kimi · YES: GLM, Sonnet | no by default (font glyph unverified); F always keeps its letter (GLM's audit: hiding F is the one colour-blind failure in all six mockups) |
| 7 | Filter rule size | 120% bold gold: all four Cursor seats, Astra (normal size) | unanimous among voters |
| 8 | Next build | D1 ONLY: all four Cursor seats, Astra, Gemini, Sonnet | unanimous; D2 probe after 3.1.0 is in-hand; D3 parked |

## The finding that changes the ticket (Kimi, round two; Cursor-Grok said it in round one)
The winning line is what the shipped composer ALREADY emits with `SignalStyle=PlainText`, `Layout=BadgeLeft`, `NameColorMode=TierColor` (TooltipRecolor.cs:843, 856, 862, 878, 898):
```
<color=#A807FF>Tier 5</color><color=#8a8478>·</color><color=#FA9E3D>A</color>  <color=#A807FF>58% increased Lightning Damage</color>
```
So the core of 3.1.0 is a DEFAULT FLIP (`SignalStyle` Badge → PlainText), not a composer rewrite. Andrew can preview it tonight by changing one setting in the current 3.0.2 build. The rest of 3.1.0 is small: Rule# size, sealed treatment, Alt detail dimming, the two-space breathing room after the unit, dead-code removal.

## Where the seven agree (blind round one; round two did not move these)
| # | Finding | Seats |
|---|---|---|
| 1 | The palette is fine; the overload is how much SURFACE carries colour: two plates per line (8 on a 4-affix idol) on top of the coloured sentence | all seven. Cursor-Grok's round-two correction accepted: the sin is the two `<mark>` quads, not the coloured name |
| 2 | ONE signal unit per line | all seven |
| 3 | D1 ships first as a point release; D2 only after a real donor pill is found; D3 a separate product decision | all seven (Cursor-Grok's "D3 above D1" applies only to a D1 with a neutral sentence) |
| 4 | Number + letter are the colour-independent signal; never replace them with colour alone | Astra, GLM, Cursor-Grok, Kimi |
| 5 | No plate alpha has ever been measured; the first in-game step is an alpha strip under SDR and HDR before any plate returns | Astra, Kimi, Cursor-Grok, GLM |
| 6 | `Colors.BadgeTextColor` is dead code; its dark-ink branch is unrenderable under `<mark>` | Astra, Kimi, Cursor-Grok |
| 7 | Filter rule `<size=200%>` is out of scale | Astra, Kimi, all Cursor votes |
| 8 | Two-stat affixes keep one signal per stat line; the repeated unit IS the grouping cue; joining is a data problem | Astra, Kimi, Sonnet (Gemini alone wants a joined row) |
| 9 | Browser mockups are art direction, not renderer previews; the render gate's ranking (D1c > D1a > D1b) is evidence about CSS, not about TMP | Astra, Cursor-Grok, Kimi, Composer ("a tie, not a clear D1c win") |

## Splits that remain (named, not smoothed)
- **Q2 sentence colour, 3–3** (above). GLM's contrast arithmetic (T5 purple `#A807FF` ≈ 3.6:1 on the dark ground) is the best case for neutral; Cursor-Grok's "veiling or bleaching the epic colour is the wrong fix for a dim hue" is the best case for keeping it. Both renders exist (D1a panels 1 and 2).
- **Q5 grade colour**: GLM alone wants one hue per line; everyone else keeps grade colour. Cursor-Grok's point stands either way: grade hues ARE tier hues, so `T5·B` is one purple blob in any shape; the letter disambiguates.
- **Q4/Q6** minor, tallied above.
- Gemini's "dual mode" (plated gear, unplated idols/weaver) and joined multi-stat row: no other seat; moot under no-plate.
- HDR cause: Gemini asserts additive blending; Astra and Cursor-Grok say unproven. Test, don't assume.
- D3 and the treaties: Gemini "violates"; Astra and Cursor-Grok "must preserve, and can". Gemini overstates.

## Fairness corrections from round two (all accepted)
- Cursor-Grok: its vote is no-plate + TierColor (D4); its "D3 above D1" ranking applies only to a neutral-sentence D1. Not a vote for neutral.
- Composer: its blind shape was one merged `[5A]` chip with the grade inside (Kimi's family), not D1c.
- Kimi: its blind "neutral sentence" was conditional on a plate; with no plate it votes tier colour.
- GLM: the D1c hex double-count, the "D1b best fit" line and the "dark plate goes muddy / second dark layer on the weaver bar" attack are from GLM's separate AUDIT session (02c0ae6f…), not its council session; both are GLM. v2's prose attributed them without the session split.
- Sonnet: "colour on the numeric value only" is a third camp on Q2, not "keep tier colour".
- Render seat: its D1b description ("plate only behind the tier abbreviation") is wrong; the markup and three seats' pixels show one quad over the whole `T5 · A` unit.

## Corrections to seat text (from the files)
- Sonnet: "hybrid lines render bare" is the June mockup caption; 3.0.2 gives each stat its own bracket (AffixInjector.cs:67).
- Gemini: dim `#8a8478` is TooltipRecolor.cs:47, not Colors.cs. Kimi: same line is :47, not :48 (self-corrected).
- Composer: `#C8C8C8` / `#9A9A9A` are outside the frozen set (accepted); "50% alpha is HDR-safe" runs against the overdraw law (accepted).
- Kimi: coloured grade ink inside a tier plate is unproven (withdrawn by the seat).
- Cursor-Grok and Kimi (round two): the CSS mockups draw the plate ABOVE the glyphs (`.mark::after` z-index 2, confirmed by Gemini and Kimi's markup reads), not behind; their point that a browser cannot prove the engine's blending still holds.
- Astra, GLM (council): no factual errors found.

## Recommended path (one, with the reason)
**3.1.0 = D1 as a default flip: `SignalStyle` default Badge → PlainText, one coloured-text unit `Tier N · G` per line, no `<mark>` anywhere by default; sentence colour per Andrew's Q2 ruling (default `TierColor` stands until he speaks); Rule# to 120% bold gold; sealed as dim text; Alt detail at 90% dim; remove `BadgeTextColor`; Badge style kept as the knob for anyone who wants plates back.** Seven blind seats agreed on one unit; every seat that voted after seeing the renders chose no plate; the line already exists in the shipped composer, so the risk is a settings default and three small string edits. Then the in-game alpha strip decides whether any plate ever returns (Q3). D2 = a bounded donor probe after 3.1.0 is in-hand (Astra's exit criteria). D3 = parked. Ground labels untouched.

Conductor's own lean, stated so it can be discounted: v1 leaned dark plate, v2 leaned no-plate, v3 does not need a lean; the council converged on no-plate by itself. On Q2 I hold the shipped default (tier colour) because reversing a genesis ruling is Andrew's and the two candidates differ by one `<color>` tag.

## Ruling queue (one line each)
1. Signal unit: NO PLATE (coloured `Tier 5 · A`, council's pick) or a PLATE (then the alpha strip runs first)?
2. **Affix sentence: keep TIER COLOUR (your genesis ruling, today's default, 3 votes) or go NEUTRAL off-white (3 votes)?**
3. If a plate ever returns: DARK plate with coloured ink, or TIER plate with white ink?
4. "Tier 5" spelled out (4 votes), or compact "T5" (3)?
5. Grade letter in its GRADE colour (6), or NEUTRAL one-hue-per-line (GLM)?
6. S ★ decoration: no (3) or yes (2)? F always keeps its letter either way.
7. Filter rule number: 120% bold gold (unanimous), or keep 200%?
8. Next build: D1 only as 3.1.0 (unanimous), or add the D2 donor probe in the same cycle? D3 stays parked unless you say otherwise.

## What the seats say LE's UI cannot do for a text-rewrite mod
Astra's table (SIGNED-codex-astra.md §4) is the reference; Cursor-Grok's attacks 1–6 and Kimi's tag rulings add: no hanging indent (a wrapped line restarts under the unit and reads as a new affix), `<nobr>`/`<cspace>`/`<voffset>` unproven in this TMP build, no plate under glyphs ever, no weaver-bar control, no controller parity. What EHG would have to reimagine: first-class tier/roll fields on the row, stable multi-stat grouping, responsive columns, progressive detail, controller parity.
