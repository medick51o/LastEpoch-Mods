# Lane 2: attack damage and defensive interactions

Access date 2026-10-02. This lane does not change code. Generic defense formulas stay in `medick_DeathCounter/research/advice/01b-MECHANICS-FACTCHECK.md`. This file applies those rules to boss moves and records only the encounter evidence.

`lane2-moves.json` has 606 reached damaging or ailment-bearing sub-abilities. Base coefficients in the notes below are the extract's `damage` array. They are ability data. They are distinct from post-mitigation health loss, ward absorption, overkill, and the death-report amount. `addedDamageScaling` is also stored and differs by move (Soul Bomb 1, a brute circular slam 25, Julra's implosion 100). The monster formula that consumes that field was not found, so coefficients are not a cross-move lethality ranking.

## How a row was built

Monster skill lists are `skills` or `cskills` on Tunklab's `monsterList` `1.5-preview1`. Each skill is walked through `nextAbilities`, `delayedCast`, `comboAbilities`, `randomCast`, `onHit`, `randomOnHit`, and `mineTrigger`. A row is emitted when a node has a non-zero damage array, beam damage, repeat damage, or an ailment application.

Damage array order used here is Physical, Fire, Cold, Lightning, Necrotic, Void, Poison. Fireball in the same ability list is `[0,25,0,0,0,0,0]`. That matches the 2023 `DamageType` order already recorded in the fact-check, and the index map in Last Epoch Planner. Delivery is the record's `isHit` flag: true is `hit`, false is `dot`.

Ability `tags` and `description` are often copied from a player skill (`playerAbilityID` is set on many of them). When the damage-type bits in `tags` disagree with the non-zero damage indexes, the row uses the array. 125 such conflicts are listed under Contradictions. A display name is the record's `name`. It is not proof of element.

`can_crit` is false when `isHit` is false, because damage over time cannot crit. On a hit it is true only when that record's `critChance` is above 0. `critType` is 0 or 1 in the extract and its enum name was not found, so it is not used as a veto when `critChance` is already above 0.

`dodgeable` is false for damage over time. It is false when the ability sets `undodgeable`. Other hits are marked dodgeable because that flag is absent. `blockable` is false for damage over time. Hits are marked blockable from the support rule that block applies to hits, including spells. The extract has no per-ability unblockable field. `armor_applies` follows the hit versus damage-over-time rule. `endurance_applies` is true for both, on the health portion only. Ward is outside endurance.

Area penetration (1% per area level, cap 75%, after the resistance cap, overcap does not offset it) is a property of the area. It is not repeated per move. Resistance shred, Shock, and Marked for Death are the overcap cases already confirmed in the fact-check. Other curses are called out below and are not assumed to work the same way.

Health thresholds on the monster record become `phase` when they limit a skill. A threshold with maximum 0 and an empty phase list is treated as disabled and skipped. Corruption multipliers for attack damage were not in these ability records. Tunklab NPC pages do show corruption-scaled boss health. That figure is health.

## Source ledger

