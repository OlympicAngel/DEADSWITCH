using Deadswitch.Game.Base;
using Deadswitch.Game.Core;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deadswitch.Game.Rendering
{
    /// <summary>
    /// URP post stack for the drone feed (ADR-0007, doc 11): ACES, exposure and grade from BaseLook, bloom for
    /// lamps, bokeh depth of field focused on the compound (tilt-shift miniature), and sensor effects (grain,
    /// chromatic aberration, vignette) that scale with the effect-intensity setting and corruption.
    /// Kept in its own assembly because it depends on URP.
    /// </summary>
    public sealed class PostFx : MonoBehaviour
    {
        private BaseLook _look;
        private Volume _volume;
        private DepthOfField _dof;
        private FilmGrain _grain;
        private ChromaticAberration _ca;
        private Vignette _vignette;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Bootstrap.Booted += root => root.AddComponent<PostFx>();
        }

        private void Start()
        {
            _look = BaseLook.Load();
            Camera cam = DroneCamera.Instance != null ? DroneCamera.Instance.Camera : Camera.main;
            if (cam != null)
            {
                UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.Medium;
                data.renderShadows = true;
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _volume = new GameObject("PostFx Volume").AddComponent<Volume>();
            _volume.transform.SetParent(transform, false);
            _volume.isGlobal = true;
            _volume.priority = 100f;
            _volume.sharedProfile = profile;

            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(Mathf.Log(Mathf.Max(0.01f, _look.exposure), 2f));
            color.contrast.Override(10f);
            color.saturation.Override(-6f);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(_look.bloomStrength * 1.4f);
            bloom.threshold.Override(_look.bloomThreshold);
            bloom.scatter.Override(0.65f);

            _dof = profile.Add<DepthOfField>(true);
            _dof.mode.Override(DepthOfFieldMode.Bokeh);
            _dof.focusDistance.Override(_look.camDistance);
            _dof.focalLength.Override(70f + (_look.tiltBlur * 20f));
            _dof.aperture.Override(5.6f);

            _vignette = profile.Add<Vignette>(true);
            _vignette.smoothness.Override(0.45f);
            _vignette.color.Override(Color.black);

            _grain = profile.Add<FilmGrain>(true);
            _grain.type.Override(FilmGrainLookup.Thin1);
            _grain.response.Override(0.8f);

            _ca = profile.Add<ChromaticAberration>(true);

            SetupReflections();
        }

        private void LateUpdate()
        {
            GameHost host = GameHost.Instance;
            if (_dof == null || host == null || !host.IsReady)
            {
                return;
            }

            float effects = host.Settings.Effects;
            GameState s = host.Sim.State;
            float corruption = (float)CorruptionSystem.Band(host.Config, s.CorruptionMilli) / 3f;
            _grain.intensity.Override(Mathf.Clamp01((_look.grain * 6f) + (corruption * 0.35f)) * effects);
            _ca.intensity.Override(Mathf.Clamp01((_look.chromatic * 10f) + (corruption * 0.5f)) * effects);
            _vignette.intensity.Override(_look.vignette * 0.7f * (0.5f + (0.5f * effects)));

            Camera cam = DroneCamera.Instance != null ? DroneCamera.Instance.Camera : null;
            if (cam != null)
            {
                _dof.focusDistance.Override(Vector3.Distance(cam.transform.position, _look.Target));
                _dof.active = effects > 0.05f;
            }
        }

        /// <summary>Dim sky-gradient cubemap so wet mud and metal reflect the mood, not black.</summary>
        private void SetupReflections()
        {
            const int size = 32;
            var cube = new Cubemap(size, TextureFormat.RGBA32, false) { name = "DS Sky Reflection" };
            Color sky = BaseLook.Srgb(_look.skyColor, 0.6f * _look.envIntensity);
            Color ground = BaseLook.Srgb(_look.groundColor, 0.6f * _look.envIntensity);
            for (int f = 0; f < 6; f++)
            {
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Vector3 d = FaceDirection((CubemapFace)f, ((x + 0.5f) / size * 2f) - 1f, ((y + 0.5f) / size * 2f) - 1f);
                        pixels[(y * size) + x] = Color.Lerp(ground, sky, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.2f, 0.6f, d.y)));
                    }
                }

                cube.SetPixels(pixels, (CubemapFace)f);
            }

            cube.Apply();
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cube;
            RenderSettings.reflectionIntensity = 1f;
        }

        private static Vector3 FaceDirection(CubemapFace face, float u, float v)
        {
            switch (face)
            {
                case CubemapFace.PositiveX: return new Vector3(1, -v, -u).normalized;
                case CubemapFace.NegativeX: return new Vector3(-1, -v, u).normalized;
                case CubemapFace.PositiveY: return new Vector3(u, 1, v).normalized;
                case CubemapFace.NegativeY: return new Vector3(u, -1, -v).normalized;
                case CubemapFace.PositiveZ: return new Vector3(u, -v, 1).normalized;
                default: return new Vector3(-u, -v, -1).normalized;
            }
        }
    }
}
