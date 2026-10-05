using UnityEngine;

namespace Deadswitch.Game.Audio
{
    /// <summary>
    /// Procedural sound (F-035, procedural-only assets): every clip is synthesized at startup from noise,
    /// oscillators, filters and envelopes. Loops are built to wrap seamlessly. Presentation only.
    /// </summary>
    public static class Synth
    {
        public const int Rate = 22050;

        /// <summary>Wind: brown noise through a slowly breathing low-pass, with gusts.</summary>
        public static AudioClip Wind(float seconds, int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(seconds * Rate);
            var d = new float[n];
            float brown = 0f;
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                brown = Mathf.Clamp((brown + ((float)rng.NextDouble() * 2f - 1f) * 0.02f) * 0.998f, -1f, 1f);
                float gust = 0.55f + (0.3f * Mathf.Sin(t * Mathf.PI * 2f * 3f)) + (0.15f * Mathf.Sin(t * Mathf.PI * 2f * 7f + 1.3f));
                float cutoff = 0.02f + (0.05f * gust);
                lp += (brown - lp) * cutoff;
                d[i] = lp * 3.2f * gust;
            }

            return Loop("Wind", d);
        }

        /// <summary>Low machine drone with beating detuned partials: the bunker breathing.</summary>
        public static AudioClip Drone(float seconds)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            // whole cycles per loop so it wraps cleanly
            float[] freqs = { 44f, 44.5f, 88f, 131f };
            float[] amps = { 0.32f, 0.28f, 0.12f, 0.05f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float s = 0f;
                for (int k = 0; k < freqs.Length; k++)
                {
                    float f = Mathf.Round(freqs[k] * seconds) / seconds;
                    s += amps[k] * Mathf.Sin(2f * Mathf.PI * f * t);
                }

                d[i] = s;
            }

