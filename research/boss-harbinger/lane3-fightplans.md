# Lane 3: specific fight plans

Access date for every web source below: 2026-10-02. This lane does not change code. Menu copy is in `lane3-tips.json`. Nothing here was checked in a live 1.5.0.1 or 1.5.1 client.

Season 5 is patch 1.5, live 2026-10-01, with hotfix 1.5.0.1 the same day and patch 1.5.1 posted 2026-10-02. Those three forum posts do not restate timeline, Harbinger, dungeon, or Aberroth moves. A historical patch note is not Season 5 proof. Ability names, icons, and copied player-skill descriptions in the extract are not damage proof. Where a damage vector and an ability tag disagree, the element stays unresolved.

## How to read a move

Tunklab's public client bundle `3nhe36ktaxpqv.js` embeds `version:"1.5-preview1"` and also a `1.3.6` string. Cloudflare's `age` header on that file was 2677452 seconds at fetch, so the cached object was about 31 days old. Treat it as datamined preview data, not the live Season 5 build.

Inside that bundle, damage arrays are ordered Physical, Fire, Cold, Lightning, Necrotic, Void, Poison. Ability tags are a different bitfield (Cold is 4, Lightning is 2, Fire is 8, DoT is 4096, and so on). `isHit:false` is not a hit. The existing defense fact-check already says armor, dodge, block, and glancing blow do not apply to damage over time, and dodge does not apply when an ability is marked undodgeable. Those rules are reused, not re-researched. Base coefficients rank moves inside this extract only. They are not player damage, and `addedDamageScaling` is not converted here. `duration` and `speed` fields are not turned into seconds.

EHG's 2024-06-25 Harbingers post says Harbingers come in Agile and Brute variants and absorb abilities from the timeline boss that spawns them. Community pages that say "Tank archetype" are using a different label. This file uses the actor ids.

## Source ledger

