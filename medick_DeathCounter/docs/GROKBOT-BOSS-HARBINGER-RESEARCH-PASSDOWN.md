# Grok Bot: boss and Harbinger research passdown

## Task

Dispatch a research team to fill the boss and Harbinger gaps in **Terrible Death Log, Assessment and Counter** for Last Epoch. Return facts we can implement, useful fight advice, identification evidence and tests. The mod is experimental. Do not install, launch the game, change game files, merge, push or publish. Research and optional isolated Core/data patches are authorized; live-tree replacement is not.

The user wants a Boss tips menu that explains damage types, relevant resistances, dangerous mechanics and how to prepare for another attempt. They specifically asked whether the Harbingers following timeline bosses are accounted for. They are not fully covered yet. Finish the roster and distinguish each encounter rather than applying a parent boss's profile blindly.

## Access for a remote Grok team

This web document is the research brief. The latest source ZIP and local reports have not been published. If your team cannot access the local workspace, proceed with the roster, mechanics, damage, tips and evidence research now. Request the source ZIP only before producing a code patch; do not substitute the older GitHub branch for the hashed current baseline. Return remote results as a downloadable archive. Paths containing `<local-workspace>` refer to Andre's laptop, not web links.

## Current baseline: read these first

These files are in `<local-workspace>\outputs\`:

1. `CODEX-DEATH-FULL-REQUEST-REVIEW.md`: request coverage, implementation limits and live test queue.
2. `CODEX-DEATH-FULL-REQUEST-MANIFEST.json`: exact baseline identity and hashes.
3. `CODEX-DEATH-FULL-REQUEST-SOURCE.zip`: current source, including Core, Game, UI, tests, prior research and build tools.

Source ZIP SHA256:

```text
BF9B28AC9E61A1EFEEBBEA82817DE74C601F07D2D6A852729A70E3CF00F5F948
```

Validated local candidate DLL SHA256:

```text
39A715596FA64B0747FEED42B0A6269A61D26FB60DACDCF09094E6537150D386
```

This is the October 2, 2026 afternoon baseline: **214 CoreTests pass**, NoGame and local builds have zero warnings/errors, both InteropGuard checks pass. Nothing in this candidate has been verified in game. Version is `0.1.2`; do not confuse it with earlier DLLs carrying that version. The source ZIP/hash is the baseline, not a Git commit. The earlier 08:50 snapshot, PR #10 and advice combined branch predate this source. Do not overwrite current work with those branches.

Live source, for read-only comparison:

```text
<local-workspace>\work\death-current\LastEpoch-Mods-claude-last-epoch-death-counter-yspgnf
```

Work from a separate extracted copy. Record its source hash in the return. If the live source advances, give Codex a patch to port by hand.

## What already exists

- Insert opens the panel; its fifth tab is **Boss tips**.
- Starter profiles: Lagon, Emperor of Corpses, Heorot, Chronomancer Julra and Harbinger of Hatred. Most lack verified complete damage tables. Julra has published encounter coverage for Void, Cold and Lightning, not a per-attack map.
- Catalog code: `medick_DeathCounter/src/Core/BossCatalog.cs`.
- Menu: `src/UI/DeathPanel.cs`; regression tests: `tests/CoreTests/BossContextTests.cs`.
- Original record: `src/Core/DeathRecord.cs`; pending capture/routing: `PendingDeath.cs`; typed game report: `DeathDetails.cs`.
- Native probes: `src/Game/PlayerProbe.cs`, `DeathReportHooks.cs`, `DeathTracker.cs`, `ArgReader.cs` and `GameHooks.cs`.
- Advice/reassessment: `AdviceRules.cs`, `CurrentAssessment.cs`, `ReadinessRating.cs`, `ReassessmentLog.cs` and `MitigationMath.cs`.
- Prior mechanics research is in `medick_DeathCounter/research/advice/`. Use the fact-check/synthesis documents when they disagree with an earlier lane. The current rules already prioritize captured resistance gaps, keep three distinct actions, and gate health/ward recommendations on evidence.

Known native metadata:

```text
Il2Cpp.PlayerActorSync.ReceiveDetailedDeathInfo(
    DamageType, Nullable<DamageType>, Boolean, Int32, Int32,
    Ailment, Ability, ActorSync, IrregularDamageSourceID, Boolean isBossfight)

