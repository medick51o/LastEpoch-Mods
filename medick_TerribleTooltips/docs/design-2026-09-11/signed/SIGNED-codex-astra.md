# 🔵 Codex (Astra) — LEAD design deep dive (2026-09-11, blind, read-only sandbox)
threadId 01a090ed-1a8f-7743-b85b-1c63929e9b57 · model gpt-6-astra · cwd C:\Sync\Projects\tt-design-2026-09-11

DONE_WITH_CONCERNS

## 2. Manifest

Read BRIEF.md first. Read every file under `src/`:

- `AaronsHouse.cs`
- `AffixInjector.cs`
- `BuildInfo.cs`
- `Colors.cs`
- `FilterRuleTooltip.cs`
- `GroundLabels.cs`
- `NativeSettings.cs`
- `Prefs.cs`
- `SettingsUi.cs`
- `TerribleTooltipsApi.cs`
- `TerribleTooltipsMod.cs`
- `TooltipRecolor.cs`

Also read:

- `docs/SPEC.md`
- `docs/ARCHAEOLOGY.md`
- `mockups/tooltip-belt-v3-badges.html`
- `mockups/tooltip-flow-v3.html`
- `context/inventory/NativeClone.cs`

Read the historical council brief and both signed reports in `context/council-2026-09-10/`. Read both tickets, both Gemini reviews, and release notes in `context/build-2026-09-10/`; skimmed both delta patches.

All paths are relative to `C:\Sync\Projects\tt-design-2026-09-11`. No files modified, helpers spawned, builds performed, mockups executed, or game launched. Visual judgments below come from source and HTML/CSS inspection plus the screenshot descriptions supplied in the brief.

## 3. My read of the CURRENT look

**OPINION:** The current design has a good information hierarchy buried under too much repeated emphasis. Tier appears as a colored plate, as the literal "Tier 5," and across the entire affix sentence. Grade adds another plate in a different color. A sealed affix can add a third plate. On a weaver line, EHG contributes another colored background.

The problem is primarily **how much surface receives color**, rather than the frozen palette itself. We can preserve every canonical tier and grade color while making the tooltip substantially calmer.

**FACT:** With default preferences, a straightforward affix produces this visible text:

```text
Tier 5  A  58% increased Lightning Damage
```

Its actual signal treatment in [ComposeCleanLine](C:/Sync/Projects/tt-design-2026-09-11/src/TooltipRecolor.cs:815) is:

```html
<mark=#A807FF66><color=#FFF6E6>Tier 5</color></mark> <mark=#FA9E3D66><color=#FFF6E6>A</color></mark>  <color=#A807FF>58% increased Lightning Damage</color>
```

The composer appends four zero-width spaces to the completed TMP text. The plates have no explicit internal padding or size adjustment. The separating spaces are real text spacing.

**FACT:** In this game's observed TMP behavior, `<mark>` draws over its enclosed glyphs. The current `66` alpha is forty percent. It does not implement an ordinary background behind independently rendered text. `Colors.BadgeTextColor()` still describes dark/light contrast ink, but the current composer does not call it; it uses warm-white ink throughout. Using that helper would reintroduce the dark-ink failure documented in archaeology.

**OPINION:** "HDR-safe plate alpha" is presently a test objective, not a known constant. The supplied white-block report establishes a readability problem under capture; it does not establish its complete rendering or capture cause.

### The mockups are useful art direction, but not faithful renderer previews

**FACT:**

- The belt mockup uses CSS backgrounds **behind** text, horizontal chip padding, and smaller chip typography. The current composer supplies none of those exact CSS behaviors.
- Its bare hybrid example predates the shipped multi-stat lookup fix.
- The flow mockup uses flexbox to allocate separate name and signal regions. Current `SignalRight` merely inserts `<pos=68%>`.
- Current `SignalRight` falls back to Trailing when `cleanName.Length > 30`. Character count does not measure glyph width; retained markup can also affect that count.
- The mockup joins and abbreviates hybrid descriptions. The shipped composer generally preserves EHG's descriptions and separate stat lines.
- The mockup's combined Alt explanation is aspirational. Current deep view emits craft annotations and range lines separately.

