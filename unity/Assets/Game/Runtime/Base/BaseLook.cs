using UnityEngine;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// Look of the 3D base (lighting, fog, camera, post). Loaded from Resources/Base/BaseLook.json, the same file
    /// tools/basepreview renders with, so headless previews and the game match. Colors are sRGB 0..1.
    /// </summary>
    /// <summary>Lighting at one game hour (a keyframe of <see cref="BaseLook.times"/>). Colors are sRGB 0..1.</summary>
    [System.Serializable]
    public sealed class LightKey
    {
        public float hour;
        public float exposure = 1f;
        public float[] skyColor = { 0.62f, 0.64f, 0.66f };
        public float[] groundColor = { 0.22f, 0.2f, 0.17f };
        public float ambient = 1.1f;
        public float[] fogColor = { 0.57f, 0.58f, 0.59f };
        public float fogDensity = 0.0075f;
        public float[] sunColor = { 0.96f, 0.93f, 0.87f };
        public float sunIntensity = 2.5f;
        public float sunAzimuth = -38f;
        public float sunElevation = 50f;
        public float pointScale = 1.5f;
        public float emissionScale = 0.3f;
        public float envIntensity = 0.8f;
        public float coneIntensity;
        public float bloomStrength = 0.2f;
        public float bloomRadius = 0.4f;
        public float bloomThreshold = 0.92f;

        /// <summary>Linear blend of two keys (t = 0..1); colors blend per channel.</summary>
        public static LightKey Lerp(LightKey a, LightKey b, float t)
        {
            return new LightKey
            {
                hour = Mathf.Lerp(a.hour, b.hour, t),
                exposure = Mathf.Lerp(a.exposure, b.exposure, t),
                skyColor = Mix(a.skyColor, b.skyColor, t),
                groundColor = Mix(a.groundColor, b.groundColor, t),
                ambient = Mathf.Lerp(a.ambient, b.ambient, t),
                fogColor = Mix(a.fogColor, b.fogColor, t),
                fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t),
                sunColor = Mix(a.sunColor, b.sunColor, t),
                sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
                sunAzimuth = Mathf.Lerp(a.sunAzimuth, b.sunAzimuth, t),
                sunElevation = Mathf.Lerp(a.sunElevation, b.sunElevation, t),
                pointScale = Mathf.Lerp(a.pointScale, b.pointScale, t),
                emissionScale = Mathf.Lerp(a.emissionScale, b.emissionScale, t),
                envIntensity = Mathf.Lerp(a.envIntensity, b.envIntensity, t),
                coneIntensity = Mathf.Lerp(a.coneIntensity, b.coneIntensity, t),
                bloomStrength = Mathf.Lerp(a.bloomStrength, b.bloomStrength, t),
                bloomRadius = Mathf.Lerp(a.bloomRadius, b.bloomRadius, t),
                bloomThreshold = Mathf.Lerp(a.bloomThreshold, b.bloomThreshold, t),
            };
        }

        private static float[] Mix(float[] a, float[] b, float t)
        {
            return new[] { Mathf.Lerp(a[0], b[0], t), Mathf.Lerp(a[1], b[1], t), Mathf.Lerp(a[2], b[2], t) };
        }
    }

    /// <summary>
    /// Look of the 3D base (camera, post, time-of-day lighting). Loaded from Resources/Base/BaseLook.json, the
    /// same file tools/basepreview renders with, so headless previews and the game match. Lighting comes from
    /// keyframes by game hour (doc 11: overcast day is the reference look; dusk and night are variants).
    /// </summary>
    [System.Serializable]
    public sealed class BaseLook
    {
        public float fov = 30f;
        public float camElevation = 43f;
        public float camAzimuth;
        public float camDistance = 70f;
        public float[] camTarget = { 0f, 1f, -1f };
        public float grain = 0.018f;
        public float vignette = 0.18f;
        public float chromatic = 0.004f;
        public float aoIntensity = 1.5f;
        public float shadowSoftness = 4f;
        public float rangeScale = 1.6f;
        public LightKey[] times = { new LightKey { hour = 0f }, new LightKey { hour = 24f } };

        // Unity-only conversion factors (three.js uses physical light units). Tune on device.
        public float unitySunScale = 0.6f;
        public float unityPointScale = 0.3f;
        public float unityAmbientScale = 1f;

        private static BaseLook _cached;

        public static BaseLook Load()
        {
            if (_cached != null)
            {
                return _cached;
            }

            var asset = Resources.Load<TextAsset>("Base/BaseLook");
            _cached = asset != null ? JsonUtility.FromJson<BaseLook>(asset.text) : new BaseLook();
            return _cached;
        }

        public static Color Srgb(float[] c, float scale = 1f)
        {
            return new Color(c[0] * scale, c[1] * scale, c[2] * scale, 1f);
        }

        /// <summary>Game hour (0..24) of a sim tick plus the fraction toward the next one.</summary>
        public static float Hour(long tick, float fraction)
        {
            return ((tick % 1440) + fraction) / 60f;
        }

        /// <summary>Lighting at a game hour, blended between the surrounding keyframes (same rule as the preview).</summary>
        public LightKey At(float hour)
        {
            int i = 0;
            while (i < times.Length - 2 && times[i + 1].hour <= hour)
            {
                i++;
            }

            LightKey a = times[i];
            LightKey b = times[Mathf.Min(i + 1, times.Length - 1)];
            return LightKey.Lerp(a, b, Mathf.Clamp01((hour - a.hour) / Mathf.Max(0.0001f, b.hour - a.hour)));
        }

        /// <summary>
        /// Unity's camera looks from -Z (the preview's +Z): converts the target from preview (right-handed) space.
        /// </summary>
        public Vector3 Target => new Vector3(camTarget[0], camTarget[1], -camTarget[2]);
    }
}
