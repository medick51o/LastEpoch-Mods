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

        // One (Lo, Hi, Default) triple per tunable — the single source for
        // the CreateEntry default, the sanitizer clamp, and the slider range.
        public const float ZoomMinLo = -200f,  ZoomMinHi = -1f,  ZoomMinDefault = -40f;
        public const float PerScrollLo = 0.1f, PerScrollHi = 20f, PerScrollDefault = 3f;
        public const float SpeedLo = 0.5f,     SpeedHi = 30f,     SpeedDefault = 10f;
        public const float AngleLo = 20f,      AngleHi = 85f,     AngleDefaultPref = 55f;
        public const float MenuScaleLo = 0.7f, MenuScaleHi = 2.0f;

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
            ZoomMin       = cat.CreateEntry("ZoomMin",       ZoomMinDefault,   "Zoom-out limit. More negative = further out. Game default ≈ -15");
            ZoomPerScroll = cat.CreateEntry("ZoomPerScroll", PerScrollDefault, "Zoom change per scroll notch. Game default ≈ 1-2");
            ZoomSpeed     = cat.CreateEntry("ZoomSpeed",     SpeedDefault,     "Camera lerp speed to target zoom. Game default ≈ 5-8");
            LockAngle     = cat.CreateEntry("LockAngle",     false,            "Lock camera viewing angle (prevents tilting)");
            Angle         = cat.CreateEntry("Angle",         AngleDefaultPref, "Camera tilt angle when locked (degrees)");
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
