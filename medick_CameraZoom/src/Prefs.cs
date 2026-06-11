using MelonLoader;

namespace medick_CameraZoom
{
    // All persisted settings. Category and entry names must stay stable
    // across versions — they are the in-place upgrade path for existing
    // users' MelonPreferences.cfg (v1.x values carry over untouched).
    internal static class Prefs
    {
        public const float DefaultPanelX = 24f;
        public const float DefaultPanelY = 120f;

        public static MelonPreferences_Entry<float> ZoomMin;
        public static MelonPreferences_Entry<float> ZoomPerScroll;
        public static MelonPreferences_Entry<float> ZoomSpeed;
        public static MelonPreferences_Entry<bool>  LockAngle;
        public static MelonPreferences_Entry<float> Angle;
        public static MelonPreferences_Entry<float> MenuScale;
        public static MelonPreferences_Entry<float> PanelX;
        public static MelonPreferences_Entry<float> PanelY;
        public static MelonPreferences_Entry<bool>  DebugLog;

        public static void Init()
        {
            var cat = MelonPreferences.CreateCategory("medick_CameraZoom");
            ZoomMin       = cat.CreateEntry("ZoomMin",       -40f, "Zoom-out limit. More negative = further out. Game default ≈ -15");
            ZoomPerScroll = cat.CreateEntry("ZoomPerScroll",   3f, "Zoom change per scroll notch. Game default ≈ 1-2");
            ZoomSpeed     = cat.CreateEntry("ZoomSpeed",      10f, "Camera lerp speed to target zoom. Game default ≈ 5-8");
            LockAngle     = cat.CreateEntry("LockAngle",    false, "Lock camera viewing angle (prevents tilting)");
            Angle         = cat.CreateEntry("Angle",          55f, "Camera tilt angle when locked (degrees)");
            MenuScale     = cat.CreateEntry("MenuScale",     1.0f, "Settings panel scale");
            PanelX        = cat.CreateEntry("PanelX", DefaultPanelX, "Settings panel screen X");
            PanelY        = cat.CreateEntry("PanelY", DefaultPanelY, "Settings panel screen Y");
            DebugLog      = cat.CreateEntry("DebugLog",     false, "Verbose log output");
        }

        public static void Save()
        {
            try { MelonPreferences.Save(); } catch { }
        }
    }

    internal static class Dbg
    {
        public static void Log(string msg)
        {
            if (Prefs.DebugLog != null && Prefs.DebugLog.Value)
                MelonLoader.MelonLogger.Msg("[debug] " + msg);
        }
    }
}
