# SPEC — MedicK's Terrible Tooltips v2.0.0 (the legacy rebuild)

*a tooltip mod* — **Terrible Tooltips**. The heart-and-soul mod: "the one I
want to stand the test of time." Companion: [ARCHAEOLOGY.md](ARCHAEOLOGY.md)
(API truth table, the solved legendary bug, the intent registry, the laws
written in crashes). This spec assumes all of it.

## The mission (unchanged since genesis)

A stranger installs this mod and their WoW-trained eyes just *know*:
gray → green → blue → purple → gold → **Mythic Pink**, F through S.
Zero learning curve, zero deciphering. "So outrageously good that EHG will
notice it." The first mod people hunt for when a new season drops.

## Frozen identifiers (the public ABI — breaking any of these breaks the ecosystem)

- MelonInfo name **"Terrible Tooltips"** — Fallen Star detects this mod BY
  THIS STRING in RegisteredMelons. It is an API, not a label.
- Assembly/DLL: `medick_Terrible_Tooltips` (underscore — the v1.2.0
  wrong-DLL incident is not to be repeated; exactly one DLL may exist)
- Prefs: category `medick_Terrible_Tooltips`, cfg
  `UserData/medick_Terrible_Tooltips.cfg`, all v1 entry names
  (EnableTooltips, TooltipTierColors, TooltipRankColors, GroundLabelStyle,
  GroundLabelFilterOnly, GroundLabelAltKey, ShowFilterRuleNumber,
  LabelRulePosition) and enum member names/orders
- `TerribleTooltipsAPI.CheckFilter(ItemDataUnpacked, out Rule, bool bypass)`
  — public, KG-signature-shaped, rarity==9 short-circuit, inverted-index
  rule lookup. Verbatim.
- The color language (Colors.cs values are canon; the Nexus copy's #ff44aa
  drift gets corrected TO the code, never the reverse):
  T1 #DADADA · T2 #E1E1E1 · T3 #16FF0E · T4 #77ACFF · T5 #A807FF ·
  T6 #FA9E3D · T7+ #FF44FF — grades F<50 / C<70 / B<80 / A<95 / S≥95,
  letter colors F #DADADA / C #77ACFF / B #A807FF / A #FA9E3D / S #FF44FF.

## The headline fix: legendary grading (zoundb)

`AffixInjector` unique-mod path replaces KG's display-value reconstruction
with the game's stored truth:

```csharp
UniqueItemMod m = uniqueEntry.mods[uniqueModIndex];
float roll = (!m.canRoll || m.value == m.maxValue)
    ? 1f                                        // fixed stat = perfect
    : item.getUniqueRoll(m.rollID) / 255f;      // authoritative, LP-immune
```

