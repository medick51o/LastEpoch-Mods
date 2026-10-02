# Last Epoch Mechanics Reference (Season 5 / Patch 1.1.3+)

This document compiles the verified mechanics behavior for Last Epoch as of Season 5 (Patch 1.1.3+). It is intended to guide the advice logic for the `medick_DeathCounter` mod.

## Mechanics Reference Table

| Mechanic | Current Behavior | Units / Caps / Formula | Exact Primary-Source URL | Source Date | Patch it Applies To | Confidence | Implication for Advice |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Resistances** | Caps at 75%. Overcap only protects against shred/shock/Marked for Death. | 75% hard cap. | https://support.lastepoch.com/hc/en-us/articles/46363284011419 | 2026-02-12 (updated) | 1.0+ | Verified Current | If player died to Elemental/Void/Necrotic/Poison, check if capped. If not 75%, advise capping. Overcapping is only advised if killed by an enemy applying Shred. |
| **Area Penetration** | Enemies penetrate resistances by 1% per area level, up to 75%. This applies *after* the player's 75% cap. | 1% per lvl, max 75% at area lvl 75+. | https://forum.lastepoch.com/t/penetration-vs-resistance-shred/61527 | 2023-07 | 0.9 / 1.0+ | Verified Current | Player resistances will effectively be 0% in high-level zones if capped. The advice should not tell players to overcap to counter Area Penetration, because it doesn't work. |
| **Resistance Shred** | Ailment reducing resistance. Applies *before* resistance cap (unlike Penetration). 5% per stack on monsters, 2% on bosses/players. | Caps at 10 stacks, lasts 4s. | https://maxroll.gg/last-epoch/resources/damage-explained | 2024-02 | 1.0+ | Verified Current | If killed while debuffed by Shred, overcapped resistances would have mitigated it. Advice can mention overcapping or cleanse. |
| **Armor** | Mitigates damage from *hits only* (not DoT). Physical is fully mitigated; non-physical is 70% effective. Diminishing returns formula based on Area Level. | Hard cap of 85% mitigation for Physical, 59.5% for Non-Physical. | https://www.lastepochtools.com/guide/section/armor | 2024 | 1.0+ | Verified Current | If died to a physical hit, advise Armor. If died to non-physical hit, advise Armor but note 70% effectiveness. If died to DoT, DO NOT advise Armor. |
| **Health & Ward** | Ward absorbs damage before health. Ward decays exponentially based on retention. | `Decay = (0.4 * Ward) / (1 + 0.5 * Ward Retention)`. | https://lastepoch.fandom.com/wiki/Ward | 2024 | 1.0+ | Verified Current | If died with high Ward decay, might advise Ward Retention. Both work against hits and DoTs. |
| **Endurance** | Mitigates damage applied to Health below the Endurance Threshold. *Does not apply to Ward.* | Cap 60% mitigation. Threshold defaults to 20% max health. | https://www.iggm.com/news/last-epoch-1-0-how-does-endurance-defense-mechanic-work | 2024-02 | 1.0+ | Verified Current | If player is a Ward build, DO NOT advise Endurance. If Health build died, check if Endurance is capped at 60%. |
| **Crit Avoidance** | Chance to turn a critical strike into a normal hit. Rolled after enemy crit chance. | Cap 100%. | https://support.lastepoch.com/hc/en-us/articles/46361891709211-Critical-Strikes | 2026-02-13 (updated) | 1.0+ | Verified Current | If death was a Crit (`wasCrit=true`), check Crit Avoidance. If not 100%, strongly advise capping it. |
| **Reduced Crit Damage** | Reduces the *bonus* damage from a crit. Base enemy crit multi is 200%. | Cap 100% (reduces the 200% multi down to 100% normal damage). | https://forum.lastepoch.com/t/critical-strike-avoidance/67777 | 2024-03 | 1.0+ | Verified Current | Alternative to Crit Avoidance. If player died to a crit and doesn't have 100% reduction, advise getting either CA or this. |
| **Block / Dodge / GB** | Mitigate Hits. Cannot mitigate DoTs. Dodge avoids 100% dmg. Glancing Blow (GB) reduces 35%. Block % depends on Effectiveness. | Dodge Cap: 85%. GB Cap: 100%. Block Cap: 85%. | https://maxroll.gg/last-epoch/resources/defenses-explained | 2024 | 1.0+ | Verified Current | If died to a hit, these are valid suggestions. If died to DoT, they are useless. |
| **Ailments (DoT)** | Ignite, Bleed, Poison, etc. Poison inherently shreds Poison Resistance for the first 30 stacks. | Unlimited stacks. Poison is 28 base dmg over 3s. | https://onlyfarms.gg/wiki/last-epoch/poison-stacks-limit-guide | 2024 | 1.0+ | Verified Current | If died to Poison DoT, recommend Poison Resistance and Cleanse. Armor/Block will not help. |
| **Mitigation Order** | Hit logic: Dodge -> Block/GB rolls -> Resistances / Armor / Endurance apply multiplicatively. | Multiplicative | https://forum.lastepoch.com/t/how-damage-mitigation-works/59952 | 2023-06 | 0.9 / 1.0+ | Verified Current | Order doesn't mathematically change the EHP calculation since they are multiplicative layers. |
| **Death Report Fields** | Death screen UI shows killing blow damage, overkill, and if it was a crit. | `damage`, `overkillDamage` in `ReceiveDetailedDeathInfo`. | https://forum.lastepoch.com/t/last-epoch-patch-1-1-3-notes/73083 | 2024-07 (Patch 1.1.3) | 1.1.3+ | Verified Current | The actual properties exposed via the Season 5 API map directly to these UI features. We must derive the pre-mitigation damage carefully if advising based on hit size. |