**OPINION:** The flow mockup's calmness is worth pursuing, but its right column should not be presented as a cheap typography change already supported by the current implementation.

### What shipped as 3.0.2 actually establishes

**FACT:** The important multi-stat fix is in [AffixInjector](C:/Sync/Projects/tt-design-2026-09-11/src/AffixInjector.cs:67). When `AffixFormatter` receives `affix == null`, it can resolve the affix through `modProperty`, provided implicit and unique indices are negative and exactly one item affix matches.

Consequently:

- Separately formatted stat lines can each receive their own tier/grade signal.
- A bracket on the first line of a multiline string produces one signal; following continuation lines receive tier coloring.
- An unbracketed multiline block can synthesize a tier from its own `Tier:` text, but cannot synthesize an honest grade.
- An ambiguous property match deliberately produces no injected signal.

Two identical grades on two stats can therefore reflect the same underlying `ItemAffix.getRollFloat()`. They do not prove independently measured rolls for those displayed stats.

The temporary `FormatAffix` hooks were removed after tracing showed they did not fire on the investigated path. Recommendations to restore those hooks as the known fix would contradict the later evidence.

### The available hooks and layout machinery

**FACT:** The current registration list contains eight patches:

| Hook | Current purpose |
|---|---|
| `TooltipItemManager.AffixFormatter` | Craftable and resolved multi-stat brackets |
| `TooltipItemManager.UniqueBasicModFormatter` | Unique/legendary grade brackets |
| `TooltipItemManager.ImplicitFormatter` | Implicit grade brackets |
| `UITooltipItem.UpdateLayout` | Scan, compose, then request native remeasurement |
| `UITooltipItem.SetAsItemTooltip` | Capture item; mark content dirty |
| `UITooltipItem.SetAsGroundTooltip` | Capture ground item; mark content dirty |
| `GroundItemLabel.SetGroundTooltipText(bool)` | Delayed ground-label augmentation |
| `SettingsPanelTabNavigable.Awake` | Native settings injection |

`OnUpdate` monitors filter-rule injection and ground-label Alt swaps. `OnLateUpdate` drives ranges, detects keyboard Alt, performs scan catch-up, and enforces cached tier colors and suppressed ranges.

The affix composer actively emits `<color>`, `<mark>`, and `<pos>`. `<size>` is already used elsewhere in this mod, including the filter rule and settings descriptions; EHG size tags can survive inside incoming affix text. `<b>`, `<i>`, and `<sprite>` are not new signals emitted by `ComposeCleanLine`; sprites require a suitable asset.

The layout sequence is:

```text
EHG measures original text
→ UpdateLayout postfix composes shorter text
→ mod calls UpdateLayout again with captured positioning arguments
→ re-entry latch prevents another composition pass
```

[RequestRelayout](C:/Sync/Projects/tt-design-2026-09-11/src/TooltipRecolor.cs:307) replays the original `Vector3`, `Vector2`, and `RectTransform` arguments. This lets EHG remeasure changed text and collapse the removed essay space. It does **not** establish that EHG will measure arbitrarily added child widgets, reserve chip columns, or position them per text line.

**FACT:** Several constraints remain beneath the release shorthand:

- Scanning still searches all active scene TMPs, with exclusions. It is not scoped to a mapped tooltip hierarchy.
- There is a five-frame dirty window and a half-second fallback. "Only scans when content changes" is an approximation.
- An active cached TMP overwritten with noncomposable text can continue triggering the marker-loss scan gate; pruning removes dead/inactive entries, not every such repurposed entry.
- Only one `s_lastTooltip` and argument set is remembered, although the original-text cache can contain multiple tooltips.
- "GameDefault" omits the new name tint after original color tags have been stripped. It does not preserve every original EHG inline color.
- Master-off restores cached pre-composition strings, which may already contain injected KG brackets. It clears enforcement caches but does not explicitly restore saved standalone TMP colors. Complete immediate vanilla restoration should not be assumed from the release wording alone.

