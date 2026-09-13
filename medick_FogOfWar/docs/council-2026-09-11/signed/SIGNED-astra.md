# 🔵 Codex (Astra) — gpt-6-astra · architecture lens · blind, read-only · 2026-09-11
threadId 01a08fb9-211c-7a82-a92e-a71113672150 · packet C:\Sync\Projects\fog-review-2026-09-11 @ a36542f · verbatim below

DONE_WITH_CONCERNS
| rank | claim | failure mechanism | verify-by or repair | path:line |
|---|---|---|---|---|
| MATERIAL | CanvasGroup and SetActive can both affect one target; rollback is incomplete. | If an existing group's alpha write succeeds but a later setter throws, fallback disables the object without retaining its original group values; exit reactivates a still-transparent object. | Make hiding transactional; retain original state before mutation and record the mechanism explicitly. Runtime setter failure: NOT PROVEN. | medick_FogOfWar/src/FogController.cs:201,210,217,226,323 |
| MATERIAL | Protection is a snapshot, not a lifetime guarantee. | Labels/tooltips added beneath an already-hidden leaf inherit hiding; `IsHidden` returns before rescanning. A later refusal also does not undo earlier hiding. | Validate hierarchy ownership throughout hiding; restore a target that becomes protected. Current-game occurrence: NOT PROVEN. | medick_FogOfWar/src/FogController.cs:65,159,164,240,274 |
| MATERIAL | Restoration can lose ownership or lose its recovery record. | Existing CanvasGroup values are restored unconditionally over intervening game/mod changes; fallback activates any tracked inactive object, including one subsequently parked by the game; restore exceptions are swallowed before clearing records. | Track ownership transitions and retain failed live restores with diagnostics. | medick_FogOfWar/src/FogController.cs:188,316,323,329,331 |
| MATERIAL | Radius provenance is global and ambiguous. | A genuine default equal to an earlier mod-written value is rejected; after 32 distinct writes, an evicted value can be recaptured from a pooled instance. | Store default/write provenance per minimap lifetime; exercise overlapping and reused instances. | medick_FogOfWar/src/FogController.cs:36,82,119 |
| MATERIAL | Reported 1.4.7 template failure creates no settings controls. | The template probe fails before category/row creation; no alternate UI is called. | Discover the actual runtime hierarchy and repair the adapter; do not infer a working dropdown from startup levels. | BRIEF.md:19; medick_FogOfWar/src/SettingsUi.cs:51; medick_FogOfWar/src/NativeSettings.cs:58 |
| MATERIAL | A refused BLIND hide can retry and warn every frame. | No hidden record is created; Update repeats the list scan, two subtree probes, warning and radius write. | Latch refusal per target/topology state and throttle diagnostics. | medick_FogOfWar/src/FogController.cs:65,112,164,166,236 |
| MATERIAL | Shutdown/restore guards cover only Update. | Awake and direct Apply can still write during teardown; an Awake caused by fallback restoration can enter Apply while BLIND remains selected. | Guard every mutation entry and explicitly end patch/callback ownership. Actual reentrant Awake: NOT PROVEN. | medick_FogOfWar/src/FogController.cs:62,70,96,135,301,325 |
| MINOR | Settings persistence failure is silent. | Save exceptions disappear; the UI still applies the new in-memory value, so the next launch can recover the older file value. | Log save failures and validate loaded enum values. | medick_FogOfWar/src/Prefs.cs:30; medick_FogOfWar/src/SettingsUi.cs:70 |
| NOT PROVEN | BLIND fully hides the minimap while preserving all other UI. | A parent/sibling frame lies outside the leaf; a descendant ignoring parent CanvasGroups or a non-UI renderer may evade alpha hiding. The three-property test does not establish rendered invisibility. | In-hand hierarchy/render checks, including loot pickup and overlay transitions. | medick_FogOfWar/src/FogController.cs:159,210,274; docs/build-2026-09-10/REVIEW-ticket02b-gemini.md:12 |