| Claim | URL | Publisher / author | Date on page | Applies to | Class | Supports |
| --- | --- | --- | --- | --- | --- | --- |
| Season 5 live | https://forum.lastepoch.com/t/season-5-rage-of-the-frostborn-is-now-live/81860 | EHG, posted by EHG_DerrickG | 2026-10-01 | Patch 1.5 launch | developer documentation | Season label and date. No boss move list. |
| Hotfix 1.5.0.1 | https://forum.lastepoch.com/t/last-epoch-hotfix-1-5-0-1-notes/81887 | EHG, MickeyEHG | 2026-10-01 | 1.5.0.1 | developer documentation | Login and Blood Crystal menu only. |
| Patch 1.5.1 | https://forum.lastepoch.com/t/last-epoch-patch-1-5-1-notes/81911 | EHG, EHG_DerrickG | 2026-10-02 | 1.5.1 | developer documentation | Enables Pinnacle Morditas for Legacy and Offline. Body is on a separate notes site that this fetch did not read. |
| Morditas and God of Bloodshed | https://forum.lastepoch.com/t/rage-of-morditas-coming-to-last-epoch-october-1/81757 | EHG, EHG_DerrickG | 2026-09-21 | Season 5 announcement | developer documentation | God of Bloodshed needs 400 corruption, does not scale with corruption, and is a harder Morditas. No move list. |
| Harbinger variants | https://forum.lastepoch.com/t/harbingers-of-ruin-whats-new/71684 | EHG, Yayifications | 2024-06-25 | Patch 1.1 design | developer documentation | Agile and Brute, inherited timeline-boss abilities, 10 timelines, eyes for the pinnacle. |
| Harbingers spawn after the timeline boss | https://www.lastepochtools.com/news/article/last-epoch-harbingers-of-ruin-patch-notes-71790 | EHG notes, mirrored by Last Epoch Tools | Forum original 2024-07-05 | Patch 1.1 | developer documentation | One Harbinger per timeline, spawned on boss defeat, absorbs that boss's abilities. |
| Lagon ability names | https://forum.lastepoch.com/t/the-convergence-update-beta-0-9-patch-notes/51975 | EHG, Yayifications | 2023-03-06 | Beta 0.9 | developer documentation | Names Moon Blast, Tidal Wave, Lightning Blast, and a melee attack. Performance note only. Not a damage table. |
| Lagon tentacles | https://forum.lastepoch.com/t/runes-of-power-beta-0-9-2-patch-notes/60471 | EHG, Yayifications | 2023-09-05 | Beta 0.9.2 | developer documentation | Campaign and monolith phases 1 and 3: damage the two tentacles, which share health with Lagon. Swipe area 56% larger. Ground visual around tentacles. |
| Soul Bomb distance | https://forum.lastepoch.com/t/beta-0-9l-patch-notes/58685 | EHG, Hackaloken | 2023-05-10 | Beta 0.9l | developer documentation | Soul Bomb deals less damage farther from its center. No element. |
| Soul Bomb indicators | https://forum.lastepoch.com/t/last-epoch-patch-1-4-2-notes/80956 | EHG, EHG_DerrickG | 2026-04-01 | Patch 1.4.2 | developer documentation | Temporary large damage cut while indicators failed. Not evidence the cut or the bug is still live. |
| Heorot Ice Spike | https://forum.lastepoch.com/t/last-epoch-patch-1-0-3-patch-notes/68385 | EHG, Yayifications | 2024-03-13 | Patch 1.0.3 | developer documentation | Names Ice Spike. The note is a multiplayer bugfix, not a damage or immunity statement. |
| Void Rahyeh Dive Bomb | https://forum.lastepoch.com/t/last-epoch-patch-1-2-1-notes/76245 | EHG, EHG_Wick | 2025-04-23 | Patch 1.2.1 | developer documentation | Harbinger of Hatred's Void Rahyeh Dive Bomb had visuals that did not match its damage. The visual exists. The note does not name the element. |
| Julra shift reminder | https://forum.lastepoch.com/t/last-epoch-beneath-ancient-skies-patch-notes/78635 | EHG, Yayifications | 2025-08-15 | Patch 1.3 | developer documentation | First use of Julra's most powerful attack shows an indicator that Temporal Shift can be used. |
| Ten timeline pairs | https://lastepoch.tunklab.com/timelines | Tunklab | No publication date on the page | Unknown versus 1.5 | datamined/reference data | Boss, Harbinger, zone, and stability table. |
| Actor skills and damage vectors | https://lastepoch.tunklab.com/_next/static/chunks/3nhe36ktaxpqv.js | Tunklab client bundle | Version string `1.5-preview1`; cache age about 31 days on 2026-10-02 | Preview, not verified live | datamined/reference data | Skill ids, damage vectors, hit flags, ailments, actor descriptions. |
| Julra damage coverage and Temporal Shift | https://lastepoch.tunklab.com/dungeon/temporal_sanctum | Tunklab | No page date | Unknown versus 1.5 | datamined/reference data | Encounter lists Void, Cold, and Lightning. Shift travels between eras without moving. |
| Last Epoch Tools timeline table | https://www.lastepochtools.com/endgame/monolith/timelines | Last Epoch Tools, Dammitt | Page chrome said Season 4 when searched | Not a Season 5 stamp | datamined/reference data | Same ten boss and Harbinger names. Boss cell for Blood, Frost, and Death said Formosus the Undying. |
| Lagon eye and wave stories | https://forum.lastepoch.com/t/how-do-i-best-defeat-lagon/74849 | Players on the EHG forum | Thread active 2025; individual post dates not re-copied | Player reports | community report | Eye telegraph, wave movement, cold and lightning stories. Also an edge safe spot this lane rejects. |
| Armor ignores damage over time | https://support.lastepoch.com/hc/en-us/articles/46361891210651 | EHG support | Updated 2026-07-13 | Guide page predates Season 5 | developer documentation | Reused. Armor does not mitigate damage over time. |

Omitted on purpose: Shade of Orobyss, Arena champions, most campaign bosses, woven-echo bosses, and exploit safe spots (Lagon stairs or platform edge). Precise cast seconds are omitted.

