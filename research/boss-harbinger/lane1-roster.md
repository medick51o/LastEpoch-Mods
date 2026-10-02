# Lane 1: Season 5 roster and encounter relationships

Access date for every web source below: 2026-10-02. No code was changed. Damage tables are out of scope. An ability name, a scene name, a visual, or an internal id is not a damage type. Unknown stays unknown.

Baseline this lane read: Terrible Death Log catalog `0.1.2` (`BossCatalog.cs`). It has five profiles: Lagon, Emperor of Corpses, Heorot, Chronomancer Julra, Harbinger of Hatred. That is not this roster.

## Current patch

Live Season 5 is **Rage of the Frostborn**, client **1.5.1**.

| Build | When | What it establishes |
| --- | --- | --- |
| 1.5.0 | Season launch 2026-10-01 16:12 UTC. Notes posted 2026-09-25 and hosted at the 1.5 patch notes page. Launch time in those notes: October 1, 2026, 11:00 CDT. | Season content, including Morditas. |
| 1.5.0.1 | 2026-10-01 22:42 UTC | Login and Blood Crystal reward fixes. No roster change. |
| 1.5.1 | 2026-10-02 16:58 UTC | Current patch. Opens Pinnacle Morditas to Legacy and Offline characters. |

The extracted bestiary used here is Tunklab **Game Version: 1.5 preview**, sitemap `lastmod` 2026-09-30. That is before launch and before 1.5.1. A search snippet earlier the same day still said 1.4.7. The pages that contain Morditas, fetched after that, carry the 1.5 preview label. Treat them as a pre-launch 1.5 extract, not as a dump of the 1.5.1 client.

1.5 notes do not add, remove, or rename a Monolith timeline. They also do not restate timeline levels, stability, or the ten Harbinger pairings. Those numbers below stay datamined unless an older developer note is cited. A historical note does not, by itself, prove the Season 5 fight.

## Key findings

1. There are 10 Monolith timelines. Each has one timeline boss and one follow-up Harbinger. Normal and Empowered are the same actors at different area level, stability, and corruption. They are not separate bosses.
2. The ten timeline Harbingers are Defilement, Pride, Hatred, War, Chaos, Treason, Destruction, Fear, Tyranny, and Ash. Four more Harbingers exist only inside Aberroth fights: Cruelty, Brutality, Regret, and Denial.
3. A Harbinger is a separate actor from the boss that spawns it. Patch 1.1 said Harbingers absorb abilities from that boss. 1.5 still has an Agile Harbinger. The inherited move list is not re-verified for 1.5.1.
4. Aberroth needs his own profile. Uber Aberroth is a second actor, displayed in the extract as Herald of Oblivion, in Threshold of Eternity at 500 corruption.
5. Season 5 adds a chain that is not a timeline: Site of Carnage, Yrun Champion of Morditas, Morditas, then God of Bloodshed. God of Bloodshed is the pinnacle EHG also calls Pinnacle Morditas. It is a different actor and a different display name from Morditas.
6. Shade of Morditas, in the 1.5 notes, is a non-corporeal follower during the random encounter. It is not a bestiary boss.
7. Same display name does not mean same encounter. Lagon, Majasa, Yrun, Harton, Formosus, and Aberroth each have at least one collision called out below.
8. Emperor of Corpses and Emperor's Remains are different encounters. Rahyeh, The Black Sun and Harbinger of Hatred are different encounters.
9. Lightless Arbor has two dungeon-boss actors: The Mountain Beneath and Stone Titan's Heart. The Mountain record has 100% less damage taken.
10. The current catalog cannot identify this roster. Its Lagon alias is the single word Lagon. Both located Lagon actors display as Lagon, God of Storms.

## Timeline boss and Harbinger pairs

Area levels and stability are Tunklab 1.5 preview. Levels match the Season 3 change on 2025-08-21 (Fall of the Outcasts 58 to 62, Stolen Lance 62 to 66, Black Sun 66 to 70, Blood, Frost, and Death 70 to 74, Ending the Storm 75 to 78, Fall of the Empire 80 to 82). Reign of Dragons 85 and the three level 90 timelines were already at those levels. 1.5 does not restate them.