            return Clip("Drone", d, true);
        }

        /// <summary>Dark minor pad (tension music): slow swelling chord over a pulse that quickens with threat.</summary>
        public static AudioClip Pad(float seconds)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            float[] chord = { 55f, 65.4f, 82.4f, 110f, 130.8f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float u = (float)i / n;
                float swell = 0.6f + (0.4f * Mathf.Sin(u * Mathf.PI * 2f));
                float s = 0f;
                for (int k = 0; k < chord.Length; k++)
                {
                    float f = Mathf.Round(chord[k] * seconds) / seconds;
                    float f2 = Mathf.Round(chord[k] * 1.003f * seconds) / seconds;
                    s += (Mathf.Sin(2f * Mathf.PI * f * t) + (0.6f * Mathf.Sin(2f * Mathf.PI * f2 * t))) / (k + 1.5f);
                }

                // heartbeat-like low pulse, 4 per loop
                float beat = Mathf.Repeat(u * 4f, 1f);
                float thump = Mathf.Exp(-beat * 18f) * Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.8f;
                d[i] = (s * 0.22f * swell) + thump;
            }

            return Clip("Pad", d, true);
        }

        /// <summary>Fire crackle: pops and hiss over a low roar.</summary>
        public static AudioClip Crackle(float seconds, int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(seconds * Rate);
            var d = new float[n];
            float lp = 0f;
            float pop = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = (float)rng.NextDouble() * 2f - 1f;
                lp += (w - lp) * 0.08f;
                if (rng.NextDouble() < 0.0009)
                {
                    pop = 0.5f + (float)rng.NextDouble() * 0.5f;
                }

                pop *= 0.985f;
                d[i] = (lp * 0.5f) + (w * pop * 0.6f);
            }

            return Loop("Crackle", d);
        }

        /// <summary>Distant (or close) explosion: a thump plus a low-passed noise roar with a long tail.</summary>
        public static AudioClip Explosion(int seed, bool close)
        {
            var rng = new System.Random(seed);
            float seconds = close ? 2.6f : 3.4f;
            int n = (int)(seconds * Rate);
            var d = new float[n];
            float lp = 0f;
            float cutoff = close ? 0.12f : 0.035f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float w = (float)rng.NextDouble() * 2f - 1f;
                lp += (w - lp) * cutoff * Mathf.Exp(-t * 1.2f);
                float env = Mathf.Exp(-t * (close ? 2.2f : 1.4f)) * Mathf.Min(1f, t * 200f);
                float thump = Mathf.Sin(2f * Mathf.PI * (38f + (30f * Mathf.Exp(-t * 9f))) * t) * Mathf.Exp(-t * 5f);
                d[i] = ((lp * 4f) + (thump * (close ? 0.9f : 0.5f))) * env;
            }

            return Clip(close ? "Explosion Close" : "Explosion Far", d, false);
        }

        /// <summary>A burst of distant rifle and machine-gun fire.</summary>
        public static AudioClip Gunfire(int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(2.4f * Rate);
            var d = new float[n];
            int next = 0;
            while (next < n)
            {
                int len = (int)(0.05f * Rate);
                float amp = 0.4f + (float)rng.NextDouble() * 0.5f;
                float lp = 0f;
                for (int i = 0; i < len && next + i < n; i++)
                {
                    float t = (float)i / Rate;
                    float w = (float)rng.NextDouble() * 2f - 1f;
                    lp += (w - lp) * 0.3f;
                    d[next + i] += lp * amp * Mathf.Exp(-t * 60f) * 2f;
                }

                // bursts of auto fire, gaps between
                next += rng.NextDouble() < 0.18 ? (int)(Rate * (0.15f + (float)rng.NextDouble() * 0.35f)) : (int)(Rate * 0.075f);
            }

            return Clip("Gunfire", d, false);
        }

        /// <summary>Raid alert (doc 10 s4): fast triple pulse.</summary>
        public static AudioClip TriplePulse()
        {
            int n = (int)(0.75f * Rate);
            var d = new float[n];
            for (int p = 0; p < 3; p++)
            {
                int start = (int)(p * 0.2f * Rate);
                int len = (int)(0.12f * Rate);
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-t * 14f);
                    float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 740f * t)) * 0.35f + (Mathf.Sin(2f * Mathf.PI * 1480f * t) * 0.2f);
                    d[start + i] += sq * env;
                }
            }

            return Clip("Alert Raid", d, false);
        }

        /// <summary>Siege alert (doc 10 s4): slow heavy rumble.</summary>
        public static AudioClip Rumble(int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(3.2f * Rate);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                float w = (float)rng.NextDouble() * 2f - 1f;
                lp += (w - lp) * 0.012f;
                float env = Mathf.Sin(u * Mathf.PI);
                float t = (float)i / Rate;
                d[i] = ((lp * 9f) + (Mathf.Sin(2f * Mathf.PI * 31f * t) * 0.4f)) * env * env;
            }

            return Clip("Alert Siege", d, false);
        }

        /// <summary>Purge alert (doc 10 s4): continuous rising-falling siren, looped.</summary>
        public static AudioClip Siren()
        {
            const float seconds = 2f;
            int n = (int)(seconds * Rate);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                float f = 520f + (300f * (0.5f - (0.5f * Mathf.Cos(u * Mathf.PI * 2f))));
                phase += 2f * Mathf.PI * f / Rate;
                d[i] = (Mathf.Sin(phase) * 0.5f) + (Mathf.Sin(phase * 2f) * 0.15f);
            }

            return Clip("Alert Purge", d, true);
        }

        /// <summary>Radio static burst (dispatch, dilemmas, ultimatum).</summary>
        public static AudioClip Static(int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(0.9f * Rate);
            var d = new float[n];
            float hp = 0f;
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                float w = (float)rng.NextDouble() * 2f - 1f;
                hp = 0.9f * (hp + w - prev);
                prev = w;
                float chop = rng.NextDouble() < 0.02 ? 0f : 1f;
                d[i] = hp * 0.35f * Mathf.Sin(u * Mathf.PI) * chop;
            }

            return Clip("Static", d, false);
        }

        /// <summary>Soft UI tick and confirm chime.</summary>
        public static AudioClip Tick(float freq, float seconds)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                d[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + (0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2.01f * t))) * Mathf.Exp(-t * (8f / seconds)) * Mathf.Min(1f, t * 600f) * 0.4f;
            }

            return Clip("Tick", d, false);
        }

        /// <summary>One syllable of the AI's synthetic voice: a formant-ish blip, bit-crushed by <paramref name="crush"/> (0..1).</summary>
        public static AudioClip Syllable(float freq, float crush, int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(0.07f * Rate);
            var d = new float[n];
            int hold = 1 + (int)(crush * 10f);
            float held = 0f;
            float formant = 2.2f + (float)rng.NextDouble() * 1.4f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float u = (float)i / n;
                float env = Mathf.Sin(u * Mathf.PI);
                float s = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f) + (Mathf.Sin(2f * Mathf.PI * freq * formant * t) * 0.3f) + (((float)rng.NextDouble() * 2f - 1f) * 0.05f);
                if (i % hold == 0)
                {
                    held = Mathf.Round(s * (8f - (crush * 6f))) / (8f - (crush * 6f));
                }

                d[i] = held * env * 0.35f;
            }

            return Clip("Syllable", d, false);
        }

        private static AudioClip Loop(string name, float[] d)
        {
            // crossfade the tail into the head so the loop has no seam
            int fade = Mathf.Min(d.Length / 4, Rate / 2);
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                d[i] = (d[i] * k) + (d[d.Length - fade + i] * (1f - k));
            }

            var trimmed = new float[d.Length - fade];
            System.Array.Copy(d, trimmed, trimmed.Length);
            return Clip(name, trimmed, true);
        }

        /// <summary>The opening's heartbeat: two low thumps (lub-dub) with a sub-bass body and a soft click on top.</summary>
        public static AudioClip Heartbeat()
        {
            int n = (int)(0.9f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                d[i] = Thump(t, 0f, 1f) + Thump(t, 0.24f, 0.7f);
            }

            return Clip("Heartbeat", d, true);
        }

        private static float Thump(float t, float at, float gain)
        {
            float u = t - at;
            if (u < 0f)
            {
                return 0f;
            }

            // pitch falls 70 -> 42 Hz as the beat decays
            float freq = 42f + (28f * Mathf.Exp(-u * 30f));
            float body = Mathf.Sin(2f * Mathf.PI * freq * u) * Mathf.Exp(-u * 11f) * Mathf.Min(1f, u * 400f);
            float click = Mathf.Sin(2f * Mathf.PI * 180f * u) * Mathf.Exp(-u * 90f) * 0.25f;
            return (body + click) * gain;
        }

        /// <summary>The core powering up: a rising sweep over a noise swell that lands on a deep thunk.</summary>
        public static AudioClip PowerUp(int seed)
        {
            var rng = new System.Random(seed);
            const float Rise = 2.2f;
            int n = (int)((Rise + 1.4f) * Rate);
            var d = new float[n];
            float lp = 0f;
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float u = Mathf.Clamp01(t / Rise);
                float freq = 60f + (340f * u * u);
                phase += 2f * Mathf.PI * freq / Rate;
                float sweep = (Mathf.Sin(phase) + (0.35f * Mathf.Sin(phase * 2.01f))) * u * u * (t < Rise ? 1f : Mathf.Exp(-(t - Rise) * 14f));
                float w = (float)rng.NextDouble() * 2f - 1f;
                lp += (w - lp) * (0.02f + (0.2f * u));
                float swell = lp * 2.2f * u * (t < Rise ? 1f : Mathf.Exp(-(t - Rise) * 10f));
                float land = Thump(t, Rise, 1.6f);
                d[i] = (sweep * 0.5f) + (swell * 0.5f) + land;
            }

            return Clip("Power Up", d, true);
        }

        /// <summary>After the big one: the ears ring, a high thin tone that fades over seconds.</summary>
        public static AudioClip Whine()
        {
            int n = (int)(4.5f * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t * 3f) * Mathf.Exp(-t * 0.7f);
                d[i] = (Mathf.Sin(2f * Mathf.PI * 3150f * t) + (0.3f * Mathf.Sin(2f * Mathf.PI * 3170f * t))) * env * 0.25f;
            }

            return Clip("Whine", d, false);
        }

        /// <summary>Time passing: a reversed noise swell that rises and cuts dead.</summary>
        public static AudioClip Swell(int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(1.8f * Rate);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                lp += (((float)rng.NextDouble() * 2f) - 1f - lp) * (0.02f + (0.25f * u * u));
                float t = (float)i / Rate;
                d[i] = ((lp * 2.5f) + (Mathf.Sin(2f * Mathf.PI * (50f + (60f * u)) * t) * 0.4f)) * u * u * u;
            }

            return Clip("Swell", d, true);
        }

        /// <summary>A building restored: welding sparks crackle over a rising hum that locks in with a thunk.</summary>
        public static AudioClip Restore(int seed)
        {
            var rng = new System.Random(seed);
            const float Rise = 1.1f;
            int n = (int)((Rise + 0.8f) * Rate);
            var d = new float[n];
            float phase = 0f;
            float spark = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float u = Mathf.Clamp01(t / Rise);
                phase += 2f * Mathf.PI * (110f + (110f * u)) / Rate;
                float hum = (Mathf.Sin(phase) + (0.5f * Mathf.Sin(phase * 1.5f))) * u * (t < Rise ? 0.5f : Mathf.Exp(-(t - Rise) * 6f) * 0.5f);
                if (t < Rise && rng.NextDouble() < 0.004)
                {
                    spark = 1f;
                }

                spark *= 0.9965f;
                float crackle = ((float)rng.NextDouble() * 2f - 1f) * spark * 0.6f;
                d[i] = hum + crackle + Thump(t, Rise, 0.9f);
            }

            return Clip("Restore", d, true);
        }

        /// <summary>The core's first breath: a sub-bass drop that falls away under a low rumble.</summary>
        public static AudioClip SubDrop(int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(2.4f * Rate);
            var d = new float[n];
            float phase = 0f;
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float freq = 28f + (90f * Mathf.Exp(-t * 2.2f));
                phase += 2f * Mathf.PI * freq / Rate;
                float env = Mathf.Min(1f, t * 60f) * Mathf.Exp(-t * 1.1f);
                lp += (((float)rng.NextDouble() * 2f) - 1f - lp) * 0.01f;
                d[i] = ((Mathf.Sin(phase) * 1.2f) + (lp * 4f)) * env;
            }

            return Clip("Sub Drop", d, true);
        }

        private static AudioClip Clip(string name, float[] d, bool normalize)
        {
            float peak = 0.0001f;
            foreach (float x in d)
            {
                peak = Mathf.Max(peak, Mathf.Abs(x));
            }

            float scale = normalize || peak > 0.98f ? 0.9f / peak : 1f;
            for (int i = 0; i < d.Length; i++)
            {
                d[i] *= scale;
            }

            AudioClip clip = AudioClip.Create(name, d.Length, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
