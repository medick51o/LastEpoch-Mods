# 🟣➤🔷 GLM — LATE blind seat, colour-blind safety and overload lens (2026-09-11, read-only)
sessionId f84a29d9-35bc-48f6-a588-59c83de16174 · 💸 CREDITS (Zhipu · glm-5.2-high) · 19577 in / 3645 out
Blind to every design-2026-09-11-* file and to the other seats' text.

DONE_WITH_CONCERNS

## 1. Manifest
- `BRIEF.md`
- `src/Colors.cs` (full)
- `src/TooltipRecolor.cs` lines 47, 810–913 (composer + `Dim` const)
- `mockups/tooltip-belt-v3-badges.html`
- `mockups/tooltip-flow-v3.html`

## 2. Read of the CURRENT look (from code/mockups)
- Every affix line is built in `ComposeCleanLine` (TooltipRecolor.cs:813). Badge mode emits up to **three `<mark>` plates** at 40% alpha (`66` hex) with warm-white ink `#FFF6E6`: `Sealed`, `Tier N`, then a grade-letter chip per stat (TooltipRecolor.cs:840–861). FACT (TooltipRecolor.cs:828–861).
- The plate draws **over** its own glyphs (TooltipRecolor.cs:825–829); ink survives, dark text would drown. FACT.
- The affix **name** is recoloured to the tier hex by default (`TierColor` mode, TooltipRecolor.cs:868–872). So on one line the tier hue appears **twice** (chip + name) and the grade hue appears **once** (chip) — for a multi-stat affix, multiple grade chips stack. FACT.
- `mockups/tooltip-belt-v3-badges.html` shows a 4-affix belt rendering **8 chips** (one tier + one grade per affix, plus a Sealed chip on the sealed line) — the "4 lines of chips on a 4-affix idol" Andrew complained about is real. FACT (mockup:66–82).
- The palette **reuses five hues for two axes**: T3/T4/T5/T6/T7 = green/blue/purple/gold/pink and grades C/B/A/S = blue/purple/gold/pink; F shares T1 grey. FACT (Colors.cs:30–59).
- T1 `#DADADA` and T2 `#E1E1E1` differ by **7 units** in one channel — sub-just-noticeable on most panels. FACT (Colors.cs:31–32).

## 3. Colour-blind collapse (OPINION — simulation reasoning, not file-backed)

For each dichromacy, the pairs that **collapse to near-indistinguishable**:

