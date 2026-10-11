// ================================================================
//  Colors.cs — the WoW-retina colour language. FROZEN BRAND.
//
//  "the colors come from world of warcraft item colorization...
//   sense of comfort and familiarity and thats what i want to bring
//   to players who use this mod" — medick
//
//  Tier colours (T1 gray → T7+ Mythic Pink):
//    T1  #DADADA   T2  #E1E1E1   T3  #16FF0E (green)
//    T4  #77ACFF (blue)   T5  #A807FF (purple)
//    T6  #FA9E3D (legendary gold)   T7+ #FF44FF (MYTHIC — D4 Ancestral pink,
//    medick's signature: "I am a diablo 4 guy")
//
//  Grades:  F <30 · C <60 · B <90 · A <97 · S ≥97  (roll × 100)
//  Gray F is a feature: "telling the player ya that roll sucks bro"
//  greater-affix tint, ruled 2026-09-11, deliberately outside the palette:
//    #C990FF
//
//  These hex values are canonical — marketing copy drifts get corrected
//  TO this file, never the reverse. Thresholds follow displayed roll quality.
//  (v1's unused RollColor() band function was removed in the v2 audit.)
// ================================================================

namespace medick_Terrible_Tooltips;

public static class Colors
{
    public const string GreaterAffixTintDefault = "#C990FF";

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

    // ── Grade letter ─────────────────────────────────────────────────
    public static string GradeLetter(double roll)
        => roll switch
        {
            < 30 => "F",
            < 60 => "C",
            < 90 => "B",
            < 97 => "A",
            _    => "S",
        };

    // ── Grade letter colour ──────────────────────────────────────────
    public static string GradeLetterColor(double roll)
        => roll switch
        {
            < 30 => "#DADADA",  // F — gray
            < 60 => "#77ACFF",  // C — blue
            < 90 => "#A807FF",  // B — purple
            < 97 => "#FA9E3D",  // A — gold
            _    => "#FF44FF",  // S — MYTHIC
        };

}
