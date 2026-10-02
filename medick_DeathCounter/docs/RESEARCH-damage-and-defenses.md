# Last Epoch: damage and defense cheatsheet (for DeathCounter advice text)

Researched 2026-10-01. Live game at that date: patch 1.4.x (Season 4, Shattered Omens, launched 2026-03-26).
Season 5 (Rage of the Frostborn, patch 1.5) launches 2026-10-01, so re-check anything marked 1.5.

## How to read this file

* Each fact carries a source tag. Source list is at the bottom (S1, S2, ...).
* Network note: direct page fetches of lastepoch.com, forum.lastepoch.com, support.lastepoch.com,
  maxroll.gg, lastepochtools.com, fandom and reddit were blocked by the research sandbox proxy.
  Facts below come from search engine excerpts of those pages, not full reads. Treat exact numbers
  as "confirmed by excerpt" and verify in the in game Game Guide (Help menu) before hard coding.
* **UNCONFIRMED** marks anything I could not confirm from at least one source excerpt.
* Patch tags: "1.x" means stated as current mechanic with no version given; otherwise the version is named.

---

## 1. Damage types

Seven damage types: Physical, Fire, Cold, Lightning, Necrotic, Void, Poison. Poison is a full damage
type with its own resistance (S6, S13). Elemental = Fire, Cold, Lightning.

| Type | Visual cue | Typical sources (examples) |
|---|---|---|
| Physical | grey/white | Most melee hits, Bleed, Lagon claw strikes (S16), Majasa melee also applies Armor Shred (S16) |
| Fire | orange | Ignite, Spreading Flames, Imperial Pyromancers (S10, 1.4.3 note) |
| Cold | light blue | Frostbite, Chill, Freeze; Lagon cold waves (S16); Froststeel Nemesis (S12, 1.1.3); Season 5 Frostborn theme (UNCONFIRMED specifics) |
| Lightning | yellow | Electrify, Shock; Lagon, Majasa Stomp (S16); Stormsteel Nemesis (S12) |
| Necrotic | cyan | Damned, Anguish; Imperial Era undead: Soul Cages, Immortal Overseers, Immortal Eyes; Void Priests also deal necrotic (S17) |
| Void | purple | Time Rot, Doom, Abyssal Decay; Void cultists / Shadow Zealots / Void Priests (S17); Julra uses void plus cold and lightning (S16) |
| Poison | green | Poison, Plague; Poison Nemesis pools and projectiles (S12, nerfed 1.1.3) |

Harbingers (added 1.1, Harbingers of Ruin) come in Agile and Brute variants and copy abilities from the
Timeline boss that spawned them, so their damage type depends on the Timeline (S18). Per Harbinger
damage type breakdown: **UNCONFIRMED**.

---

## 2. Resistances

* Cap: **75%** for every type, including Physical and Poison (S2, S3, S11). 1.x.
* Resistance shred applies BEFORE the cap; penetration applies AFTER the cap and can push you negative
  (S3, S11). Example: 30% lightning pen vs 20% lightning res = minus 10% (S3).
* **Area level penetration against the player**: multiple community guides state enemies gain 1%
  penetration per area level up to 75% (S3, S11, S19). The official Season 4 / 1.4 notes that I could
  see did not mention removal, but I also could not read the current Game Guide text.
  Status: **UNCONFIRMED for 1.4/1.5** (it is old, widely repeated, and may be outdated). Important: if it
  is still live, a capped 75% in an area level 75+ zone is effectively 0%, and an uncapped resistance is
  negative. Either way "capped" is still the baseline, not a bonus.
* Shred on the player: resistance shred and armor shred are 5% per stack on normal monsters but
  **2% per stack on players and bosses**, max 20 stacks, 4 s per stack (S11). The source excerpt states
  the max on players is 20% even though 20 x 2% = 40%, so the true maximum is **UNCONFIRMED (20% or 40%)**.
* Poison stacks shred **poison resistance** on the target: 5% per stack on monsters, **2% on players and
  bosses**, only the first 30 stacks count (S6). A 2023 bug report said it was 3% vs players (S6b, fixed
  status UNCONFIRMED).
