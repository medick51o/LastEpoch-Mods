# 🟣➤🔷 GLM — colour-load audit of the six mockups, from the markup (2026-09-11)
sessionId 02c0ae6f-3790-4a7d-95e2-256c160555bd · 💸 CREDITS (Zhipu · glm-5.2-high) · 4665 in / 702 out

# COLOUR-LOAD AUDIT — six HTML mockups

Read-only. All counts taken directly from the markup. Hex values counted as distinct full strings (so `#A807FF66` ≠ `#A807FF`). The EHG weaver bar `rgba(120,60,180,0.35)` is not a hex, so excluded from (1) but noted in (2).

## D0 — current (BadgeLeft + Badge)

**Panel 1 — Plated Belt**
- (1) Distinct hex on affix/implicit lines: **10** — `#DADADA66 #DADADA #77ACFF66 #77ACFF #A807FF66 #A807FF #FA9E3D66 #FF44FF66 #FF44FF #8A847866` [FACT]
- (2) Plates drawn: **11** (2 implicits ×1, 3 affixes ×2, sealed ×3) [FACT]
- (3) Colour signals per line: **3/3/3/3/3/3** (every line: plate + signal-text + sentence) [FACT]
- (4) Colour-blind failures: **none** — every line carries literal text ("Tier 5","A","F","C","Tier 1","B","Sealed") [FACT]
- (5) Worst contrast: **T5 sentence `#A807FF` on `#181222`** (lowest-luminance tier colour used as sentence ink) [OPINION]

**Panel 2 — Ornate Idol**
- (1) Distinct hex: **7** — `#A807FF66 #A807FF #FF44FF66 #DADADA66 #DADADA #16FF0E66 #16FF0E` [FACT]
- (2) Plates: **8 ours + 1 EHG weaver bar** [FACT]
- (3) Signals per line: **3/3/3/3** [FACT]
- (4) Colour-blind failures: **none** (all lines have text) [FACT]
- (5) Worst contrast: **T5 sentence `#A807FF` on `#181222`** [OPINION]

**Panel 3 — Belt under Alt**
- (1) Distinct hex: **10** (same set as Panel 1) [FACT]
- (2) Plates: **11 ours** [FACT]
- (3) Signals per line: **3/3/3/3/3/3** [FACT]
- (4) Colour-blind failures: **none** [FACT]
- (5) Worst contrast: **T5 sentence `#A807FF` on `#181222`** [OPINION]

## D1a — plain signal (no plate)

**Panel 1 — Plated Belt**
- (1) Distinct hex: **7** — `#DADADA #77ACFF #A807FF #FA9E3D #FF44FF #8A8478 #E8E2D2` [FACT]
- (2) Plates drawn: **0** [FACT]
- (3) Signals per line: **2/2/3/3/3/3** (implicits have signal-text + sentence only, no plate; affixes add the `·` dim as a 3rd but sentence is neutral `#E8E2D2`) [FACT]
- (4) Colour-blind failures: **none** — "Tier 5","A","F","C","Tier 1","B" all present as text [FACT]
- (5) Worst contrast: **dim `·` `#8A8478` on `#181222`** (separator is the lowest-luminance intentional element) [OPINION]

**Panel 2 — Ornate Idol**
- (1) Distinct hex: **6** — `#DADADA #A807FF #FF44FF #16FF0E #8A8478 #E8E2D2` [FACT]
- (2) Plates: **0 ours + 1 EHG weaver bar** [FACT]
- (3) Signals per line: **3/3/3/3** [FACT]
- (4) Colour-blind failures: **none** [FACT]
- (5) Worst contrast: **dim `·` `#8A8478` on `#181222`** [OPINION]

**Panel 3 — Belt under Alt (name-colour option)**
- (1) Distinct hex: **7** — adds `#A807FF` reused as sentence colour (t5) on top of the D1a set; sentences now tier-coloured [FACT]
- (2) Plates: **0** [FACT]
- (3) Signals per line: **3/3/3/3/3/3** (sentence now carries tier colour too) [FACT]
- (4) Colour-blind failures: **none** [FACT]
- (5) Worst contrast: **T5 sentence `#A807FF` on `#181222`** (name-colour option re-introduces the low-lum purple sentence) [OPINION]

