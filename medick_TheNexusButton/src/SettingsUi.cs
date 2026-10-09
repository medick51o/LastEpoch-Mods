using System;
using HarmonyLib;
using Il2Cpp;

namespace medick_A_Terrible_Button;

internal static class SettingsUi
{
    const string Category = "A Terrible Button";

    [HarmonyPatch(typeof(SettingsPanelTabNavigable), nameof(SettingsPanelTabNavigable.Awake))]
    internal static class Patch_SettingsPanel
    {
        static void Postfix(SettingsPanelTabNavigable __instance)
        {
            try
            {
                if (!NativeSettings.TemplatesAvailable(__instance)) return;

                NativeSettings.CreateToggle(__instance, Category, "NB - Show Button",
                    "Show Nexus Button", "Turn off to hide the button and use only the hotkey.",
                    Prefs.ShowButton.Value, on => { Prefs.ShowButton.Value = on; Prefs.Save(); });
                NativeSettings.CreateToggle(__instance, Category, "NB - Hotkey",
                    "Nexus Hotkey (N)", "Press N to open the Nexus. Off by default; turn on if N is free in your keybinds.",
                    Prefs.HotkeyEnabled.Value, on => { Prefs.HotkeyEnabled.Value = on; Prefs.Save(); });
                var x = NativeSettings.CreateSlider(__instance, Category, "NB - Offset X",
                    "Nexus Button X Offset", -300f, 300f, Prefs.OffsetX.Value,
                    value => { Prefs.OffsetX.Value = value; SaveAndRefresh(); });
                var y = NativeSettings.CreateSlider(__instance, Category, "NB - Offset Y",
                    "Nexus Button Y Offset", -150f, 150f, Prefs.OffsetY.Value,
                    value => { Prefs.OffsetY.Value = value; SaveAndRefresh(); });
                NativeSettings.CreateButton(__instance, Category, "NB - Reset Position",
                    "Reset Nexus Button Position", () =>
                    {
                        Prefs.OffsetX.Value = Prefs.OffsetY.Value = 0f;
                        x.SetValue(0f);
                        y.SetValue(0f);
                        SaveAndRefresh();
                    });
                MelonLoader.MelonLogger.Msg("settings: 'A Terrible Button' section built");
            }
            catch (Exception ex)
            {
                NativeSettings.RemoveSection(__instance, Category);
                NativeSettings.WarnDegradedOnce("settings build failed: " + ex.Message);
            }
        }
    }

    static void SaveAndRefresh()
    {
        NexusButtonMod.FollowCrest();
        Prefs.Save();
    }
}
