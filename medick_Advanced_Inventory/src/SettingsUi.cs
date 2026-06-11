using System;
using Il2Cpp;
using MelonLoader;

namespace medick_Terrible_Inventory
{
    // The "Terrible Inventory" category in the game's settings screen.
    // Five honest toggles (taxonomy: all real controls, no info rows, no
    // dropdowns needed in this mod) — delivering the April 2026 wish:
    // "allow the users to select what they want showing".
    internal static class SettingsUi
    {
        const string Category = BuildInfo.DisplayName;

        public static void Inject(SettingsPanelTabNavigable panel)
        {
            if (panel == null) return;
            try
            {
                if (!NativeSettings.TemplatesAvailable(panel)) return;

                NativeSettings.CreateToggle(panel, Category, "medick_Inv_ShowStash",
                    "STASH button",
                    "Opens your stash from anywhere. Toggle off to hide the button.",
                    Prefs.ShowStash.Value,
                    v => SetAndApply(Prefs.ShowStash, v));

                NativeSettings.CreateToggle(panel, Category, "medick_Inv_ShowStashAll",
                    "STASH ALL button",
                    "Dumps your whole inventory into the stash (affinity tabs respected). The flagship.",
                    Prefs.ShowStashAll.Value,
                    v => SetAndApply(Prefs.ShowStashAll, v));

                NativeSettings.CreateToggle(panel, Category, "medick_Inv_ShowVendor",
                    "VENDOR button",
                    "Opens the NPC vendor from anywhere — dump junk, collect gold.",
                    Prefs.ShowVendor.Value,
                    v => SetAndApply(Prefs.ShowVendor, v));

                NativeSettings.CreateToggle(panel, Category, "medick_Inv_ShowTeleport",
                    "Quick Teleport menu",
                    "The collapsible FACTIONS / DUNGEONS / HUBS column on the inventory panel.",
                    Prefs.ShowTeleport.Value,
                    v => SetAndApply(Prefs.ShowTeleport, v));

                NativeSettings.CreateToggle(panel, Category, "medick_Inv_DebugLog",
                    "Debug logging",
                    "Verbose MelonLoader log output for bug reports.",
                    Prefs.DebugLog.Value,
                    v => { Prefs.DebugLog.Value = v; Prefs.Save(); });
            }
            catch (Exception ex)
            {
                // Latched: this Awake re-fires per panel instance.
                NativeSettings.WarnDegradedOnce("settings injection failed: " + ex.Message);
            }
        }

        static void SetAndApply(MelonPreferences_Entry<bool> pref, bool value)
        {
            pref.Value = value;
            Prefs.Save();
            InventoryUi.ApplyVisibility();   // live — no reopen needed
        }
    }
}
