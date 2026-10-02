# Boss and Harbinger return

Read this file first. The claim table and the verdict counts live in `research/boss-harbinger/factcheck.md`. Proposed profiles live in `GROKBOT-BOSS-HARBINGER-CATALOG.json`. The coverage matrix lives in `GROKBOT-BOSS-HARBINGER-MATRIX.csv`.

This is not a statement that all Harbingers are handled. The matrix has 53 encounters. Fourteen Harbinger names appear across the sources (ten timeline Harbingers on the preview table, plus Cruelty, Brutality, Regret, and Denial). Corruption minimums, inherited move lists, live 1.5.1 coefficients, and localized killer strings are unresolved on those rows. Damage coverage is `unknown` on 52 of 53 catalog encounters. Julra is the only `known` coverage, and it is encounter scope, not a per-attack map.

## Baseline identity

| item | value |
| --- | --- |
| Mod | Terrible Death Log, Assessment and Counter, version 0.1.2 |
| Source zip SHA256 supplied by the brief | `bf9b28ac9e61a1efeebbea82817de74c601f07d2d6a852729a70e3cf00f5f948` |
| Baseline commit | `88dcaaa` on `cursor/boss-research-base` |
| Lane 6 schema commit this synthesis started from | `f31c140` on `cursor/boss-lane6-catalog` |
| Manifest prefix supplied by the brief | `223e851a` (full digest not recomputed; see fact-check C47) |
| Fact-check access date | 2026-10-02 |
| Live patch announced that day | 1.5.1, forum post by EHG_DerrickG at 2026-10-02T16:58:00Z |

Season 5 went live on 2026-10-01T16:12:40Z. Hotfix 1.5.0.1 (2026-10-01T22:42:16Z) is a login fix and a Blood Crystal menu fix. The notes pages at `lastepoch.com` returned a Nuxt shell with no note body, so specific 1.5 bugfix lines are not confirmed.

Tunklab's homepage footer says "Game Version: 1.5 preview". The client bundle contains `1.5-preview1` fourteen times. Every Tunklab row in the catalog is that preview extract, not a 1.5.1 dump.

## Roster coverage

53 encounters, matching the lane 1 matrix after the fact-check notes were applied.

| kind | rows |
| --- | --- |
| Timeline bosses | 10 |
| Timeline Harbingers | 10 |
| Campaign bosses | 9 |
| Dungeon bosses | 4 |
| Harbinger adds (Regret, Denial, and two others in the lane matrix) | 4 |
| Pinnacle | 3 |
| Adds | 3 |
| Proxies | 2 |
| Seasonal bosses | 2 |
| Woven bosses | 2 |
| Event actor, seasonal wave, quest miniboss, shade | 1 each |

The ten timeline pairs were re-read on `https://lastepoch.tunklab.com/timelines`:

| timeline | level | boss | Harbinger | stability |
| --- | --- | --- | --- | --- |
| Fall of the Outcasts | 62 | Abomination | Harbinger of Defilement | 240 / 800 |
| The Stolen Lance | 66 | God Hunter Argentus | Harbinger of Pride | 250 / 800 |
| The Black Sun | 70 | Rahyeh, The Black Sun | Harbinger of Hatred | 250 / 800 |
| Blood, Frost, and Death | 74 | Frost Lich Formosus | Harbinger of War | 300 / 800 |
| Ending the Storm | 78 | Lagon, God of Storms | Harbinger of Chaos | 320 / 850 |
| Fall of the Empire | 82 | Harton's Husk | Harbinger of Treason | 320 / 850 |
| Reign of Dragons | 85 | Emperor of Corpses | Harbinger of Destruction | 400 / 850 |
| The Last Ruin | 90 | The Husk of Elder Gaspar | Harbinger of Fear | 400 / 900 |
| The Age of Winter | 90 | Heorot | Harbinger of Tyranny | 400 / 900 |
| Spirits of Fire | 90 | Volcanic Shaman | Harbinger of Ash | 400 / 900 |

Those levels match the Season 3 note (Yayifications, 2025-08-15) for the six timelines that note changed. That is agreement between a 1.3 developer post and a 1.5 preview table. It is not a 1.5.1 client dump.

God Hunter Argentus is not Heorot. Emperor's Remains is not the Emperor of Corpses. Altar Aberroth is not the Herald of Oblivion page. Formosus the Undying is a custom name on Frost Lich Formosus, whose base level is 76 while the timeline area level is 74.

## Highest-priority verified additions

These are safe to put in front of a player, with the evidence class on the catalog claim.