## D1b — dark chip (one black plate)

**Panel 1 — Plated Belt**
- (1) Distinct hex: **8** — `#00000066 #DADADA #77ACFF #A807FF #FA9E3D #FF44FF #8A8478 #E8E2D2` [FACT]
- (2) Plates drawn: **6** (2 implicits ×1, 3 affixes ×1, sealed ×1) [FACT]
- (3) Signals per line: **3/3/3/3/3/3** (plate + signal-text + neutral sentence) [FACT]
- (4) Colour-blind failures: **none** — "T5","T4","T7","A","C","F","B","Sealed","T1" all present as text [FACT]
- (5) Worst contrast: **`#00000066` plate over `#DADADA` "T1/F" ink** — black 40% over near-white drops the T1/F signal to a muddy grey on the dark tooltip bg [OPINION]

**Panel 2 — Ornate Idol**
- (1) Distinct hex: **7** — `#00000066 #DADADA #A807FF #FF44FF #16FF0E #8A8478 #E8E2D2` [FACT]
- (2) Plates: **4 ours + 1 EHG weaver bar** (one per line, including weaver) [FACT]
- (3) Signals per line: **3/3/3/3** [FACT]
- (4) Colour-blind failures: **none** [FACT]
- (5) Worst contrast: **`#00000066` plate over `#DADADA` "T1/F" ink** (both F lines) [OPINION]

**Panel 3 — Belt under Alt**
- (1) Distinct hex: **8** (same set as Panel 1) [FACT]
- (2) Plates: **6** [FACT]
- (3) Signals per line: **3/3/3/3/3/3** [FACT]
- (4) Colour-blind failures: **none** [FACT]
- (5) Worst contrast: **`#00000066` plate over `#DADADA` "T1/F"** [OPINION]

## D1c — tier plate (25%, grade unplated)

