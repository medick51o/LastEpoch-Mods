# Lane 6 starter profile audit

Audit of the five profiles in `BossCatalog` on the v0.1.2 baseline. This lane recommends deletions. It does not add roster rows, damage types, or replacement tips, and it did not edit `BossCatalog.cs`. A later fact-check agent still needs to read the cited pages. This pass compared the catalog strings with `docs/CODEX-DEATH-FULL-REQUEST-REVIEW.md` and `research/advice/CODEX-ZONE-BOSS-RESEARCH.md`. Where those disagree, the disagreement is flagged and the starter text is not padded to match the older note.

Evidence classes used here match the schema: developer documentation, datamined reference, tactical inference, or unknown. Confidence for live Season 5 use is unknown on every claim. The catalog's own reviewed line says Season 5 encounters still need live verification.

## Already kept out, and should stay out

The current profiles already omit claims that the older zone note included. Do not put them back.

- Lagon community element table (lightning, cold, physical) and the eye, beam, and phase-three tip.
- Heorot cannot be frozen. That was labeled community evidence in the older note.
- Heorot cold damage inferred from a blessing note.
- Emperor of Corpses blood pools and any ward bypass. The mechanics fact-check says the official ward page does not state that exception.
- The 1.4.2 Soul Bomb indicator failure as if it were current behavior.
- Julra void rifts or a room-wide attack typed from a community thread.
- Matching Harbinger of Hatred, Void Rahyeh, or Rahyeh by substring.

Empty `DamageTypes` on Lagon, Emperor of Corpses, Heorot, and Harbinger of Hatred means unknown, not a claim that those fights have no element. The schema projection stores that as damage coverage status unknown with elements null.

## Lagon

Alias in code: `Lagon` only. Id `lagon`. Parent, variants, locations, abilities, ailments, and browse groups are unknown. Damage coverage is unknown.

Keep the mechanics sentence that names Moon Blast, Tidal Wave, Lightning Blast, and a melee attack, as a claim tied to the catalog's EHG 0.9 forum label (March 2023). Keep the damage-evidence sentence: attack names are not a damage table, and the recorded death's element is the one to use. Keep the forum source. The projection classes that URL as developer documentation because the host is `forum.lastepoch.com`. This lane did not re-read the post, so confidence stays unknown.

Delete both tips.

- "keep a clear route away from the next Moon Blast or Lightning Blast telegraph. Stop attacking to move before it resolves" adds a telegraph and a stop-attacking instruction the mechanics line does not contain.
- "an attack name is not proof of its element" repeats the damage-evidence sentence. It is not a fight plan.
- "reposition for each Tidal Wave rather than chasing damage" is generic movement attached to a name.
- "Reassess the recorded death's resistance gap before adding more health" belongs on Last death. It is not a Lagon mechanic, and it duplicates gear advice.

Delete the sentence "Campaign and Monolith variants may differ." The older note says both a campaign encounter and a monolith encounter exist, but this sentence names neither id nor difference. Leave variants unknown until lane 1 records two encounters. Do not split `lagon` in this lane.

## Emperor of Corpses

Alias: `Emperor of Corpses` only. Do not match Emperor, Emperor's Remains, or a summoned add. Damage coverage unknown. Parent and variants unknown.

Keep the Soul Bomb name, the 0.9l claim that damage decreases with distance from the center, and the 0.9i claim that the arena needs room. Both URLs are on the EHG forum. Keep the damage-evidence sentence that the element is not verified.

Delete both tips.

- "before it detonates" adds a timing the distance claim does not state.
- "barely leaving the center still exposes you" states a radius the sources in this catalog do not give.
- "More health does not replace avoiding the center of Soul Bomb" is generic pool advice already covered on Last death.
- "reserve a clear escape route before committing to a long attack animation" is generic for any fight.

The sourced mechanics sentences are enough until lane 3 has a tell. Do not replace the deleted tips with a softer version of the same movement advice.

## Heorot

Alias: `Heorot` only. Damage coverage unknown. Do not add Cold.

Keep the mechanics sentence that EHG documents Ice Spike and freezing interactions, and the explicit limit that historical fixes are not a current bug or a boss immunity. That limit is an evidence note, not a tip. Keep the 1.0.3 forum source. Keep the damage-evidence sentence that an Ice Spike name does not type the hit.

Delete both tips.

- "avoid its telegraph" and "standing still to finish a cast" are not in the mechanics line.
- "Do not assume freezing the boss will stop every hazard" gestures at a freeze interaction without stating it, and it sits next to the removed "Heorot cannot be frozen" claim. Delete it so that immunity claim cannot return as advice.
- "if your recorded death shows Freeze, review the ailment advice" duplicates Last death. Freeze advice already lives there.