These are relevant acceptance cases for an overhaul, not proof that every player currently encounters a visible defect.

## 4. Directions

### D1 — Rich-text polish

**OPINION — appearance:** Start with colored text signals, a neutral affix sentence, and no plates:

```text
Tier 5 · A  58% increased Lightning Damage
Tier 4 · C  28% increased Mana Regeneration
Tier 7 · C  30% increased Cooldown Recovery Speed
```

"Tier 5" remains canonical purple; "A" remains canonical gold. The sentence uses the inherited base text color. Keep the signal at the left so it stays adjacent to the stat and does not depend on a false right column.

**FACT — feasibility:** `BadgeLeft + PlainText + GameDefault` already approximates this with existing preferences. A modest spacing/typography pass fits the existing composer and remeasurement architecture.

**Cost:** A few engineering hours for focused composition changes; additional in-game validation hours across item types, scales, Alt states, and coexistence. Low implementation risk, with meaningful visual and regression checks still required.

**Constraints it hits:**

- `<mark>` overdraw if plates remain.
- Native TMP width, wrapping, font metrics, and inherited size tags.
- Existing parsing and incomplete semantic association between stat lines.
- EHG-owned section spacing and weaver highlighting.
- Post-layout mutation and native remeasurement.

**What it cannot do:** Real rounded pills, independent background/foreground rendering, a genuine responsive column system, clickable grade controls, or arbitrary panel restructuring.

**OPINION:** D1 should not attempt to win back width by silently abbreviating EHG stat descriptions. Long descriptions should remain readable even when they require another physical line.

### D2 — Native-clone chips

**OPINION — appearance:** One modest native pill with a neutral game-art background, purple "Tier 5," a muted divider, and gold "A":

```text
[ Tier 5 | A ]  58% increased Lightning Damage
```

The brackets above depict a real UI boundary, not literal characters. The affix sentence is neutral. The pill's Image is behind its own TMP label, avoiding `<mark>` overdraw. Rounded corners are conditional on the donor sprite; native cloning does not guarantee a pill-shaped asset.

For a multi-stat affix, initially preserve a signal on each separately signaled stat. Grouping can follow only when affix identity is reliably associated with those rows.

**FACT — what is proven:** [NativeClone.Button](C:/Sync/Projects/tt-design-2026-09-11/context/inventory/NativeClone.cs:28) demonstrates instantiation, removal of donor behavior and localization, replacement of events, and cleanup on failure. It also demonstrates separate free-positioned and layout-group sizing. This mod's `NativeSettings` independently proves native row cloning.

**FACT — what is unproven:** No supplied file identifies a cloneable tooltip pill, its exact hierarchy path, its sprite borders, or a safe attachment point for every affix presentation.

**Cost:** Tens of engineering hours for discovery, a prototype, lifecycle ownership, and validation. Medium architectural cost, with substantial uncertainty until the donor and wrapping experiment succeeds.

#### Candidate donors to inspect

These are **OPINION / discovery targets**, not claimed existing GameObject names.

| Candidate | Why inspect it | Evidence boundary |
|---|---|---|
| Visual container around `ItemTooltipAffix._tierText` | Closest semantic location and likely compatible font scale | `_tierText` is documented; a surrounding Image is not |
| Visual treatment around `ItemTooltipModifier._rangeText` | May reveal reusable row sizing and detail placement | Range TMP exists; a chip background is unproven |
| Native sealed/enchantment or weaver-affix decoration | Already belongs within the tooltip's visual language | Weaver bar is described; its component hierarchy is unknown |
| Compact requirement, rarity, LP, or Weaver's Will presentation | Possible small labels or frames | Inspect for actual Image/sprite components; some may be text only |
| Inventory Sort button | Concrete fallback with an implemented cloning pattern | Larger, interactive donor; even its useful nine-slice properties need verification |
| Settings dropdown background or toggle-row artwork | Proven discoverable settings donors | May be unavailable before settings creation and poorly proportioned for tooltip text |

