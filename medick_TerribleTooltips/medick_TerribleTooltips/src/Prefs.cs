namespace medick_Terrible_Tooltips;

// Enum member names and order are FROZEN — the cfg round-trips them by name.
public enum GroundLabelStyle
{
    None,         // disabled — brackets never shown
    TierAndRank,  // [5A 3C 7S]   ← default
    TierOnly,     // [5 3 7]
    RankOnly      // [A C S]
}

public enum FilterRuleDisplay
{
    Off,           // nothing shown
    NumberOnly,    // "Rule#69"
    NumberAndName  // "Rule #69: Maxroll told me to pick this up blah blah"
}

public enum RuleNumberPosition
{
    EHGDefault,  // leave wherever EHG writes it (between name and our brackets)
    Start,       // "(69) PLATED BELT [5A 1F 4C]"
    End          // "PLATED BELT [5A 1F 4C] (69)"
}

// All persisted settings. Category name, entry names and the cfg path are
// FROZEN — the in-place upgrade path for v1.x users.
internal static class Prefs
{
    public static MelonPreferences_Category Category;

    // Tooltip
    public static MelonPreferences_Entry<bool> EnableTooltips;
    public static MelonPreferences_Entry<bool> TooltipTierColors;
    public static MelonPreferences_Entry<bool> TooltipRankColors;

    // Ground labels
    public static MelonPreferences_Entry<GroundLabelStyle> LabelStyle;
    public static MelonPreferences_Entry<bool>             LabelFilterOnly;
    public static MelonPreferences_Entry<bool>             LabelAltKey;

    // Filter rule
    public static MelonPreferences_Entry<FilterRuleDisplay>  ShowFilterRuleNumber;
    public static MelonPreferences_Entry<RuleNumberPosition> LabelRulePosition;

    // New in v2
    public static MelonPreferences_Entry<bool> DebugLog;

    public static void Init()
    {
        Category = MelonPreferences.CreateCategory("medick_Terrible_Tooltips");

        EnableTooltips    = Category.CreateEntry("EnableTooltips",    true,
            "Terrible Tooltips", "Master on/off for WoW-style tier/grade tooltip colours");
        TooltipTierColors = Category.CreateEntry("TooltipTierColors", true,
            "Tooltip Tier Colors", "Colour affix names by their crafting tier (T1 gray → T7 mythic)");
        TooltipRankColors = Category.CreateEntry("TooltipRankColors", true,
            "Tooltip Rank Colors", "Colour grade letters by roll quality (F gray → S mythic)");

        LabelStyle      = Category.CreateEntry("GroundLabelStyle", GroundLabelStyle.TierAndRank,
            "Ground Label Style", "What to show on items on the ground (None / TierAndRank / TierOnly / RankOnly)");
        LabelFilterOnly = Category.CreateEntry("GroundLabelFilterOnly", false,
            "Ground Labels: Filter Only", "Only show ground labels on loot-filter highlighted items");
        LabelAltKey     = Category.CreateEntry("GroundLabelAltKey", false,
            "Ground Labels: Hold Alt to Show", "Hide ground brackets until you hold Alt (KG-style)");

        // Default changed Off → NumberOnly in v2.0.0 — the April decision,
        // finally shipped ("make a note to set... number only setting on by default").
        ShowFilterRuleNumber = Category.CreateEntry("ShowFilterRuleNumber", FilterRuleDisplay.NumberOnly,
            "Tooltip: Show Filter Rule #", "Show the matched loot filter rule number inside the item tooltip on hover");
        LabelRulePosition = Category.CreateEntry("LabelRulePosition", RuleNumberPosition.EHGDefault,
            "Ground Label: Rule # Position", "Where to place EHG's filter rule number on the ground label (Start / End / EHGDefault)");

        DebugLog = Category.CreateEntry("DebugLog", false, "Verbose log output");

        Category.SetFilePath("UserData/medick_Terrible_Tooltips.cfg", autoload: true);
    }

    public static void Save()
    {
        // printmsg: false — settings clicks must not spam the console;
        // "one startup line" is the family logging contract.
        try { Category.SaveToFile(false); Dbg.Log("prefs saved"); } catch { }
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
