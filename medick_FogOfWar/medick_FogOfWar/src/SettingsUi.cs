using System;
using Il2Cpp;
using Il2CppLE.UI.Controls;
using MelonLoader;

namespace medick_FogOfWar
{
    // Builds the "Terrible fog_OFwar" category in the game's settings:
    // one Map Vision dropdown + six colour-coded legend rows that are
    // actually clickable — click a level to select it (v1's legend rows
    // were dead buttons labelled "not clickable, just here for reference").
    internal static class SettingsUi
    {
        const string Category    = "Terrible fog_OFwar";
        const string DropdownRow = "fogOFwar - Vision Level";

        static ColoredIconDropdown _dropdown;

        // Title colours follow the family tier palette (gray → mythic pink).
        static readonly (string row, string title, string desc)[] Legend =
        {
            ("fogOFwar - 0 BLIND",
             "<color=#DADADA>0  BLIND</color>",
             "Going in blind? OK @wudijo  |  Minimap hidden, nothing reveals. The overlay map still opens — honor system.  |  Click to select."),
            ("fogOFwar - 1 HARD",
             "<color=#E1E1E1>1  HARD</color>",
             "Put in for filler, no one will use it.  |  Minimap visible but reveals nothing.  |  Click to select."),
            ("fogOFwar - 2 LIMITED",
             "<color=#77ACFF>2  LIMITED</color>",
             "Also filler, also no one will use it.  |  69% of the default reveal radius. Nice.  |  Click to select."),
            ("fogOFwar - 3 NORMAL",
             "<color=#16FF0E>3  NORMAL</color>",
             "You downloaded this from Nexus Mods, I don't think so.  |  Default game state — the mod sits idle.  |  Click to select."),
            ("fogOFwar - 4 SCOUT",
             "<color=#FA9E3D>4  SCOUT</color>",
             "*raises an eyebrow*  |  3× the default reveal radius.  |  Click to select."),
            ("fogOFwar - 5 ORACLE",
             "<color=#FF44FF>5  ORACLE</color>",
             "We already know why you came.  |  Radius 600 — the whole zone, no FPS tax.  |  Click to select."),
        };

        public static void Inject(SettingsPanelTabNavigable panel)
        {
            if (panel == null) return;
            try
            {
                _dropdown = NativeSettings.CreateEnumDropdown(panel,
                    Category, DropdownRow,
                    "<color=#FF44FF>Map Vision Level</color>",
                    "Controls how much of the map is revealed. Lower number = less you see. " +
                    "Higher number... you get the idea. Everything applies instantly — " +
                    "already-explored fog stays revealed until your next zone.",
                    Prefs.FogLevel,
                    SelectLevel);

                foreach (var (row, title, desc) in Legend)
                {
                    int level = ParseLevel(row);
                    NativeSettings.CreateButton(panel, Category, row, title, desc,
                        () => { SelectLevel(level); SyncDropdown(level); });
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("settings injection failed: " + ex.Message);
            }
        }

        static int ParseLevel(string rowName)
        {
            // "fogOFwar - 3 NORMAL" → 3
            int dash = rowName.IndexOf(" - ", StringComparison.Ordinal);
            return rowName[dash + 3] - '0';
        }

        static void SelectLevel(int level)
        {
            Prefs.FogLevel.Value = (MapVisionLevel)level;
            Prefs.Save();
            FogController.Apply();
            Dbg.Log($"level selected: {Prefs.FogLevel.Value}");
        }

        static void SyncDropdown(int level)
        {
            var dd = _dropdown;
            if (dd == null) return;
            try { dd.SetValueWithoutNotify(level); }
            catch
            {
                try { dd.value = level; } catch { }   // fires SelectLevel again — idempotent
            }
        }
    }
}
