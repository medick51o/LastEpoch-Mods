// ================================================================
//  FilterRuleTooltip.cs  —  medick_Terrible_Tooltips
//
//  Injects the matched loot filter rule number into the item tooltip.
//
//  Two-phase approach:
//    1. Harmony postfix captures ItemDataUnpacked from
//       SetAsItemTooltip / SetAsGroundTooltip
//    2. MonitorUpdate (called from OnUpdate) injects the rule tag
//       into a visible tooltip element AFTER the tooltip is fully
//       rendered — this avoids the game overwriting our text.
// ================================================================

namespace medick_Terrible_Tooltips;

public static class FilterRuleTooltip
{
    private const string Marker = "\u200B\u200B";
    private const string Gold   = "#FA9E3D";

    private static ItemDataUnpacked s_pendingItem  = null;
    private static bool             s_injected     = false;

    // ── Harmony: inventory / stash / equipment hover ──────────────────
    [HarmonyPatch(typeof(UITooltipItem), "SetAsItemTooltip")]
    private static class Patch_SetAsItemTooltip
    {
        private static void Postfix(UITooltipItem __instance, ItemDataUnpacked item)
        {
            if (item != null)
            {
                s_pendingItem = item;
                s_injected    = false;
            }
        }
    }

    // ── Harmony: ground-label hover ───────────────────────────────────
    [HarmonyPatch(typeof(UITooltipItem), "SetAsGroundTooltip")]
    private static class Patch_SetAsGroundTooltip
    {
        private static void Postfix(UITooltipItem __instance, ItemDataUnpacked _item)
        {
            if (_item != null)
            {
                s_pendingItem = _item;
                s_injected    = false;
            }
        }
    }

    // ── Called from OnUpdate — injects after tooltip is rendered ──────
    public static void MonitorUpdate()
    {
        var mode = TerribleTooltipsMod.ShowFilterRuleNumber.Value;
        if (mode == TerribleTooltipsMod.FilterRuleDisplay.Off) return;

        try
        {
            var tooltipUI = UITooltipItem.instance;
            if (tooltipUI == null) return;

            bool active = false;
            try { active = tooltipUI.tooltipActive; } catch { }

            if (!active)
            {
                s_pendingItem = null;
                s_injected    = false;
                return;
            }

            if (s_pendingItem == null || s_injected) return;

            // Check if our marker is already present anywhere
            // (re-hover of same item where injection persisted)
            try
            {
                foreach (var tmp in tooltipUI.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    if (tmp != null && (tmp.text ?? "").Contains(Marker))
                    { s_injected = true; return; }
                }
            }
            catch { }

            // ── Find the rule ─────────────────────────────────────────
            if (!TryGetMatchedRule(s_pendingItem, out int displayNum, out Rule rule) || rule == null)
            {
                s_injected = true;  // don't retry
                return;
            }

            string ruleTag;
            if (mode == TerribleTooltipsMod.FilterRuleDisplay.NumberOnly)
                ruleTag = $"<size=200%><color={Gold}>Rule#{displayNum}</color></size>";
            else
                ruleTag = $"<color={Gold}>Rule #{displayNum}: {GetRuleName(rule)}</color>";

            // ── Inject into the 'requires' element (proven visible) ───
            try
            {
                foreach (var tmp in tooltipUI.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    if (tmp == null || tmp.gameObject.name != "requires") continue;
                    if (!tmp.gameObject.activeInHierarchy) continue;

                    string orig = tmp.text ?? "";
                    tmp.text = ruleTag + Marker + "\n" + orig;
                    s_injected = true;
                    return;
                }
            }
            catch { }
        }
        catch { }
    }

    // ── Rule finder ───────────────────────────────────────────────────
    private static bool TryGetMatchedRule(ItemDataUnpacked item,
                                          out int displayNum, out Rule matched)
    {
        displayNum = 0;
        matched    = null;

        var filter = ItemFilterManager.Instance?.Filter;
        if (filter == null) return false;
        var rules = filter.rules;
        if (rules == null || rules.Count == 0) return false;

        try
        {
            var outcome = filter.Match(item, out _, out _,
                                       out int matchingRuleNum,
                                       out _, out _, out _, out _, out _);
            if (outcome != Rule.RuleOutcome.HIDE && matchingRuleNum > 0)
            {
                // matchingRuleNum IS the display number the game uses
                // (same number shown on ground labels).
                // The rules array is stored in reverse UI order.
                int idx = rules.Count - matchingRuleNum;
                if (idx >= 0 && idx < rules.Count && rules[idx] != null)
                {
                    displayNum = matchingRuleNum;
                    matched    = rules[idx];
                    return true;
                }
            }
        }
        catch { }

        // Fallback: manual scan
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
}
