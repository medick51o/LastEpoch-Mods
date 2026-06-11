# ARCHAEOLOGY — Terrible Tooltips rebuild dossier (the legacy mod)

Started 2026-06-11. Sources: live API probe against the installed interop
assemblies (research/ApiProbe — re-runnable), the KG reference fork, the
SecondBrain harvest, and the April dev-transcript mining pass (appended
below when it lands). Andrew's framing: "my heart and soul... my legacy...
the one I want to stand the test of time."

## THE LEGENDARY GRADING BUG — root cause (zoundb's Nexus report, SOLVED)

**Symptom:** legendary uniques grade C across the board even at max rolls
(Legendary Nihilis Unbound Locket: 12% Mana at max of 1–12% → C, should be S;
8% MS at max → C; 7% Health mid-range → C, which was actually correct).

**Chain of causation:**
1. v1's `AffixInjector.Patch_UniqueFormatter` (and KG's original
   `Style1/2/Letter_Style_AffixRoll_Unique` — **the bug is inherited
   verbatim from KG**) reconstructs the roll from display values:
   `roll = (modifierValue - min) / (max - min)` using
   `uniqueEntry.mods[uniqueModIndex].value/maxValue`.
2. The game's actual roll storage (API-probe verified):
   `ItemDataUnpacked.uniqueRolls` (byte array 0–255) read via
   `getUniqueRoll(byte index)` — and the index is **`UniqueItemMod.rollID`**,
   NOT the mod's position in `uniqueEntry.mods`.
3. On legendaries, `GetUniqueLPRollMultiplier(Entry, Actor)` scales values,
   so the reconstruction's arithmetic no longer matches the static
   min/max table — at-max stats land mid-band (C).

**The fix (the rebuild's centerpiece):**
```csharp
UniqueItemMod m = uniqueEntry.mods[uniqueModIndex];
float roll = (!m.canRoll || m.value == m.maxValue)
    ? 1f                                         // fixed stat = perfect by definition
    : item.getUniqueRoll(m.rollID) / 255f;       // the stored truth — LP-immune
```
No reconstruction, no display-value parsing, exact on uniques AND
legendaries. Useful related API: `isLegendary()`, `isUnique()`,
`isUniqueSetOrLegendary()`, `UniqueItemMod.hideInTooltip`,
`UniqueItemMod.GetHighestRollThatResultsInValue(float)` (the game's own
inverse, available as a cross-check).

## API truth table (probe-verified 2026-06-11, current game build)

| Concern | Truth |
|---|---|
| Affix roll (craftable) | `ItemAffix.getRollFloat()` (0–1), `ItemAffix.DisplayTier` (T1–T7+) — v1's usage is CORRECT |
| Implicit roll | `ItemDataUnpacked.getImplictRollFloat(byte implicitNumber)` — v1 CORRECT |
| Unique/legendary mod roll | `getUniqueRoll(UniqueItemMod.rollID) / 255f`, gated on `canRoll` — v1 WRONG (reconstructed) |
| Unique mod table | `UniqueList.instance.uniques[item.uniqueID].mods` — fields: `value` (min), `maxValue`, `canRoll`, `rollID`, `hideInTooltip`, `property`, `tags`, `type` |
| Tooltip format hooks | `TooltipItemManager.AffixFormatter` / `UniqueBasicModFormatter(item, ref result, int uniqueModIndex, float modifierValue)` / `ImplicitFormatter` — Harmony postfixes on all three cover every stat line |
| Legendary detection | `isLegendary()`, `legendaryPotential` (byte), `weaversWill` (byte) |
| uniqueID bound check | v1 has `item.uniqueID > uniques.Count` — off-by-one; should be `>=` |

## Inherited-from-KG inventory (credit him; surpass him)

- The bracket format (`[<color>5</color><color>A</color>]`) is deliberately
  KG-compatible — TooltipPatch's regexes parse what AffixInjector writes.
  Self-imposed legacy: the mod talks to itself through KG's string format.
  REBUILD QUESTION: keep the bracket-string pipeline (proven, ecosystem
  visible) or pass structured data internally and only RENDER KG-style?
- The unique-roll reconstruction (now fixed, above).
- Settings-injection Utils.cs (the family's hardened NativeSettings
  template now supersedes it — fog_OF_war lineage).

## Pending product changes (pre-rebuild commitments)

1. zoundb's legendary grading fix (above) — the headline.
2. `ShowFilterRuleNumber` default Off → NumberOnly (April decision).
3. Working-tree debug noise: 2 uncommitted `MelonLogger.Msg` lines in
   FilterRuleTooltip.cs (April leftovers) — superseded by rebuild.

## Known engineering facts (from brain + prior recon)

- LeHud compat: TMP `faceColor` is wiped by `.text` writes → SetText helper
  saves/restores faceColor. Harmony on OpenItemTooltip collided with
  LeHud's OpenTooltip patch (IL2CPP circular trampoline → infinite
  recursion) → Tooltips uses Harmony-free OnUpdate monitoring for that
  path. BOTH mechanisms must survive the rebuild.