Empowered rows in the extract are area level 100. Official support on 2026-07-13 says standard timelines cap at 50 corruption and Empowered timelines start at 100 corruption with no maximum. That page does not state the Empowered area level. The July 2026 support pages predate Season 5.

Quest 3 is the boss echo. Official support, updated 2026-07-13, says the third quest echo is the timeline boss. Stability is the gate to select that echo.

Archetype is the internal id prefix only. It is not a damage type. Brute and Agile are the two prefixes in this extract.

| Timeline | Level | Quest 3 zone | Stability normal / empowered | Boss | Boss internal id | Harbinger | Harbinger internal id |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Fall of the Outcasts | 62 | The Altar of Flesh | 240 / 800 | Abomination | AbominationBoss | Harbinger of Defilement | BruteHarbinger-Abomination |
| The Stolen Lance | 66 | Argentum Spire | 250 / 800 | God Hunter Argentus | OsprixHeorotSpearBossEmerging | Harbinger of Pride | AgileHarbinger-Argentus |
| The Black Sun | 70 | The Ruins of Solarum | 250 / 800 | Rahyeh, The Black Sun | VoidRahyeh | Harbinger of Hatred | BruteHarbinger-VoidRahyeh |
| Blood, Frost, and Death | 74 | The Lair of Formosus | 300 / 800 | Frost Lich Formosus | FrostLich | Harbinger of War | AgileHarbinger-FrostLich |
| Ending the Storm | 78 | Eye of the Storm | 320 / 850 | Lagon, God of Storms | LagonBossMonolith | Harbinger of Chaos | AgileHarbinger-Lagon |
| Fall of the Empire | 82 | Dreadnought's Wreckage | 320 / 850 | Harton's Husk | VoidHarton | Harbinger of Treason | AgileHarbinger-VoidHarton |
| Reign of Dragons | 85 | The Rotting Hoard | 400 / 850 | Emperor of Corpses | UndeadDragonBoss | Harbinger of Destruction | BruteHarbinger-UndeadDragon |
| The Last Ruin | 90 | The Chambers of Ruin | 400 / 900 | The Husk of Elder Gaspar | GasparHuskBoss | Harbinger of Fear | AgileHarbinger-Gaspar |
| The Age of Winter | 90 | The Frozen Citadel | 400 / 900 | Heorot | HeorotBoss | Harbinger of Tyranny | BruteHarbinger-Heorot |
| Spirits of Fire | 90 | The Caldera of Souls | 400 / 900 | Volcanic Shaman | ShamansBoss | Harbinger of Ash | BruteHarbinger-FireShamans |

LE Tools' timeline table shows the same ten bosses, the same ten Harbingers, and the same levels. That page still banners Season 4 Shattered Omens, so it is agreement, not a Season 5 stamp.

### How a Harbinger is reached

Order, with the newest source that still says it:

1. Clear quest echoes until quest 3. Kill the timeline boss there. Support, 2026-07-13.
2. The timeline Harbinger can spawn when that boss is defeated, if the Harbinger Crest is enabled. Crest rule: support, 2026-02-14. Spawn on boss defeat, and the crest toggle: patch 1.1, July 2024, via the LE Tools mirror of the notes.
3. First character: the first Harbinger is the gate to Empowered timelines. Support, 2026-02-12, still describes that character as conquering the three level 90 timelines first. Season 3, launched 2025-08-21, moved that first Forgotten Knights Harbinger to the first completed level 90 timeline rather than the last one. 1.5 still has a bugfix for the first Harbinger sending the player to the Shattered Road, so that handoff still exists. 1.5 does not restate the three-timeline rule.
4. An alt that shares a stash can open Empowered timelines after one level 90 normal timeline, once any character has the first Harbinger kill. Support, 2026-02-12. Not restated in 1.5.
5. Later Harbingers spawn in Empowered timelines. The Forgotten Knights Harbinger panel shows a minimum corruption per Harbinger. Support, 2026-02-14. This lane did not find a published numeric table for those minimums.
6. After a Harbinger has been defeated, 1.1 said it keeps spawning in that Empowered timeline while the crest is on. Not restated in 1.5.
7. Harbinger Eyes drop from defeated Harbingers. The 1.1 design used one Eye, after all 10 timeline Harbingers, at the altar, to enter Aberroth. Tunklab 1.5 preview still lists a Harbinger Eye drop on every timeline Harbinger, with a minimum corruption of 200 on that drop line. That 200 is a drop condition in the extract, not the spawn minimum.

