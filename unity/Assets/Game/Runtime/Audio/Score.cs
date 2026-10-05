using System;

namespace Deadswitch.Game.Audio
{
    /// <summary>
    /// The opening film's score (SPEC-044), composed in code like a trailer cue: one key (D minor), one tempo
    /// (90 BPM), every stem a four-bar loop of the same length so they all run in sync and crossfade on any beat.
    /// Staccato string ostinatos, taiko, huge low brass ("braams"), risers into the loop point, a heartbeat sub;
    /// it only thins out where the story goes quiet. Pure math (no Unity calls), rendered on a worker thread, each
    /// stem sent through a hall reverb that wraps the loop.
    /// </summary>
    public static class Score
    {
        public const int Rate = 22050;
        public const double Bpm = 90.0;
        public const double Beat = 60.0 / Bpm;
        public const int Bars = 4;
        public static readonly int Length = (int)(Beat * 4 * Bars * Rate);

        /// <summary>The stems, in the order <see cref="Render"/> returns them.</summary>
        public enum Stem
        {
            Orbit,
            Command,
            Launch,
            Dark,
            Hold,
            Battle,
            Ash,
            Restore,
            Wake,
        }

        // i - VI - III - VII: Dm, Bb, F, C (MIDI roots and chords per bar)
        private static readonly int[][] Minor = { new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 53, 57, 60 }, new[] { 48, 52, 55 } };

