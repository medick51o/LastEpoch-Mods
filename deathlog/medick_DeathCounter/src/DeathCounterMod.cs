using System;
using System.IO;
using HarmonyLib;
using medick_DeathCounter.Game;
using medick_DeathCounter.UI;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(medick_DeathCounter.DeathCounterMod),
    medick_DeathCounter.BuildInfo.Name,
    medick_DeathCounter.BuildInfo.Version,
    medick_DeathCounter.BuildInfo.Author)]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

// Every hook is resolved by name and applied one at a time (GameHooks), so a
// renamed game method costs that one tap, never the whole mod.
[assembly: HarmonyDontPatchAll]

namespace medick_DeathCounter
{
    // Terrible Deaths: an always-on death counter, a permanent log of every
    // death (who, what, which ailment), and a Last Death panel that says what
    // to build so it does not happen again.
    //   PanelKey (Insert)          open / close the Last Death panel
    //   Shift + PanelKey           show / hide the counter
    //   click the counter          same as PanelKey; shift-drag moves it
    public class DeathCounterMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Prefs.Init();
            DeathTracker.Init(Path.Combine(MelonEnvironment.UserDataDirectory, "medick_DeathCounter"));
            if (Prefs.ProbeApi.Value) GameHooks.DumpApi();
            GameHooks.Install(HarmonyInstance);
            InputBlocker.Install(HarmonyInstance);

            MelonLogger.Msg($"{BuildInfo.OfficialName} v{BuildInfo.Version} ready: {DeathTracker.Log.All.Count} deaths on record, " +
                $"hooks {GameHooks.HitHooks} hit / {GameHooks.DeathHooks} death / {GameHooks.AilmentHooks} ailment, {Prefs.PanelKeyCode} opens the panel");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        { DeathPanel.Close(); if (CounterHud.Moving) CounterHud.EndMove(); InputBlocker.Restore(); GameHooks.OnPlayerChanged(); }

        public override void OnUpdate()
        {
            try { DeathTracker.Update(); }
            catch (Exception ex) { Dbg.Log("tracker update: " + ex.Message); }

            if (Input.GetKeyDown(Prefs.PanelKeyCode))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    Prefs.ShowCounter.Value = !Prefs.ShowCounter.Value;
                    Prefs.Save();
                }
                else if (PlayerProbe.HasPlayer) DeathPanel.Toggle();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (DeathPanel.DismissConfirmation()) { }
                else if (CounterHud.Moving) CounterHud.EndMove();
                else if (DeathPanel.Open) DeathPanel.Close();
            }

            // Clicks on our UI must not also walk the character there.
            InputBlocker.Update();
        }

        bool _guiFailWarned;

        public override void OnGUI()
        {
            // OnGUI runs several times a frame: one bad record or style must not
            // throw on every event (review #10). Warn once, keep drawing.
            try
            {
                Theme.Ensure();
                DeathPanel.Draw();   // drawn first so the counter's click lands even with the panel open
                CounterHud.Draw();
            }
            catch (Exception ex)
            {
                if (_guiFailWarned) return;
                _guiFailWarned = true;
                MelonLogger.Warning("death counter UI error (shown once): " + ex);
            }
        }

        public override void OnApplicationQuit()
        {
            InputBlocker.Restore();
            Prefs.Save();
        }

        public override void OnDeinitializeMelon() => InputBlocker.Restore();
    }
}
