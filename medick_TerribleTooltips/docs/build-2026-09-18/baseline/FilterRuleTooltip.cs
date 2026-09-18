// ================================================================
//  FilterRuleTooltip.cs — injects the matched loot filter rule number
//  into the item tooltip.
//
//  Two-phase approach (the LeHud-war survivor — see ARCHAEOLOGY.md):
//    1. Harmony postfixes on UITooltipItem.SetAsItemTooltip /
//       SetAsGroundTooltip capture the ItemDataUnpacked. These are
//       verifiably DIFFERENT native functions from anything LeHud
//       patches. We NEVER patch TooltipItemManager.OpenItemTooltip —
//       it shares a native function with LeHud's OpenTooltip patch and
//       two Harmony patches on one native function = circular IL2CPP
//       trampoline = instant stack overflow on hover.
//    2. MonitorUpdate (from OnUpdate) injects the gold rule tag into
//       the 'requires' TMP after the tooltip is fully rendered.
//       (loreText is invisible on non-uniques; PrefixHeader gets
//       overwritten by the game — both were dead ends.)
//
//  Rule # stays GOLD #FA9E3D — Fallen Star owns filter color
//  coordination; the gold is a promise, not a limitation.
// ================================================================

namespace medick_Terrible_Tooltips;

public static class FilterRuleTooltip
{
    // Two zero-width spaces (vs GroundLabels' three) — escapes, not
    // literal chars: load-bearing bytes must be visible in review.
    private const string Marker = "\u200B\u200B";
    private const string Gold   = "#FA9E3D";

    private static ItemDataUnpacked s_pendingItem  = null;
    private static bool             s_injected     = false;
    private static UITooltipItem s_owner;
    private static GameObject s_target;
    private static int s_targetType;
    private static byte[] s_itemId;
    private static (byte type, ushort subtype, byte rarity, ushort unique, ushort individual) s_fallbackId;
    private static int s_startFrame;
    private static float s_deadline;
    private static int s_lastAttemptFrame = -1;
    private static bool s_ruleResolved;
    private static Rule s_rule;
    private static int s_displayNum;
    private static bool s_repairPending;
    private static bool s_hadDestination;
    private const float SettleSeconds = 0.5f;

    // The serialized ID, not the managed/native wrapper identity, distinguishes
    // content: callers may unpack the same item anew on every setter call.
    private static void Capture(UITooltipItem tooltip, ItemDataUnpacked item)
    {
        try
        {
            if (tooltip == null || item == null) { ClearPending(); return; }
            var id = item.id;
            bool same = s_owner == tooltip && s_target == tooltip.target &&
                        s_targetType == tooltip.targetType && s_pendingItem != null;
            if (id != null && id.Length > 0)
            {
                same &= s_itemId != null && s_itemId.Length == id.Length;
                if (same)
                    for (int i = 0; i < id.Length; i++)
                        if (s_itemId[i] != id[i]) { same = false; break; }
            }
            else
            {
                // Defensive fallback for ID-less data. Stable fields distinguish
                // material types without a fresh wrapper restarting every frame.
                same &= s_itemId == null && s_fallbackId ==
                    (item.itemType, item.subType, item.rarity, item.uniqueID, item.individualID);
            }

            if (same)
            {
                s_pendingItem = item;
                // Known destination repair is separate from discovery exhaustion.
                // Keep it pending across master/display off, including a tag
                // inherited from a prior hover whose rule was not resolved here.
                if (s_hadDestination)
                {
                    if (!UsableDestination()) BeginReplacement();
                    else if (!(s_destination.text ?? "").Contains(Marker))
                        s_repairPending = true;
                }
                return;
            }

            ClearPending();
            s_owner = tooltip;
            s_target = tooltip.target;
            s_targetType = tooltip.targetType;
            s_pendingItem = item;
            s_fallbackId = (item.itemType, item.subType, item.rarity, item.uniqueID, item.individualID);
            if (id != null && id.Length > 0)
            {
                s_itemId = new byte[id.Length];
                for (int i = 0; i < id.Length; i++) s_itemId[i] = id[i];
            }
            s_startFrame = Time.frameCount;
            s_deadline = Time.unscaledTime + SettleSeconds;
            if (TooltipPerf.Enabled) TooltipPerf.RuleStart();
        }
        catch { ClearPending(); }
    }

    private static TextMeshProUGUI s_destination;

    private static void ClearPending()
    {
        s_owner = null;
        s_target = null;
        s_pendingItem = null;
        s_itemId = null;
        s_rule = null;
        s_destination = null;
        s_ruleResolved = false;
        s_repairPending = false;
        s_hadDestination = false;
        s_injected = false;
        s_lastAttemptFrame = -1;
    }

    // ── Harmony: inventory / stash / equipment hover ──────────────────
    [HarmonyPatch(typeof(UITooltipItem), "SetAsItemTooltip")]
    internal static class Patch_SetAsItemTooltip
    {
        private static void Postfix(UITooltipItem __instance, ItemDataUnpacked item)
        {
            if (item != null) TooltipRecolor.MarkDirty();
            Capture(__instance, item);
        }
    }