Il2Cpp.DeathInformationText.UpdateDeathInfo(
    DamageType, Nullable<DamageType>, Boolean, Int32, Int32,
    Ailment, Ability, ActorSync, IrregularDamageSourceID)
```

The local callback has no boss flag. Missing is not false. Confirmed catalog linkage currently requires an explicit true flag and an exact attacker alias. A matching offline attacker can open a guide while classification stays unknown. Scene names and ability names never prove boss identity or damage typing.

We retain the exact scene name, including numeric suffixes, and positive `Il2Cpp.ZoneInfoManager.get_ZoneLevel()` output when readable. A reliable localized area lookup and stable boss/encounter identifier are not established. Network report ownership currently depends on the resolved local ActorSync pointer. Local reports are the fallback. Native report values/overkill units and ward-only hit coverage remain unverified.

## Dispatch lanes

Assign separate files and keep the live tree untouched. Have a fact-checker challenge the synthesis after the lanes return. Models may disagree; resolve disagreements with evidence rather than a vote.

### Lane 1: complete roster and encounter relationships

- Establish the current Season 5 patch/build from dated evidence. Keep historical patch facts distinct from currently verified mechanics.
- Enumerate every current Monolith timeline boss, alternate boss encounter, relevant normal/empowered variant and associated Harbinger. Include the exact boss-to-Harbinger mapping, encounter sequence, location and availability conditions when verified.
- Check the complete Harbinger roster and any final/pinnacle encounter associated with progression, including whether Aberroth needs its own profile. Verify names and relationships; do not copy a remembered list.
- Include dungeon bosses across tiers and important campaign bosses such as Lagon and Majasa. Identify new Season 5 bosses that need profiles.
- Distinguish unique encounters, reskins, inherited moves, transformed variants, summoned adds and proxy attackers. Do not merge profiles merely because names resemble each other.
- Return a coverage matrix: encounter, variants, parent/follow-up relationship, location, source, date/version, damage research status, identity confidence and unresolved fields.

Priority: all timeline boss/Harbinger pairs first, then dungeon and pinnacle bosses, then campaign and other seasonal encounters. Explicitly list omissions.

### Lane 2: attack damage and defensive interactions

For each major lethal move, find:

- Displayed/localized and internal ability names where obtainable.
- Physical, Fire, Cold, Lightning, Necrotic, Void and Poison components. Separate mixed damage, hit damage and damage over time.
- Applied ailments: shred, penetration, Shock, Marked for Death, Poison, Ignite, Bleed, Frostbite, Doom, Time Rot, Damned, Freeze, Stun or other verified effects.
- Which effects change resistance or other defenses, verified stack behavior, duration and caps. Reuse existing correct mechanics rather than duplicating research.
- Whether the move can crit, be dodged or blocked, and whether armor/endurance affect it. Unknown is allowed; do not infer from visuals.
- Boss/Harbinger inherited move versus unique move, phase and variant differences.
- Any modifiers from tier, area level, corruption or encounter state that alter the advice. Report units and scaling only when supported.

An ability called Void Rahyeh, Ice Spike or Lightning Blast is not sufficient evidence of its damage composition. A death's primary/secondary elements describe that killing blow, not the complete fight. Keep base attack damage, post-mitigation health loss, ward absorption and game report amounts distinct.

### Lane 3: specific fight plans

Replace generic "keep moving" filler with useful responses to named mechanics:

- The visual/audio tell, what action to take, when to take it and where to move.
- Dangerous ground effects, safe routes, beam targeting, waves, slams, rings, room-wide attacks and phase transitions, where verified.
- How the follow-up Harbinger changes positioning or preparation compared with the preceding boss.
- Traversal/era switching, long attack-animation commitments, recovery windows and hazards left behind by an earlier phase.
- Build-dependent exceptions and controller-friendly wording without promising that any skill guarantees safety.
- What gear can reasonably address and what should be avoided through mechanics instead.

Give each profile its two or three best distinct tips, ordered by lethal risk. Put supporting detail in the research file; the menu copy should stay short. Separate documented mechanics from tactical inference. Avoid old exploit safe spots, bug-specific workarounds and precise timing claims unless current evidence supports them.

### Lane 4: reliable native identity, zone and phase context

Inspect local metadata read-only if available:

```text
C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\MelonLoader\Il2CppAssemblies\Il2CppLE.dll
```

- Find stable boss, actor-data, encounter, timeline, quest, dungeon and Harbinger IDs. Return exact declaring types, signatures, static/instance status and how the local player's encounter could be reached.
- Find a reliable localized zone/area name lookup, with its scene/timeline mapping and loading-state behavior.
- Investigate identifying the Harbinger phase independently of the parent boss, especially when an inherited ability or proxy actor delivers the killing blow.
- Investigate explicit offline/local boss evidence without relying on substrings or a missing network flag.
- Document localization keys/aliases, unknown locale behavior and exact-name collisions. Keep all alias claims tied to evidence.
- Consider actor unload, delayed packets, party deaths, old native pointers, character switches, rapid consecutive deaths, boss adds and environmental hazards.
- Determine what can be captured at the last living moment versus at the death callback. No observer-side field should be treated as authoritative player context without a provenance check.

Do not run native assemblies as ordinary managed libraries or modify the install. Use metadata inspection. If you cannot access the laptop, return a targeted read-only probe plan and list which signatures still need verification. Distinguish metadata presence from observed runtime behavior.

### Lane 5: encounter-aware defense assessment

- Define boss-specific resistance priorities using verified attack types and captured player deficits. Follow-up Harbinger requirements must account for its verified additional threats.
- Distinguish a recorded-death reassessment from whole-encounter preparation. Do not grade a whole fight A just because the one recorded killing-blow element is capped.
- Explain which defenses address each lethal move and which do not: armor for eligible hits, endurance for eligible health damage, critical protection, recovery/cleanse, stun/freeze protection and pool size.
- Resistance gaps come first. Overcap is conditional on verified debuffs, not a way to counter area-level penetration. Do not invent ailment stack counts.
- Ward should be recommended only when captured build evidence makes it relevant. A ward peak is not sustainable ward. Missing stats remain unknown.
- Keep the original death and each saved reassessment immutable. Additions must not replace original facts with catalog expectations or today's location.
- If proposing counterfactual math, state every assumption, unit and missing input. Provide worked examples for a resistance gap, a capped heavy hit and a mixed boss/Harbinger fight.
- No calibrated fight survival percentage without a validated model and adequate input. Prefer explained defense grades and bounded modeled damage changes.
- Reuse the existing three-action limit and deduplication groups. Boss movement notes may live in the separate menu, but do not repeat three identical gear cards across sections.

### Lane 6: data, UI integration, tests and independent fact-check

- Propose a versioned machine-readable catalog: stable encounter ID, display name, aliases/locale, parent relationship, variants, locations, abilities, damage components, ailments, tips, claim-level sources and confidence.
- Make absence explicit with null/unknown fields. Separate captured game evidence, datamined reference, developer documentation and tactical inference.
- Design browsing by timeline, boss, Harbinger and dungeon without turning the UI into a wall of text. A visible guide can show concise hazard/resistance coverage and expanded move details.
- A death can link to an identified encounter or open an unconfirmed attacker guide. Manual selection must never relabel the saved death.
- Verify accessibility: readable text, damage colors plus text labels, controller interaction, long localized names, scrolling and a compact main death card.
- Audit the existing five starter profiles for weak/generic tips and outdated assumptions. Recommend deleting unsupported claims rather than padding them.
- Add test cases for every boss/Harbinger mapping, exact aliases, alternate variants, add/proxy killers, unknown/false flags, missing localized zone, late reports, manual browsing, mixed types, unknown typing and old saved records.
- Add assessment regressions: boss catalog cannot manufacture death damage, a parent's capped element cannot clear unknown Harbinger threats, catalog changes cannot rewrite saved reassessments, and source lookup cannot cause a generic three-card duplication.
- Independently fact-check each material source/claim. Flag disagreements, old patch mechanics, sources that cite each other, and purported "official" community content.

## Evidence requirements

Use primary evidence where possible: EHG documentation/patch notes, current extracted game data with version/provenance, and actual local observations clearly identified as such. Original research datasets such as LE Tools or Tunklab must be labeled as datamined/reference data, not developer documentation. Community reports can identify a question to test, but do not promote an unverified comment into a definitive damage table.

For each material claim, return the direct URL or local metadata reference, publisher/author, publication/update date when available, access date, patch/build applicability, evidence class and a short explanation of what it supports. Mark deductions as deductions. Where sources disagree, give the conflict and the smallest test needed to resolve it. Do not invent missing dates or claim a historical patch note validates Season 5 by itself.

Keep findings concise and nonduplicated. No double dashes in player-facing copy. Do not rewrite generic defense mechanics already researched unless new evidence changes them.

## Optional code boundary and verification

Research is the main deliverable. An optional implementation may modify isolated catalog/Core files and CoreTests against the supplied source snapshot. Coordinate file ownership. Do not change the live tree, Game/UI hooks, repair bootstrap, existing storage identities or deployment settings. Return native/UI changes as reviewed proposals or a separate optional patch for Codex to port.

Run tests before and after any patch:

```text
dotnet run --project medick_DeathCounter/tests/CoreTests
dotnet build medick_DeathCounter -c Release -p:NoGame=true -p:DeployToMods=false -warnaserror
```

Tests require .NET 8. Game target is .NET 6. Local SDK on Andre's laptop: `<local-workspace>\work\dotnet8\dotnet.exe`.

Use DeployToMods=false on every build. The project otherwise has a copy-to-Mods build target. Do not use a plain default build on this laptop.

The source archive includes InteropGuard, but not the 42 shipped reference DLLs used by Codex. A remote guard run without that reference corpus is not the same check. Report it as unavailable and let Codex run both modes locally; do not add blanket allowances or claim native verification.

## Return package

Place deliverables in `<local-workspace>\outputs\` when local. If remote, provide the same named files in a downloadable archive.

1. **GROKBOT-BOSS-HARBINGER-RETURN.md**: read-first synthesis. Baseline hash, roster coverage, highest-priority verified additions, current gaps, corrections to our five profiles, proposed Codex steps, changed files and precise validation results. Distinguish research complete from native tests pending.
2. **GROKBOT-BOSS-HARBINGER-CATALOG.json**: machine-readable proposed profiles and claim-level evidence. Include schema/version and null unknown fields.
3. **GROKBOT-BOSS-HARBINGER-MATRIX.csv**: complete coverage and relationship matrix.
4. **GROKBOT-BOSS-HARBINGER-research.zip**: each lane's named report, source ledger, contradictions and native probe/test plan.
5. If code is supplied: **GROKBOT-BOSS-HARBINGER.patch** and a full source ZIP, with SHA256 hashes and exact tests/build output. Provide a patch relative to our verified source snapshot, not an unrelated older branch.

The return must say exactly what Codex can port now, what remains uncertain and what needs in-game evidence. Do not say "all Harbingers handled" unless the coverage matrix accounts for the verified complete roster and every unsupported field is visibly marked.

