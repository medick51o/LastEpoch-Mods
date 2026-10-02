# Lane 6 UI design

UI code was not changed. This is the porting design for the Boss tips tab and the main death card in `DeathPanel`. Storage identities stay as they are. Browse state is not saved on the death.

## What the panel does today

The fifth tab is Boss tips. It draws one full-width button per starter profile, then the selected profile as a continuous stack: selected-death paragraph, mechanics, damage-evidence paragraph, one colored line per published element, both tips, the starter-catalog sentence, and a browser button per source.

The Last death card already shows the quote, location, `BossCatalog.EncounterLabel`, one link button, assessment, game text, ailments, the health-loss timeline, defenses, and at most three advice cards. History rows are shorter. The panel scroll view passes `GUIStyle.none` for both scrollbars, so the scroll affordance is not visible. Buttons have no visible focus ring. A hidden controller cursor is already ignored by the counter HUD. The panel itself has no documented controller order.

That boss page becomes a wall of text as soon as the roster grows. The design below replaces the stack with a browse list and a short card.

## Browse by timeline, boss, Harbinger, and dungeon

Four mode chips, one active: Timeline, Boss, Harbinger, Dungeon. They read `browseGroups` on the catalog document. A profile appears in a mode only when that group is status known and lists the mode.

Until lane 1 fills those groups, every mode shows the same empty state:

> Harbinger grouping is not in the catalog yet.

Use the matching word for the other modes: Timeline, Boss, Dungeon. Do not pour all five starter profiles into every mode. Do not put Harbinger of Hatred in the Harbinger mode because the display name contains Harbinger. The baseline projection leaves `browseGroups` unknown for all five, including that one.

The list under a filled mode is one row per encounter:

- Display name, wrapped to two lines, then ellipsis only on the row.
- One relationship chip: Follows [parent name], No parent, or Parent unknown. The chip text comes from parent status. Unknown is not shown as no parent.
- Damage chips, or the words Type unknown. A chip is the element name plus its color. Julra can show Void, Cold, and Lightning as published encounter coverage. The other four starters show Type unknown. Do not turn those chips into gear advice.

Opening a row shows one card, not the whole research note:

- Wrapped display name.
- One status line: Identified encounter, Unconfirmed attacker guide, or Browsing. This guide is not linked to the selected death.
- Coverage chips, then at most three tip sentences. Each sentence is one wrapped paragraph.
- Two collapsed controls: Details, and Sources. Details holds mechanics and per-ability rows. Sources holds claim labels and the browser buttons. Both start collapsed.

If abilities, ailments, variants, locations, or per-ability damage are unknown, the card says so in one line each, for example Abilities unknown. It does not invent a paragraph to fill the gap.

## Compact death card

The open Last death view keeps a short card. The rest moves behind More.

Always visible:

1. Death number and local time.
2. The existing quote, colored by the captured killing element, with the element name in the sentence.
3. Scene or zone, level, and one encounter status sentence. Keep the current sentences: Boss encounter: [name] (game flag and attacker match); Boss encounter confirmed by game; boss not identified; Game did not flag this as a boss encounter; Boss encounter unknown.
4. One link button, or no button, using the rules in the next section.
5. Up to three action titles from the saved death. Bodies stay behind Why on each card.

Behind More: game death text, ailments, recorded health loss, defenses, and capture confidence. Ward-only damage stays described as not captured. Assessment and saved updates stay on this card. They are not copied onto the boss card.

History rows stay one metadata line plus one cause line. Long cause text may ellipsize on the row. The opened card shows the full text.

## Identified encounter versus unconfirmed guide

| Game flag | Exact alias | Classification | Button |
|---|---|---|---|
| true | yes | identified | Boss tips: [display name] |
| true | no | confirmed, boss not identified | Browse boss tips |
| null | yes | unconfirmed guide | Attacker guide: [display name] |
| null | no | unknown | no boss button |
| false | any | not a boss encounter | no boss button |

