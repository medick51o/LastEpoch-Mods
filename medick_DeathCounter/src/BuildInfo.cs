namespace medick_DeathCounter
{
    // Single source of truth for identity/version. Internal name stays
    // medick_DeathCounter forever (DLL, prefs category, cfg path, log folder,
    // Nexus upgrade path); the brand is what players see.
    internal static class BuildInfo
    {
        public const string Name         = "medick_DeathCounter";
        public const string DisplayName  = "Terrible Deaths";
        public const string OfficialName = "MedicK's Terrible Deaths";
        public const string Tagline      = "a death counter that tells you why";
        public const string Version      = "0.1.0";
        public const string Author       = "medick";
    }
}