- Ability, monster, and ailment records, version string `1.5-preview1`. URL https://lastepoch.tunklab.com/ and bundle `https://lastepoch.tunklab.com/_next/static/chunks/3nhe36ktaxpqv.js` (build `7teO3__rMdpZQmmxVZLXm`). Publisher Tunk. Site footer on 2026-10-02 says Game Version 1.5 preview. Object stamps say `1.5-preview1`. The dungeon list object in the same bundle is still `1.3.6` and was not used for damage. Applicability: 1.5 preview extract. Season 5 went live 2026-10-01. Class: datamined/reference data. Supports per-move elements, hit versus damage over time, ailment ids, phases, and actor ids.
- Season 5 is live. URL https://forum.lastepoch.com/t/season-5-rage-of-the-frostborn-is-now-live/81860. EHG_DerrickG, 2026-10-01 16:12. Class: developer documentation. Supports the launch date. The post has no boss damage table.
- 1.5 and 1.5.1 notes moved to the site. URLs https://forum.lastepoch.com/t/season-5-rage-of-the-frostborn-patch-notes/81789 (2026-09-25) and https://forum.lastepoch.com/t/last-epoch-patch-1-5-1-notes/81911 (2026-10-02). EHG_DerrickG. The 1.5.1 post says Pinnacle Morditas is enabled for Legacy and Offline. Class: developer documentation. The note bodies are linked at `https://www.lastepoch.com/1-5/patchnotes` and `https://www.lastepoch.com/1-5-1/patchnotes`. Both pages returned a client shell with no note text on this access.
- Harbingers take abilities from the timeline boss. Ten timelines. Eyes open Aberroth. URL https://forum.lastepoch.com/t/harbingers-of-ruin-whats-new/71684. EHG developer blog, fetched 2026-10-02. The post is the 1.1 Harbingers introduction. Class: developer documentation. Supports Agile and Brute Harbingers that gain abilities from that timeline's boss. Elements are absent.
- Lagon ability names Moon Blast, Tidal Wave, Lightning Blast, melee. URL https://forum.lastepoch.com/t/the-convergence-update-beta-0-9-patch-notes/51975. Yayifications, 2023-03-06, patch dated 2023-03-09. Class: developer documentation. Those four names existed in 0.9. The 1.5 preview Lagon skill list uses different ids. See the Lagon audit.
- Soul Bomb distance falloff. URL https://forum.lastepoch.com/t/beta-0-9l-patch-notes/58685. Hackaloken, 2023-05-10. Class: developer documentation. "Emperor of Corpses' Soul Bomb now deals less damage the further you are from the center of the ability."
- Soul Bomb arena. URL https://forum.lastepoch.com/t/beta-0-9i-patch-notes/57471. Hackaloken, 2023-04-12. Class: developer documentation. Arena size restored so Soul Bomb can be avoided. Damage was increased again in that patch. Also names Argentus' ice spear stab.
- Soul Bomb temporary damage cut. URL https://forum.lastepoch.com/t/last-epoch-patch-1-4-2-notes/80956. EHG_DerrickG, 2026-04-01. Class: developer documentation. Timeline-boss Soul Bombs "now deal tremendously less damage" while indicators fail. The sentence calls the change temporary. A later note that restores the previous damage was not retrieved.
- Heorot Ice Spike. URL https://forum.lastepoch.com/t/last-epoch-patch-1-0-3-patch-notes/68385. Yayifications, 2024-03-13. Class: developer documentation. Fixes "Ice Spike stopping projectiles, and being frozen not stopping actions in multiplayer." The element is absent from that sentence.
- Harbinger of Hatred Void Rahyeh Dive Bomb. URL https://forum.lastepoch.com/t/last-epoch-patch-1-2-1-notes/76245. EHG_Wick, 2025-04-23. Class: developer documentation. Visuals of that attack were aligned with its damage. The same note says "Enemy abilities now no longer apply Time Rot or Damned."
- Timeline boss and Harbinger pairing. URL https://lastepoch.tunklab.com/timelines. Tunk, fetched 2026-10-02, site version 1.5 preview. Class: datamined/reference data. Ten pairs. Internal ids in the bundle match this pairing.
- Julra encounter elements Void, Cold, Lightning. URL https://lastepoch.tunklab.com/dungeon/temporal_sanctum. Tunk, fetched 2026-10-02. Class: datamined/reference data. The bundle's dungeon object is stamped 1.3.6, so this page is weaker than the 1.5 preview ability rows. The ability rows split those three elements by move.
- Hit, armor, dodge, block, endurance, crit, shred, Shock, Marked for Death. Support articles cited in `01b-MECHANICS-FACTCHECK.md`. EHG support, fact-check dates mostly 2026-02 and 2026-07. Class: developer documentation. Supports the defensive flags on each row. Season 5 formula edits were still unchecked in that fact-check.
- Enemy crit multiplier 200% and damage over time cannot crit. URL https://support.lastepoch.com/hc/en-us/articles/46361891709211. EHG support, fact-check updated 2026-02-20. Class: developer documentation. Supports `can_crit` false on damage over time.

