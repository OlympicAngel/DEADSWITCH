using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Interface choreography (SPEC-040): staggered entrances when a screen or page appears, meter sweeps, press
    /// ripples on every tappable surface, and a slow shimmer across primary actions. All of it is decoration on top
    /// of a readable static end state: reduced motion skips straight to it, and nothing here delays input.
    /// </summary>
    public static class Choreo
    {
        private const float StaggerStep = 0.045f;
        private const int StaggerMax = 14;
        private const float ShimmerEvery = 4.5f;

        private static readonly List<VisualElement> Shimmers = new List<VisualElement>();
        private static float _shimmerClock;

        /// <summary>Hooks ripples on a root (once) and the shimmer clock.</summary>
        public static void Install(VisualElement root)
        {
            root.RegisterCallback<PointerDownEvent>(OnPress, TrickleDown.TrickleDown);
            UiRoot.Instance.Frame += Tick;
        }

        /// <summary>Cards, sections, tiles and rows under <paramref name="root"/> rise in one after another.</summary>
        public static void Enter(VisualElement root)
        {
            if (Motion.Reduced || root == null)
            {
                return;
            }

            int n = 0;
            root.Query(className: "ds-card").ForEach(el => Rise(el, ref n));
            root.Query(className: "ds-section").ForEach(el => Rise(el, ref n));
            root.Query(className: "ds-meter").ForEach(Sweep);
        }

        /// <summary>Registers a primary action for the periodic light sweep.</summary>
        public static void Shimmer(VisualElement button)
        {
            if (button.Q(className: "ds-shimmer") != null)
            {
                return;
            }

            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("ds-shimmer");
            button.Add(bar);
            Shimmers.Add(button);
        }

        /// <summary>A brief punch (scale up and settle) for something that just changed.</summary>
        public static void Punch(VisualElement el, float amount = 0.08f)
        {
            Motion.To(el, 0.36f, Ease.OutBack, t =>
            {
                float s = 1f + (amount * (1f - t));
                el.style.scale = new Scale(new Vector3(s, s, 1f));
            });
        }

        private static void Rise(VisualElement el, ref int n)
        {
            if (el.resolvedStyle.display == DisplayStyle.None || n >= StaggerMax)
            {
                return;
            }

            float delay = n * StaggerStep;
            n++;
            Motion.After(el, delay, 0.32f, Ease.OutCubic, t =>
            {
                el.style.opacity = t;
                el.style.translate = new Translate(0, (1f - t) * 28f, 0);
            });
        }

        /// <summary>Lit segments come on left to right.</summary>
        private static void Sweep(VisualElement meter)
        {
            int count = meter.childCount;
            if (count == 0)
            {
                return;
            }

            Motion.After(meter, 0.12f, 0.5f, Ease.OutCubic, t =>
            {
                for (int i = 0; i < count; i++)
                {
                    meter[i].style.opacity = i <= t * count ? 1f : 0.25f;
                }
            }, () =>
            {
                for (int i = 0; i < count; i++)
                {
                    meter[i].style.opacity = StyleKeyword.Null;
                }
            });
        }

        private static void OnPress(PointerDownEvent e)
        {
            if (Motion.Reduced)
            {
                return;
            }

            VisualElement target = Tappable(e.target as VisualElement);
            if (target == null)
            {
                return;
            }

            Vector2 local = target.WorldToLocal(e.position);
            float size = Mathf.Max(target.layout.width, target.layout.height) * 2.2f;
            var ripple = new VisualElement { pickingMode = PickingMode.Ignore };
            ripple.AddToClassList("ds-ripple");
            ripple.style.left = local.x - (size / 2f);
            ripple.style.top = local.y - (size / 2f);
            ripple.style.width = size;
            ripple.style.height = size;
            ripple.style.borderTopLeftRadius = size / 2f;
            ripple.style.borderTopRightRadius = size / 2f;
            ripple.style.borderBottomLeftRadius = size / 2f;
            ripple.style.borderBottomRightRadius = size / 2f;
            target.Add(ripple);
            Motion.To(ripple, 0.45f, Ease.OutCubic, t =>
            {
                ripple.style.scale = new Scale(new Vector3(0.05f + (0.95f * t), 0.05f + (0.95f * t), 1f));
                ripple.style.opacity = 0.35f * (1f - t);
            }, ripple.RemoveFromHierarchy);
        }

        private static VisualElement Tappable(VisualElement v)
        {
            for (int depth = 0; v != null && depth < 5; depth++, v = v.parent)
            {
                if (v.ClassListContains("ds-btn") || v.ClassListContains("ds-tile") || v.ClassListContains("ai-chip") || v.ClassListContains("hud-tab") || v.ClassListContains("ds-pager__tab") || v.ClassListContains("rail-pill"))
                {
                    return v;
                }
            }

            return null;
        }

        private static void Tick(float dt)
        {
            if (Motion.Reduced || Shimmers.Count == 0)
            {
                return;
            }

            _shimmerClock += dt;
            if (_shimmerClock < ShimmerEvery)
            {
                return;
            }

            _shimmerClock = 0f;
            for (int i = Shimmers.Count - 1; i >= 0; i--)
            {
                VisualElement b = Shimmers[i];
                if (b.panel == null)
                {
                    Shimmers.RemoveAt(i);
                    continue;
                }

                VisualElement bar = b.Q(className: "ds-shimmer");
                if (b.resolvedStyle.display == DisplayStyle.None || b.ClassListContains("is-disabled"))
                {
                    // a sweep cut short (screen hidden, button disabled) must not leave the bar parked on the face
                    bar.style.left = -120f;
                    continue;
                }

                float w = b.layout.width;
                Motion.To(bar, 0.9f, Ease.InOutSine, t => bar.style.left = Mathf.Lerp(-120f, w + 40f, t));
            }
        }
    }
}
