# 🟣➤🌙 Kimi — LATE blind seat, typography and hierarchy lens (2026-09-11, read-only)
sessionId 5f7233ec-9757-48e3-aaed-7b23ea1ec1ef · 💸 CREDITS (Moonshot · kimi-k3-high) · 43092 in / 6668 out
Blind to every design-2026-09-11-* file and to the other seats' text.

DONE

## Manifest (files actually read)
- `BRIEF.md` (full)
- `src/TooltipRecolor.cs` — lines 1–60, 700–811, 812–913 (composer, `ComposeCleanLine`, `ComposeUnbracketed`, `TierAnnotation`)
- `src/Colors.cs` (full)
- `src/FilterRuleTooltip.cs` (full)
- `docs/SPEC.md` — "v3 — THE CLEAN LINE" (line 130) and "Lean pass" (line 201) sections
- `mockups/tooltip-belt-v3-badges.html` (full)
- `mockups/tooltip-flow-v3.html` (full)
- No `design-2026-09-11-*` file opened. Nothing written, nothing run.

## 3. My read of the CURRENT look (honest, from code + mockups)

The shipped 3.0.2 line is `[Tier 5][A]  58% increased Lightning Damage` — two `<mark>` plates at 40% alpha with warm-white ink, then the affix name in tier color (TooltipRecolor.cs:842, 855, 898). Typographically this is *three competing signals per line*: plate color, ink text, and name color all say "T5." On a 4-affix idol that's **8 glowing rectangles stacked in 4 lines** — the chips become the loudest content on the tooltip, louder than the words they describe. The `<mark>` overdraw law (the quad paints OVER its glyphs, TooltipRecolor.cs:823–828) forces the pale-ink-on-translucent-plate look, which is exactly what washed to a white block under HDR capture last night. The gold `Rule#` at `<size=200%>` (FilterRuleTooltip.cs:117) is typographically shouting — it doubles the line height of whatever line it lands on and unbalances the whole card. The bones are right (one line per affix, kill the essay); the *hierarchy* is inverted: decoration currently outranks content.

## 4. Tag-level rulings (my lens, the core ask)