## Shared Harbinger kits

The 1.5 preview ids split Harbingers into Brute and Agile. Most of the large coefficients on a Harbinger are this shared kit. Timeline-specific moves are additional rows with their own internal ids. `inherited_from` is set when the parent timeline boss has one matching display name and the same element and delivery. Identical ids across campaign and monolith Lagon are the same record, so `inherited_from` stays null there.

Brute kit, on Defilement, Hatred, Destruction, Tyranny, Brutality, and Denial: physical hits that can crit, including a circular attack, frenzy slam, hammer slam, and an echo slam that is physical and void. A howl applies Slow (ailment id 6, base duration 4 seconds, max 3, 50% less effect against players on the ailment record, and this cast also carries increased duration and increased effect). Damage over time void appears on some of the same ids.

Agile kit, on Pride, War, Chaos, Treason, Fear, Regret, and Cruelty: a fire beam, a void slash, physical slashes, Frailty, Armor Shred, and Void Resistance Shred. The void shred record is max 10 stacks, duration 4 seconds, 60% less effect against players. One portal spear stores application chance 2. That field is not treated as a stack count.

## Starter audit

### Lagon (`lagon-monolith`, `lagon-campaign`)

Both actors use the same six reached sub-abilities. Actor modifiers differ. Reading property 0 the way the Tunklab Lagon page already translates it, monolith Lagon has more damage 2.05 (the page text is +205% more damage) and campaign Lagon has more damage -0.24. Both descriptions say the body can only be damaged by attacking tentacles. `lagon-tentacle` (`Lagon's Tentacle`) reaches two physical hits with small coefficients and a stun chance. The tentacle actor's own property 0 more value is 6.596, so the small ability coefficients are not the whole story.

Reached moves:

- Moon Blast `LagonMoonBlast`: Cold hit and Lightning hit, base 660 each, `critChance` 0, `undodgeable`. Applies Slow.
- Eye beams `LagonBossEyeBeamLeftToRight` and `RightToLeft`, display name Moon Beam: Cold and Lightning damage over time, base 32 each, `undodgeable`. The description text is a fireball sentence. The array is cold and lightning.
- Low Moon Beam `LagonBossLowMoonBeam`: Cold hit and Lightning hit, base 175 each, can crit, applies Chill.
- Claw grab and melee: Physical hits, bases 150 and 350, can crit, dodgeable under the flag rule.

The 0.9 note names Tidal Wave and Lightning Blast. Those display names are not on the current Lagon `cskills` walk. Orphan ability records `LagonBossTidalWaves` and `LagonBossStorm` exist in the ability list and are not referenced by either Lagon actor. Harbinger of Chaos has its own Tidal Waves, Cold hit base 300, and a Moon Beam copied in kind from `LagonBossLowMoonBeam` at base 32. The parent low moon beam coefficient is 175. The catalog's empty element list is the right call for a death report. For encounter prep, the preview walk is cold, lightning, and physical.

### Emperor of Corpses (`emperor-corpses`)

- Soul Bomb `UndeadDragonExplosionDamage`: Necrotic hit, base 500, `critChance` 0, `critType` 1, dodge flag not set, armor applies, endurance can apply to health. Tags are Spell+Fire (264), which conflicts with the array. The 0.9l note gives distance falloff. The 1.4.2 note gives a temporary damage reduction. Neither curve nor reduction is a field on this record.
- After the bomb, `UndeadDragonDoTDonut` (display name Essence Wind) leads to `UndeadDragonDamageDonut` (display name Poison Pool): Necrotic damage over time, base 100. The name Poison Pool conflicts with the array.
- Spirit Breath `UndeadDragonBreathAttack`: Necrotic damage over time, base 350, `undodgeable`, `freezeRate` 0. The description says it freezes. That sentence is a cloned cone description.
- Flesh Impact: Necrotic hit, base 200, `critChance` 0.
- Melee: Physical hit, base 250, can crit.
- Roar: no damage array. Applies Slow. Summons 5 `UndeadDragonVolatileZombie` (display name Slaughtered Thrall). Their on-death explosion is a Necrotic hit, base 80, and can crit. The explosion's description says physical. The array is necrotic.

