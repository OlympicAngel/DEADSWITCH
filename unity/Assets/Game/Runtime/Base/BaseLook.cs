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

        /// <summary>How much this key is moonlit night (0 = day, 1 = night): scales sun and ambient by the moon level.</summary>
        public float moon;

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
                moon = Mathf.Lerp(a.moon, b.moon, t),
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
    /// <summary>
    /// Battle-scar effects (SPEC-018): multipliers on the fire and smoke emitters, and the pulsing red rim glow that
    /// marks a damaged facility. Colors are linear 0..1; the glow pulse holds steady under reduced motion.
    /// </summary>
    [System.Serializable]
    public sealed class ScarFx
    {
        public float fireRate = 1f;
        public float fireSize = 1f;
        public float fireSpread = 1f;
        public float fireBrightness = 1f;
        public float smokeRate = 1f;
        public float smokeSize = 1f;
        public float smokeDark = 0f;
        public float[] damageColor = { 1f, 0.12f, 0.05f };
        public float damageGlow = 2f;
        public float damageTint = 0.12f;
        public float damagePulseHz = 1.1f;
    }

    /// <summary>Sector map look (SPEC-033): the far recon camera needs thinner fog and a longer shadow reach.</summary>
    [System.Serializable]
    public sealed class MapLook
    {
        public float fogScale = 0.2f;

        /// <summary>Half size of the sun's shadow frustum in the headless preview.</summary>
        public float shadowExtent = 80f;

        /// <summary>URP shadow distance while the map renders (meters from the map camera).</summary>
        public float shadowDistance = 360f;
    }

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

        /// <summary>Moonlight level per night (cycled by night index), so each night has its own light.</summary>
        public float[] moonPhases = { 1f };

        /// <summary>Battle-scar fire, smoke and the damage glow on hurt facilities (Unity only).</summary>
        public ScarFx scarFx = new ScarFx();

        /// <summary>Battle report stills: exposure multiplier at full night (scaled by the key's moon weight), so night panels stay readable.</summary>
        public float reportNightBoost = 1f;

        /// <summary>Sector map overrides (SPEC-033).</summary>
        public MapLook map = new MapLook();

        // Unity-only conversion factors. three.js divides direct and hemisphere light by pi (Lambert BRDF); URP does
        // not, so the sun is 1/pi. Points were matched by eye in the Editor (URP's range falloff is softer). The
        // ambient also takes three.js's sky env-map diffuse (envIntensity), see BaseView.ApplyLight. Tune on device.
        public float unitySunScale = 0.32f;
        public float unityPointScale = 0.6f;
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

        /// <summary>Night index of a tick: a night runs noon to noon, so the hours after midnight keep the evening's moon.</summary>
        public static long Night(long tick)
        {
            long t = tick - 720;
            return t >= 0 ? t / 1440 : ((t + 1) / 1440) - 1;
        }

        /// <summary>Applies the moon level of a night to a blended key (same rule as tools/basepreview).</summary>
        public LightKey WithMoon(LightKey k, long night)
        {
            int n = moonPhases.Length;
            float level = n == 0 ? 1f : moonPhases[(int)(((night % n) + n) % n)];
            k.sunIntensity *= Mathf.Lerp(1f, level, k.moon);
            k.ambient *= Mathf.Lerp(1f, 0.6f + (0.4f * level), k.moon);
            return k;
        }

        /// <summary>
        /// Unity's camera looks from -Z (the preview's +Z): converts the target from preview (right-handed) space.
        /// </summary>
        public Vector3 Target => new Vector3(camTarget[0], camTarget[1], -camTarget[2]);
    }
}
