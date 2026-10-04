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

        private static readonly int EmissionScaleId = Shader.PropertyToID("_DsEmissionScale");
        private static readonly int ConeScaleId = Shader.PropertyToID("_DsConeScale");

        public static BaseView Instance { get; private set; }

        /// <summary>Lighting for the current game time (also read by the camera background and post stack).</summary>
        public LightKey Lighting { get; private set; } = new LightKey();

        public int SlotCount => _slots.Count;

        /// <summary>World position above a slot's facility (for labels).</summary>
        public Vector3 LabelAnchor(int slot)
        {
            return _slots[slot].Root.position + new Vector3(0, _slots[slot].Height + 0.8f, 0);
        }

        /// <summary>World position of the core door (for the CORE label).</summary>
        public Vector3 CoreAnchor => new Vector3(Deadswitch.Art.Models.Core.DoorPoint.X, 6.6f, Deadswitch.Art.Models.Core.DoorPoint.Z);

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
            Layout();
            _host.Ticked += Sync;
            Sync();
        }

        /// <summary>
        /// Builds the surroundings for the current tier and one object per slot. Runs again when a tier-up adds
        /// plots (SPEC-013): the surroundings are replaced, existing slot objects are kept.
        /// </summary>
        private void Layout()
        {
            GameState s = _host.Sim.State;
            int slots = s.Slots.Count;
            if (_surroundings != null)
            {
                Destroy(_surroundings);
            }

            _tier = s.Tier;
            _surroundings = Spawn("Surroundings", HubScene.Surroundings(Seed, slots, s.Tier), Vector3.zero, 0f, _world, true);
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

            float a = k.ambient * _look.unityAmbientScale;
            RenderSettings.ambientSkyColor = BaseLook.Srgb(k.skyColor, a);
            RenderSettings.ambientEquatorColor = Color.Lerp(BaseLook.Srgb(k.skyColor), BaseLook.Srgb(k.groundColor), 0.5f) * a;
            RenderSettings.ambientGroundColor = BaseLook.Srgb(k.groundColor, a);
            RenderSettings.fogDensity = k.fogDensity;
            RenderSettings.fogColor = BaseLook.Srgb(k.fogColor);
            Shader.SetGlobalFloat(EmissionScaleId, k.emissionScale);
            Shader.SetGlobalFloat(ConeScaleId, k.coneIntensity);
            float points = k.pointScale * _look.unityPointScale;
            for (int i = _points.Count - 1; i >= 0; i--)
            {
                if (_points[i].light == null)
                {
                    _points.RemoveAt(i);
                    continue;
                }

                _points[i].light.intensity = _points[i].baseIntensity * points;
            }
        }

        /// <summary>Re-reads the sim and updates every slot (called after ticks and commands).</summary>
        private void Sync()
        {
            GameState s = _host.Sim.State;
            if (s.Slots.Count != _slots.Count || s.Tier != _tier)
            {
                bool grew = s.Tier > _tier;
                Layout();
                if (grew && DroneCamera.Instance != null)
                {
                    // show the payoff: glide over the new district (SPEC-013)
                    DroneCamera.Instance.Focus(new Vector3(0, 0, HubScene.DistrictZ + 10f));
                }
            }

            for (int i = 0; i < _slots.Count && i < s.Slots.Count; i++)
            {
                SlotView v = SlotView.From(s, i);
                SlotObject o = _slots[i];
                bool shapeChanged = !o.Built || v.Kind != o.View.Kind || v.Level != o.View.Level || v.UnderConstruction != o.View.UnderConstruction;
                if (shapeChanged)
                {
                    Rebuild(o, v, i);
                }
                else if (v.Powered != o.View.Powered || v.Unmanned != o.View.Unmanned)
                {
                    ApplyPower(o, v);
                }

                o.View = v;
            }

            int people = Mathf.Clamp(s.People / 3, 2, _walkPoints.Length);
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

            Spawn("Pad", new Model { Static = Facilities.Pad(Seed + (uint)slot, v.Kind == FacilityKind.None && !v.UnderConstruction) }, Vector3.zero, 0f, o.Root, true);
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

            ApplyPower(o, v);
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
                Lighting = _look.At(BaseLook.Hour(_host.Sim.State.Tick, _host.TickProgress));
                ApplyLight(Lighting);
            }

            bool reduced = _host != null && _host.Settings.ReducedMotion;
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
            }

            foreach (Walker w in _walkers)
            {
                Walk(w, dt, reduced);
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

        private Light PointLight(LightSpec spec, Transform parent)
        {
            var l = new GameObject("Light").AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = ArtBridge.V(spec.Position);
            l.type = LightType.Point;
            l.color = new Color(spec.Color.X, spec.Color.Y, spec.Color.Z);
            l.intensity = spec.Intensity * Lighting.pointScale * _look.unityPointScale;
            _points.Add((l, spec.Intensity));
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

        private sealed class SlotObject
        {
            public Transform Root;
            public GameObject Selection;
            public SlotView View;
            public bool Built;
            public float Height;
            public readonly List<PartState> Parts = new List<PartState>();
            public readonly List<Light> Beacons = new List<Light>();
            public readonly List<Light> StatusLights = new List<Light>();
            public readonly List<MeshRenderer> Renderers = new List<MeshRenderer>();
            public readonly List<GameObject> Cones = new List<GameObject>();
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