The actor also has property 0 more 1.6 with tags 0, and more 0.2 with tags 8 (Fire). The Tunklab page translates those as +160% more damage and +20% more fire damage. No reached sub-ability has a fire component. The fire modifier is an actor stat with no matching component in this walk.

### Heorot (`heorot`)

Actor description: Cannot be Frozen. Property 0 increased value -0.135. The Tunklab page text is 14% reduced damage, which is the same field rounded.

Reached elements are Cold and Physical.

- Ice circles, display names Small, Medium, and Large Circle: Cold hits, bases 500, 650, and 800, `undodgeable`, can crit.
- `HeorotBossIceSpike`, display name Frosted Eruption: Cold hit, base 525, can crit. One starter is limited to above 75% health and another to at or below 75% health. The internal id matches the 1.0.3 name Ice Spike. The display string in this extract is Frosted Eruption. Which string a death report uses is unknown.
- Ice Spear `HeorotBossIceSpear`: Cold hit, base 350. The 0.9 notes mention Heorot's Ice Spear visuals. The description on this record is a hammer-throw sentence.
- Ice Stab: Physical 300 and Cold 600, `critChance` 0. The description talks about poison. The array has no poison.
- Blizzard `HeorotBossWhirlwindDonut`: Cold damage over time, base 200, at or below 75% health, applies Cold Resistance Shred.
- Frost Beam: Cold hit, base 200. Charge: Physical and Cold hits. Melee and jump: Physical hits.

Harbinger of Tyranny keeps the brute physical and void kit and adds Frost Beam `BruteHarbingerHeorot01AntlersLazer` (Cold hit, base 200) and Oblivion `BruteHarbingerHeorot00Whirlwind`, which applies Frostbite. Frostbite's record: cold damage over time, first 15 stacks each add 20% freeze chance, no stack cap stored. Those two were not auto-linked because the display names differ from Heorot's.

Heorot also summons `GraelSpiritforHeorotBossfight` (Spirit of Grael: cold and physical hits, small coefficients, property 0 more 5) and `FrozenWolfforHeorotBossfight` (display name Son of Artor: physical and cold bites).

### Chronomancer Julra (`julra`)

The walk splits the published Void, Cold, and Lightning coverage by move.

- Catastrophic Implosion `VoidNagasaBoss01.1screennukedamage`: Void hit, base 2000, `addedDamageScaling` 100, `undodgeable`, can crit. Applies Doom (id 90): void damage over time and more melee damage taken, duration 4 seconds, max 4. The application chance field is 4, which is not treated as a stack count.
- Frigid Tide: Cold hit, base 700, applies Chill.
- Chilled Seeker: Cold hit, base 240, `undodgeable`.
- Shocking Impact explosions: Lightning hits, bases 300, 600, and 900, all reachable. The records are not labeled with a dungeon tier.
- Temporal Shot: Void hit, base 450.
- Unravelling Decay beams: Void damage over time, base 150, `undodgeable`, Void Resistance Shred.
- Dimension Tear areas: Void damage over time, Void Resistance Shred.

Actor property 0 more -0.124. The Tunklab page says 12% less damage. A code table `extraMoreDamageMultiplier` has Julra values `[1, 0.572, 1.1, 1.3, 1.8]`. The call site that maps dungeon tier to that index was not in the data module, so the table is not applied here.

### Harbinger of Hatred (`harbinger-hatred`)

