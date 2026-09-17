// Controlled stubs only: compiled with the actual beta TooltipRecolor.cs,
// FilterRuleTooltip.cs and Colors.cs. Not an Il2Cpp/Unity integration test.
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Reflection;
global using HarmonyLib;
global using Il2Cpp;
global using Il2CppItemFiltering;
global using Il2CppTMPro;
global using MelonLoader;
global using UnityEngine;
global using medick_Terrible_Tooltips;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute
    { public HarmonyPatch(Type type, string method) { } }
}
namespace MelonLoader
{
    public static class MelonLogger
    {
        public static readonly List<string> Lines = new();
        public static void Msg(string text) => Lines.Add(text);
        public static void Warning(string text) => Lines.Add(text);
    }
}
namespace UnityEngine
{
    public class Object
    {
        private static int next;
        private readonly int id = ++next;
        public int GetInstanceID() => id;
        public static TextMeshProUGUI[] Scene = Array.Empty<TextMeshProUGUI>();
        public static int SceneCalls;
        public static bool FailScan;
        public static T[] FindObjectsOfType<T>()
        {
            SceneCalls++;
            if (FailScan) throw new Exception("injected scene failure");
            return (T[])(object)Scene;
        }
    }
    public class GameObject : Object
    {
        public string name;
        public bool activeInHierarchy = true;
        public GameObject(string value = "target") { name = value; }
    }
    public class Transform : Object
    {
        public Transform parent;
        public bool IsChildOf(Transform other) => parent == other || parent?.IsChildOf(other) == true;
    }
    public class RectTransform : Transform { }
    public struct Vector2 { }
    public struct Vector3 { }
    public struct Color { }
    public static class ColorUtility
    { public static bool TryParseHtmlString(string value, out Color color) { color = default; return true; } }
    public enum KeyCode { LeftAlt, RightAlt }
    public static class Input { public static bool Alt; public static bool GetKey(KeyCode key) => Alt; }
    public static class Time
    {
        public static int Frame, ClockReads;
        public static float Now;
        public static int frameCount => Frame;
        public static float unscaledTime { get { ClockReads++; return Now; } }
    }
}
namespace Il2CppTMPro
{
    public class TextMeshProUGUI : UnityEngine.Object
    {
        public readonly GameObject gameObject;
        public readonly Transform transform = new();
        public string text;
        public Color color;
        public TextMeshProUGUI(string value = "", string name = "affix")
        { text = value; gameObject = new GameObject(name); }
    }
}
namespace Il2Cpp
{
    public class ItemDataUnpacked
    {
        public byte[] id = new byte[] { 1, 2, 3 };
        public byte itemType, rarity;
        public ushort subType, uniqueID, individualID;
    }
    public class UITooltipItem : UnityEngine.Object
    {
        public static UITooltipItem instance;
        public bool tooltipActive = true;
        public GameObject target = new();
        public int targetType;
        public readonly Transform transform = new();
        public TextMeshProUGUI[] Rows = Array.Empty<TextMeshProUGUI>();
        public int ChildScans, LayoutCalls;
        public bool FailChildren;
        public Action AfterLayout;
        public T[] GetComponentsInChildren<T>()
        {
            ChildScans++;
            if (FailChildren) throw new Exception("injected child failure");
            return (T[])(object)Rows.Where(t => t.gameObject.activeInHierarchy).ToArray();
        }
        public void UpdateLayout(Vector3 position, Vector2 offset, RectTransform additional)
        { LayoutCalls++; AfterLayout?.Invoke(); }
    }
    public static class TooltipItemManager { public static bool showRangesInsteadOfDescriptionEnabled; }
}
namespace Il2CppItemFiltering
{
    public class Rule
    {
        public enum RuleOutcome { HIDE, SHOW }
        public bool isEnabled = true;
        public string nameOverride = "test rule";
        public bool Match(ItemDataUnpacked item, int value) { return true; }
        public string GetRuleDescription() => nameOverride;
    }
    public class ItemFilter
    {
        public List<Rule> rules = new() { new Rule() };
        public int Calls;
        public Rule.RuleOutcome Match(ItemDataUnpacked item, out bool a, out bool b,
            out int matching, out bool c, out bool d, out bool e, out bool f, out bool g)
        { Calls++; a = b = c = d = e = f = g = false; matching = rules.Count; return Rule.RuleOutcome.SHOW; }
    }
    public class ItemFilterManager
    { public static ItemFilterManager Instance = new(); public ItemFilter Filter = new(); }
}
namespace medick_Terrible_Tooltips
{
    public class Setting<T> { public T Value; public Setting(T value) { Value = value; } }
    public enum FilterRuleDisplay { Off, NumberOnly, NumberAndName }
    public enum AffixNameColorMode { GameDefault, TierColor, GreaterAffix }
    public enum SignalStyle { Badge, PlainText }
    public enum TooltipLayout { BadgeLeft, Trailing, SignalRight }
    public enum TierWordStyle { Spelled, Compact }
    public enum UnitSeparatorStyle { Dot, Bar }
    public enum DividerStyle { Strip, Glyph }
    public static class Prefs
    {
        public static Setting<bool> DebugLog = new(false), EnableTooltips = new(true),
            AlwaysShowRanges = new(false), AlwaysShowTierDetails = new(false),
            TooltipTierColors = new(true), TooltipRankColors = new(true), ShowGradeLetters = new(true);
        public static Setting<FilterRuleDisplay> ShowFilterRuleNumber = new(FilterRuleDisplay.NumberOnly);
        public static Setting<AffixNameColorMode> NameColorMode = new(AffixNameColorMode.GreaterAffix);
        public static Setting<SignalStyle> Style = new(SignalStyle.PlainText);
        public static Setting<TooltipLayout> Layout = new(TooltipLayout.BadgeLeft);
        public static Setting<TierWordStyle> TierWord = new(TierWordStyle.Spelled);
        public static Setting<UnitSeparatorStyle> UnitSeparator = new(UnitSeparatorStyle.Bar);
        public static Setting<DividerStyle> DividerStyle = new(global::medick_Terrible_Tooltips.DividerStyle.Strip);
        public static Setting<string> GreaterAffixTint = new("#C990FF");
    }
    public static class GroundLabels { public const string Marker = "\u200B\u200B\u200B"; }
    public static class Dbg
    { public static void Log(string text) { if (Prefs.DebugLog.Value) MelonLogger.Msg("[debug] " + text); } }
}

