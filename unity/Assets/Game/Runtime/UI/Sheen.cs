using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Depth without shadows (SPEC-041): a soft vertical gradient drawn inside an element. <c>.ds-sheen</c> lights
    /// the top of a surface (--sheen-color fading out over --sheen-reach of its height); <c>.ds-scrim</c> darkens
    /// from one edge so HUD text reads over the 3D world (<c>ds-scrim--up</c> darkens from the bottom). Kit adds
    /// the sheen to cards, tiles, buttons and pods; tools/uipreview mirrors it with CSS gradients.
    /// </summary>
    public static class Sheen
    {
        private static readonly CustomStyleProperty<Color> SheenColor = new CustomStyleProperty<Color>("--sheen-color");
        private static readonly CustomStyleProperty<float> SheenReach = new CustomStyleProperty<float>("--sheen-reach");

        private static readonly string[] Surfaces = { "ds-card", "ds-tile", "ds-btn", "pod", "hud-advisor", "ds-sheet" };

        public static void Decorate(VisualElement root)
        {
            foreach (string cls in Surfaces)
            {
                root.Query(className: cls).ForEach(Attach);
            }

            root.Query(className: "ds-scrim").ForEach(Attach);
        }

        /// <summary>Adds the gradient to one element (idempotent; safe for code-built elements).</summary>
        public static void Attach(VisualElement el)
        {
            if (el.ClassListContains("ds-sheen"))
            {
                return;
            }

            el.AddToClassList("ds-sheen");
            var state = new State { Color = new Color(0.59f, 0.9f, 0.94f, 0.07f), Reach = 0.6f };
            el.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                if (e.customStyle.TryGetValue(SheenColor, out Color c))
                {
                    state.Color = c;
                }

                if (e.customStyle.TryGetValue(SheenReach, out float r))
                {
                    state.Reach = Mathf.Clamp01(r);
                }

                el.MarkDirtyRepaint();
            });
            el.generateVisualContent += ctx => Draw(ctx, el, state);
        }

        private static void Draw(MeshGenerationContext ctx, VisualElement el, State state)
        {
            float w = el.layout.width;
            float h = el.layout.height;
            if (float.IsNaN(w) || w <= 2f || h <= 2f || state.Color.a <= 0f)
            {
                return;
            }

            IResolvedStyle s = el.resolvedStyle;
            float left = s.borderLeftWidth;
            float top = s.borderTopWidth;
            float right = s.borderRightWidth;
            float bottom = s.borderBottomWidth;
            float inner = h - top - bottom;
            float reach = inner * state.Reach;
            var clear = new Color(state.Color.r, state.Color.g, state.Color.b, 0f);
            if (el.ClassListContains("ds-scrim--up"))
            {
                Mesh2D.GradientQuad(ctx, new Rect(left, h - bottom - reach, w - left - right, reach), clear, state.Color);
            }
            else
            {
                Mesh2D.GradientQuad(ctx, new Rect(left, top, w - left - right, reach), state.Color, clear);
            }
        }

        private sealed class State
        {
            public Color Color;
            public float Reach;
        }
    }
}
