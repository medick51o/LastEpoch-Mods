using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(medick_CameraZoom.CameraZoomMod),
    medick_CameraZoom.BuildInfo.Name,
    medick_CameraZoom.BuildInfo.Version,
    medick_CameraZoom.BuildInfo.Author)]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_CameraZoom
{
    // Camera FOV/zoom control for Last Epoch. End toggles the settings
    // panel. See README.md for the feature tour.
    public partial class CameraZoomMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Prefs.Init();
            MelonLogger.Msg($"{BuildInfo.OfficialName} v{BuildInfo.Version} ready — scroll zooms, End opens settings");
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.End))
            {
                if (SettingsPanel.Visible) SettingsPanel.Close();
                else                       SettingsPanel.Visible = true;
            }

            try
            {
                var mgr = CameraManager.instance;
                if (mgr == null) return;
                CameraState.TryCapture(mgr);
                CameraState.Apply(mgr);
            }
            catch { /* CameraManager not yet active */ }
        }

        public override void OnGUI()
        {
            Theme.Ensure();
            if (SettingsPanel.Visible) SettingsPanel.Draw();
        }

        // Leave the camera exactly as the game owns it if the mod unloads.
        public override void OnDeinitializeMelon()
        {
            try { CameraState.RestoreToGame(CameraManager.instance); } catch { }
        }

        public override void OnApplicationQuit() => Prefs.Save();
    }
}
