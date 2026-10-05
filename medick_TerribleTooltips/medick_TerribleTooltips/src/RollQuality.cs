// Shared roll mathematics: display snapping, short ranges and near-maximum grades.

namespace medick_Terrible_Tooltips;

internal static class RollQuality
{
    public const string Ladder = "FCBAS";

    public const int LadderLength = 5;
    public const char NoLetter = '\0';
    public const char NoGrade = '\0';
    public static char BandLetter(double roll01)
    {
        if (double.IsNaN(roll01)) return 'S';
        double pct = Math.Round(Clamp01(roll01) * 100.0, 6);
        if (pct < 30.0) return 'F';
        if (pct < 60.0) return 'C';
        if (pct < 90.0) return 'B';
        if (pct < 97.0) return 'A';
        return 'S';
    }
    public static char Letter(double roll01, int steps)
    {
        char band = BandLetter(roll01);
        if (steps >= LadderLength) return band;

        if (steps < 1) steps = 1;
        int floor = LadderLength - steps;
        int idx   = Ladder.IndexOf(band);
        if (idx < floor) idx = floor;
        return Ladder[idx];
    }
    public static char LetterForRange(double min, double max, int precisionDigits, double roll01)
    {
        double fraction = SnappedFraction(min, max, precisionDigits, roll01);
        char letter = Letter(fraction, StepCount(min, max, precisionDigits));

        int distinct = DistinctValues(min, max, precisionDigits);
        if (distinct > 1)
        {
            int below = (int)Math.Round((1.0 - fraction) * (distinct - 1), MidpointRounding.AwayFromZero);
            if (below < 0) below = 0;
            int idx = LadderLength - 1 - below;
            if (idx < 0) idx = 0;
            char near = Ladder[idx];
            if (Ladder.IndexOf(near) > Ladder.IndexOf(letter)) letter = near;
        }
        return letter;
    }
    public static int DistinctValues(double min, double max, int precisionDigits)
    {
        if (double.IsNaN(min) || double.IsNaN(max)) return 1;
        if (double.IsInfinity(min) || double.IsInfinity(max)) return 1;
        if (!(max > min)) return 1;

        double step = StepSize(precisionDigits);
        if (!(step > 0.0)) return 1;
        double steps = Math.Floor((max - min) / step + 0.5) + 1.0;
        if (steps < 1.0) steps = 1.0;
        if (steps > 100000.0) steps = 100000.0;
        return (int)steps;
    }
    public static double SnappedFraction(double min, double max, int precisionDigits, double roll01)
    {
        if (double.IsNaN(roll01)) return roll01;
        double fraction = Clamp01(roll01);

        if (double.IsNaN(min) || double.IsNaN(max)) return fraction;
        if (double.IsInfinity(min) || double.IsInfinity(max)) return fraction;

        double length = max - min;
        if (length <= 0.0) return 1.0;

        double scale = Math.Pow(10.0, precisionDigits < 0 ? 0 : precisionDigits);
        double value = min + fraction * length;
        double cleanMin = Math.Round(min * scale) / scale;
        double cleanMax = Math.Round(max * scale) / scale;
        if (Math.Abs(min - cleanMin) * scale < 1e-4) min = cleanMin;
        if (Math.Abs(max - cleanMax) * scale < 1e-4) max = cleanMax;
        length = max - min;
        if (length <= 0.0) return 1.0;
        double snapped = Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
        if (snapped < min) snapped = min;
        if (snapped > max) snapped = max;
        return Clamp01((snapped - min) / length);
    }
    public static double FractionFor(double min, double max, double value)
    {
        if (double.IsNaN(value)) return 0.0;
        double length = max - min;
        if (!(length > 0.0)) return 1.0;
        return Clamp01((value - min) / length);
    }
    public static int StepCount(double min, double max, int precisionDigits)
    {
        if (double.IsNaN(min) || double.IsNaN(max)) return 1;
        if (double.IsInfinity(min) || double.IsInfinity(max)) return 1;
        if (max <= min) return 1;

        double step  = StepSize(precisionDigits);
        double steps = Math.Floor((max - min) / step + 0.5) + 1.0;
        if (steps < 1.0) steps = 1.0;
        if (steps > LadderLength) steps = LadderLength;
        return (int)steps;
    }
    public static double StepSize(int precisionDigits)
    {
        switch (precisionDigits)
        {
            case 0:  return 1.0;
            case 1:  return 0.1;
            case 2:  return 0.01;
            case 3:  return 0.001;
            case 4:  return 0.0001;
            case 5:  return 0.00001;
            case 6:  return 0.000001;
            default: return 1.0;
        }
    }
    public static double IdolMultiplier(double modifier) => 1.0 + modifier;
    public static int InferPrecisionDigits(double min, double max)
    {
        if (double.IsNaN(min) || double.IsNaN(max)) return 0;
        for (int digits = 0; digits <= 3; digits++)
        {
            double scale = Math.Pow(10.0, digits);
            if (IsWhole(min * scale) && IsWhole(max * scale)) return digits;
        }
        return 3;
    }

    private static double Clamp01(double value)
        => value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);

    private static bool IsWhole(double value) => Math.Abs(value - Math.Round(value)) < 1e-4;
    public static char LegacyLetter(double roll01)
    {
        double pct = roll01 * 100.0;
        if (double.IsNaN(pct)) return 'S';
        if (pct < 50.0) return 'F';
        if (pct < 70.0) return 'C';
        if (pct < 80.0) return 'B';
        if (pct < 95.0) return 'A';
        return 'S';
    }

    public static char RarityLetter(double weighting)
    {
        if (double.IsNaN(weighting)) return NoLetter;
        if (weighting >= 1.0) return NoLetter;   // common
        if (weighting >= 0.5) return 'C';        // uncommon
        if (weighting >= 0.3) return 'B';        // rare
        if (weighting >= 0.1) return 'A';        // very rare
        return 'S';                              // extremely rare
    }
}
