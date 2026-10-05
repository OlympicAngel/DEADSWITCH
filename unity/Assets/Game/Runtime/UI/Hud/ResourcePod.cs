using Deadswitch.Game.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// One resource pod in the top bar (SPEC-039 ideas 11-13, 19): animated value, capacity bar, rate, time to
    /// full or empty and a state flag (FULL / LOW / SHORT as a word and a color). Pulses when its state worsens.
    /// </summary>
    public sealed class ResourcePod
    {
        private readonly VisualElement _root;
        private readonly AnimatedNumber _value;
        private readonly Label _cap;
        private readonly Label _rate;
        private readonly Label _eta;
        private readonly VisualElement _fill;
        private readonly VisualElement _flag;
        private readonly Label _flagLabel;
        private ResState _state = ResState.Ok;
        private float _fillShown = -1f;
        private float _fillTarget;

        public ResourcePod(VisualElement hud, string prefix, ResKind kind, System.Action<ResKind> onTap)
        {
            Kind = kind;
            _root = hud.Q(prefix + "-cell");
            _value = new AnimatedNumber(hud.Q<Label>(prefix + "-value"), Fmt.Compact);
            _cap = hud.Q<Label>(prefix + "-cap");
            _rate = hud.Q<Label>(prefix + "-rate");
            _eta = hud.Q<Label>(prefix + "-eta");
            _fill = hud.Q(prefix + "-fill");
            _flag = hud.Q(prefix + "-flag");
            _flagLabel = hud.Q<Label>(prefix + "-flag-label");
            _root.RegisterCallback<ClickEvent>(_ => onTap(kind));
        }

        public ResKind Kind { get; }

        public VisualElement Root => _root;

        public void SetVisible(bool visible)
        {
            _root.EnableInClassList("is-hidden", !visible);
        }

        public void Set(ResourceInfo r, double secondsPerGameHour, bool instant, string rateOverride = null)
        {
            _value.Set(r.Value, instant);
            _cap.text = "/" + Fmt.Compact(r.Cap);
            _fillTarget = r.Fill;
            if (instant || _fillShown < 0f)
            {
                _fillShown = _fillTarget;
                _fill.style.width = Length.Percent(_fillShown * 100f);
            }

            if (rateOverride != null)
            {
                _rate.text = rateOverride;
                _rate.EnableInClassList("is-down", false);
                _rate.EnableInClassList("is-flat", true);
            }
            else
            {
                _rate.text = r.HasRate ? Fmt.Signed(r.PerHour) + "/h" : "--";
                _rate.EnableInClassList("is-down", r.HasRate && r.PerHour < 0);
                _rate.EnableInClassList("is-flat", !r.HasRate || r.PerHour == 0);
            }

            double hours = r.Hours;
            _eta.text = hours < 0 ? string.Empty : (r.PerHour > 0 ? "FULL " : "EMPTY ") + Fmt.SpanCoarse(hours * secondsPerGameHour);

            _root.EnableInClassList("is-low", r.State == ResState.Low);
            _root.EnableInClassList("is-short", r.State == ResState.Short);
            _root.EnableInClassList("is-full", r.State == ResState.Full);
            _flag.EnableInClassList("is-hidden", r.State == ResState.Ok);
            _flagLabel.text = ResourceInfo.StateName(r.State);
            if (!instant && Worse(r.State, _state))
            {
                Pulse();
            }

            _state = r.State;
        }

        public void Tick(float dt)
        {
            _value.Tick(dt);
            if (Mathf.Abs(_fillShown - _fillTarget) > 0.001f)
            {
                _fillShown = Motion.Reduced ? _fillTarget : Mathf.Lerp(_fillShown, _fillTarget, 1f - Mathf.Exp(-dt * 6f));
                _fill.style.width = Length.Percent(_fillShown * 100f);
            }
        }

        /// <summary>A brief white flash of the border: something changed for the worse here.</summary>
        public void Pulse()
        {
            _root.AddToClassList("is-pulse");
            _root.schedule.Execute(() => _root.RemoveFromClassList("is-pulse")).StartingIn(260);
            Motion.To(_root, 0.36f, Ease.OutBack, t => _root.style.scale = new Scale(new Vector3(1.06f - (0.06f * t), 1.06f - (0.06f * t), 1f)));
        }

        private static bool Worse(ResState now, ResState before)
        {
            return Rank(now) > Rank(before);
        }

        private static int Rank(ResState s)
        {
            return s == ResState.Short ? 3 : s == ResState.Low ? 2 : s == ResState.Full ? 1 : 0;
        }
    }
}
