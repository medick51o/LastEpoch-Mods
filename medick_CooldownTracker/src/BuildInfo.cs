namespace medick_CooldownTracker
{
    // Single source of truth for identity/version — referenced by the
    // MelonInfo assembly attribute and the settings panel chrome.
    // Internal name stays medick_CooldownTracker forever (DLL, prefs,
    // Nexus upgrade path); the brand is what players see.
    internal static class BuildInfo
    {
        public const string Name         = "medick_CooldownTracker";
        public const string DisplayName  = "Terrible Cooldowns";
        public const string OfficialName = "MedicK's Terrible Cooldowns";
        public const string Tagline      = "a cooldown tracker";
        public const string Version      = "5.0.0";
        public const string Author       = "medick";
    }
}