        // the darker turn for the war: Dm, Bb, Gm, A (the major V pulls back to D every loop)
        private static readonly int[][] War = { new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 43, 46, 50 }, new[] { 45, 49, 52 } };

        // the Hub's own heroic turn: F, C, Dm, Bb
        private static readonly int[][] Warm = { new[] { 53, 57, 60 }, new[] { 48, 52, 55 }, new[] { 50, 53, 57 }, new[] { 46, 50, 53 } };

        public static float[][] Render()
        {
            var stems = new float[Enum.GetValues(typeof(Stem)).Length][];
            for (int i = 0; i < stems.Length; i++)
            {
                stems[i] = new float[Length];
            }

            Orbit(stems[(int)Stem.Orbit]);
            Command(stems[(int)Stem.Command]);
            Launch(stems[(int)Stem.Launch]);
            Dark(stems[(int)Stem.Dark]);
            Hold(stems[(int)Stem.Hold]);
            Battle(stems[(int)Stem.Battle]);
            Ash(stems[(int)Stem.Ash]);
            Restore(stems[(int)Stem.Restore]);
            Wake(stems[(int)Stem.Wake]);
            for (int i = 0; i < stems.Length; i++)
            {
                Reverb(stems[i], 0.32f);
                Normalize(stems[i], 0.85f);
            }

            return stems;
        }

        // ---------------------------------------------------------------- the stems

        /// <summary>Signal: the planet, before. Wide and uneasy: a low braam, a ticking 16th pulse that grows, the motif on bell.</summary>
        private static void Orbit(float[] b)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                foreach (int n in Minor[bar])
                {
                    Pad(b, Bar(bar), Beat * 4, Hz(n), 0.12f, 900f);
                }

                // the pulse arrives in the second bar and swells toward the loop point
                if (bar >= 1)
                {
                    Ostinato(b, bar, Minor[bar][0] + 12, Minor[bar][2] + 12, 0.04f + (0.025f * bar));
                }

                Bass(b, Bar(bar), Beat * 3.9, Hz(Minor[bar][0] - 24), 0.25f);
            }

            Braam(b, 0, Beat * 4, Hz(38), 0.35f);
            Motif(b, 0, 74, 0.22f, Bell2);
            Motif(b, 2, 74, 0.18f, Bell2);
            Riser(b, Bar(3), Beat * 4, 0.18f);
        }

        /// <summary>Command: the machine takes the keys. Driving 16th strings, a clock, braams on the downbeats.</summary>
        private static void Command(float[] b)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                Ostinato(b, bar, Minor[bar][0], Minor[bar][2], 0.11f);
                Ostinato(b, bar, Minor[bar][0] + 12, Minor[bar][1] + 12, 0.06f);
                for (int e = 0; e < 8; e++)
                {
                    Click(b, Bar(bar) + (e * Beat * 0.5), e % 2 == 0 ? 0.22f : 0.1f);
                }

                Kick(b, Bar(bar), 0.7f);
                Kick(b, Bar(bar) + (Beat * 2), 0.55f);
                Bass(b, Bar(bar), Beat * 3.9, Hz(Minor[bar][0] - 24), 0.3f);
            }

            Braam(b, 0, Beat * 2.5, Hz(38), 0.4f);
            Braam(b, Bar(2), Beat * 2.5, Hz(41), 0.4f);
            Motif(b, 1, 62, 0.16f, Brass2);
            Riser(b, Bar(3) + (Beat * 2), Beat * 2, 0.2f);
        }

        /// <summary>Launch: everything goes. Taiko, braams every bar, 16th strings an octave apart, a roll into the loop.</summary>
        private static void Launch(float[] b)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                double t = Bar(bar);
                Taiko(b, t, 1f);
                Taiko(b, t + (Beat * 0.75), 0.55f);
                Taiko(b, t + (Beat * 1.5), 0.8f);
                Taiko(b, t + (Beat * 2), 0.9f);
                Taiko(b, t + (Beat * 3), 0.6f);
                Taiko(b, t + (Beat * 3.5), 0.7f);
                Braam(b, t, Beat * 1.6, Hz(War[bar][0] - 12), 0.45f);
                Ostinato(b, bar, War[bar][0], War[bar][2], 0.12f);
                Ostinato(b, bar, War[bar][0] + 12, War[bar][1] + 12, 0.08f);
                foreach (int n in War[bar])
                {
                    Brass(b, t, Beat * 0.9, Hz(n), 0.12f);
                }
            }

            for (int r = 0; r < 16; r++)
            {
                Tom(b, Bar(3) + (Beat * 2) + (r * Beat * 0.125), 0.25f + (0.03f * r), 130f - (r * 4f));
            }

            Motif(b, 2, 74, 0.2f, Brass2);
            Riser(b, Bar(3), Beat * 4, 0.25f);
        }

        /// <summary>Dark: the lights go out. Braams falling a step a bar, a cello line, the heartbeat under it.</summary>
        private static void Dark(float[] b)
        {
            int[] line = { 50, 48, 46, 45 };
            for (int bar = 0; bar < Bars; bar++)
            {
                Braam(b, Bar(bar), Beat * 3.5, Hz(line[bar] - 24), 0.35f);
                Strings(b, Bar(bar), Beat * 3.8, Hz(line[bar]), 0.2f, 0.3);
                Heartbeat(b, Bar(bar), 0.7f);
                Heartbeat(b, Bar(bar) + (Beat * 2), 0.5f);
            }

            Bell(b, Bar(1) + (Beat * 2), Hz(74), 0.08f, 3.5);
            Bell(b, Bar(3) + (Beat * 2), Hz(69), 0.07f, 3.5);
        }

        /// <summary>Hold: the Hub as it was. Heroic and warm, but on a march: drums, 8th strings, the motif turned major.</summary>
        private static void Hold(float[] b)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                foreach (int n in Warm[bar])
                {
                    Strings(b, Bar(bar), Beat * 4, Hz(n), 0.07f, 0.3);
                    Brass(b, Bar(bar), Beat * 1.2, Hz(n - 12), 0.06f);
                }

                Bass(b, Bar(bar), Beat * 3.8, Hz(Warm[bar][0] - 24), 0.25f);
                for (int e = 0; e < 8; e++)
                {
                    Strings(b, Bar(bar) + (e * Beat * 0.5), Beat * 0.35, Hz(Warm[bar][e % 3] + 12), 0.06f, 0.005);
                }

                Kick(b, Bar(bar), 0.6f);
                Taiko(b, Bar(bar) + (Beat * 2), 0.4f);
                Tom(b, Bar(bar) + (Beat * 3.5), 0.3f, 120f);
            }

            int[] motif = { 69, 72, 76, 74 };
            for (int i = 0; i < motif.Length; i++)
            {
                Brass(b, Bar(1) + (i * Beat * 0.75), Beat * 0.7, Hz(motif[i]), 0.14f);
                Brass(b, Bar(3) + (i * Beat * 0.75), Beat * 0.7, Hz(motif[i]), 0.14f);
            }
        }

        /// <summary>Battle: the hardest it gets. Taiko 8ths, braams on every beat one, strings rubbing a half step, a riser.</summary>
        private static void Battle(float[] b)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                double t = Bar(bar);
                for (int k = 0; k < 8; k++)
                {
                    Taiko(b, t + (k * Beat * 0.5), k % 4 == 0 ? 1f : k % 2 == 0 ? 0.7f : 0.45f);
                }

                Braam(b, t, Beat * 1.2, Hz(War[bar][0] - 12), 0.5f);
                Ostinato(b, bar, 62, 63, 0.1f);
                Ostinato(b, bar, War[bar][0] + 12, War[bar][2] + 12, 0.08f);
                for (int s = 0; s < 4; s++)
                {
                    Brass(b, t + (s * Beat), Beat * 0.3, Hz(War[bar][0]), 0.12f);
                }
            }

            Riser(b, Bar(2), Beat * 8, 0.22f);
        }

        /// <summary>Ash: after. A very low drone, glass high above, a slow heartbeat; one far braam.</summary>
        private static void Ash(float[] b)
        {
            Pad(b, 0, Beat * 16, Hz(26), 0.25f, 200f);
            Pad(b, 0, Beat * 16, Hz(38), 0.12f, 250f);
            for (int bar = 0; bar < Bars; bar++)
            {
                Heartbeat(b, Bar(bar), 0.8f);
                Glass(b, Bar(bar) + Beat, Beat * 3, Hz(bar % 2 == 0 ? 93 : 88), 0.05f);
            }

            Braam(b, Bar(2), Beat * 6, Hz(26), 0.3f);
        }

        /// <summary>Restore: something starts again. A heartbeat pulse becomes a drive: 8ths, the bass, the motif on pluck.</summary>
        private static void Restore(float[] b)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                for (int k = 0; k < 4; k++)
                {
                    Kick(b, Bar(bar) + (k * Beat), 0.45f);
                }

                for (int e = 0; e < 8; e++)
                {
                    Bass(b, Bar(bar) + (e * Beat * 0.5), Beat * 0.4, Hz(Minor[bar][0] - 12), 0.2f);
                }

                Ostinato(b, bar, Minor[bar][0] + 12, Minor[bar][2] + 12, 0.04f + (0.02f * bar));
            }

            Motif(b, 0, 62, 0.16f, Pluck);
            Motif(b, 2, 69, 0.14f, Pluck);
            Riser(b, Bar(3), Beat * 4, 0.12f);
        }

        /// <summary>The wake: everything at once, braams and taiko, the chords lifting to a major V, the motif on brass.</summary>
        private static void Wake(float[] b)
        {
            for (int bar = 0; bar < Bars; bar++)
            {
                double t = Bar(bar);
                foreach (int n in War[bar])
                {
                    Strings(b, t, Beat * 4, Hz(n), 0.1f, 0.3);
                    Strings(b, t, Beat * 4, Hz(n + 12), 0.07f, 0.3);
                }

                Braam(b, t, Beat * 3, Hz(War[bar][0] - 12), 0.45f);
                Bass(b, t, Beat * 3.9, Hz(War[bar][0] - 24), 0.3f);
                Taiko(b, t, 1f);
                Taiko(b, t + (Beat * 2), 0.8f);
                Ostinato(b, bar, War[bar][0] + 12, War[bar][2] + 12, 0.07f);
                for (int r = 0; r < 8; r++)
                {
                    Tom(b, t + (Beat * 3) + (r * Beat * 0.125), 0.15f + (0.04f * r), 85f);
                }
            }

            Motif(b, 0, 74, 0.22f, Brass2);
            Motif(b, 2, 74, 0.22f, Brass2);
        }

        // ---------------------------------------------------------------- trailer voices

        /// <summary>Staccato 16th strings for a bar, alternating two notes (the engine of every tense cue).</summary>
        private static void Ostinato(float[] b, int bar, int low, int high, float amp)
        {
            for (int s = 0; s < 16; s++)
            {
                int n = s % 4 == 3 ? high : low;
                Strings(b, Bar(bar) + (s * Beat * 0.25), Beat * 0.18, Hz(n), amp * (s % 4 == 0 ? 1.3f : 1f), 0.004);
            }
        }

        /// <summary>A braam: a huge low brass blast in octaves that swells, bites and rumbles away.</summary>
        private static void Braam(float[] b, double t0, double dur, float f, float amp)
        {
            float lp = 0f;
            int n = (int)((dur + 1.5) * Rate);
            int start = (int)(t0 * Rate);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                float env = Env(t, 0.06, dur, 1.4);
                float cutoff = Math.Max(90f, 180f + (1400f * (float)Math.Exp(-t * 1.6)) + (300f * (float)Math.Sin(Math.Min(t, dur) * 2.5)));
                double p = t * f;
                float v = (float)((Saw(p) + Saw(p * 1.007) + Saw(p * 0.993) + (0.7 * Saw(p * 2.003)) + (0.5 * Saw(p * 0.5))) / 4.2);
                lp += (v - lp) * Lowpass(cutoff);
                Add(b, start + i, (float)Math.Tanh(lp * 2.2) * env * amp);
            }
        }

        /// <summary>A riser: noise and a pitch climbing to the next downbeat, cut dead at the top.</summary>
        private static void Riser(float[] b, double t0, double dur, float amp)
        {
            var rng = new Random((int)(t0 * 313));
            float lp = 0f;
            int n = (int)(dur * Rate);
            int start = (int)(t0 * Rate);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                double u = (double)i / n;
                lp += ((float)((rng.NextDouble() * 2) - 1) - lp) * Lowpass(300f + (6000f * (float)(u * u)));
                phase += (200 + (1800 * u * u)) / Rate;
                double tone = Math.Sin(2 * Math.PI * phase) * 0.3;
                Add(b, start + i, (float)((lp * 1.6) + tone) * (float)(u * u * u) * amp);
            }
        }

        /// <summary>A taiko: a deep skin with a slap on top.</summary>
        private static void Taiko(float[] b, double t0, float amp)
        {
            Kick(b, t0, amp);
            Tom(b, t0, amp * 0.45f, 70f);
        }

        /// <summary>The heartbeat: lub-dub in the sub.</summary>
        private static void Heartbeat(float[] b, double t0, float amp)
        {
            Kick(b, t0, amp);
            Kick(b, t0 + 0.24, amp * 0.65f);
        }

        // ---------------------------------------------------------------- the motif

        private delegate void Voice(float[] b, double t, float freq, float amp);

        /// <summary>The switch motif: four notes over one bar (root, minor third, second, fifth), from a base note.</summary>
        private static void Motif(float[] b, int bar, int root, float amp, Voice voice)
        {
            int[] steps = { 0, 3, 2, 7 };
            double[] at = { 0, 1.5, 2, 3 };
            for (int i = 0; i < steps.Length; i++)
            {
                voice(b, Bar(bar) + (at[i] * Beat), Hz(root + steps[i]), amp);
            }
        }

        // ---------------------------------------------------------------- instruments

        private static void Pad(float[] b, double t0, double dur, float f, float amp, float cutoff)
        {
            float lp = 0f;
            float k = Lowpass(cutoff);
            int n = (int)((dur + 1.2) * Rate);
            int start = (int)(t0 * Rate);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                float env = Env(t, 0.9, dur, 1.2);
                double p = t * f;
                float saw = (float)(Saw(p) + Saw(p * 1.004) + Saw(p * 0.996)) / 3f;
                lp += (saw - lp) * k;
                Add(b, start + i, lp * env * amp);
            }
        }

        private static void Strings(float[] b, double t0, double dur, float f, float amp, double attack)
        {
            float lp = 0f;
            float k = Lowpass(Math.Min(2400f, f * 6f));
            int n = (int)((dur + 0.6) * Rate);
            int start = (int)(t0 * Rate);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                float env = Env(t, attack, dur, 0.5);
                double vib = 1.0 + (0.004 * Math.Sin(t * 2 * Math.PI * 5.2));
                phase += f * vib / Rate;
                float saw = (float)(Saw(phase) + Saw(phase * 1.003)) * 0.5f;
                lp += (saw - lp) * k;
                Add(b, start + i, lp * env * amp);
            }
        }

        private static void Brass(float[] b, double t0, double dur, float f, float amp)
        {
            float lp = 0f;
            int n = (int)((dur + 0.4) * Rate);
            int start = (int)(t0 * Rate);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                float env = Env(t, 0.04, dur, 0.35);
                // the filter opens on the attack and closes as the note holds: the "blat" of a horn
                float cutoff = 400f + (2600f * (float)Math.Exp(-t * 3.0));
                float saw = (float)(Saw(t * f) + Saw(t * f * 1.006)) * 0.5f;
                lp += (saw - lp) * Lowpass(cutoff);
                Add(b, start + i, lp * env * amp);
            }
        }

        private static void Brass2(float[] b, double t, float f, float amp)
        {
            Brass(b, t, Beat * 0.9, f, amp);
        }

        private static void Bass(float[] b, double t0, double dur, float f, float amp)
        {
            float lp = 0f;
            float k = Lowpass(300f);
            int n = (int)((dur + 0.2) * Rate);
            int start = (int)(t0 * Rate);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                float env = Env(t, 0.01, dur, 0.15);
                float v = (float)(Math.Sin(2 * Math.PI * f * t) + (0.5 * Saw(t * f)));
                lp += (v - lp) * k;
                Add(b, start + i, lp * env * amp);
            }
        }

        private static void Pluck(float[] b, double t0, float f, float amp)
        {
            // Karplus-Strong: a burst of noise in a delay line that loses its highs every pass
            int period = Math.Max(2, (int)(Rate / f));
            var line = new float[period];
            var rng = new Random((int)(f * 100) + (int)(t0 * 1000));
            for (int i = 0; i < period; i++)
            {
                line[i] = (float)((rng.NextDouble() * 2) - 1);
            }

            int n = (int)(2.2 * Rate);
            int start = (int)(t0 * Rate);
            for (int i = 0; i < n; i++)
            {
                int j = i % period;
                int j1 = (i + 1) % period;
                float v = line[j];
                line[j] = 0.497f * (line[j] + line[j1]);
                Add(b, start + i, v * amp);
            }
        }

        private static void Bell(float[] b, double t0, float f, float amp, double decay)
        {
            int n = (int)((decay + 0.2) * Rate);
            int start = (int)(t0 * Rate);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double e = Math.Exp(-t * 4.5 / decay);
                double v = Math.Sin(2 * Math.PI * f * t) + (0.4 * Math.Sin(2 * Math.PI * f * 2.76 * t) * Math.Exp(-t * 3)) + (0.2 * Math.Sin(2 * Math.PI * f * 5.4 * t) * Math.Exp(-t * 6));
                Add(b, start + i, (float)(v * e * Math.Min(1.0, t * 400)) * amp);
            }
        }

        private static void Bell2(float[] b, double t0, float f, float amp)
        {
            Bell(b, t0, f, amp, 2.5);
        }

        private static void Glass(float[] b, double t0, double dur, float f, float amp)
        {
            int n = (int)((dur + 1.0) * Rate);
            int start = (int)(t0 * Rate);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                float env = Env(t, dur * 0.5, dur, 1.0);
                double v = Math.Sin(2 * Math.PI * f * t) + (0.3 * Math.Sin(2 * Math.PI * f * 1.5 * t));
                Add(b, start + i, (float)v * env * amp);
            }
        }

        private static void Kick(float[] b, double t0, float amp)
        {
            int n = (int)(0.9 * Rate);
            int start = (int)(t0 * Rate);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double f = 42 + (90 * Math.Exp(-t * 28));
                phase += f / Rate;
                double v = Math.Sin(2 * Math.PI * phase) * Math.Exp(-t * 4.5) * Math.Min(1.0, t * 600);
                Add(b, start + i, (float)v * amp);
            }
        }

        private static void Tom(float[] b, double t0, float amp, float pitch)
        {
            int n = (int)(0.6 * Rate);
            int start = (int)(t0 * Rate);
            var rng = new Random((int)(t0 * 977));
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double f = pitch * (1 + (0.6 * Math.Exp(-t * 20)));
                phase += f / Rate;
                double body = Math.Sin(2 * Math.PI * phase) * Math.Exp(-t * 7);
                double skin = ((rng.NextDouble() * 2) - 1) * Math.Exp(-t * 40) * 0.4;
                Add(b, start + i, (float)(body + skin) * amp);
            }
        }

        private static void Click(float[] b, double t0, float amp)
        {
            int n = (int)(0.03 * Rate);
            int start = (int)(t0 * Rate);
            var rng = new Random((int)(t0 * 1201));
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = (float)((rng.NextDouble() * 2) - 1);
                float hp = w - prev;
                prev = w;
                Add(b, start + i, hp * (float)Math.Exp(-i / (0.004 * Rate)) * amp);
            }
        }

        // ---------------------------------------------------------------- helpers

        private static double Bar(int bar)
        {
            return bar * Beat * 4;
        }

        private static float Hz(int midi)
        {
            return 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0);
        }

        private static double Saw(double phase)
        {
            return (2.0 * (phase - Math.Floor(phase))) - 1.0;
        }

        private static float Lowpass(float cutoff)
        {
            return (float)(1.0 - Math.Exp(-2.0 * Math.PI * cutoff / Rate));
        }

        /// <summary>Attack, hold until <paramref name="dur"/>, then release.</summary>
        private static float Env(double t, double attack, double dur, double release)
        {
            if (t < attack)
            {
                return (float)(t / Math.Max(1e-4, attack));
            }

            if (t < dur)
            {
                return 1f;
            }

            return (float)Math.Max(0.0, 1.0 - ((t - dur) / release));
        }

        /// <summary>Adds into the loop: anything past the end rings on at its start, so the loop has no seam.</summary>
        private static void Add(float[] b, int i, float v)
        {
            b[((i % Length) + Length) % Length] += v;
        }

        /// <summary>A small hall (Schroeder: four combs, two allpasses), run twice so the tail wraps the loop.</summary>
        private static void Reverb(float[] b, float mix)
        {
            int[] combs = { 1557, 1617, 1491, 1422 };
            int[] alls = { 225, 556 };
            var cb = new float[combs.Length][];
            var ci = new int[combs.Length];
            for (int c = 0; c < combs.Length; c++)
            {
                cb[c] = new float[combs[c]];
            }

            var ab = new float[alls.Length][];
            var ai = new int[alls.Length];
            for (int a = 0; a < alls.Length; a++)
            {
                ab[a] = new float[alls[a]];
            }

            var wet = new float[b.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < b.Length; i++)
                {
                    float x = b[i] * 0.25f;
                    float sum = 0f;
                    for (int c = 0; c < combs.Length; c++)
                    {
                        float y = cb[c][ci[c]];
                        cb[c][ci[c]] = x + (y * 0.84f);
                        ci[c] = (ci[c] + 1) % combs[c];
                        sum += y;
                    }

                    for (int a = 0; a < alls.Length; a++)
                    {
                        float y = ab[a][ai[a]];
                        float o = -sum + y;
                        ab[a][ai[a]] = sum + (y * 0.5f);
                        ai[a] = (ai[a] + 1) % alls[a];
                        sum = o;
                    }

                    wet[i] = sum;
                }
            }

            for (int i = 0; i < b.Length; i++)
            {
                b[i] = (b[i] * (1f - (mix * 0.5f))) + (wet[i] * mix);
            }
        }

        private static void Normalize(float[] b, float peak)
        {
            float max = 1e-4f;
            foreach (float v in b)
            {
                max = Math.Max(max, Math.Abs(v));
            }

            float k = peak / max;
            for (int i = 0; i < b.Length; i++)
            {
                b[i] *= k;
            }
        }
    }
}
