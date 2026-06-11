using MelonLoader;

namespace medick_FogOfWar
{
    // All persisted settings. Category name, entry name, and the custom cfg
    // path are frozen — they are the in-place upgrade path for existing
    // users' UserData/medick_The_fogOFwar.cfg.
    internal static class Prefs
    {
        public static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<MapVisionLevel> FogLevel;
        public static MelonPreferences_Entry<bool> DebugLog;

        public static void Init()
        {
            Category = MelonPreferences.CreateCategory("medick_The_fogOFwar", "Map Vision");
            FogLevel = Category.CreateEntry(
                "VisionLevel",
                MapVisionLevel.NORMAL,
                "Map Vision Level",
                "0=BLIND  1=HARD  2=LIMITED  3=NORMAL  4=SCOUT  5=ORACLE");
            DebugLog = Category.CreateEntry("DebugLog", false, "Verbose log output");
            Category.SetFilePath("UserData/medick_The_fogOFwar.cfg", autoload: true);
        }

        public static void Save()
        {
            try { Category.SaveToFile(); } catch { }
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
