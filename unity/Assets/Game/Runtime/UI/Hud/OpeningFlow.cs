using Deadswitch.Game.Core;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// The opening (SPEC-009): the prologue over the dimmed drone feed (new runs only, skippable), the HUD coming
    /// online in steps, and the objective guide that points at the control to use. Reduced motion skips typing
    /// and staggering; essential information never waits on animation.
    /// </summary>
    public sealed class OpeningFlow
    {
        private const float CharsPerSecond = 34f;
        private const float HoldSeconds = 1.9f;
        private const float RevealStagger = 0.32f;
        private const string GuideOffKey = "ds.guide.off";

        private readonly GameHost _host;
        private readonly VisualElement _hud;
        private readonly AdvisorVoice _voice;
        private readonly Base.BaseScreen _base;
        private readonly VisualElement _guide;
        private VisualElement _prologue;
        private Label _proText;
        private int _card = -1;
        private float _shown;
        private float _hold;
        private GuideStep _step = GuideStep.Done;
        private bool _stepKnown;
        private VisualElement _highlight;
        private float _pulse;

        public OpeningFlow(GameHost host, VisualElement hud, AdvisorVoice voice, Base.BaseScreen baseScreen)
        {
            _host = host;
            _hud = hud;
            _voice = voice;
            _base = baseScreen;
            _guide = hud.Q("guide");
            _guide.Q("guide-close").RegisterCallback<ClickEvent>(_ =>
            {
                PlayerPrefs.SetInt(GuideOffKey, 1);
                ShowGuide(default);
            });
            host.Ticked += RefreshGuide;

            if (host.IsNewRun)
            {
                StartPrologue();
            }

            RefreshGuide();
        }

        private static string[] RevealOrder => new[] { "topbar", "hud-right", "advisor", "tabbar" };

        public void Tick(float dt)
        {
            if (_prologue != null)
            {
                TickPrologue(dt);
            }

            if (_highlight != null && !Motion.Reduced)
            {
                _pulse += dt;
                float a = 0.55f + (0.45f * Mathf.Sin(_pulse * 5f));
                _highlight.style.borderTopColor = _highlight.style.borderBottomColor = _highlight.style.borderLeftColor = _highlight.style.borderRightColor = new Color(0.91f, 0.71f, 0.36f, a);
            }
        }

        private void StartPrologue()
        {
            _voice.Paused = true;
            TemplateContainer tree = UiRoot.Load("Opening");
            tree.style.position = Position.Absolute;
            tree.style.left = 0;
            tree.style.right = 0;
            tree.style.top = 0;
            tree.style.bottom = 0;
            tree.pickingMode = PickingMode.Position;
            UiRoot.Instance.Root.Add(tree);
            _prologue = tree;
            _proText = tree.Q<Label>("pro-text");
            tree.Q("pro-skip").RegisterCallback<ClickEvent>(_ => EndPrologue());
            UiRoot.Instance.SetGlitch(0.6f);
            foreach (string part in RevealOrder)
            {
                Group(part).style.opacity = 0f;
            }

            NextCard();
        }

        private void TickPrologue(float dt)
        {
            string text = Prologue.Cards[_card];
            if (_shown < text.Length)
            {
                _shown = Motion.Reduced ? text.Length : _shown + (dt * CharsPerSecond);
                int n = Mathf.Min(text.Length, (int)_shown);
                _proText.text = text.Substring(0, n) + (n < text.Length ? "_" : string.Empty);
                return;
            }

            _hold += dt;
            if (_hold >= HoldSeconds)
            {
                NextCard();
            }
        }

        private void NextCard()
        {
            _card++;
            if (_card >= Prologue.Cards.Length)
            {
                EndPrologue();
                return;
            }

            _shown = 0f;
            _hold = 0f;
            VisualElement pips = _prologue.Q("pro-pips");
            for (int i = 0; i < pips.childCount; i++)
            {
                pips[i].EnableInClassList("is-on", i <= _card);
            }
        }

        private void EndPrologue()
        {
            if (_prologue == null)
            {
                return;
            }

            _prologue.RemoveFromHierarchy();
            _prologue = null;
            UiRoot.Instance.SetGlitch(0f);
            _voice.Paused = false;

            // Boot sequence (rule 5): each system comes online in turn.
            string[] parts = RevealOrder;
            for (int i = 0; i < parts.Length; i++)
            {
                VisualElement el = Group(parts[i]);
                if (Motion.Reduced)
                {
                    el.style.opacity = 1f;
                    continue;
                }

                float delay = i * RevealStagger;
                Motion.To(el, 0.35f + delay, Ease.OutCubic, t => el.style.opacity = Mathf.Clamp01(((t * (0.35f + delay)) - delay) / 0.35f));
            }
        }

        private VisualElement Group(string name)
        {
            return name == "hud-right" ? _hud.Q(className: "hud-right") : _hud.Q(name);
        }

        private void RefreshGuide()
        {
            GameState s = _host.Sim.State;
            GuideObjective o = OpeningGuide.Current(s);
            if (_stepKnown && o.Step != _step && o.Step > _step)
            {
                _voice.Notify("guide_done");
            }

            _stepKnown = true;
            _step = o.Step;
            ShowGuide(PlayerPrefs.GetInt(GuideOffKey, 0) == 1 ? default : o);
        }

        private void ShowGuide(GuideObjective o)
        {
            bool on = o.Title != null && o.Title.Length > 0 && o.Step != GuideStep.Done && PlayerPrefs.GetInt(GuideOffKey, 0) == 0;
            _guide.EnableInClassList("is-hidden", !on);
            SetHighlight(null);
            _base.Guide(-1);
            if (!on)
            {
                return;
            }

            _guide.Q<Label>("guide-title").text = "OBJECTIVE // " + o.Title;
            _guide.Q<Label>("guide-reason").text = o.Reason;
            GameState s = _host.Sim.State;
            switch (o.Target)
            {
                case "defend":
                    SetHighlight(_hud.Q("raid-defend"));
                    break;
                case "tab-core":
                    SetHighlight(_hud.Q("tab-core"));
                    break;
                case "report":
                    SetHighlight(_hud.Q("tab-ops"));
                    break;
                case "empty-plot":
                    _base.Guide(s.Slots.FindIndex(x => x.IsEmpty));
                    break;
                case "generator":
                    _base.Guide(s.Slots.FindIndex(x => x.Kind == FacilityKind.Generator));
                    break;
            }
        }

        private void SetHighlight(VisualElement el)
        {
            if (_highlight != null)
            {
                _highlight.RemoveFromClassList("is-guide");
                _highlight.style.borderTopColor = _highlight.style.borderBottomColor = _highlight.style.borderLeftColor = _highlight.style.borderRightColor = StyleKeyword.Null;
            }

            _highlight = el;
            _highlight?.AddToClassList("is-guide");
        }
    }
}
