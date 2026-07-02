using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine.UI;

namespace medick_FogOfWar
{
    // Builds the "Terrible fog_OF_war" category in the game's settings:
    // six colour-coded legend rows that ARE the control — click a level to
    // select it; the active level's row stays checked, radio-style.
    //
    // v2.0 history: v1 had a dropdown plus dead "not clickable" legend rows;
    // the first v2 build made the rows clickable but kept the dropdown.
    // Once the rows proved out in-game, the dropdown was redundant and cut.
    internal static class SettingsUi
    {
        const string Category = BuildInfo.DisplayName;

        static readonly Toggle[] _legendToggles = new Toggle[6];

        // Title colours follow the family tier palette (gray → mythic pink).
        static readonly (int level, string row, string title, string desc)[] Legend =
        {
            (0, "fogOFwar - 0 BLIND",
                "<color=#DADADA>0  BLIND</color>",
                "Going in blind? OK @wudijo  |  Minimap disappears the moment you click — and comes right back when you leave.  The overlay map still opens — honor system.  |  Click to select."),
            (1, "fogOFwar - 1 HARD",
                "<color=#E1E1E1>1  HARD</color>",
                "Put in for filler, no one will use it.  |  Minimap visible but reveals nothing. Already-explored fog stays revealed until your next zone.  |  Click to select."),
            (2, "fogOFwar - 2 LIMITED",
                "<color=#77ACFF>2  LIMITED</color>",
                "Also filler, also no one will use it.  |  69% of the default reveal radius. Nice.  |  Click to select."),
            (3, "fogOFwar - 3 NORMAL",
                "<color=#16FF0E>3  NORMAL</color>",
                "You downloaded this from Nexus Mods, I don't think so.  |  Default game state — the mod sits idle. Fog you already revealed stays revealed until your next zone.  |  Click to select."),
            (4, "fogOFwar - 4 SCOUT",
                "<color=#FA9E3D>4  SCOUT</color>",
                "*raises an eyebrow*  |  3× the default reveal radius, applied instantly.  |  Click to select."),
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
            SyncLegend(level);
            Dbg.Log($"level selected: {Prefs.FogLevel.Value}");
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
