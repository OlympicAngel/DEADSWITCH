using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// The core's holographic presence (SPEC-039 ideas 46, 47, 51): counter-rotating rings, a tick dial and a
    /// pulsing heart drawn in --ring-color / --core-color. <see cref="Voice"/> (0..1) speeds the rings and swells
    /// the heart while a line is spoken; <see cref="Stutter"/> (0..1, the true corruption weight) makes the rings
    /// skip and drift. Reduced motion freezes the rotation but keeps every ring visible.
    /// </summary>
    public sealed class AiOrb
    {
        private static readonly CustomStyleProperty<Color> RingColor = new CustomStyleProperty<Color>("--ring-color");
        private static readonly CustomStyleProperty<Color> CoreColor = new CustomStyleProperty<Color>("--core-color");

        private readonly VisualElement _el;
        private Color _ring = new Color(0.27f, 0.86f, 0.9f);
        private Color _core = new Color(0.27f, 0.86f, 0.9f, 0.35f);
        private float _time;
        private float _spin;
        private float _voice;
        private float _skip;
        private float _build = 1f;

        public AiOrb(VisualElement el)
        {
            _el = el;
            el.pickingMode = PickingMode.Ignore;
            el.generateVisualContent += Draw;
            el.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                if (e.customStyle.TryGetValue(RingColor, out Color r))
                {
                    _ring = r;
                }

                if (e.customStyle.TryGetValue(CoreColor, out Color c))
                {
                    _core = c;
                }

                _el.MarkDirtyRepaint();
            });
        }

        /// <summary>Speech energy 0..1 (eases toward the value).</summary>
        public float Voice { get; set; }

        /// <summary>Corruption stutter 0..1.</summary>
        public float Stutter { get; set; }

        /// <summary>Boot (idea 53): rings unfold from the heart outward over about a second.</summary>
        public void Assemble()
        {
            _build = Motion.Reduced ? 1f : 0f;
        }

        /// <summary>A short brighter flash when the AI "thinks" (idea 50).</summary>
        public void Ping()
        {
            _voice = 1f;
        }

        public void Tick(float dt)
        {
            _time += dt;
            _build = Mathf.Min(1f, _build + (dt * 0.9f));
            _voice = Mathf.Lerp(_voice, Voice, 1f - Mathf.Exp(-dt * 6f));
            if (!Motion.Reduced)
            {
                float speed = InterfaceConfig.Current.orb.ringSpeed * (1f + (_voice * 2.5f));
                _spin += dt * speed;
                if (Stutter > 0f && Random.value < Stutter * dt * 3f)
                {
                    _skip = Random.Range(-0.6f, 0.6f) * Stutter;
                }

                _skip = Mathf.Lerp(_skip, 0f, 1f - Mathf.Exp(-dt * 4f));
            }

            _el.MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = _el.contentRect;
            float size = Mathf.Min(r.width, r.height);
            if (size <= 0f)
            {
                return;
            }

            Vector2 c = r.center;
            float R = size * 0.5f * Ease.OutBack(_build);
            if (R <= 1f)
            {
                return;
            }

            float wobble = Motion.Reduced ? 0f : Mathf.Sin(_time * 2.1f) * InterfaceConfig.Current.orb.wobble;
            float a = _spin + _skip;

            // glow and heart
            Color glow = _core;
            glow.a *= 0.55f + (0.45f * _voice);
            Mesh2D.Disc(ctx, c, R * (0.62f + (0.06f * _voice)), glow, new Color(glow.r, glow.g, glow.b, 0f));
            float heart = R * (0.2f + (0.05f * wobble) + (0.07f * _voice));
            Mesh2D.Disc(ctx, c, heart, Color.Lerp(_ring, Color.white, 0.55f), new Color(_ring.r, _ring.g, _ring.b, 0.15f));

            // outer tick dial
            Color faint = _ring;
            faint.a = 0.35f;
            Mesh2D.Arc(ctx, c, R * 0.93f, 1.5f, 0f, Mathf.PI * 2f, faint);
            const int ticks = 36;
            var tick = new Vector2[2];
            for (int i = 0; i < ticks; i++)
            {
                float t = (a * 0.25f) + (Mathf.PI * 2f * i / ticks);
                var d = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
                float inner = i % 9 == 0 ? 0.8f : 0.86f;
                tick[0] = c + (d * R * inner);
                tick[1] = c + (d * R * 0.91f);
                Mesh2D.Polyline(ctx, tick, 2, i % 9 == 0 ? 2.5f : 1.2f, faint);
            }

            // three segment ring, clockwise
            for (int i = 0; i < 3; i++)
            {
                float s = a + (i * Mathf.PI * 2f / 3f);
                Mesh2D.Arc(ctx, c, R * 0.72f, 4f, s, s + 1.25f, _ring);
            }

            // dashed counter ring
            Color mid = _ring;
            mid.a = 0.7f;
            for (int i = 0; i < 12; i++)
            {
                float s = (-a * 1.6f) + (i * Mathf.PI * 2f / 12f);
                Mesh2D.Arc(ctx, c, R * 0.52f, 2f, s, s + 0.32f, mid);
            }
        }
    }

    /// <summary>A small voice waveform (idea 47): bars that move with <see cref="AiOrb.Voice"/>; flat when silent.</summary>
    public sealed class AiWave
    {
        private static readonly CustomStyleProperty<Color> WaveColor = new CustomStyleProperty<Color>("--wave-color");

        private const int Bars = 18;
        private readonly VisualElement _el;
        private readonly float[] _h = new float[Bars];
        private Color _color = new Color(0.27f, 0.86f, 0.9f);
        private float _time;

        public AiWave(VisualElement el)
        {
            _el = el;
            el.pickingMode = PickingMode.Ignore;
            el.generateVisualContent += Draw;
            el.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                if (e.customStyle.TryGetValue(WaveColor, out Color w))
                {
                    _color = w;
                    _el.MarkDirtyRepaint();
                }
            });
        }

        public void Tick(float dt, float voice)
        {
            _time += dt;
            for (int i = 0; i < Bars; i++)
            {
                float target = Motion.Reduced ? voice * 0.5f : voice * (0.3f + (0.7f * Mathf.Abs(Mathf.Sin((_time * 9f) + (i * 1.7f)) * Mathf.Sin((_time * 3.3f) + (i * 0.6f)))));
                _h[i] = Mathf.Lerp(_h[i], target, 1f - Mathf.Exp(-dt * 14f));
            }

            _el.MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = _el.contentRect;
            if (r.width <= 0f)
            {
                return;
            }

            float step = r.width / Bars;
            for (int i = 0; i < Bars; i++)
            {
                float h = Mathf.Max(2f, _h[i] * r.height);
                Mesh2D.Quad(ctx, new Rect(r.x + (i * step) + 1f, r.center.y - (h * 0.5f), step - 3f, h), _color);
            }
        }
    }
}