**Panel 1 — Plated Belt**
- (1) Distinct hex: **8** — `#A807FF40 #DADADA40 #77ACFF40 #FF44FF40 #FFF6E6 #DADADA #77ACFF #FA9E3D #A807FF #8A8478 #E8E2D2` — recount: `#A807FF40 #77ACFF40 #FF44FF40 #DADADA40 #FFF6E6 #FA9E3D #77ACFF #A807FF #DADADA #8A8478 #E8E2D2` = **11** [FACT]
- (2) Plates drawn: **4** (3 affixes ×1 tier plate + sealed ×1 tier plate; implicits have no plate — F implicit has no chip at all, C implicit has no chip) [FACT]
- (3) Signals per line: **2/2/3/3/3/3** (F implicit: sentence only; C implicit: signal-text + sentence; affixes: plate + signal-text + sentence) [FACT]
- (4) Colour-blind failures: **the F implicit line `+48 Armor`** — no grade letter shown at all (Sonnet's "F lines get no letter" rule), so the grade is conveyed by colour alone (the `gF` `#DADADA` sentence). Also the two F affixes on the idol panel. [FACT]
- (5) Worst contrast: **`#FFF6E6` "Tier 1" under `#DADADA40` plate on `#181222`** — 25% near-white plate over near-white ink on dark bg leaves a faint grey signal [OPINION]

**Panel 2 — Ornate Idol**
- (1) Distinct hex: **8** — `#A807FF40 #DADADA40 #16FF0E40 #FFF6E6 #DADADA #FF44FF #A807FF #8A8478 #E8E2D2` = **9**; recount: `#A807FF40 #DADADA40 #16FF0E40 #FFF6E6 #FF44FF #A807FF #DADADA #8A8478 #E8E2D2` = **9** [FACT]
- (2) Plates: **4 ours + 1 EHG weaver bar** [FACT]
- (3) Signals per line: **3/2/2/3** (the two F lines have no grade letter → signal-text missing) [FACT]
- (4) Colour-blind failures: **both F lines** (`+3% Physical Penetration`, `+3% Minion Physical Penetration`) — no "F" letter shown, grade is colour-only [FACT]
- (5) Worst contrast: **`#FFF6E6` "Tier 1" under `#DADADA40` plate** [OPINION]

**Panel 3 — Belt under Alt**
- (1) Distinct hex: **11** (same set as Panel 1) [FACT]
- (2) Plates: **4** [FACT]
- (3) Signals per line: **2/2/3/3/3/3** [FACT]
- (4) Colour-blind failures: **F implicit `+48 Armor`** (no grade letter) [FACT]
- (5) Worst contrast: **`#FFF6E6` "Tier 1" under `#DADADA40` plate** [OPINION]

## D2 — native pill (design intent)

**Panel 1 — Plated Belt**
- (1) Distinct hex: **7** — `#DADADA #77ACFF #A807FF #FA9E3D #FF44FF #8A8478 #E8E2D2` (pill bg `#2a2238` and border `#6a5a80` are pill chrome, not affix-signal colour; excluded as "plate" chrome akin to frame) [FACT + OPINION on the exclusion]
- (2) Pills drawn: **6** (2 implicits, 3 affixes, 1 sealed) [FACT]
- (3) Signals per line: **2/2/3/3/3/3** (implicits: signal-text + sentence; affixes: pill + signal-text + neutral sentence) [FACT]
- (4) Colour-blind failures: **none** — "F","C","Tier 5","A","Tier 4","C","Tier 7","Tier 1","B","Sealed" all present [FACT]
- (5) Worst contrast: **`#DADADA` "F" inside `#2a2238` pill on `#181222`** — light grey on near-black pill, but the pill bg is only slightly lighter than the tooltip bg, so the pill edge does most of the work [OPINION]

**Panel 2 — Ornate Idol**
- (1) Distinct hex: **6** — `#DADADA #A807FF #FF44FF #16FF0E #8A8478 #E8E2D2` [FACT]
- (2) Pills: **3 ours + 1 EHG weaver bar** (the two shared-affix stats share ONE pill) [FACT]
- (3) Signals per line: **3/3/3/3** (shared pill spans 2 rows but counts once per logical affix) [FACT + OPINION]
- (4) Colour-blind failures: **none** [FACT]
- (5) Worst contrast: **`#DADADA` "F" in `#2a2238` pill on `#181222`** [OPINION]

**Panel 3 — Belt under Alt**
- (1) Distinct hex: **7** (same set as Panel 1) [FACT]
- (2) Pills: **6** [FACT]
- (3) Signals per line: **2/2/3/3/3/3** [FACT]
- (4) Colour-blind failures: **none** [FACT]
- (5) Worst contrast: **`#DADADA` "F" in `#2a2238` pill on `#181222`** [OPINION]

## D3 — custom overlay (design intent)

**Panel 1 — Plated Belt**
- (1) Distinct hex: **7** — `#DADADA #77ACFF #A807FF #FA9E3D #FF44FF #8A8478 #E8E2D2` plus rarity glow `#A807FF`/`#A807FF40` on the panel border/title (excluded as frame/title) [FACT]
- (2) Plates/pills drawn: **0** (signals are bare coloured text in a right column, no plate) [FACT]
- (3) Signals per line: **2/2/3/3/3/3** (implicits: signal-text + sentence; affixes: signal-text tier + signal-text grade + neutral sentence) [FACT]
- (4) Colour-blind failures: **none** — "F","C","Tier 5","A","Tier 4","Tier 7","Tier 1","B","Sealed" all present [FACT]
- (5) Worst contrast: **T5 signal `#A807FF` on `#181222`** (right-aligned "Tier 5" in purple on dark) [OPINION]

**Panel 2 — Ornate Idol**
- (1) Distinct hex: **6** — `#DADADA #A807FF #FF44FF #16FF0E #8A8478 #E8E2D2` (rarity glow `#FF44FF`/`#FF44FF40` excluded as title/frame) [FACT]
- (2) Plates/pills: **0 ours + 1 EHG weaver bar** [FACT]
- (3) Signals per line: **3/3/3/3** (grouped signal spans the two shared-affix rows) [FACT + OPINION]
- (4) Colour-blind failures: **none** [FACT]
- (5) Worst contrast: **T5 signal `#A807FF` on `#181222`** [OPINION]

**Panel 3 — Belt under Alt**
- (1) Distinct hex: **7** (same set as Panel 1) [FACT]
- (2) Plates/pills: **0** [FACT]
- (3) Signals per line: **2/2/3/3/3/3** [FACT]
- (4) Colour-blind failures: **none** [FACT]
- (5) Worst contrast: **T5 signal `#A807FF` on `#181222`** [OPINION]

---

## Ranking — total colour load (lowest first) [OPINION, derived from FACT counts]

Scoring each file by (distinct hex across 3 panels) + (plates/pills across 3 panels, ours only):

| Rank | File | Hex total (sum of 3 panels) | Plates/pills total | Load verdict |
|---|---|---|---|---|
| 1 | **D1a** | 7+6+7 = **20** | 0 | lowest |
| 2 | **D2** | 7+6+7 = **20** | 6+3+6 = 15 | low hex, but 15 pills |
| 3 | **D3** | 7+6+7 = **20** | 0 | low hex, no plates |
| 4 | **D1b** | 8+7+8 = **23** | 6+4+6 = 16 | mid |
| 5 | **D1c** | 11+9+11 = **31** | 4+4+4 = 12 | high hex, low plates |
| 6 | **D0** | 10+7+10 = **27** | 11+8+11 = 30 | highest plate count |

D1a / D2 / D3 tie on raw hex (20). Breaking the tie by plates drawn: **D1a (0 plates) < D3 (0 plates, but adds rarity glow + 2-column chrome) < D2 (15 pills) < D1b < D0 < D1c** by hex. Final order: **D1a ≈ D3 < D2 < D1b < D0 < D1c**. [OPINION]

## Ranking — colour-blind safety (safest first) [OPINION]

- **Safest (no failures):** D0, D1a, D1b, D2, D3 — every tier and grade is present as literal text/number/letter on every line.
- **Fails:** D1c — the F-grade lines (idol's two penetration stats, and the belt's F implicit) carry no letter at all; grade is colour-only.

Order: **D0 = D1a = D1b = D2 = D3 (safe) > D1c (fails)**.

## Three lines — which D1 variant best fits "clear but colorful, not overwhelmed, WoW palette kept" [OPINION]

1. **D1b (dark chip) best fits the brief from a colour-load standpoint:** it keeps every WoW tier/grade colour on the ink (palette preserved), uses a single neutral black plate per line (no second colour introduced), sentences stay neutral `#E8E2D2`, and every signal is text-backed so it stays colour-blind safe.
2. **D1a is lighter still** (zero plates) but reads as "plain" rather than "colorful" — the colour is only on the tiny `Tier 5 · A` signal, so the WoW palette is present but under-sold.
3. **D1c is the most decorative** (tier-coloured plates + ★ on S) but it is the heaviest on hex (31), and it fails colour-blind safety on F lines — so it is "colorful" but tips past "not overwhelmed" and past safety.

**Where D1b still fails:** the `#00000066` plate over the `#DADADA` T1/F ink drops the lowest-contrast signals to muddy grey, and the dark plate over the EHG weaver purple bar stacks two dark layers — the weaver line signal is the weakest-readable row in the file.

🟣➤🔷 GLM

---
Conductor note (not seat text): D1c's colour-blind "failure" is entirely Sonnet's optional "no letter on F" rule that the mockup carries; the 25% tier plate itself is text-backed. GLM's D1c hex count of 11 double-counts plate-alpha variants of the same hue (`#A807FF40` and `#A807FF`), which is why D1c scores "heaviest" despite drawing fewer plates than D1b.
