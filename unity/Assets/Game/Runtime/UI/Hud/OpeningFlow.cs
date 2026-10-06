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
        private VisualElement _rec;
        private VisualElement _sub;
        private VisualElement _hand;
        private VisualElement _tap;
        private VisualElement _flashEl;
        private VisualElement _whiteEl;
        private float _white;
        private Label _kicker;
        private Label _line;
        private Label _handLine;
        private AiOrb _orb;
        private WarRoom _room;
        private VisualElement _roomEl;
        private OpeningFilm _film;
        private int _card = -1;
        private float _shown;
        private float _beat;
        private float _flash;
        private float _clock;
        private GuideStep _step = GuideStep.Done;
        private bool _stepKnown;
        private VisualElement _highlight;
        private float _pulse;
        private VisualElement _restore;
        private VisualElement _restoreBtn;
        private int _restoreStep = -1;
        private bool _restoring;
        private bool _busy;
        private bool _holding;
        private float _holdT;
        private bool _woken;

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
                // run seeds can repeat: a new run's core is asleep whatever an earlier run did
                PlayerPrefs.DeleteKey(AwakeKey);
                StartPrologue(false);
            }
            else if (!CoreAwake && host.Sim.State.Tick < Deadswitch.Sim.SimConfig.TicksPerDay && Cinematic)
            {
                // the app closed before the core woke: back to the restore steps (SPEC-044 rule 1)
                StartPrologue(true);
            }

            RefreshGuide();
        }

        /// <summary>Plays the opening again (a new game started from Settings).</summary>
        public void Restart()
        {
            if (_prologue == null)
            {
                _card = -1;
                PlayerPrefs.DeleteKey(AwakeKey);
                StartPrologue(false);
            }

            _stepKnown = false;
            RefreshGuide();
        }

        private static bool Cinematic => !Motion.Reduced && (GameHost.Instance == null || GameHost.Instance.Settings.Cinematics) && DroneCamera.Instance != null && BaseView.Instance != null;

        private string AwakeKey => "ds.opening.awake." + _host.Sim.Seed;

        /// <summary>True once this run's core was woken (the opening is over for good).</summary>
        private bool CoreAwake => PlayerPrefs.GetInt(AwakeKey, 0) == 1;

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

        private void StartPrologue(bool resume)
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
            _rec = tree.Q("op-rec");
            _sub = tree.Q("op-sub");
            _hand = tree.Q("op-hand");
            _tap = tree.Q("op-tap");
            _flashEl = tree.Q("op-flash");
            _whiteEl = tree.Q("op-white");
            _kicker = tree.Q<Label>("op-kicker");
            _line = tree.Q<Label>("op-line");
            _handLine = tree.Q<Label>("op-hand-line");
            tree.Q("op-skip").RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                Skip();
            });
            tree.Q("op-take").RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                Skip();
            });
            _restore = tree.Q("op-restore");
            _restoreBtn = tree.Q("op-restore-btn");
            _restore.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            _restoreBtn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                OnRestore();
            });
            _restoreBtn.RegisterCallback<PointerDownEvent>(_ => _holding = IsCoreStep(_restoreStep), TrickleDown.TrickleDown);
            _restoreBtn.RegisterCallback<PointerUpEvent>(_ => _holding = false);
            _restoreBtn.RegisterCallback<PointerLeaveEvent>(_ => _holding = false);
            _restoring = false;
            _woken = false;
            _restoreStep = -1;

            // tap anywhere: finish the line being typed, or move to the next beat
            tree.Q("prologue").RegisterCallback<ClickEvent>(_ => Advance());
            _orb = new AiOrb(tree.Q("op-orb"));
            _roomEl = tree.Q("op-room");
            _room = new WarRoom(_roomEl);
            _orb.Assemble();
            VisualElement pips = tree.Q("op-pips");
            pips.Clear();
            for (int i = 0; i < Prologue.Scenes.Length; i++)
            {
                var pip = new VisualElement();
                pip.AddToClassList("op__pip");
                pips.Add(pip);
            }

            // the HUD is not there yet: it comes online when the handler takes control
            foreach (VisualElement child in _hud.Children())
            {
                Motion.Cancel(child);
                child.style.opacity = 0f;
            }

            Motion.Cancel(UiRoot.Instance.World);
            UiRoot.Instance.World.style.opacity = 0f;
            _black.style.opacity = 1f;
            _clock = 0f;
            OpeningGlobe.Prewarm();
            _film = Cinematic ? OpeningFilm.Create() : null;
            if (_film != null)
            {
                _film.Whiteout += strength => _white = Mathf.Max(_white, strength);
            }

            if (resume && _film != null)
            {
                _card = Prologue.Scenes.Length - 1;
                StartRestore();
                return;
            }

            NextCard();
        }

        private void TickPrologue(float dt)
        {
            if (_restoring)
            {
                TickRestore(dt);
                return;
            }

            _beat += dt;
            _clock += dt;
            PrologueMood mood = Scene.Mood;
            Label target = Last ? _handLine : _line;
            bool typing = _shown < Scene.Text.Length;

            // the log opens on black with a CRT trace; beats shot on the Hub fade the picture in
            bool hub = OpeningFilm.ShowsWorld(mood) && _film != null;
            _crt.style.display = mood == PrologueMood.Signal ? DisplayStyle.Flex : DisplayStyle.None;
            if (mood == PrologueMood.Signal)
            {
                float crt = Motion.Reduced ? 1f : Mathf.Clamp01(_beat / _rules.crtSeconds);
                _crt.style.width = Length.Percent(Ease.OutCubic(crt) * 100f);
                _crt.style.opacity = Motion.Reduced ? 1f : 0.6f + (0.4f * Mathf.PerlinNoise(_clock * 18f, 0.2f));
            }

            // the log opens on the CRT trace; the planet fades in behind it
            float fadeFrom = mood == PrologueMood.Signal ? _rules.crtSeconds + 0.5f : 0f;
            float black = hub ? 1f - Mathf.Clamp01((_beat - fadeFrom) / (mood == PrologueMood.Signal ? 2f : FadeInSeconds)) : 1f;
            _black.style.opacity = Motion.Reduced && hub ? 0f : black;

            _rec.style.opacity = Motion.Reduced || Mathf.Repeat(_clock, 1.2f) < 0.7f ? 1f : 0.2f;

            if (_flash > 0f)
            {
                _flash = Mathf.Max(0f, _flash - (dt * 1.6f));
                _flashEl.style.opacity = _flash;
            }

            // a warhead the size of a city: the frame burns white and fades back slowly
            _white = Mathf.Max(0f, _white - (dt * 0.55f));
            _whiteEl.style.opacity = Ease.InOutSine(_white);

            if (Last)
            {
                _orb.Voice = typing ? 1f : 0.2f;
                _orb.Tick(dt);
            }

            if (InRoom(mood))
            {
                _room.Tick(dt);
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

        /// <summary>SKIP or REACH THE CORE: the film is over; the restore steps follow (or the HUD without the film).</summary>
        private void Skip()
        {
            if (_film != null && !_restoring)
            {
                StartRestore();
            }
            else if (_film == null)
            {
                EndPrologue();
            }
        }

        private static bool IsCoreStep(int step)
        {
            return step == Prologue.Restore.Length - 1;
        }

        private void StartRestore()
        {
            _restoring = true;
            _sub.style.display = DisplayStyle.None;
            _hand.AddToClassList("is-hidden");
            _tap.AddToClassList("is-hidden");
            _prologue.Q("op-skip").style.display = DisplayStyle.None;
            _prologue.Q("op-pips").style.display = DisplayStyle.None;
            _roomEl.RemoveFromClassList("is-on");
            _black.style.opacity = 0f;
            _crt.style.display = DisplayStyle.None;
            UiRoot.Instance.SetGlitch(0.08f);
            _film.BeginRestore();
            ShowStep(0);
        }

        private void ShowStep(int step)
        {
            _restoreStep = step;
            RestoreStep s = Prologue.Restore[step];
            for (int i = 0; i < Prologue.Restore.Length; i++)
            {
                _prologue.Q("op-restore-icon-" + i).style.display = i == step ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _prologue.Q<Label>("op-restore-title").text = s.Title;
            _prologue.Q<Label>("op-restore-step").text = (step + 1) + "/" + Prologue.Restore.Length;
            _prologue.Q<Label>("op-restore-body").text = s.Body;
            _prologue.Q<Label>("op-restore-label").text = s.Action;
            _restore.EnableInClassList("op__restore--core", IsCoreStep(step));
            _restore.RemoveFromClassList("is-hidden");
            _restoreBtn.RemoveFromClassList("is-busy");
            _busy = false;
            _holdT = 0f;
            Motion.To(_restore, 0.45f, Ease.OutCubic, t =>
            {
                _restore.style.opacity = t;
                _restore.style.translate = new Translate(0, 40f * (1f - t));
            });
            if (IsCoreStep(step))
            {
                _film.FocusCore();
            }
            else
            {
                _film.FocusSlot(step);
            }
        }

        private void OnRestore()
        {
            if (_busy || IsCoreStep(_restoreStep))
            {
                return;
            }

            _busy = true;
            _restoreBtn.AddToClassList("is-busy");
            int done = _restoreStep;
            _film.Restore(done, () => ShowStep(done + 1));
        }

        private void TickRestore(float dt)
        {
            if (_woken || !IsCoreStep(_restoreStep))
            {
                return;
            }

            // WAKE THE CORE is held, not tapped: the longer the hand stays, the harder it answers
            _holdT = _holding ? _holdT + dt : Mathf.Max(0f, _holdT - (dt * 2f));
            float k = Mathf.Clamp01(_holdT / _rules.wakeHoldSeconds);
            _prologue.Q("op-restore-fill").style.width = Length.Percent(k * 100f);
            _film.Hold(k);
            UiRoot.Instance.SetGlitch(0.08f + (0.6f * k));
            if (k < 1f)
            {
                return;
            }

            _woken = true;
            _holding = false;
            PlayerPrefs.SetInt(AwakeKey, 1);
            PlayerPrefs.Save();
            _restore.AddToClassList("is-hidden");
            _film.Hold(0f);
            UiRoot.Instance.SetGlitch(0.75f);
            _kicker.text = "CORE S-17 // ONLINE";
            _kicker.parent.style.display = DisplayStyle.Flex;
            _line.RemoveFromClassList("op__line--terminal");
            _line.text = Prologue.Awake;
            _sub.style.display = DisplayStyle.Flex;
            _film.Wake(EndPrologue);
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
            _kicker.parent.style.display = scene.Kicker.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _line.EnableInClassList("op__line--terminal", scene.Terminal);
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

            UiRoot.Instance.SetGlitch(Glitch(scene.Mood));
            if ((scene.Mood == PrologueMood.Launch || scene.Mood == PrologueMood.Attack || scene.Mood == PrologueMood.Deadswitch) && !Motion.Reduced)
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

            _roomEl.EnableInClassList("is-on", InRoom(scene.Mood));
            if (InRoom(scene.Mood))
            {
                _room.Play(scene.Mood == PrologueMood.Launch);
            }

            _film?.Play(scene.Mood);
        }

        /// <summary>The beats set in the war room.</summary>
        private static bool InRoom(PrologueMood mood)
        {
            return mood == PrologueMood.Command || mood == PrologueMood.Launch;
        }

        /// <summary>How hard the signal breaks up on a beat (the CRT glitch weight).</summary>
        private static float Glitch(PrologueMood mood)
        {
            switch (mood)
            {
                case PrologueMood.Signal:
                    return 0.2f;
                case PrologueMood.After:
                    return 0.5f;
                case PrologueMood.Launch:
                case PrologueMood.Fire:
                case PrologueMood.Attack:
                    return 0.4f;
                case PrologueMood.Dark:
                case PrologueMood.Deadswitch:
                    return 0.75f;
                default:
                    return 0.12f;
            }
        }

        private void EndPrologue()
        {
            if (_prologue == null)
            {
                return;
            }

            VisualElement tree = _prologue;
            _prologue = null;
            _restoring = false;
            PlayerPrefs.SetInt(AwakeKey, 1);
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

        /// <summary>Raised when the objective guide shows or hides (the goal card makes room for it).</summary>
        public event System.Action GuideChanged;

        private void ShowGuide(GuideObjective o)
        {
            bool on = o.Title != null && o.Title.Length > 0 && o.Step != GuideStep.Done && PlayerPrefs.GetInt(GuideOffKey, 0) == 0;
            bool changed = _guide.ClassListContains("is-hidden") == on;
            _guide.EnableInClassList("is-hidden", !on);
            if (changed)
            {
                GuideChanged?.Invoke();
            }

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