## Shared kits

Agile body, on Pride, War, Fear, Chaos, Treason, Cruelty, and Regret. Shared damaging moves in the extract: fire Beam hit (base 900), lunge physical plus void, portal spear physical plus void with Void Resistance Shred, void bolt with Frailty and Armour Shred, void rift stab, void slash, dash-back physical hit, melee physical hit. Several dashes are starters with no damage vector. The fire beam is the Harbinger's own line, not the inherited boss move.

Brute body, on Defilement, Ash, Tyranny, Destruction, Hatred, Brutality, and Denial. Shared damaging moves: Hammer Slam and its wave (physical hits, base 700), Time Slam (physical 700), Circular Attack (physical 1500), melee (physical 600). Frenzy Howl applies Slow, is marked undodgeable, and has no damage vector. Time Rift, Charge, Frenzy Slam, and Time Echoes are starters without a damage vector in the parent record, so their hit size is unknown. A brute stands in melee and slams. An agile boss dashes and throws a fire line. Neither kit is the previous boss's arena plan.

Inherited moves are the first two skills on each timeline Harbinger and are listed under that pair. Uber adds Harbinger of Regret and Harbinger of Denial have the shared kit only. Their actor text says they make Aberroth immune to damage.

## Timeline pairs

Documented means an official sentence or a field in the extract. Inference is the movement advice built from those fields. Gear lines use the reused defense rules.

### Abomination, then Harbinger of Defilement

Fall of the Outcasts. Actor `AbominationBoss`, zone `R1Q30`, timeline id 1. Description: invulnerable until Soul Vessels are destroyed. Vessels (`SoulGeneratorsinAbominationBossFight`) say they grant that invulnerability. Vessel beam display is Soul Wave, necrotic vector, fire tag: element unresolved. Spirit Decay on the vessel is a necrotic non-hit. Tail Flare is a physical and fire hit. Soul Reave is a necrotic hit. Stomps are physical hits.

Inference: break the vessels before expecting the body to take damage, and do not stand in the decay left around them. Tail Flare is a hit, so armor can matter for the physical part and fire resistance for the fire part. Decay is not a hit.

Harbinger `BruteHarbinger-Abomination` keeps Tail Flare and a Spirit Decay non-hit. It does not keep the vessels or the invulnerability text. The new problem is the brute slam kit at melee range. Pools are leftover ground, not a phase you wait out in a corner.

### God Hunter Argentus, then Harbinger of Pride

The Stolen Lance. Actor `OsprixHeorotSpearBossEmerging`, zone `R5Q30`, timeline id 2. Flame Leap's impact is a fire hit (base 1200). Ice Stab is physical plus cold and applies Frostbite. Ice Nova is a cold hit and applies Chill and Frostbite. Fire Bomb's explosion is a fire hit. Melee is a small physical hit. A phase-transition skill exists with no damage vector. This extract does not list a summon, so an add phase is not documented here.

Inference: leave the leap landing and the ice stab. Fire and cold are both real hits. Frostbite's chance field is 2.5 on Ice Stab, which is not a 0 to 1 chance, so stack behavior is not stated.

Harbinger `AgileHarbinger-Argentus` keeps Ballista (no damage on the parent) and Fire Bomb. It does not keep Ice Stab or Ice Nova. It adds the agile fire beam and void and physical dashes. Cold from the bird fight is the wrong prep for this Harbinger. Leave lines and spear telegraphs instead of watching for ice.

### Rahyeh, the Black Sun, then Harbinger of Hatred

The Black Sun. Actor `VoidRahyeh`, zone `R3Q30`, timeline id 3. Void Flare is a void hit and undodgeable. Void Skyfall's impact is a void hit. Dive Bomb's area is a void vector with a fire tag, so the element is unresolved if you trust the tag, and void if you trust the vector. This file does not pick a winner in the menu. Void Blast is a void hit. Teleport, the flare starter, and the summon parent have no damage vector. Patch 1.2.1 names the Harbinger's Void Rahyeh Dive Bomb and says its visual was wrong relative to the damage. That does not type the Rahyeh fight.