Q1 — References below use S=`medick_FogOfWar/src/`, P=`medick_FogOfWar/medick_FogOfWar.csproj`, D=`docs/build-2026-09-10/`.
Method renames: missing Minimap.Awake loses capture and ordinary-level acquisition/application; Update can still acquire a minimap in BLIND, using fallback/stale defaults. Missing Update loses reassertion; missing SettingsPanel Awake loses injection. Each registration failure warns separately (S/FogOfWarMod.Patches.cs:19,24; S/FogController.cs:63,66,70).
That isolation does not prove resilience to removed types or changed interop accessors: runtime callbacks lack a common exception boundary; capture exceptions are silent, radius-write exceptions warn (S/FogOfWarMod.Patches.cs:43,57; S/FogController.cs:90,128).
Minimap GameObject renaming no longer changes scope. Reparenting unrelated HUD beneath that object does: only GroundItemLabel/UITooltipItem are checked. Settings template/header renames or the two-child root shift abort injection with a latched warning (S/FogController.cs:159,240; S/NativeSettings.cs:31,37,46,58).
Terrible Tooltips has no direct shared Harmony target here; indirect conflict requires shared hierarchy/state. A minimap mod's CanvasGroup writes are repeatedly overwritten; radius-only writes can survive because IsHidden skips Apply, and non-BLIND Update exits. No ordering contract is declared. Actual conflicts with any listed mod: NOT PROVEN without their code/runtime evidence (BRIEF.md:20; S/FogOfWarMod.Patches.cs:39,53,63; S/FogController.cs:63,65,289).

Q2 — Only BLIND initiates hiding; other levels restore and write radius. Shared ancestors are no longer targeted, but unrelated descendants remain exposed to alpha/raycast/deactivation effects (S/FogController.cs:103,112,159,210,226; S/FogLevels.cs:36).
HasProtectedItemUI conservatively refuses inactive protected descendants and inspection exceptions—even when nothing currently visible would be harmed. It passes for future descendants or replacement/custom UI types; scene-name changes alone do not defeat its type checks (S/FogController.cs:240,245,251).
BLIND→BLIND distinct active targets are individually tracked; destroyed targets need no resurrection. Existing CanvasGroup targets retain game-owned activation state on exit; fallback targets can be resurrected after pooling. Destroyed groups silently switch tracked targets to SetActive behavior (S/FogController.cs:172,188,213,269,285,313,323).
Both mechanisms remain: partial CanvasGroup mutation followed by fallback can strand transparency; deferred destruction of an added group also allows temporary overlap with SetActive. Fresh live failures are not retained for retry on exit (S/FogController.cs:210,222,226,329,331).

Q3 — Successful CanvasGroup hiding leaves the component active: every BLIND Update performs an O(H) hidden-record scan with interop property calls; successful recognition avoids Apply. The "costs nothing" comment is false for this path (S/FogController.cs:56,65,210,263).
Dead records are skipped, never pruned until ShowHidden; distinct targets across uninterrupted BLIND zones grow H without a cap. Static references retain managed interop wrappers; native Unity objects remaining alive because of those wrappers is NOT PROVEN (S/FogController.cs:19,20,213,269,331).
_writtenRadii is bounded to 32 floats, but _minimap/default are global and unload restores only the last instance's radius. Settings also retains six Toggle wrappers without teardown (S/FogController.cs:36,50,121,138; S/SettingsUi.cs:19).
Destroy of an added CanvasGroup is deferred: re-entering BLIND before destruction can reuse a doomed group, then switch to fallback on Update. Destruction also removes a component another mod may have started using; use explicit ownership and pending-destruction state (S/FogController.cs:194,285,319).

Q4 — On the reported failing settings invocation, zero new headers/rows/dropdowns are built; the warning latch suppresses logs, not future probes. Existing objects are not deleted by this early return (S/SettingsUi.cs:51; S/NativeSettings.cs:39,53,210).
Working legend callback: SelectLevel → preference assignment → Save → Apply → SyncLegend; Apply restores before checking minimap survival. Startup cfg reaches the game through Prefs.Init/autoload followed by Minimap.Awake. This source provides no preference-change subscription establishing live cfg application (S/SettingsUi.cs:68; S/Prefs.cs:14,23; S/FogController.cs:70,103).
Reported in-game changes despite missing-template logs remain NOT PROVEN by this packet; startup messages establish values at initialization, not their change mechanism (BRIEF.md:19; S/FogOfWarMod.cs:18,21).
If an undefined numeric enum reaches the controller, it behaves like NORMAL radius and does not hide; no legend row matches it. Misspelled-string parsing, malformed-file recovery, watcher behavior and autoload data loss are NOT PROVEN without MelonLoader implementation evidence. Init has no local recovery; Save suppresses failures (S/FogLevels.cs:32,36; S/SettingsUi.cs:84; S/Prefs.cs:14,23,30).