Patch 1.1's sentence that Harbingers absorb abilities from the spawning timeline boss is the developer basis for "inherited moves." It is not a list. 1.5 fixes "the Agile Harbinger's fire laser" indicator and does not name which Agile Harbinger. Do not assign that laser, or any community move list, from this lane.

### Same actor, second name, or several bodies

- Frost Lich Formosus has custom name Formosus the Undying. One actor. LE Tools labels the boss Formosus the Undying. Not a second timeline.
- Volcanic Shaman has custom names Bhuldar, Herkir, and Logi. Quest text puts all three at the caldera. One encounter with three named members. Not three timelines. Whether each nameplate has a private kit is unknown.
- God Hunter Argentus's internal id contains Heorot. The display name, zone, and timeline are Argentus in The Stolen Lance. Do not merge him into Heorot.
- Abomination's description says it is invulnerable until Soul Vessels are destroyed. Soul Vessel is its own actor, difficulty Boss, and its description says it grants that invulnerability. A death can name the vessel.
- Heorot's description in the extract says "Cannot be Frozen." That is datamined text, not a 1.5 developer note. Son of Artor and Spirit of Grael are summoned by this Heorot in The Frozen Citadel.
- Monolith Lagon's description says he can only be damaged by attacking tentacles. Lagon's Tentacle is a separate display name and can appear in the monolith arena and in campaign Lagon zones. Another tentacle actor reuses the display name Lagon, God of Storms. A killer string of Lagon, God of Storms can be the body or that tentacle actor.
- Emperor of Corpses is monolith only, The Rotting Hoard. Emperor's Remains is a campaign boss, Void Puppet, in The End of Ruin, and the extract also places that actor in Fall of the Empire, The Black Sun, and The Last Ruin echoes. 1.5 updates the Emperor's Remains model, so the campaign encounter remains. 1.5 also fixes Emperor of Corpses explosion indicator size online. They are not variants of each other.

## Shade, Aberroth, and the other Harbingers

Shade of Orobyss is not a timeline boss. Support, 2026-07-13: shades appear farther from a timeline's center, add corruption, reset the echo web, and do not reset stability or active modifiers. The actor's zones in the extract are Echo of a World and Confluence of Oblivion. Confluence of Oblivion is a woven echo, minimum corruption 100, whose objective is to slay five Shades. Tentacle, blood effigy, and sentient cube actors exist on that echo and can be the killer.

Aberroth, internal id Aterroth, is a pinnacle boss in the extract. Zone field: Altar of Orobyss. Phases at 100 to 77, 77 to 63, 63 to 49, 49 to 35, and 35 to 0 percent. Spawn flags: monolith yes, campaign no, dungeon no.

The June 25, 2024 developer blog says all 10 Harbingers, then a Harbinger Eye placed on the altar, pulls the player into the Harbinger's Domain to fight Aberroth across four eras. It does not use the strings Altar of Orobyss or Altar of Oblivion. The player-facing zone name is unresolved. The 10-Harbinger gate is 1.1 design. 1.5 does not restate it. The preview still has the 10 Harbingers, the Eye, and Aberroth, and 1.5 still routes the first Harbinger kill to the Shattered Road.

Two Harbingers stand at Altar of Orobyss and are not timeline Harbingers:

| Display name | Internal id | Notes |
| --- | --- | --- |
| Harbinger of Cruelty | AgileHarbinger-pre-Aberroth | Monolith spawn flag is false. Zone is still Altar of Orobyss. |
| Harbinger of Brutality | BruteHarbinger-pre-Aberroth | Same flag conflict. |

Uber fight, woven echo Threshold of Eternity, minimum corruption 500, not purchasable, not reweavable, objective text "Defeat Aberroth":

| Display name in the extract | Internal id | Relationship |
| --- | --- | --- |
| Herald of Oblivion | UberAterroth | Difficulty Uber Boss. Description: immune while defended by other Harbingers. Tunklab subtitle on the page is Uber Aberroth. |
| Harbinger of Regret | AgileHarbinger-UberAberroth | Listed as the phase 2 add. Description: makes Aberroth immune to damage. |
| Harbinger of Denial | BruteHarbinger-UberAberroth | Listed as the phase 4 add. Same immunity description. |

