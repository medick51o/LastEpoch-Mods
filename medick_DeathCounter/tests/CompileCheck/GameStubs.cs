// Shape-only stand-ins for the Il2CppLE types the mod names directly.
// Members match what earlier Terrible mods used against the real game.
namespace Il2Cpp
{
    public class Actor : UnityEngine.MonoBehaviour { }

    public static class PlayerFinder
    {
        public static Actor getPlayerActor() => null;
    }

    public class EpochInputManager : UnityEngine.MonoBehaviour
    {
        public static EpochInputManager instance;
        public bool forceDisableInput;
    }
}