Rahyeh's own walk (`rahyeh`) is void only: Dive Bomb void hit base 850 with `critChance` 0, Void Blast void hit base 730 that can crit, Void Flare void hit base 450 with `undodgeable`, Void Skyfall void hit base 350 with `critChance` 0, plus void damage over time.

The Harbinger keeps the brute physical kit and adds void copies: Void Flare base 530 `undodgeable` and `critChance` 0, Dive Bomb Slam void hit base 730 that can crit, Dive Bomb `BruteHarbingerVoidRahyeh01.2DiveBombAoE` void hit base 1000 with `critChance` 0, and Void Skyfall void hit base 350. The Dive Bomb and the flare and skyfall rows set `inherited_from` to Rahyeh's matching sub-abilities. Tags on the Dive Bomb AoE are Spell+Fire while the array is void. The 1.2.1 note names "Harbinger of Hatred's Void Rahyeh Dive Bomb" and says the visuals were misaligned with the damage. It does not state an element. The array is the element source.

The catalog is right that the word Void in the ability name is not a measurement. The measurement in this extract is the void component on those rows, plus a separate physical kit Rahyeh's walk does not have.

## Other timeline pairs

Internal ids are the bundle's `monolithBossIds` and `harbingerIds`. Display names are the monster `name` field.

- `abomination` Abomination, actor `AbominationBoss`. Physical and Fire hits, Necrotic hit and damage over time. Description: invulnerable until Soul Vessels are destroyed. Tail Flare is physical and fire and applies Ignite. Soul Reave is a necrotic hit. Stomp thresholds sit around 90% health.
- `harbinger-defilement`, actor `BruteHarbinger-Abomination`. Brute kit plus fire, physical, and necrotic. Tail Flare and a fire wave inherit from the Abomination tail. Spirit Decay is necrotic damage over time inherited from the boss generator. The 1.2.1 note names this Harbinger's slam shockwaves at the end of the Abomination fight.
- `god-hunter-argentus`, actor `OsprixHeorotSpearBossEmerging`. Fire, Cold, and Physical hits. At or below 50% health: Impact Flare fire base 2000 and Flame Leap fire base 1200. Ice Stab is physical and cold with `critChance` 0. The 0.9i notes name Argentus' ice spear stab.
- `harbinger-pride`, actor `AgileHarbinger-Argentus`. Agile kit: fire, void, physical. The name rule did not attach a unique Argentus id. The agile kit is the bulk of the walk.
- `formosus` Frost Lich Formosus, actor `FrostLich`. Cold and Necrotic, both hits and damage over time. Spirit Beam is necrotic damage over time, base 250, `undodgeable`, `critChance` 0. Ice Vortex is cold damage over time and `undodgeable`. Ice Patch applies Slow and has an empty damage array.
- `harbinger-war`, actor `AgileHarbinger-FrostLich`. Agile kit plus cold and necrotic damage over time. Two rows inherit from Formosus.
- `harton-husk` Harton's Husk, actor `VoidHarton`. Physical hits, Void hits and damage over time. Void Decay is a void hit, base 550. Claw Slam is physical. Corrupted Slash is physical and void.
- `harbinger-treason`, actor `AgileHarbinger-VoidHarton`. Agile kit plus void. One inherited row.
- `harbinger-destruction`, actor `BruteHarbinger-UndeadDragon`. Brute kit plus Spirit Breath. Spirit Breath inherits `UndeadDragonBreathAttack`: necrotic damage over time, base 350. Also summons `BruteHarbingerVolatileZombie`.
- `gaspar-husk` The Husk of Elder Gaspar, actor `GasparHuskBoss`. Cold, Fire, Lightning, Necrotic, and Void. Doom Blast is a void hit, base 1150, `critChance` 0. Frozen Ice Storm is a cold hit, base 750. Totemic Flame Burst is a fire hit, base 400. Summons `GasparBossTotem` (Flaming Void Totem). That actor is in the monster list and the skill walk reached no damaging child.
- `harbinger-fear`, actor `AgileHarbinger-Gaspar`. Agile kit plus Gaspar's tri-beams. Tri-beams inherit fire, cold, and lightning damage over time, base 210 each. Spark Discharge inherits a lightning hit.
- `volcanic-shaman`, actor `ShamansBoss`. Fire hits and fire damage over time, plus one physical hit. Lava plumes are fire, bases 500, 800, and 1150. Magma Ball explosion is fire, base 700. A freeze rate is stored on a fire row. That flag is Freeze chance on the hit. The damage component stays fire.
- `harbinger-ash`, actor `BruteHarbinger-FireShamans`. Brute kit plus fire and necrotic. Magma Ball inherits the shaman's. Totemic Flame Burst on this Harbinger is a necrotic hit, base 800. The display name uses flame wording. Necrotic Spirit is a small necrotic damage over time.

