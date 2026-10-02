# Terrible Death Counter and Log: current-stat assessment

Update October 2, 11:57 AM PT: current assessments now save as separate dated history with review/deletion controls and explained defense progress grades. Read `CODEX-DEATH-REASSESSMENT-HISTORY.md` for the latest 198-test snapshot and candidate hash. Temporary-only behavior and hashes below describe the earlier implementation.

October 2, 2026, 11:36 AM PT. Experimental review build only. Not installed, merged, pushed or published. The previous advice expansion and season-history features remain included.

## Advice priorities

Measured resistance gaps now come first, ahead of crit checks. Health-buffer advice requires known capped resistances for the relevant damage types, no unresolved resistance-loss or crit recommendation, and either substantial recorded health loss during a hit/burst or a large game report with positive overkill. A merely large damage number, unknown resistance, missing max health, DoT tick or gradual attrition does not establish this pool recommendation.

The pool card reviews maximum health and reliable hit mitigation. Ward is mentioned only when a positive ward value was readable, and only conditionally for a build that already maintains ward in combat. One instantaneous reading cannot prove sustainable ward. Freeze advice no longer defaults to extra ward. Reported damage and overkill remain unverified against health/ward units, so no exact survival threshold is claimed.

## Reassess with current stats

The button is near the selected death's headline. It reads fresh, normalized defenses only for the same loaded, living character. The result shows relevant resistance, max-health, instantaneous ward, crit and physical armor comparisons where available, then at most three distinct actions. The original action cards are hidden while the current assessment is shown, avoiding a duplicate set. Saved death details remain visible below it.

A closed current resistance gap stops receiving a resistance gear recommendation. A new gap becomes the first action. Newly acquired full crit protection does not conflict with the old crit report; recorded Critical Vulnerability remains a conditional risk if it returns. If maximum health has already increased, the buffer card acknowledges that change and directs attention to maintaining it and reviewing mitigation before adding still more health.

Resistance comparisons can show a modeled percentage change when both readings and the original area's level are known. They never substitute town's current area level, split a mixed hit evenly, or turn unverified overkill into a survival guarantee. Missing current values stay unknown and never fall back to old gear. Current buffs may expire, and the old debuffs may recur.

The assessment is a click-time snapshot with a visible timestamp. Refresh after changing gear or buffs. Closing the panel, switching death/character, adding a new death, or receiving relevant updated death details clears it. Neither the stored death nor its season label, counter, or caller's stat dictionary is changed. There is no new persistent file.

## Checks and remaining native tests

- CoreTests: 182/182 pass. Nineteen new regression tests cover priorities, pool/ward gates, attrition/DoT, partial/missing/invalid stats, mixed types, changed gear, crit/debuff context, immutable history, old area level, modeled resistance deltas and the three-action limit.
- NoGame and local game builds: zero warnings and errors with `DeployToMods=false` and warnings as errors.
- InteropGuard: both modes pass, 75 calls against 42 reference DLLs. Existing fenced `Application.OpenURL` exception unchanged.
- Installed DLL hash remains `2A02FE16530697BD2765753D5B4B42D4C99C15ADA8266F4F9D6C75B8E19EF704`.

Native behavior remains unverified. After installation is authorized, test the button after respawning, change resistance gear, refresh, and verify the displayed change against the character sheet. Review an old monolith death while in town to check area context. Try dead/loading states, another character, a crit death after gaining protection, and ward changes without declaring a sustained value. Check legibility and scrolling at the user's resolution, preservation of `deaths.jsonl`, and the earlier season-label tests. No game was launched for these checks.

## Current artifacts

- `Terrible_Death_Counter_and_Log_v0.1.2-advice-review.zip`: one root DLL, refreshed current assessment/advice/season build.
- `CODEX-DEATH-ADVICE-PORTED-SOURCE.zip`: refreshed source and required compatibility/guard tools, excluding generated build files.
- Candidate DLL SHA256: `3C341AC0E3BECE24140FCEDB90C1070120DD449C9D2567D0D484369143DE0374`.

Still v0.1.2 review source, not a versioned public release. Earlier reports' hashes and test counts describe their own snapshots.
