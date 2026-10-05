using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Game.UI;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
using Motion = Deadswitch.Game.UI.Motion;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The AI's overlay on the living base (SPEC-039 C): a cyan highlight and a rising scan ring on the framed
    /// facility, breathing holo rings on free plots, data links that carry pulses from running facilities to the
    /// core, a scan burst and toast when construction finishes, and a red siren on the bunker while an attack is
    /// inbound. Bright color stays on AI technology and alarms (doc 11); effect intensity scales it, reduced
    /// motion stills it. Presentation only.
    /// </summary>
    public sealed class BaseFx : MonoBehaviour
    {
        private static readonly Color Cyan = new Color(0.27f, 0.86f, 0.9f);
        private static readonly Color Siren = new Color(1f, 0.22f, 0.16f);

        private readonly List<LineRenderer> _plots = new List<LineRenderer>();
        private readonly List<Link> _links = new List<Link>();
        private readonly List<Scan> _scans = new List<Scan>();
        private GameHost _host;
        private Transform _root;
        private Light _focusLight;
        private Light _siren;
        private readonly List<Drone> _drones = new List<Drone>();
        private readonly List<Pickup> _pickups = new List<Pickup>();
        private int _focus = -1;
        private int _lastEnergy = -1;
        private int _gained;
        private float _pickupClock;
        private float _time;
        private float _nextScan;

        public static BaseFx Instance { get; private set; }

        /// <summary>Hides the game's markers in the world (plot rings, links, scans) while the opening film plays.</summary>
        public bool Hidden { get; set; }

        /// <summary>Highlights the framed slot (-1 clears).</summary>
        public void Focus(int slot)
        {
            _focus = slot;
            _nextScan = 0f;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _host = GameHost.Instance;
            _root = new GameObject("Base FX").transform;
            _root.SetParent(transform, false);
            _focusLight = NewLight("Focus Light", LightType.Point, Cyan, 9f);
            _siren = NewLight("Siren", LightType.Spot, Siren, 30f);
            _siren.spotAngle = 55f;
            _host.Ticked += Sync;
            _host.EventRaised += OnEvent;
        }

        private void OnDestroy()
        {
            if (_host != null)
            {
                _host.Ticked -= Sync;
                _host.EventRaised -= OnEvent;
            }
        }

        private void OnEvent(SimEvent e)
        {
            BaseView view = BaseView.Instance;
            if (e.Kind != EventKind.BuildCompleted || view == null || e.A < 0 || e.A >= view.SlotCount)
            {
                return;
            }

            // build-complete moment (idea 29): three quick rings sweep the new facility, a toast names it
            for (int i = 0; i < 3; i++)
            {
                StartScan(e.A, i * 0.22f, 1.1f, 1.4f);
            }

            Toasts.Show(Icons.ForFacility((FacilityKind)e.B), "ONLINE // " + Fmt.FacilityName((FacilityKind)e.B) + " L" + e.C, Toasts.Tone.Good);
        }

        /// <summary>Rebuilds plot markers and data links after the slots changed.</summary>
        private void Sync()
        {
            BaseView view = BaseView.Instance;
            if (view == null)
            {
                return;
            }

            GameState s = _host.Sim.State;
            if (_lastEnergy >= 0 && s.Energy > _lastEnergy)
            {
                _gained += s.Energy - _lastEnergy;
            }

            _lastEnergy = s.Energy;
            SyncDrones(s.Tier);
            int n = Mathf.Min(view.SlotCount, s.Slots.Count);
            while (_plots.Count < n)
            {
                _plots.Add(Ring("Plot Marker", 2.1f, 0.06f));
            }

            while (_links.Count < n)
            {
                _links.Add(new Link { Line = Curve("Data Link", 0.035f), Packet = Curve("Packet", 0.09f), Phase = _links.Count * 0.37f });
            }

            Vector3 core = view.CoreAnchor;
            core.y = 0.6f;
            for (int i = 0; i < n; i++)
            {
                FacilitySlot slot = s.Slots[i];
                bool free = slot.IsEmpty && s.JobForSlot(i) == null;
                LineRenderer plot = _plots[i];
                plot.gameObject.SetActive(free);
                plot.transform.position = view.SlotGround(i) + new Vector3(0, 0.12f, 0);

                Link link = _links[i];
                link.Active = !slot.IsEmpty && slot.Enabled && slot.Powered;
                link.Line.gameObject.SetActive(link.Active);
                link.Packet.gameObject.SetActive(link.Active);
                if (link.Active)
                {
                    link.From = view.SlotGround(i) + new Vector3(0, 0.3f, 0);
                    link.To = core;
                    SetCurve(link.Line, link.From, link.To, 0f, 1f);
                }
            }

            for (int i = n; i < _plots.Count; i++)
            {
                _plots[i].gameObject.SetActive(false);
                _links[i].Line.gameObject.SetActive(false);
                _links[i].Packet.gameObject.SetActive(false);
                _links[i].Active = false;
            }
        }

        private void Update()
        {
            BaseView view = BaseView.Instance;
            if (_host == null || !_host.IsReady || view == null)
            {
                return;
            }

            if (_root.gameObject.activeSelf == Hidden)
            {
                _root.gameObject.SetActive(!Hidden);
            }

            float dt = Time.deltaTime;
            _time += dt;
            bool reduced = _host.Settings.ReducedMotion;
            float effects = _host.Settings.Effects;
            float night = Mathf.Clamp01(view.Lighting.emissionScale);

            // free plots breathe (idea 31)
            float breathe = reduced ? 0.6f : 0.45f + (0.3f * Mathf.Sin(_time * 2.2f));
            foreach (LineRenderer plot in _plots)
            {
                SetColor(plot, Cyan, breathe * Mathf.Max(0.35f, effects));
            }

            // data links (idea 32): faint by day, clearer at night; a pulse runs each one toward the core
            float linkAlpha = (0.08f + (0.22f * night)) * effects;
            foreach (Link link in _links)
            {
                if (!link.Active)
                {
                    continue;
                }

                SetColor(link.Line, Cyan, linkAlpha);
                float t = reduced ? 0.5f : Mathf.Repeat((_time * 0.35f) + link.Phase, 1f);
                SetCurve(link.Packet, link.From, link.To, Mathf.Max(0f, t - 0.06f), t);
                SetColor(link.Packet, Cyan, Mathf.Min(1f, linkAlpha * 3.5f));
            }

            // the framed facility: a cyan key light and a scan ring every few seconds (ideas 22, 50)
            bool focused = _focus >= 0 && _focus < view.SlotCount;
            _focusLight.enabled = focused && effects > 0f;
            if (focused)
            {
                float h = Mathf.Max(1.5f, view.SlotHeight(_focus));
                _focusLight.transform.position = view.SlotGround(_focus) + new Vector3(0, h + 2.5f, 0);
                _focusLight.intensity = (reduced ? 2.2f : 2f + (0.6f * Mathf.Sin(_time * 3f))) * effects;
                _nextScan -= dt;
                if (_nextScan <= 0f && !reduced)
                {
                    _nextScan = 2.6f;
                    StartScan(_focus, 0f, 1.3f, 1f);
                }
            }

            TickScans(dt, view, effects);
            TickDrones(dt, reduced, night, effects);
            TickPickups(dt, view);

            // raid siren (idea 43): a red beam sweeps from the bunker while an attack is inbound
            bool raid = _host.Sim.State.RaidId != 0;
            _siren.enabled = raid && effects > 0f;
            if (raid)
            {
                _siren.transform.position = view.CoreAnchor + new Vector3(0, 1.2f, 0);
                float yaw = reduced ? 30f : _time * 140f;
                _siren.transform.rotation = Quaternion.Euler(28f, yaw, 0f);
                _siren.intensity = 6f * effects;
            }
        }

        /// <summary>Patrol drones (idea 33): blinking nav lights on slow orbits, more with each tier.</summary>
        private void SyncDrones(int tier)
        {
            InterfaceConfig.AmbientRules a = InterfaceConfig.Current.ambient;
            int want = Mathf.Max(0, tier * a.dronesPerTier);
            while (_drones.Count < want)
            {
                int i = _drones.Count;
                LineRenderer body = NewLine("Patrol Drone", 0.22f);
                body.useWorldSpace = true;
                body.positionCount = 2;
                _drones.Add(new Drone
                {
                    Body = body,
                    Radius = a.droneRadius * (0.7f + (0.12f * (i % 4))),
                    Height = a.droneHeight + ((i % 3) * 1.6f),
                    Speed = (i % 2 == 0 ? 1f : -1f) * (0.05f + (0.012f * (i % 5))),
                    Angle = i * 2.3f,
                });
            }
        }

        private void TickDrones(float dt, bool reduced, float night, float effects)
        {
            Vector3 center = BaseView.Instance.CoreAnchor;
            foreach (Drone d in _drones)
            {
                d.Angle += reduced ? 0f : d.Speed * dt;
                var dir = new Vector3(Mathf.Cos(d.Angle), 0f, Mathf.Sin(d.Angle));
                Vector3 p = new Vector3(center.x, d.Height, center.z) + (dir * d.Radius);
                Vector3 tangent = Vector3.Cross(Vector3.up, dir) * Mathf.Sign(d.Speed) * 0.35f;
                d.Body.SetPosition(0, p - tangent);
                d.Body.SetPosition(1, p + tangent);
                bool blink = reduced || Mathf.Repeat(_time + d.Angle, 1.4f) < 0.12f;
                Color c = blink ? new Color(1f, 0.3f, 0.25f) : Cyan;
                SetColor(d.Body, c, (blink ? 0.9f : 0.25f + (0.4f * night)) * Mathf.Max(0.3f, effects));
            }
        }

        /// <summary>A floating "+N" over the strongest energy producer every few seconds (idea 17).</summary>
        private void TickPickups(float dt, BaseView view)
        {
            _pickupClock += dt;
            if (_pickupClock >= InterfaceConfig.Current.resources.pickupSeconds && _gained > 0)
            {
                _pickupClock = 0f;
                int best = -1;
                int bestOut = 0;
                GameState s = _host.Sim.State;
                for (int i = 0; i < s.Slots.Count && i < view.SlotCount; i++)
                {
                    FacilitySlot slot = s.Slots[i];
                    if (Deadswitch.Sim.Systems.Economy.IsSource(slot.Kind) && Deadswitch.Sim.Systems.Economy.IsRunning(slot))
                    {
                        int o = Deadswitch.Sim.Systems.Economy.EffectiveOutput(s, _host.Config, slot);
                        if (o > bestOut)
                        {
                            bestOut = o;
                            best = i;
                        }
                    }
                }

                if (best >= 0 && UiRoot.Instance != null)
                {
                    var label = new VisualElement { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("pickup");
                    label.Add(Icons.Create("bolt", "pickup__icon"));
                    label.Add(Kit.Label("+" + Fmt.Compact(_gained), "pickup__label"));
                    UiRoot.Instance.World.Add(label);
                    _pickups.Add(new Pickup { Label = label, Slot = best });
                }

                _gained = 0;
            }

            Camera cam = DroneCamera.Instance != null ? DroneCamera.Instance.Camera : null;
            VisualElement root = UiRoot.Instance != null ? UiRoot.Instance.Root : null;
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                Pickup p = _pickups[i];
                p.Time += dt;
                if (p.Time > 1.6f || cam == null || root == null || p.Slot >= view.SlotCount)
                {
                    p.Label.RemoveFromHierarchy();
                    _pickups.RemoveAt(i);
                    continue;
                }

                Vector3 world = view.SlotGround(p.Slot) + new Vector3(0, view.SlotHeight(p.Slot) + 1.2f + (p.Time * 0.9f), 0);
                Vector3 sp = cam.WorldToScreenPoint(world);
                p.Label.style.left = sp.x / Screen.width * root.layout.width;
                p.Label.style.top = (Screen.height - sp.y) / Screen.height * root.layout.height;
                p.Label.style.opacity = p.Time < 0.2f ? p.Time / 0.2f : 1f - Mathf.Clamp01((p.Time - 1f) / 0.6f);
            }
        }

        private void StartScan(int slot, float delay, float seconds, float strength)
        {
            LineRenderer ring = Ring("Scan", 2.6f, 0.08f);
            _scans.Add(new Scan { Ring = ring, Slot = slot, Delay = delay, Seconds = seconds, Strength = strength });
        }

        private void TickScans(float dt, BaseView view, float effects)
        {
            for (int i = _scans.Count - 1; i >= 0; i--)
            {
                Scan sc = _scans[i];
                if (sc.Delay > 0f)
                {
                    sc.Delay -= dt;
                    sc.Ring.gameObject.SetActive(false);
                    continue;
                }

                sc.Time += dt;
                float t = sc.Time / sc.Seconds;
                if (t >= 1f || sc.Slot >= view.SlotCount)
                {
                    Destroy(sc.Ring.gameObject);
                    _scans.RemoveAt(i);
                    continue;
                }

                sc.Ring.gameObject.SetActive(true);
                float h = Mathf.Max(1.5f, view.SlotHeight(sc.Slot)) + 0.6f;
                sc.Ring.transform.position = view.SlotGround(sc.Slot) + new Vector3(0, Ease.OutCubic(t) * h, 0);
                SetColor(sc.Ring, Cyan, Mathf.Sin(t * Mathf.PI) * 0.9f * sc.Strength * Mathf.Max(0.3f, effects));
            }
        }

        private Light NewLight(string name, LightType type, Color color, float range)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(_root, false);
            l.type = type;
            l.color = color;
            l.range = range;
            l.shadows = LightShadows.None;
            l.enabled = false;
            return l;
        }

        private LineRenderer Ring(string name, float radius, float width)
        {
            LineRenderer line = NewLine(name, width);
            const int segments = 48;
            line.loop = true;
            line.useWorldSpace = false;
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.PI * 2f * i / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }

            return line;
        }

        private LineRenderer Curve(string name, float width)
        {
            LineRenderer line = NewLine(name, width);
            line.useWorldSpace = true;
            line.positionCount = 12;
            return line;
        }

        private LineRenderer NewLine(string name, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = BattleFx.Additive;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
            return line;
        }

        /// <summary>A section [t0, t1] of the arc from a facility up and over to the core.</summary>
        private static void SetCurve(LineRenderer line, Vector3 from, Vector3 to, float t0, float t1)
        {
            int n = line.positionCount;
            float lift = Mathf.Min(6f, Vector3.Distance(from, to) * 0.25f);
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.Lerp(t0, t1, i / (float)(n - 1));
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y += Mathf.Sin(t * Mathf.PI) * lift;
                line.SetPosition(i, p);
            }
        }

        private static void SetColor(LineRenderer line, Color c, float alpha)
        {
            var col = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
            line.startColor = col;
            line.endColor = col;
        }

        private sealed class Link
        {
            public LineRenderer Line;
            public LineRenderer Packet;
            public Vector3 From;
            public Vector3 To;
            public float Phase;
            public bool Active;
        }

        private sealed class Drone
        {
            public LineRenderer Body;
            public float Radius;
            public float Height;
            public float Speed;
            public float Angle;
        }

        private sealed class Pickup
        {
            public VisualElement Label;
            public int Slot;
            public float Time;
        }

        private sealed class Scan
        {
            public LineRenderer Ring;
            public int Slot;
            public float Delay;
            public float Seconds;
            public float Strength;
            public float Time;
        }
    }
}
