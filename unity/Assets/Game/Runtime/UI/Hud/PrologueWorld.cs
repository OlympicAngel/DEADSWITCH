using Deadswitch.Host.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// The prologue's world (SPEC-043 s4): the curve of the planet with its cities as points of light. The lights
    /// follow the story: lit before the war, flashing red through it, going out one by one in the blackout, and a
    /// single survivor pulsing where the bunker is. Colours come from the USS custom properties on the element.
    /// </summary>
    public sealed class PrologueWorld
    {
        private const int Cities = 23;
        private const int Bunker = 14;

        private static readonly CustomStyleProperty<Color> LitColor = new CustomStyleProperty<Color>("--lit-color");
        private static readonly CustomStyleProperty<Color> WarColor = new CustomStyleProperty<Color>("--war-color");
        private static readonly CustomStyleProperty<Color> RimColor = new CustomStyleProperty<Color>("--rim-color");

        private readonly VisualElement _el;
        private readonly float[] _x = new float[Cities];
        private readonly float[] _phase = new float[Cities];
        private Color _lit = new Color(0.27f, 0.86f, 0.9f);
        private Color _war = new Color(0.85f, 0.36f, 0.32f);
        private Color _rim = new Color(0.27f, 0.86f, 0.9f, 0.5f);
        private PrologueMood _mood;
        private float _inMood;
        private float _time;

        public PrologueWorld(VisualElement el)
        {
            _el = el;
            _el.generateVisualContent += Draw;
            _el.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                e.customStyle.TryGetValue(LitColor, out _lit);
                e.customStyle.TryGetValue(WarColor, out _war);
                e.customStyle.TryGetValue(RimColor, out _rim);
                _el.MarkDirtyRepaint();
            });

            // fixed, hand-spread positions (presentation only, no RNG): denser toward the middle like a coastline
            for (int i = 0; i < Cities; i++)
            {
                float u = (i + 0.5f) / Cities;
                _x[i] = 0.06f + (0.88f * (u + (0.035f * Mathf.Sin(i * 2.7f))));
                _phase[i] = i * 0.618f % 1f;
            }
        }

        public void SetMood(PrologueMood mood)
        {
            _mood = mood;
            _inMood = 0f;
            _el.MarkDirtyRepaint();
        }

        public void Tick(float dt)
        {
            _time += dt;
            _inMood += dt;
            _el.MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = _el.contentRect;
            if (r.width <= 0f || r.height <= 0f)
            {
                return;
            }

            // a shallow arc: a circle far below the element so only its crown shows
            float radius = r.width * 1.6f;
            Vector2 center = new Vector2(r.center.x, r.yMin + (r.height * 0.42f) + radius);
            float half = Mathf.Asin(Mathf.Min(1f, r.width * 0.62f / radius));
            float top = -Mathf.PI * 0.5f;
            Color rim = _rim;
            rim.a *= _mood == PrologueMood.Boot ? Mathf.Clamp01(_inMood * 0.5f) : 1f;
            Mesh2D.Arc(ctx, center, radius, 3f, top - half, top + half, rim);
            Color haze = rim;
            haze.a *= 0.18f;
            Mesh2D.Arc(ctx, center, radius - 18f, 30f, top - half, top + half, haze);

            for (int i = 0; i < Cities; i++)
            {
                float a = top + ((_x[i] - 0.5f) * 2f * half * 0.94f);
                Vector2 p = center + (new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (radius - 10f));
                Light(ctx, p, i);
            }
        }

        private void Light(MeshGenerationContext ctx, Vector2 p, int i)
        {
            float pulse = Motion.Reduced ? 0.8f : 0.6f + (0.4f * Mathf.Sin((_time * 3f) + (_phase[i] * 6.28f)));
            Color c;
            float size = 7f;
            switch (_mood)
            {
                case PrologueMood.Boot:
                    return;
                case PrologueMood.Before:
                    // the cities come on one by one
                    c = _lit;
                    c.a = Mathf.Clamp01((_inMood * 6f) - (_phase[i] * 3f)) * pulse;
                    break;
                case PrologueMood.War:
                    // flashes travel the arc; each city burns red
                    float flash = Motion.Reduced ? 1f : Mathf.Clamp01(1f - Mathf.Abs(Mathf.Repeat((_inMood * 0.9f) + _phase[i], 1.6f) - 0.2f) * 4f);
                    c = Color.Lerp(_war, Color.white, flash * 0.6f);
                    c.a = 0.55f + (0.45f * flash);
                    size = 7f + (flash * 9f);
                    break;
                case PrologueMood.Dark:
                    // they go out, the bunker last but faint
                    float outAt = 0.4f + (_phase[i] * 3f);
                    c = _war;
                    c.a = Mathf.Clamp01(1f - ((_inMood - outAt) * 1.5f)) * 0.8f;
                    break;
                default:
                    if (i != Bunker)
                    {
                        return;
                    }

                    c = _lit;
                    c.a = pulse;
                    size = 9f + (pulse * 5f);
                    Mesh2D.Disc(ctx, p, size * 3.4f, new Color(c.r, c.g, c.b, 0.3f * pulse), new Color(c.r, c.g, c.b, 0f));
                    break;
            }

            if (c.a <= 0.01f)
            {
                return;
            }

            Mesh2D.Disc(ctx, p, size * 2.4f, new Color(c.r, c.g, c.b, c.a * 0.35f), new Color(c.r, c.g, c.b, 0f));
            Mesh2D.Disc(ctx, p, size * 0.5f, Color.Lerp(c, Color.white, 0.5f), c);
        }
    }
}