EHG's September 21, 2026 Morditas post still points at Uber Aberroth as the fight God of Bloodshed is meant to bridge toward. So the uber encounter is still part of the Season 5 difficulty curve in developer writing. The nameplate conflict is below.

An invisible add summoned by Aberroth uses the display name Aberroth. A sibling slug, UberAterrothsInvisibleFriend, exists. This lane did not open that second page, so its display name is unknown.

## Dungeons

Three dungeons, four tiers each. Tier data and the explicit "Damage Types" lines are Tunklab 1.5 preview. Those lines are encounter coverage, not an attack map. 1.5 does not restate the tier levels. It does fix Julra's teleport when a player stays outside the arena, party respawn on a dungeon boss, and Stone Titan's Heart spawning in the wrong place online. All three dungeon bosses therefore still exist in 1.5.

| Dungeon | Boss | Internal id | Zone | Tiers (area level) | Datamined damage coverage |
| --- | --- | --- | --- | --- | --- |
| Temporal Sanctum | Chronomancer Julra | VoidNagasaBoss | The Sanctum Archive | 55, 80, 95, 100 | Void, Cold, Lightning |
| Lightless Arbor | The Mountain Beneath | StoneTitanBoss | Titan's Hollow and Titan's Rest | 20, 65, 88, 100 | Physical, Poison |
| Lightless Arbor | Stone Titan's Heart | StoneTitanHeartBoss | Titan's Rest | same tiers | unknown |
| Soulfire Bastion | Fire Lich Cremorus | FireLichBoss | The Soul Furnace | 45, 75, 92, 100 | Fire, Necrotic |

The Mountain record includes 100% less damage taken and stun immunity. The Heart is a second dungeon-boss actor, not a cosmetic. Roots, barricades, and bulbs exist and are omitted as proxies. Julra's internal id contains Nagasa and Void. That id does not prove her damage. The coverage line above is a separate field. Pillars and a healing fountain actor are omitted.

The Julra coverage line matches the type set already in the mod catalog. This lane does not extend it to attacks.

## Campaign bosses that collide with endgame names

| Encounter | Internal id | Where | Relationship |
| --- | --- | --- | --- |
| Lagon, God of Storms | LagonBoss | Seafloor Colosseum, campaign only | Same display name as the Ending the Storm boss. Different actor. Description also says tentacles are the damageable part. |
| Lagon's Tentacle | LagonTentacleEnemy | Temple of Lagon, Temple Depths, Seafloor Colosseum, Eye of the Storm | Separate killer name. Not the boss. |
| Majasa, phase 1 | Majasa | The Chamber of Vessels | Description Goddess of Opulence. Display name Majasa. |
| Majasa, phase 2 | MajasaBossPhase2 | The Chamber of Vessels | Description Goddess of Avarice. Summoned by phase 1. Display name still Majasa. |
| Emperor's Remains | EmperorsRemains | The End of Ruin, plus three timelines as an echo boss | Not Emperor of Corpses. |
| Admiral Harton | AdmiralHarton | The Dreadnought's Deck, and as an echo boss on several timelines | Not Harton's Husk. |
| Admiral Harton (The Immortal Summit) | E90AdmiralHarton | The Immortal Citadel, campaign only | Second Harton boss actor. Same display name in the title, with the summit subtitle on the record. |
| The Husk of Elder Pannion | ElderPannionHusk | The Lower District, and echo boss on Fall of the Empire, The Black Sun, The Last Ruin | Not Gaspar's husk. |
| Yulia's Husk | ElderPannionHuskMonolith | Voidscarred Necropolis, Fall of the Empire quest 1 | Miniboss. Not the timeline boss. |
| The Observer | Observer | The Observer's Prison | Campaign boss. 1.5 still changes the Observer quest reward, so the quest remains. |
| Yrun | YrunBoss | The Tomb of Morditas | Description The Frostroot Warden. Also flagged as a monolith echo boss on five timelines. |

No spawning campaign boss record was found for a separate Heorot or a separate Rahyeh. `/npc/Heorot` and `/npc/Rahyeh` do not spawn. The monolith Heorot and Rahyeh, The Black Sun are the located boss actors. A campaign Heorot or Rahyeh nameplate may still exist under an unopened id.