Guards: `uniqueID >= uniques.Count` (fix v1's off-by-one), rollID bounds
against `uniqueRolls.Length` with the old reconstruction as a LOGGED
fallback. Acceptance: a known max-roll legendary grades S; plain uniques
keep grading as before; implicit/affix paths untouched (they were correct).

## Pipeline decision: keep the bracket pipeline (v2), document the v3 pivot

The two-stage text pipeline (AffixInjector writes KG-format brackets →
TooltipRecolor parses and recolors on `UITooltipItem.UpdateLayout`) is
battle-tested across seasons and stays. The five regexes, the
one-TMP-per-affix reality, the GetInstanceID parent-inheritance for
set/unique Range lines, and the OnLateUpdate tier-color cache all carry
over with their hard-won exactness. A future data-pipeline
(ItemTooltipModifierInfo / GetUniqueModifierInfoFromMod) is documented as
the v3 candidate — not attempted in the legacy rebuild's risk budget.

## Laws (each one written in a crash — see ARCHAEOLOGY)

1. NEVER patch `TooltipItemManager.OpenItemTooltip` (LeHud circular
   trampoline). FilterRule capture stays on `SetAsItemTooltip` /
   `SetAsGroundTooltip` + MonitorUpdate injection into the `requires` TMP.
2. `UpdatePrefixAndSuffixesText` is unpatchable. `UpdateLayout` is the hook.
3. No Il2Cpp generic List parameters in any patch signature; parameter
   names copied verbatim from the game ('data', 'value', 'ttInfo', '_item').
4. Ground labels: one-frame coroutine;
   `sceneFollower?.calculateDimensions()`; three-zero-width-space Marker
   (double-process guard AND MedianAura strip); reuse EHG's written text as
   the base (never rebuild from FullName); `\((\d+)\)` parenthesized rule
   regex; never ToUpper assembled rich text. (The old `tmp.text = ""`
   vertex-buffer ritual was deliberately RETIRED in v1.5.0's LeHud truce —
   direct write avoids a blank frame other mods could react to; SetText's
   faceColor save/restore is the canonical form.)
5. LeHud truce: SetText faceColor save/restore on every ground-label write;
   never call `LoadFilter("")`.
6. Fallen treaty: `isUniqueSetOrLegendary()` ground items are skipped,
   entirely, forever. Rule # stays GOLD #FA9E3D.

## Settings (native injection — hardened NativeSettings template, taxonomy applied)

Category **"Terrible Tooltips"**, rows in this order, titles keep their
mythic-pink color tags (brand voice):

| Row | Class | Notes |
|---|---|---|
| Terrible Tooltips (master) | toggle | verbatim v1 copy |
| Tooltip: Tier Colors | toggle | verbatim |
| Tooltip: Rank Colors | toggle | verbatim |
| Tier color ladder T7→T1 | **info row** | verbatim colors/copy, rendered visually non-interactive (no checkbox); the "this box is not clickable" apology DIES — the row simply looks like what it is |
| (PoG) S→F (RiP) grade ladder | **info row** | same treatment; PoG/RiP verbatim |
| Ground Label Style | dropdown (KEEP) | None/TierAndRank/TierOnly/RankOnly |
| Ground Labels: Filter Only | toggle | verbatim |
| Ground Labels: Hold Alt to Show | toggle | verbatim |
| Tooltip: Show Filter Rule # | dropdown (KEEP) | **default changes Off → NumberOnly** (the April decision, finally shipped); the "Maxroll told me to pick this up blah blah" description stays |
| Ground Label: Rule # Position | dropdown (KEEP) | verbatim |
| DEF/START/END worked example | **info row** | the 69 stays. Forever. |
| To Aaron's House | button (real) | description verbatim INCLUDING the map workaround joke — the jank is canon; unlock-gated travel; always LAST |

All rows idempotent-rebind, latched one-warning degradation, clones named
last + destroyed in catch — the fog_OF_war standard.

## Dev-noise audit (cut vs keep)

CUT: version drift (BuildInfo single source — v1 shipped three versions);
the two uncommitted April debug lines; per-event MelonLogger spam → Dbg
gate + one startup line; `Colors.RollColor` if the rebuild confirms nothing
calls it; "not clickable" apologies (replaced by honest info rows).
KEEP (intentional): every joke quoted in the intent registry, the
self-deprecating brand voice, Aaron's House in full, PoG/RiP, the 69,
"roll sucks bro" semantics (gray F insult), mythic-pink setting titles.

## Engineering standard (family playbook, full strength)

src/ modules · nested patches + `HarmonyDontPatchAll` with per-feature
TryPatch (a game update killing one hook degrades ONE feature: tooltip
colors, ground labels, rule #, settings, Aaron — each falls alone) ·
BuildInfo lockstep with csproj `<Version>` · deploy must fail loudly
(the stale-DLL fiasco) · csproj relic audit · README/CHANGELOG rewritten
with KG credit prominent ("aint no punks and stealing peoples work") ·
review fleet: 7 lenses (+ ecosystem-compat verifying every treaty clause,
+ color-language clarity) · Andrew's acceptance tests: max-roll legendary
grades S; LeHud + Fallen co-installed session crash-free; a fresh-eyes
read of the settings panel "just clicks".

## v3 — THE CLEAN LINE (vision-interview contract, 2026-06-11)

The Karpathy interview surfaced the real project. Andrew's words: "we are
basically taking KG's mod and EHG's broken vision of a tooltip and doing
their job... our goal is to have them look at this and be like wow, why
isn't this in our game." The options are the pitch — an EHG dev should be
able to toy with the layouts and steal ideas.

**Mission:** one line per affix. Kill the essay. A 4-affix exalted reads in
five lines, not sixteen. The half-second decision ("pick it up / keep
blasting") never waits on reading.

**The line:** affix text (+ value as EHG renders it) + the SIGNAL =
`Tier N` spelled out in its tier color + grade letter in its grade color.
Untiered affixes: grade only. Sealed: dim "Sealed" + grade. Hybrid affixes:
ONE line — names joined, grade per stat (S·S).

**Layout presets — THE SHIP GATE: v3 does not release unless several work:**
- `BadgeLeft` (DEFAULT — Andrew's pick, "b with colors"): signal leads, text follows
- `SignalRight`: text left, signal right-aligned column
- `Trailing`: text, then signal right behind it

**Toggles (zero-settings perfection for the homies; knobs for the sweats):**
- Affix name color: TierColor (default) | GameDefault (white)
- Grade letter: shown (default) | hidden ("if they want the ABC gone they get it gone")
- Ranges + craft detail: HIDDEN by default ("it's freaking clutter") —
  **Hold-Alt = deep view** (same Alt muscle as ground labels); per-detail
  settings pins exist but default OFF

**Untouched, forever (this round):** ground labels (the KG bracket is
beloved — question parked, default = no change) · all frozen ABIs ·
LeHud truce · Fallen Star treaty · crash laws (the clean line lives inside
the existing UpdateLayout postfix reality — UpdatePrefixAndSuffixesText
remains unpatchable, OpenItemTooltip remains forbidden).

**Sequencing (Andrew's call):** v2.0.0 = GitHub-only, never uploads to
Nexus. The Nexus debut IS v3 — the legacy arrives looking like it belongs
in the game. (Open call for Andrew: whether to reply to zoundb before v3
ships, since the legendary fix is in v2/GitHub but won't reach Nexus until v3.)

**Audience order (conscious choice):** homies → sweats → WoW/D4 vets → Andrew.
Mockups: `medick_TerribleTooltips/mockups/tooltip-flow-v3.html`.
Parked ideas: player-custom color presets (fights "deepen, don't widen" — his call later).
