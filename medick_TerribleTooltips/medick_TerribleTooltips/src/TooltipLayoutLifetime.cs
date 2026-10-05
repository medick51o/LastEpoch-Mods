namespace medick_Terrible_Tooltips;

public static partial class TooltipRecolor
{
    private static readonly BindingLayoutCache<ArgumentSnapshot> s_layoutCache = new();
    private static readonly Dictionary<int, UITooltipItem> s_layoutUis = new();
    private static readonly Dictionary<int, (UITooltipItem Ui, long Epoch)> s_originalOwners = new();

    private static bool SameUi(UITooltipItem a, UITooltipItem b)
        => a != null && b != null && a.Pointer == b.Pointer;
    private static bool LiveUi(UITooltipItem ui)
        => ui != null && ui.gameObject != null && ui.gameObject.activeInHierarchy && ui.tooltipActive;

    // Bounded suspension, shared by the row owner and every epoch pruner.
    // Rebinding/destroy/scene release still invalidates immediately.
    internal static bool RetainBinding(UITooltipItem ui, long epoch)
    {
        if (!IsCurrentBinding(ui, epoch) || ui.gameObject == null) return false;
        return s_layoutCache.Retain(ui.GetInstanceID(), epoch, LiveUi(ui), Time.unscaledTime, 3f);
    }
    internal static long BeginBinding(UITooltipItem ui)
    {
        int id = ui.GetInstanceID();
        s_layoutUis[id] = ui;
        long epoch = s_layoutCache.BeginBinding(id);
        PruneOriginalOwners();
        return epoch;
    }
    private static long CaptureEpoch(UITooltipItem ui)
    {
        int id = ui.GetInstanceID();
        if (s_layoutUis.TryGetValue(id, out var old) && !SameUi(old, ui))
            s_layoutCache.Forget(id);
        s_layoutUis[id] = ui;
        return s_layoutCache.CaptureCurrentEpoch(id);
    }
    internal static bool IsCurrentBinding(UITooltipItem ui, long epoch)
    {
        if (ui == null || epoch == 0) return false;
        int id = ui.GetInstanceID();
        return s_layoutUis.TryGetValue(id, out var tracked) && SameUi(tracked, ui) &&
            s_layoutCache.TryGetCurrentEpoch(id, out long current) && current == epoch;
    }
    private static bool CaptureLayout(UITooltipItem ui, long epoch, object[] args)
    {
        if (!IsCurrentBinding(ui, epoch) || args == null) return false;
        return s_layoutCache.TryCapture(ui.GetInstanceID(), epoch, new ArgumentSnapshot(args));
    }
    internal static bool RequestRelayoutFor(UITooltipItem ui, long epoch)
    {
        if (s_relayouting) return false;
        try
        {
            if (!LiveUi(ui) || !IsCurrentBinding(ui, epoch) ||
                !s_layoutCache.TryGet(ui.GetInstanceID(), epoch, out var snapshot)) return false;
            var args = snapshot.CopyArguments();
            if (args.Length != 3 || args[0] is not Vector3 position || args[1] is not Vector2 size ||
                (args[2] != null && args[2] is not RectTransform)) return false;
            var relative = args[2] as RectTransform;
            // A boxed destroyed Unity object is not a valid live positioning target.
            if (args[2] != null && relative == null) return false;
            s_relayouting = true;
            try { ui.UpdateLayout(position, size, relative); return true; }
            finally { s_relayouting = false; }
        }
        catch { return false; }
    }
    internal static void ForgetBinding(UITooltipItem ui, long epoch)
    {
        if (!IsCurrentBinding(ui, epoch)) return;
        int id = ui.GetInstanceID();
        s_layoutCache.Forget(id);
        s_layoutUis.Remove(id);
    }
    private static void PruneLayouts()
    {
        foreach (var pair in new List<KeyValuePair<int, UITooltipItem>>(s_layoutUis))
        {
            bool live = false;
            try
            {
                live = s_layoutCache.TryGetCurrentEpoch(pair.Key, out long epoch) &&
                    RetainBinding(pair.Value, epoch);
            }
            catch { }
            if (live) continue;
            s_layoutCache.Forget(pair.Key);
            s_layoutUis.Remove(pair.Key);
        }
        PruneOriginalOwners();
    }
    private static void PruneOriginalOwners()
    {
        foreach (var pair in new List<KeyValuePair<int, (UITooltipItem Ui, long Epoch)>>(s_originalOwners))
        {
            bool current = false;
            try { current = RetainBinding(pair.Value.Ui, pair.Value.Epoch); } catch { }
            if (current) continue;
            s_originalOwners.Remove(pair.Key);
            s_originals.Remove(pair.Key);
            s_suppressedRanges.Remove(pair.Key);
        }
    }
    private static void RecordOriginal(TextMeshProUGUI tmp, string text,
        Dictionary<int, (UITooltipItem Ui, long Epoch)> affected)
    {
        int id = tmp.GetInstanceID();
        s_originals[id] = (tmp, text);
        s_originalOwners.Remove(id);
        // Closest actual tooltip ancestor; ordinary recolor-only owners count too.
        var ui = tmp.GetComponentInParent<UITooltipItem>();
        if (ui == null) return;
        long epoch = CaptureEpoch(ui);
        s_originalOwners[id] = (ui, epoch);
        affected[ui.GetInstanceID()] = (ui, epoch);
    }
    private static void AddOriginalOwner(int id,
        Dictionary<int, (UITooltipItem Ui, long Epoch)> affected)
    {
        if (s_originalOwners.TryGetValue(id, out var owner) && IsCurrentBinding(owner.Ui, owner.Epoch))
            affected[owner.Ui.GetInstanceID()] = owner;
    }
    private static void ReplayOwners(Dictionary<int, (UITooltipItem Ui, long Epoch)> affected)
    {
        foreach (var owner in affected.Values) RequestRelayoutFor(owner.Ui, owner.Epoch);
    }
    private static bool OriginalMaySurviveHidden(int id)
    {
        return s_originalOwners.TryGetValue(id, out var owner) &&
            RetainBinding(owner.Ui, owner.Epoch) && !LiveUi(owner.Ui);
    }
    private static bool OriginalIsCurrent(int id, TextMeshProUGUI tmp)
    {
        if (!s_originalOwners.TryGetValue(id, out var owner) || !IsCurrentBinding(owner.Ui, owner.Epoch))
            return false;
        return LiveUi(owner.Ui) && SameUi(owner.Ui, tmp.GetComponentInParent<UITooltipItem>());
    }
    private static bool AnyLiveTooltip()
    {
        foreach (var ui in s_layoutUis.Values)
            try { if (LiveUi(ui)) return true; } catch { }
        return false;
    }
    internal static void ReleaseLayoutContexts()
    {
        try { RestoreVanillaOnMasterOff(); }
        finally
        {
            s_layoutCache.Clear();
            s_layoutUis.Clear();
            s_originalOwners.Clear();
        }
    }
}