- Fallen Star treaty: TT owns normal/magic/rare ground labels; Fallen owns
  unique/set/legendary ground labels. Rule # stays GOLD (not filter-colored)
  out of brand respect. Treaty survives verbatim.
- MedianAura's fix: EHG's ground-label rule number "(69)" (parenthesized)
  must not be overwritten — 3-option display dropdown + position control.
- IL2CPP: ItemTooltipAffix._tierText / ItemTooltipModifier._rangeText are
  the real tier/range components (UITooltipItemAffix was the wrong guess);
  patch UpdateLayout not UpdatePrefixAndSuffixesText (crashes); Transform
  keys via GetInstanceID (Il2Cpp Transforms don't hash); Harmony patch
  parameter names must match the game method exactly ('data', 'value').
- Colors (frozen brand language): T1 #DADADA / T2 #E1E1E1 / T3 #16FF0E /
  T4 #77ACFF / T5 #A807FF / T6 #FA9E3D / T7+ #FF44FF (Mythic Pink).
  Grades: F <50 / C <70 / B <80 / A <95 / S ≥95 (on roll×100).
  RollColor bands for range lines. WoW-retina language — zero learning curve.
- Aaron's House easter egg: INTENTIONAL quirk (preserve); teleport must be
  unlock-gated like the map (softlock lesson); Divine-era map-tab population
  requirement — the silent-primer technique from Terrible Inventory now
  solves this properly.
- AssemblyName `medick_Terrible_Tooltips` (underscore) — DLL-name gotcha.

## Transcript mining results (6-miner dig, 2026-06-11 — distilled; full raw
## findings preserved in the workflow output, this is the load-bearing set)

### The mission (Andrew's words — the rebuild's north star)
- Genesis: inspired by Fallen Star's tooltips + EHG_Mike saying a filter-match
  feature was "too difficult" on stream → "i want to make a version so
  outrageously good that EHG will notice it and at least put a basic core
  function into the game... my most ambitious project yet."
- Color language: WoW item-quality ladder + D4 capstone — "sense of comfort
  and familiarity and thats what i want to bring to players."
- THE direction message (his caps: "THIS IS VERY IMPORTANT"): affix NAME
  colored by TIER; grade letter "(a)" appended in GRADE color; Range line in
  grade color with NO letter; the game's own line colors OVERRIDDEN.
  He rejected shipping KG's many display modes — the featured set only.
- Mythic Pink #FF44FF is his signature ("I am a diablo 4 guy"); NOTE: Nexus
  copy/legend drifted to #ff44aa — code is canonical, fix the marketing.
- Gray F is a deliberate insult: "telling the player ya that roll sucks bro".
- "(PoG) ◄─── TIER & GRADE LEGEND ───► (RiP)" rows + the 69 example +
  real explanations on every setting = intentional brand. PRESERVE.

### Pipeline architecture (shipped v1.5.0 — what the rebuild replaces/keeps)
- Two-stage TEXT pipeline: AffixInjector writes KG-format brackets into
  TooltipItemManager formatter strings → TooltipPatch (UITooltipItem.
  UpdateLayout postfix) re-finds ALL TextMeshProUGUI via FindObjectsOfType
  EVERY layout, re-parses brackets with 5 compiled regexes, strips, recolors.
  KG main line + EHG "Tier:" + "Range:" all live in ONE TMP per affix.
- EHG resets standalone Tier TMP colors after UpdateLayout →
  s_tierColorCache re-applied every OnLateUpdate.
- Set/unique Range-only TMPs inherit grade color via parent-transform map
  keyed by GetInstanceID (Transform keys silently fail under IL2CPP).
- REBUILD QUESTION (flagged): a data-pipeline reading
  ItemTooltipAffixInfo/ItemTooltipModifierInfo (or GetUniqueModifierInfoFromMod
  w/ Entry_UniqueModDisplayListEntry — the modern unique-line source and prime
  suspect for the legendary index/value semantics) could kill the regex layer
  and the full-scene TMP scans. Weigh against: the bracket pipeline is
  battle-tested and ecosystem-visible.

### Unpatchable/forbidden (laws written in crashes)
- NEVER patch TooltipItemManager.OpenItemTooltip — same native fn as LeHud's
  OpenTooltip → circular trampoline → instant stack overflow (registering the
  patch crashes, even with an empty body; HarmonyPriority made it worse).
- UITooltipItem.UpdatePrefixAndSuffixesText (#29) is intrinsically
  unpatchable (0xc0000005 even with empty postfix). Safe: UpdateLayout (#31),
  SetAsItemTooltip (#17: ttInfo, Vector2, GameObject, _item, targetSlot),
  SetAsGroundTooltip (#23: + compare bool) — param names recovered by raw
  byte-scan; HarmonyX matches BY NAME ('data', 'value' laws).
- Never inject Il2Cpp generic List params into patches.
- TooltipContentBuilder.GetDescription: ref __result does NOT propagate
  (native invoke) — dead end.
- FilterRuleTooltip v1.5.0 shape: SetAsItemTooltip/SetAsGroundTooltip
  postfixes capture ItemDataUnpacked; MonitorUpdate (from OnUpdate) injects
  the gold Rule # into the 'requires' TMP after render (loreText invisible on
  non-uniques; PrefixHeader gets overwritten — both tried, both dead).

### Ground labels (battle-tested subsystem — minimal change)
- GroundItemLabel.SetGroundTooltipText(bool) postfix + ONE-frame coroutine;
  direct text write via SetText (faceColor save/restore) — the old
  tmp.text = "" vertex-buffer ritual was RETIRED in v1.5.0's LeHud truce
  (the blank frame it caused was something other mods could react to);
  sceneFollower?.calculateDimensions() after; Marker = three zero-width
  spaces (load-bearing twice: double-processing guard + MedianAura strip).
- MedianAura fix: REUSE EHG's written text as base (preserves the native
  "(53)" rule number — parenthesized regex \((\d+)\)), never rebuild from
  FullName; never ToUpper the assembled rich text (breaks <color> tags).
- RuleNumberPosition { EHGDefault, Start, End }; GroundLabelStyle
  { None, TierAndRank, TierOnly, RankOnly } + FilterOnly bool + AltKey bool
  (alt-cache with state-change-only swap; both cached strings keep Marker).
- Known fragility: rule number arriving a frame late is lost forever
  (Marker blocks reprocessing) — rebuild candidate fix.

### Ecosystem treaties (FROZEN — contracts, not preferences)
- TerribleTooltipsAPI.CheckFilter stays public and KG-signature-shaped:
  Fallen Star detects mods by RegisteredMelons NAME — TT's MelonInfo name
  "Terrible Tooltips" is part of the public ABI. rarity==9 short-circuit.
- Fallen treaty: TT skips isUniqueSetOrLegendary() ground labels entirely.
- Rule # stays GOLD #FA9E3D — "lets leave it that way" (brand respect).
- LeHud: SetText faceColor save/restore on every ground-label write; never
  call LoadFilter("") (LeHud silently suppresses it); LeHud's own settings
  are an asset-bundle overlay (no SettingsPanelTabNavigable collision).

### Aaron's House (intentional quirk — preserve verbatim)
- "(don't press this button)  For the homies  ♥  AaronActionRPG" + the
  map-tab workaround joke in the description; always LAST in settings.
  Unlock-gated like the map (softlock lesson). Andrew DELIBERATELY declined
  to import Advanced Inventory's primer ("banking on" users having both
  mods) — the jank is canonized. Rebuild may keep the joke text even if the
  primer tech could now fix it; that's HIS call to change, not ours.

### Engineering traps (rebuild checklist)
- Stale-DLL fiasco: csproj post-build copy exits 1 while the game runs and
  hours of tests ran old code ("YOU PROBABLY DID IT HOURS AGO LOL") —
  deploy must fail loudly; verify DLL timestamp in any test ritual.
- AssemblyName medick_Terrible_Tooltips (underscore) vs folder name
  produced TWO DLLs; v1.2.0 shipped the wrong one to Nexus. One name, ever.
- Standalone port traps: Il2Cpp List<Rule>.get() vs managed indexer;
  settings rows inserted after-header render REVERSED (append to category
  bottom); cloned toggle template has ONE text component (descriptions
  needed explicit support).
- Colors.RollColor 7-band fn is near-vestigial (pipeline colors Range lines
  with the parsed GRADE color) — audit in rebuild.
- v1.5.1 acceptance: legendary max-roll grades S (test: known max-roll
  legendary); ShowFilterRuleNumber default Off → NumberOnly.
