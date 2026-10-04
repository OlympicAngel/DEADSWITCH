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
    /// URP post stack for the drone feed (ADR-0007, doc 11): ACES, exposure and bloom from the time-of-day
    /// lighting (BaseLook keyframes), and faint sensor effects (grain, chromatic aberration, vignette) that scale
    /// with the effect-intensity setting and corruption. No depth of field: blur reads as a miniature (doc 11).
    /// Kept in its own assembly because it depends on URP.
    /// </summary>
    public sealed class PostFx : MonoBehaviour
    {
        private BaseLook _look;
        private Volume _volume;
        private ColorAdjustments _color;
        private Bloom _bloom;
        private float _envDay;
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
            _color = profile.Add<ColorAdjustments>(true);
            _color.contrast.Override(10f);
            _color.saturation.Override(-6f);

            _bloom = profile.Add<Bloom>(true);
            _bloom.scatter.Override(0.65f);

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
            if (_color == null || host == null || !host.IsReady)
            {
                return;
            }

            LightKey k = BaseView.Instance != null ? BaseView.Instance.Lighting : _look.At(12f);
            // three.js ACES pre-scales by exposure / 0.6 (+0.74 stop); URP's does not, so match the preview here
            _color.postExposure.Override(Mathf.Log(Mathf.Max(0.01f, k.exposure) / 0.6f, 2f));
            _bloom.intensity.Override(k.bloomStrength * 1.4f);
            _bloom.threshold.Override(k.bloomThreshold);
            RenderSettings.reflectionIntensity = k.envIntensity / Mathf.Max(0.01f, _envDay);

            float effects = host.Settings.Effects;
            GameState s = host.Sim.State;
            float corruption = (float)CorruptionSystem.Band(host.Config, s.CorruptionMilli) / 3f;
            _grain.intensity.Override(Mathf.Clamp01((_look.grain * 6f) + (corruption * 0.35f)) * effects);
            _ca.intensity.Override(Mathf.Clamp01((_look.chromatic * 10f) + (corruption * 0.5f)) * effects);
            _vignette.intensity.Override(_look.vignette * 0.7f * (0.5f + (0.5f * effects)));
        }

        /// <summary>Overcast sky-gradient cubemap (midday key) so wet mud and metal reflect the sky; its intensity follows the hour.</summary>
        private void SetupReflections()
        {
            const int size = 32;
            LightKey day = _look.At(12f);
            _envDay = day.envIntensity;
            var cube = new Cubemap(size, TextureFormat.RGBA32, false) { name = "DS Sky Reflection" };
            Color sky = BaseLook.Srgb(day.skyColor, day.envIntensity);
            Color ground = BaseLook.Srgb(day.groundColor, day.envIntensity);
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