* Empowered Monolith: enemies are set to level 100 and echo modifier values are larger (S20).
  "Reduced player resistances" is listed as a possible echo modifier (S20). Exact values per corruption:
  **UNCONFIRMED**. Corruption itself adds monster health and damage, not a direct player resistance
  penalty (S19). A flat "per corruption" resistance penalty: **not found, UNCONFIRMED**.
* Physical resistance: same 75% cap; rarer on gear; Sentinel Battle Hardened gives +6% per point in 1.5
  and a 7 point bonus of 1% less physical damage taken per 5% overcapped physical res, up to 10% (S7, 1.5).
* Resistances DO reduce damage over time; they are the main DoT defense along with ward, endurance and
  "less damage over time taken" (S1, S12b).

---

## 3. Armor

* Reduces damage from **hits only**. Does **not** reduce damage over time by default (S1, S2, S12b). 1.x.
  Exception: Eternal Gauntlets implicit "11 to 24% of Armor Mitigation also applies to Damage over Time" (S1).
* Applies to ALL hit damage types, not just physical. Against non physical hits it is **70% as effective**
  (S2, S4). Mitigation cap: **85% physical, 59.5% (= 85% x 0.7) for other types** (S4).
* Formula: mitigation depends on armor and **area level** (same armor = less mitigation at higher level),
  capped at 85% (S2). Exact formula text: **UNCONFIRMED** (one search excerpt claimed
  armor / (armor + max HP), which conflicts with every other source; do not use).
* Armor shred on the player: see section 2 (2% per stack vs players is stated for armor shred too, S11;
  whether armor shred is a % or flat value vs players: **UNCONFIRMED**).

---

## 4. Other defense layers

| Layer | How it works | Source |
|---|---|---|
| Health | Base pool. Community floor for Empowered Monolith ~1500+, many say much more (S14) | S14 |
| Endurance | Base 20% endurance and threshold 20% of max HP. Cap 60%. Applies to the part of ANY damage that lands below the threshold, even if the hit started above it (100 dmg hit, 50 below threshold: endurance on those 50). Threshold has no cap. Does not apply to damage taken by ward | S5, S8 |
| Endurance Threshold | 1.2: affix only rolls on belts | S9 |
| Ward | Absorbs damage before health. Decays: (0.2 x ward + 0.00005 x ward^2) / (1 + 0.5 x retention). Ward Decay Threshold: no decay below it. Intelligence gives ward retention (4% per point per one excerpt; **UNCONFIRMED** value) | S15 |
| Ward vs DoT | Ward absorbs damage generally; explicit statement that it absorbs DoT: **UNCONFIRMED** (believed yes) | S8 |
| Dodge | Dodge rating converts to chance, lower at higher area level; cap 85%; rule of thumb rating = 10 x area level gives ~50%. Avoids the hit AND its ailments. Cannot dodge DoT | S21, S8 |
| Block | Chance to block hits; blocked hits mitigated by Block Effectiveness, scaled by area level, cap 85%. Hits only | S21 |
| Glancing Blow | Still exists: hit deals 35% less damage, chance capped at 100%, hits only. Removed from most items and skills | S22 |
| Crit Avoidance | Chance to turn an enemy crit into a normal hit, rolled after enemy crit chance. Cap 100%: at 100% you are never crit | S23, S24 |
| Enemy crits | Enemy crit multiplier is 200% (double damage). "Reduced bonus damage taken from critical strikes" cuts the bonus part, cap 100%. 1.5: Iron Reflexes 4% per point | S24, S7 |
| Stun avoidance | Non player hits need > 5% of your max HP to have a chance to stun; each 100 stun avoidance raises that by 1%. Players have 250 + 5 per level inherent | S25 |
| Freeze on player | Freeze chance is lower vs targets with more max HP and current ward; Frostbite and chill stacks raise it; base duration 1.2 s | S26 |
| Leech / regen / potions | Leech is applied over time (more leech = faster), regen per second, potion slots on belts. Exact potion heal and leech caps: **UNCONFIRMED** | S27 |
| Less / reduced damage taken | Multiplicative layers from passives and skills (example 1.4: Arcane Shield 4% less damage taken per stack, up to 4). "Reduced damage over time taken" affixes and statuses exist (Crimson Shroud 5%) | S10, S12b |

---

## 5. Ailments that can hit the player

DoT = damage over time (resistances, ward, endurance, "less DoT taken" reduce it; armor, dodge, block,
glancing blow, crit avoidance do not). Ailments delivered by a hit are prevented if the hit is dodged (S8).