1.5 also mentions these encounters without giving them new profiles: Elder Pannion ability visuals, Flame Guard Sula music in Welryn Outskirts, Spreading Frost, Praetor Taranis volume, The Crimson Blade decoys. Tunklab's echo list spells Flame Guard Sulla. Spelling is unresolved. They are echo or story bosses, not timeline bosses.

## Season 5 encounters

Developer post, 2026-09-21, plus the 1.5 notes, plus Tunklab's Rage of Morditas page and echo pages.

Rage of Morditas is a random encounter in the campaign from Chapter 2 and in echoes, started at a Blood Crystal. A non-corporeal Shade of Morditas follows and collects souls. Phase two revives enemies inside a blood-ice gauntlet. This shade is not in the boss bestiary pages opened here. Whether it can land a killing blow is unknown.

Kill count during those encounters unlocks woven echoes:

| Echo | Kill count in the extract | Objective | Boss actor |
| --- | --- | --- | --- |
| Site of Carnage | 750 | Fight waves of Wengari | No single boss. Minibosses include Wengari Beastmaster and Wengari Raid Leader. |
| Blood of Farwood | 2,000 | Slay Yrun, Champion of Morditas | YrunBossEndgame. Zone code WE538. |
| Circle of Frozen Blood | 4,000 | Defeat Morditas | MorditasBoss. Description Demigod of Bloodshed. Zone code WE536. |
| Demigod's Ascendance | drops from the Circle of Frozen Blood kill | Defeat the God of Bloodshed | MorditasBossPinnacle. Zone code WE537. Minimum corruption 400. |

EHG: God of Bloodshed is a more difficult version of Morditas, also called Pinnacle Morditas. It requires 400 corruption to place and does not scale difficulty with corruption. Anger, tracked in the Nexus, raises difficulty and rewards of the three earlier echoes and can be soothed.

1.5.0 race rules required a seasonal online solo character. 1.5.1, on 2026-10-02, enables Pinnacle Morditas for Legacy and Offline characters. That is an availability change, not a new actor.

Yrun, Champion of Morditas is not the campaign warden. Different actor, different display name, monolith boss, types Human, Plant, Heorot follower. The campaign Yrun uses the same type tags in the extract. Shared moves are not verified. 1.5.1 names Yrun, Champion of Morditas in a Dreamslash hitbox fix, so the seasonal actor is in the live notes, not only in the preview.

The God of Bloodshed record has datamined chances to apply bleed and frostbite, plus movement and damage modifiers. That is not a damage table. Lane 2 should start there and still treat elements as unknown until an attack is sourced.

Endgame Majasa is a separate woven echo, Tomb of Vessels, minimum corruption 300. Objective text says "Slay Majasa." Actors are Goddess of the Sands, then Goddess of Blood. They are not the campaign phase names Goddess of Opulence and Goddess of Avarice.

## Omissions

Not given rows, on purpose:

