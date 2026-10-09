# TICKET-01 — In-game settings: live position sliders + reset (A Terrible Button: The Nexus Button)

## TASK (the owner's words, verbatim)
- "we may need to give players a slider to move its location"
- "but we want to give a settting to put it back to its defualt location, and we want to get the default
  location as much as possible, the menu slider should also work live, similar to the menu sacaling in the
  gameplay settings do what you can to make it work"

## EXPECTED OUTCOME (gradeable)
1. The game's own Settings panel gains a section for this mod (category label "A Terrible Button") with:
   - a slider row "Nexus Button X Offset" (range -300..300 UI units, step 1, default 0),
   - a slider row "Nexus Button Y Offset" (range -150..150 UI units, step 1, default 0),
   - a button row "Reset Nexus Button Position" that sets both offsets to 0 AND moves both slider knobs to 0.
2. Dragging a slider moves the on-screen Nexus button LIVE (same frame), like the game's UI-scale slider.
3. Offsets persist across restarts (MelonPreferences category "medick_A_Terrible_Button", entries OffsetX, OffsetY).
4. Offsets are applied ON TOP OF the existing placement in `FollowCrest()` (crest snap / last crest spot /
   CrestOffset default), in the button's own scaled units (multiply by the current button scale so the
   nudge looks the same at any resolution/UI scale).
5. Every UI row is a CLONE of the game's own settings rows (native look), never hand-built. If a template is
   missing, the mod logs ONE warning and simply has no settings rows — the button itself keeps working.
6. `dotnet build -c Release -p:DeployToMods=false` in `medick_TheNexusButton` succeeds with 0 errors.

## CONTEXT (read these, do not guess)
- Mod source: `C:\Users\andre\le-sync\medick_TheNexusButton\src\NexusButtonMod.cs` (placement = `FollowCrest()`,
  button lookup = `CurrentButton()`; button scale set there as `btn.localScale`). SPEC.md in that folder.
- WORKING REFERENCE for native settings rows (same game, same author, shipped and working in-game):
  `C:\Users\andre\le-sync\medick_TerribleTooltips\medick_TerribleTooltips\src\NativeSettings.cs` (clones the
  game's toggle/dropdown rows; `CreateButton`, `CreateToggle`, `CreateEnumDropdown`, `TemplatesAvailable`,
  `WarnDegradedOnce`) and `...\src\SettingsUi.cs` (Harmony postfix on `SettingsPanelTabNavigable.Awake`,
  category header + rows). Copy/adapt these into this mod (namespace `medick_A_Terrible_Button`). Also read
  `...\src\Prefs.cs` there for the MelonPreferences pattern.
- NO slider helper exists yet. The game's settings panel has slider rows (`SettingsPanelTabNavigable._specificStepSliders`
  is an `Il2CppReferenceArray<Slider>`; the gameplay UI-scale control is a slider row). Find a slider row
  template at runtime: under the same settings root the reference code uses, take the first row whose
  children contain a `UnityEngine.UI.Slider`, preferring a row whose name contains "Scale". Clone it, relabel
  it (kill `LocalizeStringEvent` components first, as the reference does), set min/max/wholeNumbers/value,
  replace `onValueChanged` with a fresh event, and keep the delegate referenced (Il2Cpp GC — see the reference's
  keep-alive pattern). Update any value-text child on change.
- Game: Last Epoch (Unity 6, IL2CPP, MelonLoader 0.7.3, Il2CppInterop). Interop gotchas already learned:
  `parent as RectTransform` returns null — use `TryCast`/`GetComponent`; Il2Cpp delegates must stay referenced.

## CONSTRAINTS
- Only touch the WRITE SET. Keep the existing probe logging in NexusButtonMod.cs intact.
- No per-frame scene scans (FollowCrest already runs per frame; reading two cached floats there is fine).
- Defaults: offsets 0 = the crest-snap default spot ("get the default location as much as possible").

## MUST DO
- Run: `cd C:\Users\andre\le-sync\medick_TheNexusButton && dotnet build -c Release -p:DeployToMods=false`
  and paste the last lines of its output.

## MUST NOT
- Do not deploy/copy anything into the game's Mods folder. Do not commit. Do not edit files outside the
  write set. No undeclared spawns.

## WRITE SET
- C:\Users\andre\le-sync\medick_TheNexusButton\src\NexusButtonMod.cs  (modify)
- C:\Users\andre\le-sync\medick_TheNexusButton\src\Prefs.cs           (create)
- C:\Users\andre\le-sync\medick_TheNexusButton\src\NativeSettings.cs  (create)
- C:\Users\andre\le-sync\medick_TheNexusButton\src\SettingsUi.cs      (create)
- C:\Users\andre\le-sync\medick_TheNexusButton\medick_TheNexusButton.csproj (modify ONLY if a reference is missing)

## OUTPUT FORMAT
First line: DONE / DONE_WITH_CONCERNS / NEEDS_CONTEXT / BLOCKED. Then: files changed, the slider-template
strategy you implemented, build output tail, and anything unverified (it cannot be run in-game by you).

LAWS: builder never commits or deploys; review comes from another vendor.
'I could not tell what you meant' is a good outcome. Propose, don't guess.