| Ailment | Type | Kind | Stacking | Effect | Counters |
|---|---|---|---|---|---|
| Bleed | Physical | DoT | unlimited stacks, each timed | physical DoT | physical res, less DoT taken, endurance; armor does NOT help (S12b) |
| Ignite | Fire | DoT | unlimited | fire DoT | fire res, ward, DoT reduction (S12b) |
| Poison | Poison | DoT + debuff | unlimited | poison DoT; each stack minus 2% poison res on players (first 30 stacks) | overcap poison res, burst cleanse, kill or avoid poison sources (S6) |
| Plague | Poison | DoT | 1 stack, spreads | 150 base over 4 s | poison res (S13) |
| Spreading Flames | Fire | DoT | 1 stack, spreads after 0.6 s | 140 base over 4 s | fire res (S13) |
| Frostbite | Cold | DoT + debuff | stacks | cold DoT and raises chance to be frozen (20% per stack per one excerpt) | cold res, higher max HP and ward (freeze), dodge the hits (S13b, S26) |
| Electrify | Lightning | DoT | no limit, 2.5 s | lightning DoT, 44 base | lightning res (S28) |
| Time Rot | Void | DoT + debuff | up to 12, 3 s | void DoT, +5% stun duration received per stack; some sources also say reduced attack/cast speed (UNCONFIRMED) | void res, stun avoidance (S28) |
| Doom | Void | DoT + debuff | up to 4, 4 s | void DoT 400 base, +4% melee damage taken per stack | void res, kill or avoid the source (S28) |
| Damned | Necrotic | DoT + debuff | no limit, 2.5 s | necrotic DoT 35 base, minus 20% health regen | necrotic res, leech or ward instead of regen (S28) |
| Anguish | Necrotic | DoT | UNCONFIRMED | necrotic DoT | necrotic res (S17) |
| Abyssal Decay | Void | DoT | 1 stack | 120 void over 6 s | void res (S13) |
| Shock | Lightning | debuff | max 20 | per stack: minus 5% lightning res and +10% chance to be stunned, 4 s; **60% less effect vs players and bosses** | lightning res overcap, stun avoidance (S6c) |
| Chill | Cold | debuff | max 3 | minus 12% move, attack, cast speed per stack; raises freeze chance | cold res does not stop it (UNCONFIRMED); freedom/ailment immunity effects, dodge (S29) |
| Freeze | Cold | CC | n/a | cannot move or act, base 1.2 s | more max HP and current ward lowers chance; freeze immunity effects (S26) |
| Slow | none | debuff | max 3 | minus 20% move speed per stack | movement skills, slow immunity (S29) |
| Stun | none | CC | n/a | interrupt; big hits (> 5% max HP) can stun | stun avoidance, max HP (S25) |
| Blind | none | debuff | n/a | blinded target cannot crit (vs player: you cannot crit; UNCONFIRMED that enemies apply it) | n/a (S29) |
| Frailty | none | debuff | max 3 | target deals 6% less damage per stack (offensive debuff, not damage taken) | n/a (S29) |
| Armor Shred | Physical | debuff | max 20, 4 s | reduces armor; 2% per stack vs players | more armor, kill shredders (Majasa) (S11, S16) |
| Resistance shreds (fire, cold, lightning, necrotic, void, poison, physical) | matching | debuff | max 20, 4 s | minus 2% res per stack on players, applied before cap | overcap that resistance (S11) |

---

## 6. Common death causes and survival checklist

**By content (community consensus, S14, S16, S19, S20):**

* Monolith echoes (normal): uncapped resistance vs a single type rare pack; DoT pools (poison, cold,
  necrotic) stood in; Nemesis DoT abilities (heavily nerfed in 1.1.3, S12).
* Empowered Monolith / high corruption: one shots from crits (crit avoidance below 100%), low HP pool
  (1000 HP was called the main problem even with capped res), echo modifiers that add enemy damage or
  reduce player res, level 100 enemies making armor/dodge/block weaker (area level scaling).
* Arena: wave stacking, no sustain (leech/regen) for long fights. Detail: **UNCONFIRMED**.
* Dungeons: boss telegraphed slams; Lagon cold waves and lightning with stuns (S16). Detail on other
  dungeon bosses: **UNCONFIRMED**.