Q5 — Top three cleanup priorities:
1. Replace stale scope/lifetime guarantees: SPEC rule 4 contradicts leaf-only code; zero-update-cost and "every radius" claims contradict CanvasGroup activity and the cap. Correct README's 0.6.x requirement against the supplied 0.7.2 environment (SPEC.md:35; S/FogController.cs:37,61; CHANGELOG.md:36; README.md:26; BRIEF.md:8).
2. Retire unused CreateEnumDropdown from this mod; it retains extra control types and fragile child-index assumptions without supplying a fallback (S/NativeSettings.cs:210,223,262; S/SettingsUi.cs:53).
3. Make deployment opt-in. Missing `Compile Remove` is NOT a demonstrated duplicate-source defect here: archived docs are outside the project directory; exclusion becomes relevant if archives move beneath it (P:1,72; D/baseline/FogController.cs:17).

Q6 — Recommendations only; no rebuild, edit, game run or deployment performed.
S/FogController.cs — REBUILD ownership/state bookkeeping; global defaults, inferred hide mechanism and unconditional restoration cross lifetime boundaries (19,22,82,314).
S/FogOfWarMod.cs — PATCH teardown coordination and the "exactly" restore claim; it delegates to best-effort current-instance restoration (24; S/FogController.cs:132).
S/FogOfWarMod.Patches.cs — PATCH lifecycle/error boundaries and stale inactive-minimap narration (43,47,57).
S/NativeSettings.cs — PATCH hierarchy discovery/widget validation; retire the unused dropdown helper. Missing Toggle/Button can silently leave a named inert row (154,184,210).
S/SettingsUi.cs — PATCH callback/reference teardown and availability reporting; static toggles and callbacks survive without explicit cleanup (19,56,68).
S/Prefs.cs — PATCH enum validation and observable persistence failures (17,23,30).
S/FogLevels.cs — PATCH narration: NORMAL writes a radius; ORACLE's universal coverage/performance claim is NOT PROVEN (10,12,29; S/FogController.cs:114).
S/BuildInfo.cs — PATCH only as needed for release/version coordination; identity constants have no demonstrated functional defect (8,12; P:6).
P — PATCH default deployment policy; preserve the added UIModule reference required by the CanvasGroup implementation (57,72).
README.md — PATCH unavailable-settings guidance, supported loader version and inactive/instant guarantees (20,22,26).
SPEC.md — PATCH leaf scope, ownership/restoration contract and actual configuration paths (22,32,35,74).
CHANGELOG.md — PATCH with a new corrective entry qualifying restoration, pooling and bounded-history guarantees; preserve historical incident evidence (16,34,36,57).

MANIFEST — shell `Get-FileHash -Algorithm MD5`; S/P/D expand as above. Game stub not read.
```text
d3350c2e4ae0499d2f7171d594ef6f5f BRIEF.md
859318953aac7aa725e476f86fc674ff README.md
7d8a6fc98ba875f10f2cb8b88a35df82 SPEC.md
22471c81f768f1b73da502c6d52b588d CHANGELOG.md
e25d72d4430521d1560c964466598e3f P
413064da5908545d31f5aa51a8ab6861 S/BuildInfo.cs
ec37b77b2f39c8f0e94dbc4848fb055e S/FogController.cs
8464b638c0ebefb4bad9c69fdc643638 S/FogLevels.cs
5b2af4b2739c274bc0616c7a2b51d376 S/FogOfWarMod.cs
d5c21638dcf64890ba850c3be5f855db S/FogOfWarMod.Patches.cs
e4b9cce30d0da6b7285b0e832356e246 S/NativeSettings.cs
ffecfd5a4de6cb5c734cff585ac50eed S/Prefs.cs
3191d92a186e05604eeacd123449892a S/SettingsUi.cs
61e07a30ae7fd9749a2e749a57e56f35 D/baseline.md5
0b5a4a44c6ee53e89f77a47728618bd9 D/DELTA-ticket02.patch
8a5338634129f7e9dd5f645b6d9f2cd0 D/DELTA-ticket02b.patch
5a5a8d1c5d910688ceb134e3f6ec3855 D/REVIEW-ticket02-gemini-REJECT.md
2f93ab02c9feb81bdb5d130e82a9411f D/REVIEW-ticket02b-gemini.md
819d4e10692f91d3900be9f8e8a1f039 D/TICKET-02-blind-hide-scope.md
a501c1a45f4356f090da860524ea87d0 D/TICKET-02b-repair-leaf-only.md
50bfd42c6f2af9d99ac4873a245294cf D/baseline/BuildInfo.cs
a323f9a159599d7db93aa2eabc155c09 D/baseline/CHANGELOG.md
7ca7ec3a77d1c1d1dd6b6ae7a1359540 D/baseline/FogController.cs
36a294da5192fad30e9e52e9b3229a7e D/baseline/medick_FogOfWar.csproj
```
🔵 Codex (Astra)
