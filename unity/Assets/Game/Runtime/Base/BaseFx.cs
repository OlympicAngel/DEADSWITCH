using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Game.Presentation;
using Deadswitch.Game.UI;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using UnityEngine;

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
        private int _focus = -1;
        private float _time;
        private float _nextScan;

        public static BaseFx Instance { get; private set; }

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
