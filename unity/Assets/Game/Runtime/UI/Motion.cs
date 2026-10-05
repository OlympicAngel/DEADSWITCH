using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadswitch.Game.UI
{
    /// <summary>Easing curves (t in 0..1). Names match the USS timing functions used by the kit.</summary>
    public static class Ease
    {
        public static float Linear(float t) => t;

        public static float OutCubic(float t)
        {
            float u = 1f - t;
            return 1f - (u * u * u);
        }

        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + (c3 * u * u * u) + (c1 * u * u);
        }

    }

    /// <summary>
    /// Tiny tween runner for UI motion (doc 11: motion explains state, never delays input, always has a static
    /// end state). Ticked by <see cref="UiRoot"/>. With reduced motion every tween jumps to its end value.
    /// </summary>
    public static class Motion
    {
        private static readonly List<Tween> Active = new List<Tween>();

        /// <summary>Set from GameSettings.ReducedMotion.</summary>
        public static bool Reduced { get; set; }

        /// <summary>Starts a tween. A tween with the same non-null key replaces the running one.</summary>
        public static void To(object key, float duration, Func<float, float> ease, Action<float> apply, Action done = null)
        {
            After(key, 0f, duration, ease, apply, done);
        }

        /// <summary>Starts a tween after <paramref name="delay"/> seconds; its start value applies at once (staggers).</summary>
        public static void After(object key, float delay, float duration, Func<float, float> ease, Action<float> apply, Action done = null)
        {
            if (key != null)
            {
                Active.RemoveAll(t => t.Key == key);
            }

            if (Reduced || duration <= 0f)
            {
                apply(1f);
                done?.Invoke();
                return;
            }

            apply(0f);
            Active.Add(new Tween { Key = key, Duration = duration, Ease = ease, Apply = apply, Done = done, Time = -Mathf.Max(0f, delay) });
        }

        public static void Cancel(object key)
        {
            Active.RemoveAll(t => t.Key == key);
        }

        public static void Tick(float dt)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                Tween t = Active[i];
                t.Time += dt;
                if (t.Time < 0f)
                {
                    continue;
                }

                float p = Mathf.Clamp01(t.Time / t.Duration);
                t.Apply(t.Ease(p));
                if (p >= 1f)
                {
                    Active.RemoveAt(i);
                    t.Done?.Invoke();
                }
            }
        }

        private sealed class Tween
        {
            public object Key;
            public float Duration;
            public float Time;
            public Func<float, float> Ease;
            public Action<float> Apply;
            public Action Done;
        }
    }

    /// <summary>
    /// A number that glides to its target (critically damped approach) and renders with fixed-width digits so
    /// the readout never jitters. Large jumps animate; ticks of +1 roll smoothly.
    /// </summary>
    public sealed class AnimatedNumber
    {
        private readonly UnityEngine.UIElements.Label _label;
        private readonly Func<long, string> _format;
        private float _value;
        private long _shown = long.MinValue;

        public AnimatedNumber(UnityEngine.UIElements.Label label, Func<long, string> format)
        {
            _label = label;
            _format = format;
        }

        public float Target { get; private set; }

        public void Set(float target, bool instant = false)
        {
            Target = target;
            if (instant || Motion.Reduced)
            {
                _value = target;
                Render();
            }
        }

        public void Tick(float dt)
        {
            float k = 1f - Mathf.Exp(-dt * 9f);
            _value = Mathf.Abs(Target - _value) < 0.5f ? Target : Mathf.Lerp(_value, Target, k);
            Render();
        }

        private void Render()
        {
            long v = (long)Mathf.Round(_value);
            if (v != _shown && _label != null)
            {
                _shown = v;
                _label.text = _format(v);
            }
        }
    }
}
