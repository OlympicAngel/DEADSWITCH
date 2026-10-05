using System.Collections.Generic;
using Deadswitch.Art.World;
using Deadswitch.Game.Core;
using Deadswitch.Game.UI;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
using Motion = Deadswitch.Game.UI.Motion;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The contact sequence (SPEC-039 D): when an attack hits the wall, letterbox bars close in and a title card
    /// names it (signature shape + word + gate + faction); the camera cuts low at the gate looking out at the
    /// attackers, dollies along their line with impacts and shake, then pulls back to the drone. When the attack
    /// resolves, an aftermath shot frames the gate in smoke with a DEFENSE HELD / BREACH stamp. A tap skips it.
    /// Off with the cinematics setting or reduced motion. Presentation only: it never delays input or decides
    /// anything; the sim already resolved what it shows.
    /// </summary>
    public sealed class AttackCinematic : MonoBehaviour
    {
        private readonly Queue<Shot> _queue = new Queue<Shot>();
        private GameHost _host;
        private VisualElement _layer;
        private VisualElement _top;
        private VisualElement _bottom;
        private VisualElement _card;
        private Label _cardKind;
        private Label _cardLine;
        private VisualElement _cardPip;
        private Label _stamp;
        private ParticleSystem _fire;
        private ParticleSystem _smoke;
        private Shot _shot;
        private float _shotTime;
        private float _nextImpact;
        private Vector3 _gate;
        private Vector3 _outward;
        private bool _playing;

        private enum ShotKind
        {
            Title,
            GateLow,
            Dolly,
            Pullback,
            Aftermath,
        }

        public static AttackCinematic Instance { get; private set; }

        /// <summary>True while a sequence holds the camera.</summary>
        public bool Playing => _playing;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _host = GameHost.Instance;
            _host.EventRaised += OnEvent;
            BuildUi();
        }

        private void OnDestroy()
        {
            if (_host != null)
            {
                _host.EventRaised -= OnEvent;
            }
        }

        private bool Allowed => _host.Settings.Cinematics && !_host.Settings.ReducedMotion;

        private void OnEvent(SimEvent e)
        {
            // events replayed by offline catch-up are history, not a moment to film
            if (!Allowed || e.Tick < _host.Sim.State.Tick - 1)
            {
                return;
            }

            GameState s = _host.Sim.State;
            if (e.Kind == EventKind.RaidContact)
            {
                RaidGate gate = (RaidGate)e.B == RaidGate.None ? RaidGate.South : (RaidGate)e.B;
                ReportScene.Gate(gate, BaseView.Seed, out System.Numerics.Vector3 g, out System.Numerics.Vector3 o);
                _gate = ArtBridge.V(g);
                _outward = ArtBridge.V(o);
                QueueContact(s, gate);
            }
            else if (e.Kind == EventKind.RaidResolved && (RaidOutcome)e.B != RaidOutcome.Tribute && (RaidOutcome)e.B != RaidOutcome.Lockdown && _gate != Vector3.zero)
            {
                RaidOutcome outcome = (RaidOutcome)e.B;
                _queue.Enqueue(new Shot
                {
                    Kind = ShotKind.Aftermath,
                    Seconds = InterfaceConfig.Current.cinematic.aftermathSeconds,
                    Text = outcome == RaidOutcome.Breached ? "BREACH" : outcome == RaidOutcome.Missed ? "THEY MISSED US" : "DEFENSE HELD",
                    Bad = outcome == RaidOutcome.Breached,
                });
                Begin();
            }
        }

        private void QueueContact(GameState s, RaidGate gate)
        {
            InterfaceConfig.CinematicRules r = InterfaceConfig.Current.cinematic;
            _queue.Clear();
            _queue.Enqueue(new Shot { Kind = ShotKind.Title, Seconds = r.titleSeconds, Text = Names.Attack(s.RaidKind), Line = "CONTACT // " + Names.Gate(gate) + " // " + Names.Faction(s.RaidFaction), Attack = s.RaidKind });
            _queue.Enqueue(new Shot { Kind = ShotKind.GateLow, Seconds = r.gateSeconds });
            _queue.Enqueue(new Shot { Kind = ShotKind.Dolly, Seconds = r.dollySeconds });
            _queue.Enqueue(new Shot { Kind = ShotKind.Pullback, Seconds = r.pullbackSeconds });
            Begin();
        }

        private void Begin()
        {
            if (_playing)
            {
                return;
            }

            _playing = true;
            _layer.style.display = DisplayStyle.Flex;
            float bars = InterfaceConfig.Current.cinematic.letterboxPct;
            Motion.To(_layer, 0.45f, Ease.OutCubic, t =>
            {
                _top.style.height = Length.Percent(bars * t);
                _bottom.style.height = Length.Percent(bars * t);
                UiRoot.Instance.Hud.style.opacity = 1f - t;
            });
            Next();
        }

        private void Next()
        {
            _card.style.display = DisplayStyle.None;
            _stamp.style.display = DisplayStyle.None;
            if (_queue.Count == 0)
            {
                End();
                return;
            }

            _shot = _queue.Dequeue();
            _shotTime = 0f;
            _nextImpact = 0.3f;
            switch (_shot.Kind)
            {
                case ShotKind.Title:
                    ShowCard(_shot);
                    break;
                case ShotKind.GateLow:
                case ShotKind.Dolly:
                case ShotKind.Aftermath:
                    Pose(0f);
                    DroneCamera.Instance.Cut();
                    if (_shot.Kind == ShotKind.Aftermath)
                    {
                        ShowStamp(_shot);
                    }

                    break;
                case ShotKind.Pullback:
                    DroneCamera.Instance.Release();
                    break;
            }
        }

        private void End()
        {
            _playing = false;
            _shot = null;
            DroneCamera.Instance.Release();
            Motion.To(_layer, 0.4f, Ease.OutCubic, t =>
            {
                float bars = InterfaceConfig.Current.cinematic.letterboxPct * (1f - t);
                _top.style.height = Length.Percent(bars);
                _bottom.style.height = Length.Percent(bars);
                UiRoot.Instance.Hud.style.opacity = t;
            }, () => _layer.style.display = DisplayStyle.None);
        }

        private void Skip()
        {
            _queue.Clear();
            End();
        }

        private void Update()
        {
            if (!_playing || _shot == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _shotTime += dt;
            float t = Mathf.Clamp01(_shotTime / Mathf.Max(0.01f, _shot.Seconds));
            if (_shot.Kind == ShotKind.GateLow || _shot.Kind == ShotKind.Dolly || _shot.Kind == ShotKind.Aftermath)
            {
                Pose(t);
                Impacts(dt);
            }

            if (_shotTime >= _shot.Seconds)
            {
                Next();
            }
        }

        /// <summary>Where the camera is at shot progress t (0..1).</summary>
        private void Pose(float t)
        {
            InterfaceConfig.CinematicRules r = InterfaceConfig.Current.cinematic;
            Vector3 side = Vector3.Cross(Vector3.up, _outward);
            Vector3 up = Vector3.up;
            Vector3 pos;
            Vector3 look;
            float fov = r.fov;
            float e = Ease.InOutSine(t);
            switch (_shot.Kind)
            {
                case ShotKind.GateLow:
                    // behind the wall, low, looking out at what is coming; a slow push toward the gate
                    pos = _gate - (_outward * Mathf.Lerp(5f, 3f, e)) + (up * 1.3f) + (side * 1.8f);
                    look = _gate + (_outward * 11f) + (up * 1.1f);
                    fov = Mathf.Lerp(r.fov, r.fov - 5f, e);
                    break;
                case ShotKind.Dolly:
                    // outside the wall, tracking sideways along the attackers' line
                    pos = _gate + (_outward * 7f) + (side * Mathf.Lerp(-9f, 9f, e)) + (up * 2.4f);
                    look = _gate + (_outward * 12f) + (side * Mathf.Lerp(-3f, 3f, e)) + (up * 0.9f);
                    break;
                default:
                    // aftermath: high over the gate looking back in at the yard and the smoke
                    pos = _gate + (_outward * 6f) + (up * Mathf.Lerp(7f, 9f, e)) + (side * 4f);
                    look = _gate - (_outward * 6f);
                    fov = r.fov + 6f;
                    break;
            }

            pos.y = Mathf.Max(pos.y, HubScene.Height(pos.x, pos.z, BaseView.Seed) + 0.6f);
            DroneCamera.Instance.Direct(pos, look, fov);
        }

        /// <summary>Shells and muzzle fire among the attackers; a small shake per hit (scaled by effect intensity).</summary>
        private void Impacts(float dt)
        {
            float effects = _host.Settings.Effects;
            if (effects <= 0f)
            {
                return;
            }

            _nextImpact -= dt;
            if (_nextImpact > 0f)
            {
                return;
            }

            bool aftermath = _shot.Kind == ShotKind.Aftermath;
            _nextImpact = aftermath ? Random.Range(0.4f, 0.8f) : Random.Range(0.35f, 0.9f);
            Vector3 side = Vector3.Cross(Vector3.up, _outward);
            Vector3 at = aftermath
                ? _gate - (_outward * Random.Range(1f, 6f)) + (side * Random.Range(-4f, 4f))
                : _gate + (_outward * Random.Range(7f, 15f)) + (side * Random.Range(-6f, 6f));
            at.y = HubScene.Height(at.x, at.z, BaseView.Seed);
            if (aftermath)
            {
                BattleFx.Burst(_smoke, at + new Vector3(0, 0.6f, 0), (int)(6 * effects) + 1, 1f);
                return;
            }

            BattleFx.Burst(_fire, at + new Vector3(0, 0.4f, 0), (int)(16 * effects) + 2, 1.2f);
            BattleFx.Burst(_smoke, at + new Vector3(0, 0.6f, 0), (int)(9 * effects) + 1, 1f);
            DroneCamera.Instance.Shake(InterfaceConfig.Current.cinematic.shake);
        }

        private void ShowCard(Shot shot)
        {
            _card.style.display = DisplayStyle.Flex;
            _cardKind.text = shot.Text;
            _cardLine.text = shot.Line;
            bool red = shot.Attack != AttackKind.Raid;
            _card.EnableInClassList("cine-card--red", red);
            _cardPip.EnableInClassList("ds-pip--red", red);
            _cardPip.EnableInClassList("ds-pip--amber", !red);
            _cardPip.EnableInClassList("ds-pip--diamond", shot.Attack != AttackKind.Siege);
            Motion.To(_card, 0.5f, Ease.OutBack, t =>
            {
                _card.style.opacity = t;
                _card.style.scale = new Scale(new Vector3(1.25f - (0.25f * t), 1.25f - (0.25f * t), 1f));
            });
        }

        private void ShowStamp(Shot shot)
        {
            _stamp.style.display = DisplayStyle.Flex;
            _stamp.text = shot.Text;
            _stamp.EnableInClassList("cine-stamp--bad", shot.Bad);
            Motion.To(_stamp, 0.35f, Ease.OutBack, t =>
            {
                _stamp.style.opacity = t;
                _stamp.style.scale = new Scale(new Vector3(1.6f - (0.6f * t), 1.6f - (0.6f * t), 1f));
            });
        }

        private void BuildUi()
        {
            VisualElement root = UiRoot.Instance.Root;
            _layer = new VisualElement();
            _layer.AddToClassList("cine");
            _layer.style.display = DisplayStyle.None;
            _top = new VisualElement();
            _top.AddToClassList("cine-bar");
            _top.AddToClassList("cine-bar--top");
            _bottom = new VisualElement();
            _bottom.AddToClassList("cine-bar");
            _bottom.AddToClassList("cine-bar--bottom");
            _bottom.Add(Kit.Label("TAP TO SKIP", "cine-skip"));
            _card = new VisualElement();
            _card.AddToClassList("cine-card");
            _cardPip = new VisualElement();
            _cardPip.AddToClassList("ds-pip");
            _cardPip.AddToClassList("cine-card__pip");
            _cardKind = Kit.Label("RAID", "cine-card__kind");
            _cardLine = Kit.Label(string.Empty, "cine-card__line");
            var row = new VisualElement();
            row.AddToClassList("row");
            row.Add(_cardPip);
            row.Add(_cardKind);
            _card.Add(row);
            _card.Add(_cardLine);
            _stamp = Kit.Label(string.Empty, "cine-stamp");
            _layer.Add(_top);
            _layer.Add(_card);
            _layer.Add(_stamp);
            _layer.Add(_bottom);
            _layer.RegisterCallback<ClickEvent>(_ => Skip());
            // above the HUD and sheets, under the CRT overlay
            root.Insert(root.childCount - 1, _layer);
            root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Cinematic"));

            var fx = new GameObject("Cinematic FX").transform;
            fx.SetParent(transform, false);
            _fire = BattleFx.BurstEmitter(fx, false);
            _smoke = BattleFx.BurstEmitter(fx, true);
        }

        private sealed class Shot
        {
            public ShotKind Kind;
            public float Seconds;
            public string Text;
            public string Line;
            public AttackKind Attack;
            public bool Bad;
        }
    }
}
