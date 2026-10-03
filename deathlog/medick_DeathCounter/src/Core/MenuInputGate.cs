namespace medick_DeathCounter.Core
{
    // Retain ownership through the release of the closing click, then one
    // neutral frame. No global game flags to restore over another mod's lock.
    public sealed class MenuInputGate
    {
        public bool Blocked { get; private set; }
        int _neutralFrames;
        public void Update(bool ownsInput, bool pointerHeld)
        {
            if (ownsInput) { Blocked = true; _neutralFrames = 0; }
            else if (Blocked)
            {
                if (pointerHeld) _neutralFrames = 0;
                else if (++_neutralFrames >= 2) { Blocked = false; _neutralFrames = 0; }
            }
        }
        public void Clear() { Blocked = false; _neutralFrames = 0; }
    }
}
