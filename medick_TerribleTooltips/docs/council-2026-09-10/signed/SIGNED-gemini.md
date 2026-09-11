# 🟢 Gemini — council read (2026-09-10, blind, read-only, alternative-cause lens)
conversationId 53f8190e-6844-45c1-9e5e-6a4637f2481d · brain UNREPORTED (agy footer) · first attempt returned empty; this is the one retry

DONE_WITH_CONCERNS

## Manifest (MD5 computed by the seat)
ARCHAEOLOGY.md E8200BC04BBFC91C66AC437E28A71B93 · SPEC.md 58A23C49223B7474A1F1964E7C5B8649 · docs/BRIEF.md 987B158C8A81A86728E8577A3BA769B9 · DIFF CF9F4E3C21C64B693437BE4811E0B37F · cfg 4278F5798A2554FF021B27E876C079EF · log 019F1B203FCE5A2670B32030EBA4A198 · fallen/GroundLabelManager.cs 89D6B6D3F1DACFDB8A094E653415E251 · src: AaronsHouse 8AD7F75C… AffixInjector DF8B37D6… BuildInfo 61081F45… Colors F618270A… FilterRuleTooltip DF527124… GroundLabels 007871FB… NativeSettings D4F9877D… Prefs EBBD6F26… SettingsUi BBAC9FD0… TerribleTooltipsApi 93822285… TerribleTooltipsMod 3AC02BF6… TooltipRecolor 6EB761CD…

## Findings
| Rank | Claim | Failure Mechanism | Verify-by / Repair |
|---|---|---|---|
| MATERIAL | Scan gate bypasses composition on Alt flip | `ShouldScan()` (TooltipRecolor.cs:86-104) checks `s_originals`. New EHG TMPs active on Alt range flip are unmonitored; `ShouldScan()` returns false, skipping `RunScan` and rendering raw `[1B]` text. | Hold Alt on 3.0.1; fix by marking dirty or scanning active un-marked TMPs on Alt state flip. |
| MATERIAL | Multi-stat affixes lack brackets in normal view | `TooltipItemManager.FormatAffix` (AffixInjector.cs:83-106) is bypassed by EHG's multi-stat formatters. `ComposeUnbracketed` synthesizes `[Tier N]` only under Alt when EHG emits `Tier: N`. | Inspect multi-stat idol in inventory; fix by patching EHG's hybrid/multi-stat affix formatters. |
| MINOR | Scan gate can still run every frame | If any tracked TMP in `s_originals` is overwritten by EHG without `Marker`, `ShouldScan()` (TooltipRecolor.cs:99) returns true continuously, causing stutter. | Profile frame times while moving; fix by tracking dirty flag on actual text mutations. |
| NOT PROVEN | TT 3.0.1 patch hides dropped item ground labels | `GroundLabels.cs` is unchanged in 3.0.1 diff; `TooltipRecolor.cs:281` explicitly skips `GroundLabels.Marker`. Zero code mechanism. | Test vanilla / 3.0.0 with identical keybind/config. |

## Analysis
Q1 — Mechanism in 3.0.1 diff: None. Ranked candidates: (1) plain Last Epoch 1.4.7 behaviour: native keybind 'Z' (toggle ground item labels) pressed, or native setting hiding ground labels; pickup requires clicking labels. (2) Config state (FilterOnly + NO FILTER + AltKey): GroundLabels.cs:109 → CheckFilter false (TerribleTooltipsApi.cs:34) → DelayRoutine yield-breaks without altering labels. (3) Fallen NPE in GameReferencesCache; but fallen/GroundLabelManager.cs:74 yield-breaks on !isUniqueSetOrLegendary(), so it ignores dropped idols anyway. (4) TT 3.0.1 diff: NOT PROVEN. Separating test: press 'Z' in-game on dropped items, or drop with GroundLabelFilterOnly=false under NO FILTER.
Q2 — Alt pressed → OnLateUpdate sets showRangesInsteadOfDescriptionEnabled → EHG re-renders → Patch_FormatAffix injects '[1B]' → EHG writes TMP → Patch_UpdateLayout → ShouldScan checks only s_originals → new TMPs unmonitored → false → RunScan skipped → raw '[1B]' remains.
Q3 — FormatAffix reach: No; EHG formats multi-stat/hybrid via separate formatters. Repair: patch EHG's multi-stat formatting methods directly.
Q4 — Unsound. Content change without scan: EHG writes to a TMP not in s_originals (e.g. comparison tooltip) → no scan until 0.5 s fallback. Per-frame: EHG continuously overwrites an active tracked TMP without Marker → ShouldScan true every frame.
Q5 — Alt/pins degraded when ShouldScan skips recycled/new TMPs. LeHud/Fallen: NPE independent; faceColor preservation (GroundLabels.cs:198) intact. Ground labels untouched; FilterRuleTooltip.cs:39,59 marks dirty; TooltipRecolor.cs:296 skips 'requires'.

🟢 Gemini
