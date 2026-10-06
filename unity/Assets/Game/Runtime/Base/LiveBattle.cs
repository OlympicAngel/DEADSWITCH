using System.Collections.Generic;
using Deadswitch.Art.Models;
using Deadswitch.Art.World;
using Deadswitch.Game.Core;
using Deadswitch.Game.UI;
using Deadswitch.Sim;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// The fight at the wall during a live battle (SPEC-020): attackers push in from the gate they really used,
    /// turrets and defenders answer with tracers and muzzle flashes, shells land among them, and every ability
    /// the handler spends shows (barrage strikes, seized machines go dark, focus fire doubles the guns).
    /// Presentation only; the outcome is the sim's. Slow motion (assist) slows only this scene.
    /// </summary>
    public sealed class LiveBattle : MonoBehaviour
    {
        private const uint Seed = BaseView.Seed;

        private readonly List<Raider> _raiders = new List<Raider>();
        private bool _hitStopping;
        private readonly List<Tracer> _tracers = new List<Tracer>();
        private readonly List<Vector3> _guns = new List<Vector3>();
        private GameHost _host;
        private Transform _root;
        private ParticleSystem _fire;
        private ParticleSystem _smoke;
        private Material _tracerMat;
        private Vector3 _gate;
        private Vector3 _outward;
        private int _raidId;
        private float _nextShot;
        private float _nextShell;
        private float _gunRate = 1f;

        public static LiveBattle Instance { get; private set; }

        /// <summary>Assist: the battle scene runs at a fraction of its speed (sim time is unaffected).</summary>
        public bool SlowMotion { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _host = GameHost.Instance;
            _host.Ticked += Sync;
            _host.EventRaised += OnSimEvent;
        }

        private void OnDestroy()
        {
            if (_host != null)
            {
                _host.Ticked -= Sync;
                _host.EventRaised -= OnSimEvent;
            }
        }

        private void Sync()
        {
            GameState s = _host.Sim.State;
            bool live = BattleSystem.InBattle(s);
            if (live && _raidId != s.RaidId)
            {
                Begin(s);
            }
            else if (!live && _raidId != 0)
            {
                End();
            }
        }

        private void OnSimEvent(SimEvent e)
        {
            if (_raidId == 0 || e.Kind != EventKind.BattleAbilityUsed)
            {
                return;
            }

            switch ((BattleAbility)e.B)
            {
                case BattleAbility.Barrage:
                    for (int i = 0; i < 6; i++)
                    {
                        Shell(Target() + (Random.insideUnitSphere * 2f), 1.4f);
                    }

                    HitStop();
                    break;
                case BattleAbility.Seize:
                    // their machines go dark and stop
                    for (int i = 0; i < _raiders.Count; i += 3)
                    {
                        _raiders[i].Seized = true;
                    }

                    break;
                case BattleAbility.FocusFire:
                    _gunRate *= 1.6f;
                    break;
                case BattleAbility.Takeover:
                    _gunRate *= 2f;
                    break;
            }
        }

        private void Begin(GameState s)
        {
            End();
            _raidId = s.RaidId;
            _root = new GameObject("Live Battle").transform;
            _root.SetParent(transform, false);
            _fire = BattleFx.BurstEmitter(_root, false);
            _smoke = BattleFx.BurstEmitter(_root, true);
            _tracerMat = _fire.GetComponent<ParticleSystemRenderer>().sharedMaterial;

            ReportScene.Gate(s.RaidGate == RaidGate.None ? RaidGate.South : s.RaidGate, Seed, out System.Numerics.Vector3 g, out System.Numerics.Vector3 o);
            _gate = ArtBridge.V(g);
            _outward = ArtBridge.V(o);

            int count = Mathf.Clamp(s.RaidStrength / 8, 6, 22);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Raider " + i);
                go.transform.SetParent(_root, false);
                Mesh mesh = ArtBridge.ToMesh(Props.Raider(Seed + 300 + (uint)i, (int)s.RaidFaction), "Raider", out Material[] mats);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterials = mats;
                Vector3 side = Vector3.Cross(Vector3.up, _outward);
                float spread = Random.Range(-5f, 5f);
                float depth = Random.Range(9f, 18f);
                var r = new Raider { T = go.transform, Side = spread, Depth = depth, Speed = Random.Range(0.35f, 0.7f), Phase = Random.value * 10f };
                r.T.position = _gate + (_outward * depth) + (side * spread);
                _raiders.Add(r);
            }

            _guns.Clear();
            for (int i = 0; i < s.Slots.Count; i++)
            {
                if (s.Slots[i].Kind == FacilityKind.Turret)
                {
                    _guns.Add(ArtBridge.V(HubScene.SlotPosition(i, s.Slots.Count)) + new Vector3(0, 2.6f, 0));
                }
            }

            // defenders on the wall by the gate
            for (int i = 0; i < Mathf.Max(2, s.Garrison); i++)
            {
                _guns.Add(_gate - (_outward * 1.5f) + (Vector3.Cross(Vector3.up, _outward) * ((i - (s.Garrison * 0.5f)) * 1.2f)) + new Vector3(0, 1.4f, 0));
            }

            _gunRate = 1f;
            _nextShot = 0f;
            _nextShell = 0.6f;
            DroneCamera.Instance?.Focus(_gate);
        }

        private void End()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }

            _raiders.Clear();
            _tracers.Clear();
            _raidId = 0;
            _root = null;
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            float dt = Time.deltaTime * (SlowMotion ? 0.3f : 1f);
            float effects = _host.Settings.Effects;
            Vector3 side = Vector3.Cross(Vector3.up, _outward);
            foreach (Raider r in _raiders)
            {
                if (!r.Seized)
                {
                    r.Depth = Mathf.Max(1.2f, r.Depth - (r.Speed * dt));
                }

                r.Phase += dt * 7f;
                Vector3 p = _gate + (_outward * r.Depth) + (side * r.Side);
                p.y = HubScene.Height(p.x, p.z, Seed) + (r.Seized ? 0f : Mathf.Abs(Mathf.Sin(r.Phase)) * 0.06f);
                r.T.position = p;
                r.T.rotation = Quaternion.LookRotation(r.Seized ? _outward : -_outward);
            }

            _nextShot -= dt;
            if (_nextShot <= 0f && _guns.Count > 0 && _raiders.Count > 0)
            {
                _nextShot = Random.Range(0.12f, 0.35f) / _gunRate;
                Vector3 from = _guns[Random.Range(0, _guns.Count)];
                Vector3 to = Target() + new Vector3(0, 0.9f, 0) + (Random.insideUnitSphere * 0.6f);
                Fire(from, to);
                BattleFx.Burst(_fire, from, 2, 0.25f);
                if (Random.value < 0.25f * effects)
                {
                    BattleFx.Burst(_smoke, to, 2, 0.4f);
                }
            }

            _nextShell -= dt;
            if (_nextShell <= 0f)
            {
                _nextShell = Random.Range(1.5f, 3.5f);
                Shell(Target() + (Random.insideUnitSphere * 3f), 1f);
            }

            for (int i = _tracers.Count - 1; i >= 0; i--)
            {
                Tracer t = _tracers[i];
                t.Life -= dt;
                if (t.Life <= 0f)
                {
                    Destroy(t.Line.gameObject);
                    _tracers.RemoveAt(i);
                    continue;
                }

                float k = t.Life / 0.09f;
                t.Line.startColor = new Color(1f, 0.8f, 0.45f, k);
                t.Line.endColor = new Color(1f, 0.6f, 0.25f, 0f);
            }
        }

        private Vector3 Target()
        {
            Raider r = _raiders[Random.Range(0, _raiders.Count)];
            return r.T.position;
        }

        /// <summary>
        /// A barrage lands: the world all but freezes for a beat (visuals only; the sim runs on unscaled time). Off with
        /// reduced motion or zero effects.
        /// </summary>
        private void HitStop()
        {
            InterfaceConfig.CinematicRules r = InterfaceConfig.Current.cinematic;
            if (r.hitStopSeconds <= 0f || _host.Settings.ReducedMotion || _host.Settings.Effects <= 0f || Time.timeScale < 1f)
            {
                return;
            }

            StartCoroutine(HitStopRoutine(r.hitStopSeconds, r.hitStopScale));
        }

        private System.Collections.IEnumerator HitStopRoutine(float seconds, float scale)
        {
            _hitStopping = true;
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = 1f;
            _hitStopping = false;
        }

        private void OnDisable()
        {
            // a battle torn down mid hit-stop must not leave the world frozen
            if (_hitStopping)
            {
                Time.timeScale = 1f;
                _hitStopping = false;
            }
        }

        private void Shell(Vector3 at, float scale)
        {
            at.y = HubScene.Height(at.x, at.z, Seed);
            float effects = Mathf.Max(0.25f, _host.Settings.Effects);
            BattleFx.Burst(_fire, at + new Vector3(0, 0.4f, 0), (int)(18 * scale * effects), 1.2f * scale);
            BattleFx.Burst(_smoke, at + new Vector3(0, 0.6f, 0), (int)(10 * scale * effects), 1f * scale);
        }

        private void Fire(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(_root, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _tracerMat;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, Vector3.Lerp(from, to, Random.Range(0.85f, 1f)));
            line.startWidth = 0.05f;
            line.endWidth = 0.02f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            _tracers.Add(new Tracer { Line = line, Life = 0.09f });
        }

        private sealed class Raider
        {
            public Transform T;
            public float Side;
            public float Depth;
            public float Speed;
            public float Phase;
            public bool Seized;
        }

        private sealed class Tracer
        {
            public LineRenderer Line;
            public float Life;
        }
    }
}