## Aberroth, Herald of Oblivion, and the pre-fight Harbingers

`Aterroth` displays as Aberroth. Zone in the extract: `M_Harbinger`. `isPinnacleBoss` in the bundle includes `Aterroth`, `SuperShade`, and `MorditasBossPinnacle`. `isUberBoss` is `UberAterroth` only.

Aberroth phases on the actor are explicit: phase 2 at 77% to 63% health, phase 3 at 63% to 49%, phase 4 at 49% to 35%, phase 5 at or below 35%. Reached elements cover physical, fire, cold, lightning, necrotic, and void, as hits and some damage over time. The large slam `Aterroth08.4BigSlamDamage` (Timeshattering Slam) is a physical hit and a void hit, base 5000 each, `addedDamageScaling` 1, can crit. It is attached to the above 77% health window and again to the at or below 35% window. Flail Dash is a physical hit, base 3000.

Ailments reached on Aberroth and on Herald of Oblivion:

- Curse of Aberroth, id 132. Description: reduces all resistances by 10%, cannot be cleansed. Duration field 1000000. No stack cap stored. The support article's overcap list is shred, Shock, and Marked for Death. This curse is a different record, so whether overcap soaks the 10% is unknown.
- Aberroth's Time Rot, id 133. Description: deals void damage and shreds void resistance. Duration 3. No stack cap stored. This is not ailment id 9 (Time Rot). The 1.2.1 sentence about Time Rot and Damned does not name id 133.
- Frailty of Aberroth, id 129. Description: 5% less damage dealt, 10% less health leech, ward retention, and health regen, 50% less healing effectiveness. Duration 4.
- Also Chill, Slow, Ignite, and Frostbite on specific sub-abilities. Ignite and Frostbite application chance fields are 3 or 4 on some rows. Those fields are not stack counts.

Shock of Aberroth, id 130, description "You take 5% increased damage," is in the ailment list. It was not attached to a reached Aberroth or Herald node in this walk.

`UberAterroth` displays as Herald of Oblivion. Zone `WE524`. Description: immune while defended by other Harbingers. The same skill ids are walked, with the same phase bands. Property 0 more 5.6. `adds` says Phase 2 spawns `AgileHarbinger-UberAberroth` and Phase 4 spawns `BruteHarbinger-UberAberroth`.

Those two display as Harbinger of Regret and Harbinger of Denial. Both descriptions say "Makes Aberroth Immune to Damage." Regret is the agile kit (fire, void, physical) with property 0 more 5.3. Denial is the brute kit with property 0 more 5. Their moves did not inherit Aberroth ids. They are the shared kits at a higher actor modifier.

`AgileHarbinger-pre-Aberroth` is Harbinger of Cruelty (agile kit, property 0 more values 3 and -0.2). `BruteHarbinger-pre-Aberroth` is Harbinger of Brutality (brute kit, more values 3 and -0.15). `isNoScalingBoss` includes the pre-Aberroth Harbingers, Aberroth, Herald of Oblivion, the uber Harbingers, `SuperShade`, and `MorditasBossPinnacle`.

