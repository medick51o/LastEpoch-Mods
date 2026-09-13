# BORDER council — synthesis (2026-09-11 14:48, conductor Fable 5.1). Six blind seats on BRIEF.md (md5 4a3fbe4a…); reads verbatim in signed\.
Seats: 🔵 Astra LEAD (gpt-6-astra, thread 01a09266…) · 🟢 Gemini · 🟣➤⚫ Cursor-Grok ♾️ 89550/8956 · 🟣➤🎼 Composer ♾️ 29317/4043 · 🟣➤🌙 Kimi 💸 101459/5899 · 🟣➤🔷 GLM 💸 34648/2035. All six: DONE_WITH_CONCERNS. Astra hit no limit.

## Ranked techniques (council)
| Rank | Technique | Seats ranking it #1 | Why |
|---|---|---|---|
| 1 | **(a) Fresh GameObject + RectTransform + CanvasRenderer + Image, runtime-drawn 9-slice ring sprite (`Sprite.Create` with border, transparent centre, `Image.type=Sliced`, `fillCenter=false`), inserted as a sibling BEFORE the row's `Text` so it draws UNDER the glyphs, sized from TMP `textInfo` glyph bounds of the unit, `LayoutElement.ignoreLayout`, `raycastTarget=false`, never a donor** | Astra, Gemini, Cursor-Grok, Composer, Kimi (GLM #2) | the only way to get a border under glyphs; nothing cloned, so nothing can reset its sprite; geometry is canvas-local so UI scale is free; per-row sibling so tooltip motion costs nothing |
| 2 | (f) a native 9-slice sprite (`border-nice`, Sliced 5.8/5.4 borders, found by Astra in the dump) copied onto the SAME fresh Image | Astra as the next asset experiment | native art, but transparency/tintability NOT PROVEN |
| 3 | (d) rich-text brackets `[ ]` + `<u>` | Gemini #2 as the text-only fallback | ink, not a frame; `⟦⟧` glyphs NOT PROVEN in the game font |
| 4 | (e) low-alpha `<mark>` | Kimi #2 | over the glyphs, washed to white under HDR, and it is the plate Andrew just rejected |
| 5 | (c) shrink `item-tooltip-foreground-border-high-tier` | none | 12/20/12/67 slices cannot make a 1px pill; the Compare-Content copy has sprite=null |
| 6 | (b) reuse `corrupted-border` | GLM #1, self-undercut | full-row corruption VFX (447×42, slice 4/8/73/7), game-owned, rename-fragile; five seats reject |

## Agreements (blind)
- Never clone a donor for the border; build fresh (all six). The probe's clone died inside `CreatePill` right after Instantiate + strip, not in a later layout pass (Astra, Cursor-Grok from the source; Gemini's "lost on refresh" is a hypothesis). Exact mechanism NOT PROVEN (OnDestroy of a stripped EHG behaviour nulling the graphic, or a runtime-assigned sprite that does not copy).
- ONE border per line around the whole `Tier N · G` unit, neutral `#5A4670` at full alpha, transparent centre, ~1 UI unit stroke, padding ≈ 9 horizontal / 2 vertical (the approved mockup). Tier-coloured border only as a later pref (Kimi, Composer: ~50–70% alpha; Astra ~45%). Two borders = noise (all).
- Bounds from `textInfo` after ONE `ForceMeshUpdate` inside the composer's dirty window, never per frame (all; the stutter bug is fresh). Union `bottomLeft/topRight` of the unit's VISIBLE chars only; skip the 4 ZWSP; margins are already baked into character positions, do not add them; `TransformPoint` → parent `InverseTransformPoint`; anchors/pivot top-left.
- Wrap guard: if any unit char is not on line 0, no border on that line (Astra, Kimi, Cursor-Grok).
- One border per TMP keyed by instance id + text hash; comparison tooltips are separate rows; hide when the marker is gone or the TMP is reused; positioning-only frames do nothing (all).
- `TierWord = Spelled | Compact`, default Spelled, enum order frozen day one, registered in WarnOrphanedKeys, produced ONLY in `ComposeCleanLine`'s tierPart; must NOT touch ground labels nor EHG's deep-view "Tier: N" text (GLM); any composer regex that re-reads `Tier N` must accept `TN` (Cursor-Grok); Compact only narrows the unit and lowers wrap risk (all).
- Degrade = the absence of the Image; the composer already emits the coloured text (GLM: "the degrade path is free"). One log line per distinct failure, latch structural failures for the session, retry transient hides on the next tooltip.

## Disagreements (named)
- Locating the unit: Kimi proposes a `<link="ttu">…</link>` wrapper at compose time and reading `textInfo.linkInfo` (invisible, robust to tag miscounts; NOT PROVEN in this TMP build). Astra proposes composer-recorded span indices per TMP. Composer/GLM/Cursor-Grok: first-K visible chars. Ticket: `<link>` first, characterInfo prefix-count fallback.
- TMP margin: Gemini would shift `margin.x` to make room; Astra says the unit's left pad fits in the existing 32.4 left margin and the sentence must not move. Ticket: margin untouched; if the right pad would collide with the sentence, shrink the pad, never the text.
- corrupted-border: GLM #1 vs five rejections. Not built.
- Astra's probe defects (targeted "Sealed Affix Header", NearbyImage ignores inactive objects, one watch for many rows): PillProbe.cs is left as is (off by default) and superseded by the new file; noted for the CHANGELOG.

## The ONE to build first, its test, the fallback
Build (a) as `UnitBorder.cs`. In-game test (Andrew, PROBE-README style): hover the logged gloves (T5/T4/T4/T7 + sealed T1) → every affix line shows a thin `#5A4670` outline hugging `Tier N · G` under the coloured glyphs, sentence untouched; open a comparison; hover a second item fast; hold Alt; set UI scale 125/150; toggle TierWord Compact; SDR + HDR screenshot. Log line for PASS: `[UnitBorder] placed n/m rows path=runtime-9slice`. Log line for DEGRADED: `[UnitBorder] OFF (<reason>) — plain coloured text`. Fallback = today's PlainText coloured text (already shipping).
