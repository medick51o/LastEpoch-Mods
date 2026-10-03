# v0.1.4 review and Astra handoff

October 2, 2026, PT. Experimental local candidate. Do not merge, publish or prepare a Nexus release. This review describes source changes and checks, not successful native verification.

Installed locally at 6:04 PM PT with the game closed and existing user authorization. DLL SHA256: 0E193C8A59B9564FEF78FC02D28A50CD33BFCA895C7BDA3DAA7458ADF9BC587D. A verified v0.1.3 DLL backup exists. Configuration, original history and saved reassessments have unchanged hashes. The user is starting the next native test.

## What actually happened

The v0.1.3 test restored the game's report. The screenshot showed Void Blast from Rahyeh, The Black Sun, Void damage, 2609 killing damage and 2015 overkill. The mod counted and saved death #5, with an approximately 0.15-second-old defense snapshot, but saved empty cause/type and zero damage/overkill. No formatter callback capture lines appeared in Latest.log. Registration of four observers therefore did not prove that the native helpers ran. No nullable callback exception appeared in that test.

The saved defenses matched the character sheet: Cold 75% effective and 112% total; Void 2%; Physical 2%; Fire and Lightning 29%; Necrotic 56%; Poison 51%. The frozen zone level was 70. No screenshot data was backfilled into the original record.

## Changes

- DeathMessageInbox keeps changed reports for up to five seconds across the report-before-health race. Character changes, stale times and repeated persistent text cannot create a new report. Newly captured formatter fragments can establish a fresh identical message. A UI redraw alone cannot.
- DeathReportHooks reads the static field and active DeathInformationText labels, observes OnEnable as well as UpdateText, and records diagnostic read states during death. It still avoids every Nullable-bearing report callback.
- DeathMessageParser recognizes the explicit English cause/type/damage/overkill layout shown by the game. It preserves original rich text and sets DetailSource to game death message (parsed). It accepts only enum-listed explicit types and valid integer amounts. Conflicting, unsupported and translated formats remain text-only. No type is inferred from an attacker, ability, text color or boss catalog. Crit, ailment identity, irregular source and boss flags remain unknown unless another capture path supplies them.
- ResistanceReview builds separate effective/total/previous readings. Missing and nonfinite values remain unknown. Capped cards are ordered first, then relevant damage types. Red arrows identify relevant uncapped defenses. Large gap is a presentation threshold of at least 25% modeled extra same-type damage, not a game rule or survival probability.
- DeathPanel uses colored resistance cards and a separate additional-defense grid. Reassessment shows its saved stat reading and original percentages. Its displayed resistance comparison replaces duplicated plain comparison lines; saved calculation text remains available in the reassessment data.
- Frozen ZoneLevel supplies area context when the online stat snapshot omits AreaLevel. Original death and caller stat dictionaries remain untouched.
- Recording was renamed Death tracking, with an explanation. It pauses counting/saving deaths, not video capture. Existing tracking preference and saved history remain intact.

## Verification

275 CoreTests pass, including early report timing, stale/wrong-character/identical-report behavior, the exact Rahyeh screenshot layout, rich-text persistence, mixed explicit types, conflicting/overflowing values, capped order, area-specific gap math, unknown-value handling, no boss inference, original/reassessed snapshot separation and frozen-area reassessment.

NoGame Release and local Release both use DeployToMods=false and warnings as errors. Both have zero warnings/errors and pass InteropGuard: 75 referenced interop members, 246 proven members from 42 shipped DLLs, with the existing Application.OpenURL allowance. .NET 6.0.32 loads the same Core source and both boss resources. Native metadata confirms OnEnable and UpdateText have no parameters and the original three formatter helper signatures exist. These checks do not execute the native observers or render Unity GUI.

## Required native test

1. Restart with v0.1.4 and confirm the game explanation still appears.
2. Die once to Rahyeh. Latest.log should contain death message read status and game death message capture. Check exactly one appended JSONL record for killer, ability, Void, damage, overkill and original text. Repeat without restarting.
3. Open Last death. Cold should appear capped with its overcap total below. Void should be marked as killing damage, with a red arrow and its measured gap. At 2% Void in area 70, the standard resistance-only model is approximately 77% more Void damage than at 75%, holding other modifiers constant. This is not a whole-fight survival claim.
4. Change gear, reassess, and verify current readings are saved separately and original values are preserved. Reload and review/delete saved updates.
5. Verify smaller-screen/text-size layouts, all seven hues and warning contrast. No in-game rendering test has yet been performed on this candidate.
6. Check unsupported/localized/no-report deaths remain honest. Season/Legacy and readable localized zone names still need online API work. Boss tips remain experimental.

If capture still fails, inspect the diagnostic static getter result, selected text length and active label count before changing hooks. The report-before-health discard was a real code path; it is not yet established as the sole runtime cause. Do not patch the Nullable signatures or replace MelonLoader DLLs to chase it. Preserve original records and use another test death.

## Preserved scope

The compact movable HUD, reset warning joke, separate unlimited reassessment history, character counter, saved season/Legacy and zone context, three-action advice limit, contribution link, embedded CoreModule repair and all 54 experimental encounter records remain. Boss data is factual or independently authored, with collapsed sources and uncertainty flags. Raw guide research remains outside the distributable source. No GitHub merge, push or Nexus publication was made.
