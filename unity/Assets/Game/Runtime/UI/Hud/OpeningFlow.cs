using Deadswitch.Game.Base;
using Deadswitch.Game.Core;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// The opening (SPEC-009, SPEC-043 s4), new runs only. A letterboxed film on the real Hub: black and a heartbeat,
    /// then six short subtitled beats while <see cref="OpeningFilm"/> shows them (the compound lit, the war, the
    /// blackout rolling in, the core waking and powering the lights back on), and the handler card with TAKE CONTROL.
    /// The HUD stays hidden until then and comes online piece by piece. Tap finishes a line or moves on; SKIP ends it.
    /// Reduced motion or cinematics off: no camera or light moves, the cards wait for a tap. The objective guide then
    /// points at the control to use.
    /// </summary>
    public sealed class OpeningFlow
    {
        private const float RevealStagger = 0.28f;
        private const float RevealDelay = 0.45f;
        private const float HoldSeconds = 2.4f;
        private const float FadeInSeconds = 0.7f;
        private const string GuideOffKey = "ds.guide.off";

        private readonly GameHost _host;
        private readonly VisualElement _hud;
        private readonly AdvisorVoice _voice;
        private readonly Base.BaseScreen _base;
        private readonly VisualElement _guide;
        private readonly InterfaceConfig.OpeningRules _rules = InterfaceConfig.Current.opening;
        private VisualElement _prologue;
        private VisualElement _black;
        private VisualElement _crt;
        private VisualElement _barTop;
        private VisualElement _barBottom;
        private VisualElement _rec;
        private VisualElement _sub;
        private VisualElement _hand;
        private VisualElement _tap;
        private VisualElement _flashEl;
        private Label _kicker;
        private Label _line;
        private Label _handLine;
        private AiOrb _orb;
        private OpeningFilm _film;
        private int _card = -1;
        private float _shown;
        private float _beat;
        private float _flash;
        private float _bars;
        private float _clock;
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

        /// <summary>Plays the opening again (a new game started from Settings).</summary>
        public void Restart()
        {
            if (_prologue == null)
            {
                _card = -1;
                StartPrologue();
            }

            _stepKnown = false;
            RefreshGuide();
        }

        private static bool Cinematic => !Motion.Reduced && (GameHost.Instance == null || GameHost.Instance.Settings.Cinematics) && DroneCamera.Instance != null && BaseView.Instance != null;

        private PrologueScene Scene => Prologue.Scenes[_card];

        private bool Last => _card == Prologue.Scenes.Length - 1;

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
            _black = tree.Q("op-black");
            _crt = tree.Q("op-crt");
            _barTop = tree.Q("op-bar-top");
            _barBottom = tree.Q("op-bar-bottom");
            _rec = tree.Q("op-rec");
            _sub = tree.Q("op-sub");
            _hand = tree.Q("op-hand");
            _tap = tree.Q("op-tap");
            _flashEl = tree.Q("op-flash");
            _kicker = tree.Q<Label>("op-kicker");
            _line = tree.Q<Label>("op-line");
            _handLine = tree.Q<Label>("op-hand-line");
            tree.Q("op-skip").RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                EndPrologue();
            });
            tree.Q("op-take").RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                EndPrologue();
            });

            // tap anywhere: finish the line being typed, or move to the next beat
            tree.Q("prologue").RegisterCallback<ClickEvent>(_ => Advance());
            _orb = new AiOrb(tree.Q("op-orb"));
            _orb.Assemble();

            // the HUD is not there yet: it comes online when the handler takes control
            foreach (VisualElement child in _hud.Children())
            {
                Motion.Cancel(child);
                child.style.opacity = 0f;
            }

            Motion.Cancel(UiRoot.Instance.World);
            UiRoot.Instance.World.style.opacity = 0f;
            _black.style.opacity = 1f;
            _bars = 0f;
            _clock = 0f;
            _film = Cinematic ? OpeningFilm.Create() : null;
            NextCard();
        }

        private void TickPrologue(float dt)
        {
            _beat += dt;
            _clock += dt;
            PrologueMood mood = Scene.Mood;
            Label target = Last ? _handLine : _line;
            bool typing = _shown < Scene.Text.Length;

            // black until the core's signal finds the handler: a CRT trace draws across, then the picture fades in
            if (mood == PrologueMood.Boot)
            {
                float crt = Motion.Reduced ? 1f : Mathf.Clamp01(_beat / _rules.crtSeconds);
                _crt.style.width = Length.Percent(Ease.OutCubic(crt) * 100f);
                _crt.style.opacity = Motion.Reduced ? 1f : 0.6f + (0.4f * Mathf.PerlinNoise(_clock * 18f, 0.2f));
            }
            else
            {
                _black.style.opacity = Motion.Reduced ? 0f : 1f - Mathf.Clamp01(_beat / FadeInSeconds);
            }

            // letterbox bars close in once the picture is up
            float barsTarget = mood == PrologueMood.Boot ? 0f : 1f;
            _bars = Motion.Reduced ? barsTarget : Mathf.MoveTowards(_bars, barsTarget, dt * 1.8f);
            Length bar = Length.Percent(_rules.letterboxPct * Ease.InOutSine(_bars));
            _barTop.style.height = bar;
            _barBottom.style.height = bar;
            _rec.style.opacity = Mathf.Repeat(_clock, 1.2f) < 0.7f ? 1f : 0.2f;

            if (_flash > 0f)
            {
                _flash = Mathf.Max(0f, _flash - (dt * 1.6f));
                _flashEl.style.opacity = _flash;
            }

            if (Last)
            {
                _orb.Voice = typing ? 1f : 0.2f;
                _orb.Tick(dt);
            }

            if (typing)
            {
                _shown = Motion.Reduced ? Scene.Text.Length : _shown + (dt * _rules.charsPerSecond);
                int n = Mathf.Min(Scene.Text.Length, (int)_shown);
                target.text = Scene.Text.Substring(0, n) + (n < Scene.Text.Length ? "_" : string.Empty);
                return;
            }

            // the last beat waits for the handler; the others move on with their shot (reduced motion waits for a tap)
            _tap.EnableInClassList("is-hidden", Last);
            if (Last || Motion.Reduced)
            {
                return;
            }

            float seconds = _film != null ? _film.Seconds(mood) : HoldSeconds + (Scene.Text.Length * 0.03f);
            if (_beat >= seconds)
            {
                NextCard();
            }
        }

        private void Advance()
        {
            if (_prologue == null)
            {
                return;
            }

            if (_shown < Scene.Text.Length)
            {
                _shown = Scene.Text.Length;
                (Last ? _handLine : _line).text = Scene.Text;
                return;
            }

            if (!Last)
            {
                NextCard();
            }
        }

        private void NextCard()
        {
            _card++;
            if (_card >= Prologue.Scenes.Length)
            {
                EndPrologue();
                return;
            }

            PrologueScene scene = Scene;
            _shown = 0f;
            _beat = 0f;
            _line.text = string.Empty;
            _handLine.text = string.Empty;
            _kicker.text = scene.Kicker;
            _prologue.Q<Label>("op-hand-kicker").text = scene.Kicker;
            foreach (PrologueMood m in (PrologueMood[])System.Enum.GetValues(typeof(PrologueMood)))
            {
                _prologue.Q("prologue").EnableInClassList("op--" + m.ToString().ToLowerInvariant(), m == scene.Mood);
            }

            _sub.style.display = Last ? DisplayStyle.None : DisplayStyle.Flex;
            _hand.EnableInClassList("is-hidden", !Last);
            _tap.EnableInClassList("is-hidden", true);
            if (Last)
            {
                _orb.Ping();
                Motion.To(_hand, 0.6f, Ease.OutCubic, t =>
                {
                    _hand.style.opacity = t;
                    _hand.style.translate = new Translate(0, 40f * (1f - t));
                });
            }

            UiRoot.Instance.SetGlitch(scene.Mood == PrologueMood.Dark ? 0.7f : scene.Mood == PrologueMood.Boot ? 0.5f : scene.Mood == PrologueMood.War ? 0.35f : 0.1f);
            if (scene.Mood == PrologueMood.War && !Motion.Reduced)
            {
                _flash = 0.8f;
                Feedback.Alert();
            }

            Motion.To(_sub, 0.45f, Ease.OutCubic, t =>
            {
                _sub.style.opacity = t;
                _sub.style.translate = new Translate(0, 24f * (1f - t));
            });
            VisualElement pips = _prologue.Q("op-pips");
            for (int i = 0; i < pips.childCount; i++)
            {
                pips[i].EnableInClassList("is-on", i <= _card);
            }

            _film?.Play(scene.Mood);
        }

        private void EndPrologue()
        {
            if (_prologue == null)
            {
                return;
            }

            VisualElement tree = _prologue;
            _prologue = null;
            UiRoot.Instance.SetGlitch(0f);
            _voice.Paused = false;
            _film?.Finish();
            _film = null;
            if (Motion.Reduced)
            {
                tree.RemoveFromHierarchy();
            }
            else
            {
                Motion.To(tree, 0.5f, Ease.OutCubic, t => tree.style.opacity = 1f - t, tree.RemoveFromHierarchy);
            }

            // Boot sequence (rule 5): the HUD comes online piece by piece while the camera settles on the drone;
            // the top bar, the resource frame and the labels first, the rest together after them
            var parts = new System.Collections.Generic.List<VisualElement> { _hud.Q("topbar"), _hud.Q("frame"), UiRoot.Instance.World };
            foreach (VisualElement child in _hud.Children())
            {
                if (!parts.Contains(child))
                {
                    parts.Add(child);
                }
            }

            for (int i = 0; i < parts.Count; i++)
            {
                VisualElement el = parts[i];
                if (Motion.Reduced)
                {
                    el.style.opacity = StyleKeyword.Null;
                    continue;
                }

                Motion.After(el, RevealDelay + (Mathf.Min(i, 3) * RevealStagger), 0.35f, Ease.OutCubic, t => el.style.opacity = t, () => el.style.opacity = StyleKeyword.Null);
            }
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