1. Ten timeline boss to Harbinger name pairs, normal area levels, and stability, labeled as Tunklab 1.5 preview.
2. Lagon, campaign and monolith, is damaged through two tentacles that share his health (EHG 0.9.2). The preview description still says "can only be damaged by attacking tentacles". A tentacle actor reuses the display name "Lagon, God of Storms".
3. Soul Bomb deals less damage farther from its center (EHG 0.9l). No element and no radius.
4. Julra: Temporal Shift does not move you. Encounter damage line is Void, Cold, Lightning. A 1.3 note adds an indicator the first time she uses her most powerful attack. The attack is unnamed.
5. Heorot's extracted description says "Cannot be Frozen". The 1.0.3 Ice Spike sentence is a multiplayer bugfix, not a damage type.
6. Harbingers keep an Agile or Brute style and also gain abilities from the timeline boss (EHG 2024-06-25). That is why Harbinger of Hatred is not Rahyeh's full move set.
7. God of Bloodshed, also called Pinnacle Morditas, is placed at 400 corruption and does not scale with corruption (EHG 2026-09-21). Patch 1.5.1's forum post enables it for Legacy and Offline.
8. Herald of Oblivion is the Tunklab title, with subtitle Uber Aberroth, Harbinger of Regret in phase 2, and Harbinger of Denial in phase 4. Do not merge that page with altar Aberroth.

The catalog carries 13 tips, two or three only where a fact-check passed, ordered by lethal risk, with no double dashes. They are tactical inference at low confidence. Profiles with tips: `lagon-campaign`, `lagon-monolith`, `emperor-of-corpses`, `heorot-monolith`, `julra`, `harbinger-hatred`, `god-of-bloodshed`. Every other profile has an empty tip list on purpose.

## Current gaps

- Per-attack elements for every encounter except Julra's encounter-level line.
- Soul Bomb element, Dive Bomb element, and the Lagon tidal-wave record (array versus description versus tags).
- Whether the 1.4.2 Soul Bomb damage cut is still live.
- Whether 1.2.1's Time Rot and Damned removal is still live. The preview bundle still contains the string "Aberroth's Time Rot".
- Corruption minimums, the 10-eye gate, and which absorbed abilities each Harbinger still has.
- Empowered area level, except where a re-opened NPC page shows an experience line (Heorot and Frost Lich show 100). Not generalized.
- Localized killer strings, especially Lagon versus his tentacle, and Herald of Oblivion versus Uber Aberroth versus Aberroth.
- Cruelty, Brutality, Mountain Beneath, Cremorus, Yrun, and the Morditas internal ids. Those pages were not re-opened.
- Lane 4 signatures. The DLL is absent here.
- The 1.5 and 1.5.1 note bodies.

## Corrections to the five starter profiles

Code still has exactly those five profiles. New encounters were not added to `BossCatalog.cs`.

| profile | what changed | what stayed |
| --- | --- | --- |
| Lagon | Mechanics now say the 0.9 names are not a current skill list. Both movement tips deleted. | Empty damage types. Alias remains "Lagon" only, which does not match "Lagon, God of Storms". |
| Emperor of Corpses | Both Soul Bomb tips deleted. | Distance-from-center mechanics sentence. Empty damage types. |
| Heorot | Mechanics now quote the 1.0.3 bugfix. Both tips deleted. | Empty damage types. Ice Spike is not given an element. |
| Julra | Both tips deleted, including "safe timing". | Void, Cold, Lightning as encounter coverage. The position fact is in the JSON tip, not in code, because the old code tip mixed it with the unsupported timing sentence. |
| Harbinger of Hatred | Telegraph tip and "use the captured ability" deleted. | One tip: a Harbinger imitation is not the original boss's full move set. Empty damage types. The 1.2.1 name stays in the mechanics sentence. |

The sentence "Campaign and Monolith variants may differ" was removed. The preview actors share five skill ids and differ by level.

`BossCatalogSchemaTests` no longer edits `julra` tips, because that list is empty. It rewrites a mechanics claim instead, and still checks that the saved reassessment does not absorb the rewrite.

## What Codex can port now

- The JSON catalog and the matrix, as proposed data. Do not copy preview coefficients into `DamageTypes`.
- Deletion of the unsupported starter tips and of the variants sentence.
- The tightened Lagon and Heorot mechanics sentences.
- The one remaining Hatred sentence.
- Julra's three published elements as encounter coverage, with per-ability left unknown.
- Identity splits: Emperor versus Remains, Lagon campaign versus monolith versus the tentacle display collision, Heorot versus God Hunter Argentus, Formosus as one actor with two names, God of Bloodshed versus seasonal Morditas, Herald of Oblivion versus altar Aberroth.

## What is uncertain

- Every preview coefficient, tag-versus-array element, and inherited move list.
- Empowered area levels on pages that were not re-opened.
- Corruption minimums and the 10-eye gate.
- Whether Time Rot removal and the Soul Bomb damage cut survived into 1.5.1.
- Dungeon tier numbers and unopened NPC pages.
- The full manifest digest.

## What needs in-game evidence

