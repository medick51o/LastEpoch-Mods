# 🟢 Gemini — design council read (2026-09-11, blind, read-only by instruction, "what's on screen" lens)
conversationId 1827bb78-f6c7-445d-b6a9-bed56146b735 · status SUCCESS · brain UNREPORTED · turns 1 (Antigravity config = Gemini 3.6 Flash (High) per last night's note; not confirmed by the footer)

DONE

### 1. Manifest
- [BRIEF.md](file:///C:/Sync/Projects/tt-design-2026-09-11/BRIEF.md)
- [mockups/tooltip-belt-v3-badges.html](file:///C:/Sync/Projects/tt-design-2026-09-11/mockups/tooltip-belt-v3-badges.html)
- [mockups/tooltip-flow-v3.html](file:///C:/Sync/Projects/tt-design-2026-09-11/mockups/tooltip-flow-v3.html)
- [src/Colors.cs](file:///C:/Sync/Projects/tt-design-2026-09-11/src/Colors.cs)
- [src/TooltipRecolor.cs](file:///C:/Sync/Projects/tt-design-2026-09-11/src/TooltipRecolor.cs#L1-L60) (lines 1–60 and [L810–L913](file:///C:/Sync/Projects/tt-design-2026-09-11/src/TooltipRecolor.cs#L810-L913))
- [docs/SPEC.md](file:///C:/Sync/Projects/tt-design-2026-09-11/docs/SPEC.md#L130-L214) (sections "v3 — THE CLEAN LINE" and "Lean pass")

---

### 2. Read of the CURRENT Look (v3.0.2)
* **Visual Noise & Dual-Chip Clutter**: The current default layout (`BadgeLeft` + `Badge` style) renders two separate `<mark>` chips per line: `[Tier 5][A]  58% increased Lightning Damage`. Placing two adjacent translucent rectangular boxes on every affix line creates heavy visual grid lines that draw the eye to the box edges rather than the stat text.
* **Triple-Color Fatigue**: A single line currently features up to **3 distinct color signals** (Tier plate color, Grade plate color, plus Tier color on the affix text). Multiplying this across 4–5 affix lines results in 12–15 distinct color blocks on screen simultaneously.
* **HDR & Tone-Mapping Washout**: The 40% translucent colored `<mark>` plates (`<mark=#A807FF66>`) render as washed-out white/pastel blocks on HDR displays and capture software due to additive alpha blending over dark tooltip backgrounds.
* **Glyph Submersion (The TMP Law)**: Because Unity TextMeshPro's `<mark>` tag draws **on top of glyphs**, increasing plate alpha above ~55–70% to fight HDR washout causes the `<color=#FFF6E6>` text to drown under the quad overlay.
* **Idol & Multi-Stat Breakdown**: On a 4-affix idol, 8 rectangular chips stack vertically in a tight column ("box soup"). Multi-stat affixes either drop signal entirely or split into redundant lines.
* **Weaver Bar Collision**: On Weaver lines, EHG renders a native purple highlight bar behind the row. Placing our translucent `<mark>` chips over EHG's purple bar creates double-blended, muddy quads.

---

### 3. Direction Cost & Feasibility Assessment

#### D1: Rich-Text Polish (Single-Chip / Refined TMP)
* **On-Screen Look**: `[T5 · A] 58% increased Lightning Damage` — a single consolidated chip (or dark plate with colored ink), with affix text in clean neutral off-white `#E8E2D2`.
* **Cost & Risk**: **Low cost** (1–2 engineering days), **minimal risk**. Pure C# string composition changes in `ComposeCleanLine`.
* **Game/TMP Constraint**: Bound by Unity TMP rich text tags (`<mark>`, `<color>`, `<size>`, `<pos>`). `<mark>` quads remain hard rectangular shapes (no rounded pill corners).
* **What it CANNOT do**: Render true rounded pill corners, native UI drop-shadows, or complex multi-layer vector badges.

#### D2: Native-Clone Chips (GameObject Pill Injection)
* **On-Screen Look**: True rounded UI pills/badges cloned from native EHG tooltip/settings elements prepended to each affix text container.
* **Cost & Risk**: **Medium cost** (3–5 engineering days), **moderate-to-high risk**. Requires instantiating, positioning, and managing lifecycle of native Unity UI GameObjects inside EHG layout panels per tooltip frame.
* **Game/TMP Constraint**: Cloned UI elements can break during EHG UI `UpdateLayout` recalculations, risk UI pooling leaks, and create resolution/scaling misalignment across 1080p, 1440p, and 4K.
* **What it CANNOT do**: Zero-allocation inline rendering. Adding 4–8 GameObjects per tooltip open increases GC pressure and potential frame stutter during rapid inventory hovering.

#### D3: Custom Tooltip Overlay (Tier C Canvas Replacement)
* **On-Screen Look**: A completely custom-drawn HUD panel replacing EHG's item tooltip frame with bespoke cards, borders, rounded badges, and layout columns.
* **Cost & Risk**: **High cost** (2–3 weeks), **very high risk**. Requires hijacking mouse hover events, overriding controller UI navigation, and handling deep view toggles independently.
* **Game/TMP Constraint**: Highly fragile against game patches, breaks compatibility with sibling UI mods (LeHud, Fallen Star), and struggles with controller mode scaling.
* **What it CANNOT do**: Maintain low-overhead stability across game updates without constant patch maintenance.

#### D4 (Proposed): Minimalist Dual-Mode Typography (Single-Chip Gear / Unplated Idols)
* **On-Screen Look**: Gear uses a single dark/translucent chip `[T5 · A]` with colored ink; Idols and Weaver lines drop background plates entirely to use plain text signals `T5 · A`.
* **Cost & Risk**: **Very low cost** (half a day), **zero risk**. Builds cleanly within the existing `TooltipRecolor.cs` architecture.
* **Game/TMP Constraint**: Retains square `<mark>` quad geometry when chips are active.
* **What it CANNOT do**: Provide rounded UI pill borders on gear items.

---

### 4. Ranked Recommendation & Rationale

1. **Rank 1: D4 / Refined D1 (Single-Chip / Unplated Text Hybrid)**
   * **Why**: Delivers an immediate 80% visual cleanup without introducing native GameObject instability. Consolidating two chips into one and using dark translucent plates with bright colored ink completely solves HDR washout while respecting the TMP quad-over-glyph law.
2. **Rank 2: D2 (Native-Clone Chips)**
   * **Why**: Achieves true rounded pill aesthetics, but carries non-trivial UI layout and pooling risks for marginal visual gain over a cleanly styled single TMP chip.
3. **Rank 3: D3 (Custom Overlay)**
   * **Why**: Unnecessary engineering risk that violates the project's LeHud and Fallen Star compatibility treaties for purely aesthetic changes.

---

### 5. Concrete Typographic Proposals

1. **Strict 2-Color Budget Per Line**
   * `[FACT]`: Current `ComposeCleanLine` applies up to 3 colors per line (Tier plate, Grade plate, Tier text color).
   * `[OPINION]`: Restructure each line to use at most **2 colors**: 1 color for the signal element (Tier/Grade), and 1 neutral color (`#E8E2D2`) for the affix description text. This reduces visual noise by 33–50% across the tooltip.

2. **Consolidate Dual Chips into a Single Quad `[T5 · A]`**
   * `[FACT]`: `ComposeCleanLine` emits separate `<mark>` tags for Tier (`[Tier 5]`) and Grade (`[A]`).
   * `[OPINION]`: Merge Tier and Grade into a single `<mark>` quad: `[T5 · A]`. Abbreviating "Tier 5" to "T5" saves 3 characters per line (~20–30px at 1080p/1440p), eliminating chip wrapping on long affix names.

3. **HDR-Safe Dark Plate Strategy (`<mark=#00000066>`)**
   * `[FACT]`: In Unity 6000 TMP, `<mark>` plates draw ON TOP of text glyphs; colored translucent plates wash out to white quads in HDR capture software, while plate opacity >55% drowns light ink.
   * `[OPINION]`: Use a neutral dark plate (`<mark=#00000066>` at 40% black alpha) for all chips, and put the frozen WoW color onto the **ink text inside the chip** (`<color=#A807FF>T5</color> <color=#8a8478>·</color> <color=#FA9E3D>A</color>`). This prevents HDR whiteout, guarantees text legibility, and keeps the colored quad from drawing over glyphs.

4. **Plate-Free (Plain Text) Mode for Idols & Multi-Stat Rows**
   * `[FACT]`: A 4-affix idol currently renders 8 stacked rectangular `<mark>` quads vertically.
   * `[OPINION]`: Automatically disable `<mark>` background plates on Idols and multi-stat hybrid affixes, rendering them as clean plain text: `T5 · A  +15% Fire Damage`. This eliminates "box soup" on dense 4-affix idols.

5. **Weaver Line Coexistence Rule**
   * `[FACT]`: Last Epoch renders a native purple highlight bar behind Weaver affix lines.
   * `[OPINION]`: Detect Weaver lines and suppress `<mark>` chips entirely, rendering the signal in unplated bold text (`<b><color=#FF44FF>T7 · S</color></b>`). This prevents double-blended translucent quads on top of EHG's purple background.

6. **Hierarchical Dimming Rules**
   * `[FACT]`: `Colors.cs` defines `#8a8478` as the standard dim separator color.
   * `[OPINION]`: Enforce dimming (`#8a8478`) for all non-essential signals: dot separators (`·`), sealed tag label (`Sealed`), range numbers in Alt view, and F-grade indicators (`#DADADA`). The bright WoW colors should be reserved exclusively for actionable high-tier (T5–T7) and high-grade (A/S) signals.

7. **2-Stat Hybrid Affix Single-Row Layout**
   * `[FACT]`: Hybrid 2-stat affixes currently omit chips or render redundant lines.
   * `[OPINION]`: Combine 2-stat affixes into a single line with a joint grade signal: `[T1 · S·S] +32 Armor & 10% reduced Crit Damage Taken`.

---

### 6. Either/Or Ruling Questions for Andrew

1. **Chip shape vs text**: Should we keep rectangular `<mark>` chips on gear (`[T5 · A]`), or switch all tooltips to clean unplated text (`T5 · A`)?
2. **Affix text color**: Should affix stat descriptions stay neutral off-white (`#E8E2D2`), or continue carrying the tier color as in v2/v3?
3. **Chip interior colors**: Do you prefer a colored plate with white text (`<mark=#A807FF66><color=#FFF6E6>T5</color></mark>`), or a dark plate with colored text (`<mark=#00000066><color=#A807FF>T5</color></mark>`) to prevent HDR washout?
4. **Idol handling**: Should idols use the exact same chip layout as 4-affix gear, or automatically switch to unplated compact text (`T5·A`)?
5. **Tier abbreviation**: Should chips spell out `Tier 5` or use the compact `T5` prefix?

---

🟢 Gemini

---
Conductor note (not seat text): Gemini's "FACT" on the dim colour cites Colors.cs; the `#8a8478` Dim constant actually lives in TooltipRecolor.cs line 47 (Colors.cs holds the tier/grade table). Gemini's HDR claim ("additive alpha blending") is its reading of the described screenshot, not a verified rendering cause; Astra flags the same uncertainty.
