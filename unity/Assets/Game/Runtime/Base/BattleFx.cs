using System.Collections.Generic;
using Deadswitch.Art.Models;
using UnityEngine;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// Fire and smoke for battle scars (SPEC-018), procedural particles with no imported assets. Thickness follows
    /// the damage: a thin grey wisp, a dark column with flames, a heavy black plume with embers. Effect intensity
    /// scales emission; reduced motion slows and thins everything. Presentation only.
    /// </summary>
    public static class BattleFx
    {
        private static Material _smoke;
        private static Material _fire;

        /// <summary>Additive glow material (tracers, scan rings, data links, holographic markers).</summary>
        public static Material Additive
        {
            get
            {
                Build();
                return _fire;
            }
        }

        /// <summary>Adds the fire and smoke emitters of a scar set under <paramref name="parent"/> (scar-local space).</summary>
        public static void Attach(ScarSet set, Transform parent, float effects, bool reduced, List<ParticleSystem> into)
        {
            Build();
            ScarFx fx = BaseLook.Load().scarFx;
            float amount = Mathf.Clamp01(effects) * (reduced ? 0.5f : 1f);
            if (amount <= 0f)
            {
                return;
            }

            foreach (System.Numerics.Vector3 p in set.Fires)
            {
                into.Add(Flames(parent, ArtBridge.V(p), amount, reduced, fx));
                if (set.SmokeLevel >= 3)
                {
                    into.Add(Embers(parent, ArtBridge.V(p), amount, reduced));
                }
            }

            if (set.SmokeLevel > 0)
            {
                foreach (System.Numerics.Vector3 p in set.Smokes)
                {
                    into.Add(Smoke(parent, ArtBridge.V(p), set.SmokeLevel, amount, reduced, fx));
                }
            }
        }

        /// <summary>A pooled burst emitter (flash, fireball or dust): nothing emits until <see cref="Burst"/>.</summary>
        public static ParticleSystem BurstEmitter(Transform parent, bool smoke)
        {
            Build();
            ParticleSystem ps = New(smoke ? "Burst Smoke" : "Burst Fire", parent, Vector3.zero, smoke ? _smoke : _fire);
            var main = ps.main;
            // keeps playing (emission off) so particles emitted later still simulate
            main.loop = true;
            main.prewarm = false;
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = smoke ? new ParticleSystem.MinMaxCurve(1.6f, 3.2f) : new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = smoke ? new ParticleSystem.MinMaxCurve(0.4f, 1.6f) : new ParticleSystem.MinMaxCurve(1.5f, 5f);
            main.startSize = smoke ? new ParticleSystem.MinMaxCurve(0.8f, 1.8f) : new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startColor = smoke ? new ParticleSystem.MinMaxGradient(new Color(0.12f, 0.11f, 0.1f, 0.7f), new Color(0.3f, 0.28f, 0.25f, 0.6f)) : new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.2f, 1f), new Color(1f, 0.85f, 0.45f, 1f));
            var emission = ps.emission;
            emission.enabled = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, smoke ? 0.5f : 1f), new Keyframe(1f, smoke ? 2.4f : 0.2f)));
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            color.color = g;
            ps.Play();
            return ps;
        }

        /// <summary>Emits one burst (explosion, muzzle flash) at a world point.</summary>
        public static void Burst(ParticleSystem ps, Vector3 world, int count, float scale)
        {
            var p = new ParticleSystem.EmitParams { position = world };
            for (int i = 0; i < count; i++)
            {
                p.position = world + (Random.insideUnitSphere * 0.4f * scale);
                p.velocity = (Random.insideUnitSphere + Vector3.up * 0.8f) * 2.5f * scale;
                ps.Emit(p, 1);
            }
        }

        /// <summary>Flicker for a fire light: layered sines so it never pulses evenly.</summary>
        public static float Flicker(float time, float seed)
        {
            return 0.78f + (0.14f * Mathf.Sin((time * 13.1f) + seed)) + (0.08f * Mathf.Sin((time * 23.7f) + (seed * 2.3f)));
        }

        private static ParticleSystem Smoke(Transform parent, Vector3 at, int level, float amount, bool reduced, ScarFx fx)
        {
            ParticleSystem ps = New("Smoke", parent, at, _smoke);
            var main = ps.main;
            float speed = reduced ? 0.45f : 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f + level, 6f + (level * 2f));
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * speed, (0.9f + (0.3f * level)) * speed);
            main.startSize = new ParticleSystem.MinMaxCurve((0.5f + (0.25f * level)) * fx.smokeSize, (0.9f + (0.45f * level)) * fx.smokeSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            float grey = (level == 1 ? 0.48f : level == 2 ? 0.2f : 0.08f) * (1f - Mathf.Clamp01(fx.smokeDark));
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(grey, grey * 0.96f, grey * 0.92f, level == 1 ? 0.28f : 0.55f), new Color(grey * 1.2f, grey * 1.15f, grey * 1.1f, level == 1 ? 0.36f : 0.7f));
            main.maxParticles = Mathf.CeilToInt(30 * level * fx.smokeRate);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = (2.2f + (2.6f * level)) * amount * fx.smokeRate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f + (3f * level);
            shape.radius = 0.15f + (0.12f * level);
            shape.rotation = new Vector3(-90f, 0, 0);

            // drift with the wind, grow and thin out as it rises
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.35f * speed, 0.7f * speed);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0.1f);
            vel.z = new ParticleSystem.MinMaxCurve(0.05f * speed, 0.25f * speed);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.45f), new Keyframe(1f, 2.6f + (0.4f * level))));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = g;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

            var noise = ps.noise;
            noise.enabled = !reduced;
            noise.strength = 0.25f;
            noise.frequency = 0.35f;
            ps.Play();
            return ps;
        }

        private static ParticleSystem Flames(Transform parent, Vector3 at, float amount, bool reduced, ScarFx fx)
        {
            ParticleSystem ps = New("Flames", parent, at, _fire);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f * Mathf.Sqrt(fx.fireSize), 0.7f * Mathf.Sqrt(fx.fireSize));
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.3f * Mathf.Sqrt(fx.fireSize));
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f * fx.fireSize, 0.55f * fx.fireSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.18f, 0.9f * fx.fireBrightness), new Color(1f, 0.78f, 0.36f, fx.fireBrightness));
            main.maxParticles = Mathf.CeilToInt(60 * fx.fireRate * Mathf.Sqrt(fx.fireSize));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            if (reduced)
            {
                main.simulationSpeed = 0.5f;
            }

            var emission = ps.emission;
            emission.rateOverTime = 26f * amount * fx.fireRate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f * fx.fireSpread;
            shape.radius = 0.22f * fx.fireSpread;
            shape.rotation = new Vector3(-90f, 0, 0);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.15f)));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.42f, 0.12f), 0.45f), new GradientColorKey(new Color(0.5f, 0.12f, 0.04f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = g;
            ps.Play();
            return ps;
        }

        private static ParticleSystem Embers(Transform parent, Vector3 at, float amount, bool reduced)
        {
            ParticleSystem ps = New("Embers", parent, at + new Vector3(0, 0.3f, 0), _fire);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0.2f, 1f), new Color(1f, 0.85f, 0.4f, 1f));
            main.gravityModifier = -0.05f;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = (reduced ? 3f : 8f) * amount;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.25f;
            shape.rotation = new Vector3(-90f, 0, 0);

            var noise = ps.noise;
            noise.enabled = !reduced;
            noise.strength = 0.6f;
            noise.frequency = 1.2f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.3f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = g;
            ps.Play();
            return ps;
        }

        private static ParticleSystem New(string name, Transform parent, Vector3 at, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = material == _smoke ? 2f : 0f;
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.prewarm = true;
            return ps;
        }

        private static void Build()
        {
            if (_smoke != null)
            {
                return;
            }

            Shader shader = Resources.Load<Shader>("Shaders/DeadswitchParticle");
            if (shader == null || !shader.isSupported)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }

            _smoke = new Material(shader) { name = "DS Smoke" };
            _smoke.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _smoke.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _smoke.SetFloat("_Softness", 1.4f);
            _smoke.SetFloat("_Noise", 0.45f);
            _smoke.SetFloat("_Lit", 1f);
            _smoke.renderQueue = 3010;

            _fire = new Material(shader) { name = "DS Fire" };
            _fire.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _fire.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            _fire.SetVector("_Color", new Vector4(2.2f, 2.2f, 2.2f, 1f));
            _fire.SetFloat("_Softness", 2.2f);
            _fire.SetFloat("_Noise", 0.6f);
            _fire.SetFloat("_Lit", 0f);
            _fire.renderQueue = 3020;
        }
    }
}
