using System.Collections.Generic;
using Deadswitch.Art.World;
using Deadswitch.Game.Audio;
using Deadswitch.Game.UI;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim.State;
using UnityEngine;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The opening film's director (SPEC-044). Each beat plays one or more directed shots from <c>Interface.json</c>
    /// <c>opening.shots</c>. On the Hub it stages a fully built compound (<see cref="BaseView.Stage"/>), breaks it cut by
    /// cut while fire comes in from the sky, leaves the ruin with no lamps (only fire), then flickers the core when the
    /// deadswitch fires. Presentation only; created by <c>OpeningFlow</c> when cinematics are allowed and destroyed when
    /// the film ends, handing the camera, the lights and the view back.
    /// </summary>
    public sealed class OpeningFilm : MonoBehaviour
    {
        private static readonly Color Cyan = new Color(0.27f, 0.86f, 0.9f);
        private static readonly Color WarRed = new Color(1f, 0.32f, 0.2f);

        private readonly List<LineRenderer> _rings = new List<LineRenderer>();
        private readonly List<Tracer> _tracers = new List<Tracer>();
        private readonly List<InterfaceConfig.OpeningShot> _cuts = new List<InterfaceConfig.OpeningShot>();
        private InterfaceConfig.OpeningRules _rules;
        private BaseView.Staging _stage;
        private PrologueMood _mood;
        private int _cut;
        private float _t;
        private float _cutT;
        private float _nextImpact;
        private float _flash;
        private float _blastScale = 1f;
        private Light _flare;
        private Light _blast;
        private ParticleSystem _fire;
        private ParticleSystem _smoke;
        private Vector3 _core;
        private OpeningGlobe _globe;
        private readonly System.Random _rng = new System.Random(19);
        private readonly List<float> _launchAt = new List<float>();
        private Vector3 _mapFrom;
        private Vector3 _mapTo;
        private Vector3 _mapLook;
        private Vector3 _outpost;
        private int _cameraMask = -1;
        private float _cameraFar;
        private Phase _phase = Phase.Film;
        private int _focusSlot = -1;
        private bool _focusCore;
        private float _focusT;
        private Vector3 _camPos;
        private Vector3 _camLook;
        private float _camFov = 40f;
        private float _hold;
        private float _wakeT;
        private System.Action _wakeDone;
        private int _restored;
        private RestoreJob _job;

        private enum Phase
        {
            Film,
            Restore,
            Wake,
        }

        public static OpeningFilm Create()
        {
            return new GameObject("Opening Film").AddComponent<OpeningFilm>();
        }

        /// <summary>True when a beat is shot on the Hub.</summary>
        public static bool OnHub(PrologueMood mood)
        {
            return mood >= PrologueMood.Hold;
        }

        /// <summary>True when a beat is shot on the planet.</summary>
        public static bool OnGlobe(PrologueMood mood)
        {
            return mood == PrologueMood.Signal || mood == PrologueMood.Fire || mood == PrologueMood.Dark;
        }

        /// <summary>True when the film draws the picture for a beat (the others are the overlay's own).</summary>
        public static bool ShowsWorld(PrologueMood mood)
        {
            return OnHub(mood) || OnGlobe(mood) || mood == PrologueMood.Outposts;
        }

        /// <summary>Raised when a hit should white out the frame (argument: strength 0..1).</summary>
        public event System.Action<float> Whiteout;

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

            _stage = FullHub();
        }

        /// <summary>How long a beat runs (the sum of its cuts; the beat moves on by itself after this).</summary>
        public float Seconds(PrologueMood mood)
        {
            float total = 0f;
            foreach (InterfaceConfig.OpeningShot s in Shots(mood))
            {
                total += s.seconds;
            }

            return total > 0f ? total : 4f;
        }

        /// <summary>Starts a beat with a hard cut to its first shot.</summary>
        public void Play(PrologueMood mood)
        {
            _mood = mood;
            _cuts.Clear();
            _cuts.AddRange(Shots(mood));
            _t = 0f;
            _cut = -1;
            _nextImpact = 0.2f;
            AudioDirector audio = AudioDirector.Instance;
            switch (mood)
            {
                case PrologueMood.Signal:
                    audio?.Hush(600f);
                    audio?.Opening(OpeningCue.Heartbeat);
                    break;
                case PrologueMood.Hold:
                    // the compound as it was: every plot built, people in the street, lamps lit
                    _stage = FullHub();
                    BaseView.Instance?.Stage(_stage);
                    break;
                case PrologueMood.Ash:
                    audio?.Opening(OpeningCue.Static);
                    break;
                case PrologueMood.Deadswitch:
                    audio?.Opening(OpeningCue.Heartbeat);
                    break;
            }

            if (OnGlobe(mood) && _globe == null)
            {
                _globe = OpeningGlobe.Create(transform);
                _globe.Landed += OnLanded;
            }
            else if (!OnGlobe(mood) && _globe != null && mood != PrologueMood.Command && mood != PrologueMood.Launch)
            {
                Destroy(_globe.gameObject);
                _globe = null;
            }

            MapShot(mood == PrologueMood.Outposts);
            _launchAt.Clear();
            if (mood == PrologueMood.Fire)
            {
                // a first wave, the big one, then everyone answers
                float[] at = { 0.2f, 0.45f, 0.7f, 0.9f, 1.4f, 2.0f, 2.25f, 2.5f, 2.7f, 2.9f, 3.1f, 3.3f };
                _launchAt.AddRange(at);
            }

            NextCut();
        }

        private void OnLanded(float size)
        {
            AudioDirector.Instance?.Opening(size >= 2f ? OpeningCue.Impact : OpeningCue.FarImpact);
            DroneCamera.Instance?.Shake(_rules.warShake * (size >= 2f ? 1.2f : 0.35f));
            if (size >= 2f)
            {
                Whiteout?.Invoke(1f);
            }
        }

        /// <summary>
        /// The restore steps (SPEC-044 s8): the film is over, the Hub is a ruin lit by fire, and the handler brings the
        /// buildings back one by one. A resumed run starts here with the ruin rebuilt from the attack's cuts.
        /// </summary>
        public void BeginRestore()
        {
            _phase = Phase.Restore;
            _mood = PrologueMood.You;
            _cuts.Clear();
            MapShot(false);
            if (_globe != null)
            {
                Destroy(_globe.gameObject);
                _globe = null;
            }

            if (!_stage.Burning)
            {
                // a resumed run: rebuild the ruin the attack left
                foreach (InterfaceConfig.OpeningShot s in Shots(PrologueMood.Attack))
                {
                    _cuts.Add(s);
                }

                for (_cut = 0; _cut < _cuts.Count; _cut++)
                {
                    Break(_cuts[_cut]);
                }

                _cuts.Clear();
            }

            _stage.People = 0;
            BaseView.Instance?.Stage(_stage);
            Transform cam = DroneCamera.Instance != null ? DroneCamera.Instance.Camera.transform : null;
            _camPos = cam != null ? cam.position : new Vector3(0f, 20f, -24f);
            _camLook = cam != null ? cam.position + (cam.forward * 20f) : _core;
            _camFov = DroneCamera.Instance != null ? DroneCamera.Instance.Camera.fieldOfView : 40f;
        }

        /// <summary>Frames a building for its restore card.</summary>
        public void FocusSlot(int slot)
        {
            _focusSlot = slot;
            _focusCore = false;
            _focusT = 0f;
        }

        /// <summary>Frames the core for the last step.</summary>
        public void FocusCore()
        {
            _focusSlot = -1;
            _focusCore = true;
            _focusT = 0f;
        }

        /// <summary>Brings a ruined building back (sparks, the damage falls away, its lamps come on), then calls done.</summary>
        public void Restore(int slot, System.Action done)
        {
            _job = new RestoreJob { Slot = slot, Done = done };
            AudioDirector.Instance?.Opening(OpeningCue.Restore);
        }

        /// <summary>How far the handler has held WAKE THE CORE (0..1): the core answers, red.</summary>
        public void Hold(float amount)
        {
            _hold = Mathf.Clamp01(amount);
        }

        /// <summary>The core wakes: a breath, then the shockwave relights the Hub; calls done when it has passed.</summary>
        public void Wake(System.Action done)
        {
            _phase = Phase.Wake;
            _wakeT = 0f;
            _wakeDone = done;
            AudioDirector.Instance?.Opening(OpeningCue.SubDrop);
            DroneCamera.Instance?.Shake(_rules.warShake * 0.6f);
        }

        /// <summary>Seconds from the wake to the hand-over (the breath plus the wave).</summary>
        public float WakeSeconds => _rules.wakeBreath + _rules.waveSeconds + 0.6f;

        /// <summary>
        /// The sector map at night (the map world the MAP screen renders, far below the Hub): the drone camera may
        /// see its layer, reaches further and the air thins for the beat; the shot pushes in on one outpost, burning.
        /// </summary>
        private void MapShot(bool on)
        {
            DroneCamera drone = DroneCamera.Instance;
            if (drone == null)
            {
                return;
            }

            if (on)
            {
                MapView.Ensure();
                if (_cameraMask == -1)
                {
                    _cameraMask = drone.Camera.cullingMask;
                    _cameraFar = drone.Camera.farClipPlane;
                }

                drone.Camera.cullingMask = _cameraMask | (1 << MapView.Layer);
                drone.Camera.farClipPlane = _rules.mapFar;
                Art.World.CameraPose pose = Art.World.SectorScene.Camera(drone.Camera.aspect);
                Vector3 origin = MapView.Origin;
                _outpost = origin + ArtBridge.V(Art.World.SectorScene.SitePosition(_rules.mapSite, BaseView.Seed));
                Vector3 back = ArtBridge.V(pose.Position - pose.Target);
                _mapFrom = origin + ArtBridge.V(pose.Position);
                _mapLook = origin + ArtBridge.V(pose.Target);
                _mapTo = _outpost + (back * _rules.mapPush);
                BaseView.Instance?.OpeningFog(_rules.mapFog);
            }
            else if (_cameraMask != -1)
            {
                drone.Camera.cullingMask = _cameraMask;
                drone.Camera.farClipPlane = _cameraFar;
                _cameraMask = -1;
                BaseView.Instance?.OpeningFog(1f);
            }
        }

        /// <summary>Hands the camera, the lights and the view back and removes the film's objects.</summary>
        public void Finish()
        {
            MapShot(false);
            DroneCamera.Instance?.Release();
            if (BaseView.Instance != null)
            {
                BaseView.Instance.ClearOpeningPower();
                BaseView.Instance.Stage(null);
            }

            if (BaseFx.Instance != null)
            {
                BaseFx.Instance.Hidden = false;
            }

            AudioDirector.Instance?.Hush(0f);
            Destroy(gameObject);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_phase != Phase.Film)
            {
                TickRestore(dt);
                Tracers(dt);
                return;
            }

            _t += dt;
            _cutT += dt;
            if (_cut >= 0 && _cut < _cuts.Count - 1 && _cutT >= _cuts[_cut].seconds)
            {
                NextCut();
            }

            Frame();
            Lights(dt);
            Planet();
            if (_mood == PrologueMood.Attack)
            {
                Incoming(dt);
            }

            if (_mood == PrologueMood.Outposts)
            {
                Burning(dt);
            }

            Tracers(dt);
        }

        private void NextCut()
        {
            _cut++;
            _cutT = 0f;
            if (_cut >= _cuts.Count)
            {
                return;
            }

            Break(_cuts[_cut]);
            Frame();
            DroneCamera.Instance?.Cut();
        }

        /// <summary>What a cut shows breaking on the staged Hub.</summary>
        private void Break(InterfaceConfig.OpeningShot s)
        {
            bool broke = false;
            foreach (int slot in s.raze)
            {
                if (slot >= 0 && slot < _stage.Razed.Length)
                {
                    _stage.Razed[slot] = true;
                    _stage.Wreckage = Mathf.Min(_rules.ruinWreckage, _stage.Wreckage + 1);
                    broke = true;
                }
            }

            for (int i = 0; i + 1 < s.hurt.Length; i += 2)
            {
                int slot = s.hurt[i];
                if (slot >= 0 && slot < _stage.Slots.Length)
                {
                    SlotView v = _stage.Slots[slot];
                    int damage = s.hurt[i + 1];
                    // a building hit this hard loses its upper floors: what is left matches the run's start
                    _stage.Slots[slot] = new SlotView(v.Kind, damage >= 3 ? 1 : v.Level, damage < 3, false, FacilityKind.None, 0, damage);
                    broke = true;
                }
            }

            if (broke)
            {
                _stage.Burning = true;
                _stage.People = Mathf.Max(0, _stage.People - 14);
                BaseView.Instance?.Stage(_stage);
                if (_phase == Phase.Film)
                {
                    Impact(_core + new Vector3(Random.Range(-6f, 6f), 0.6f, Random.Range(-14f, -4f)), false);
                }
            }
        }

        private void Frame()
        {
            if (_cut < 0 || _cut >= _cuts.Count || DroneCamera.Instance == null || !ShowsWorld(_mood))
            {
                return;
            }

            if (_mood == PrologueMood.Outposts)
            {
                float m = Ease.InOutSine(Mathf.Clamp01(_cutT / Mathf.Max(0.01f, _cuts[_cut].seconds)));
                DroneCamera.Instance.Direct(Vector3.Lerp(_mapFrom, _mapTo, m), Vector3.Lerp(_mapLook, _outpost, m), Mathf.Lerp(28f, 34f, m));
                return;
            }

            // planet shots are given relative to the planet centre
            Vector3 origin = OnGlobe(_mood) ? OpeningGlobe.Center : Vector3.zero;
            InterfaceConfig.OpeningShot s = _cuts[_cut];
            float k = Ease.InOutSine(Mathf.Clamp01(_cutT / Mathf.Max(0.01f, s.seconds)));
            DroneCamera.Instance.Direct(origin + Vector3.Lerp(s.from, s.to, k), origin + Vector3.Lerp(s.lookFrom, s.lookTo, k), Mathf.Lerp(s.fovFrom, s.fovTo, k));
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
            Color flareColor = Cyan;
            view.OpeningGrade(WarRed, 0f);
            switch (_mood)
            {
                case PrologueMood.Hold:
                    view.OpeningPower(full, 1f);
                    break;
                case PrologueMood.Attack:
                    // the grid browns out with every hit and the sky burns
                    view.OpeningPower(full * (1f - (0.18f * _cut)), Mathf.Lerp(1f, 0.3f, _flash));
                    view.OpeningGrade(WarRed, 0.25f + (0.35f * _flash));
                    break;
                case PrologueMood.Deadswitch:
                    // the core stutters red: something in it is still awake
                    view.OpeningPower(0f, 0f);
                    flareColor = WarRed;
                    flare = Mathf.PerlinNoise(_t * 9f, 0.4f) > 0.55f ? Mathf.Clamp01(_t / 2f) * 0.5f : 0.02f;
                    break;
                case PrologueMood.You:
                    view.OpeningPower(0f, 0f);
                    flare = 0.12f + (0.06f * Mathf.Sin(_t * 2.4f));
                    break;
                default:
                    // the ruin and every beat before the Hub: no lamps at all
                    view.OpeningPower(0f, 0f);
                    break;
            }

            if (!OnHub(_mood) && _mood != PrologueMood.Outposts)
            {
                // off the Hub (and off the map) the sky is space: black fog, black background
                view.OpeningGrade(Color.black, 1f);
            }

            _flare.color = flareColor;
            _flare.enabled = flare > 0.01f;
            _flare.intensity = flare * _rules.flareIntensity;
            DrawRings(-1f);
            _flash = Mathf.Max(0f, _flash - (dt * 2.5f));
            if (_mood != PrologueMood.Outposts)
            {
                _blast.enabled = _flash > 0.01f;
                _blast.intensity = _flash * _blastScale * _rules.blastIntensity;
            }
        }

        private void TickRestore(float dt)
        {
            _t += dt;
            _focusT += dt;
            BaseView view = BaseView.Instance;
            if (view == null)
            {
                return;
            }

            // the camera glides from building to building, then rises off the core as the wave goes out
            Vector3 pos = _camPos;
            Vector3 look = _camLook;
            float fov = _camFov;
            if (_phase == Phase.Wake)
            {
                float u = Ease.InOutSine(Mathf.Clamp01((_wakeT - _rules.wakeBreath) / (_rules.waveSeconds + 0.6f)));
                pos = Vector3.Lerp(_core + new Vector3(0f, 8f, -14f), _rules.wakeCamera, u);
                look = Vector3.Lerp(_core, new Vector3(0f, 0f, 4f), u);
                fov = Mathf.Lerp(40f, 34f, u);
            }
            else if (_focusSlot >= 0 && _focusSlot < view.SlotCount)
            {
                // from the central walkway at head height, a little toward the gate, so the whole building
                // stands in frame; a slow drift along the walk keeps it alive
                Vector3 ground = view.SlotGround(_focusSlot);
                float side = ground.x < 0f ? -1f : 1f;
                float drift = Mathf.Sin(_focusT * 0.25f) * 0.8f;
                pos = new Vector3(side * _rules.restoreWalkX, _rules.restoreEyeHeight, ground.z - _rules.restoreBack + drift);
                // aim under the building so it stands above the card
                look = view.FocusPoint(_focusSlot) + new Vector3(0f, -_rules.restoreAimDrop, 0f);
                fov = _rules.restoreFov;
            }
            else if (_focusCore)
            {
                pos = _core + new Vector3(Mathf.Sin(_focusT * 0.1f) * 2f, 4f, -15f);
                look = _core;
                fov = 42f;
            }

            float k = 1f - Mathf.Exp(-dt * (_phase == Phase.Wake ? 20f : 2.2f));
            _camPos = Vector3.Lerp(_camPos, pos, k);
            _camLook = Vector3.Lerp(_camLook, look, k);
            _camFov = Mathf.Lerp(_camFov, fov, k);
            DroneCamera.Instance?.Direct(_camPos, _camLook, _camFov);

            if (_job != null)
            {
                TickJob(dt, view);
            }

            float flare = 0f;
            Color flareColor = Cyan;
            float lamps = 0f;
            float level = 0.12f * _restored;
            if (_phase == Phase.Restore)
            {
                flare = _focusCore ? 0.1f + (0.05f * Mathf.Sin(_t * 2f)) : 0.04f;
                if (_hold > 0f)
                {
                    // it answers the hand on the switch: red, and harder the longer it is held
                    flareColor = WarRed;
                    flare = _hold * (0.6f + (0.4f * Mathf.PerlinNoise(_t * 14f, 0.2f)));
                }
            }
            else
            {
                _wakeT += dt;
                if (_wakeT < _rules.wakeBreath)
                {
                    // the breath: dark, the red dies away
                    flareColor = WarRed;
                    flare = 0.6f * (1f - (_wakeT / _rules.wakeBreath));
                }
                else
                {
                    float w = Mathf.Clamp01((_wakeT - _rules.wakeBreath) / _rules.waveSeconds);
                    lamps = Ease.OutCubic(w) * _rules.waveRadius;
                    level = Mathf.Max(level, Ease.OutCubic(w));
                    flare = Mathf.Lerp(1f, 0.2f, w);
                    DrawRings(w < 1f ? lamps : -1f);
                    if (_wakeT - dt < _rules.wakeBreath)
                    {
                        AudioDirector.Instance?.Opening(OpeningCue.PowerUp);
                        DroneCamera.Instance?.Shake(_rules.warShake * 0.5f);
                        Feedback.Alert();
                    }

                    if (_wakeT >= WakeSeconds && _wakeDone != null)
                    {
                        System.Action done = _wakeDone;
                        _wakeDone = null;
                        done();
                        return;
                    }
                }
            }

            view.OpeningGrade(WarRed, 0f);
            view.OpeningPower(lamps, level);
            view.OpeningFire(_phase == Phase.Wake ? 1f : _rules.restoreFire);
            _flare.color = flareColor;
            _flare.enabled = flare > 0.01f;
            _flare.intensity = flare * _rules.flareIntensity;
        }

        private void TickJob(float dt, BaseView view)
        {
            RestoreJob job = _job;
            job.Age += dt;
            job.NextSpark -= dt;
            if (job.NextSpark <= 0f)
            {
                // welding sparks, small and quick, here and there on the frame
                job.NextSpark = 0.04f;
                Vector3 at = view.FocusPoint(job.Slot) + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1f, 1f), Random.Range(-1.2f, 0.4f));
                BattleFx.Burst(_fire, at, 2, 0.18f);
            }

            // the damage falls away in steps, each with its own jolt
            SlotView v = _stage.Slots[job.Slot];
            int damage = Mathf.Max(0, 3 - Mathf.FloorToInt(job.Age / (_rules.restoreSeconds / 3f)));
            if (damage < v.Damage)
            {
                _stage.Slots[job.Slot] = new SlotView(v.Kind, 1, damage == 0, false, FacilityKind.None, 0, damage);
                if (damage == 0)
                {
                    view.OpeningLit(job.Slot);
                }

                view.Stage(_stage);
                DroneCamera.Instance?.Shake(0.12f);
            }

            if (job.Age >= _rules.restoreSeconds + 0.3f)
            {
                _job = null;
                _restored++;
                job.Done?.Invoke();
            }
        }

        private void Planet()
        {
            if (_globe == null)
            {
                return;
            }

            float u = Mathf.Clamp01(_t / Seconds(_mood));
            switch (_mood)
            {
                case PrologueMood.Signal:
                    _globe.Lights = Mathf.Clamp01(_t / 2.5f);
                    break;
                case PrologueMood.Fire:
                    _globe.Burn = Mathf.Clamp01(u * 1.3f);
                    while (_launchAt.Count > 0 && _t >= _launchAt[0])
                    {
                        _launchAt.RemoveAt(0);
                        Vector3 cam = DroneCamera.Instance != null ? DroneCamera.Instance.Camera.transform.position : OpeningGlobe.Center;
                        bool big = _launchAt.Count == 7;
                        _globe.Fire(_globe.FacingPoint(cam, _rng), _globe.FacingPoint(cam, _rng), big ? 1.4f : 1.0f, big ? 2f : 1f);
                    }

                    break;
                case PrologueMood.Dark:
                    _globe.Burn = 1f;
                    _globe.Dark = Mathf.Clamp01(u * 1.25f);
                    _globe.Embers = Mathf.Clamp01(u * 1.4f);
                    break;
            }
        }

        /// <summary>The outpost on the map burns: flames, smoke and a flickering light.</summary>
        private void Burning(float dt)
        {
            _nextImpact -= dt;
            if (_nextImpact <= 0f)
            {
                _nextImpact = 0.06f;
                BattleFx.Burst(_fire, _outpost + new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-2f, 2f)), 6, 1.6f);
                BattleFx.Burst(_smoke, _outpost + new Vector3(0f, 2f, 0f), 2, 2.4f);
            }

            _blast.transform.position = _outpost + new Vector3(0f, 5f, 0f);
            _blast.enabled = true;
            _blast.intensity = _rules.blastIntensity * (0.5f + (0.3f * Mathf.PerlinNoise(_t * 7f, 0.3f)));
        }

        private void Incoming(float dt)
        {
            _nextImpact -= dt;
            if (_nextImpact > 0f || DroneCamera.Instance == null)
            {
                return;
            }

            // incoming fire lands in front of the lens: a streak out of the sky, then the hit
            _nextImpact = _rules.warImpactEvery * Random.Range(0.5f, 1.2f);
            Transform cam = DroneCamera.Instance.Camera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 p = cam.position + (forward * Random.Range(7f, 24f)) + (right * Random.Range(-7f, 7f));
            p.y = 0.6f;
            Vector3 from = p + (right * Random.Range(-30f, 30f)) + (forward * Random.Range(20f, 50f)) + new Vector3(0f, Random.Range(35f, 55f), 0f);
            _tracers.Add(new Tracer { Line = Streak(), From = from, To = p, Seconds = Random.Range(0.25f, 0.4f) });
        }

        private void Tracers(float dt)
        {
            for (int i = _tracers.Count - 1; i >= 0; i--)
            {
                Tracer tr = _tracers[i];
                tr.Age += dt;
                float t = Mathf.Clamp01(tr.Age / tr.Seconds);
                tr.Line.SetPosition(0, Vector3.Lerp(tr.From, tr.To, Mathf.Max(0f, t - 0.35f)));
                tr.Line.SetPosition(1, Vector3.Lerp(tr.From, tr.To, t));
                if (t < 1f)
                {
                    continue;
                }

                Impact(tr.To, true);
                Destroy(tr.Line.gameObject);
                _tracers.RemoveAt(i);
            }
        }

        private void Impact(Vector3 p, bool sound)
        {
            BattleFx.Burst(_fire, p, 90, 3.2f);
            BattleFx.Burst(_smoke, p, 30, 3.4f);
            float dist = DroneCamera.Instance != null ? Vector3.Distance(DroneCamera.Instance.Camera.transform.position, p) : 30f;
            _blast.transform.position = p + new Vector3(0f, 4f, 0f);
            // a hit right next to the lens must not blow the frame out to white
            _blastScale = Mathf.Clamp01(dist / 24f);
            _flash = 1f;
            bool close = dist < 22f;
            if (sound)
            {
                AudioDirector.Instance?.Opening(close ? OpeningCue.Impact : OpeningCue.FarImpact);
            }

            DroneCamera.Instance?.Shake(_rules.warShake * (close ? 1f : 0.6f));
        }

        private BaseView.Staging FullHub()
        {
            int n = Mathf.Min(_rules.hubKinds.Length, _rules.hubLevels.Length);
            var st = new BaseView.Staging { Tier = _rules.hubTier, People = _rules.hubPeople, Slots = new SlotView[n], Razed = new bool[n] };
            for (int i = 0; i < n; i++)
            {
                FacilityKind kind = System.Enum.TryParse(_rules.hubKinds[i], out FacilityKind k) ? k : FacilityKind.None;
                st.Slots[i] = new SlotView(kind, _rules.hubLevels[i], true, false, FacilityKind.None, 0);
            }

            return st;
        }

        private List<InterfaceConfig.OpeningShot> Shots(PrologueMood mood)
        {
            var list = new List<InterfaceConfig.OpeningShot>();
            string key = mood.ToString().ToLowerInvariant();
            foreach (InterfaceConfig.OpeningShot s in _rules.shots)
            {
                if (s.mood == key)
                {
                    list.Add(s);
                }
            }

            return list;
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

        private sealed class RestoreJob
        {
            public int Slot;
            public float Age;
            public float NextSpark;
            public System.Action Done;
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
