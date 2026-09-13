# 🟢 Gemini — cross-vendor review of TICKET-3.1.0-border (2026-09-11 15:08)
conversationId d9ae6aa0-3187-436e-90e0-ab30205eb48d · status SUCCESS · brain UNREPORTED · builder 🔵 Codex thread 01a09275… (OpenAI) → cross-vendor holds · packet C:\Sync\Projects\tt-border-2026-09-11\build (DELTA-310border.patch 746 lines, UnitBorder.cs 614 lines) · repair rounds used: 0 of 2

# Code Review: 3.1.0-border Unit Border & TierWord Toggle

**Reviewer:** 🟢 Gemini
**Builder:** 🔵 Codex (OpenAI)
**Verdict:** **APPROVE**

## 1. Executive Summary & Findings Matrix

| Severity | Count | Summary |
| :--- | :---: | :--- |
| **BLOCKER** | 0 | None. |
| **MATERIAL** | 0 | None. |
| **MINOR** | 1 | `UnitBorder.cs:487`: `GetComponent("CanvasRenderer")` uses string lookup rather than generic `<CanvasRenderer>()`. Creation-only, zero steady-state impact. |
| **NOT PROVEN** | 0 | None. |

## 2. Itemized Verification Checklist

### (1) Containment & File Boundaries — PASS
Write set verified exact (`UnitBorder.cs`, `TerribleTooltipsMod.cs`, `Prefs.cs`, `TooltipRecolor.cs`, `CHANGELOG.md`). No hook, scan-gate, marker, or re-entrancy latch changes in TooltipRecolor.cs. PillProbe.cs completely untouched (bytes identical).

### (2) Harmony Patch Signature & Forbidden Hooks — PASS
`Patch_UpdateLayout` postfix uses `(UITooltipItem __instance, object[] __args)` — verbatim match with TooltipRecolor.cs. No patches on forbidden hooks. No IL2CPP generic `List<T>` parameters.

### (3) Gating & Runtime Off Lifecycle — PASS
Gate = `Prefs.UnitBorder.Value && Prefs.EnableTooltips.Value && Prefs.Style.Value == SignalStyle.PlainText`. Disabled → owned borders hidden, `OnLateUpdate()` short-circuits with zero per-frame cost. Runtime off → `CleanupAll()` destroys all owned GameObjects and emits `[UnitBorder] off — borders removed` once.

### (4) Sprite & Image Hierarchy Setup — PASS
Single 16×16 RGBA32 texture generated lazily per session; 1-texel ring, 4 corners cleared; `Sprite.Create(..., FullRect, new Vector4(4,4,4,4))`; static refs retained. Image: `Sliced`, `fillCenter = false`, `raycastTarget = false`, default material. Parented to `tmp.transform.parent` with `LayoutElement { ignoreLayout = true }`; sibling index = TMP's (renders under the glyphs). `tmp.margin` never modified.

### (5) Unit Location, Measurement & Padding — PASS
Unit bounds via `textInfo.linkInfo` tag `"ttu"`; fallback = prefix length with once-log `[UnitBorder] linkInfo empty; using prefix count`. Non-visible chars and U+200B skipped. Wrap guard: `[UnitBorder] unit wrapped; row skipped` once. Pad +6 left/right, +2 top/bottom; right pad shrinks against the first sentence glyph. Corners TMP local → world → parent local; anchors/pivot top-left.

### (6) Per-Frame Cost — PASS
`ForceMeshUpdate()` only when `textHash` changes or on first measurement. `OnLateUpdate()`: zero mesh updates, scans, allocations in steady state. `GetComponentsInChildren<TextMeshProUGUI>(false)` bounded to the candidate `UITooltipItem`.

### (7) Lifecycle & Isolation — PASS
Row states keyed by TMP instance id per tooltip instance id; comparison tooltips independent. Hidden when marker/link missing or TMP inactive/destroyed; tooltip root destruction purges state and destroys borders.

### (8) Failure Policy & Logging — PASS
Any exception latches `s_latchedOff`, destroys owned objects, emits `[UnitBorder] OFF (<stage>: <type>: <message>) — plain coloured text`; the game's `UpdateLayout` returns normally. `[UnitBorder] placed n/m rows path=runtime-9slice` once per content fingerprint change.

### (9) Composer Formatting & Regex Audit — PASS
PlainText emits `<link="ttu"><color=#A807FF>Tier 5</color><color=#8a8478>·</color><color=#FA9E3D>A</color></link>` (Spelled) / `…<color=#A807FF>T5</color>…` (Compact). Link wraps tierPart + separator + gradePart; sealedPart excluded; Badge path and 4-ZWSP marker unchanged. All five `s_tierRegex` sites (479, 680, 745, 764, 819) parse raw EHG `Tier: N` text; composed output carries the marker and is skipped on re-entry (line 429); Compact output never feeds them.

### (10) Preferences — PASS
`TierWordStyle { Spelled, Compact }`, `BorderColorMode { Neutral, TierColor }` appended, order frozen. `UnitBorder=true`, `TierWord=Spelled`, `BorderColorMode=Neutral`; all three in `WarnOrphanedKeys`; existing keys unchanged.

### (11) IL2CPP & Runtime Safety — PASS
Array bounds checked against `linkCount`/`characterCount`; `GetLinkID()` and Unity objects null/destroyed-guarded; `#5A4670` parsed as `Color(90/255, 70/255, 112/255, 1)`.

### (12) CHANGELOG — PASS

## 3. Verdict
**APPROVE**

Signed, 🟢 Gemini

---
Conductor: verify build 0 warnings/0 errors; containment vs baseline-310border.md5 = exactly Prefs/TerribleTooltipsMod/TooltipRecolor/CHANGELOG + UnitBorder.cs new; hook/scan/marker grep-hash identical to baseline. The MINOR (string GetComponent) is left as is for this dev build; noted for 3.1.0 final.