## Detailed Mechanics Breakdown

### Resistances & Area Penetration
- **Resistances** cap at 75%. Overcapping (having more than 75%) provides absolutely no benefit against standard enemy hits, because it only protects against resistance reduction effects like Resistance Shred or Shock. (Source: [LE Support](https://support.lastepoch.com/hc/en-us/articles/46363284011419-If-75-resistance-is-the-max-and-the-enemies-get-penetration-per-level-up-to-75-do-I-need-150-resistance))
- **Area Penetration**: Monsters gain 1% resistance penetration per area level, up to a maximum of 75% at Area Level 75+. This is applied **after** the resistance cap. Therefore, a player with 75% resistance in a level 100 Monolith will functionally have 0% resistance. Overcapping does not counter Area Penetration. (Source: [LE Forums](https://forum.lastepoch.com/t/penetration-vs-resistance-shred/61527))
- **Resistance Shred**: A debuff that reduces resistance by 5% per stack against normal enemies, or 2% against bosses and players. It caps at 10 stacks and lasts 4 seconds. Because Shred applies *before* the 75% resistance cap, overcapped resistances will successfully absorb Shred. (Source: [Maxroll](https://maxroll.gg/last-epoch/resources/damage-explained))

### Armor
- **Hits Only**: Armor mitigates damage taken from all hits. **It has no effect against Damage over Time (DoT).**
- **Effectiveness**: Armor is 100% effective against Physical damage and 70% effective against non-Physical damage (Void, Necrotic, Elemental, Poison hits).
- **Caps and Scaling**: The exact damage reduction is scaled against the Area Level (similar to penetration) and suffers from diminishing returns. It cannot exceed 85% mitigation for Physical damage, meaning the hard cap for non-Physical damage mitigation from Armor is 59.5%. (Source: [LE Game Guide - Armor](https://www.lastepochtools.com/guide/section/armor))

### Health, Ward, and Endurance
- **Ward**: Acts as an energy shield that absorbs incoming damage (from hits and DoTs) before Health. Ward decays exponentially based on a player's Ward Retention. The decay formula is: `Ward Decay Rate = (0.4 * Current Ward) / (1 + 0.5 * Ward Retention)`. (Source: [LE Wiki](https://lastepoch.fandom.com/wiki/Ward))
- **Endurance**: Reduces damage taken to Health when Health falls below the Endurance Threshold. **Endurance does not mitigate damage dealt to Ward.** Therefore, low-health / high-ward builds gain zero defensive benefit from Endurance. Endurance caps at 60% less damage taken. (Source: [IGGM - Endurance](https://www.iggm.com/news/last-epoch-1-0-how-does-endurance-defense-mechanic-work))

### Critical Strikes Mitigation
- **Base Crit**: Enemies have a base critical strike multiplier of 200% (dealing double damage).
- **Critical Strike Avoidance (CSA)**: Provides a % chance to turn an incoming critical strike into a normal hit. Capping this at 100% makes the player immune to enemy crits entirely.
- **Reduced Bonus Damage Taken from Crits**: Rather than avoiding the crit, this stat reduces the *bonus* multiplier. Since the bonus portion is 100% (on top of the 100% base), achieving 100% Reduced Bonus Damage makes critical strikes deal 100% damage, effectively acting the same as 100% Crit Avoidance. (Source: [LE Support - Crits](https://support.lastepoch.com/hc/en-us/articles/46361891709211-Critical-Strikes))

### Hit Avoidance: Block, Dodge, and Glancing Blow
- **Dodge**: A binary avoidance mechanic that completely negates a hit (0 damage taken). Dodge chance caps at 85% with diminishing returns.
- **Block**: Grants a chance to reduce damage from a hit. If a block occurs, the damage mitigated is determined by Block Effectiveness, capped at 85%.
- **Glancing Blow**: Specific to the Rogue class/passives, grants a chance to receive a Glancing Blow, which reduces the damage of the hit by a flat 35%. Caps at 100% chance.
- **NOTE**: None of these mechanics apply to Damage over Time (DoT) effects. (Source: [Maxroll - Defenses](https://maxroll.gg/last-epoch/resources/defenses-explained))

### Ailments & DoTs
- **Poison**: A DoT ailment dealing 28 base Poison damage over 3 seconds. The stack limit is theoretically infinite. The first 30 stacks inherently shred Poison Resistance by 5% per stack (2% vs players), meaning Poison naturally escalates its own damage if not cleansed. (Source: [OnlyFarms - Poison](https://onlyfarms.gg/wiki/last-epoch/poison-stacks-limit-guide))
- **Armor/Block Bypass**: Ailments do not "hit". Therefore, defensive layers like Armor, Block, Dodge, and Glancing Blow provide exactly zero mitigation against them. Only Health, Ward, Resistances, and Endurance (if hitting health) mitigate DoT damage.

### Mitigation Order
The game processes hits in the following sequence:
1. **Dodge** (Did the hit land?)
2. **Block / Glancing Blow** (Did the hit get partially mitigated at the source?)
3. **Multiplicative Reductions**: Resistances, Block Effectiveness damage reduction, Armor damage reduction, and Endurance (if below threshold) all apply as multiplicative layers. Because they are multiplicative, the mathematical order of operations between them does not matter (e.g., `Hit * Resistance_Mod * Armor_Mod = Hit * Armor_Mod * Resistance_Mod`). (Source: [LE Forums - Mitigation Order](https://forum.lastepoch.com/t/how-damage-mitigation-works/59952))

### Death Report Fields API
In Patch 1.1.3, Last Epoch added a Death Screen UI that details:
- Damage dealt by the killing blow.
- Overkill damage (damage dealt beyond what was necessary to reach 0 HP).
- Whether the killing blow was a crit.
These map perfectly to the new `PlayerActorSync.ReceiveDetailedDeathInfo` API fields (`damage`, `overkillDamage`, `wasCrit`). (Source: [Patch 1.1.3 Notes](https://forum.lastepoch.com/t/last-epoch-patch-1-1-3-notes/73083))

---

## Conflicting or Outdated Sources
- **Resistance Shred Stacks**: Older beta patches (e.g. 0.8.4) had a maximum of 20 stacks for Resistance Shred. The current cap is 10 stacks. Do not use older guides suggesting 20 stacks.
- **Glancing Blow**: Very old beta guides treated Glancing Blow as a universal stat that reduced damage by 50%. It was reworked to 35% and made primarily accessible to the Rogue via Dusk/Silver Shrouds.
- **"What Killed Me" Misconceptions**: The game only reports the final killing blow. A player dying to 10 stacks of Poison taking 90% of their HP, followed by a 100-damage physical attack, will see the physical attack as their death cause. Advice logic must be careful not to overstate the importance of the final hit if `damage` is extremely low compared to maximum HP.

## What Must Be Measured In-Game
While we have the API parameters for the `medick_DeathCounter` mod, several assumptions require in-game testing to ensure the advice is calculated correctly:

1. **Pre-Mitigation vs Post-Mitigation Damage:**
   - *Test Procedure:* Unequip all gear to minimize Ward/Endurance. Go to a low-level zone, let an enemy hit you once for a recorded amount, then let them kill you. Observe the `damage` field in `ReceiveDetailedDeathInfo`. 
   - *Question:* Does `damage` equal the final HP lost (Post-Mitigation), or does it equal the incoming damage before Armor/Resistances (Pre-Mitigation)? Given the forum math regarding Ward and overkill, it is highly likely `damage` represents the **post-mitigation hit** that actually subtracted from Health/Ward.
2. **Overkill & Ward Calculation:**
   - *Test Procedure:* Generate 1000 Ward with 100 HP. Let an enemy one-shot you for 2000 damage. 
   - *Question:* Is `overkillDamage` = 2000 - (1000 + 100) = 900? We must verify if the Ward buffer is included in the overkill math.
3. **secondaryDamageType & irregularDamageSourceID Meaning:**
   - *Test Procedure:* Die to a ground effect (e.g., Lagon's void puddle), a generic DoT (Bleed/Poison), and a dual-element attack.
   - *Question:* Does `irregularDamageSourceID` map to environmental hazards or DoTs? Does `secondaryDamageType` get populated on dual-element skills, or is it reserved for ailments?
4. **Endurance & Ward Edge Case:**
   - *Test Procedure:* Set Endurance to 60% with a high threshold. Generate massive Ward. Die to a single large hit.
   - *Question:* Is the damage to Ward mitigated by Endurance if the hit spans across Ward and dips into Health? (Mechanically, it shouldn't be, but the final `damage` logged might behave weirdly at the boundary).