    // ── Harmony: ground-label hover ───────────────────────────────────
    // Parameter is named _item in the game binary — HarmonyX matches by name.
    [HarmonyPatch(typeof(UITooltipItem), "SetAsGroundTooltip")]
    internal static class Patch_SetAsGroundTooltip
    {
        private static void Postfix(UITooltipItem __instance, ItemDataUnpacked _item)
        {
            if (_item != null) TooltipRecolor.MarkDirty();
            Capture(__instance, _item);
        }
    }

    // ── Called from OnUpdate — injects after tooltip is rendered ──────
    public static void MonitorUpdate()
    {
        try
        {
            // Ownership is checked even while the display/master preference is off.
            // A pooled tooltip must not inherit the previous item's pending rule.
            bool owned = false;
            try
            {
                owned = s_owner != null && s_owner.tooltipActive &&
                        s_owner == UITooltipItem.instance && s_owner.target == s_target &&
                        s_owner.targetType == s_targetType;
            }
            catch { }

            if (!owned)
            {
                if (s_pendingItem != null && TooltipPerf.Enabled) TooltipPerf.RuleReuse();
                ClearPending();
                return;
            }
            if (!Prefs.EnableTooltips.Value ||
                Prefs.ShowFilterRuleNumber.Value == FilterRuleDisplay.Off) return;
            if (s_repairPending)
            {
                if (!UsableDestination())
                {
                    BeginReplacement();
                }
                else
                {
                    if (!(s_destination.text ?? "").Contains(Marker))
                    {
                        ResolveRuleOnce();
                        if (s_rule != null) WriteRule(s_destination);
                    }
                    s_repairPending = false;
                    return;
                }
            }
            if (s_pendingItem == null || s_injected) return;
            int frame = Time.frameCount;
            if (frame == s_lastAttemptFrame) return;
            if (s_lastAttemptFrame >= 0 && Time.unscaledTime > s_deadline)
            {
                GiveUp();
                return;
            }
            s_lastAttemptFrame = frame;
            if (TooltipPerf.Enabled) TooltipPerf.RuleAttempt();

            // One descendant pass per attempt; only matching is cached, so a
            // requirements row created/activated a few frames late is still found.
            TextMeshProUGUI destination = null;
            foreach (var tmp in s_owner.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (tmp == null || tmp.gameObject.name != "requires" ||
                    !tmp.gameObject.activeInHierarchy) continue;
                if ((tmp.text ?? "").Contains(Marker))
                {
                    s_destination = tmp;
                    s_hadDestination = true;
                    s_injected = true;
                    Dbg.Log("marker already in 'requires' — injection persisted");
                    return;
                }
                if (destination == null) destination = tmp;
            }

            ResolveRuleOnce();
            if (s_rule == null) { s_injected = true; return; }
            if (destination != null)
            {
                WriteRule(destination);
                return;
            }
            if (TooltipPerf.Enabled) TooltipPerf.RuleNoTarget();
            if (Time.unscaledTime >= s_deadline)
                GiveUp();
        }
        catch
        {
            // Discovery/native failures share the same deadline, never an endless retry.
            if (Time.unscaledTime >= s_deadline)
                GiveUp();
        }
    }

    private static void GiveUp()
    {
        s_injected = true; // terminal for this content, even if SetAs repeats
        s_repairPending = false;
        s_hadDestination = false;
        s_destination = null;
        if (TooltipPerf.Enabled) TooltipPerf.RuleGiveUp();
    }

    private static bool UsableDestination()
        => s_destination != null && s_destination.gameObject.name == "requires" &&
           s_destination.gameObject.activeInHierarchy &&
           s_destination.transform.IsChildOf(s_owner.transform);

    private static void BeginReplacement()
    {
        // Consume a previously found destination once. Repeated setters with no
        // replacement cannot reopen this budget; another success is required.
        s_hadDestination = false;
        s_destination = null;
        s_repairPending = false;
        s_injected = false;
        s_startFrame = Time.frameCount;
        s_deadline = Time.unscaledTime + SettleSeconds;
        s_lastAttemptFrame = -1;
        if (TooltipPerf.Enabled) TooltipPerf.RuleReplacement();
    }

    private static void ResolveRuleOnce()
    {
        if (s_ruleResolved) return;
        // Set before native matching: even an exception cannot retry it forever.
        s_ruleResolved = true;
        if (TooltipPerf.Enabled) TooltipPerf.RuleMatch();
        TryGetMatchedRule(s_pendingItem, out s_displayNum, out s_rule);
    }

    private static void WriteRule(TextMeshProUGUI destination)
    {
        if (!Prefs.EnableTooltips.Value || Prefs.ShowFilterRuleNumber.Value == FilterRuleDisplay.Off)
            return;
        string ruleTag = Prefs.ShowFilterRuleNumber.Value == FilterRuleDisplay.NumberOnly
            ? $"<size=120%><b><color={Gold}>Rule#{s_displayNum}</color></b></size>"
            : $"<color={Gold}>Rule #{s_displayNum}: {GetRuleName(s_rule)}</color>";
        destination.text = ruleTag + Marker + "\n" + (destination.text ?? "");
        s_destination = destination;
        s_hadDestination = true;
        s_injected = true;
        if (TooltipPerf.Enabled) TooltipPerf.RuleInjected();
        Dbg.Log($"rule #{s_displayNum} injected");
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
                // matchingRuleNum IS the display number the game uses (same
                // as ground labels); the rules array is in reverse UI order.
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