- **Deuteranopia** (red-green gone, ~5% of men): T3 green `#16FF0E` ↔ T6 gold `#FA9E3D` collapse to a muddy yellow; grade A ↔ T3, grade S ↔ T6 also blur. T4 blue ↔ T5 purple drift closer (both read as blue-violet). T1↔T2 still separable (luminance only).
- **Protanopia** (red gone): same green↔gold collapse as deutan, slightly worse; T6 gold darkens and can read as brown next to T7 pink. T4↔T5 blue↔purple also compress.
- **Tritanopia** (blue-yellow gone, rare): T4 blue ↔ T5 purple collapse (both read as grey-pink); T6 gold ↔ T3 green compress; T7 pink stays distinct (it's magenta, red+blue). T1↔T2 still fine.

**Tier pairs that collapse:** T3↔T6 (deutan/protan), T4↔T5 (tritan, weak in deutan), T1↔T2 (everyone, by design — luminance only).
**Grade pairs that collapse:** C↔B (tritan), A↔C (deutan/protan, gold↔blue can blur), A↔S weak in protan, F↔T1 (same hex, intentional).

The cruellest one: **T5 purple = grade B purple = `#A807FF`** — the *same hex* means two axes. A T5/B affix and a T5/A affix differ only in the grade chip; if you can't see gold-vs-pink, the grade signal is gone *and* there's no second cue.

## 4. Non-colour redundancy that survives colour failure (FACT where cited)

- **The tier number** (`Tier 5`, `Tier 7`) — always present, always legible, fully colour-independent. FACT (TooltipRecolor.cs:843).
- **The grade letter** (F/C/B/A/S) — one glyph, colour-independent. FACT (TooltipRecolor.cs:854–859). This is the **single most important redundancy** and it is already in the build.
- **Position/layout** — BadgeLeft puts signal first (mockup:88–95), SignalRight right-aligns it (TooltipRecolor.cs:887–892). Position is a stable cue.
- **The plate itself** — a chip vs no-chip is a shape cue even when hue is gone (Sealed chip is dim grey regardless).
- **Size/weight** — currently *not* used to differentiate tier or grade; all chips same size. FACT (TooltipRecolor.cs:840–861). **This is an unused redundancy.**
- **Count** — multi-stat affixes already emit one grade chip per stat (TooltipRecolor.cs:852–859); the *number* of chips is a cue, but it's currently noise, not signal.

What is **missing**: no tier letter (only "Tier N"), no grade number, no size/weight step, no icon. Colour is currently load-bearing for tier *identity* (which tier 3 vs 4), not just tier *presence*.

## 5. Contrast (OPINION — my WCAG-ish math, sRGB→linear, #181222 bg L≈0.0074)

**As text on `#181222`:**
| hex | role | contrast | WCAG |
|---|---|---|---|
| #DADADA | T1/F | 13.1 | AAA |
| #E1E1E1 | T2 | 14.0 | AAA |
| #16FF0E | T3 | 13.4 | AAA |
| #77ACFF | T4/C | 7.95 | AAA |
| #A807FF | T5/B | **3.60** | **fails AA normal**, passes AA-large only |
| #FA9E3D | T6/A | 8.73 | AAA |
| #FF44FF | T7/S | 6.55 | AA |

**T5/B purple `#A807FF` is the weak leg** — it's the only tier colour that fails AA normal text contrast on the dark bg. When it's used as the *name* colour (TierColor mode) on a long affix line, readability drops. FACT that the hex is used for names (TooltipRecolor.cs:870); OPINION that 3.6 is marginal.

**As a 40%-alpha plate over `#FFF6E6` ink** (plate blends over ink, then that sits on dark bg — the actual chip readout): the ink dominates, so **every chip stays >11:1 readable** (range 11.7–15.8). The plate only *tints* the visible text. The chip's distinguishability therefore comes from the **plate hue against the surrounding dark tooltip**, not from text contrast. Approx plate-vs-dark-bg contrast at 40%: T1 5.8, T2 6.2, T3 5.9, T4 3.8, T5 **2.0**, T6 4.1, T7 3.2. **T5 purple plate is nearly invisible at 40% on dark** — the worst chip by far, and it's also the one that doubles as grade B.

## 6. Minimum colour set per affix line (OPINION — my proposal)

Rule: **one hue per line, carried by the tier chip only.** Everything else stays neutral.

- **Tier chip** = tier hue (1 colour). Carries tier identity.
- **Grade** = letter only, **neutral ink `#E8E2D2`**, no grade colour, no grade chip. The letter is the signal; colour is dropped.
- **Affix name** = neutral `#E8E2D2` (NOT tier colour). Removes the second copy of the tier hue.
- **Sealed** = dim `#8A8478` text, no chip (shape/word is the cue).
- **Separator** = dim `#8A8478` `·`.

Per line: **1 colour** (the tier hue) + 2 neutrals. Down from today's 2–3 colours/line.

**4-affix rule (keep the count low, keep it distinguishable without colour):**
1. Tier identity → **the number** ("Tier 5", "T7"). Colour is a bonus, never the sole cue.
2. Grade identity → **the letter** (F/C/B/A/S), always neutral ink, always present. No grade colour anywhere.
3. At most **4 tier hues visible** on a 4-affix item (one per line); if two affixes share a tier, the chip colour repeats — that's fine, the *number* disambiguates.
4. T1/T2 (near-identical greys) → **never rely on hue**; the number is the only cue and that's acceptable since both are "low" tiers. Optionally bump T2 plate alpha to 55% so luminance differs (FACT: alpha is a tunable dial, mockup:92–99).

**Exact rich-text for a normal line under this rule** (BadgeLeft, tier-coloured chip, neutral everything else):

```
<mark=#A807FF66><color=#FFF6E6>Tier 5</color></mark>  <color=#E8E2D2>58% increased Lightning Damage</color> <color=#8A8478>·</color> <color=#E8E2D2>A</color>
```

One hue (`#A807FF`), one neutral body (`#E8E2D2`), one dim separator (`#8A8478`). Grade "A" reads in neutral — letter carries it, no gold needed. A colour-blind player sees: a purple-tinted "Tier 5" chip, white "58% increased Lightning Damage", dim dot, white "A". Tier from the number, grade from the letter, both intact.

## 7. D1 / D2 / D3 through this lens

**D1 — Rich-text polish:** This is where my rule lives. Dropping the grade chip and the name tint is a **one-line code change** in `ComposeCleanLine` (delete the grade `<mark>` branch at TooltipRecolor.cs:857–858, drop the name recolour at :870–872). It directly cuts the chip count in half (8→4 on a 4-affix item) and removes the T5/B hex collision from the visible surface. **Cannot do**: give T5/B a second cue (size/weight) without more layout work; the `<mark>` quad has no border/outline, so a "failing hue" chip can't be rescued by an outline in pure rich text. FACT constraint (TooltipRecolor.cs:825–829, `<mark>` is a flat quad).

**D2 — Native-clone chips:** A real cloned game badge could carry **border + fill + icon**, giving T5/B a luminance border that survives colour failure. This is the only direction that adds a *shape* redundancy the flat `<mark>` cannot. **Cannot do**: nothing for colour-blind safety that D1+letter doesn't already do cheaper; the win is aesthetic, not accessibility. FACT that NativeClone is proven (BRIEF.md:7).

**D3 — Custom overlay:** Full freedom → could draw tier as **icon + number + colour**, grade as **bar length + letter**, the gold-standard of colour-blind redundancy. **Cannot do**: justify its risk for accessibility alone — the letter already covers grade, the number already covers tier. Overkill for this lens. FACT it's the Tier C risk path (BRIEF.md:8).

## 8. Ranked recommendation
1. **D1, with the one-hue-per-line rule.** Biggest colour-overload reduction for the smallest cost; keeps every signal colour-independent via the existing number + letter. The T5/B contrast problem is *removed* (purple no longer used as body text) rather than *fixed*.
2. **D2** only if Andrew wants the chips to look like real UI — not an accessibility need.
3. **D3** not warranted by this lens.

## 9. Concrete proposals (FACT/OPINION marked)

1. **Drop the grade chip; keep only the grade letter in neutral ink.** OPINION. Cuts chip count 50%, removes the T5/B hex double-use from chips.
2. **Drop the tier-coloured affix name; render names in `#E8E2D2`.** OPINION. Removes the second copy of the tier hue per line; the tier chip is the only tier-colour surface. FACT that name tint is today's default (TooltipRecolor.cs:868–872).
3. **Always show the grade letter, even when `ShowGradeLetters` is off — make it the accessibility floor.** OPINION. The letter is the only grade cue that survives all dichromacies.
4. **Bump T2 plate alpha to ~55% (T1 stays 40%) so T1/T2 differ by luminance, not hue.** OPINION. FACT that alpha is already a tuned dial (mockup:92–99) and the engine tolerates ≤55% before ink drowns (mockup:99 note).
5. **For T5/B specifically, add a `<b>` bold to the grade letter** so the worst-contrast, worst-colliding grade has a weight cue. OPINION. FACT that `<b>` is an allowed tag (BRIEF.md:7).
6. **Never use `#A807FF` as body text** — only as a chip plate. FACT it fails AA normal at 3.6:1 on the dark bg; OPINION that this should be a rule.
7. **Add a tier *letter* option (T1→"F-tier"? no) — actually, keep "Tier N" spelled out; the number is sufficient and unambiguous.** OPINION, no change needed.

## 10. Either/or ruling questions for Andrew
1. Grade colour: **drop it entirely** (letter-only, neutral ink) **or keep it** on the letter only (no grade chip)?
2. Affix name colour: **neutral `#E8E2D2` for all** or **keep tier-coloured names** (the WoW retina read)?
3. T1/T2 disambiguation: **accept that the number is the only cue** or **differentiate by plate alpha** (40% vs 55%)?
4. T5/B purple: **ban it as body text** (chip-only) or **allow it on names** despite 3.6:1 contrast?

🟣➤🔷 GLM

---
Conductor note (not seat text): GLM's contrast numbers are its own arithmetic (OPINION as labelled), not measured; the T5 purple weakness on a dark ground is consistent with Astra's "purple text on dark/purple backgrounds" caution. GLM's "always show the grade letter even when ShowGradeLetters is off" would override a shipped user setting; the deck records it as a proposal, not a rule.