* Pinnacle bosses (Aberroth, Uber Aberroth, Season 4 pinnacle): telegraphed big hits, "most kills come
  from timed or telegraphed attacks" (S14). Per boss damage type: **UNCONFIRMED**.
* Harbingers: copy Timeline boss abilities; Agile vs Brute archetype (S18).

**Standard survival checklist:**

1. All seven resistances at 75% (including poison, necrotic, void; physical as high as you can).
2. 100% critical strike avoidance (non negotiable for Empowered).
3. Health pool: 1500+ is a floor, many recommend far more for corruption pushing.
4. A sustain source: leech, regen, healing or ward generation.
5. At least one hit layer: armor, dodge, block, or glancing blow (rogue).
6. Endurance toward 60% and a decent threshold.
7. A movement skill off cooldown for telegraphs.
8. Overcap the resistance that the current Timeline or modifier shreds or penetrates.

---

## 7. Quick advice table (top 3 per damage type and ailment)

| Threat | Advice 1 | Advice 2 | Advice 3 |
|---|---|---|---|
| Physical hit | Stack armor (strongest vs physical, cap 85%) | Raise physical resistance | Add block or dodge |
| Fire hit | Cap fire resistance at 75% | Add armor (70% effective) | Raise max health or ward |
| Cold hit | Cap cold resistance at 75% | Raise max health and ward to resist freeze | Use dodge to avoid the hit and its chill |
| Lightning hit | Cap lightning resistance at 75% | Overcap it to absorb shock res loss | Add stun avoidance |
| Necrotic hit | Cap necrotic resistance at 75% | Add armor | Raise health pool |
| Void hit | Cap void resistance at 75% | Add armor | Raise health pool |
| Poison hit | Cap poison resistance at 75% | Overcap poison res to absorb poison stack shred | Add armor |
| Big crit spike | Get critical strike avoidance to 100% | Add reduced bonus damage taken from crits | Raise max health |
| Bleed | Raise physical resistance (armor does not help) | Get less damage over time taken | Raise endurance |
| Ignite | Cap fire resistance | Build ward | Leave the fire ground |
| Poison | Overcap poison resistance | Kill or avoid poison casters fast | Add leech or regen |
| Plague | Cap poison resistance | Spread away from allies or minions | Add sustain |
| Spreading Flames | Cap fire resistance | Do not stand near burning enemies | Add sustain |
| Frostbite | Cap cold resistance | Raise max health and ward to lower freeze chance | Dodge the applying hits |
| Electrify | Cap lightning resistance | Build ward | Add sustain |
| Time Rot | Cap void resistance | Add stun avoidance | Leave the void zone |
| Doom | Cap void resistance | Avoid melee range of the source | Raise health pool |
| Damned | Cap necrotic resistance | Use leech or ward instead of regen | Kill the applier |
| Abyssal Decay | Cap void resistance | Build ward | Add sustain |
| Shock | Overcap lightning resistance | Add stun avoidance | Kill shockers first |
| Chill | Dodge the applying hits | Keep a movement skill ready | Get chill or ailment immunity |
| Freeze | Raise max health | Build ward | Get freeze immunity |
| Slow | Keep a movement skill ready | Get slow immunity | Dodge the applying hits |
| Stun | Add stun avoidance | Raise max health | Avoid telegraphed slams |
| Armor Shred | Stack more armor | Kill shredders first | Add a non armor layer (dodge, block) |
| Resistance shred | Overcap the shredded resistance | Kill the shredder first | Add ward or health |

---

## Sources

