using System;
using MelonLoader;
using UnityEngine;

namespace medick_DeathCounter
{
    // All persisted settings. Category name, entry names, and the custom cfg
    // path are frozen from v0.1.0: they are the in-place upgrade path for
    // users' UserData/medick_DeathCounter.cfg.
    internal static class Prefs
    {
        public static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<bool>   Tracking;
        public static MelonPreferences_Entry<bool>   ShowCounter;
        public static MelonPreferences_Entry<bool>   ShowDeathToast;
        public static MelonPreferences_Entry<string> PanelKey;
        public static MelonPreferences_Entry<float>  HudX;
        public static MelonPreferences_Entry<float>  HudY;
        public static MelonPreferences_Entry<float>  HudScale;
        public static MelonPreferences_Entry<float>  PanelScale;
        public static MelonPreferences_Entry<bool>   ProbeApi;
        public static MelonPreferences_Entry<string> HookOverrides;
        public static MelonPreferences_Entry<bool>   DebugLog;
        static bool _saveFailedWarned;

        public static KeyCode PanelKeyCode { get; private set; } = KeyCode.Insert;

        public static void Init()
        {
            Category = MelonPreferences.CreateCategory("medick_DeathCounter", BuildInfo.DisplayName);
            Tracking       = Category.CreateEntry("Tracking",       true,  "Record deaths",
                "Master switch. Off = no deaths are counted or logged.");
            ShowCounter    = Category.CreateEntry("ShowCounter",    true,  "Show the death counter",
                "Shift + PanelKey toggles this in game.");
            ShowDeathToast = Category.CreateEntry("ShowDeathToast", true,  "Show 'killed by' under the counter for a few seconds after a death");
            PanelKey       = Category.CreateEntry("PanelKey",       "Insert", "Key that opens the Last Death panel",
                "Any Unity KeyCode name: Insert, F9, PageDown, Backslash ...");
            HudX           = Category.CreateEntry("HudX",           0.5f,  "Counter X (0 = left edge, 1 = right edge)");
            HudY           = Category.CreateEntry("HudY",           0.015f, "Counter Y (0 = top, 1 = bottom)");
            HudScale       = Category.CreateEntry("HudScale",       1.0f,  "Counter scale");
            PanelScale     = Category.CreateEntry("PanelScale",     1.0f,  "Death log text scale");
            ProbeApi       = Category.CreateEntry("ProbeApi",       false, "Log the game's health/death/ailment API at startup",
                "Turn on if deaths show as UNKNOWN, then send the MelonLoader log.");
            HookOverrides  = Category.CreateEntry("HookOverrides",  "",    "Extra hooks, e.g. hit:Il2Cpp.BaseHealth.ReceiveDamage; death:Il2Cpp.PlayerHealth.Die",
                "Roles: hit, death, ailment. Separate with ';'. Applied at the next launch.");
            DebugLog       = Category.CreateEntry("DebugLog",       false, "Verbose log output");
            Category.SetFilePath("UserData/medick_DeathCounter.cfg", autoload: true);
            Sanitize();
        }

        static void Sanitize()
        {
            if (!float.IsFinite(HudX.Value))     HudX.Value = HudX.DefaultValue;
            if (!float.IsFinite(HudY.Value))     HudY.Value = HudY.DefaultValue;
            if (!float.IsFinite(HudScale.Value)) HudScale.Value = HudScale.DefaultValue;
            if (!float.IsFinite(PanelScale.Value)) PanelScale.Value = PanelScale.DefaultValue;
            HudX.Value     = Mathf.Clamp01(HudX.Value);
            HudY.Value     = Mathf.Clamp01(HudY.Value);
            HudScale.Value = Mathf.Clamp(HudScale.Value, 0.9f, 2f);
            PanelScale.Value = Mathf.Clamp(PanelScale.Value, 1f, 1.4f);

            if (Enum.TryParse(PanelKey.Value?.Trim(), ignoreCase: true, out KeyCode k) && k != KeyCode.None)
                PanelKeyCode = k;
            else
            {
                MelonLogger.Warning($"PanelKey '{PanelKey.Value}' is not a Unity KeyCode; using Insert");
                PanelKeyCode = KeyCode.Insert;
            }
        }

        public static void Save()
        {
            try { Category.SaveToFile(false); Dbg.Log("prefs saved"); }
            catch (Exception ex)
            {
                if (_saveFailedWarned) return;
                _saveFailedWarned = true;
                MelonLogger.Warning($"prefs save failed ({ex.Message}); settings may not persist to the next launch");
            }
        }
    }

    internal static class Dbg
    {
        public static void Log(string msg)
        {
            if (Prefs.DebugLog != null && Prefs.DebugLog.Value)
                MelonLogger.Msg("[debug] " + msg);
        }
    }
}
