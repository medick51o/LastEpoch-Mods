using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(medick_CooldownTracker.CooldownTrackerMod),
    medick_CooldownTracker.BuildInfo.Name,
    medick_CooldownTracker.BuildInfo.Version,
    medick_CooldownTracker.BuildInfo.Author)]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_CooldownTracker
{
    // Floating skill cooldown icons above the player character.
    // Home toggles the settings panel. See README.md for the feature tour.
    public partial class CooldownTrackerMod : MelonMod
    {
        float _tick;
        const float TickRate = 0.05f;

        public override void OnInitializeMelon()
        {
            Prefs.Init();
            MelonLogger.Msg($"{BuildInfo.DisplayName} v{BuildInfo.Version} ready — Home toggles settings");
        }

        public override void OnUpdate()
        {
            InputTracker.Update();

            if (!UiState.ShowSettings) UiState.TextFieldActive = false;

            // While typing in a label field the Home key must not also toggle the panel.
            if (!UiState.TextFieldActive && Input.GetKeyDown(KeyCode.Home))
            {
                UiState.ShowSettings = !UiState.ShowSettings;
                if (!UiState.ShowSettings)
                {
                    UiState.PickerSlot      = -1;
                    UiState.TextFieldActive = false;
                    InputBlocker.Restore();
                    Prefs.Save();
                }
            }

            // Block game input while the panel wants it: movement lock enabled,
            // or a label text field has keyboard focus.
            InputBlocker.Apply(UiState.ShowSettings &&
                (Prefs.LockInput.Value || UiState.TextFieldActive));

            _tick += Time.deltaTime;
            if (_tick < TickRate) return;
            _tick = 0f;
            SlotRegistry.Tick(Time.time);
        }

        public override void OnGUI()
        {
            Styles.Ensure();
            OverheadRenderer.Draw();
            if (!UiState.ShowSettings) return;

            SettingsPanel.Draw();
            if (UiState.PickerSlot >= 0)
            {
                int effMI = ButtonLabels.GetModeIndex();
                if (effMI == 0) UiState.PickerSlot = -1;   // keyboard mode has no picker
                else ButtonPicker.Draw(effMI);
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