The `requires` TMP is also already occupied by filter-rule injection. It is a potential inspection landmark, not a free placement region.

#### How I would position the pills

**OPINION — proposed implementation, requiring a hierarchy probe:**

1. **Establish ownership first.** Associate each discovered affix TMP with its actual tooltip instance, content generation, and semantic source lines. Do not create pills for arbitrary regex hits from the full-scene scan.

2. **Keep EHG's existing affix TMP in its current parent.** Attach a mod-owned decoration layer within that row/TMP's coordinate hierarchy. Make the decoration layer excluded from layout participation where a layout group could otherwise allocate it another row.

3. **Reserve a left gutter in the text's measured area.** For example, use the native TMP margin mechanism after saving its original value. Size the gutter from the largest pill needed by that block plus a small gap. This is component/layout work beyond D1; its effect on EHG's measurement must be verified.

4. **Place pills from rendered text geometry.** Record the character position corresponding to each composed affix start, then resolve its rendered line and baseline after text layout. Transform those coordinates into the decoration parent's local space. Use top-left anchors and pivot for the pill RectTransforms; align the label baseline with the affix's first visual line.

5. **Treat soft wrapping differently from a new stat.** A wrapped continuation gets no extra pill. A separately bracketed stat does. Do not calculate vertical positions as `line index × guessed font height`; Alt rows, size tags, and wrapping invalidate that formula.

6. **Make all decoration noninteractive.** Disable raycast targets on Images and TMPs; remove donor `Selectable` behavior, navigation, callbacks, localization, and any scripts that rewrite visuals. Disable or remove donor animations that could retint the pill.

7. **Keep clone labels out of composition and formatter-health discovery.** Explicit ownership exclusion is safer than relying on their current text not matching a regex.

If the actual tooltip exposes a clean, supported row container, a layout-aware alternative is a horizontal row containing a fixed signal region and flexible text region, inside the native vertical list. `LayoutElement` sizes can then participate in measurement. However, reparenting EHG's referenced TMP into a new wrapper may break its assumptions. I would not choose that approach before inspecting the real hierarchy and layout drivers.

#### What happens on `UpdateLayout`

**FACT:** Current composition runs after native measurement and immediately requests another native layout.

**OPINION:** D2 must establish the gutter and final text **before** that remeasurement, then synchronize pill geometry after the text has settled. Unity may defer mesh/layout work; returning from `UpdateLayout` is not, by itself, proof that every geometry field is final.

Chip synchronization must also run when geometry changes without a text scan: UI scale, native positioning, comparison changes, and layout rebuilds. Parenting should make ordinary tooltip movement carry the chips automatically. It should not require a full-scene scan or clone creation every positioning frame.

The added synchronization needs its own guard so resizing a pill cannot produce a repeated layout/rebuild cycle.

#### What happens on pooled-TMP reuse

**OPINION:** Store pill ownership by tooltip instance, TMP instance, content generation, and source-line identity. `GetInstanceID()` plus the existing marker is insufficient as the complete identity model.

On a new item, lost ownership, hidden tooltip, destroyed parent, or master-off:

- Hide old pills before presenting replacement content.
- Restore any owned margin/layout changes.
- Rebind reusable pills to the new lines; retire extras.
- Keep comparison-tooltip ownership separate.
- Never leave a stale pill visible because a new affix could not be resolved.

If validation fails, fall back to the D1 signal. Create and validate the replacement before removing the readable text signal.

