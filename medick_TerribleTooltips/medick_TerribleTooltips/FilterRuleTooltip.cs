// ================================================================
//  FilterRuleTooltip.cs  —  medick_Terrible_Tooltips
// ================================================================

namespace medick_Terrible_Tooltips;

public static class FilterRuleTooltip
{
    private const string Marker = "\u200B\u200B";

    // ── Patch — nested like every other patch in this mod ─────────────
    [HarmonyPatch(typeof(TooltipItemManager), "OpenItemTooltip")]
    [HarmonyPriority(100)]
    internal static class Patch_OpenItemTooltip
    {
        private static void Prefix(ItemDataUnpacked data)
        {
            var mode = TerribleTooltipsMod.ShowFilterRuleNumber.Value;
            if (mode == TerribleTooltipsMod.FilterRuleDisplay.Off) return;
            if (data == null) return;
            try
            {
                string lore = "";
                try { lore = data._loreText ?? ""; } catch { }

                int mark = lore.IndexOf(Marker, StringComparison.Ordinal);
                if (mark >= 0) lore = lore.Substring(0, mark);

                if (!TryGetMatchedRule(data, out int displayNum, out Rule rule) || rule == null)
                    return;

                const string Gold = "#FA9E3D";
                string inject;

                if (mode == TerribleTooltipsMod.FilterRuleDisplay.NumberOnly)
                    inject = $"\n<size=120%><color={Gold}>Rule #{displayNum}</color></size>";
                else // NumberAndName
                    inject = $"\n<color={Gold}>Rule #{displayNum}: {GetRuleName(rule)}</color>";

                data._loreText = lore + Marker + inject;
            }
            catch (Exception ex) { MelonLogger.Warning($"[TT:Tooltip] error: {ex.Message}"); }
        }
    }

    // ── Rule finder ────────────────────────────────────────────────────
    private static bool TryGetMatchedRule(ItemDataUnpacked item,
                                          out int displayNum, out Rule matched)
    {
        displayNum = 0;
        matched    = null;

        var filter = ItemFilterManager.Instance?.Filter;
        if (filter == null) return false;
        var rules = filter.rules;
        if (rules == null || rules.Count == 0) return false;

        // Attempt 1: filter.Match() with out params
        try
        {
            var outcome = filter.Match(item, out _, out _,
                                       out int matchingRuleNum,
                                       out _, out _, out _, out _, out _);
            if (outcome != Rule.RuleOutcome.HIDE && matchingRuleNum > 0)
            {
                int idx = rules.Count - matchingRuleNum;
                if (idx >= 0 && idx < rules.Count && rules[idx] != null)
                {
                    displayNum = idx + 1;
                    matched    = rules[idx];
                    return true;
                }
            }
        }
        catch { }

        // Attempt 2: iterate rules manually
        try
        {
            for (int i = rules.Count - 1; i >= 0; i--)
            {
                Rule r = rules[i];
                if (r == null || !r.isEnabled) continue;
                try
                {
                    if (!r.Match(item, 0)) continue;
                    displayNum = rules.Count - i;
                    matched    = r;
                    return true;
                }
                catch { }
            }
        }
        catch { }

        return false;
    }

    private static string GetRuleName(Rule rule)
    {
        try { if (!string.IsNullOrWhiteSpace(rule.nameOverride)) return rule.nameOverride; } catch { }
        try { var d = rule.GetRuleDescription(); if (!string.IsNullOrWhiteSpace(d)) return d; } catch { }
        return "Unnamed Rule";
    }

    private static string GetRuleColorHex(Rule rule)
    {
        try
        {
            foreach (string name in new[] { "labelColor", "textColor", "itemColor",
                                            "color", "nameColor", "highlightColor" })
            {
                var f = rule.GetType().GetField(name);
                if (f == null) continue;
                if (f.GetValue(rule) is UnityEngine.Color c)
                    return "#" + ColorUtility.ToHtmlStringRGB(c);
            }
        }
        catch { }
        return "#FA9E3D";
    }
}