Inference: leave marked flares and the dive landing. Do not chase the landing to keep hitting. Summoned adds are a separate attacker. A death to an add is not Rahyeh.

Harbinger `BruteHarbinger-VoidRahyeh` keeps Void Flare (void, undodgeable) and Dive Bomb Slam (void hit, base 730) plus a Dive Bomb area (void vector, fire tag) and a Void Skyfall child. It adds the brute physical slams. It is a body in melee, not a flyer you only dodge at range. The current starter tip that says a Harbinger imitation is not the original move set is true and too vague to keep. See the deletion list.

### Frost Lich Formosus, then Harbinger of War

Blood, Frost, and Death. Actor `FrostLich`, zone not in the skill object, timeline id 4, level 76. Last Epoch Tools' Season 4 table says Formosus the Undying. That name is not a second actor in this extract. Ice Vortex is cold damage over time. Spirit Beam is a necrotic non-hit with a fire tag: element unresolved. Frost Spirit is cold plus necrotic, flagged both as a hit and as damage over time: dodge and armor are unresolved. Ice Patch applies Slow and has no damage vector. Resurrect Wengari has no damage vector.

Inference: leave the vortex and the beam. Slow on the patch is a positioning problem, not a resistance problem. Do not treat the resurrected adds as the lich.

Harbinger `AgileHarbinger-FrostLich` keeps a smaller Ice Vortex (cold damage over time) and the same conflicted Spirit Beam, marked undodgeable. It drops the resurrect and the ice patch from the skill list and adds agile void, physical, and fire-beam moves. The lich arena's "stand still and burn the boss" plan fails because this Harbinger dashes.

### Lagon, then Harbinger of Chaos

Ending the Storm, and the campaign actor. `LagonBoss` is campaign, zone `G120`, level 53. `LagonBossMonolith` is timeline id 5, zone `R6Q30`, level 80. Both descriptions say Lagon can only be damaged by attacking tentacles. Both use the same five skill ids. Patch 0.9.2 says the same tentacle change for campaign and monolith. Tentacle actors `LagonTentacleEnemy` and `LagonBossTentacleEnemy` are physical hits. A killer named Lagon's Tentacle is not the beam.

Sweep `LagonBossEyeBeamLeftToRight`, display Moon Beam: cold and lightning, not a hit, undodgeable, damage over time. Tags agree with the vector. Armor and dodge do not cover it. Official 0.9 names Lightning Blast, not Moon Beam, so the player-facing name of this sweep is not settled. Moon Blast's damage child is cold and lightning, a hit, undodgeable, and applies Slow. Tidal Waves are cold hits. Campaign base is 450 and monolith base is 700 on different ability ids. That is an extract difference, not a live measurement. Claw grab and melee are physical hits.

Community reports, not developer text: the eye changes before the sweep, and one post says phase-three waves move clockwise. Another post describes a stair or edge spot that avoids everything except Moon Blast. That spot is rejected. It is an exploit-style safe place, and 0.9.2's intent is the tentacle fight.

Inference: cross the platform when the sweep lines up, leave the Moon Blast mark, and give the cold waves a lane. Stop a long animation before the sweep. A movement skill can close the gap and does not make a caught player safe. Campaign and monolith share the plan. The monolith wave coefficient is the harsher of the two records.

Harbinger `AgileHarbinger-Lagon` keeps Tidal Waves (cold hit, base 300, lower than either Lagon wave) and a moving eye beam that is cold and lightning, undodgeable, tagged damage over time, and also flagged `isHit:true`. Hit versus damage-over-time is unresolved, so do not promise dodge or armor. It has no tentacles. The new lethal set is the agile fire beam and the void and physical dashes, including Void Resistance Shred on the portal spear. Prep adds fire and void. Positioning changes from "hit the tentacles, cross for the eye" to "a mobile attacker who also draws a beam and waves."

### Harton's Husk, then Harbinger of Treason

