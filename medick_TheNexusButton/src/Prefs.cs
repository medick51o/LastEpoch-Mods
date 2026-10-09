using System;
using MelonLoader;
using UnityEngine;

namespace medick_A_Terrible_Button;

internal static class Prefs
{
    internal static MelonPreferences_Category Category;
    internal static MelonPreferences_Entry<float> OffsetX;
    internal static MelonPreferences_Entry<float> OffsetY;
    internal static MelonPreferences_Entry<bool> HotkeyEnabled;
    internal static MelonPreferences_Entry<bool> ShowButton;
    internal static MelonPreferences_Entry<bool> DebugLog;
    static bool _saveWarned;

    internal static void Init()
    {
        Category = MelonPreferences.CreateCategory("medick_A_Terrible_Button");
        OffsetX = Category.CreateEntry("OffsetX", 0f, "Nexus Button X Offset");
        OffsetY = Category.CreateEntry("OffsetY", 0f, "Nexus Button Y Offset");
        // Off by default (Andrew 20:54): players who already use N must opt in.
        HotkeyEnabled = Category.CreateEntry("HotkeyEnabled", false, "N key opens the Nexus");
        // Andrew 21:59: let hotkey-only players hide the button. On by default.
        ShowButton = Category.CreateEntry("ShowButton", true, "Show the Nexus button");
        DebugLog = Category.CreateEntry("DebugLog", false, "Write diagnostic lines (button recipe, layout, what is under the button) on every click");
        Category.SetFilePath("UserData/medick_A_Terrible_Button.cfg", autoload: true);
        OffsetX.Value = Normalize(OffsetX.Value, 300f);
        OffsetY.Value = Normalize(OffsetY.Value, 150f);
    }

    static float Normalize(float value, float limit)
        => float.IsFinite(value) ? Mathf.Round(Mathf.Clamp(value, -limit, limit)) : 0f;

    internal static void Save()
    {
        try { Category.SaveToFile(false); }
        catch (Exception ex)
        {
            if (_saveWarned) return;
            _saveWarned = true;
            MelonLogger.Warning("Nexus button prefs save failed: " + ex.Message);
        }
    }
}
