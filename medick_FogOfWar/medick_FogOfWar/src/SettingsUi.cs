using System;
using Il2Cpp;
using Il2CppLE.UI.Controls;
using MelonLoader;
using UnityEngine.UI;

namespace medick_FogOfWar
{
    // Builds the "Terrible fog_OFwar" category in the game's settings:
    // one Map Vision dropdown + six colour-coded legend rows that are
    // clickable level selectors. The active level's row stays checked,
    // radio-style — v1's legend rows were dead buttons captioned "not
    // clickable, just here for reference" and gave no selection feedback.
    internal static class SettingsUi
    {
        const string Category    = BuildInfo.DisplayName;
        const string DropdownRow = "fogOFwar - Vision Level";

        static ColoredIconDropdown _dropdown;
        static readonly Toggle[] _legendToggles = new Toggle[6];

        // Title colours follow the family tier palette (gray → mythic pink);
        // the dropdown title stays uncoloured so pink remains ORACLE's alone.
        static readonly (int level, string row, string title, string desc)[] Legend =
        {
            (0, "fogOFwar - 0 BLIND",
                "<color=#DADADA>0  BLIND</color>",
                "Going in blind? OK @wudijo  |  Minimap hidden, nothing reveals. The overlay map still opens — honor system.  |  Click to select."),
            (1, "fogOFwar - 1 HARD",
                "<color=#E1E1E1>1  HARD</color>",
                "Put in for filler, no one will use it.  |  Minimap visible but reveals nothing.  |  Click to select."),
            (2, "fogOFwar - 2 LIMITED",
                "<color=#77ACFF>2  LIMITED</color>",
                "Also filler, also no one will use it.  |  69% of the default reveal radius. Nice.  |  Click to select."),
            (3, "fogOFwar - 3 NORMAL",
                "<color=#16FF0E>3  NORMAL</color>",
                "You downloaded this from Nexus Mods, I don't think so.  |  Default game state — the mod sits idle.  |  Click to select."),
            (4, "fogOFwar - 4 SCOUT",
                "<color=#FA9E3D>4  SCOUT</color>",
                "*raises an eyebrow*  |  3× the default reveal radius.  |  Click to select."),
            (5, "fogOFwar - 5 ORACLE",
                "<color=#FF44FF>5  ORACLE</color>",
                "We already know why you came.  |  Radius 600 — the whole zone, no FPS tax.  |  Click to select."),
        };

        public static void Inject(SettingsPanelTabNavigable panel)
        {
            if (panel == null) return;
            try
            {
                // One up-front probe: missing template/anchor → one latched
                // warning, no injection, fog still configurable via cfg.
                if (!NativeSettings.TemplatesAvailable(panel)) return;

                _dropdown = NativeSettings.CreateEnumDropdown(panel,
                    Category, DropdownRow,
                    "Map Vision Level",
                    "Controls how much of the map is revealed. Lower number = less you see. " +
                    "Higher number... you get the idea. Everything applies instantly — " +
                    "already-explored fog stays revealed until your next zone.",
                    Prefs.FogLevel,
                    SelectLevel);

                foreach (var (level, row, title, desc) in Legend)
                {
                    int lv = level;   // capture per iteration, not the loop variable
                    _legendToggles[lv] = NativeSettings.CreateButton(panel,
                        Category, row, title, desc, () => SelectLevel(lv));
                }

                SyncLegend((int)Prefs.FogLevel.Value);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("settings injection failed: " + ex.Message);
            }
        }

        static void SelectLevel(int level)
        {
            Prefs.FogLevel.Value = (MapVisionLevel)level;
            Prefs.Save();
            FogController.Apply();
            SyncDropdown(level);
            SyncLegend(level);
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

        // Radio-style selection state on the legend rows.
        static void SyncLegend(int level)
        {
            for (int i = 0; i < _legendToggles.Length; i++)
            {
                var t = _legendToggles[i];
                if (t == null) continue;
                try { t.SetIsOnWithoutNotify(i == level); }
                catch { }   // stripped interop method: rows stay unchecked, clicks still work
            }
        }
    }
}
