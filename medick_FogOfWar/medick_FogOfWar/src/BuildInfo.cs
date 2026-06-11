namespace medick_FogOfWar
{
    // Single source of truth for identity/version. Internal name stays
    // medick_The_fogOFwar forever (DLL, prefs category, cfg path, Nexus
    // upgrade path); the brand is what players see.
    internal static class BuildInfo
    {
        public const string Name         = "medick_The_fogOFwar";
        public const string DisplayName  = "Terrible fog_OFwar";   // settings category title
        public const string OfficialName = "MedicK's Terrible fog_OFwar";
        public const string Version      = "2.0.0";
        public const string Author       = "medick";
    }
}