Fall of the Empire. Actor `VoidHarton`, zone `R4Q30`, timeline id 6. Description: Prophet of the Abyssal Sea. Corrupted Slash is physical plus void. Claw Slam and the small stab are physical hits. Oblivion Spike is a void hit. The sundering starter and Void Stab parent have no damage vector. This is not Admiral Harton. Admiral Harton is a separate campaign actor with lightning skills.

Harbinger `AgileHarbinger-VoidHarton` keeps Abyssal Sundering (small void hit) and a void spike (void hit, base 400), plus the agile kit and fire beam. The husk's claw slam is not on this list. You lose a mostly stationary slam pattern and gain dashes. Void still matters. Fire and physical are new.

### Emperor of Corpses, then Harbinger of Destruction

Reign of Dragons. Actor `UndeadDragonBoss`, zone `R7Q30`, timeline id 7. Official 0.9l: Soul Bomb deals less damage the farther you are from its center. No element in that note. The damage child is a necrotic hit with a fire tag. A chained ability is named Poison Pool, repeats necrotic damage, is not a hit, and is tagged poison. Three labels disagree. Element is unknown. Patch 1.4.2 cut Soul Bomb damage and said indicators often failed. The note calls the cut temporary. Season 5 posts do not say whether the cut or the bad indicators remain. Spirit Breath is necrotic damage over time, undodgeable. Flesh Impact's explosion is a necrotic hit with a fire tag. Melee and roar exist. Roar applies Slow.

Inference: leave the center of Soul Bomb by a wide margin, even if the marker looks late or missing, because the official rule is distance and the 1.4.2 bug was bad markers. Do not stand in the pool afterward. Breath is the non-hit that resistance and endurance address and armor does not. This is not Emperor's Remains.

Harbinger `BruteHarbinger-UndeadDragon` keeps Spirit Breath and a roar that spawns adds. It does not have Soul Bomb. The brute slams are the new one-shot candidates, especially Circular Attack. The "run from the bomb center" plan does not transfer. Leave slam markers, and do not stand in the breath. Physical hits can crit in this extract (crit chance 0.05, multiplier 2). Breath cannot, because it is not a hit.

### Husk of Elder Gaspar, then Harbinger of Fear

The Last Ruin. Actor `GasparHuskBoss`, zone `R8Q30`, timeline id 8. Tri-beam children are separate non-hits: fire, lightning, and cold. Frozen Ice Storm is a cold hit (base 750). Doom Blast's void explosion is a void hit (base 1150). Spark Discharge's mine is a lightning hit. Totemic flame is a fire hit. Piercing Void is a void hit. Teleport explosion is a small void damage-over-time hit flag. Flaming Void Totems are summoned. The display name Doom Blast is used for both a fire starter and a void explosion, so the name is not the element.

Inference: when he goes to the center, leave every beam. Beams are not hits, so dodge and armor do not cover them. Mines detonate on contact in the skill name only. Treat the spark marks as unsafe ground. This is the widest resistance set in the timeline bosses: fire, cold, lightning, and void, plus physical only if a totem or add says so. Overcap does not answer area penetration. That rule is already in the defense research.

Harbinger `AgileHarbinger-Gaspar` keeps the three-element tri-beam (non-hits, lower bases) and lightning mines. It does not keep the void explosion or the ice storm. It adds agile movement, void hits, and the fire beam. The "everything at once" boss becomes a mobile fighter plus a spinning beam and mines. Still leave the beams. Add void and physical for the agile kit. The community line about walking with a turret is inference, not a documented safe radius.

### Heorot, then Harbinger of Tyranny

The Age of Winter. Actor `HeorotBoss`, zone `R9Q30`, timeline id 9. Description: Cannot be Frozen. That sentence is data, not the 1.0.3 bugfix. Ice Spike's damage child, display Frosted Eruption, is a cold hit. Three ice circles are cold hits and undodgeable (bases 500, 650, 800). Blizzard is cold damage over time and applies Cold Resistance Shred. Charge is physical plus cold. The antler sweep is a small physical and cold hit. Jump back is a physical hit. Ice Guard is an invulnerability-named buff with no damage vector. Adds in the same zone include Spirit of Grael and Spirit of Jormun. They are not Heorot.

