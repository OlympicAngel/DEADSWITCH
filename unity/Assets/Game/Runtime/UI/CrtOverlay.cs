using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// The AI's display surface over everything: fine scanlines, a vignette, a slow roll bar and, as corruption
    /// rises, brief horizontal glitch slices (doc 11). Scaled by the effect-intensity setting; ignores input and
    /// stays faint enough never to hide values, warnings or controls.
    /// </summary>
    public sealed class CrtOverlay
    {
        private const float LineSpacing = 6f;
        private const float LineHeight = 2f;
        private const int VignetteCols = 6;
        private const int VignetteRows = 10;

        private readonly VisualElement _static;
        private readonly VisualElement _dynamic;
        private float _effects = 1f;
        private float _glitch;
        private float _time;
        private uint _rng = 0x9E3779B9u;
        private float _sliceY;
        private float _sliceH;
        private float _sliceLife;

        public CrtOverlay(VisualElement parent)
        {
            _static = Layer("ds-crt");
            _dynamic = Layer("ds-crt-dynamic");
            parent.Add(_static);
            parent.Add(_dynamic);
            _static.generateVisualContent += DrawStatic;
            _dynamic.generateVisualContent += DrawDynamic;
        }

        /// <param name="effects">0..1 effect intensity setting.</param>
        /// <param name="glitch">0..1 corruption glitch weight.</param>
        public void Configure(float effects, float glitch)
        {
            _effects = Mathf.Clamp01(effects);
            _glitch = Mathf.Clamp01(glitch);
            _static.MarkDirtyRepaint();
            _static.style.display = _effects > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            _dynamic.style.display = _effects > 0f ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Tick(float dt, bool reducedMotion)
        {
            if (_effects <= 0f)
            {
                return;
            }

            _time += dt;
            if (_sliceLife > 0f)
            {
                _sliceLife -= dt;
            }
            else if (!reducedMotion && _glitch > 0f && Next() < dt * 0.6f * _glitch)
            {
                Rect r = _dynamic.contentRect;
                _sliceY = Next() * r.height;
                _sliceH = 6f + (Next() * 38f * _glitch);
                _sliceLife = 0.05f + (Next() * 0.12f);
            }

            // Phosphor flicker: a tiny breathing of the overlay, never on the content itself.
            float flicker = reducedMotion ? 0f : (Mathf.Sin(_time * 50f) * 0.015f) + (Mathf.Sin(_time * 7.3f) * 0.01f);
            _dynamic.style.opacity = 1f + flicker;
            _dynamic.MarkDirtyRepaint();
        }

        private static VisualElement Layer(string cls)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(cls);
            e.style.position = Position.Absolute;
            e.style.left = 0;
            e.style.right = 0;
            e.style.top = 0;
            e.style.bottom = 0;
            return e;
        }

        private float Next()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return (_rng & 0xFFFFFF) / 16777216f;
        }

        private void DrawStatic(MeshGenerationContext ctx)
        {
            Rect r = _static.contentRect;
            if (r.width <= 0f)
            {
                return;
            }

            byte lineAlpha = (byte)(26 * _effects);
            var line = new Color32(0, 0, 0, lineAlpha);
            for (float y = 0; y < r.height; y += LineSpacing)
            {
                Mesh2D.Quad(ctx, new Rect(0, y, r.width, LineHeight), line);
            }

            // Vignette: a grid whose vertex alpha grows with distance from the center (elliptical falloff).
            MeshWriteData m = ctx.Allocate((VignetteCols + 1) * (VignetteRows + 1), VignetteCols * VignetteRows * 6);
            for (int j = 0; j <= VignetteRows; j++)
            {
                for (int i = 0; i <= VignetteCols; i++)
                {
                    float u = (float)i / VignetteCols;
                    float v = (float)j / VignetteRows;
                    float dx = (u - 0.5f) * 2f;
                    float dy = (v - 0.5f) * 2f;
                    float d = Mathf.Clamp01(Mathf.Sqrt((dx * dx) + (dy * dy)) / 1.414f);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d)) * 0.55f * (0.4f + (0.6f * _effects));
                    m.SetNextVertex(Mesh2D.V(u * r.width, v * r.height, new Color32(4, 6, 5, (byte)(a * 255))));
                }
            }

            for (int j = 0; j < VignetteRows; j++)
            {
                for (int i = 0; i < VignetteCols; i++)
                {
                    ushort tl = (ushort)((j * (VignetteCols + 1)) + i);
                    ushort tr = (ushort)(tl + 1);
                    ushort bl = (ushort)(tl + VignetteCols + 1);
                    ushort br = (ushort)(bl + 1);
                    m.SetNextIndex(tl);
                    m.SetNextIndex(tr);
                    m.SetNextIndex(br);
                    m.SetNextIndex(tl);
                    m.SetNextIndex(br);
                    m.SetNextIndex(bl);
                }
            }
        }

        private void DrawDynamic(MeshGenerationContext ctx)
        {
            Rect r = _dynamic.contentRect;
            if (r.width <= 0f)
            {
                return;
            }

            // Roll bar: a soft bright band drifting down the screen every ~9 s.
            float barH = r.height * 0.12f;
            float y = Mathf.Repeat(_time / 9f, 1f) * (r.height + barH) - barH;
            byte a = (byte)(10 * _effects);
            var clear = new Color32(168, 213, 138, 0);
            var mid = new Color32(168, 213, 138, a);
            Mesh2D.GradientQuad(ctx, new Rect(0, y, r.width, barH * 0.5f), clear, mid);
            Mesh2D.GradientQuad(ctx, new Rect(0, y + (barH * 0.5f), r.width, barH * 0.5f), mid, clear);

            if (_sliceLife > 0f)
            {
                byte sa = (byte)(40 * _effects * _glitch);
                Mesh2D.Quad(ctx, new Rect(0, _sliceY, r.width, _sliceH), new Color32(193, 118, 180, sa));
                Mesh2D.Quad(ctx, new Rect(r.width * 0.1f, _sliceY + (_sliceH * 0.3f), r.width * 0.6f, 2f), new Color32(240, 235, 221, (byte)(sa * 2)));
            }
        }
    }
}
