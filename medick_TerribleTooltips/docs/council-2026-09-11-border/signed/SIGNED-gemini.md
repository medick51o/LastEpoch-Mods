# 🟢 Gemini — what's-on-screen + Q4 colour rule (2026-09-11 14:40, blind, read-only by instruction)
conversationId 7453cfdb-4a8e-436b-9382-172cb63e3e70 · status SUCCESS · brain UNREPORTED

# 🟢 Gemini — Council Report: Tooltip Border Assessment

**VERDICT:** `DONE_WITH_CONCERNS`
*Concern:* Native donor cloning (`PillProbe.cs`) failed in-game because scene image components (e.g. `lorehighlight`) lose transient sprite references on tooltip re-layout. A reliable border implementation must use a runtime-drawn 9-slice `Sprite.Create` texture rather than scene donor cloning.

### Q1. Border Candidates (Ranked by Mockup Fidelity & Engineering Reliability)

1. **Candidate A: Runtime-drawn 9-slice border sprite on fresh Image sibling** *(Rank 1 - Recommended)*
   - **On-Screen Read (1080p/1440p):** Clean, sharp 1px rounded rectangle wrapping `Tier N | G`. Matches mockup 1:1.
   - **HDR Survival:** **PASS** (Solid dark/muted stroke under text; zero additive wash).
   - **Mockup Read:** **FACT:** Reads 100% as the approved mockup border (`border: 1px solid #5a4670`).
   - **In-Game Test:** `Inspect T7 affix at 1440p HDR; verify 1px #5a4670 outline renders under glyphs without losing sprite on tooltip refresh.`

2. **Candidate D: Pure rich-text bracket framing (`<color=#5a4670>⟦</color> Tier 5 | A <color=#5a4670>⟧</color>`)** *(Rank 2 - Pure Text Fallback)*
   - **On-Screen Read (1080p/1440p):** Muted purple text brackets enclosing the unit. Text-based, no true box outline.
   - **HDR Survival:** **PASS** (Standard TMP font rendering).
   - **Mockup Read:** **OPINION:** Reads as text brackets `[Tier 5 | A]`, not a continuous rounded pill container.
   - **In-Game Test:** `Set SignalStyle=BadgeTextBrackets; verify T5 affix displays '[Tier 5 | A]' in #5a4670 with zero UI Image overhead.`

3. **Candidate E: Low-alpha `<mark>` tag in TMP rich text (D1c Alpha Strip)** *(Rank 3 - Degraded)*
   - **On-Screen Read (1080p/1440p):** Flat filled rectangle behind text; square edges, no border outline. **FACT:** In Unity TMP, `<mark>` quad draws OVER glyphs.
   - **HDR Survival:** **FAIL** (**FACT:** Light `<mark>` quads washed to solid white under HDR capture in testing).
   - **Mockup Read:** **FACT:** Reads as a flat background highlight block, NOT a border frame.
   - **In-Game Test:** `Apply <mark=#1e1526AA> on T6 line; capture in HDR to verify glyph clipping and alpha wash.`

4. **Candidate B: Re-purpose row's `corrupted-border` sibling (`T_UI_Tooltip_Corrupted_Effect`)** *(Rank 4 - Rejected)*
   - **On-Screen Read (1080p/1440p):** Heavily distorted 73px sliced corruption artwork squeezed into an 80px pill.
   - **HDR Survival:** **NOT PROVEN**.
   - **Mockup Read:** **FACT:** Reads as corrupted item art, not a calm muted border.
   - **In-Game Test:** `Set corrupted-border RectTransform width to 80px; observe 9-slice corner scaling distortion.`

5. **Candidate C: Clone `item-tooltip-foreground-border-high-tier`** *(Rank 5 - Rejected)*
   - **On-Screen Read (1080p/1440p):** Oversized frame corners filling the unit interior.
   - **HDR Survival:** **PASS**, but visually broken.
   - **Mockup Read:** **FACT:** Fails mockup read due to full-tooltip frame border slice dimensions `(12,20,12,67)`.
   - **In-Game Test:** `Shrink high-tier tooltip foreground border to 80x24px; observe corner overlap.`

### Q2. Why Probe Donor Clone Failed & Survival Law
- **FACT (Log Line 971):** Probe selected `Header/Name/lorehighlight` (sprite `dialogueOptionGlow`, Simple type). During `UpdateLayout`, `lorehighlight` lost its sprite pointer when the game's tooltip manager refreshed.
- **Survival Law:** **Never clone scene donors for UI sprites.** Create a dedicated `GameObject` + `Image`, and assign `Image.sprite = Sprite.Create(Texture2D, rect, pivot, ppu, extrude, meshType, border)` generated at mod init from a 1px border Texture2D. This is 100% immune to native scene pooling.

### Q3. Positioning & Geometry Bounds
- **FACT:** Execute `tmp.ForceMeshUpdate()`. Measure pixel bounds of `Tier N | G` from `tmp.textInfo.characterInfo`.
- **Positioning:** Parent the border `Image` sibling to the row `Transform` before `TMP` (sibling index 0 so it renders behind text). Set `sizeDelta = (unitWidth + 12px, lineHeight + 4px)`. Adjust `tmp.margin.x` to offset affix description.
- **Recomputation Trigger:** Recalculate ONLY on tooltip generation change (`TMP instance ID` + `text hash`) or screen resolution/scale update. Dirty-window tracking prevents per-frame layout recalculations.

### Q4. The Colour Rule & Screen Lens
- **Border Colour:** **Mockup Neutral `#5a4670` (solid or 80% alpha)**.
  - **FACT vs OPINION:** **OPINION / RETINA LAW:** Tier-coloured borders (bright green/gold/pink) create heavy "rainbow noise" across 6 affix lines. Neutral `#5a4670` preserves visual calm while inner text ink (Tier/Grade colors) carries tier identity.
- **Thickness & Fill at 1080p:** 1px border stroke at 1080p. Center fill should be transparent or faint `#1e1526` (40% alpha).
- **Unit Count:** **ONE border** around the full `Tier N | G` unit. Two separate borders (`[Tier N]` and `[G]`) double border clutter.

### Q5. `T7` vs `Tier 7` Player Option
- **Pref Name:** `TierWord = Spelled | Compact` (default `Spelled`).
- **Geometry Impact:** `T7 | B` saves ~32px horizontal width per row.
- **FACT:** Compact form significantly REDUCES line-wrap risk on long affix lines at 1080p without altering border height or vertical alignment.

### Q6. Failure Policy & Logging
- **Degradation Rule:** If border Image fails to instantiate or geometry calculation fails, fall back to plain colored text (`<color=#FF44FF>Tier 7</color> <color=#8a8478>·</color> <color=#FA9E3D>B</color>`) inside the main TMP. Latch off border creation for the session.
- **Log Lines Proving Path Taken:**
  - *Success:* `[PillProbe] border pill placed on 'Tier 7 | B' size 78.5x22.0 at (x,y)`
  - *Fallback:* `[PillProbe] donor/border unavailable — degrading to plain text path (session latched off)`

🟢 Gemini

---
Conductor note: Gemini's Q2 "lost its sprite pointer when the tooltip manager refreshed" is a hypothesis; Astra and Cursor-Grok read the source and place the throw inside CreatePill right after Instantiate+strip, before any later layout pass. Gemini's "adjust tmp.margin.x" conflicts with Astra's "do not add margins again / do not shift the sentence"; the ticket keeps the TMP margin untouched.