- Killer strings, especially when the display name and the internal id differ.
- Boss flag versus `ActorData.isHarbinger` on a local report.
- Zone lookup and scene suffix behavior.
- Whether Temporal Shift still leaves the character in place.
- Whether Heorot can be frozen.
- A Soul Bomb death report and a Hatred Dive Bomb death report, each with ability string and damage type.
- A pool tick versus remaining health on Harbinger of Destruction, before any ward-bypass sentence.

## Proposed Codex steps

1. Port the catalog JSON beside the existing Core catalog. Do not add the 53 profiles to `BossCatalog.cs` until alias collisions are decided. The short alias "Lagon" must not be applied to both the campaign actor and the monolith actor, and must not match the tentacle's display name by substring.
2. Leave `DamageTypes` empty where the catalog says `unknown`. Empty means unknown, not "no damage".
3. Do not match Harbinger of Hatred by the substring "Void Rahyeh".
4. Manual browse must not relabel a saved death. Encounter grade A must not be implemented from one capped killing blow. Lane 5's two-grade machine is not in Core. Do not add it from this package.
5. On the laptop, diff `Il2CppLE.dll` against `lane4-probe-plan.md` before any native hook. Run InteropGuard there with the 42 reference DLLs. This remote run did not.

## Changed files

Research (gitignored, force-added):

- Lane files brought in without merging code: `lane1-roster.md`, `lane1-matrix.csv`, `lane2-damage.md`, `lane2-moves.json`, `lane3-fightplans.md`, `lane3-tips.json`, `lane4-native.md`, `lane4-probe-plan.md`, `lane5-assessment.md`, `lane5-regressions.md`.
- `factcheck.md`.
- `return/GROKBOT-BOSS-HARBINGER-RETURN.md` (this file).
- `return/GROKBOT-BOSS-HARBINGER-CATALOG.json`.
- `return/GROKBOT-BOSS-HARBINGER-MATRIX.csv`.
- `return/GROKBOT-BOSS-HARBINGER-research.zip`. SHA256 `9255a829f2ec94eb885e6dbc2f70d305f076baf16e479eae441346c991202e35`.
- `return/GROKBOT-BOSS-HARBINGER.patch` (code diff against `f31c140`, which already contains the lane 6 schema on top of `88dcaaa`). SHA256 `215a8056936b075d535e8db8c764d9d3da1c5274b6f27791b22b755da0087605`.

Code, inside the allowed boundary:

- `medick_DeathCounter/src/Core/BossCatalog.cs`
- `medick_DeathCounter/tests/CoreTests/BossCatalogSchemaTests.cs`
- `medick_DeathCounter/tests/CoreTests/BossFactcheckRegressionTests.cs`
- `medick_DeathCounter/tests/CoreTests/CoreTests.csproj` (the test project sets `EnableDefaultCompileItems` to false, so the new file had to be listed)

Not changed: `BossCatalogSchema.cs`, `Game/`, `UI/`, repair bootstrap, storage identities, deploy settings, `main`.

## Schema validation

`GROKBOT-BOSS-HARBINGER-CATALOG.json` was validated with `jsonschema` Draft 2020-12 against `research/boss-harbinger/lane6-catalog-schema.json`.

Result: schema errors 0. Encounters 53. Tips 13. Known damage-coverage rows: 1 (`julra`).

## Tests and build

SDK: .NET 8.0.425 at `$HOME/.dotnet`. Game target remains `net6.0`. Both commands used `PATH` with that SDK.

Before the catalog edits, this run recorded 228 tests, all passed, exit 0, and a Release build with 0 warnings, 0 errors, elapsed 00:00:03.21. The verbatim pass list from that first run was not kept in a terminal file.

After the edits, the first `dotnet run` failed to compile because `CoreTests.csproj` does not use default compile items, and because the new file used `Single` without `using System.Linq`. Both were fixed. The run that includes the new tests:

```text
pass  Test_Catalog_FactcheckRemovesUnsupportedStarterTips
pass  Test_Math_Lane5ResistanceAndPoolPins
230 tests, all passed
```

Exit code 0.

Release build after that test run:

```text
  Determining projects to restore...
  All projects are up-to-date for restore.
  medick_DeathCounter -> /workspace/medick_DeathCounter/bin/Release/net6.0/medick_DeathCounter.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.67
```

Command: `dotnet build medick_DeathCounter -c Release -p:NoGame=true -p:DeployToMods=false -warnaserror`.

Lane 5 encounter grades were not implemented. Lane 6 schema tests M1 through M20 and A1 through A12 were already in `BossCatalogSchemaTests.cs` and were not duplicated. The new file pins tip deletion and the lane 5 math examples that current Core types can answer.

## InteropGuard

Unavailable remotely. The project file is in the tree. The 42 reference DLLs are not. No allowances were added. Codex should run both guard modes on the laptop.
