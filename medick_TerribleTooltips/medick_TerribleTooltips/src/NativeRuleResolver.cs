namespace medick_Terrible_Tooltips;

internal static class NativeRuleResolver
{
    internal static string Label(ItemDataUnpacked item, FilterRuleDisplay mode)
    {
        if (item == null || mode == FilterRuleDisplay.Off) return null;
        var manager = ItemFilterManager.Instance;
        if (manager == null) return null;
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        // Temporary ALT ground-filter suppression must not hide the explanation.
        if (OrderedRuleResolver.SuppressTooltipLabel(manager.ManualFilterDisable, alt)) return null;
        var rules = manager.Filter?.rules;
        if (rules == null || rules.Count == 0 || PlayerFinder.getPlayer() == null) return null;
        int level = PlayerFinder.localPlayerLevel();
        if (!OrderedRuleResolver.First(rules.Count, i => rules[i], r => r.isEnabled,
            (r, actualLevel) => r.Match(item, actualLevel), level, out Rule rule, out int number)) return null;
        if (rule.type == Rule.RuleOutcome.HIDE) return null;
        string name = null;
        if (mode == FilterRuleDisplay.NumberAndName || mode == FilterRuleDisplay.NameOnly)
            name = !string.IsNullOrWhiteSpace(rule.nameOverride)
                ? RuleLabelText.Plain(rule.nameOverride, false)
                : RuleLabelText.Plain(rule.GetRuleDescription(), true);
        return OrderedRuleResolver.Style(rule.recolor, rule.color, rule.emphasized,
            Localization.DescriptorPreferences.EmphasizeLabelsWithBold,
            mode == FilterRuleDisplay.NameOnly ? 0 : number, name, preset =>
            {
                var color = EpochColorManager.GetLootfilterRuleColor(preset, item.rarity);
                return color == null ? null : "#" + ColorUtility.ToHtmlStringRGB(color.color);
            });
    }
}