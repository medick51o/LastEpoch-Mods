internal static partial class Program
{
    private const string Bracket = "[<color=#FA9E3D>A</color>] +10 Health";
    private static void Attach(Transform root, params TextMeshProUGUI[] rows)
    {
        foreach (var row in rows) row.transform.parent = root;
        UnityEngine.Object.Scene = UnityEngine.Object.Scene.Concat(rows).Distinct().ToArray();
    }
    private static void Layout(UITooltipItem ui)
        => Call(typeof(TooltipRecolor.Patch_UpdateLayout), "Postfix", ui, Args);

    private static void ScopedRegression()
    {
        var ui = Reset(); Prefs.DebugLog.Value = true;
        var main = new TextMeshProUGUI(Bracket);
        var comparison = new TextMeshProUGUI(Bracket);
        var blessing = new TextMeshProUGUI(Bracket);
        var noise = new TextMeshProUGUI(Bracket + " unrelated HUD");
        var inactive = new TextMeshProUGUI(Bracket); inactive.gameObject.activeInHierarchy = false;
        // Deliberately OUTSIDE ui.transform and inside a second UITooltipItem.
        var second = new UITooltipItem();
        ui.compareContent = new GameObject("comparison");
        ui.compareContent.transform.parent = second.transform;
        ui.blessingCompareContent = new GameObject("blessing comparison");
        Attach(ui.transform, main, inactive);
        Attach(ui.compareContent.transform, comparison);
        Attach(ui.blessingCompareContent.transform, blessing);
        Attach(new Transform(), noise);
        Scan(ui);
        Check(comparison.text.EndsWith(Marker, StringComparison.Ordinal) && blessing.text.EndsWith(Marker, StringComparison.Ordinal),
            "detached item and blessing comparison TMPs compose, including a second instance's panel");
        Check(main.text.EndsWith(Marker, StringComparison.Ordinal) && noise.text == Bracket + " unrelated HUD" && inactive.text == Bracket,
            "scoped scan composes main but never unrelated HUD or inactive pooled rows");
        Check(UnityEngine.Object.SceneCalls == 0 && Field<long>(typeof(TooltipPerf), "s_tmps") == 4 &&
              Field<long>(typeof(TooltipPerf), "s_scoped") == 1 && Field<long>(typeof(TooltipPerf), "s_fullScene") == 0,
            "scoped scan examines exactly four hierarchy TMPs, no whole-scene enumeration");
        At(300, 5f); TooltipPerf.Tick();
        Check(MelonLogger.Lines.Last().Contains("scoped=1, fullScene=0, tmps=4"), "new cost counters appear in five-second summary");
        At(600, 10f); TooltipPerf.Tick();
        Check(MelonLogger.Lines.Last().Contains("scoped=0, fullScene=0, tmps=0"), "new counters reset with window");

        ui = Reset(); Prefs.DebugLog.Value = true;
        ui.content = new GameObject("content"); ui.content.transform.parent = ui.transform;
        ui.compareContent = ui.content;
        main = new TextMeshProUGUI(Bracket); Attach(ui.content.transform, main);
        Scan(ui);
        Check(Field<long>(typeof(TooltipPerf), "s_tmps") == 1 && ui.content.transform.ChildCalls == 0,
            "nested/aliased panel roots are enumerated only once");

        ui = Reset(); second = new UITooltipItem();
        main = new TextMeshProUGUI(Bracket); comparison = new TextMeshProUGUI(Bracket);
        Attach(ui.transform, main); Attach(second.transform, comparison);
        TooltipRecolor.MarkDirty(); Layout(ui); Layout(second);
        Check(!comparison.text.EndsWith(Marker, StringComparison.Ordinal), "fixture proves second instance was not accidentally in first scope");
        At(1, .02f); TooltipRecolor.OnLateUpdate();
        Check(comparison.text.EndsWith(Marker, StringComparison.Ordinal) && second.LayoutCalls > 0,
            "same-frame gated second instance is retained for LateUpdate composition and relayout");
        second.tooltipActive = false;
        main.text = Bracket; At(2, .04f); TooltipRecolor.OnLateUpdate();
        Check(main.text.EndsWith(Marker, StringComparison.Ordinal), "last observed comparison can close without disabling main catch-up");
        Input.Alt = true; second.tooltipActive = true; Layout(second); At(3, .06f); TooltipRecolor.OnLateUpdate();
        int mainLayouts = ui.LayoutCalls, secondLayouts = second.LayoutCalls;
        Prefs.EnableTooltips.Value = false; At(4, .08f); TooltipRecolor.OnLateUpdate();
        Check(main.text == Bracket && comparison.text == Bracket && ui.LayoutCalls > mainLayouts && second.LayoutCalls > secondLayouts,
            "master-off restores and relayouts both observed instances");

        foreach (bool nested in new[] { false, true })
        {
            ui = Reset(); Prefs.AlwaysShowRanges.Value = true;
            ui.compareContent = new GameObject("comparison");
            var group = new Transform { parent = ui.compareContent.transform };
            var rangeParent = nested ? new Transform { parent = group } : group;
            main = new TextMeshProUGUI(Bracket); var range = new TextMeshProUGUI("Range: 1 to 10");
            Attach(group, main); Attach(rangeParent, range); Scan(ui);
            Check(range.text.Contains("<color=#FA9E3D>Range: 1 to 10"),
                "comparison range inherits grade through " + (nested ? "grandparent" : "sibling parent"));
            range.text = "Range: 2 to 12"; At(1, .02f); Scan(ui);
            Check(range.text.Contains("<color=#FA9E3D>Range: 2 to 12"), "range rewrite inherits already composed sibling grade");
        }

        ui = Reset(); Prefs.DebugLog.Value = true; ui.transform.FailChildren = true;
        main = new TextMeshProUGUI(Bracket); Attach(ui.transform, main);
        for (int i = 0; i < 30; i++) { At(i, i / 60f); Scan(ui); }
        Check(UnityEngine.Object.SceneCalls == 1 && Field<long>(typeof(TooltipPerf), "s_fullScene") == 1 &&
              Field<long>(typeof(TooltipPerf), "s_scoped") == 0,
            "broken hierarchy cannot trigger whole-scene search per dirty frame");
        At(30, .5f); Scan(ui);
        Check(UnityEngine.Object.SceneCalls == 2, "explicit full-scene compatibility fallback allowed again after 0.5 seconds");
        ui.transform.FailChildren = false; At(31, .52f); Scan(ui);
        Check(UnityEngine.Object.SceneCalls == 2 && Field<long>(typeof(TooltipPerf), "s_scoped") == 1,
            "recovered hierarchy immediately returns to scoped fast path");
        ui = Reset(); Scan(ui);
        Check(UnityEngine.Object.SceneCalls == 0, "valid empty tooltip never justifies full-scene fallback");

        ui = Reset(); Prefs.DebugLog.Value = true;
        comparison = new TextMeshProUGUI(Bracket);
        Attach(new Transform(), comparison);
        ui.comparePrefixes = new() { new UITooltipItemAffix { _text = comparison } };
        Scan(ui);
        Check(UnityEngine.Object.SceneCalls == 1 && comparison.text.EndsWith(Marker, StringComparison.Ordinal) &&
              Field<long>(typeof(TooltipPerf), "s_scoped") == 0,
            "referenced comparison affix outside every panel triggers visible compatibility fallback");

        ui = Reset(); Prefs.DebugLog.Value = true; Prefs.AlwaysShowRanges.Value = true;
        var externalParent = new Transform(); ui.transform.parent = externalParent;
        var edgeRange = new TextMeshProUGUI("Range: 3 to 9"); main = new TextMeshProUGUI(Bracket);
        Attach(ui.transform, edgeRange); Attach(externalParent, main);
        Scan(ui);
        Check(UnityEngine.Object.SceneCalls == 1 && edgeRange.text.Contains("<color=#FA9E3D>Range: 3 to 9") &&
              Field<long>(typeof(TooltipPerf), "s_tmps") == 3,
            "range grade outside scope triggers fallback and counts partial plus full-scene work honestly");

        ui = Reset(); var shard = new TextMeshProUGUI("Crafting material"); Attach(ui.transform, shard);
        for (int i = 0; i < 30; i++) { At(i, i); Scan(ui); }
        Check(!MelonLogger.Lines.Any(x => x.Contains("formatter hook may be dead")), "shard-only scoped scans never false-warn");
        main = new TextMeshProUGUI("+10 Health\nTier: 5"); Attach(ui.transform, main);
        var elsewhere = new TextMeshProUGUI(Bracket); Attach(new Transform(), elsewhere);
        for (int i = 0; i < 20; i++) { At(30+i, 30+i); Scan(ui); }
        Check(MelonLogger.Lines.Count(x => x.Contains("formatter hook may be dead")) == 1,
            "dead tiered formatter warns despite synthesized marker and brackets elsewhere in scene");
        ui = Reset(); main = new TextMeshProUGUI(Bracket); Attach(ui.transform, main);
        for (int i = 0; i < 30; i++) { At(i, i); Scan(ui); }
        Check(!MelonLogger.Lines.Any(x => x.Contains("formatter hook may be dead")) &&
              Field<int>(typeof(TooltipRecolor), "s_emptyBracketScans") == 0,
            "real bracket evidence survives composition through saved originals");
    }
}