#### Failure modes

**OPINION — concrete risks to test:**

- Donor Image has no useful slicing borders; shrinking it crushes its ornamentation.
- A donor script, persistent callback, localization binding, or animator survives and changes state.
- A cloned graphic intercepts pointer input or enters controller navigation.
- Layout groups count decorations as rows, or override their RectTransforms.
- Text margin changes are ignored or overwritten by EHG.
- Pills overlap wrapped text, Alt details, sealed labels, or comparison panels.
- Masking clips the pill outside the measured row.
- Deferred layout creates a one-frame jump or stale baseline.
- Per-layout instantiation produces garbage, leaked objects, or stutter.
- Pooled reuse attaches a valid-looking grade to the wrong item.
- A scene change destroys the donor or owned hierarchy.
- Another mod changes the same margin, parent, material, or hierarchy.

**What D2 cannot do:** Recover missing roll data, infer reliable affix grouping from equal text/grades, guarantee a redesigned tooltip skeleton, or eliminate patch maintenance. A real chip fixes rendering ownership; it does not automatically fix data ownership.

### D3 — Custom overlay

**OPINION — appearance:** A native-inspired panel with actual columns and grouped affixes:

```text
58% increased Lightning Damage                 Tier 5   A

+3% Physical Penetration                       Tier 1   F
+3% Minion Physical Penetration
```

The penetration pair shares a signal only when its common affix identity is known. Alt expands a subordinate detail region with range and crafting information. The name column wraps independently; the signal stays aligned. The palette remains frozen.

**FACT — current reach:** Safe SetAs hooks expose item context; formatter hooks expose useful stat data; native layout provides positioning arguments. The current implementation does not provide a complete structured tooltip model, controller lifecycle, or overlay renderer.

**OPINION:** Build an explicit tooltip model before building the final panel. Cover implicits, craftable affixes, sealed state, unique/legendary modifiers, ranges, requirements, special item metadata, comparisons, and other native sections. Reconstructing the whole presentation from item data also means reproducing EHG's localization and context-dependent display semantics.

**Cost:** Many dozens of engineering hours, potentially a multiweek effort once parity and compatibility are included. High implementation and continuing maintenance risk.

#### Concrete risk list

| Risk | Failure mechanism and required evidence |
|---|---|
| Input capture | Overlay graphics can intercept inventory clicks, dragging, ground-label pickup, or hover detection. A passive first version should have no raycast targets or focusable controls. Verify actual pickup and drag behavior. |
| Native tooltip suppression | Covering the original does not reliably hide its edges when dimensions differ. Deactivating it could stop native lifecycle updates. A reversible visual suppression method must preserve required behavior. |
| Canvas sort order | "Always on top" can cover menus, chat, mod windows, or comparison tooltips. Inspect the relevant canvas, camera, sorting layer, order, and modal behavior; do not choose an arbitrary extreme order. |
| UI scale and resolution | Screen pixels and canvas units may differ. Resolve canvas scaling, coordinate conversion, font scale, edge clamping, ultrawide layouts, and oversized tooltips. |
| Controller/gamepad mode | Current deep view reads LeftAlt/RightAlt. Controller focus, inspect/compare actions, placement, and mode changes are not mapped. A mouse-following overlay is insufficient. |
| Multiple tooltips | Hovered and equipped comparisons need distinct models, lifetimes, positioning, and available screen space. Current single remembered tooltip is not enough. |
| LeHud coexistence | Avoid the forbidden native-function alias. Verify text/material behavior and whichever canvas or presentation properties LeHud also touches. |
| Fallen coexistence | A replacement can cover information Fallen adds to native tooltips even without changing ground labels. Preserve the ground-label treaty, gold rule number, public API, and mod identity; explicitly validate visible tooltip additions. |
| Display correctness | Display values can depend on context. The legendary grading history proves that displayed values and stored roll semantics are not interchangeable. |
| Loading and transitions | Hide stale overlays on scene changes, inventory closure, lost hover/focus, destroyed item/UI references, and master-off. |
| Every game patch | Audit hooks, field names, donors/assets, enum modes, tooltip sections, scaling, and compatibility again. New native sections may disappear silently from a replacement. |
| Failure recovery | If model construction or placement fails, restore native visibility immediately. A beautiful partial tooltip that omits material information is a failed replacement. |

