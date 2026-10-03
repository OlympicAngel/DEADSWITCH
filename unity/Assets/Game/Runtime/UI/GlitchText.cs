using System.Text;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Corruption glitches for the AI's text (ADVISOR_VOICE.md): dropped letters, stutters, block noise and
    /// [REDACTED], scaled by corruption and the effect-intensity setting. Never unreadable: at most a small
    /// share of characters change and numbers are never touched (values must stay trustworthy on screen).
    /// </summary>
    public static class GlitchText
    {
        private const string Noise = "#%&@$/\\|=+*<>";

        /// <param name="intensity">0..1 (corruption band x effect intensity).</param>
        /// <param name="seed">Changes per frame for flicker, fixed for stable text.</param>
        public static string Apply(string text, float intensity, int seed)
        {
            if (string.IsNullOrEmpty(text) || intensity <= 0.01f)
            {
                return text;
            }

            uint rng = (uint)seed * 2654435761u;
            float p = 0.06f * intensity;
            var sb = new StringBuilder(text.Length + 8);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                rng = (rng * 1664525u) + 1013904223u;
                float r = (rng >> 8) / 16777216f;
                if (char.IsDigit(c) || char.IsWhiteSpace(c) || r >= p)
                {
                    sb.Append(c);
                    continue;
                }

                float pick = r / p;
                if (pick < 0.4f)
                {
                    sb.Append(Noise[(int)(rng % (uint)Noise.Length)]);
                }
                else if (pick < 0.7f)
                {
                    // dropped letter
                }
                else
                {
                    sb.Append(c).Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>Corruption band (0..3) to a 0..1 glitch weight.</summary>
        public static float BandWeight(int band)
        {
            switch (band)
            {
                case 0: return 0f;
                case 1: return 0.35f;
                case 2: return 0.7f;
                default: return 1f;
            }
        }
    }
}
