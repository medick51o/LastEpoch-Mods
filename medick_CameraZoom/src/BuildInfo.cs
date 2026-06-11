namespace medick_CameraZoom
{
    // Single source of truth for identity/version — referenced by the
    // MelonInfo assembly attribute and the settings panel chrome.
    // Internal name stays medick_CameraZoom forever (DLL, prefs, Nexus
    // upgrade path); the brand is what players see.
    internal static class BuildInfo
    {
        public const string Name         = "medick_CameraZoom";
        public const string DisplayName  = "Terrible Zoom";
        public const string OfficialName = "MedicK's Terrible Zoom";
        public const string Tagline      = "a camera mod";
        public const string Version      = "2.0.0";
        public const string Author       = "medick";
    }
}