`SuperShade` displays as Vision of the Observer. Zone `WE535`. Reached elements include cold, fire, lightning, physical, and void. `SuperShade14.2VoidTrianglesDamage` (Void Rift) is a void hit, base 20000, `addedDamageScaling` 1, `critChance` 0. Starburst is a fire hit, base 12000. Those coefficients are still ability data, not a health-loss measurement. Phase flags on the actor turn groups of abilities on and off. The JSON repeats a row when two phase entries both enable it.

## Morditas

`MorditasBoss` displays as Morditas, description Demigod of Bloodshed, zone `WE536`. `MorditasBossPinnacle` displays as God of Bloodshed, zone `WE537`. The reached skill ids match: physical hits and cold hits and cold damage over time. Largest physical hit in the walk is Runic Leap, base 1500. Mega Slam is physical and cold. Blood Storm is cold damage over time and applies Cold Resistance Shred. The pinnacle actor adds property 0 more 4, plus other stat rows (property 9 increased 0.5, property 2 more 0.4, and two added values of 2 on special tags 2 and 23). Those special tags were not given names here.

The 2026-10-02 1.5.1 forum post says Pinnacle Morditas is enabled for Legacy and Offline. It does not give a damage table. The October 1 dev post says Anger changes the difficulty of the three-echo chain. No Anger coefficient is on these ability records.

`YrunBossEndgame` displays as Yrun, Champion of Morditas. Cold and physical. Cold splinters are cold hits with `undodgeable` and `critChance` 0, gated to narrow health bands (the walk shows 10% wide windows). `FrozenFlesh` (Fist of Morditas) was not given its own encounter in the JSON.

## Dungeons

Julra is above. `FireLichBoss` is Fire Lich Cremorus, description Immolator of Souls. Reached elements are fire hits, fire damage over time, and necrotic hits. Collapsing Decay is a necrotic hit, base 600, `undodgeable`, and the walk applies Marked for Death (id 17: the support text is 25% lower resistances, duration 8, max 1). Wall of Fire is a fire hit, base 600, `undodgeable`, and can apply Ignite. One skill is limited to at or below 70% health. The actor's property 0 row uses tags 8192, which is the Minion tag, increased 0.5. That is not a player-facing element.

`StoneTitanBoss` is The Mountain Beneath. Reached damage is physical hits and physical damage over time, with small coefficients (shockwaves base 160). It summons roots and a rock plant. `StoneTitanHeartBoss` is Stone Titan's Heart: physical hits, physical damage over time, and a poison application, plus Armor Shred. A code array gives extra health and extra more-damage multipliers per dungeon boss. The tier index for that array was not resolved, so it is not applied to the rows.

## Campaign Majasa

`Majasa`, description Goddess of Opulence, zone `H140`. Phase 1 reach: fire hit (ruby spear explosion base 1100), lightning hits (diamond slam base 700 and a chasing spike), a physical damage over time on `MajasaBoss11MedusaStare` (display name Flamethrower, base 5) that applies Stone Stare, and a poison application. Stone Stare, id 92, duration 5, description "You are being turned to stone by Majasa." The 0.9 notes name Majasa's Stone Stare as a targeting fix. They do not give an element. The damaging node on this id is a small physical damage over time. Sand teleports are gated at or below 75% and at or below 40% health.

Essences summoned from phase 1: Essence of Opulence lightning hit base 300, Essence of Avarice fireball fire hit base 150, Essence of Greed applies Poison and has no direct damage array. Descriptions say those essences are vulnerable to melee.

`MajasaBossPhase2`, description Goddess of Avarice, is summoned by the phase transition. Its reached damage is physical hits, including a combo nova base 850, plus Armor Shred, Physical Resistance Shred, and Stone Stare. Sprints are gated at or below 60% and at or below 40% health.

## Contradictions

