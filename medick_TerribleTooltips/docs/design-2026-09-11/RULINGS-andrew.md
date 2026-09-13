# Andrew's rulings on the design deck — 2026-09-11 08:33 (verbatim + conductor reading)

Verbatim: "this one looks great [the D2 native-pill render] but i dont know if the ui or the game is even capable of doing this. i do miss the t6 and t7 stats being colored themselves like how the vanilla display shows anything above t5 --meaning t6 and up it would be a purple text by default gives the user of oh look legendary power but i agree anything between t1-and t5 should just be in white basic text"

Rulings (conductor reading, to be confirmed by Andrew where marked):
- R1 TARGET LOOK = D2 native-cloned pill (`Tier 5 | A` as a real UI pill with its own label, signal gutter, sentence beside it). Feasibility NOT PROVEN → the next build is a BOUNDED D2 PROBE (one cloned donor pill on one affix line, in-game, logged, fail-loud), not a full D2 build. D1 (one signal unit per line, same information layout) is the fallback if the probe fails, and its shape should visually approximate the D2 pill (D1b dark plate or D1c tier plate, whichever renders closer in-game).
- R2 SENTENCE COLOUR RULE: affix sentence is plain white for T1–T5; T6 and T7 sentences are COLOURED so "legendary power" reads at a glance. Sub-choice pending: colour = our palette (T6 #FA9E3D gold, T7 #FF44FF mythic pink, matching the pill ink) [conductor's default] OR the game's vanilla purple. Andrew to say "purple" if he wants vanilla's.
- R3 (implied) Grade letter stays on every line inside the pill (`Tier 5 | A`), as rendered in D2. Not yet ruled: S★/hide-F decoration (queue Q4), spelled vs compact tier (Q3), rule-number size (Q6).
- Open queue: Q3, Q4, Q5 (two-stat grouping — the D2 render groups them; only possible if the probe proves a per-affix container), Q6, Q8 (D3 stays parked unless he says otherwise).

## 08:37 — R2 resolved (verbatim): "anything t6 and t7 for text of the stats "30% COOLDOWN REDUCTION" should share the same color i think the vanilla has it as purple but i think we should set it a a light purple something different from what is actually listed on the tier list color pallette.. i seee we have a color for "SHOW" --we should make t6 and t7 text stats that color but level the actual TIER 6 AND TIER 7 AND THE ABC colors as the pallette we have in place"
- R2 FINAL: T6 and T7 affix SENTENCES share ONE colour: a LIGHT PURPLE that is NOT a palette colour (must read apart from T5 #A807FF and T7 #FF44FF). The pill/signal keeps the palette exactly: Tier 6 ink #FA9E3D, Tier 7 ink #FF44FF, grade letters F/C/B/A/S as today. T1–T5 sentences plain white.
- Fact: the mockup's "SHOW" is #FF44FF (T7 pink) styled italic+underline, which reads lighter on screen — so "the SHOW colour" as literally coded is a palette colour and would collide with the Tier 7 ink. Candidates for the shared light purple, to be rendered side by side for Andrew's eye: LP-A #C990FF (lavender) · LP-B #D8A5FF (paler lavender). Andrew picks by render; default LP-A until he does.

## 08:38 — intent (verbatim): "yes a light purple may fit better just enough to calmly signify these are your greater affixes or soething"
- Design intent for R2: the T6/T7 sentence colour is a CALM signal of "greater affixes", not a highlight. Prefer the quieter of LP-A/LP-B if both read apart from the pills; never bold, never a plate, no glow. Working name for the rule: GREATER-AFFIX TINT.
- 08:40 conductor-drawn mockup (Fable, design artifact not mod code): mockups\design-2026-09-11-D2-greater-affix-tint.html — D2 pill + R2 tint A/B for Andrew's eye; conductor's Codex render of the same rule still to come.

## 08:41 — DESIGN APPROVED (verbatim): "yes print it we will go with this design"
- Approved: D2 native-cloned pill layout (`Tier N | G` pill on the palette, sentence beside) + GREATER-AFFIX TINT for T6/T7 sentences, T1–T5 white. Tint shade not named → default LP-A #C990FF until Andrew says "right"/"B".
- Next build = TICKET-D2-PROBE (bounded: one cloned donor pill on one affix line, in-game, fail-loud, degrade to the text path if the donor is missing), Codex builds, Gemini reviews, STAGED ONLY — deploy waits for Andrew. D1 (one-signal-per-line, same colours, tint rule) is the fallback if the probe fails.

## 08:42 — shade CONFIRMED (verbatim): "sorry, meant to say left got to excited" → GREATER-AFFIX TINT = LP-A #C990FF, FINAL. LP-B retired.

## 21:43 — IN-GAME APPROVED (verbatim): "its works print it"
- The 3.1.0-dev build in the game (105,472 B, md5 b476c18938b2, deployed 21:35) is the approved look: PlainText unit inside a runtime-drawn 9-slice box (UnitBorder.cs), fuller padding (BorderPadX=6, BorderPadY=2), full-height divider STRIP between the tier cell and the grade cell (DividerStyle=Strip), neutral ring #8A74A8 @0.9 (BorderColorMode=Neutral), thickness 2, spelled "Tier N" (TierWord=Spelled), greater-affix tint #C990FF on T6/T7 sentences, T1–T5 white, rule number 120% bold, lone grade letters (implicits) boxed. Wrapped rows re-measured; multi-unit TMPs get one box per unit.
- Earlier today: 15:26 border invisible (Il2Cpp cast of parent RectTransform returned null → off-row placement; found via BorderDebug); 20:58 magenta ring proved the path; 21:16 Andrew asked for the divider bar and a fuller box; 21:19 sketch = split cells; 21:20 "fuller shape, not hugging".
- Superseded: PillProbe.cs (donor clone) stays in tree, off by default; delete at release. LP-B, D1b/D1c plates, D3 retired.
- To ship 3.1.0: Gemini cross-vendor review of DELTA fix1..fix4 (border-fix, geometry, divider, padding — none reviewed yet), version 3.1.0, CHANGELOG/README polish, zip, commit, push, Nexus text. Open cfg tastes: TierWord Compact, BorderColorMode TierColor, PillTiers concept dropped.
