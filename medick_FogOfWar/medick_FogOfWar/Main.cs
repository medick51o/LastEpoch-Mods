// ================================================================
//  Main.cs  —  medick_The_fogOFwar  v1.0.0
//
//  A fog of war control mod with a 6-level vision slider.
//  Slide left to go in blind. Slide right because we all know
//  why you're really here.
//
//  Levels:
//    0  BLIND   — both minimap and overlay map hidden, fog on. Going in blind? OK @wudijo
//    1  HARD    — minimap normal, overlay map hidden (filler, no one will use it)
//    2  LIMITED — 69% reveal radius (also filler, also no one will use it)
//    3  NORMAL  — default game state (you downloaded from nexusmods, i dont think so)
//    4  SCOUT   — 3x reveal radius (*raises an eyebrow*)
//    5  ORACLE  — full map reveal, instant. we already know why you came.
// ================================================================

[assembly: MelonInfo(typeof(medick_FogOfWar.FogOfWarMod),
    "The_fogOFwar", "1.0.0", "medick")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_FogOfWar;

public class FogOfWarMod : MelonMod
{
    // ── Preferences ──────────────────────────────────────────────────
    internal static MelonPreferences_Category Category;
    internal static MelonPreferences_Entry<MapVisionLevel> FogLevel;

    // ── Runtime state ────────────────────────────────────────────────
    // Captured once from the game so all multipliers are relative to default
    internal static float DefaultRevealRadius = -1f;

    // ── Vision level enum ─────────────────────────────────────────────
    public enum MapVisionLevel
    {
        BLIND   = 0,
        HARD    = 1,
        LIMITED = 2,
        NORMAL  = 3,
        SCOUT   = 4,
        ORACLE  = 5
    }

    public override void OnInitializeMelon()
    {
        Category = MelonPreferences.CreateCategory("medick_The_fogOFwar", "Map Vision");

        FogLevel = Category.CreateEntry(
            "VisionLevel",
            MapVisionLevel.NORMAL,
            "Map Vision Level",
            "0=BLIND  1=HARD  2=LIMITED  3=NORMAL  4=SCOUT  5=ORACLE");

        Category.SetFilePath("UserData/medick_The_fogOFwar.cfg", autoload: true);

        MelonLogger.Msg("[The_fogOFwar] v1.0.0 loaded. Vision level: " + FogLevel.Value);
    }

    // ── Helper: convert level to RevealRadius ─────────────────────────
    internal static float GetRevealRadius()
    {
        float def = DefaultRevealRadius > 0f ? DefaultRevealRadius : 150f;

        return FogLevel.Value switch
        {
            MapVisionLevel.BLIND   => 0f,          // minimap hidden, RevealRadius 0
            MapVisionLevel.HARD    => 0f,          // minimap visible, RevealRadius 0
            MapVisionLevel.LIMITED => def * 0.69f,
            MapVisionLevel.NORMAL  => def,
            MapVisionLevel.SCOUT   => def * 3f,
            MapVisionLevel.ORACLE  => 600f,  // covers full zone without FPS penalty
            _                      => def
        };
    }
}