internal static class Program
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private static int passed;
    private static object Call(Type type, string method, params object[] args)
        => type.GetMethod(method, PrivateStatic).Invoke(null, args);
    private static T Field<T>(Type type, string name) => (T)type.GetField(name, PrivateStatic).GetValue(null);
    private static void Set(Type type, string name, object value) => type.GetField(name, PrivateStatic).SetValue(null, value);
    private static void Check(bool condition, string description)
    { if (!condition) throw new Exception(description); passed++; Console.WriteLine("PASS " + description); }
    private static Dictionary<int, (TextMeshProUGUI tmp, string original)> Originals
        => Field<Dictionary<int, (TextMeshProUGUI tmp, string original)>>(typeof(TooltipRecolor), "s_originals");
    private static readonly string Marker = new('\u200B', 4);
    private static readonly object[] Args = { new Vector3(), new Vector2(), null };
    private static UITooltipItem Reset()
    {
        Prefs.DebugLog.Value = false;
        TooltipPerf.Tick();
        Call(typeof(FilterRuleTooltip), "ClearPending");
        Originals.Clear();
        Field<Dictionary<int, TextMeshProUGUI>>(typeof(TooltipRecolor), "s_suppressedRanges").Clear();
        Field<List<(TextMeshProUGUI tmp, Color color)>>(typeof(TooltipRecolor), "s_tierColorCache").Clear();
        Set(typeof(TooltipRecolor), "s_dirtyUntilFrame", -1);
        Set(typeof(TooltipRecolor), "s_lastScanFrame", -1);
        Set(typeof(TooltipRecolor), "s_lastScanTime", -1f);
        Set(typeof(TooltipRecolor), "s_altHeld", false);
        Set(typeof(TooltipRecolor), "s_masterPreviouslyEnabled", null);
        Set(typeof(TooltipRecolor), "s_nativeRangesOriginal", null);
        Set(typeof(TooltipRecolor), "s_nativeRangesWasEnabled", false);
        Time.Frame = 0; Time.Now = 0;
        Input.Alt = false;
        TooltipItemManager.showRangesInsteadOfDescriptionEnabled = false;
        Prefs.EnableTooltips.Value = true;
        Prefs.ShowFilterRuleNumber.Value = FilterRuleDisplay.NumberOnly;
        Prefs.AlwaysShowRanges.Value = Prefs.AlwaysShowTierDetails.Value = false;
        UnityEngine.Object.Scene = Array.Empty<TextMeshProUGUI>();
        UnityEngine.Object.SceneCalls = 0; UnityEngine.Object.FailScan = false;
        ItemFilterManager.Instance = new();
        MelonLogger.Lines.Clear();
        var ui = UITooltipItem.instance = new UITooltipItem();
        Set(typeof(TooltipRecolor), "s_lastTooltip", ui);
        Set(typeof(TooltipRecolor), "s_lastArgs", Args);
        return ui;
    }
    private static void Capture(UITooltipItem ui, ItemDataUnpacked item)
        => Call(typeof(FilterRuleTooltip), "Capture", ui, item);
    private static void At(int frame, float seconds) { Time.Frame = frame; Time.Now = seconds; }
    private static void Scan(UITooltipItem ui)
        => Call(typeof(TooltipRecolor), "RunScan", ui, Args, TooltipRecolor.ScanTrigger.MarkerLoss);
    private static TooltipRecolor.ScanTrigger Gate()
        => (TooltipRecolor.ScanTrigger)Call(typeof(TooltipRecolor), "ShouldScan");

    private static void Main()
    {
        var ui = Reset();
        Prefs.DebugLog.Value = true;
        for (int frame = 0; frame < 300; frame++)
        {
            At(frame, frame / 60f);
            Capture(ui, new ItemDataUnpacked()); // new wrapper, equal serialization
            FilterRuleTooltip.MonitorUpdate();
            FilterRuleTooltip.MonitorUpdate(); // duplicate call in same frame
        }
        Check(ui.ChildScans == 13, "missing target: at most 13 discovery attempts despite repeated equal-content setters");
        Check(ItemFilterManager.Instance.Filter.Calls == 1, "missing target: rule matching exactly once");
        Check(Field<long>(typeof(TooltipPerf), "s_ruleStart") == 1 &&
              Field<long>(typeof(TooltipPerf), "s_ruleGiveUp") == 1 &&
              Field<long>(typeof(TooltipPerf), "s_ruleNoTarget") == 13,
              "telemetry proves one generation, missing destinations and terminal give-up");
        At(300, 5f); TooltipPerf.Tick();
        string summary = MelonLogger.Lines.Single(x => x.StartsWith("[perf]"));
        Check(summary.Contains("ruleMatch=1") && summary.Contains("ruleGiveUp=1"), "summary includes decisive retry counters");
        for (int frame = 301; frame < 600; frame++) { At(frame, frame / 60f); TooltipPerf.Tick(); }
        Check(MelonLogger.Lines.Count(x => x.StartsWith("[perf]")) == 1, "summary rate limited through next five-second boundary");
        At(600, 10f); TooltipPerf.Tick();
        Check(MelonLogger.Lines.Last().Contains("ruleMatch=0") && MelonLogger.Lines.Last().Contains("ruleAttempts=0"), "idle heartbeat resets counters");

        ui = Reset();
        var requires = new TextMeshProUGUI("Level 5", "requires");
        requires.transform.parent = ui.transform;
        requires.gameObject.activeInHierarchy = false;
        ui.Rows = new[] { requires };
        for (int frame = 0; frame <= 7; frame++)
        {
            At(frame, frame / 60f);
            if (frame == 7) requires.gameObject.activeInHierarchy = true;
            Capture(ui, new ItemDataUnpacked());
            FilterRuleTooltip.MonitorUpdate();
        }
        Check(requires.text.Contains("Rule#1") && requires.text.EndsWith("Level 5"), "target activated at frame 7 still receives Rule# and original requirements");
        Check(ItemFilterManager.Instance.Filter.Calls == 1, "late target does not repeat matching");
        requires.text = "Level 5";
        Capture(ui, new ItemDataUnpacked());
        FilterRuleTooltip.MonitorUpdate();
        Check(requires.text.Contains("Rule#1"), "successful same-content setter rewrite reapplies cached Rule#");
        string once = requires.text; Capture(ui, new ItemDataUnpacked()); FilterRuleTooltip.MonitorUpdate();
        Check(requires.text == once, "same-content setter does not stack existing tag");
        Prefs.EnableTooltips.Value = false;
        requires.text = "Level 5";
        Capture(ui, new ItemDataUnpacked());
        FilterRuleTooltip.MonitorUpdate();
        Check(requires.text == "Level 5", "master-off prevents cached Rule# reapplication");
        Prefs.EnableTooltips.Value = true;
        At(30, 1f); FilterRuleTooltip.MonitorUpdate();
        Check(requires.text.Contains("Rule#1"), "known destination repair survives master-off and resumes after original deadline");
        Prefs.ShowFilterRuleNumber.Value = FilterRuleDisplay.Off;
        requires.text = "Level 5"; Capture(ui, new ItemDataUnpacked()); FilterRuleTooltip.MonitorUpdate();
        Prefs.ShowFilterRuleNumber.Value = FilterRuleDisplay.NumberOnly;
        FilterRuleTooltip.MonitorUpdate();
        Check(requires.text.Contains("Rule#1"), "known destination repair survives rule-display off/on without another setter");

        Call(typeof(FilterRuleTooltip), "ClearPending"); // closed/reopened with old tag retained
        Capture(ui, new ItemDataUnpacked()); FilterRuleTooltip.MonitorUpdate();
        int matchesBeforeRepair = ItemFilterManager.Instance.Filter.Calls;
        requires.text = "Level 5"; Capture(ui, new ItemDataUnpacked());
        At(90, 2f); FilterRuleTooltip.MonitorUpdate();
        Check(requires.text.Contains("Rule#1") && ItemFilterManager.Instance.Filter.Calls == matchesBeforeRepair + 1,
              "persisted-marker reuse resolves rule once when rewritten after discovery deadline");
        int scansBeforeRepair = ui.ChildScans;
        requires.text = "Level 5"; Capture(ui, new ItemDataUnpacked()); FilterRuleTooltip.MonitorUpdate();
        Check(ui.ChildScans == scansBeforeRepair && ItemFilterManager.Instance.Filter.Calls == matchesBeforeRepair + 1,
              "known-target repairs neither rediscover descendants nor repeat matching");

        foreach (string invalidation in new[] { "inactive", "detached", "renamed" })
        {
            ui = Reset(); Prefs.DebugLog.Value = true;
            var oldRow = new TextMeshProUGUI("Level 5", "requires"); oldRow.transform.parent = ui.transform;
            ui.Rows = new[] { oldRow }; Capture(ui, new ItemDataUnpacked()); FilterRuleTooltip.MonitorUpdate();
            if (invalidation == "inactive") oldRow.gameObject.activeInHierarchy = false;
            if (invalidation == "detached") oldRow.transform.parent = null;
            if (invalidation == "renamed") { oldRow.gameObject.name = "lore"; oldRow.text = "Flavor text"; }
            var newRow = new TextMeshProUGUI("Level 7", "requires"); newRow.transform.parent = ui.transform;
            ui.Rows = new[] { newRow };
            At(40, 1f); Capture(ui, new ItemDataUnpacked()); FilterRuleTooltip.MonitorUpdate();
            Check(newRow.text.Contains("Rule#1") && ItemFilterManager.Instance.Filter.Calls == 1,
                  invalidation + " previously successful row is replaced without rematching");
            Check(Field<long>(typeof(TooltipPerf), "s_ruleReplacement") == 1,
                  invalidation + " destination invalidation is visible as one bounded replacement window");
            if (invalidation == "renamed") Check(oldRow.text == "Flavor text", "repurposed cached row never receives Rule#");
            newRow.gameObject.activeInHierarchy = false;
            ui.Rows = Array.Empty<TextMeshProUGUI>();
            for (int frame = 41; frame < 150; frame++)
            { At(frame, 1f + (frame - 40) / 60f); Capture(ui, new ItemDataUnpacked()); FilterRuleTooltip.MonitorUpdate(); }
            Check(ui.ChildScans == 15 && ItemFilterManager.Instance.Filter.Calls == 1,
                  invalidation + " missing replacement remains bounded despite equal-content setters");
        }

        ui = Reset();
        for (int frame = 0; frame < 60; frame++)
        { At(frame, frame / 60f); Capture(ui, new ItemDataUnpacked { id = null, subType = 7 }); FilterRuleTooltip.MonitorUpdate(); }
        Check(ui.ChildScans == 13, "ID-less equal-content wrappers retain retry bound");
        Capture(ui, new ItemDataUnpacked { id = null, subType = 8 }); FilterRuleTooltip.MonitorUpdate();
        Check(ui.ChildScans == 14, "ID-less changed material subtype starts a fresh generation");

        ui = Reset();
        Capture(ui, new ItemDataUnpacked()); FilterRuleTooltip.MonitorUpdate();
        ui.target = new GameObject();
        Prefs.ShowFilterRuleNumber.Value = FilterRuleDisplay.Off;
        FilterRuleTooltip.MonitorUpdate();
        Check(Field<ItemDataUnpacked>(typeof(FilterRuleTooltip), "s_pendingItem") == null, "target reuse clears stale pending ownership even with rule display off");
        Capture(ui, new ItemDataUnpacked());
        ui.tooltipActive = false; FilterRuleTooltip.MonitorUpdate();
        Check(Field<ItemDataUnpacked>(typeof(FilterRuleTooltip), "s_pendingItem") == null, "closure releases pending ownership");

        ui = Reset(); Capture(ui, new ItemDataUnpacked());
        for (int frame = 0; frame < 30; frame++) { At(frame, frame / 17f); FilterRuleTooltip.MonitorUpdate(); }
        Check(ui.ChildScans <= 9 && ItemFilterManager.Instance.Filter.Calls == 1, "low FPS uses wall-clock deadline");
        int previous = ui.ChildScans;
        Capture(ui, new ItemDataUnpacked { id = new byte[] { 9 } });
        FilterRuleTooltip.MonitorUpdate();
        Check(ui.ChildScans == previous + 1 && ItemFilterManager.Instance.Filter.Calls == 2, "different item content opens a fresh budget");

        ui = Reset(); ui.FailChildren = true; Capture(ui, new ItemDataUnpacked());
        for (int frame = 0; frame < 90; frame++) { At(frame, frame / 60f); FilterRuleTooltip.MonitorUpdate(); }
        Check(ui.ChildScans == 13, "discovery exceptions are bounded too");

        ui = Reset();
        var empty = new TextMeshProUGUI("");
        var shard = new TextMeshProUGUI("Crafting material");
        var marked = new TextMeshProUGUI("composed" + Marker);
        var range = new TextMeshProUGUI("Range: 1 to 2");
        foreach (var tmp in new[] { empty, shard, marked, range })
            Originals[tmp.GetInstanceID()] = (tmp, "old original");
        Originals[marked.GetInstanceID()] = (marked, "correct vanilla");
        Field<Dictionary<int, TextMeshProUGUI>>(typeof(TooltipRecolor), "s_suppressedRanges")[shard.GetInstanceID()] = shard;
        UnityEngine.Object.Scene = new[] { empty, shard, marked, range };
        Prefs.DebugLog.Value = true;
        Scan(ui);
        Check(!Originals.ContainsKey(empty.GetInstanceID()) && !Originals.ContainsKey(shard.GetInstanceID()), "completed scan retires active empty and ineligible originals");
        Check(Originals[marked.GetInstanceID()].original == "correct vanilla" && Originals[range.GetInstanceID()].original == "Range: 1 to 2", "marked and successfully composed originals retain restoration data");
        Check(!Field<Dictionary<int, TextMeshProUGUI>>(typeof(TooltipRecolor), "s_suppressedRanges").ContainsKey(shard.GetInstanceID()), "retirement cannot leave range enforcement without restoration data");
        Check(Field<long>(typeof(TooltipPerf), "s_staleRetired") == 2, "retirement counter measures actual retired entries");
        At(1, .01f); Check(Gate() == TooltipRecolor.ScanTrigger.None, "retirement ends marker-loss loop next frame");
        At(40, .51f); Check(Gate() == TooltipRecolor.ScanTrigger.Fallback, "fallback survives for late text");
        TooltipRecolor.MarkDirty();
        At(45, .59f); Check(Gate() == TooltipRecolor.ScanTrigger.Dirty, "five-frame dirty window remains inclusive");
        Set(typeof(TooltipRecolor), "s_lastScanFrame", 45);
        Check(Gate() == TooltipRecolor.ScanTrigger.None, "one scan per frame cap retained");
        Call(typeof(TooltipRecolor), "RestoreVanillaOnMasterOff");
        Check(marked.text == "correct vanilla" && range.text == "Range: 1 to 2", "master-off restores retained marked originals");

        ui = Reset();
        var stat = new TextMeshProUGUI("[<color=#FFFFFF>6</color><color=#FA9E3D>A</color>] +10 Health\n+2 Health Regen\nTier: 6\nRange: 1 to 10");
        string vanilla = stat.text;
        UnityEngine.Object.Scene = new[] { stat };
        Scan(ui);
        Check(stat.text.Contains("<link=\"ttu\">") && stat.text.Contains("+2 Health Regen") && stat.text.EndsWith(Marker), "two-stat clean text retains border links and marker");
        Input.Alt = true; At(1, .02f); TooltipRecolor.OnLateUpdate();
        Check(stat.text.Contains("Range: 1 to 10") && Originals[stat.GetInstanceID()].original == vanilla, "Alt restores range from valid original");
        Prefs.EnableTooltips.Value = false; At(2, .04f); TooltipRecolor.OnLateUpdate();
        Check(stat.text == vanilla, "master transition restores full original after Alt");

        ui = Reset();
        stat = new TextMeshProUGUI(vanilla);
        UnityEngine.Object.Scene = new[] { stat };
        ui.AfterLayout = () => stat.text = "native late rewrite";
        Scan(ui);
        Check(Originals.ContainsKey(stat.GetInstanceID()), "retirement precedes relayout: freshly composed original survives native late rewrite");
        ui.AfterLayout = null; At(1, .01f); Scan(ui);
        Check(!Originals.ContainsKey(stat.GetInstanceID()), "next completed pass retires uncomposable native replacement");

        ui = Reset(); shard = new TextMeshProUGUI("Crafting material");
        Originals[shard.GetInstanceID()] = (shard, "old original");
        UnityEngine.Object.FailScan = true; Scan(ui);
        Check(Originals.ContainsKey(shard.GetInstanceID()), "failed scan does not retire unknown ownership");

        ui = Reset();
        stat = new TextMeshProUGUI("SEALED AFFIX\n+10 Health\n+2 Health Regen\nTier: 6\nRange: 1 to 10");
        vanilla = stat.text;
        var ground = new TextMeshProUGUI("GROUND ITEM [<color=#FA9E3D>A</color>]" + GroundLabels.Marker);
        string groundBefore = ground.text;
        UnityEngine.Object.Scene = new[] { stat, ground };
        Scan(ui);
        Check(stat.text.Contains("Sealed") && stat.text.Contains("Tier 6") && stat.text.Contains("+2 Health Regen"),
              "unbracketed sealed two-stat block still composes");
        Check(ground.text == groundBefore, "existing ground marker exclusion remains unchanged");
        Call(typeof(TooltipRecolor), "RestoreVanillaOnMasterOff");
        Check(stat.text == vanilla, "sealed original restores intact");

        ui = Reset(); Prefs.DebugLog.Value = true;
        stat = new TextMeshProUGUI("[<color=#FFFFFF>6</color><color=#FA9E3D>A</color>] +10 Health");
        UnityEngine.Object.Scene = new[] { stat };
        TooltipRecolor.MarkDirty(); TooltipRecolor.OnLateUpdate();
        At(6, .1f); stat.text = "Crafting material"; TooltipRecolor.OnLateUpdate();
        At(40, .7f); TooltipRecolor.OnLateUpdate();
        Check(Field<long>(typeof(TooltipPerf), "s_scans") == 3 &&
              Field<long>(typeof(TooltipPerf), "s_fullScene") == 3 &&
              Field<long>(typeof(TooltipPerf), "s_dirty") == 1 &&
              Field<long>(typeof(TooltipPerf), "s_markerLoss") == 1 &&
              Field<long>(typeof(TooltipPerf), "s_fallback") == 1,
              "actual LateUpdate gate counts scans once with exclusive dirty/marker-loss/fallback reasons");

        ui = Reset(); Capture(ui, new ItemDataUnpacked()); FilterRuleTooltip.MonitorUpdate(); Scan(ui);
        Check(Field<long>(typeof(TooltipPerf), "s_scans") == 0 &&
              Field<long>(typeof(TooltipPerf), "s_ruleMatch") == 0 &&
              !Field<bool>(typeof(TooltipPerf), "s_running"), "disabled event sites neither count nor start a telemetry window");

        Reset();
        int clocks = Time.ClockReads;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++) TooltipPerf.Tick();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(Time.ClockReads == clocks && allocated == 0 && MelonLogger.Lines.Count == 0,
            "disabled telemetry heartbeat: no clock reads, allocations or logging (preference guard only)");
        Console.WriteLine($"RESULT: {passed} assertions passed; controlled stubs, no in-game proof.");
    }
}