Exact alias means the trimmed killer, compared ordinal-ignore-case with an alias string. Ability name, zone, raw scene, and irregular source do not count. Substring and add or proxy names do not count.

The unconfirmed button opens the guide and leaves `IsBossFight` null. The card adds the sentence already used in the boss tab: The attacker name matches this guide, but the game did not provide a boss-fight flag. Encounter classification remains unknown.

Opening a guide sets the browse selection. It does not write the encounter id, display name, damage coverage, or location onto the death.

## Manual selection never relabels the saved death

Choosing a row, switching mode, or expanding Details changes only the browse selection. These death fields stay byte for byte: id, killer, killer ability, elements, damage, crit, boss flag, zone, raw scene, zone level, ailments, defenses, and the reassessment key.

If the open guide is not the guide for the selected death, show: This guide is not linked to the selected death. That sentence is already in the boss tab. Keep it.

A grade A on the recorded killing element does not clear unknown follow-up threats, and it does not change the death. The boss card may say Follow-up threats remain unknown. It must not rewrite the saved reassessment or add three resistance cards.

## Accessibility

Measured against the current theme, WCAG contrast of foreground on panel background `10141C` and on surface `161A24`:

| Text | On background | On surface |
|---|---|---|
| Primary text `E0E4EB` | 14.46 | 13.64 |
| Muted text `B8C1CF` | 10.16 | 9.58 |
| Physical `C8B9A6` | 9.61 | 9.07 |
| Fire `FF625E` | 6.29 | 5.93 |
| Cold `6FB7E8` | 8.44 | 7.96 |
| Lightning `E8D04A` | 11.90 | 11.22 |
| Necrotic `4FB0A0` | 7.07 | 6.67 |
| Void `9B6BD6` | 4.80 | 4.53 |
| Poison `7BC043` | 8.31 | 7.84 |

Body text clears 4.5 to 1. Void on the surface is the tight pair at 4.53. Keep Void as normal text on the dark surface, with the word Void beside the color. Do not put Void in a smaller muted style, and do not move it onto a lighter chip without rechecking contrast. Every damage color already has a text label in the death timeline. The boss card must do the same. Identified versus unconfirmed is a word, not a color.

Controller order when the panel is open: Close, mode chips, encounter rows, the open card's Details and Sources, then More on the death card if that tab is active. The focused control draws a 2 pixel gold ring using the existing accent `C9A653`. Focusing a control scrolls it into the viewport. Shoulder buttons scroll the same view. The current hidden scrollbars are not a controller affordance. Add a visible thumb, or keep the focus ring and shoulder scroll together so a controller can move through a long localized name without a mouse wheel.

Long localized names wrap on the open card and on the death quote. Row ellipsis is allowed only in the list. The link button grows up to the card width and wraps. It does not stay capped at a width that clips Boss tips: plus a long name. Aliases with an unknown locale are matched as exact strings and are not treated as every language.

Panel scale already runs from 1 to 1.4. The layout has to wrap at the narrow end of that range and at a short viewport. One scroll region covers the tab body. The mode chips and the open encounter name stay pinned above that region so scrolling the tips does not scroll the mode away.

Proposed player-facing strings use a single hyphen where a compound word needs one. They do not use a double dash.

## What stays on Last death

Resistance gaps, crit protection, recovery, and the three-action limit stay on the death card. The boss card does not repeat them. Julra's three published elements are coverage chips, not three Check resistance cards. Source links do not generate advice.

## Port notes

Implement the layout in `DeathPanel` later. Point the boss tab at `BossCatalogSchema.ProjectBaseline` browse fields when groups become known. Until then, show the empty state rather than the five-button list. Do not persist browse selection on `DeathRecord`. Do not change reassessment files. The caption strings in `BossCatalogSchema.DeathLinkCaption` match the current button text so a later UI port can call one method.
