using System.Collections.Generic;
using Deadswitch.Art.Models;
using Deadswitch.Art.World;
using Deadswitch.Game.Core;
using Deadswitch.Sim;
using Deadswitch.Sim.State;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The living 3D compound (SPEC-003). Builds the static world once from Deadswitch.Art, keeps one facility
    /// object per slot in sync with the sim (rebuild on kind/level/construction change; lamps, lights, moving
    /// parts and beacons follow power and crew) and animates people walking the courtyard. Presentation only.
    /// </summary>
    public sealed class BaseView : MonoBehaviour
    {
        /// <summary>Art seed for the compound (also places report stills).</summary>
        public const uint Seed = 17;

        private readonly List<SlotObject> _slots = new List<SlotObject>();
        private readonly List<Walker> _walkers = new List<Walker>();
        private GameHost _host;
        private BaseLook _look;
        private Transform _world;
        private Vector3[] _walkPoints;
        private GameObject _surroundings;
        private Light _sun;
        private readonly List<(Light light, float baseIntensity)> _points = new List<(Light, float)>();
        private int _tier;
        private float _time;
        private readonly List<FireLight> _fireLights = new List<FireLight>();
        private Transform _yard;
        private int _yardWrecks = -1;
        private bool _yardBurning;
        private float _fxEffects = -1f;
        private bool _fxReduced;
        private float _flicker = 1f;
        private float _flickerHold;
        private readonly List<SlotMove> _moves = new List<SlotMove>();
        private ParticleSystem _dust;
        private bool _openingPower;
        private float _powerRadius;
        private float _powerLevel = 1f;
        private Color _grade;
        private float _fogScale = 1f;
        private float _fireScale = 1f;
        private readonly List<int> _litSlots = new List<int>();
        private Staging _stage;
        private float _gradeAmount;

        private static readonly int EmissionScaleId = Shader.PropertyToID("_DsEmissionScale");
        private static readonly int ConeScaleId = Shader.PropertyToID("_DsConeScale");
        private static readonly int DamageId = Shader.PropertyToID("_DsDamage");
        private MaterialPropertyBlock _damageBlock;
        private static readonly int SmokeLightId = Shader.PropertyToID("_DsSmokeLight");

        public static BaseView Instance { get; private set; }

        /// <summary>Lighting for the current game time (also read by the camera background and post stack).</summary>
        public LightKey Lighting { get; private set; } = new LightKey();

        public int SlotCount => _slots.Count;

        /// <summary>World position above a slot's facility (for labels).</summary>
        public Vector3 LabelAnchor(int slot)
        {
            return _slots[slot].Root.position + new Vector3(0, _slots[slot].Height + 0.8f, 0);
        }

        /// <summary>Ground position of a slot (plot center).</summary>
        public Vector3 SlotGround(int slot)
        {
            return _slots[slot].Root.position;
        }

        /// <summary>Height of the facility standing on a slot (0 for an empty plot).</summary>
        public float SlotHeight(int slot)
        {
            return _slots[slot].Height;
        }

        /// <summary>Center of a slot's facility at half height (what the drone frames when focusing).</summary>
        public Vector3 FocusPoint(int slot)
        {
            return _slots[slot].Root.position + new Vector3(0, _slots[slot].Height * 0.5f, 0);
        }

        /// <summary>World position of the core door (for the CORE label).</summary>
        public Vector3 CoreAnchor => new Vector3(Deadswitch.Art.Models.Core.DoorPoint.X, 6.6f, Deadswitch.Art.Models.Core.DoorPoint.Z);

        /// <summary>
        /// The opening's power (SPEC-043 s4): only lamps within <paramref name="radius"/> m of the core shine and
        /// emissive surfaces glow at <paramref name="level"/> (0..1), so the blackout and the power wave play out on the
        /// real Hub. Presentation only; <see cref="ClearOpeningPower"/> hands the lights back to the hour.
        /// </summary>
        public void OpeningPower(float radius, float level)
        {
            _openingPower = true;
            _powerRadius = radius;
            _powerLevel = Mathf.Clamp01(level);
        }

        public void ClearOpeningPower()
        {
            _openingPower = false;
            _gradeAmount = 0f;
            _fogScale = 1f;
            _fireScale = 1f;
            _litSlots.Clear();
        }

        /// <summary>The opening's fires: their light times <paramref name=scale/> (softer for the close restore shots).</summary>
        public void OpeningFire(float scale)
        {
            _fireScale = Mathf.Max(0f, scale);
        }

        /// <summary>The opening's air: fog density times <paramref name="scale"/> (thin for the long map shot).</summary>
        public void OpeningFog(float scale)
        {
            _fogScale = Mathf.Max(0f, scale);
        }

        /// <summary>
        /// A scripted Hub for the opening film (SPEC-044): the view shows <paramref name="stage"/> instead of the sim
        /// (layout, tier, yard, people) until it is cleared with null. Presentation only; the sim never sees it.
        /// </summary>
        public void Stage(Staging stage)
        {
            _stage = stage;
            if (_host != null && _host.IsReady)
            {
                Sync();
            }
        }

        /// <summary>What a staged Hub shows (SPEC-044). <see cref="Razed"/> plots are rubble on fire.</summary>
        public sealed class Staging
        {
            public SlotView[] Slots = new SlotView[0];
            public bool[] Razed = new bool[0];
            public int Tier = 1;
            public int Wreckage;
            public bool Burning;
            public int People = 12;
        }

        /// <summary>The opening's sky: fog and ambient pulled toward <paramref name="color"/> by <paramref name="amount"/> (the war's red glow).</summary>
        public void OpeningGrade(Color color, float amount)
        {
            _grade = color;
            _gradeAmount = Mathf.Clamp01(amount);
        }

        /// <summary>1 for a lamp the opening's power reaches, fading over the last few metres of the radius.</summary>
        private float Reach(Light light)
        {
            if (!_openingPower)
            {
                return 1f;
            }

            // a building the handler restored has its own lamps back, whatever the wave has reached
            foreach (int slot in _litSlots)
            {
                if (slot < _slots.Count && light.transform.IsChildOf(_slots[slot].Root))
                {
                    return 1f;
                }
            }

            Vector3 d = light.transform.position - CoreAnchor;
            d.y = 0f;
            return Mathf.Clamp01((_powerRadius - d.magnitude) / 4f);
        }

        /// <summary>The opening's restore steps (SPEC-044): this plot's lamps shine even in the dark.</summary>
        public void OpeningLit(int slot)
        {
            if (!_litSlots.Contains(slot))
            {
                _litSlots.Add(slot);
            }
        }

        /// <summary>The slot whose plot contains a ground point, or -1.</summary>
        public int SlotAt(Vector3 ground)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                Vector3 local = _slots[i].Root.InverseTransformPoint(ground);
                if (Mathf.Abs(local.x) < 2.5f && Mathf.Abs(local.z) < 2.5f)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsCoreDoor(Vector3 ground)
        {
            return Mathf.Abs(ground.x) < 3.2f && ground.z > Deadswitch.Art.Models.Core.FacadeZ - 4.5f && ground.z < Deadswitch.Art.Models.Core.FacadeZ;
        }

        /// <summary>Highlights a slot (selection ring); -1 clears.</summary>
        public void Select(int slot)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Selection.SetActive(i == slot);
            }
        }

        private void Awake()
        {
            Instance = this;
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                go.SetActive(false);
            }
        }

        private void Start()
        {
            _host = GameHost.Instance;
            _look = BaseLook.Load();
            _world = new GameObject("World").transform;
            _world.SetParent(transform, false);
            SetupLighting();

            Spawn("Terrain", new Model { Static = HubScene.Terrain(Seed) }, Vector3.zero, 0f, _world, true);
            Spawn("Bunker", Deadswitch.Art.Models.Core.Build(Seed), Vector3.zero, 0f, _world, true);
            Layout(_host.Sim.State.Slots.Count, _host.Sim.State.Tier);
            _host.Ticked += Sync;
            Sync();
        }

        /// <summary>
        /// Builds the surroundings for the current tier and one object per slot. Runs again when a tier-up adds
        /// plots (SPEC-013): the surroundings are replaced, existing slot objects are kept.
        /// </summary>
        private void Layout(int slots, int tier)
        {
            if (_surroundings != null)
            {
                Destroy(_surroundings);
            }

            _tier = tier;
            _surroundings = Spawn("Surroundings", HubScene.Surroundings(Seed, slots, tier), Vector3.zero, 0f, _world, true);
            // a relocation (SPEC-022) starts a smaller site: drop plots that no longer exist
            while (_slots.Count > slots)
            {
                Destroy(_slots[_slots.Count - 1].Root.gameObject);
                _slots.RemoveAt(_slots.Count - 1);
            }

            for (int i = _slots.Count; i < slots; i++)
            {
                var root = new GameObject("Slot " + i).transform;
                root.SetParent(_world, false);
                root.localPosition = ArtBridge.V(HubScene.SlotPosition(i, slots));
                root.localRotation = Quaternion.Euler(0, HubScene.SlotYaw(i, slots), 0);
                _slots.Add(new SlotObject { Root = root, Selection = SelectionRing(root) });
            }

            _walkPoints = System.Array.ConvertAll(HubScene.WalkPoints(slots), ArtBridge.V);
        }

        private void OnDestroy()
        {
            if (_host != null)
            {
                _host.Ticked -= Sync;
            }
        }

        private void SetupLighting()
        {
            _sun = new GameObject("Sun").AddComponent<Light>();
            _sun.transform.SetParent(transform, false);
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.85f;
            RenderSettings.sun = _sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            ApplyLight(_look.At(12f));
        }

        /// <summary>Time-of-day lighting (doc 11): sun, sky/ground ambient, fog, practical lamps, emission.</summary>
        private void ApplyLight(LightKey k)
        {
            _sun.color = BaseLook.Srgb(k.sunColor);
            _sun.intensity = k.sunIntensity * _look.unitySunScale;
            // Preview space is right-handed: mirror Z of the light direction.
            float az = k.sunAzimuth * Mathf.Deg2Rad;
            float el = k.sunElevation * Mathf.Deg2Rad;
            var toSun = new Vector3(Mathf.Cos(el) * Mathf.Sin(az), Mathf.Sin(el), -Mathf.Cos(el) * Mathf.Cos(az));
            _sun.transform.rotation = Quaternion.LookRotation(-toSun);

            // three.js: hemisphere light / pi plus the sky env map's diffuse at envIntensity (no pi)
            float a = ((k.ambient / Mathf.PI) + k.envIntensity) * _look.unityAmbientScale;
            RenderSettings.ambientSkyColor = BaseLook.Srgb(k.skyColor, a);
            RenderSettings.ambientEquatorColor = Color.Lerp(BaseLook.Srgb(k.skyColor), BaseLook.Srgb(k.groundColor), 0.5f) * a;
            RenderSettings.ambientGroundColor = BaseLook.Srgb(k.groundColor, a);
            RenderSettings.fogDensity = k.fogDensity * _fogScale;
            RenderSettings.fogColor = BaseLook.Srgb(k.fogColor);
            if (_gradeAmount > 0f)
            {
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, _grade, _gradeAmount);
                RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, _grade * a * 2f, _gradeAmount);
                RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, _grade * a, _gradeAmount);
            }
            float level = _openingPower ? _powerLevel : 1f;
            Shader.SetGlobalFloat(EmissionScaleId, k.emissionScale * level);
            Shader.SetGlobalFloat(ConeScaleId, k.coneIntensity * level);
            // smoke takes the light of the hour: pale grey by day, near black against the night sky
            Shader.SetGlobalColor(SmokeLightId, Color.Lerp(RenderSettings.ambientSkyColor * 1.6f + new Color(0.05f, 0.05f, 0.05f), Color.white, Mathf.Clamp01(k.sunElevation / 25f)));
            float points = k.pointScale * _look.unityPointScale;
            for (int i = _points.Count - 1; i >= 0; i--)
            {
                if (_points[i].light == null)
                {
                    _points.RemoveAt(i);
                    continue;
                }

                _points[i].light.intensity = _points[i].baseIntensity * points * Reach(_points[i].light);
            }
        }

        /// <summary>Re-reads the sim and updates every slot (called after ticks and commands).</summary>
        private void Sync()
        {
            GameState s = _host.Sim.State;
            int count = _stage != null ? _stage.Slots.Length : s.Slots.Count;
            int tier = _stage != null ? _stage.Tier : s.Tier;
            if (count != _slots.Count || tier != _tier)
            {
                bool grew = _stage == null && tier > _tier;
                Layout(count, tier);
                if (grew && DroneCamera.Instance != null)
                {
                    // show the payoff: glide over the new district (SPEC-013)
                    DroneCamera.Instance.Focus(new Vector3(0, 0, HubScene.DistrictZ + 10f));
                }
            }

            // a settings change (effect intensity, reduced motion) rebuilds the scar effects
            bool fxChanged = !Mathf.Approximately(_fxEffects, _host.Settings.Effects) || _fxReduced != _host.Settings.ReducedMotion;
            _fxEffects = _host.Settings.Effects;
            _fxReduced = _host.Settings.ReducedMotion;
            if (_stage != null)
            {
                SyncYard(_stage.Wreckage, _stage.Burning, fxChanged);
            }
            else
            {
                bool burning = s.ScarredAtTick > 0 && s.Tick - s.ScarredAtTick < (long)_host.Sim.Config.Scars.BurnHours * SimConfig.TicksPerHour;
                SyncYard(s.Wreckage, burning, fxChanged);
            }

            for (int i = 0; i < _slots.Count && i < count; i++)
            {
                SlotView v = _stage != null ? _stage.Slots[i] : SlotView.From(s, i);
                bool razed = _stage != null && i < _stage.Razed.Length && _stage.Razed[i];
                bool slotFx = fxChanged && (v.Damage > 0 || razed);
                SlotObject o = _slots[i];
                bool shapeChanged = !o.Built || v.Kind != o.View.Kind || v.Level != o.View.Level || v.UnderConstruction != o.View.UnderConstruction || v.Damage != o.View.Damage || razed != o.Razed || slotFx;
                if (shapeChanged)
                {
                    bool first = !o.Built;
                    SlotView before = o.View;
                    o.Razed = razed;
                    Rebuild(o, v, i);
                    if (!first)
                    {
                        Animate(o, before, v);
                    }
                }
                else if (v.Powered != o.View.Powered || v.Unmanned != o.View.Unmanned)
                {
                    ApplyPower(o, v);
                }

                o.View = v;
            }

            int people = _stage != null ? Mathf.Clamp(_stage.People / 3, 0, _walkPoints.Length) : Mathf.Clamp(s.People / 3, 2, _walkPoints.Length);
            while (_walkers.Count < people)
            {
                _walkers.Add(NewWalker(_walkers.Count));
            }

            while (_walkers.Count > people)
            {
                Destroy(_walkers[_walkers.Count - 1].T.gameObject);
                _walkers.RemoveAt(_walkers.Count - 1);
            }
        }

        /// <summary>
        /// Construction motion (SPEC-040 ideas 73-74): a new site rises out of the ground in dust, a finished build
        /// punches up and settles, fresh damage jolts the facility with a burst. Off under reduced motion.
        /// </summary>
        private void Animate(SlotObject o, SlotView before, SlotView now)
        {
            if (_host.Settings.ReducedMotion)
            {
                return;
            }

            _dust ??= BattleFx.BurstEmitter(_world, true);
            Vector3 at = o.Root.position + new Vector3(0, 0.4f, 0);
            float effects = Mathf.Max(0.25f, _host.Settings.Effects);
            if (now.UnderConstruction && !before.UnderConstruction)
            {
                _moves.Add(new SlotMove { Target = o.Root, Kind = 0, Seconds = 0.9f });
                BattleFx.Burst(_dust, at, (int)(14 * effects), 1.2f);
            }
            else if (before.UnderConstruction && !now.UnderConstruction && now.Kind != FacilityKind.None)
            {
                _moves.Add(new SlotMove { Target = o.Root, Kind = 1, Seconds = 0.55f });
                BattleFx.Burst(_dust, at, (int)(10 * effects), 1f);
            }
            else if (now.Damage > before.Damage)
            {
                _moves.Add(new SlotMove { Target = o.Root, Kind = 2, Seconds = 0.45f });
                BattleFx.Burst(_dust, at + new Vector3(0, o.Height * 0.5f, 0), (int)(12 * effects), 1.4f);
            }
        }

        private void TickMoves(float dt)
        {
            for (int i = _moves.Count - 1; i >= 0; i--)
            {
                SlotMove m = _moves[i];
                m.Time += dt;
                float t = Mathf.Clamp01(m.Time / m.Seconds);
                if (m.Target == null)
                {
                    _moves.RemoveAt(i);
                    continue;
                }

                switch (m.Kind)
                {
                    case 0:
                        // rise from the ground
                        float y = Mathf.Lerp(0.05f, 1f, Deadswitch.Game.UI.Ease.OutBack(t));
                        float xz = Mathf.Lerp(0.92f, 1f, t);
                        m.Target.localScale = new Vector3(xz, y, xz);
                        break;
                    case 1:
                        // punch on completion
                        float p = 1f + (0.12f * Mathf.Sin(t * Mathf.PI) * (1f - t));
                        m.Target.localScale = new Vector3(p, p + (0.06f * Mathf.Sin(t * Mathf.PI)), p);
                        break;
                    default:
                        // a jolt from a hit
                        float k = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t) * 0.05f;
                        m.Target.localScale = new Vector3(1f + k, 1f - k, 1f + k);
                        break;
                }

                if (t >= 1f)
                {
                    m.Target.localScale = Vector3.one;
                    _moves.RemoveAt(i);
                }
            }
        }

        private void Rebuild(SlotObject o, SlotView v, int slot)
        {
            foreach (Transform child in o.Root)
            {
                if (child.gameObject != o.Selection)
                {
                    Destroy(child.gameObject);
                }
            }

            o.Parts.Clear();
            o.Beacons.Clear();
            o.StatusLights.Clear();
            o.Renderers.Clear();
            o.Cones.Clear();
            o.Built = true;
            o.Height = 0.5f;

            Spawn("Pad", new Model { Static = Facilities.Pad(Seed + (uint)slot, (v.Kind == FacilityKind.None && !v.UnderConstruction) || o.Razed) }, Vector3.zero, 0f, o.Root, true);
            if (o.Razed)
            {
                // the opening's ruin (SPEC-044): the facility came down, a burning pile on its footprint
                Facilities.Build(v.Kind == FacilityKind.None ? FacilityKind.LifeSupport : v.Kind, Mathf.Max(1, v.Level), Seed + (uint)(slot * 31)).Static.Bounds(out System.Numerics.Vector3 min, out System.Numerics.Vector3 max);
                SpawnScars(Scars.Rubble(min, max, Seed + (uint)(slot * 71)), o.Root);
                o.Height = 1.5f;
                return;
            }

            if (v.Kind != FacilityKind.None)
            {
                Model model = Facilities.Build(v.Kind, v.Level, Seed + (uint)(slot * 31));
                SpawnFacility(o, model, v);
                model.Static.Bounds(out _, out System.Numerics.Vector3 max);
                o.Height = max.Y;
            }

            if (v.UnderConstruction)
            {
                Spawn("Scaffold", new Model { Static = Facilities.Scaffold(Mathf.Max(2.8f, o.Height), Seed + (uint)slot) }, Vector3.zero, 0f, o.Root, true);
                o.Height = Mathf.Max(o.Height, 2.8f);
            }

            if (v.Kind != FacilityKind.None && v.Damage > 0)
            {
                // battle scars (SPEC-018): soot, debris, embers, then fire and smoke by damage
                Model facility = Facilities.Build(v.Kind, v.Level, Seed + (uint)(slot * 31));
                facility.Static.Bounds(out System.Numerics.Vector3 min, out System.Numerics.Vector3 max);
                ScarSet scars = Scars.Facility(v.Damage, min, max, Seed + (uint)(slot * 53));
                SpawnScars(scars, o.Root);
            }

            ApplyPower(o, v);
        }

        /// <summary>Scar mesh, flickering fire lights and particle fire and smoke under a parent.</summary>
        private void SpawnScars(ScarSet scars, Transform parent)
        {
            var root = new GameObject("Scars").transform;
            root.SetParent(parent, false);
            MeshObject("Scar Mesh", scars.Model.Static, root, true);
            foreach (LightSpec spec in scars.Model.Lights)
            {
                Light l = PointLight(spec, root, false);
                _fireLights.Add(new FireLight { Light = l, BaseIntensity = spec.Intensity, Seed = Random.Range(0f, 100f) });
            }

            BattleFx.Attach(scars, root, _host.Settings.Effects, _host.Settings.ReducedMotion, new List<ParticleSystem>());
        }

        /// <summary>Yard wrecks (SPEC-018): burnt hulks and craters at fixed spots; the newest burn while fresh.</summary>
        private void SyncYard(int wreckage, bool burning, bool force)
        {
            int wrecks = Mathf.Min(wreckage, Scars.SpotCount);
            if (!force && wrecks == _yardWrecks && burning == _yardBurning)
            {
                return;
            }

            _yardWrecks = wrecks;
            _yardBurning = burning;
            if (_yard != null)
            {
                Destroy(_yard.gameObject);
            }

            _yard = new GameObject("Yard Wrecks").transform;
            _yard.SetParent(_world, false);
            for (int i = 0; i < wrecks; i++)
            {
                System.Numerics.Vector3 spot = Scars.Spot(i);
                var at = new GameObject("Wreck " + i).transform;
                at.SetParent(_yard, false);
                at.localPosition = new Vector3(spot.X, HubScene.Height(spot.X, spot.Z, Seed), spot.Z);
                at.localRotation = Quaternion.Euler(0, Scars.SpotYaw(i), 0);
                SpawnScars(Scars.Wreck(i, burning && i >= wrecks - 2, Seed), at);
            }
        }

        private void SpawnFacility(SlotObject o, Model model, SlotView v)
        {
            GameObject body = MeshObject("Facility", model.Static, o.Root, v.Powered);
            o.Renderers.Add(body.GetComponent<MeshRenderer>());
            GameObject cones = ConeObject(model, o.Root);
            if (cones != null)
            {
                o.Cones.Add(cones);
            }
            foreach (AnimPart part in model.Parts)
            {
                var pivot = new GameObject("Part").transform;
                pivot.SetParent(o.Root, false);
                pivot.localPosition = ArtBridge.V(part.Pivot);
                // Parts are authored in pivot-local space, so the mesh sits at the pivot's origin.
                GameObject mesh = MeshObject("Mesh", part.Mesh, pivot, v.Powered);
                o.Renderers.Add(mesh.GetComponent<MeshRenderer>());
                o.Parts.Add(new PartState { Pivot = pivot, Spec = part, Phase = Random.value * 10f });
            }

            foreach (LightSpec spec in model.Lights)
            {
                Light l = PointLight(spec, o.Root);
                if (spec.Role == LightRole.Beacon)
                {
                    var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(bulb.GetComponent<Collider>());
                    bulb.transform.SetParent(l.transform, false);
                    bulb.transform.localScale = Vector3.one * 0.2f;
                    bulb.GetComponent<MeshRenderer>().sharedMaterial = ArtBridge.On[(int)Deadswitch.Art.Geometry.Mat.LampAmber];
                    o.Beacons.Add(l);
                }
                else if (spec.Role == LightRole.Status)
                {
                    o.StatusLights.Add(l);
                }
            }
        }

        /// <summary>
        /// Damaged facility (SPEC-018): a red rim glow pulsing at <see cref="ScarFx.damagePulseHz"/>, stronger per damage
        /// level, so a hurt building reads at a glance. Steady (no pulse) under reduced motion.
        /// </summary>
        private void DamageGlow(SlotObject o, bool reduced)
        {
            int damage = o.View.Kind != FacilityKind.None ? o.View.Damage : 0;
            if (damage <= 0)
            {
                if (o.Glowing)
                {
                    foreach (MeshRenderer r in o.Renderers)
                    {
                        r.SetPropertyBlock(null);
                    }

                    o.Glowing = false;
                }

                return;
            }

            _damageBlock ??= new MaterialPropertyBlock();
            ScarFx fx = _look.scarFx;
            float pulse = reduced ? 0.6f : 0.5f + (0.5f * Mathf.Sin(_time * fx.damagePulseHz * Mathf.PI * 2f));
            float k = fx.damageGlow * (0.4f + (0.3f * Mathf.Min(3, damage))) * (0.25f + (0.75f * pulse)) * _fireScale;
            var glow = new Vector4(fx.damageColor[0] * k, fx.damageColor[1] * k, fx.damageColor[2] * k, fx.damageTint);
            foreach (MeshRenderer r in o.Renderers)
            {
                r.GetPropertyBlock(_damageBlock);
                _damageBlock.SetVector(DamageId, glow);
                r.SetPropertyBlock(_damageBlock);
            }

            o.Glowing = true;
        }

        /// <summary>Lamps dark and status lights off when unpowered; amber beacons only while unmanned.</summary>
        private void ApplyPower(SlotObject o, SlotView v)
        {
            foreach (MeshRenderer r in o.Renderers)
            {
                r.sharedMaterials = ArtBridge.Swap(r.sharedMaterials, v.Powered);
            }

            foreach (Light l in o.StatusLights)
            {
                l.enabled = v.Powered;
            }

            foreach (GameObject c in o.Cones)
            {
                c.SetActive(v.Powered);
            }

            foreach (Light l in o.Beacons)
            {
                l.gameObject.SetActive(v.Unmanned);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _time += dt;
            if (_host != null && _host.IsReady)
            {
                long tick = _host.Sim.State.Tick;
                Lighting = _look.WithMoon(_look.At(BaseLook.Hour(tick, _host.TickProgress)), BaseLook.Night(tick));
                ApplyLight(Lighting);
                CorruptionFlicker();
            }

            bool reduced = _host != null && _host.Settings.ReducedMotion;
            TickMoves(dt);
            foreach (SlotObject o in _slots)
            {
                bool powered = o.View.Powered;
                foreach (PartState p in o.Parts)
                {
                    if (!powered)
                    {
                        continue;
                    }

                    float speedScale = reduced ? 0.25f : 1f;
                    switch (p.Spec.Kind)
                    {
                        case AnimKind.SpinY:
                            p.Pivot.localRotation *= Quaternion.Euler(0, p.Spec.Speed * dt * speedScale, 0);
                            break;
                        case AnimKind.SpinZ:
                            p.Pivot.localRotation *= Quaternion.Euler(0, 0, p.Spec.Speed * dt * speedScale);
                            break;
                        case AnimKind.SweepY:
                            p.Phase += dt * speedScale;
                            p.Pivot.localRotation = Quaternion.Euler(0, Mathf.Sin(p.Phase * p.Spec.Speed * Mathf.PI * 2f) * p.Spec.Range, 0);
                            break;
                    }
                }

                bool blink = Mathf.Repeat(_time, 1.1f) < 0.45f;
                foreach (Light b in o.Beacons)
                {
                    b.enabled = blink;
                    b.transform.GetChild(0).gameObject.SetActive(blink);
                }

                DamageGlow(o, reduced);
            }

            foreach (Walker w in _walkers)
            {
                Walk(w, dt, reduced);
            }

            float points = Lighting.pointScale * _look.unityPointScale;
            for (int i = _fireLights.Count - 1; i >= 0; i--)
            {
                FireLight f = _fireLights[i];
                if (f.Light == null)
                {
                    _fireLights.RemoveAt(i);
                    continue;
                }

                // fire stays readable by day: never below a floor of the night strength
                float flicker = reduced ? 0.9f : BattleFx.Flicker(_time, f.Seed);
                f.Light.intensity = f.BaseIntensity * Mathf.Max(points, 0.6f * _look.unityPointScale) * flicker * _fireScale;
            }
        }

        /// <summary>
        /// Corruption visuals (doc 06 s3): from Unstable the Hub's lamps and lights stutter, harder at Critical.
        /// Scaled by effect intensity; off with reduced motion.
        /// </summary>
        private void CorruptionFlicker()
        {
            if (_host.Settings.ReducedMotion || _host.Settings.Effects <= 0f)
            {
                return;
            }

            int band = (int)Sim.Systems.CorruptionSystem.Band(_host.Sim.Config, _host.Sim.State.CorruptionMilli);
            if (band < 2)
            {
                return;
            }

            _flickerHold -= Time.deltaTime;
            if (_flickerHold <= 0f)
            {
                bool dip = Random.value < (band == 3 ? 0.12f : 0.05f) * _host.Settings.Effects;
                _flicker = dip ? Random.Range(0.05f, 0.4f) : 1f;
                _flickerHold = dip ? Random.Range(0.04f, 0.16f) : Random.Range(0.1f, 0.4f);
            }

            if (_flicker < 1f)
            {
                Shader.SetGlobalFloat(EmissionScaleId, Lighting.emissionScale * _flicker);
                float points = Lighting.pointScale * _look.unityPointScale * _flicker;
                foreach ((Light light, float baseIntensity) p in _points)
                {
                    if (p.light != null)
                    {
                        p.light.intensity = p.baseIntensity * points;
                    }
                }
            }
        }

        private Walker NewWalker(int index)
        {
            GameObject go = MeshObject("Person " + index, Props.Person(Seed + 900 + (uint)index), _world, true);
            var w = new Walker { T = go.transform, Target = Random.Range(0, _walkPoints.Length), Wait = Random.Range(0f, 3f) };
            w.T.position = _walkPoints[(index * 5) % _walkPoints.Length] + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            return w;
        }

        private void Walk(Walker w, float dt, bool reduced)
        {
            if (w.Wait > 0f)
            {
                w.Wait -= dt;
                return;
            }

            Vector3 target = _walkPoints[w.Target] + w.Offset;
            Vector3 to = target - w.T.position;
            to.y = 0;
            if (to.magnitude < 0.2f)
            {
                w.Target = Random.Range(0, _walkPoints.Length);
                w.Offset = new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f));
                w.Wait = Random.Range(1.5f, 6f);
                return;
            }

            Vector3 step = to.normalized * Mathf.Min(to.magnitude, 1.15f * dt);
            Vector3 p = w.T.position + step;
            w.Stride += dt * 9f;
            p.y = HubScene.Height(p.x, p.z, Seed) + (reduced ? 0f : Mathf.Abs(Mathf.Sin(w.Stride)) * 0.05f);
            w.T.position = p;
            w.T.rotation = Quaternion.Slerp(w.T.rotation, Quaternion.LookRotation(-to.normalized), dt * 6f);
        }

        private GameObject Spawn(string name, Model model, Vector3 position, float yaw, Transform parent, bool powered)
        {
            GameObject go = MeshObject(name, model.Static, parent, powered);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            ConeObject(model, go.transform);
            foreach (LightSpec spec in model.Lights)
            {
                PointLight(spec, go.transform);
            }

            return go;
        }

        /// <summary>Additive light cones render apart from the shadow-casting mesh.</summary>
        private static GameObject ConeObject(Model model, Transform parent)
        {
            if (model.Cones.IsEmpty)
            {
                return null;
            }

            GameObject cones = MeshObject("Light Cones", model.Cones, parent, true);
            MeshRenderer r = cones.GetComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return cones;
        }

        private static GameObject MeshObject(string name, Deadswitch.Art.Geometry.MeshData data, Transform parent, bool powered)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Mesh mesh = ArtBridge.ToMesh(data, name, out Material[] mats, powered);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
            return go;
        }

        private Light PointLight(LightSpec spec, Transform parent, bool timeOfDay = true)
        {
            var l = new GameObject("Light").AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = ArtBridge.V(spec.Position);
            l.type = LightType.Point;
            l.color = new Color(spec.Color.X, spec.Color.Y, spec.Color.Z);
            l.intensity = spec.Intensity * Lighting.pointScale * _look.unityPointScale;
            if (timeOfDay)
            {
                _points.Add((l, spec.Intensity));
            }
            l.range = spec.Range * _look.rangeScale;
            l.shadows = LightShadows.None;
            if (spec.Intensity <= 0f)
            {
                l.enabled = false;
            }

            return l;
        }

        private static GameObject SelectionRing(Transform parent)
        {
            var ring = new GameObject("Selection");
            ring.transform.SetParent(parent, false);
            var b = new Deadswitch.Art.Geometry.MeshBuilder(7);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    b.Box(new System.Numerics.Vector3(sx * 2.45f, 0.2f, sz * 2.0f), new System.Numerics.Vector3(0.12f, 0.06f, 0.9f), Deadswitch.Art.Geometry.Mat.LampPhosphor, 0f);
                    b.Box(new System.Numerics.Vector3(sx * 2.0f, 0.2f, sz * 2.45f), new System.Numerics.Vector3(0.9f, 0.06f, 0.12f), Deadswitch.Art.Geometry.Mat.LampPhosphor, 0f);
                }
            }

            Mesh mesh = ArtBridge.ToMesh(b.Mesh, "Selection", out Material[] mats);
            ring.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = ring.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.Off;
            ring.SetActive(false);
            return ring;
        }

        private sealed class SlotMove
        {
            public Transform Target;
            public int Kind;
            public float Seconds;
            public float Time;
        }

        private sealed class SlotObject
        {
            public Transform Root;
            public GameObject Selection;
            public SlotView View;
            public bool Built;
            public float Height;
            public bool Glowing;
            public bool Razed;
            public readonly List<PartState> Parts = new List<PartState>();
            public readonly List<Light> Beacons = new List<Light>();
            public readonly List<Light> StatusLights = new List<Light>();
            public readonly List<MeshRenderer> Renderers = new List<MeshRenderer>();
            public readonly List<GameObject> Cones = new List<GameObject>();
        }

        private sealed class FireLight
        {
            public Light Light;
            public float BaseIntensity;
            public float Seed;
        }

        private sealed class PartState
        {
            public Transform Pivot;
            public AnimPart Spec;
            public float Phase;
        }

        private sealed class Walker
        {
            public Transform T;
            public int Target;
            public Vector3 Offset;
            public float Wait;
            public float Stride;
        }
    }
}