Inference: leave ice circles as soon as they appear, because dodge will not save a player who stays in them. Ice Spike is the named official move and is a cold hit, so armor can help the hit and cold resistance helps the element. Cold Resistance Shred on the blizzard means overcap is the relevant extra, not more armor. Freezing Heorot is not a defensive plan. The current freeze tip does not say this and should be removed.

Harbinger `BruteHarbinger-Heorot` keeps a whirlwind with no damage vector that applies Frostbite (chance field 1.6, so stacks are not stated) and a Frost Beam that is a cold non-hit. It does not keep the undodgeable circles. It adds brute physical slams. Cold resistance still matters for the beam. The lethal new hits are physical. A movement skill helps leave a slam and does not cover the beam if you stay in it.

### Volcanic Shaman, then Harbinger of Ash

Spirits of Fire. Actor `ShamansBoss`, timeline id 10, level 90. Lava Plume children are fire hits at three bases (500, 800, 1150). Eruption, Magma Ball, and Flaming Orb are fire hits. Dragon Strike is physical plus fire. Torch Flame is a fire non-hit without a damage-over-time tag, so armor is unresolved.

Inference: leave the plume marks. The largest plume coefficient is the hit to respect. Fire resistance covers the fire hits. Armor can matter on Dragon Strike's physical part. This is a ground-marker fight, not a beam fight.

Harbinger `BruteHarbinger-FireShamans` keeps Magma Ball, which is a fire hit and undodgeable, and adds Necrotic Spirit plus a necrotic hit named Totemic Flame Burst (base 800). Plus brute physical slams. The shaman's all-fire prep is incomplete. Necrotic and physical are the Harbinger's extra lethal types. Undodgeable magma means walking out, not stacking dodge.

## Aberroth and the other pinnacle actors

Normal Aberroth is actor `Aterroth`, zone `M_Harbinger`. The actor is flagged `isHarbinger` in the extract. That flag is not the timeline Harbinger system. Documented moves: Persisting Void is a void hit and undodgeable. Void Beam, on the separate actor `AterrothsInvisibleFriend`, is a void non-hit, undodgeable, with beam interval 0.25 and duration 2.5 in unnamed units. Its description text is a copied player freeze skill and is ignored. Its ailment id 129 is past the end of this bundle's ailment enum (last id 128), so the ailment is unknown. Fiery Ground is fire damage over time. Roots, Winds, Lightning, and Quicksand parents have no damage vector. Roots apply ailment id 7, which is Poison in the enum, with chance field 5. That reading is too strange to ship as "applies poison."

Inference: leave the beam and the fire ground. Armor and dodge do not cover either if the flags hold. Persisting Void is a hit that dodge will not roll. Do not invent a safe side of the room. Community posts about parking beams opposite the slam are reports from 2024, not a current map.

Uber actor `UberAterroth`, display Herald of Oblivion, zone `WE524`. Description: immune while defended by other Harbingers. The skill list in this extract matches normal Aberroth. That does not prove the numbers match. Harbinger of Regret (`AgileHarbinger-UberAberroth`) and Harbinger of Denial (`BruteHarbinger-UberAberroth`) have the shared kits only, and their text says they make Aberroth immune. Kill order inference: remove those two before expecting the body to take damage. Their own tips are the agile and brute kits, with no inherited Aberroth spell.

Harbinger of Cruelty and Harbinger of Brutality are the same shared kits in `M_Harbinger`, with no inherited boss skills. A death to either is not a timeline Harbinger and not Aberroth.

