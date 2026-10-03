using System;

namespace Deadswitch.Art.World
{
    /// <summary>Hash-based 2D value noise with smooth interpolation and octaves (terrain only).</summary>
    public static class Noise
    {
        public static float Value(float x, float y, uint seed)
        {
            int xi = (int)Math.Floor(x);
            int yi = (int)Math.Floor(y);
            float tx = x - xi;
            float ty = y - yi;
            float a = H(xi, yi, seed);
            float b = H(xi + 1, yi, seed);
            float c = H(xi, yi + 1, seed);
            float d = H(xi + 1, yi + 1, seed);
            float sx = tx * tx * (3 - (2 * tx));
            float sy = ty * ty * (3 - (2 * ty));
            return Lerp(Lerp(a, b, sx), Lerp(c, d, sx), sy);
        }

        public static float Fbm(float x, float y, uint seed, int octaves)
        {
            float sum = 0f;
            float amp = 0.5f;
            float freq = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Value(x * freq, y * freq, seed + (uint)i);
                amp *= 0.5f;
                freq *= 2.03f;
            }

            return sum;
        }

        private static float H(int x, int y, uint seed)
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u + seed * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * t);
        }
    }
}
