// ================================================================
//  SettingsPatch.cs  —  medick_The_fogOFwar
//
//  Injects the Map Vision dropdown + level legend + notes into the
//  in-game settings panel under a "Map Vision" tab.
// ================================================================

namespace medick_FogOfWar;

[HarmonyPatch(typeof(SettingsPanelTabNavigable), nameof(SettingsPanelTabNavigable.Awake))]
public static class SettingsPanelPatch
{
    public static void Postfix(SettingsPanelTabNavigable __instance)
    {
        try
        {
            const string Cat = "The FoG OF wAr";

            // ── Vision Level dropdown ─────────────────────────────────
            __instance.CreateNewOption_EnumDropdown(Cat,
                "<color=#FF44FF>Map Vision Level</color>",
                "Controls how much of the map is revealed. Lower number = less you see. Higher number... you get the idea. NORMAL = default game state.",
                FogOfWarMod.FogLevel,
                i =>
                {
                    FogOfWarMod.FogLevel.Value = (FogOfWarMod.MapVisionLevel)i;
                    FogOfWarMod.Category.SaveToFile();
                    // Attempt live apply for levels 1-5
                    MinimapAwakePatch.TryApplyLive();
                    MelonLogger.Msg($"[The_fogOFwar] Level changed to {FogOfWarMod.FogLevel.Value}.");
                });

            // ── Apply notes ───────────────────────────────────────────
            __instance.CreateNewOption_Button(Cat,
                "<color=#FA9E3D>⚠  BLIND requires a full game restart — both ways.</color>",
                "Switching TO blind hides the minimap at startup. Switching OUT of blind also needs a full restart to bring it back. If you are currently in blind mode and want to move to 1-5, close the game, change the setting, relaunch. All other levels (1-5) attempt to apply live or take effect on zone change / character reload.",
                () => { });

            __instance.CreateNewOption_Button(Cat,
                "<color=#DADADA>Levels 1-5 apply on zone change or character reload.</color>",
                "We try to apply the radius live when you change the settings but if nothing changes just swap zones or relog. Its just better to just pick your poison now and leave it. We all know you're here for option 5 anyway.",
                () => { });

            // ── Level Legend (display only) ───────────────────────────
            __instance.CreateNewOption_Button(Cat,
                "<color=#DADADA>0  BLIND</color>",
                "Going in blind? OK @wudijo  |  Minimap (top right) hidden entirely. RevealRadius = 0. Overlay map still accessible — honor system.  |  Requires full game restart.",
                () => { });

            __instance.CreateNewOption_Button(Cat,
                "<color=#E1E1E1>1  HARD</color>",
                "This setting was put in for filler, no one will use it.  |  Minimap visible but reveals nothing. RevealRadius = 0. Going in blind but at least you have the UI.  |  Note: this box is not clickable, it is just here for reference.",
                () => { });

            __instance.CreateNewOption_Button(Cat,
                "<color=#77ACFF>2  LIMITED</color>",
                "This too was put in for filler, no one will use it.  |  RevealRadius = 69% of default.  |  Note: this box is not clickable, it is just here for reference.",
                () => { });

            __instance.CreateNewOption_Button(Cat,
                "<color=#16FF0E>3  NORMAL</color>",
                "You downloaded this from nexusmods, I don't think so.  |  Default game state — no change.  |  Note: this box is not clickable, it is just here for reference.",
                () => { });

            __instance.CreateNewOption_Button(Cat,
                "<color=#FA9E3D>4  SCOUT</color>",
                "*raises an eyebrow*  |  RevealRadius = 3x default.  |  Note: this box is not clickable, it is just here for reference.",
                () => { });

            __instance.CreateNewOption_Button(Cat,
                "<color=#FF44FF>5  ORACLE</color>",
                "We already know why you came.  |  RevealRadius = 600 — full zone reveal without tanking your FPS.  |  Note: this box is not clickable, it is just here for reference.",
                () => { });
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[The_fogOFwar] Settings panel injection failed: {ex.Message}");
        }
    }
}