Morditas, actor `MorditasBoss`, zone `WE536`, description Demigod of Bloodshed. God of Bloodshed is `MorditasBossPinnacle`, zone `WE537`. The skill ids match. Official text says the pinnacle is harder, requires 400 corruption, and does not scale with corruption. Same ids are not a tuning proof. Largest hits in the extract: Runic Leap physical 1500, Runic Eruption physical 1000, Blood Spear physical 900, Mega Slam physical and cold 800. Blood Storm is cold damage over time and applies Cold Resistance Shred. Wave of Cold, Ice Shard, and Bloodice Slam add cold hits. Chest of Ice and Bloodwind Trap are cold and undodgeable, and the trap is not a hit. Many parents (Blood Runes, the slam starter) have no vector. The mega slam is the mixed hit. The cold ring after it is the leftover hazard.

Inference: leave the leap and the mega slam before they finish, then leave the cold ground they leave behind. Physical resistance and armor address the hits. Cold resistance addresses the cold part and the ring. Armor does not address the ring. A movement skill is a way to get out, not a promise. Use the same shapes for both Morditas fights, and do not tell the player the pinnacle hits as hard as the seasonal one.

## Dungeons

### Chronomancer Julra

Actor `VoidNagasaBoss`, zone `Dun1Q30`. Tunklab's dungeon page lists Void, Cold, and Lightning for the encounter, not per attack. Temporal Shift moves between Divine and Ruined eras and does not move the character. Patch 1.3 adds an indicator the first time her most powerful attack starts, reminding the player to shift.

Catastrophic Implosion's damage child is a void hit, base 2000, undodgeable, and applies Doom with chance field 4. Doom's stack cap in the support article is 4. The chance field is not a normal probability, so "four Doom stacks" is a community reading, not a clean field. Dimension Tear areas are void damage over time and apply Void Resistance Shred. One tear child is tagged poison while its vector is void. Unravelling Decay beams are void damage over time, undodgeable, and shred void resistance. Frigid Tide is a cold hit and applies Chill. Chilled Seeker is a cold undodgeable hit. Shocking Impact explosions are lightning hits at three sizes (300, 600, 900). Ruined Pillars are immune in the Ruined Era and cast Lightning Bolt, a lightning hit that applies Shock. Divine Pillars are vulnerable to melee, and the text says killing one destroys its ruined counterpart. Healing Fountains are vulnerable to melee.

Inference: shift before the room-wide blast. Shifting does not carry you off a tear or a beam, so place tears at an edge and move after you arrive. Beams are not a dodge check. Pillars in the ruined era are a lightning problem. Higher dungeon tiers change health in the extract. They are not a new move list. No skill guarantees the shift is on time.

### The Mountain Beneath, then Stone Titan's Heart

`StoneTitanBoss` and `StoneTitanHeartBoss`, zones `Dun2Q20` and `Dun2Q30`. Kindling skills are named Burning Kindling and say 40% self damage. Root Walls say destroy them to access kindling. Community guides say to burn both pyres with Burning Amber while the body is untargetable. The extract does not say invulnerable in the description field. Do not ship invulnerable as a fact. Shockwave starter has no damage vector. Entangling Roots are a small physical damage-over-time hit.

The Heart is a second boss actor. Spore Beam is physical plus poison, not a hit, undodgeable. Energy Wave is a physical hit and applies Poison. Energy Stone is a physical hit. Rock Fall and bulbs have no damage on the parent. Volatile Bulbs say they are vulnerable to melee.

Inference: use the dungeon's light tool on the kindling, then on the Heart leave the spore beams and the energy wave. Poison on the beam is part of the vector, so poison resistance matters there and armor does not. The wave is a hit, so armor can matter. Do not promise that destroying bulbs stops the beams. That link is not in the records.

### Fire Lich Cremorus

Actor `FireLichBoss`, zone `Dun3Q30`, description Immolator of Souls. Collapsing Decay is a necrotic hit, base 600, undodgeable. Incendiary Torrent is a fire non-hit and applies Ignite. Raging Soul is a fire hit and applies Ignite. Flaming Orbiter is a fire hit. Swap Floors, Disintegration, golem bursts, and the summons have no damage vector on the parent. Thrall of Cremorus is a separate melee actor.

Inference: walk out of the necrotic ring. Dodge will not roll it. Leave the fire waves. Ignite is fire damage over time, so armor does not cover the ignite even if the wave's hit flag were friendly. Floor swaps are a phase change with unknown safety. The dungeon shield ability is not documented here as immunity. Do not promise a shield.

