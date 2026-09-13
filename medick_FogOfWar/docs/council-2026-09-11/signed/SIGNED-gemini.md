# 🟢 Gemini — Antigravity · robustness / player-experience lens · blind, read-only · 2026-09-11
conversationId 2140221b-f25b-427d-8b6f-6117b866e8d8 · status SUCCESS · brain UNREPORTED · packet C:\Sync\Projects\fog-review-2026-09-11 @ a36542f · verbatim below (file:// links kept as delivered)

DONE_WITH_CONCERNS

| rank | claim | failure mechanism | verify-by or repair | path:line |
| --- | --- | --- | --- | --- |
| MATERIAL | BLIND zone transitions leak dead handles in `_hidden` | `_hidden` only cleared on `ShowHidden()`; staying in BLIND across N zones appends dead targets | Prune null/destroyed entries in `Apply()` or zone `Awake` | [FogController.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L20) |
| MATERIAL | Settings UI disabled on Last Epoch 1.4.7 | `'Toogle - Minion Health Bars'` template removed/renamed in 1.4.7; `TemplatesAvailable` returns false | Update template string search or fall back to resilient selector | [NativeSettings.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/NativeSettings.cs#L31) |
| MINOR | Unused 78-line `CreateEnumDropdown` in `NativeSettings.cs` | v2.0 replaced dropdown with clickable legend rows; helper retained needlessly | Remove dead method | [NativeSettings.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/NativeSettings.cs#L210-L287) |
| MINOR | `SPEC.md` rule 4 contradicts v1.0.1 leaf-only law | `SPEC.md` describes searching outermost 'minimap' parent; v1.0.1 hides leaf only | Update `SPEC.md` rule 4 | [SPEC.md](file:///C:/Sync/Projects/fog-review-2026-09-11/SPEC.md#L35) |
| NOT PROVEN | BLIND leaf hide leaves empty minimap frame on screen | `CanvasGroup.alpha=0` on leaf hides minimap content, but outer frame may reside on parent | Test in-game or inspect UI prefab hierarchy | [FogController.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L160) |

## Q1 — Breakage & Mod Interactions
Renaming `Minimap.Awake` ([FogOfWarMod.Patches.cs:39](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogOfWarMod.Patches.cs#L39)), `Minimap.Update` ([FogOfWarMod.Patches.cs:53](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogOfWarMod.Patches.cs#L53)), or `SettingsPanelTabNavigable.Awake` ([FogOfWarMod.Patches.cs:63](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogOfWarMod.Patches.cs#L63)) catches exception in `ApplyPatches()` ([FogOfWarMod.Patches.cs:32](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogOfWarMod.Patches.cs#L32)), logging one warning and disabling that feature. Settings hierarchy changes or missing template ([NativeSettings.cs:31,58](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/NativeSettings.cs#L31)) trigger `WarnDegradedOnce` ([NativeSettings.cs:37](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/NativeSettings.cs#L37)), disabling settings UI while keeping cfg control (`UserData/medick_The_fogOFwar.cfg`). Leaf-only hide ([FogController.cs:160](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L160)) ensures zero interaction with Terrible Tooltips ground labels.

## Q2 — Correctness & Player Harm
Hiding leaf GameObject ([FogController.cs:160](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L160)) prevents hiding ground item labels/tooltips/HUD. `HasProtectedItemUI` ([FogController.cs:236](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L236)) degrades BLIND to HARD if protected UI is found. Leaving BLIND restores targets ([FogController.cs:303](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L303)) ungated by live minimap instance ([FogController.cs:103](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L103)). CanvasGroup path falls back to SetActive on exception ([FogController.cs:217-226](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L217-L226)); both never apply to one target. NOT PROVEN whether hiding leaf leaves an empty minimap frame visible ([FogController.cs:160](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L160)).

## Q3 — Per-Frame Cost & Lifetime
`Minimap.Update` postfix ([FogOfWarMod.Patches.cs:57](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogOfWarMod.Patches.cs#L57)) executes 3 bool checks + 1-element `IsHidden` check per frame in BLIND ([FogController.cs:54-67](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L54-L67)), sub-microsecond cost. `_hidden` ([FogController.cs:20](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L20)) grows by 1 reference per zone change during BLIND until `ShowHidden()`, holding GCHandles. `_writtenRadii` ([FogController.cs:36](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L36)) capped at 32. `Destroy` of added `CanvasGroup` ([FogController.cs:320](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs#L320)) is safe.

## Q4 — Settings & Config Resilience
Missing `'Toogle - Minion Health Bars'` template ([NativeSettings.cs:31](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/NativeSettings.cs#L31)) causes `TemplatesAvailable` ([NativeSettings.cs:58](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/NativeSettings.cs#L58)) to return false on 1.4.7, logging one warning ([NativeSettings.cs:37](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/NativeSettings.cs#L37)) and safely bypassing UI injection. UI clicks call `FogController.Apply()` ([SettingsUi.cs:72](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/SettingsUi.cs#L72)) immediately. Invalid/misspelled `VisionLevel` cfg entries parse/fallback to `NORMAL` / zone default radius ([FogLevels.cs:33](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogLevels.cs#L33)). `Prefs.Save()` ([Prefs.cs:30](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/Prefs.cs#L30)) persists silently without losing entries.

## Q5 — Cleanup
1) Remove unused `CreateEnumDropdown` helper ([NativeSettings.cs:210-287](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/NativeSettings.cs#L210-L287)). 2) Update `SPEC.md:35` rule 4 to document v1.0.1 leaf-only law. 3) Correct `README.md:26` MelonLoader version reference (0.6.x -> 0.7.2 net6).

## Q6 — Verdict Per File
[medick_FogOfWar.csproj](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/medick_FogOfWar.csproj): PATCH — Add doc compile exclusions & update ML comments.
[BuildInfo.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/BuildInfo.cs): PATCH — Maintain single source of truth versioning.
[FogController.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogController.cs): PATCH — Prune dead/null targets from `_hidden` during zone transitions.
[FogLevels.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogLevels.cs): PATCH — Preserve frozen vision level definitions.
[FogOfWarMod.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogOfWarMod.cs): PATCH — Retain lifecycle hooks.
[FogOfWarMod.Patches.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/FogOfWarMod.Patches.cs): PATCH — Retain isolated Harmony patches.
[NativeSettings.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/NativeSettings.cs): PATCH — Update 1.4.7 template selector and purge unused dropdown method.
[Prefs.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/Prefs.cs): PATCH — Retain frozen cfg paths and category keys.
[SettingsUi.cs](file:///C:/Sync/Projects/fog-review-2026-09-11/medick_FogOfWar/src/SettingsUi.cs): PATCH — Retain radio legend controls.

## Manifest
BRIEF.md: D3350C2E4AE0499D2F7171D594EF6F5F
README.md: 859318953AAC7AA725E476F86FC674FF
SPEC.md: 7D8A6FC98BA875F10F2CB8B88A35DF82
CHANGELOG.md: 22471C81F768F1B73DA502C6D52B588D
medick_FogOfWar/medick_FogOfWar.csproj: E25D72D4430521D1560C964466598E3F
medick_FogOfWar/src/BuildInfo.cs: 413064DA5908545D31F5AA51A8AB6861
medick_FogOfWar/src/FogController.cs: EC37B77B2F39C8F0E94DBC4848FB055E
medick_FogOfWar/src/FogLevels.cs: 8464B638C0EBEFB4BAD9C69FDC643638
medick_FogOfWar/src/FogOfWarMod.cs: 5B2AF4B2739C274BC0616C7A2B51D376
medick_FogOfWar/src/FogOfWarMod.Patches.cs: D5C21638DCF64890BA850C3BE5F855DB
medick_FogOfWar/src/NativeSettings.cs: E4B9CCE30D0DA6B7285B0E832356E246
medick_FogOfWar/src/Prefs.cs: FFECFD5A4DE6CB5C734CFF585AC50EED
medick_FogOfWar/src/SettingsUi.cs: 3191D92A186E05604EEACD123449892A
docs/build-2026-09-10/DELTA-ticket02b.patch: 8A5338634129F7E9DD5F645B6D9F2CD0

🟢 Gemini
