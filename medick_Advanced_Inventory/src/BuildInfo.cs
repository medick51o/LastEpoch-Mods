namespace medick_Terrible_Inventory
{
    // Single source of truth for identity/version — v1 shipped with three
    // disagreeing version strings across its files. Never again.
    // Internal name stays medick_Terrible_Inventory (DLL, Nexus #29 path).
    internal static class BuildInfo
    {
        public const string Name         = "medick_Terrible_Inventory";
        public const string DisplayName  = "Terrible Inventory";   // settings category title
        public const string OfficialName = "MedicK's Terrible Inventory";
        public const string Version      = "2.0.0";   // bump in lockstep with <Version> in the csproj
        public const string Author       = "medick";
    }
}
