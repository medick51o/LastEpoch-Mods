namespace medick_CooldownTracker
{
    // Shared GUI state between the mod loop, settings panel and button picker.
    internal static class UiState
    {
        public static bool ShowSettings;
        public static int  PickerSlot = -1;     // -1 = picker closed
        public static bool TextFieldActive;     // a label field has keyboard focus
    }
}