**What D3 cannot do:** Guarantee native parity or compatibility automatically; make unknown data authoritative; use forbidden hooks as shortcuts; or become maintenance-free after its first successful demonstration.

### What Last Epoch's UI "simply cannot do" through this text-rewrite mod

**FACT, scoped carefully:** These are limits of the current text-rewrite approach. The files do not prove that Unity or Last Epoch as a whole is incapable of them. D2 and D3 are ways a mod could assume more of the responsibility.

| Requested experience | Why text rewriting alone cannot supply it | What would change the boundary |
|---|---|---|
| An opaque colored pill with independently legible foreground text | Current `<mark>` overdraw couples plate and glyph appearance | Separate Image/TMP controls or different rendering |
| Real rounded borders, independent padding, shadows, or hover states | A text highlight is not a UI container | Native widgets or custom UI |
| A responsive affix column plus a separately measured signal column | `<pos>` moves a caret; it does not negotiate column widths or prevent collisions | Layout-aware rows or a replacement panel |
| Guaranteed one physical line for every affix at every scale | Some descriptions exceed the available width | More width, wrapping, abbreviation, or truncation; each has a tradeoff |
| Independently clickable tiers, grade explanations, pin controls, or focus targets | Styling text does not create controls or navigation behavior | Event handling and an interaction model |
| Reliable grouping of all multi-stat affixes | The composer receives strings and brackets, not a complete identity map | Structured affix-to-row data |
| Grades for text whose roll cannot be identified | Typography cannot recover absent or ambiguous data | Better authoritative data association |
| Independent control of the weaver bar, panel border, section gaps, or sibling widgets | Those are outside the rewritten string | Hierarchy/component changes |
| Guaranteed icons from arbitrary `<sprite>` tags | The TMP must resolve a suitable sprite asset | Asset discovery or provision |
| Native-quality controller inspection and comparison behavior | Keyboard Alt handling does not define controller modes | Mapped game input/lifecycle integration |
| Durable layout ownership across native rewrites and pooled reuse | EHG remains the owner and can overwrite/reassign objects | Explicit lifecycle integration and ownership |

**OPINION:** What Andrew should ask EHG to reimagine is the tooltip's **row structure and information hierarchy**: first-class tier/roll fields, stable multi-stat grouping, responsive columns, progressive detail, and controller parity. Those would let a theme change presentation without reverse-engineering formatted strings. Text polish can improve the wrapper substantially; it cannot supply that underlying contract.

All directions remain bound by the absolute prohibitions: never patch `OpenItemTooltip`, never patch `UpdatePrefixAndSuffixesText`, and never inject IL2CPP generic List parameters into Harmony signatures. `TooltipContentBuilder.GetDescription` with `ref __result` is also a documented dead end. LeHud's ground-label `faceColor` preservation, the `LoadFilter("")` prohibition, and the Fallen treaty remain requirements.

## 5. Ranked recommendation

1. **D1 first: plain colored signals, neutral descriptions, left placement.** It directly addresses visual overload using existing capabilities. Retain tier-colored names as an option. Compare against the current default before changing established users' preferences.

2. **D2 second: a bounded native-pill prototype.** Advance only after demonstrating correct donor rendering, wrapping, remeasurement, pooled reuse, and comparison ownership. Prefer one neutral native container with separately colored tier and grade text over two saturated plates.

