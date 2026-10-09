
namespace medick_Terrible_Tooltips
{
    internal static class RollSourcePick
    {
        internal static bool TryPick(double ownLo, double ownHi,
                                     double probeLo, double probeHi,
                                     double fieldLo, double fieldHi,
                                     out double lo, out double hi)
        {
            lo = 0.0;
            hi = 0.0;

            if (Readable(ownLo) && Readable(ownHi) && ownHi > ownLo)
            {
                lo = ownLo;
                hi = ownHi;
                return true;
            }

            if (Readable(probeLo) && Readable(probeHi) && probeHi > probeLo)
            {
                lo = probeLo;
                hi = probeHi;
                return true;
            }

            if (Readable(fieldLo) && Readable(fieldHi) && fieldHi > fieldLo)
            {
                lo = fieldLo;
                hi = fieldHi;
                return true;
            }
            if (Fixed(ownLo, ownHi))
            {
                lo = ownLo;
                hi = ownHi;
                return true;
            }

            if (Fixed(probeLo, probeHi) && Fixed(fieldLo, fieldHi) && probeLo == fieldLo)
            {
                lo = probeLo;
                hi = probeLo;
                return true;
            }

            return false;
        }

        private static bool Readable(double v)
        {
            return !double.IsNaN(v) && !double.IsInfinity(v);
        }

        private static bool Fixed(double lo, double hi)
        {
            return lo == hi && lo != 0.0;
        }
    }
}