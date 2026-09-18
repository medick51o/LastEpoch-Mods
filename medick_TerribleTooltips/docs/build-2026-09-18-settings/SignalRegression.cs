internal static partial class Program
{
    private const string Affix = "+10 Health";
    private const string TierBracket = "[2<color=#FA9E3D>A</color>] +10 Health";
    private static string CleanLine(Type composer, bool sealedAffix = false, int tier = 2)
        => (string)Call(composer, "ComposeCleanLine", Affix, tier, Colors.TierColor(tier),
            new List<(string color, string letter)> { ("#FA9E3D", "A") }, sealedAffix);
    private static string Plain(string text) => Regex.Replace(text, "<[^>]+>", "");
    private static void SignalRegression()
    {
        Reset(); Prefs.Init();
        Check(Prefs.ShowSignal.Value, "real preference defaults ShowSignal to true");
        string prefSource = System.IO.File.ReadAllText(System.IO.Path.Combine(Environment.GetEnvironmentVariable("TT_TEST_SOURCE"), "Prefs.cs"));
        Check(prefSource.Substring(prefSource.IndexOf("var registered =")).Contains("\"ShowSignal\""), "ShowSignal is in real orphan-key allow-list");
        int variants = 0;
        foreach (TooltipLayout layout in Enum.GetValues<TooltipLayout>())
        foreach (SignalStyle style in Enum.GetValues<SignalStyle>())
        foreach (TierWordStyle word in Enum.GetValues<TierWordStyle>())
        foreach (DividerStyle divider in Enum.GetValues<DividerStyle>())
        foreach (UnitSeparatorStyle separator in Enum.GetValues<UnitSeparatorStyle>())
        foreach (bool grades in new[] { true, false })
        foreach (bool sealedAffix in new[] { true, false })
        {
            Prefs.Layout.Value = layout; Prefs.Style.Value = style; Prefs.TierWord.Value = word;
            Prefs.DividerStyle.Value = divider; Prefs.UnitSeparator.Value = separator; Prefs.ShowGradeLetters.Value = grades;
            Prefs.ShowSignal.Value = true;
            string shown = CleanLine(typeof(TooltipRecolor), sealedAffix);
            if (shown != CleanLine(typeof(BaselineTooltipRecolor), sealedAffix)) throw new Exception("ShowSignal=true differs from beta3");
            if (style == SignalStyle.PlainText && (!BorderProbe.Candidate(shown + Marker) || BorderProbe.Boxes(shown) != 1)) throw new Exception("Missing signal box");
            if (style == SignalStyle.PlainText && shown.Contains("<link=\"ttd\">") != grades) throw new Exception("Wrong divider visibility");
            Prefs.ShowSignal.Value = false;
            string hidden = CleanLine(typeof(TooltipRecolor), sealedAffix);
            if (BorderProbe.Candidate(hidden + Marker) || BorderProbe.Boxes(hidden) != 0) throw new Exception("Signal OFF left a box");
            // Exact equality: do NOT trim, collapse spaces or remove zero-width characters.
            if (hidden != Affix) throw new Exception("Signal OFF left divider, whitespace, marker or other signal content");
            var oldUnit = BorderProbe.Seed(hidden + Marker);
            BorderProbe.OnLateUpdate();
            if (oldUnit.Border.activeSelf || oldUnit.Divider.activeSelf) throw new Exception("Signal OFF left stale box/divider active");
            variants++;
        }
        Check(variants == 192, "192 combinations: default matches beta3; tier-only keeps box/no divider; OFF exact name/no box/no whitespace; existing border retirement hides stale images");
        Prefs.Init();
        string both = Plain(CleanLine(typeof(TooltipRecolor)));
        Check(both == "Tier 2 | A  +10 Health", "signal ON + grades ON: Tier 2 | A");
        Prefs.ShowGradeLetters.Value = false;
        Check(Plain(CleanLine(typeof(TooltipRecolor))) == "Tier 2  +10 Health", "signal ON + grades OFF: tier without divider");
        Prefs.TierWord.Value = TierWordStyle.Compact;
        Check(Plain(CleanLine(typeof(TooltipRecolor))) == "T2  +10 Health", "compact enum produces T2");
        Prefs.ShowSignal.Value = false;
        Check((string)Call(typeof(TooltipRecolor), "Compose", TierBracket) == Affix + Marker,
            "full bracket composer: exact name + existing four-ZWSP ownership suffix only (no trim)");
        Check((string)Call(typeof(TooltipRecolor), "ComposeUnbracketed", "SEALED AFFIX\n+10 Health\nTier: 2") == Affix + Marker,
            "unbracketed/sealed composer: no prefix, separator, box or extra whitespace");
        Prefs.NameColorMode.Value = AffixNameColorMode.TierColor;
        Check(CleanLine(typeof(TooltipRecolor)) == $"<color={Colors.TierColor(2)}>{Affix}</color>", "signal OFF preserves independently selected name colour");
        Prefs.Init();
        var ui = Reset();
        Call(typeof(TooltipRecolor), "RememberTooltip", ui, Args);
        var tmp = new TextMeshProUGUI((string)Call(typeof(TooltipRecolor), "Compose", TierBracket));
        Originals[tmp.GetInstanceID()] = (tmp, TierBracket);
        Call(typeof(SettingsUi), "Build", new SettingsPanelTabNavigable());
        Check(NativeSettings.Toggles.Count == 11, "exactly three new toggles, no developer knobs");
        var compact = NativeSettings.Toggles["TT - Compact Tier"];
        var signal = NativeSettings.Toggles["TT - Show Signal"];
        var border = NativeSettings.Toggles["TT - Unit Border"];
        Check(!compact.Initial && signal.Initial && border.Initial, "settings initial values reflect actual preferences");
        int layouts = ui.LayoutCalls, saves = Prefs.Category.Saves;
        compact.Callback(true);
        Check(Prefs.TierWord.Value == TierWordStyle.Compact && Plain(tmp.text) == "T2 | A  +10 Health" + Marker && ui.LayoutCalls > layouts,
            "compact ON saves and re-renders the cached open tooltip immediately");
        compact.Callback(false);
        Check(Prefs.TierWord.Value == TierWordStyle.Spelled && Plain(tmp.text).StartsWith("Tier 2 | A"), "compact OFF restores Spelled");
        signal.Callback(false);
        Check(tmp.text == Affix + Marker && !BorderProbe.Candidate(tmp.text), "signal row OFF immediately removes entire unit from cached tooltip");
        signal.Callback(true);
        Check(BorderProbe.Candidate(tmp.text), "signal row ON restores unit from cached original");
        layouts = ui.LayoutCalls;
        border.Callback(false);
        Check(!Prefs.UnitBorder.Value && ui.LayoutCalls > layouts, "border row changes existing pref and immediately re-renders");
        Check(Prefs.Category.Saves == saves + 5, "all five toggle transitions persist preferences");
        string description = NativeSettings.Descriptions["TT - Name Color"];
        Check(description.Contains("GreaterAffix") && description.Contains("default") && description.Contains("pre-3.1.0"), "name colour description exposes current default and old coloured text");
    }
}
