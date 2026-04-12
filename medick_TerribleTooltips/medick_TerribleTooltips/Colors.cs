// ================================================================
//  Colors.cs  —  medick_Terrible_Tooltips
//
//  Central colour + grade definitions used by both the tooltip
//  patch and the ground label renderer.
//
//  Tier colours (T1 gray → T7+ Mythic Pink):
//    T1  #DADADA  common
//    T2  #E1E1E1  uncommon
//    T3  #16FF0E  green
//    T4  #77ACFF  blue
//    T5  #A807FF  purple / epic
//    T6  #FA9E3D  legendary gold
//    T7+ #FF44FF  MYTHIC (D4 Ancestral beam pink)
//
//  Grade letters:   F  C  B  A  S
//  Grade colours: gray blue purple gold MYTHIC
// ================================================================

namespace medick_Terrible_Tooltips;

public static class Colors
{
    // ── Tier colour — keyed by crafting tier number ──────────────────
    public static string TierColor(int tier)
        => tier switch
        {
            1 => "#DADADA",  // T1
            2 => "#E1E1E1",  // T2
            3 => "#16FF0E",  // T3 green
            4 => "#77ACFF",  // T4 blue
            5 => "#A807FF",  // T5 purple / epic
            6 => "#FA9E3D",  // T6 legendary gold
            _ => "#FF44FF",  // T7+ MYTHIC
        };

    // ── Roll quality colour — keyed by 0–100% roll value ─────────────
    public static string RollColor(double roll)
        => roll switch
        {
            < 20 => "#D2D2D2",  // poor
            < 40 => "#E1E1E1",  // common
            < 60 => "#16FF0E",  // uncommon
            < 70 => "#77ACFF",  // rare
            < 80 => "#A807FF",  // epic
            < 95 => "#FA9E3D",  // legendary
            _    => "#FF44FF",  // S-tier MYTHIC
        };

    // ── Grade letter ─────────────────────────────────────────────────
    public static string GradeLetter(double roll)
        => roll switch
        {
            < 50 => "F",
            < 70 => "C",
            < 80 => "B",
            < 95 => "A",
            _    => "S",
        };

    // ── Grade letter colour ──────────────────────────────────────────
    public static string GradeLetterColor(double roll)
        => roll switch
        {
            < 50 => "#DADADA",  // F — gray
            < 70 => "#77ACFF",  // C — blue
            < 80 => "#A807FF",  // B — purple
            < 95 => "#FA9E3D",  // A — gold
            _    => "#FF44FF",  // S — MYTHIC
        };
}
