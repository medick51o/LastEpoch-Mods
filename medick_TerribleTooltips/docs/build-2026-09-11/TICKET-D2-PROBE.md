# TICKET-D2-PROBE — one cloned native pill on one affix line (bounded, fail-loud, STAGE ONLY)
Version target: **3.1.0-dev**. Builder: 🔵 Codex (workspace-write). Reviewer: 🟢 Gemini (fallback 🟣➤🌙 Kimi). Ruling basis: docs\design-2026-09-11\RULINGS-andrew.md (08:41 APPROVED: D2 native pill + GREATER-AFFIX TINT #C990FF). Council basis: docs\design-2026-09-11\signed\SIGNED-codex-astra.md §D2 (six donor candidates, positioning plan, twelve failure modes, exit criteria).

## Repo layout (note the nested folder)
Repo root: `C:\Users\andre\Downloads\LastEpoch-Mods\medick_TerribleTooltips`
Project: `medick_TerribleTooltips\medick_Terrible_Tooltips.csproj` (AssemblyName `medick_Terrible_Tooltips`, `<Version>3.0.2</Version>`, CopyToMods target skipped when `-p:DeployToMods=false`).
Source: `medick_TerribleTooltips\src\*.cs`. Entry: `TerribleTooltipsMod.cs` (`HarmonyDontPatchAll`, per-feature `TryPatch`). Prefs: `Prefs.cs` (category `medick_Terrible_Tooltips`, entry names FROZEN for existing keys; new keys allowed). Version lockstep: `BuildInfo.Version` + csproj `<Version>`.

## WRITE SET (absolute fence; anything else = containment failure)
1. `medick_TerribleTooltips\src\PillProbe.cs` — NEW, the whole probe.
2. `medick_TerribleTooltips\src\TerribleTooltipsMod.cs` — add ONE `TryPatch(typeof(PillProbe.Patch_UpdateLayout), "pill probe (D2)")` line at the END of `ApplyPatches`, and ONE `PillProbe.OnLateUpdate();` call at the END of `OnLateUpdate`. Nothing else.
3. `medick_TerribleTooltips\src\Prefs.cs` — add three entries (create in `Init`, after `DebugLog`, no other changes): `PillProbe` bool default **false** (description: "D2 PROBE: clone one native pill onto one affix line. Experimental; logs loudly; off by default."), `PillTiers` enum `PillTierMode { All, GreaterOnly }` default **All** (description: "Which tiers get the probe pill: All, or only Tier 6/7 (greater affixes)."), `GreaterAffixTint` string default **"#C990FF"** (description: "GREATER-AFFIX TINT for Tier 6/7 affix sentences (ruled 2026-09-11). Consumed by the 3.1.0 composer ticket; the probe only logs it.").
4. `medick_TerribleTooltips\src\BuildInfo.cs` — `Version = "3.1.0-dev"`.
5. `medick_TerribleTooltips\medick_Terrible_Tooltips.csproj` — `<Version>3.1.0-dev</Version>`.
6. `CHANGELOG.md` — an `## v3.1.0-dev (unreleased) — D2 probe` entry, three lines.
FORBIDDEN: `TooltipRecolor.cs`, `AffixInjector.cs`, `GroundLabels.cs`, `FilterRuleTooltip.cs`, `Colors.cs`, `NativeSettings.cs`, `SettingsUi.cs`, `AaronsHouse.cs`, `TerribleTooltipsApi.cs`, README, mockups, docs. Never patch `OpenItemTooltip` or `UpdatePrefixAndSuffixesText`; no Il2Cpp generic List params in any patch signature; parameter names copied verbatim from the game (see the existing `TooltipRecolor.Patch_UpdateLayout` for the exact `UpdateLayout` signature and reuse it).

## Behaviour (all of it gated on `Prefs.PillProbe.Value == true` AND `Prefs.EnableTooltips.Value == true`; when either is false the probe does NOTHING and any existing pill is hidden and margins restored)
### Patch
`PillProbe.Patch_UpdateLayout`: a Harmony **postfix** on the same `UITooltipItem.UpdateLayout` the composer already patches (a second postfix is fine; ours runs after the original and after/before the composer's, order not guaranteed, so it must be idempotent). Body = `try { Run(__instance); } catch (Exception ex) { Fail("UpdateLayout", ex); }`. It must be cheap when nothing changed (compare a per-tooltip generation: TMP instance id + text hash; if unchanged, only re-sync position).

### Stage A — hierarchy dump (ONCE per session, first tooltip after arming)
Walk `__instance.transform` recursively. For every `UnityEngine.UI.Image`: log `[PillProbe] IMG <full path> sprite=<sprite name or null> type=<image.type> border=<sprite.border or -> rect=<w>x<h> raycast=<bool>`. For every `TextMeshProUGUI`: log `[PillProbe] TMP <full path> len=<text.Length> first30='<...>' margin=<margin>`. Then `[PillProbe] dump done: N images, M tmps`. This is the evidence for donor selection even if everything below fails.

### Stage B — donor selection (ONCE per session; cache the donor transform; re-run if it is destroyed)
Try Astra's candidates IN THIS ORDER, log each attempt as `[PillProbe] donor try <k>: <what> → <found path | none>`:
1. An `Image` on the parent or a sibling of the FIRST affix TMP (the first TMP under the tooltip whose text starts with `Tier ` or contains `<mark=` or whose object name contains `Affix`, case-insensitive; log which rule matched).
2. An `Image` on the parent/sibling of any TMP whose text starts with `Range:` (ItemTooltipModifier range widget).
3. Any `Image` whose object name contains `Weaver`, `Sealed`, `Enchant`, `Highlight` (case-insensitive) inside the tooltip.
4. Any `Image` whose object name contains `Potential`, `Weaver`, `LP`, `Frame`, `Badge`, `Pill`, `Tag` (case-insensitive) inside the tooltip.
5. The inventory Sort button's background Image, found by `GameObject.Find` on a name containing `Sort` under an active `Inventory` canvas (may be inactive; skip if not found).
6. Any `Image` with a non-zero `sprite.border` (a sliceable sprite) anywhere in the tooltip hierarchy.
A donor is accepted only if it has an `Image` with a non-null sprite. If none: log ONCE `[PillProbe] no donor found — degrading to the text path (nothing changed)` and latch the probe OFF for the session. Never fabricate a sprite; never draw a flat-colour Image as a "pill".

### Stage C — one pill on one line
Target line = the FIRST affix TMP found in Stage B rule 1 (the same rule, so the target is deterministic). If `PillTiers == GreaterOnly`, the target is instead the first affix TMP whose parsed tier is 6 or 7; if there is none on this item, log `[PillProbe] no T6/T7 line on this item — no pill (GreaterOnly)` and do nothing for this tooltip.
Parse tier and grade from the TMP's own text with tags stripped: regex `Tier\s*(\d)` and a following single letter in `[FCBAS]`; if the tier is missing, label `Tier ?` and log it. The probe has NO roll data of its own and must not compute grades.
Create (ONCE per target TMP, keyed by TMP instance id; reuse on later passes; never per frame):
- `GameObject pill = Object.Instantiate(donor.gameObject, targetTmp.transform.parent)` named `TT_PillProbe`. Strip: every component that is not `RectTransform`, `CanvasRenderer`, `Image` (destroy Button/Selectable/EventTrigger/Animator/LayoutElement/LayoutGroup/ContentSizeFitter/LocalizeStringEvent/any script). Set `image.raycastTarget = false`. Add a `LayoutElement` with `ignoreLayout = true`. Keep the donor's `Image.type` and sprite; set `image.color = Color.white` (so the sprite's own art shows; log if the sprite is a solid white square).
- Label: `Object.Instantiate(targetTmp.gameObject, pill.transform)` named `TT_PillProbeLabel`, strip everything but `RectTransform`, `CanvasRenderer`, `TextMeshProUGUI` (and its required `CanvasRenderer`), `raycastTarget=false`, `enableWordWrapping=false`, `alignment=Center`, `fontSize = targetTmp.fontSize * 0.9f`, text = `<color={tierHex}>Tier {n}</color> <color=#8A8478>|</color> <color={gradeHex}>{G}</color>` using `Colors.TierColor(n)` and `Colors.GradeLetterColor` semantics already in Colors.cs (map the letter back to its colour: F #DADADA C #77ACFF B #A807FF A #FA9E3D S #FF44FF; write a tiny local switch, do NOT edit Colors.cs).
- Size: pill width = label.preferredWidth + 12, height = targetTmp's first line height (from `textInfo.lineInfo[0].lineHeight` after `ForceMeshUpdate()`); log both.
- Gutter: save `targetTmp.margin` once (per TMP id), then set `margin = new Vector4(saved.x + pillWidth + 6, saved.y, saved.z, saved.w)`. Log `[PillProbe] margin <saved> → <new>`.
- Position: anchor/pivot top-left; local position = TMP rect top-left + (saved.x, -(first line ascender offset)) so the pill's vertical centre sits on the first line's centre (`lineInfo[0].baseline`, `ascender`, `descender` are available after `ForceMeshUpdate()`). Re-sync position on every postfix and in `OnLateUpdate` while the tooltip is active (cheap: just set anchoredPosition/sizeDelta; no allocation).
- Log ONCE per target: `[PillProbe] pill placed on '<first 30 chars>' tier=<n> grade=<G> at <anchoredPosition> size <w>x<h> donor=<path>`.
### Lifecycle (Astra's failure list is the checklist)
- Pooled reuse: if the target TMP's text no longer parses as an affix line, or the tooltip is inactive, or the TMP was destroyed → hide the pill (`SetActive(false)`), restore the saved margin, log once `[PillProbe] pill hidden: <reason>`.
- Never two pills on one TMP; never a pill on a TMP that is not the current target; never a stale pill visible after an item change (check every postfix and every LateUpdate while active).
- Master off / PillProbe off at runtime → destroy all pills, restore all margins, log once.
- `Fail(stage, ex)`: log `[PillProbe] FAIL at <stage>: <ex.GetType().Name}: <message>` ONCE, restore margins, destroy pills, latch OFF for the session. The rest of the mod must be unaffected (the postfix's try/catch guarantees the game's layout call returns normally).
- No `GameObject.Find` or full-scene scans per frame; only in Stage A/B (once).

## Logging contract
Every line prefixed `[PillProbe]`. Startup: `[PillProbe] armed (PillTiers=<mode>, GreaterAffixTint=<hex>)` when on; nothing when off. Everything above at `MelonLogger.Msg` level (this is a probe; the noise is the point). Nothing else in the mod changes its logging.

## Build & stage
`dotnet build medick_TerribleTooltips\medick_Terrible_Tooltips.csproj -c Release -p:DeployToMods=false` → `medick_TerribleTooltips\bin\Release\net6.0\medick_Terrible_Tooltips.dll`. 0 warnings target (report any). NEVER copy into the game's Mods folder. No git.

## Done-when (builder reports)
Files changed = exactly the write set; build output path + size; the three new cfg keys and their defaults; the first 12 lines of `PillProbe.cs` (header comment must name the ticket and the degrade rule); one paragraph on anything you could not implement as specified and why. Sign "🔵 Codex".
