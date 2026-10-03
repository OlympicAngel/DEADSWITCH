using System;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Deterministic fixed-point math for curves the design specifies with real exponents
    /// (for example raid strength ~ power^0.7, doc 10 s4). Q16.16 values (1.0 = 65536); integer ops only,
    /// so results are bit-identical on every platform. Relative error is below 0.01% over the game's ranges.
    /// </summary>
    public static class FixedMath
    {
        public const int One = 1 << 16;

        private const int Q = 30;

        // round(2^(2^-i) * 2^30) for i = 1..16
        private static readonly ulong[] Exp2Fraction =
        {
            1518500250UL, 1276901417UL, 1170923762UL, 1121280436UL, 1097253708UL, 1085434106UL, 1079572136UL, 1076653033UL,
            1075196443UL, 1074468888UL, 1074105294UL, 1073923544UL, 1073832680UL, 1073787251UL, 1073764537UL, 1073753181UL,
        };

        /// <summary>log2 of a positive Q16 value, as Q16.</summary>
        public static long Log2(long xQ16)
        {
            if (xQ16 <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(xQ16), "log2 needs a positive value");
            }

            int msb = 63;
            while (((ulong)xQ16 >> msb) == 0)
            {
                msb--;
            }

            long result = (long)(msb - 16) << 16;

            // Normalize into y in [1, 2) as Q30.
            ulong y = msb >= Q ? (ulong)xQ16 >> (msb - Q) : (ulong)xQ16 << (Q - msb);
            for (int i = 1; i <= 16; i++)
            {
                y = (y * y) >> Q;
                if (y >= (2UL << Q))
                {
                    y >>= 1;
                    result += 1L << (16 - i);
                }
            }

            return result;
        }

        /// <summary>2^p for a Q16 exponent, as Q16. Saturates at long.MaxValue for huge exponents.</summary>
        public static long Exp2(long pQ16)
        {
            long ip = pQ16 >> 16;
            long frac = pQ16 & 0xFFFF;
            ulong r = 1UL << Q;
            for (int i = 1; i <= 16; i++)
            {
                if ((frac & (1L << (16 - i))) != 0)
                {
                    r = (r * Exp2Fraction[i - 1]) >> Q;
                }
            }

            int shift = (int)ip + 16 - Q;
            if (shift >= 0)
            {
                return shift >= 32 ? long.MaxValue : (long)(r << shift);
            }

            return shift <= -63 ? 0 : (long)(r >> -shift);
        }

        /// <summary>x^(p/1000) for a non-negative integer x, rounded to the nearest integer.</summary>
        public static long PowPermille(long x, int exponentPermille)
        {
            if (x <= 0)
            {
                return 0;
            }

            long log = Log2(x << 16);
            long scaled = (log * exponentPermille) / 1000;
            return (Exp2(scaled) + (One / 2)) >> 16;
        }
    }
}
