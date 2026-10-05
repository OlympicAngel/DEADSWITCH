using Deadswitch.Game.Core;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.UI.Hud
{
    /// <summary>
    /// The opening (SPEC-009, SPEC-043 s4): the fragment tells its story in six beats over the world while the drone
    /// hangs in orbit (new runs only; tap advances, skippable), the drone descends onto the Hub, the HUD comes online
    /// in steps, and the objective guide points at the control to use. Reduced motion shows still cards that wait
    /// for a tap, with no camera move; essential information never waits on animation.
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

        private static string[] RevealOrder => new[] { "topbar", "frame", "world", "advisor", "tabbar" };

        public void Tick(float dt)
        {
            if (_prologue != null)
            {
                TickPrologue(dt);
            }
            else if (_descent >= 0f && Deadswitch.Game.Base.DroneCamera.Instance != null)
            {
                TickDescent(dt);
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
            _proRoot = tree.Q("prologue");
            _proText = tree.Q<Label>("pro-text");
            _proKicker = tree.Q<Label>("pro-kicker");
            _proFlash = tree.Q("pro-flash");
            _proTake = tree.Q("pro-take");
            _proTap = tree.Q("pro-tap");
            tree.Q("pro-skip").RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                EndPrologue();
            });
            _proTake.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                EndPrologue();
            });

            // tap anywhere: finish the line being typed, or move to the next beat
            _proRoot.RegisterCallback<ClickEvent>(_ => Advance());
            _proOrb = new AiOrb(tree.Q("pro-orb"));
            _proOrb.Assemble();
            _proWorld = new PrologueWorld(tree.Q("pro-world"));
            _orbit = 0f;
            _descent = -1f;
            foreach (string part in RevealOrder)
            {
                Group(part).style.opacity = 0f;
            }

            NextCard();
        }

        private VisualElement _proRoot;
        private Label _proKicker;
        private VisualElement _proFlash;
        private VisualElement _proTake;
        private VisualElement _proTap;
        private AiOrb _proOrb;
        private PrologueWorld _proWorld;
        private float _orbit;
        private float _descent = -1f;
        private float _flash;

        private static bool Cinematic => !Motion.Reduced && (GameHost.Instance == null || GameHost.Instance.Settings.Cinematics) && Deadswitch.Game.Base.DroneCamera.Instance != null;

        private void TickPrologue(float dt)
        {
            PrologueScene scene = Prologue.Scenes[_card];
            bool typing = _shown < scene.Text.Length;
            _proOrb.Voice = typing ? 1f : 0.2f;
            _proOrb.Tick(dt);
            _proWorld.Tick(dt);
            if (_flash > 0f)
            {
                _flash = Mathf.Max(0f, _flash - (dt * 1.6f));
                _proFlash.style.opacity = _flash;
            }

            // the drone hangs in orbit high over the Hub while the story plays, turning slowly
            if (Cinematic)
            {
                _orbit += dt;
                float az = _orbit * 0.05f;
                Deadswitch.Game.Base.DroneCamera.Instance.Direct(new Vector3(Mathf.Sin(az) * 60f, 240f, -Mathf.Cos(az) * 60f), Vector3.zero, 50f);
            }

            if (typing)
            {
                _shown = Motion.Reduced ? scene.Text.Length : _shown + (dt * CharsPerSecond);
                int n = Mathf.Min(scene.Text.Length, (int)_shown);
                _proText.text = scene.Text.Substring(0, n) + (n < scene.Text.Length ? "_" : string.Empty);
                return;
            }

            // the last beat waits for the handler's answer; the others move on by themselves (unless motion is reduced)
            bool last = _card == Prologue.Scenes.Length - 1;
            _proTake.EnableInClassList("is-hidden", !last);
            _proTap.EnableInClassList("is-hidden", last);
            if (last || Motion.Reduced)
            {
                return;
            }

            _hold += dt;
            if (_hold >= HoldSeconds + (scene.Text.Length * 0.02f))
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

            PrologueScene scene = Prologue.Scenes[_card];
            if (_shown < scene.Text.Length)
            {
                _shown = scene.Text.Length;
                _proText.text = scene.Text;
                return;
            }

            if (_card < Prologue.Scenes.Length - 1)
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

            PrologueScene scene = Prologue.Scenes[_card];
            _shown = 0f;
            _hold = 0f;
            _proText.text = string.Empty;
            _proKicker.text = scene.Kicker;
            _proTake.AddToClassList("is-hidden");
            _proTap.RemoveFromClassList("is-hidden");
            foreach (PrologueMood m in (PrologueMood[])System.Enum.GetValues(typeof(PrologueMood)))
            {
                _proRoot.EnableInClassList("pro--" + m.ToString().ToLowerInvariant(), m == scene.Mood);
            }

            _proWorld.SetMood(scene.Mood);
            _proOrb.Stutter = scene.Mood == PrologueMood.Dark ? 0.7f : scene.Mood == PrologueMood.War ? 0.3f : 0f;
            UiRoot.Instance.SetGlitch(scene.Mood == PrologueMood.Dark ? 0.85f : scene.Mood == PrologueMood.War ? 0.5f : scene.Mood == PrologueMood.Boot ? 0.6f : 0.15f);
            if (scene.Mood == PrologueMood.War && !Motion.Reduced)
            {
                _flash = 0.9f;
                Feedback.Alert();
                if (Cinematic)
                {
                    Deadswitch.Game.Base.DroneCamera.Instance.Shake(0.5f);
                }
            }

            _proOrb.Ping();
            Motion.To(_proKicker, 0.4f, Ease.OutCubic, t =>
            {
                _proKicker.style.opacity = t;
                _proKicker.style.translate = new Translate(0, 16f * (1f - t));
            });
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

            VisualElement tree = _prologue;
            _prologue = null;
            UiRoot.Instance.SetGlitch(0f);
            _voice.Paused = false;
            if (Motion.Reduced)
            {
                tree.RemoveFromHierarchy();
            }
            else
            {
                Motion.To(tree, 0.5f, Ease.OutCubic, t => tree.style.opacity = 1f - t, tree.RemoveFromHierarchy);
            }

            // the drone falls out of orbit onto the Hub, then hands the camera back
            _descent = Cinematic ? 0f : -1f;
            if (_descent < 0f && Deadswitch.Game.Base.DroneCamera.Instance != null)
            {
                Deadswitch.Game.Base.DroneCamera.Instance.Release();
            }

            // Boot sequence (rule 5): each system comes online in turn, after the drone has landed.
            string[] parts = RevealOrder;
            float start = _descent >= 0f ? DescentSeconds * 0.6f : 0f;
            for (int i = 0; i < parts.Length; i++)
            {
                VisualElement el = Group(parts[i]);
                if (Motion.Reduced)
                {
                    el.style.opacity = 1f;
                    continue;
                }

                float delay = start + (i * RevealStagger);
                Motion.To(el, 0.35f + delay, Ease.OutCubic, t => el.style.opacity = Mathf.Clamp01(((t * (0.35f + delay)) - delay) / 0.35f));
            }
        }

        private const float DescentSeconds = 2.8f;

        private void TickDescent(float dt)
        {
            _descent += dt;
            float t = Mathf.Clamp01(_descent / DescentSeconds);
            float k = Ease.InOutSine(t);
            float az = _orbit * 0.05f;
            Vector3 from = new Vector3(Mathf.Sin(az) * 60f, 240f, -Mathf.Cos(az) * 60f);
            Vector3 to = new Vector3(0f, 62f, -58f);
            Deadswitch.Game.Base.DroneCamera.Instance.Direct(Vector3.Lerp(from, to, k), Vector3.zero, Mathf.Lerp(50f, 42f, k));
            if (t >= 1f)
            {
                _descent = -1f;
                Deadswitch.Game.Base.DroneCamera.Instance.Shake(0.15f);
                Deadswitch.Game.Base.DroneCamera.Instance.Release();
            }
        }

        private VisualElement Group(string name)
        {
            // "world" is the floating facility labels: kept dark behind the prologue with the rest of the HUD
            return name == "world" ? UiRoot.Instance.World : _hud.Q(name);
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