## Campaign notes beyond the starters

Majasa, zone `H140`, has two actors. Phase 1, Goddess of Opulence: Medusa Stare is undodgeable, display name Flamethrower, tiny physical vector, fire tag, and ailment Stone Stare. Element and damage are a poor match, so do not call it fire. Sand teleport, falling emeralds, ruby spear, and diamond slam parents have no damage vector. Gem projectile is a fire vector with a cold tag and the display name Frostbolt. Element unresolved. Three essence actors are separate killers: Essence of Opulence casts a lightning orb, Essence of Greed a poison orb, Essence of Avarice a flamethrower. Phase 2, Goddess of Avarice, is physical melee, including Armour Shred and Physical Resistance Shred on the tail, plus the stare again. A coffin sprint is a physical hit.

Inference: break the stare's line. It is undodgeable, so a movement skill only helps if it actually leaves the line. Phase 2 is a physical melee fight. Do not merge an essence death into Majasa's profile.

Emperor's Remains, zone `B100`, description Void Puppet, is not the Emperor of Corpses. Its list is transforms, void rifts, mortars, and claw and scythe swipes. Do not show Soul Bomb tips for that name.

## Starter tips to delete or replace

Current menu text is from `BossCatalog.cs`.

Lagon, tip 1. Keep the idea, replace the wording. Moon Blast and Lightning Blast are official names, but the extract's sweep is display-named Moon Beam and is the undodgeable cold and lightning damage over time. The tip never says to cross the platform or that tentacles are the damage target.

Lagon, tip 2. Delete the sentence about reassessing health. That is generic Last-death advice. The Tidal Wave sentence can stay only if it says the waves are the cold hits and that another hazard can block the lane. "Rather than chasing damage" is filler.

Emperor of Corpses, tip 1. Keep. It matches the 0.9l distance rule. Do not add the 1.4.2 damage cut as if it were still the live hit.

Emperor of Corpses, tip 2. Delete. "Reserve an escape route before a long animation" is generic, and the health sentence repeats tip 1.

Heorot, tip 1. Delete as written. "Avoid its telegraph" and "do not stand still" do not say which mark, and they hide the undodgeable ice circles and the blizzard shred.

Heorot, tip 2. Delete. It sends the player to generic freeze advice and does not state the datamined "Cannot be Frozen" line. The 1.0.3 note does not support boss immunity.

Julra, tip 1. Keep. Shift does not move you. That matches the dungeon ability text. Add the room-wide blast and the leftover tears when replacing nearby copy, but this tip is not unsupported.

Julra, tip 2. Delete the health, crit, and pool sentence. It is generic and repeats Last death. The Void, Cold, and Lightning checklist is real encounter coverage, not a dodge plan. It should not be a second fight tip unless the death's element is unknown.

Harbinger of Hatred, tip 1. Keep the landing idea. Patch 1.2.1 is why the visual matters. Drop the word "early." No current source gives a second count.

Harbinger of Hatred, tip 2. Delete. "Use the captured element" and "not interchangeable" are matcher warnings. They do not say that this Harbinger is a brute: physical slams plus the flare and the dive, in melee, after a flyer.

## Open questions

Live 1.5.0.1 and 1.5.1 were not diffed against `1.5-preview1`. The forum posts do not list these moves.

Soul Bomb's element is unresolved (necrotic vector, fire tag, poison-named pool). Whether the 1.4.2 damage cut and broken indicators are still in the game is unknown.

Aberroth beam ailment 129 has no name in this enum. Uber and normal Aberroth share skill ids here. Official difficulty text for God of Bloodshed says the pinnacle is harder, which the shared ids do not measure.

Several parents are starters with no damage vector. Child size is unknown until that child is the killing blow.

Visual and audio tells are missing for most Harbinger slams. Community eye color for Lagon is not an official telegraph.

Tag versus vector conflicts are listed above and must stay unknown in any damage table.
