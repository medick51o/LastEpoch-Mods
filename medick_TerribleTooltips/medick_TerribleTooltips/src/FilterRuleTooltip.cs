namespace medick_Terrible_Tooltips;
// Main only: dedicated native layout row, no shared caption or manager hooks.
public static class FilterRuleTooltip
{
    internal const string RowName = "TT_FilterRuleLabel";
    private sealed class Owner
    {
        public UITooltipItem Ui;
        public ItemDataUnpacked Item;
        public TextMeshProUGUI Row, Face;
        public SimpleLayoutGroup Group;
        public SimpleLayoutGroup.SimpleLayoutElement Element;
        public float NextMatch, Width, Height, MeasureRetryAt;
        public SimpleLayoutGroup Section;
        public bool PositionPending, Suspended;
        public float PositionRetryAt;

        public FilterRuleDisplay Mode;
        public string Label;
        public long Epoch;

    }
    private static readonly Dictionary<int, Owner> Owners = new();
    private static bool warned;

    [HarmonyPatch(typeof(UITooltipItem), "SetAsItemTooltip")]
    internal static class Patch_SetAsItemTooltip
    {
        private static void Prefix(UITooltipItem __instance, out long __state) { __state = PrepareBinding(__instance); }
        private static void Postfix(UITooltipItem __instance, ItemDataUnpacked item, long __state) { Bind(__instance, item, __state); }
    }
    [HarmonyPatch(typeof(UITooltipItem), "SetAsGroundTooltip")]
    internal static class Patch_SetAsGroundTooltip
    {
        private static void Prefix(UITooltipItem __instance, out long __state) { __state = PrepareBinding(__instance); }
        private static void Postfix(UITooltipItem __instance, ItemDataUnpacked _item, long __state) { Bind(__instance, _item, __state); }
    }
    private static long PrepareBinding(UITooltipItem ui)
    {
        try
        {
            int id = ui.GetInstanceID();
            // Restore OLD binding before native code moves/reuses the panel.
            if (Owners.TryGetValue(id, out var old)) RemoveRow(old);
            Owners.Remove(id);
            long epoch = TooltipRecolor.BeginBinding(ui);

            return epoch;
        }
        catch (Exception ex) { Warn(ex); return 0; }
    }
    private static void Bind(UITooltipItem ui, ItemDataUnpacked item, long epoch)
    {
        try
        {
            int id = ui.GetInstanceID();
            if (!TooltipRecolor.IsCurrentBinding(ui, epoch)) { return; }
            if (Owners.TryGetValue(id, out var old)) RemoveRow(old);
            Owners.Remove(id);
            if (item != null) { Owners[id] = new Owner { Ui = ui, Item = item, Epoch = epoch }; }

            TooltipRecolor.MarkDirty();
        }
        catch (Exception ex) { Warn(ex); }
    }
    public static void MonitorUpdate()
    {

        bool enabled = Prefs.EnableTooltips.Value;
        var mode = Prefs.ShowFilterRuleNumber.Value;
        foreach (var pair in new List<KeyValuePair<int, Owner>>(Owners))
        {
            var owner = pair.Value;
            try
            {
                if (owner.Ui == null || owner.Ui.gameObject == null ||
                    !TooltipRecolor.RetainBinding(owner.Ui, owner.Epoch) ||
                    !TooltipRecolor.IsCurrentBinding(owner.Ui, owner.Epoch))
                {

                    RemoveRow(owner);
                    TooltipRecolor.ForgetBinding(owner.Ui, owner.Epoch);
                    Owners.Remove(pair.Key);
                    continue;
                }
                if (!owner.Ui.gameObject.activeInHierarchy || !owner.Ui.tooltipActive)
                {
                    if (!owner.Suspended)
                    {
                        owner.Suspended = true;
                        RemoveRow(owner); // No stale label on a pooled hidden panel.

                    }
                    continue; // No resolver, measurement or replay while hidden.
                }
                if (owner.Suspended)
                {
                    owner.Suspended = false;
                    owner.NextMatch = 0;
                    TooltipRecolor.MarkDirty();
                    if (enabled) TooltipRecolor.ReRenderNow();

                }

                if (!enabled || mode == FilterRuleDisplay.Off)
                {

                    // Preserve hover context for re-enable without re-hover.
                    RemoveRow(owner);
                    owner.NextMatch = 0;
                    continue;
                }
                // Live filter edits and preferences invalidate within 250 ms.
                if (Time.unscaledTime >= owner.NextMatch || owner.Mode != mode)
                {
                    owner.NextMatch = Time.unscaledTime + 0.25f;
                    owner.Mode = mode;
                    owner.Label = ResolveLabel(owner.Item, mode);
                }
                if (owner.Label == null) { RemoveRow(owner); continue; }
                if (!EnsureOwnedRow(owner)) continue;
                MeasureRow(owner);
                RetryPositioning(owner);

            }
            catch (Exception ex) {  owner.Label = null; RemoveRow(owner); Warn(ex); }
        }
    }
    // Valid epoch and live UI are checked by MonitorUpdate before repair.
    private static bool EnsureOwnedRow(Owner owner)
    {
        bool intact = owner.Row != null && owner.Section != null && owner.Group != null &&
            owner.Row.transform.parent == owner.Section.transform &&
            owner.Section.transform.parent == owner.Group.transform &&
            FindElement(owner.Group, owner.Section.transform.Cast<RectTransform>()) != null &&
            FindElement(owner.Section, owner.Row.rectTransform) != null;
        if (!intact)
        {
            RemoveRow(owner);

            return CreateRow(owner);
        }
        // Refresh the owned registration reference if native registration was replaced.
        owner.Element = FindElement(owner.Group, owner.Section.transform.Cast<RectTransform>());
        if (!owner.Row.gameObject.activeSelf || !owner.Section.gameObject.activeSelf)
        {
            owner.Row.gameObject.SetActive(true);
            owner.Section.gameObject.SetActive(true);
            owner.Width = owner.Height = owner.MeasureRetryAt = 0;

        }
        return true;
    }
    private static void Warn(Exception ex)
    {
        if (warned) return;
        warned = true;
        CleanupStep.Run(() => MelonLogger.Warning("filter rule label unavailable: " + ex.Message), _ => { });
    }
    private static string ResolveLabel(ItemDataUnpacked item, FilterRuleDisplay mode)
        => NativeRuleResolver.Label(item, mode);
    private static bool CreateRow(Owner owner)
    {
        if (owner.Section != null || owner.Element != null) RemoveRow(owner);
        var main = owner.Ui.content;
        var compare = owner.Ui.compareContent;
        if (main == null) return false;
        TextMeshProUGUI face = null;
        foreach (var affix in main.GetComponentsInChildren<UITooltipItemAffix>(true))
        {
            if (affix == null || affix._text == null) continue;
            if (compare != null && affix.transform.IsChildOf(compare.transform)) continue;
            face = affix._text;
            break;
        }
        if (face == null) face = owner.Ui.implicitText;
        if (face == null || !face.transform.IsChildOf(main.transform)) return false;
        var footer = owner.Ui.requirementsFooter;
        if (footer == null) return false;
        SimpleLayoutGroup group = null;
        int order = -1;
        for (var at = footer.transform.parent; at != null; at = at.parent)
        {
            var candidate = at.GetComponent<SimpleLayoutGroup>();
            if (candidate != null && candidate.GroupLayoutMode == SimpleLayoutGroup.LayoutMode.Vertical &&
                face.transform.IsChildOf(at) && (compare == null || !compare.transform.IsChildOf(at)))
            {
                order = FooterOrder(candidate, owner.Ui, face.transform);
                if (order >= 0) { group = candidate; break; }
            }
            if (at == owner.Ui.transform) break;
        }
        if (group == null) { return false; }
        var sectionGo = new GameObject(RowName + "_Section");
        sectionGo.AddComponent<RectTransform>();
        owner.Section = sectionGo.AddComponent<SimpleLayoutGroup>();
        sectionGo.SetActive(false);
        owner.Group = group;
        owner.Face = face;
        sectionGo.transform.SetParent(group.transform, false);
        var sectionRect = sectionGo.transform.Cast<RectTransform>();
        owner.Section._rectTransform = sectionRect;
        sectionRect.anchorMin = sectionRect.anchorMax = sectionRect.pivot = new Vector2(0, 1);
        owner.Section._layoutMode = SimpleLayoutGroup.LayoutMode.Vertical;
        owner.Section._adaptHorizontalSize = false;
        owner.Section._adaptVerticalSize = true;
        owner.Section._padding = NativeInset(group.transform.Cast<RectTransform>(), face, group._padding);
        owner.Section._spacing = 0;
        owner.Section._minHeight = owner.Section._preferredHeight = 0;
        owner.Section._childAlignment = TextAnchor.UpperLeft;
        var go = new GameObject(RowName);
        owner.Row = go.AddComponent<TextMeshProUGUI>();
        var row = owner.Row;
        row.transform.SetParent(sectionGo.transform, false);
        row.font = face.font;
        row.fontSharedMaterial = face.fontSharedMaterial;
        row.fontSize = face.fontSize;
        row.fontStyle = FontStyles.Normal;
        row.color = Color.white;
        row.alignment = TextAlignmentOptions.TopLeft;
        row.margin = Vector4.zero;
        row.richText = true;
        row.raycastTarget = false;
        row.enableAutoSizing = row.autoSizeTextContainer = false;
        row.textWrappingMode = TextWrappingModes.Normal;
        row.overflowMode = TextOverflowModes.Overflow;
        row.rectTransform.anchorMin = row.rectTransform.anchorMax = row.rectTransform.pivot = new Vector2(0, 1);
        owner.Section.AddElement(go);
        var textElement = FindElement(owner.Section, row.rectTransform);
        if (textElement == null) throw new InvalidOperationException("section text registration failed");
        textElement._parentControlsSize = false;
        textElement._expandAcrossAxis = true;
        textElement._excludeIfInactive = true;
        group.AddElement(sectionGo);
        owner.Element = FindElement(group, sectionRect);
        if (owner.Element == null) throw new InvalidOperationException("section registration failed");
        owner.Element._layoutGroupReference = owner.Section;
        // The row owns its measured height, not a share of parent leftover space.
        owner.Element._parentControlsSize = false;
        owner.Element._expandAcrossAxis = true;
        owner.Element._excludeIfInactive = true;
        group.SetItemOrder(sectionGo, order);
        // Registered element order is not a hierarchy sibling index.
        var boundary = group.Elements[order + 1].RectTransformReference;
        sectionGo.transform.SetSiblingIndex(boundary.transform.GetSiblingIndex());
        sectionGo.SetActive(true);
        owner.Width = owner.Height = owner.MeasureRetryAt = 0;
        return true;
    }
    // Reuse the selected native face only; never inspect another tooltip subtree.
    private static RectOffset NativeInset(RectTransform parent, TextMeshProUGUI face, RectOffset padding)
    {
        var zero = new RectOffset(0, 0, 0, 0);
        if (face == null) return zero;
        var rect = face.rectTransform;
        var box = rect.rect;
        var margin = face.margin;
        var a = parent.InverseTransformPoint(rect.TransformPoint(new Vector3(box.xMin + margin.x, box.center.y, 0)));
        var b = parent.InverseTransformPoint(rect.TransformPoint(new Vector3(box.xMax - margin.z, box.center.y, 0)));
        float width = parent.rect.width - (padding == null ? 0 : padding.left + padding.right);
        float left = a.x - parent.rect.xMin - (padding == null ? 0 : padding.left);
        float right = parent.rect.xMax - (padding == null ? 0 : padding.right) - b.x;
        // Fail closed for stale, rotated, inverted or implausibly narrow geometry.
        if (!float.IsFinite(width) || width <= 0 || !float.IsFinite(left) || !float.IsFinite(right) ||
            !float.IsFinite(a.y) || !float.IsFinite(b.y) || Mathf.Abs(a.y - b.y) > 0.5f ||
            left < 0 || right < 0 || left + right > width * 0.5f) return zero;
        return new RectOffset((int)Mathf.Round(left), (int)Mathf.Round(right), 0, 0);
    }
    private static SimpleLayoutGroup.SimpleLayoutElement FindElement(SimpleLayoutGroup group, RectTransform rect)
    {
        foreach (var element in group.Elements)
            if (element.RectTransformReference != null && element.RectTransformReference.GetInstanceID() == rect.GetInstanceID()) return element;
        return null;
    }
    private static int FooterOrder(SimpleLayoutGroup group, UITooltipItem ui, Transform face)
    {
        int index = 0;
        foreach (var element in group.Elements)
        {
            var rect = element.RectTransformReference;
            if (rect != null)
                foreach (var footer in new[] { ui.requirementsFooter, ui.goldValueHolder, ui.soulEmberValueHolder, ui._promptsFooterGo })
                    if (footer != null && (footer.transform == rect || footer.transform.IsChildOf(rect)))
                        return face.IsChildOf(rect) ? -1 : index;
            index++;
        }
        return -1;
    }
    private static void MeasureRow(Owner owner)
    {

        var group = owner.Group;
        var rect = group.transform.Cast<RectTransform>();
        float width = rect.rect.width;
        if (group._padding != null) width -= group._padding.left + group._padding.right;
        float sectionWidth = width;
        var inset = NativeInset(rect, owner.Face, group._padding);
        bool insetChanged = owner.Section._padding.left != inset.left || owner.Section._padding.right != inset.right;
        owner.Section._padding = inset;
        width -= inset.left + inset.right;
        if (!float.IsFinite(width) || width <= 0)
        { return; }
        var row = owner.Row;
        var sectionRect = owner.Section.transform.Cast<RectTransform>();
        bool cached = !insetChanged && row.text == owner.Label && float.IsFinite(owner.Width) &&
            Mathf.Abs(owner.Width - width) <= 0.5f && float.IsFinite(owner.Height) && owner.Height > 0;
        if (cached)
        {
            var leaf = row.rectTransform.rect;
            var section = sectionRect.rect;
            if (float.IsFinite(leaf.width) && float.IsFinite(leaf.height) &&
                float.IsFinite(section.width) && float.IsFinite(section.height) &&
                Mathf.Abs(leaf.width - width) <= 0.5f && Mathf.Abs(leaf.height - owner.Height) <= 0.5f &&
                Mathf.Abs(section.width - sectionWidth) <= 0.5f && Mathf.Abs(section.height - owner.Height) <= 0.5f) return;
            // Persistent native disagreement must not cause per-frame replay storms.
            if (Time.unscaledTime < owner.MeasureRetryAt) return;
        }
        Vector2 preferred = row.GetPreferredValues(owner.Label, width, float.PositiveInfinity);
        if (!float.IsFinite(preferred.x) || preferred.x <= 0 || !float.IsFinite(preferred.y) || preferred.y <= 0) return;
        float height = Mathf.Ceil(preferred.y);
        if (!float.IsFinite(height) || height <= 0) return;
        // Commit only validated measurements. Native section owns its vertical adaptation.
        row.text = owner.Label;
        owner.Width = width;
        owner.Height = height;
        owner.MeasureRetryAt = Time.unscaledTime + 0.25f;
        row.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        row.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        sectionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sectionWidth);
        row.ForceMeshUpdate();
        Refresh(owner);

    }
    private static void Refresh(Owner owner)
    {
        if (owner.Section != null) owner.Section.UpdateLayout();
        // Recompute the actual registered body/footer flow before root positioning.
        if (owner.Group != null) owner.Group.UpdateLayout();
        bool replayed = TooltipRecolor.RequestRelayoutFor(owner.Ui, owner.Epoch);
        owner.PositionPending = owner.Row != null && !replayed;
        owner.PositionRetryAt = Time.unscaledTime + 0.25f;

    }
    public static void ReleaseAll()
    {
        try
        {
            foreach (var owner in new List<Owner>(Owners.Values))
                Cleanup(() => RemoveRow(owner));
        }
        finally
        {
            Owners.Clear();

        }
    }
    private static bool cleanupWarned;
    private static void Cleanup(Action step) => CleanupStep.Run(step, ex =>
    {
        if (cleanupWarned) return;
        cleanupWarned = true;
        MelonLogger.Warning("filter rule label cleanup incomplete: " + ex.Message);
    });
    private static void RemoveRow(Owner owner)
    {
        try
        {
            Cleanup(() => { if (owner.Section != null) owner.Section.gameObject.SetActive(false); });
            Cleanup(() => { if (owner.Group != null && owner.Element != null) owner.Group.RemoveElement(owner.Element); });
            Cleanup(() => { if (owner.Section != null) UnityEngine.Object.Destroy(owner.Section.gameObject); });
            owner.Section = null;
            Cleanup(() => { if (owner.Group != null) Refresh(owner); });
        }
        finally
        {
            owner.PositionPending = false;
            owner.PositionRetryAt = 0;
            owner.Row = null;
            owner.Face = null;
            owner.Section = null;
            owner.Group = null;
            owner.Element = null;
            owner.Width = owner.Height = owner.MeasureRetryAt = 0;
        }
    }
    // Positioning success is separate from coherent measured geometry.
    // Bound failed attempts to four per second; retain arbitrarily late captures.
    private static void RetryPositioning(Owner owner)
    {
        if (!owner.PositionPending) return;
        if (owner.Row == null || !TooltipRecolor.IsCurrentBinding(owner.Ui, owner.Epoch))
        { owner.PositionPending = false; owner.PositionRetryAt = 0; return; }
        if (Time.unscaledTime < owner.PositionRetryAt) return;
        owner.PositionRetryAt = Time.unscaledTime + 0.25f;
        if (TooltipRecolor.RequestRelayoutFor(owner.Ui, owner.Epoch))
            owner.PositionPending = false;
    }
}