Fact-check flag, not a catalog change: the older note describes the 1.0.3 text as an Ice Spike freeze bug, and a 1.0 note as a cold-resistance blessing. This profile does not claim cold damage or immunity. Leave both out until the fact-checker quotes the notes.

## Chronomancer Julra

Aliases: `Chronomancer Julra` and `Julra`. Do not match Chronomancer alone. Damage coverage is the one non-empty starter table: Void, Cold, Lightning, scope encounter, per-ability unknown. Evidence class on that row is datamined reference because the catalog points at Tunklab and a Tunklab URL is on the profile. Confidence unknown. Browse group unknown, so the UI must not file this under Dungeon until a group is actually set. Temporal Sanctum remains inside the mechanics prose, not a structured location.

Keep the mechanics prose: Temporal Sanctum, Temporal Shift, Divine and Ruined eras, and the statement that higher tiers and modifiers increase danger. Do not mint tier ids from that sentence. Keep the damage-evidence sentence that this is encounter coverage, not an attack-by-attack breakdown. Keep the Tunklab URL as datamined reference.

The second source is labeled "EHG 0.8.4 dungeon announcement (December 2021)" but the URL is `www.lastepochtools.com`, not the EHG forum. The projection marks that claim's evidence class unknown. Do not call the reprint developer documentation until the fact-checker compares it with an EHG page.

Delete both tips.

- "learn the safe timing in your tier" is an unsupported precision claim.
- "Switching eras keeps your position, so do not assume it also moves you out of a ground hazard" is a mechanical claim that is not in the mechanics or damage-evidence strings. The older research tip said to use Temporal Shift to leave a room-wide attack. Those two sentences disagree. Delete the position sentence. Do not restore the older community tip.
- "if capped, review critical protection and measured health loss" and the health-pool clause are Last death assessment copy. Three published elements must not become three gear cards on the boss page. The coverage chips already name Void, Cold, and Lightning.

No replacement tip in this lane. Lane 3 can write a short era tip after the position claim is sourced or dropped.

## Harbinger of Hatred

Alias: `Harbinger of Hatred` only. Id `harbinger-hatred`. This is not a Rahyeh profile. The baseline has no Rahyeh encounter to use as a parent, so parent status stays unknown rather than a guessed link. Damage coverage unknown. The word Void in Void Rahyeh Dive Bomb is not an element. Browse group stays unknown. Do not infer Harbinger from the name.

Keep the mechanics sentences: EHG names this variant's Void Rahyeh Dive Bomb, and this entry does not describe the ordinary Rahyeh timeline encounter. Keep the damage-evidence sentence that the word Void is not a measured damage report. Keep the 1.2.1 forum source.

Delete the first tip entirely. "leave the landing telegraph early" and "do not follow the landing point" add a telegraph, a timing, and a chase rule. The older note describes that 1.2.1 citation as a visual damage mismatch fix, not a movement guide. The fact-checker should confirm that reading from the page. Until then the telegraph tip is unsupported.

Delete the first sentence of the second tip, "use the captured ability and element in Last death." That is a pointer to another tab, not a Harbinger plan.

Keep one sentence, still labeled tactical inference: "A Harbinger imitation is not interchangeable with the original boss's full move set." It restates the mechanics distinction and does not invent an element or a parent id.

## Cross-profile

Generic movement, stop-casting, and "more health does not replace avoiding the hit" show up on more than one profile. Delete them here so the boss card does not repeat Last death. Assessment already owns resistance gaps, crit protection, recovery, and pool advice, with a three-card limit.

Claim-level sources on the projection are the source label plus URL. Mechanics and damage-evidence prose are separate claims with evidence class unknown, because each one mixes catalog wording and is not a single page quote. That is deliberate. It is not a finding that the prose is false.

## What Codex can delete without waiting

Delete the ten tip strings except the one Harbinger sentence named above. Delete "Campaign and Monolith variants may differ." Do not add Cold to Heorot, Void to Harbinger of Hatred, or per-attack rows to Julra. Do not add browse groups. Do not add parents.

## Open questions for the fact-checker

- Confirm the five EHG forum labels against the pages, including dates. This lane used the catalog's labels and hosts, and did not re-fetch them.
- Compare the LE Tools 0.8.4 reprint with an EHG original before anyone marks that URL as developer documentation.
- Resolve Julra's position-preservation sentence against the older "leave the room-wide attack" tip. Both should stay out of the menu until one is sourced.
- Quote the 1.0.3 Heorot note and say whether any freeze sentence is still current.
- Quote the 1.2.1 Harbinger note and say whether it contains a dive-bomb telegraph or only a visual fix.