- The long echo-boss pool inside each timeline (Orchirian the Rampant, Spymaster Zerrick, Praetor Taranis, Primeval Dragon, Oracle of the Black Sun, Argolos the Blessed, Flame Guard, and the other names on each timeline's boss line). Those are echo encounters. They are not the timeline boss or its Harbinger.
- Arena champions, Nemeses, Omens, and Rift Beasts.
- Woven bosses other than Threshold of Eternity, Confluence of Oblivion, Tomb of Vessels, and the four Morditas-chain echoes. Season 2 added encounters such as the Draal Queen. This lane did not re-audit that whole woven list against 1.5.1.
- Julra pillars and fountain, Mountain roots and barricades, Cremorus minions and soul cages, Shade tentacle, blood effigy, and cube, Void Despair on the uber echo, BruteHarbingerVolatileZombie, and UberAterrothsInvisibleFriend's display name.
- Campaign actors not opened: Apophis, Ada, Immortal Emperor, and any unlocated campaign Heorot or Rahyeh boss.
- Per-Harbinger corruption minimums. They are a UI value, not a number in the sources above.
- Attack names and damage mixes, except the three dungeon coverage lines and the God of Bloodshed ailment-chance stats.

## Contradictions

1. Uber name. Threshold of Eternity's objective says "Defeat Aberroth." The enemy row says Herald of Oblivion, with a Tunklab subtitle Uber Aberroth. The June 2024 blog and the LE Tools Season 4 echo page call the boss Aberroth. EHG in September 2026 still says Uber Aberroth. Localized killer string is unresolved. Do not collapse the two actor ids.
2. Altar name. Tunklab says Altar of Orobyss. The 2024 blog says the altar and the Harbinger's Domain. A community wiki uses Altar of Oblivion. No single official player-facing string was verified for Season 5.
3. Pinnacle Morditas. EHG treats God of Bloodshed as a harder Morditas. The extract has two display names and two ids. Keep both profiles.
4. Tomb of Vessels says "Slay Majasa" while the actors are Goddess of the Sands and Goddess of Blood.
5. Formosus the Undying and Frost Lich Formosus are one actor. Listing both as bosses would double count Blood, Frost, and Death.
6. LE Tools still labels its timeline page Season 4. Tunklab says 1.5 preview. The live client is 1.5.1. The ten pairs agree across the first two. None of the three is a 1.5.1 memory dump.
7. Flame Guard Sula in the 1.5 notes, Sulla on the Tunklab echo list.
8. Harbinger of Cruelty and Harbinger of Brutality have a zone and a false monolith spawn flag.
9. Both Lagon actors share Lagon, God of Storms. The catalog alias Lagon matches neither string.
10. Campaign Yrun's health at level 100 in one search snippet (69,363) does not match the page fetched later (160,422). The fetched page is the one used here. The snippet is not a second source of truth.

## Source ledger

Evidence class is one of: developer documentation, datamined/reference data, community report, community reproduction of developer documentation.

| Id | Publisher | Date shown | URL | Class | What it supports |
| --- | --- | --- | --- | --- | --- |
| S1 | EHG_DerrickG, Last Epoch Forums | 2026-10-02 16:58 UTC | https://forum.lastepoch.com/t/last-epoch-patch-1-5-1-notes/81911 | developer documentation | 1.5.1 is the current Season 5 patch. Pinnacle Morditas opens to Legacy and Offline. Yrun, Champion of Morditas and Demigod's Ascendance are named. |
| S2 | EHG_DerrickG, Last Epoch Forums | 2026-10-01 16:12 UTC | https://forum.lastepoch.com/t/season-5-rage-of-the-frostborn-is-now-live/81860 | developer documentation | Season 5 Rage of the Frostborn went live on Steam. |
| S3 | MickeyEHG, Last Epoch Forums | 2026-10-01 22:42 UTC | https://forum.lastepoch.com/t/last-epoch-hotfix-1-5-0-1-notes/81887 | developer documentation | 1.5.0.1 existed. No roster change. |
| S4 | EHG, lastepoch.com patch notes, announced by EHG_DerrickG | Notes announced 2026-09-25. Launch written as October 1, 2026, 11:00 CDT. Page: https://www.lastepoch.com/1-5/patchnotes | https://forum.lastepoch.com/t/season-5-rage-of-the-frostborn-patch-notes/81789 | developer documentation | Morditas chain, Circle of Frozen Blood, God of Bloodshed, 400 corruption, Shade of Morditas, Chapter 2 availability, Agile Harbinger, Shattered Road, and the boss bugfixes used as existence proof. No timeline list edit. |
| S5 | EHG_DerrickG, Last Epoch Forums | 2026-09-21 16:00 UTC. A same-day correction covers unique drops. | https://forum.lastepoch.com/t/rage-of-morditas-coming-to-last-epoch-october-1/81757 | developer documentation | Three-part chain, Anger, Demigod's Ascendance, 400 corruption, no corruption scaling, bridge toward Uber Aberroth, Pinnacle Morditas naming. |
| S6 | KRAFTON, via Games Press | Page date 02/10/2026. Body says October 2, 2026. | https://www.gamespress.com/en-GB/LAST-EPOCH-SEASON-5-RAGE-OF-THE-FROSTBORN-IS-NOW-AVAILABLE | developer documentation (publisher press release) | Corroborates the season name and that Morditas and Pinnacle Morditas shipped. Not a mechanic source. |
| S7 | Tunklab | Homepage label "Game Version: 1.5 preview". Sitemap lastmod 2026-09-30. | https://lastepoch.tunklab.com/ and https://lastepoch.tunklab.com/sitemap-bestiary.xml | datamined/reference data | Version stamp and the date of the extract. Not 1.5.1. |
| S8 | Tunklab | Same 1.5 preview extract | https://lastepoch.tunklab.com/timelines and the ten `/timeline/` pages and `/npc/` pages named above | datamined/reference data | Pairing, levels, stability, zones, internal ids, custom names, spawn flags, dungeon tiers, woven echo minimums. |
| S9 | Last Epoch Support | Updated 2026-02-12 | https://support.lastepoch.com/hc/en-us/articles/46363316626715-How-do-I-unlock-Empowered-Monoliths | developer documentation | First Harbinger unlocks Empowered timelines. First character versus alt rule. Predates Season 5. |
| S10 | Last Epoch Support | Updated 2026-02-14 | https://support.lastepoch.com/hc/en-us/articles/46363294907547-Why-aren-t-Harbingers-spawning | developer documentation | Crest checkbox and per-Harbinger corruption shown in the Forgotten Knights panel. Numbers not in the article. Predates Season 5. |
| S11 | Last Epoch Support | Updated 2026-07-13 | https://support.lastepoch.com/hc/en-us/articles/46361874426523-Shade-of-Orobyss-and-Corruption | developer documentation | Shade placement, corruption caps (50 normal, 100 start and no cap empowered). Predates Season 5. |
| S12 | Last Epoch Support | Updated 2026-07-13 | https://support.lastepoch.com/hc/en-us/articles/46361839099931-What-is-the-Monolith-of-Fate | developer documentation | Third quest echo is the timeline boss. Predates Season 5. |
| S13 | Yayifications, Last Epoch Forums | 2024-06-25, for a July 9, 2024 launch | https://forum.lastepoch.com/t/harbingers-of-ruin-whats-new/71684 | developer documentation | Original 10-Harbinger and Aberroth altar design. Historical. |
| S14 | LE Tools mirror of EHG patch notes. Posted by Vapid_Actions. | Posted 2024-07-05. Launch 2024-07-09. | https://www.lastepochtools.com/news/article/last-epoch-harbingers-of-ruin-patch-notes-71790 | community reproduction of developer documentation | Harbingers spawn on timeline-boss defeat, absorb abilities, crest toggle, Eye drops, Empowered unlock. Historical. |
| S15 | Yayifications, Last Epoch Forums | Posted 2025-08-15. Launch 2025-08-21. | https://forum.lastepoch.com/t/last-epoch-beneath-ancient-skies-patch-notes/78635 | developer documentation | Season 3 timeline level changes and first Harbinger spawning in the first completed level 90 timeline. Historical. Levels match the preview. |
| S16 | LE Tools mirror of EHG Season 2 notes. Posted by Vapid_Actions. | Posted 2025-04-11. Launch 2025-04-17. | https://www.lastepochtools.com/news/article/last-epoch-tombs-of-the-erased-patch-notes-75247 | community reproduction of developer documentation | Threshold of Eternity introduced as Uber Aberroth. Historical. The echo still exists in the 1.5 preview. |
| S17 | LE Tools | Page banner still says Season 4 Shattered Omens. No page date found. | https://www.lastepochtools.com/endgame/monolith/timelines and https://www.lastepochtools.com/endgame/monolith/woven-echoes/threshold-of-eternity | datamined/reference data | Agrees on the ten pairs. Threshold minimum corruption 500 and boss label Aberroth. Stale banner. |

## Open questions

1. What killer string does 1.5.1 localize for Herald of Oblivion, for the Lagon tentacle actor that reuses the boss name, and for the invisible Aberroth add?
2. What are the current corruption minimums for each of the ten timeline Harbingers?
3. Does 1.5.1 still require all ten Harbingers before Aberroth, and does the first character still need three level 90 timelines?
4. Is there a campaign Heorot boss actor and a campaign Rahyeh boss actor under ids this lane did not open?
5. Can Shade of Morditas be the attacker on a death, or is it only a follower?
6. Do Bhuldar, Herkir, and Logi share one kit?
7. What is the player-facing name of the Aberroth altar in 1.5.1?
8. Did any timeline level, stability value, or Harbinger pairing change after the 2026-09-30 preview? The 1.5 and 1.5.1 notes do not say so, and that is not the same as a client check.