**Per-tag, inside this game's TMP:**
- `<color>` — the workhorse. Tier on the name OR the chip, never both at full strength (OPINION; today it's both, TooltipRecolor.cs:842+871).
- `<mark>` — chips only. Overdraw law (FACT, TooltipRecolor.cs:823–826, verified in-game 2026-06-11) caps plate alpha at ~55%; the mockup's own tuning strip says 70% drowns letters (FACT, tooltip-belt-v3-badges.html:98). Ink must stay light `#FFF6E6` — dark ink physically vanishes under the plate. Consequence: `Colors.BadgeTextColor()` (Colors.cs:66–76) is **dead code** — defined, never called (grep-confirmed), and its dark-ink `#141210` branch is unrenderable under `<mark>`. Recommend deleting or documenting it as PlainText-only.
- `<size>` — three sizes total, no more: chips 90%, body 100%, Alt detail 85%. Below ~80% the small-caps serif goes muddy at 1080p (OPINION). The 200% rule tag breaks this scale — see proposals.
- `<b>` — faux bold fattens strokes. **Helps:** nothing inside chips (stroke + overdraw = blob), arguably the Rule# at 110–120%. **Hurts:** affix names (counters fill in at tooltip size), grade letters, anything ≤90% size (OPINION, standard TMP faux-bold behavior on a serif face).
- `<i>` — reserve for flavor/hint text only; never in the signal.
- `<pos>` — SignalRight only, keep the 30-char fallback (FACT, TooltipRecolor.cs:887–893; char-count approximates width). Do not extend its use.
- `<space>`, `<cspace>`, `<voffset>`, `<alpha>`, `<nobr>` — **not on the proven tag list** (BRIEF.md hard facts). Treat as unverified in this TMP build; each needs a one-line in-game test before it touches the composer. `<nobr>` around the signal is the one worth testing first — it would guarantee a chip never splits across a wrap.

**Exact strings (BadgeLeft + Badge, defaults):**

Normal affix line (FACT — this is what ComposeCleanLine emits today):
```
<mark=#A807FF66><color=#FFF6E6>Tier 5</color></mark> <mark=#FA9E3D66><color=#FFF6E6>A</color></mark>  <color=#A807FF>58% increased Lightning Damage</color>
```
My proposed D1 revision (one plate, grade rides inside, name drops to bright-neutral so the chip is the only tier signal):
```
<mark=#A807FF66><color=#FFF6E6>Tier 5 </color><color=#FA9E3D>A</color></mark>  <color=#E8E2D2>58% increased Lightning Damage</color>
```
Sealed line (current, FACT from TooltipRecolor.cs:832, 898):
```
<mark=#8a847866><color=#FFF6E6>Sealed</color></mark> <mark=#DADADA66><color=#FFF6E6>Tier 1</color></mark> <mark=#A807FF66><color=#FFF6E6>B</color></mark>  <color=#DADADA>13% of Potion Health Converted to Ward</color>
```
Proposed sealed (Sealed demoted to dim italic text — it's a state, not a stat):
```
<mark=#DADADA66><color=#FFF6E6>Tier 1 </color><color=#A807FF>B</color></mark>  <color=#E8E2D2>13% of Potion Health Converted to Ward</color> <color=#8a8478><i>· sealed</i></color>
```
Implicit / grade-only (current, FACT — name borrows grade color, TooltipRecolor.cs:870):
```
<mark=#DADADA66><color=#FFF6E6>F</color></mark>  <color=#DADADA>+48 Armor</color>
```
Alt detail line (current emits dim `max craftable` and tier-colored `Range:` — FACT, TooltipRecolor.cs:810, 765). Proposed unified detail line:
```
<size=85%><color=#8a8478>  Range: 40% to 60% · rolled 58% · max craftable</color></size>
```
Separators: chips self-separate with a single space (FACT, TooltipRecolor.cs:877); plain-text mode needs the dim middot `<color=#8a8478>·</color>` (FACT, line 862). Never `—` and chips on the same line.

**Long affixes:** the worst real case (~53 chars, e.g. "10% reduced Bonus Damage Taken from Critical Strikes") plus two chips risks clipping; tight chips already mitigate (FACT, TooltipRecolor.cs:828 comment). Ruling: hold the line on one physical line via the reserve "T7" compact chip (mockup line 100–104) rather than allowing a wrap — TMP gives us no hanging indent, so a wrapped line restarts at x=0 *under* the chips and reads as a new, chipless affix (OPINION). If a wrap must happen, let it wrap and accept it; do not fake indents with `<pos>`.

**4-affix idol:** today = 8 plates. With the merged chip above = 4 plates, one per line, tier color doing the grouping down the left edge. **Two-stat affix (3.0.2: two lines, each with its own signal — FACT, SPEC.md:182–185):** keep per-line signals (each stat honestly has its own roll), but the repetition of identical tier chips IS the grouping cue; do not invent a continuation glyph the font may not have.

## 5. D1 / D2 / D3 from the typographic result

- **D1 (rich-text polish):** Concrete line: the merged-chip string above. Cost: hours; ships in a point release. Constraint hit: `<mark>` overdraw caps plate alpha at ~55% and forbids dark ink forever; no hanging indent; `<pos>` is a guess. CANNOT: rounded chips, real padding, a true right-aligned column, per-line background bars.
- **D2 (native-clone chips):** Concrete line: a real cloned pill with proper padding and *ink drawn over the plate* — dark-ink-on-bright-chip becomes possible for the first time, and HDR washout dies with the `<mark>` quad. Cost: days, medium risk; the NativeClone pattern is proven in the sibling mod (FACT, BRIEF.md). Constraint: cloned widgets must join EHG's layout group and survive its re-measure; the weaver purple bar still sits behind whatever we draw. CANNOT: reposition beyond row insertion, or escape the tooltip's own width logic.
- **D3 (custom overlay):** Concrete line: anything — real tracking, hanging indents, a two-column signal rail. Cost: weeks, highest risk (input, scaling, controller, mod coexistence, every patch). CANNOT: reuse EHG's item-text pipeline — you'd re-author affixes, implicits, uniques, weaver, forging potential, comparisons. Typographically perfect, economically absurd for the gain over D2.

## 6. Ranked recommendation

**D1 now → D2 for the chips specifically → D3 parked.** D1's merged chip + demoted name color + fixed Rule# size is a same-week release that inverts the hierarchy back to content-first. D2 is the correct permanent home for chips because it kills the overdraw law, which is the single ugliest constraint in the system. D3 buys typography we can get 90% of from D2 at 10% of the risk.

## 7. Concrete proposals (testable)

1. Merge tier+grade into ONE plate: `<mark={tierHex}66><color=#FFF6E6>Tier {n} </color><color={gradeHex}>{L}</color></mark>` — halves plate count; testable in the belt mockup in minutes. (OPINION)
2. Drop affix-name color to neutral `#E8E2D2` when Badge style is on; keep TierColor names only for PlainText style — kills the triple signal. (OPINION)
3. Rule# from `<size=200%>` (FACT, FilterRuleTooltip.cs:117) to `<size=120%><b>` gold — same prominence, no line-height explosion. (OPINION)
4. Alt detail lines at `<size=85%>` dim `#8a8478`, two-space indent, `·`-joined facts. (OPINION; Dim value is FACT, TooltipRecolor.cs:48)
5. Plate alpha test strip in-game at 30/40/50% under HDR capture before touching anything else — last night's white-block washout is an alpha problem, not a color problem. (OPINION grounded in FACT: overdraw law, TooltipRecolor.cs:823–826)
6. Delete or fence `Colors.BadgeTextColor` — dead code whose dark-ink branch is physically unrenderable under `<mark>`. (FACT: defined Colors.cs:66, zero call sites)
7. One-line in-game probe for `<nobr>` around the signal; adopt if the game's TMP honors it. (OPINION)
8. Sealed demoted from a chip to trailing dim italic `· sealed` — it's a state flag, not a stat. (OPINION)

## 8. Either/or ruling questions for Andrew

1. One merged chip per line (`Tier 5 A` on one plate) — or keep the two-chip look?
2. Badge style on: affix names go neutral-white so chips carry tier — or names stay tier-colored (double signal)?
3. Rule# at 120% bold gold — or keep the 200% shout?
4. Sealed as trailing dim `· sealed` text — or keep the full Sealed chip?

🟣➤🌙 Kimi

---
Conductor note (not seat text): Kimi's "grade letter in grade colour INSIDE a tier-coloured mark" puts coloured ink under a coloured plate; the in-game law only proves warm-white ink survives a 40% plate, so that string needs the same in-game test as any plate. Kimi's `BadgeTextColor` dead-code finding matches Astra's independent read.