1. Soul Bomb element. Damage array index 4 is necrotic and `isHit` is true. Tags are Spell+Fire. Community guides (Maxroll, Icy Veins) also say necrotic. The array and those guides agree. The tags do not. Distance falloff is a 0.9l developer sentence and is absent from the 1.5 preview record. The 1.4.2 temporary damage cut (2026-04-01) has no retrieved reversal. The preview base is still 500.

2. Spirit Breath and several other descriptions name Freeze or Fire or Poison while `freezeRate` is 0 and the array is something else. Cloned `playerAbilityID` text is the likely cause. Rows use the array and the ailment id list.

3. Emperor display name Poison Pool is necrotic damage over time. Harbinger of Ash display name Totemic Flame Burst is a necrotic hit.

4. Lagon 0.9 names include Tidal Wave and Lightning Blast. The current actor walk does not reference those ability ids. Harbinger of Chaos has a separate Tidal Waves record. Treating the 0.9 names as the current move list would invent a link.

5. Heorot Ice Spike versus Frosted Eruption. Internal id `HeorotBossIceSpike`, display name Frosted Eruption, cold hit. Both strings are evidence of different layers. The death-report label is unknown.

6. Time Rot. Ailment id 9 and Damned id 39 are not applied by any walked node in this lane. That matches the 1.2.1 sentence, which is older than Season 5. Aberroth applies id 133, Aberroth's Time Rot, which the 1.2.1 sentence does not name.

7. Dive Bomb tags versus array. `BruteHarbingerVoidRahyeh01.2DiveBombAoE` tags include Fire. The array is void only. The 1.2.1 note is about visuals matching damage, not about an element.

8. Emperor fire modifier versus reached components. The actor stat with fire tags has no fire component in the walked tree.

9. Vision of the Observer coefficients (void 20000, fire 12000) sit far above other bosses' bases while `addedDamageScaling` stays 1. Until the monster damage formula is confirmed, those numbers must not be read as health loss or as a ratio against Soul Bomb's 500.

10. Community guides say the Emperor roar applies maximum Slow stacks, and that blood pools are a percent of current health that ignore ward. The roar record applies Slow (id 6) with increased duration 0.25 and increased effect -0.2. The stack count of that cast is absent. A percent-of-health field is absent on the walked Emperor abilities. The pool after Soul Bomb is a flat necrotic damage-over-time coefficient on `UndeadDragonDamageDonut`. Maxroll's ward exception for Emperor blood pools remains a community report. The official ward page cited in the fact-check does not mention that exception.

## Open questions

- The 1.5 and 1.5.1 patch note bodies were not readable from the site shell. Any Season 5 change to these coefficients is unchecked.
- Monster `addedDamageScaling` formula, so relative threat across moves is unknown.
- `critType` enum name. Global enemy crit chance on top of a 0 `critChance` was not found as a named stat on these actors.
- Whether block or dodge is disabled on a specific hit without an `undodgeable` or unblockable field.
- Which localized string `ReceiveDetailedDeathInfo` puts in the ability argument when the display name and the internal id differ.
- Whether Curse of Aberroth's 10% all-resistance cut is soaked by overcap. The support list does not name this curse.
- Doom, Ignite, Frostbite, and some shred rows store chance values of 2, 3, or 4. Stacks applied by one cast stay unknown. Ailment max stacks are stored separately.
- Dungeon tier index for `extraMoreDamageMultiplier` and `extraHpModifiers`. Julra's 300, 600, and 900 lightning explosions are all reachable and unlabeled.
- Gaspar's Flaming Void Totem has no reached damaging child.
- Whether a percent-health, ward-ignoring pool still exists on the Emperor outside this ability walk.
- Anger scaling on Morditas, and whether players see Herald of Oblivion, Uber Aberroth, or Aberroth as the uber actor name. The extract stores Herald of Oblivion on `UberAterroth` and "Makes Aberroth Immune to Damage" on the two uber Harbingers.
- Orphan Lagon records for Tidal Waves and Lightning Storm: leftover data, or wired through a field this walk does not follow.
