namespace medick_Terrible_Tooltips;

// Single source of truth for identity/version — v1 shipped with disagreeing
// version strings (Core header said 1.2.0 while the assembly said 1.5.0).
//
// ⚠ Name is PUBLIC ABI: Fallen Star's Improved Tooltips detects this mod by
// scanning RegisteredMelons for the exact string "Terrible Tooltips".
// Changing it breaks the ecosystem treaty. (See ARCHAEOLOGY.md.)
internal static class BuildInfo
{
    public const string Name         = "Terrible Tooltips";   // MelonInfo name — FROZEN (Fallen Star ABI)
    public const string OfficialName = "MedicK's Terrible Tooltips";
    public const string Version      = "3.0.0";   // bump in lockstep with <Version> in the csproj
    public const string Author       = "medick";
}
