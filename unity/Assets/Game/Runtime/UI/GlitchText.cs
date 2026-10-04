using System.Text;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Corruption glitches for the AI's text (ADVISOR_VOICE.md): dropped letters, stutters, block noise and
    /// [REDACTED], scaled by corruption and the effect-intensity setting. Never unreadable: at most a small
    /// share of characters change, and numbers and upper-case words (gates, facilities, postures: the advisor's
    /// data tokens) are never touched, so values stay trustworthy on screen.
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
                if (char.IsDigit(c) || char.IsUpper(c) || char.IsWhiteSpace(c) || r >= p)
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

        /// <summary>
        /// The map's AI estimates (SPEC-033): unlike advisor text, a figure may flicker for a frame, one character
        /// swapped for noise or a wrong digit, with a chance per frame that grows with corruption. Most frames show
        /// the estimate as computed, so it stays readable; off at zero intensity.
        /// </summary>
        public static string Flicker(string text, float intensity, int frame)
        {
            if (string.IsNullOrEmpty(text) || intensity <= 0.01f)
            {
                return text;
            }

            uint rng = ((uint)frame * 2654435761u) ^ ((uint)text.Length * 40503u);
            rng = (rng * 1664525u) + 1013904223u;
            if ((rng >> 8) / 16777216f >= 0.22f * intensity)
            {
                return text;
            }

            rng = (rng * 1664525u) + 1013904223u;
            int at = (int)((rng >> 8) % (uint)text.Length);
            rng = (rng * 1664525u) + 1013904223u;
            char swap = char.IsDigit(text[at]) && (rng & 1u) == 0u ? (char)('0' + ((rng >> 4) % 10u)) : Noise[(int)((rng >> 4) % (uint)Noise.Length)];
            return text.Substring(0, at) + swap + text.Substring(at + 1);
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
