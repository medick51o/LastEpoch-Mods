// No disk writes or Unity calls: compile actual Prefs and SettingsUi against recording doubles.
namespace MelonLoader
{
    public class MelonPreferences_Entry<T> { public T Value; public MelonPreferences_Entry(T value) { Value = value; } }
    public class MelonPreferences_Category
    {
        public int Saves;
        public MelonPreferences_Entry<T> CreateEntry<T>(string key, T value, string title = null, string description = null) => new(value);
        public void SetFilePath(string path, bool autoload) { }
        public void SaveToFile(bool printmsg) { Saves++; }
    }
    public static class MelonPreferences
    { public static MelonPreferences_Category CreateCategory(string key) => new(); }
}
namespace Il2Cpp { public class SettingsPanelTabNavigable { public void Awake() { } } }
namespace medick_Terrible_Tooltips
{
    public static class AaronsHouse { public static void TeleportToBazaar() { } }
    public static class NativeSettings
    {
        public record Row(string Label, string Description, bool Initial, Action<bool> Callback);
        public static Dictionary<string, Row> Toggles = new();
        public static Dictionary<string, string> Descriptions = new();
        public static bool TemplatesAvailable(SettingsPanelTabNavigable settings) => true;
        public static void WarnDegradedOnce(string message) => throw new Exception(message);
        public static void CreateToggle(SettingsPanelTabNavigable settings, string category, string key, string label, string description, bool initial, Action<bool> callback)
            => Toggles.Add(key, new(label, description, initial, callback));
        public static void CreateEnumDropdown<T>(SettingsPanelTabNavigable settings, string category, string key, string label, string description, MelonPreferences_Entry<T> value, Action<int> callback) where T : Enum
            => Descriptions.Add(key, description);
        public static void CreateInfoRow(SettingsPanelTabNavigable settings, string category, string key, string label, string description) { }
        public static void CreateButton(SettingsPanelTabNavigable settings, string category, string key, string label, string description, Action callback) { }
    }
}
// Only geometry and engine objects are doubled. Generated methods are copied verbatim from UnitBorder.cs.
internal static partial class BorderProbe
{
    private const string LinkOpen = "<link=\"ttu\">", DividerLinkOpen = "<link=\"ttd\">";
    private static readonly string Marker = new('\u200B', 4);
    internal class UnitState { public GameObject Border = new(), Divider = new(); }
    internal class RowState { public TextMeshProUGUI Tmp; public int ResyncAfterFrame; public List<UnitState> Units = new(); }
    internal class TooltipState { public UITooltipItem Tooltip; public List<RowState> Rows = new(); }
    private static readonly Dictionary<int, TooltipState> s_tooltips = new();
    private static readonly List<int> s_deadTooltipKeys = new();
    private static bool s_latchedOff = false, s_runtimeActive;
    private static int s_ownedBorderCount;
    private static bool GateEnabled() => true;
    private static void HandleGateOff() => throw new Exception("Unexpected gate off");
    private static bool LayoutChanged(RowState row) => false;
    private static void RefreshRow(RowState row, string text, int index, bool scheduleNextFrame) => throw new Exception("Unexpected geometry refresh");
    private static void DestroyRows(TooltipState state) => throw new Exception("Unexpected destruction");
    private static void DestroyRow(RowState row) => throw new Exception("Unexpected destruction");
    private static void Fail(string context, Exception ex) => throw new Exception(context, ex);
    internal static bool Candidate(string text) => IsCandidateText(text);
    internal static int Boxes(string text) => CountUnitBoxes(text);
    internal static UnitState Seed(string text)
    {
        s_tooltips.Clear(); s_ownedBorderCount = 1;
        var unit = new UnitState();
        s_tooltips.Add(1, new TooltipState { Tooltip = new(), Rows = new() { new() { Tmp = new(text), ResyncAfterFrame = 0, Units = new() { unit } } } });
        return unit;
    }
    internal static bool Active => s_runtimeActive;
}
