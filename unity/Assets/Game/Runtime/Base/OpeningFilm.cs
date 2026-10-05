using System.Collections.Generic;
using Deadswitch.Game.Audio;
using Deadswitch.Game.UI;
using Deadswitch.Host.Narrative;
using UnityEngine;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The opening film on the real Hub (SPEC-043 s4). One directed shot per story beat (<c>Interface.json</c>
    /// <c>opening.shots</c>): the compound lit as it was, the war (impacts, red light, shake), the blackout rolling in
    /// to the core, then the core waking: a flare that builds with the power-up sound and a shockwave that switches the
    /// lights back on as it passes. Presentation only; created by <c>OpeningFlow</c> when cinematics are allowed and
    /// destroyed when the opening ends, handing the camera and the lights back.
    /// </summary>
    public sealed class OpeningFilm : MonoBehaviour
    {
        private static readonly Color Cyan = new Color(0.27f, 0.86f, 0.9f);
        private static readonly Color WarRed = new Color(1f, 0.32f, 0.2f);

        private readonly List<LineRenderer> _rings = new List<LineRenderer>();
        private readonly List<Tracer> _tracers = new List<Tracer>();
        private InterfaceConfig.OpeningRules _rules;
        private InterfaceConfig.OpeningShot _shot;
        private PrologueMood _mood;
        private float _t;
        private bool _waved;
        private float _nextImpact;
        private float _flash;
        private Light _flare;
        private Light _blast;
        private ParticleSystem _fire;
        private ParticleSystem _smoke;
        private Vector3 _core;

        public static OpeningFilm Create()
        {
            return new GameObject("Opening Film").AddComponent<OpeningFilm>();
        }

        private void Awake()
        {
            _rules = InterfaceConfig.Current.opening;
            _core = BaseView.Instance != null ? BaseView.Instance.CoreAnchor : Vector3.zero;
            _flare = NewLight("Core Flare", Cyan, _rules.flareRange);
            _flare.transform.position = _core + new Vector3(0f, 2f, -2f);
            _blast = NewLight("War Light", WarRed, _rules.flareRange);
            _fire = BattleFx.BurstEmitter(transform, false);
            _smoke = BattleFx.BurstEmitter(transform, true);
            for (int i = 0; i < 2; i++)
            {
                _rings.Add(Ring(i == 0 ? _rules.ringWidth : _rules.ringWidth * 0.35f));
            }

            if (BaseFx.Instance != null)
            {
                BaseFx.Instance.Hidden = true;
            }
        }

        /// <summary>How long a beat's shot lasts (the beat moves on by itself after this).</summary>
        public float Seconds(PrologueMood mood)
        {
            InterfaceConfig.OpeningShot s = Find(mood);
            return s != null ? s.seconds : 4f;
        }

        /// <summary>Starts the shot for a beat (a hard cut).</summary>
        public void Play(PrologueMood mood)
        {
            _mood = mood;
            _shot = Find(mood);
            _t = 0f;
            _waved = false;
            _nextImpact = 0.3f;
            AudioDirector audio = AudioDirector.Instance;
            switch (mood)
            {
                case PrologueMood.Boot:
                    audio?.Hush(60f);
                    break;
                case PrologueMood.Dark:
                    audio?.Opening(OpeningCue.Static);
                    break;
                case PrologueMood.Now:
                    audio?.Opening(OpeningCue.PowerUp);
                    break;
            }

            Frame();
            DroneCamera.Instance?.Cut();
        }

        /// <summary>Hands the camera and the lights back and removes the film's objects.</summary>
        public void Finish()
        {
            DroneCamera.Instance?.Release();
            BaseView.Instance?.ClearOpeningPower();
            if (BaseFx.Instance != null)
            {
                BaseFx.Instance.Hidden = false;
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            Frame();
            Lights(dt);
            if (_mood == PrologueMood.Boot && _t - dt < 0.5f && _t >= 0.5f)
            {
                AudioDirector.Instance?.Opening(OpeningCue.Heartbeat);
            }

            if (_mood == PrologueMood.War)
            {
                War(dt);
            }

            Tracers(dt);
        }

        private void Frame()
        {
            if (_shot == null || DroneCamera.Instance == null)
            {
                return;
            }

            float k = Ease.InOutSine(Mathf.Clamp01(_t / Mathf.Max(0.01f, _shot.seconds)));
            DroneCamera.Instance.Direct(Vector3.Lerp(_shot.from, _shot.to, k), Vector3.Lerp(_shot.lookFrom, _shot.lookTo, k), Mathf.Lerp(_shot.fovFrom, _shot.fovTo, k));
        }

        private void Lights(float dt)
        {
            BaseView view = BaseView.Instance;
            if (view == null)
            {
                return;
            }

            float full = _rules.waveRadius;
            float flare = 0f;
            float ring = -1f;
            view.OpeningGrade(WarRed, 0f);
            switch (_mood)
            {
                case PrologueMood.Boot:
                    view.OpeningPower(0f, 0f);
                    break;
                case PrologueMood.Before:
                case PrologueMood.Handler:
                    view.OpeningPower(full, 1f);
                    break;
                case PrologueMood.War:
                    // the grid browns out with every hit and the sky burns
                    view.OpeningPower(full, Mathf.Lerp(1f, 0.35f, _flash));
                    view.OpeningGrade(WarRed, 0.3f + (0.35f * _flash));
                    break;
                case PrologueMood.Dark:
                    // the dark rolls in from the edge to the core
                    float d = Ease.InOutSine(Mathf.Clamp01((_t - 0.5f) / _rules.blackoutSeconds));
                    view.OpeningPower(full * (1f - d), 1f - d);
                    break;
                case PrologueMood.Now:
                    // the flare builds with the rising sound; on the landing a shockwave carries the power outward
                    float build = Mathf.Clamp01(_t / _rules.waveDelay);
                    float w = Mathf.Clamp01((_t - _rules.waveDelay) / _rules.waveSeconds);
                    if (!_waved && _t >= _rules.waveDelay)
                    {
                        _waved = true;
                        DroneCamera.Instance?.Shake(_rules.warShake * 0.7f);
                        Feedback.Alert();
                    }

                    float r = Ease.OutCubic(w) * full;
                    view.OpeningPower(r, _waved ? Ease.OutCubic(w) : 0f);
                    flare = _waved ? Mathf.Lerp(1f, 0.25f, w) : build * build * 0.6f;
                    ring = _waved && w < 1f ? r : -1f;
                    break;
            }

            _flare.enabled = flare > 0.01f;
            _flare.intensity = flare * _rules.flareIntensity;
            DrawRings(ring);
            _flash = Mathf.Max(0f, _flash - (dt * 2.5f));
            _blast.enabled = _flash > 0.01f;
            _blast.intensity = _flash * _rules.flareIntensity;
        }

        private void War(float dt)
        {
            _nextImpact -= dt;
            if (_nextImpact > 0f || DroneCamera.Instance == null)
            {
                return;
            }

            // incoming fire lands in front of the lens: a streak out of the sky, then the hit
            _nextImpact = _rules.warImpactEvery * Random.Range(0.6f, 1.4f);
            Transform cam = DroneCamera.Instance.Camera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 p = cam.position + (forward * Random.Range(12f, 34f)) + (right * Random.Range(-9f, 9f));
            p.y = 0.6f;
            Vector3 from = p + (right * Random.Range(-30f, 30f)) + (forward * Random.Range(20f, 50f)) + new Vector3(0f, Random.Range(35f, 55f), 0f);
            _tracers.Add(new Tracer { Line = Streak(), From = from, To = p, Seconds = Random.Range(0.3f, 0.45f) });
        }

        private void Tracers(float dt)
        {
            for (int i = _tracers.Count - 1; i >= 0; i--)
            {
                Tracer tr = _tracers[i];
                tr.Age += dt;
                float t = Mathf.Clamp01(tr.Age / tr.Seconds);
                Vector3 head = Vector3.Lerp(tr.From, tr.To, t);
                tr.Line.SetPosition(0, Vector3.Lerp(tr.From, tr.To, Mathf.Max(0f, t - 0.35f)));
                tr.Line.SetPosition(1, head);
                if (t < 1f)
                {
                    continue;
                }

                Impact(tr.To);
                Destroy(tr.Line.gameObject);
                _tracers.RemoveAt(i);
            }
        }

        private void Impact(Vector3 p)
        {
            BattleFx.Burst(_fire, p, 90, 3.2f);
            BattleFx.Burst(_smoke, p, 30, 3.4f);
            _blast.transform.position = p + new Vector3(0f, 4f, 0f);
            _flash = 1f;
            float dist = DroneCamera.Instance != null ? Vector3.Distance(DroneCamera.Instance.Camera.transform.position, p) : 30f;
            bool close = dist < 22f;
            AudioDirector.Instance?.Opening(close ? OpeningCue.Impact : OpeningCue.FarImpact);
            DroneCamera.Instance?.Shake(_rules.warShake * (close ? 1f : 0.6f));
        }

        private LineRenderer Streak()
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = BattleFx.Additive;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(1f, 0.5f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 2;
            line.startColor = new Color(WarRed.r, WarRed.g, WarRed.b, 0f);
            line.endColor = new Color(1f, 0.85f, 0.55f, 1f);
            return line;
        }

        private void DrawRings(float radius)
        {
            for (int i = 0; i < _rings.Count; i++)
            {
                LineRenderer line = _rings[i];
                float r = radius - (i * 3.5f);
                line.enabled = r > 0.5f;
                if (!line.enabled)
                {
                    continue;
                }

                line.transform.localScale = new Vector3(r, r, 1f);
                float alpha = (1f - (radius / _rules.waveRadius)) * (i == 0 ? 1f : 0.6f);
                var c = new Color(Cyan.r, Cyan.g, Cyan.b, Mathf.Clamp01(alpha));
                line.startColor = c;
                line.endColor = c;
            }
        }

        private InterfaceConfig.OpeningShot Find(PrologueMood mood)
        {
            string key = mood.ToString().ToLowerInvariant();
            foreach (InterfaceConfig.OpeningShot s in _rules.shots)
            {
                if (s.mood == key)
                {
                    return s;
                }
            }

            return null;
        }

        private Light NewLight(string name, Color color, float range)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.shadows = LightShadows.None;
            l.enabled = false;
            return l;
        }

        private LineRenderer Ring(float width)
        {
            var go = new GameObject("Shockwave");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(_core.x, 0.4f, _core.z);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = BattleFx.Additive;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.loop = true;
            line.useWorldSpace = false;
            line.alignment = LineAlignment.TransformZ;
            const int Segments = 96;
            line.positionCount = Segments;
            for (int i = 0; i < Segments; i++)
            {
                float a = Mathf.PI * 2f * i / Segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
            }

            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            line.enabled = false;
            return line;
        }

        private sealed class Tracer
        {
            public LineRenderer Line;
            public Vector3 From;
            public Vector3 To;
            public float Seconds;
            public float Age;
        }
    }
}
