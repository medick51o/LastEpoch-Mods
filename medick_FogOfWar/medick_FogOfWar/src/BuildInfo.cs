namespace medick_FogOfWar
{
    // Single source of truth for identity/version. Internal name stays
    // medick_The_fogOFwar forever (DLL, prefs category, cfg path, Nexus
    // upgrade path); the brand is what players see.
    internal static class BuildInfo
    {
        public const string Name         = "medick_The_fogOFwar";
        // The OF stands alone on purpose. You know why. ("bastos" branding)
        public const string DisplayName  = "Terrible fog_OF_war";   // settings category title
        public const string OfficialName = "MedicK's Terrible fog_OF_war";
        public const string Version      = "1.0.0";
        public const string Author       = "medick";
    }
}