3. **D3 third: a separate product decision.** Choose it if Andrew wants a replacement tooltip's layout and interaction capabilities enough to fund ongoing parity work. Chip aesthetics alone do not justify that responsibility.

**OPINION:** Keep the current ground-label presentation during this overhaul. It already communicates compactly, and its treaty boundaries and pickup-adjacent behavior make it a poor place to bundle unrelated visual experimentation.

## 6. Concrete proposals

The rich-text samples below omit the existing four-zero-width-space output marker for readability. A real composer must append it once to the finished TMP text. Samples assume deliberately scoped tags; retained EHG size tags must be checked so they do not defeat the proposed sizing.

### 1. Establish a no-plate baseline

**FACT:** Existing `PlainText`, `GameDefault`, and `BadgeLeft` preferences supply the architecture.

**OPINION:** Give the separator breathing room and retain full-size numeric/letter signals:

```html
<color=#A807FF>Tier 5</color> <color=#8A8478>·</color> <color=#FA9E3D>A</color>  58% increased Lightning Damage
```

**Test:** Show the same four-affix item in current defaults and this treatment. Ask Andrew to identify the highest tier, best roll, and relevant stat. Check both speed and mistakes. No palette changes.

### 2. Test one plate instead of two

**FACT:** The composer already has independent tier and grade pieces.

**OPINION:** If Andrew wants a plate, keep it on the tier only and use an unplated colored grade:

```html
<size=90%><mark=#A807FF40><color=#FFF6E6>Tier 5</color></mark></size> <color=#FA9E3D>A</color>  58% increased Lightning Damage
```

Here `40` is hexadecimal alpha, approximately one quarter opacity—not forty percent.

**Test:** Compare no plate, quarter-opacity plate, and current forty-percent plate across every tier, SDR/HDR viewing, and weaver backgrounds. Reject any setting where the tier digit becomes harder to read. Do not label the winner universally "HDR-safe" from a browser mockup.

### 3. Preserve the colorful-name option without plates

**FACT:** Tier-colored names are an explicit Andrew preference in the historical contract.

**OPINION:** Offer this as the stronger-color alternative:

```html
<color=#A807FF>Tier 5</color> <color=#8A8478>·</color> <color=#FA9E3D>A</color>  <color=#A807FF>58% increased Lightning Damage</color>
```

**Test:** Compare it against proposal 1 on mixed-tier items, especially purple text on dark/purple backgrounds. This isolates name coloring from the plate problem.

### 4. Use small type for secondary detail, not the stat

**FACT:** `TierAnnotation` already removes the repeated tier number and returns dim annotation text. Deep ranges currently inherit grade color, or tier color on the unbracketed fallback.

**OPINION:** Preserve the native range wording while reducing its emphasis:

```html
<color=#A807FF>Tier 5</color> <color=#8A8478>·</color> <color=#FA9E3D>A</color>  58% increased Lightning Damage
<size=90%><color=#8A8478>max craftable</color></size>
<size=90%><color=#FA9E3D>Range: 40% to 60%</color></size>
```

**Test:** Alt and each pin independently, including standalone unique/set ranges. Verify that releasing Alt removes the extra height. Reject sizing that makes details difficult to read at the smallest supported UI scale.

### 5. Make sealed status textual

**FACT:** Badge mode can currently create separate Sealed, Tier, and grade plates.

**OPINION:** Remove the status plate:

```html
<size=90%><color=#8A8478>Sealed</color></size> <color=#DADADA>Tier 1</color> <color=#8A8478>·</color> <color=#A807FF>B</color>  13% of Potion Health Converted to Ward
```

An untiered implicit remains honestly grade-only:

```html
<color=#DADADA>F</color>  +48 Armor
```

**Test:** Normal and Alt views of sealed affixes, including long names and multiple grades. Preserve every supplied grade and the sealed meaning.

### 6. Treat compact signals and dots as alternatives, not extra decoration

**FACT:** "Tier N" is the current product contract. The belt mockup explicitly parks `T7` as a compact alternative.