* S1 Eternal Gauntlets / armor vs DoT: https://forum.lastepoch.com/t/armor-mitigation-damage-over-time/71210 (1.x)
* S2 Official support article (armor, 70%, 85%, area level): https://support.lastepoch.com/hc/en-us/articles/46361891210651/ (1.x)
* S3 Steam discussion on resistance and penetration: https://steamcommunity.com/app/899770/discussions/0/6063574513308070211/
* S4 Maxroll Defenses Explained (85% / 59.5%): https://maxroll.gg/last-epoch/resources/defenses-explained
* S5 Official support endurance article: https://support.lastepoch.com/hc/en-us/articles/46361855013787/
* S6 Poison: https://lastepoch.fandom.com/wiki/Poison and https://maxroll.gg/last-epoch/resources/ailments-explained
* S6b Poison 3% vs players bug: https://forum.lastepoch.com/t/poison-reduces-resistances-by-3-instead-of-2-against-players-and-possibly-bosses/48624
* S6c Shock: https://lastepoch.fandom.com/wiki/Shock
* S7 Season 5 patch notes (1.5): https://lastepoch.com/1-5/patchnotes/ and https://maxroll.gg/last-epoch/news/season-5-patch-notes
* S8 Maxroll Defenses for Beginners: https://maxroll.gg/last-epoch/getting-started/defenses-for-beginners ; ward vs endurance: https://forum.lastepoch.com/t/ward-vs-endurance-vs-life-leech/70545
* S9 Endurance threshold belts only (1.2): https://forum.lastepoch.com/t/where-did-endurance-threshold-go-patch-1-2/75279 ; https://www.icy-veins.com/last-epoch/latest-patch-update
* S10 Season 4 / 1.4 notes: https://forum.lastepoch.com/t/last-epoch-shattered-omens-patch-notes/80571 ; 1.4.3: https://maxroll.gg/last-epoch/news/last-epoch-patch-1-4-3-notes
* S11 Shred vs players: https://forum.lastepoch.com/t/game-guide-resistances-section/25531 ; https://forum.lastepoch.com/t/resistance-shred-and-penetraiton-interaction/75411
* S12 1.1.3 enemy DoT rebalance and death screen: https://maxroll.gg/last-epoch/news/last-epoch-1-1-3-patch-notes
* S12b DoT only reduced by resistances and DoT modifiers: https://lastepoch.fandom.com/wiki/Statuses
* S13 Plague, Spreading Flames, Abyssal Decay: https://lastepoch.tunklab.com/ailment/spreadingflames ; https://www.icy-veins.com/last-epoch/glossary-of-terms
* S13b Frostbite: https://primagames.com/gaming/last-epoch-all-ailments-explained
* S14 Empowered one shots: https://forum.lastepoch.com/t/getting-one-shot-in-empowered-monoliths/73161 ; https://forum.lastepoch.com/t/one-shot-by-bosses-what-i-am-doing-wrong/72870
* S15 Ward decay: https://maxroll.gg/last-epoch/resources/defenses-explained ; https://support.lastepoch.com/hc/en-us/articles/46361876030235/
* S16 Bosses: https://maxroll.gg/last-epoch/monolith/lagon-ending-the-storm-boss-guide ; https://maxroll.gg/last-epoch/resources/majasa-chapter-9-boss-guide
* S17 Enemy damage types: https://forum.lastepoch.com/t/how-do-i-identify-what-damage-type-an-enemy-does/57334
* S18 Harbingers: https://maxroll.gg/last-epoch/monolith/harbinger-of-fear-boss-guide
* S19 Corruption: https://timesaver.gg/blog/last-epoch-corruption-guide ; https://maxroll.gg/last-epoch/monolith/advanced-strategies
* S20 Empowered Monolith: https://www.icy-veins.com/last-epoch/empowered-monolith-of-fate-overview-for-last-epoch
* S21 Dodge and block: https://support.lastepoch.com/hc/en-us/articles/46361899172379/ ; https://forum.lastepoch.com/t/dodge-chance-formula/36954
* S22 Glancing Blow: https://forum.lastepoch.com/t/glancing-blow-questions/29288
* S23 Crit avoidance: https://lastepoch.fandom.com/wiki/Critical_Strike_Avoidance
* S24 Enemy crit 200%: https://support.lastepoch.com/hc/en-us/articles/46361891709211/ ; https://steamcommunity.com/app/899770/discussions/0/4361246746315670437
* S25 Stun: https://support.lastepoch.com/hc/en-us/articles/46361891772443-Stun
* S26 Freeze: https://support.lastepoch.com/hc/en-us/articles/46361855307035-Freeze
* S27 Leech: https://forum.lastepoch.com/t/what-is-the-internal-resolution-for-dot-ailments-and-leech/73453
* S28 Time Rot, Doom, Damned, Electrify: https://www.icy-veins.com/last-epoch/ailments-and-debuffs
* S29 Chill, Slow, Frailty, Blind: https://maxroll.gg/last-epoch/resources/ailments-explained
