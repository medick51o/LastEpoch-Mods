# 🟣➤🎼 Composer — fresh-eyes lens (2026-09-11 14:41, blind, read-only)
sessionId 7688baa4-c111-4e02-8843-d42153e7c798 · ♾️ INCLUDED (Cursor · composer-2.5) · 29317 in / 4043 out

# 🟣➤🎼 Composer — FRESH-EYES READ (blind seat)

**Verdict: DONE_WITH_CONCERNS** — runtime 9-slice sibling Image behind the existing affix TMP is the fewest moving parts that can draw a real border under glyphs tonight; donor-clone and row-native Images are traps. **NOT PROVEN** until one affix line shows a rounded outline in-game.

## Q1 — Most reliable border path (ranked)

| Rank | Candidate | Cost | In-game test |
|------|-----------|------|--------------|
| **1** | **(a) Fresh `GameObject` + `Image` (Sliced), runtime `Sprite.Create` border, sibling **before** row `Text`, sized from `textInfo` of unit chars** | One static 16×16 texture once; ~15 lines bounds math per affix row on layout dirty; no clone | Hover rare with `Tier 5 · A` — thin purple-grey rounded outline hugs only the unit, sentence unchanged beside it |
| 2 | (e) Low-alpha `<mark>` in tier colour | Zero UI objects; TMP-only | Border reads as flat tint **over** glyphs (HDR may wash white) — kills the mockup look |
| 3 | (b) Repurpose `corrupted-border` sibling | Per-row native Image; fight game ownership | Activating/recolouring it lights the **full row** (~448×42), not the unit pill |
| 4 | (c) Clone/shrink `item-tooltip-foreground-border-high-tier` | Clone fragility + ornate art at ~40px wide | Mini frame looks like a shrunk tooltip chrome blob, not a 1px pill |
| 5 | (d) Bracket/box-drawing + `<u>` | Rich-text only | Glyphs vary by font; no padding; line sits **in** text colour, not under it |
| 6 | (f) Separate overlay TMP (current PillProbe shape) | Duplicate label + margin shift | Border is beside/replacing text, not under the composed affix string |

**FACT:** TMP has no border tag; `<mark>` draws over glyphs. **FACT:** `SetSiblingIndex(0)` under `Prefix N` draws behind `Text`. **OPINION:** (a) matches mockup (`border` + transparent centre) with no donor dependency.

## Q2 — Why the probe clone lost its sprite

**FACT (log):** donor try 3 accepted `lorehighlight` (`dialogueOptionGlow`, **Simple**, border=0); `CreatePill` then threw `accepted donor clone lost its Image sprite`.

**Likely causes (OPINION, not isolated):**
1. **Wrong donor** — glow plate, not a 9-slice frame; may be material-driven or cleared on enable.
2. **Il2CPP Instantiate** — sprite reference on `Image` not stable after `Instantiate` + child destruction (PillProbe destroys all children before the check).
3. **Surviving clone recipe (NativeClone.cs):** strip game `MonoBehaviour`s (`LocalizeStringEvent`, custom buttons) **before** trusting visuals; copy `sprite`, `material`, `color`, `type` explicitly after strip; **for borders, skip clone entirely** — `new GameObject` + `Sprite.Create` cannot be reset by donor scripts.

## Q3 — Positioning & recompute

**Bounds recipe (FACT: TMP law):** after compose + `ForceMeshUpdate()`, walk `textInfo.characterInfo[i]` for visible chars covering the unit span (plain-text indices of `Tier N|G` / `TN|G` from composer, skip rich tags). Union `bottomLeft`/`topRight` in TMP local space → pad mockup (+2v, +9h px) → `TransformPoint` to parent (`Prefix N`).

**Recompute when:** `UpdateLayout` dirty window fires (tmp instance id + text hash change — same gate as TooltipRecolor). **LateUpdate:** cheap resync only if tooltip active and hash unchanged (layout-only frames: skip).

**Scale / pool / compare:** same-parent coordinates inherit canvas scale (100/125/150%); keyed state per TMP id; comparison tooltips are separate tooltip roots — independent watches. **NOT PROVEN:** char-index map stays correct if EHG rewrites text mid-layout.

## Q4 — Colour rule

**OPINION (calm, mockup-faithful):** **one** border per line around the whole `TN | G` unit — not split tier/grade frames.

- Border stroke: mockup neutral `#5a4670` at full alpha (readable on dark tooltip).
- Centre: transparent (or optional `#1e1526` at ~0.35 if Andrew wants subtle fill without a "plate").
- Ink stays tier colour + grade colour; sentence rule unchanged.

Tier-coloured border at ~0.5α is a pref toggle later; default neutral avoids rainbow noise on dense items.

## Q5 — `T7` vs `Tier 7`

**Pref:** `TierWord = Spelled | Compact` (default Spelled for mockup parity).

**Geometry:** compact only narrows unit width — bounds-from-`textInfo` auto-follows. **Wrap risk:** `Tier 7` wider → slightly higher wrap chance on narrow tooltips; `T7` safer. Border logic identical; composer must emit the same `| G` delimiter in both modes.

## Q6 — Failure policy

On any exception or bounds failure: **destroy/hide border GO, leave coloured text untouched, log once, latch border path off for session** (PillProbe degrade pattern).

| Path | One log line |
|------|----------------|
| **Success** | `[TierBorder] placed Prefix 3 bounds=38.2x17.6 tier=5 grade=A` |
| **Degrade** | `[TierBorder] FAIL: <reason> — text-only (latched off)` |

## First pick — C# sketch (≤40 lines)

```csharp
// static Sprite s_border; built once: 16x16 Texture2D, transparent centre, #5a4670 1px ring + corner radius
static void PlaceBorder(TextMeshProUGUI tmp, int unitEndChar) {
    tmp.ForceMeshUpdate();
    var ti = tmp.textInfo; Bounds b = default; bool any = false;
    for (int i = 0; i < unitEndChar && i < ti.characterCount; i++) {
        var c = ti.characterInfo[i]; if (!c.isVisible) continue;
        var bl = c.bottomLeft; var tr = c.topRight;
        if (!any) { b = new Bounds((bl + tr) * 0.5f, tr - bl); any = true; }
        else b.Encapsulate(bl), b.Encapsulate(tr);
    }
    if (!any) throw new InvalidOperationException("no unit glyphs");
    var parent = tmp.transform.parent;
    var go = new GameObject("TT_UnitBorder");
    go.transform.SetParent(parent, false);
    go.transform.SetSiblingIndex(tmp.transform.GetSiblingIndex()); // behind Text
    var rt = go.AddComponent<RectTransform>();
    rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
    var pad = new Vector2(9f, 2f);
    var size = (Vector2)b.size + pad * 2f;
    var local = tmp.rectTransform.TransformPoint(b.min) - pad;
    rt.anchoredPosition = parent.InverseTransformPoint(local);
    rt.sizeDelta = size;
    var img = go.AddComponent<Image>();
    img.sprite = BorderSprite.Get(); // Sprite.Create(tex, fullRect, pivot, 100, 0, FullRect, border=(4,4,4,4))
    img.type = Image.Type.Sliced; img.color = Color.white; img.raycastTarget = false;
    MelonLogger.Msg($"[TierBorder] placed {parent.name} bounds={size.x:F1}x{size.y:F1}");
}
```

**Fallback:** if `PlaceBorder` throws → log degrade line above; optional `SignalStyle` pref swaps to low-alpha `<mark=#1e1526aa>` behind unit chars only (text-only degrade, no Image).

🟣➤🎼 Composer
