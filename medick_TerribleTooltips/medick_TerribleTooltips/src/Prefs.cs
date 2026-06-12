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

// v3 clean-line layouts — the ship gate: several must work at release.
public enum TooltipLayout
{
    BadgeLeft,    // "Tier 5·A  58% increased Lightning Damage"  ← default (Andrew's pick)
    SignalRight,  // "58% increased Lightning Damage        Tier 5 A"
    Trailing      // "58% increased Lightning Damage — Tier 5 A"
}

public enum AffixNameColorMode
{
    TierColor,   // affix text wears its tier color (the WoW retina read) ← default
    GameDefault  // the game's own text color; only the Tier·Grade signal is colored
}

public enum SignalStyle
{
    Badge,      // colored chip behind "Tier 7" / grades — the label look ← default
    PlainText   // colored text only, no chips
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

    // v3 clean line
    public static MelonPreferences_Entry<TooltipLayout>       Layout;
    public static MelonPreferences_Entry<SignalStyle>         Style;
    public static MelonPreferences_Entry<AffixNameColorMode>  NameColorMode;
    public static MelonPreferences_Entry<bool>                ShowGradeLetters;
    public static MelonPreferences_Entry<bool>                AlwaysShowRanges;
    public static MelonPreferences_Entry<bool>                AlwaysShowTierDetails;

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

        // v3 — THE CLEAN LINE (one line per affix; the essay dies)
        Layout = Category.CreateEntry("TooltipLayout", TooltipLayout.BadgeLeft,
            "Tooltip Layout", "Where the Tier·Grade signal sits on each affix line (BadgeLeft / SignalRight / Trailing)");
        Style = Category.CreateEntry("SignalStyle", SignalStyle.Badge,
            "Signal Style", "Badge = Tier/Grade as colored chips (label look); PlainText = colored text only");
        NameColorMode = Category.CreateEntry("AffixNameColor", AffixNameColorMode.TierColor,
            "Affix Name Color", "TierColor = affix text wears its tier color; GameDefault = game's own text color");
        ShowGradeLetters = Category.CreateEntry("ShowGradeLetters", true,
            "Show Grade Letters", "The S/A/B/C/F roll grade on each affix line");
        AlwaysShowRanges = Category.CreateEntry("AlwaysShowRanges", false,
            "Always Show Ranges", "Pin EHG's 'Range: X to Y' lines permanently (default: hidden, hold Alt to peek)");
        AlwaysShowTierDetails = Category.CreateEntry("AlwaysShowTierDetails", false,
            "Always Show Tier Details", "Pin EHG's full 'Tier: N (max craftable)' line (default: folded into the clean line, hold Alt to peek)");

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