**OPINION:** If Andrew approves compact wording, test:

```html
<color=#FF44FF>T7</color><color=#8A8478>·</color><color=#77ACFF>C</color>  30% increased Cooldown Recovery Speed
```

A dot-led variant is technically simple:

```html
<color=#FF44FF>•</color> T7 <color=#77ACFF>C</color>  30% increased Cooldown Recovery Speed
```

I rank the dot variant lower: it spends width without replacing the need for explicit tier and grade characters.

**Test:** Glyph availability, wrapping, and comprehension without consulting the legend. Never replace the tier number with color alone; T1/T2 are particularly close.

### 7. Correct the right-column demonstration

**FACT:** Current `<pos=68%>` plus a thirty-character threshold cannot guarantee collision-free placement.

**OPINION:** Keep BadgeLeft as the default. Present SignalRight as approximate until measured name **and signal** widths determine whether both fit. A measured-fit upgrade requires access to the actual TMP and font metrics beyond the current pure string helper.

**Test:** Narrow and wide glyph strings, long localized text, sealed signals, multiple grades, maximum UI scale, and comparison panels. Pass only when no text overlaps and fallback placement remains intelligible.

### 8. Reduce the filter rule's visual dominance

**FACT:** [NumberOnly](C:/Sync/Projects/tt-design-2026-09-11/src/FilterRuleTooltip.cs:118) currently emits `<size=200%>` into `requires`.

**OPINION:** Test a normal-size gold rule label:

```html
<color=#FA9E3D>Rule#69</color>
```

**Test:** Judge the whole tooltip, including requirements. Verify layout after the post-render injection; that path does not itself request the composer's native relayout. Keep the gold color and the separate filter-rule ownership.

### 9. Build an honest renderer comparison before choosing plates

**FACT:** The belt mockup's CSS background order does not reproduce TMP's documented overdraw.

**OPINION:** A revised comparison should distinguish "design intent" from an approximation of the TMP compositing order. Use the exact same affix strings and widths; remove flexbox advantages when demonstrating D1.

**Test:** Compare those approximations against actual in-game captures before accepting plate opacity, spacing, or contrast. Include the weaver bar. Browser appearance alone cannot pass the game-rendering gate.

### 10. Give D2 a specific exit criterion

**FACT:** Native cloning is demonstrated; tooltip-specific ownership is not.

**OPINION:** Prototype one neutral native pill on a known affix TMP, then exercise a long wrapped stat, Alt details, a two-stat idol, rapid item changes, and side-by-side comparison.

**Test:** No duplicate or stale pills, no intercepted input, correct text height, no per-positioning-frame clone creation, and full removal/restoration on master-off. If any lifecycle case is unresolved, retain D1 rather than expanding the prototype.

### 11. Require D3 to prove parity before visual replacement

**FACT:** Current data and lifecycle handling are incomplete for a full overlay.

**OPINION:** First construct and compare the proposed overlay model while the native tooltip remains visible. Require matching stat values, ranges, requirements, special sections, and comparison identities before suppressing the original.

**Test:** Keyboard and controller sessions; inventory, stash, equipment, and ground hover; LeHud and Fallen co-installed; scene transitions; supported UI scales; and a deliberate model failure that restores the native tooltip.

## 7. Either/or ruling questions for Andrew

1. Should the default use colored signals with neutral affix text, or keep full tier-colored affix text?
2. Should the default signal have no plate, or one tier plate with an unplated grade?
3. Keep "Tier 5" spelled out, or authorize compact "T5"?
4. Keep repeated signals on separately formatted multi-stat lines, or fund reliable grouping under one signal?
5. Pursue D1 polish alone next, or include a bounded D2 native-pill prototype?
6. Keep the native tooltip as the product boundary, or authorize a separate D3 replacement-tooltip discovery phase?

🔵 Codex (Astra)
