using UnityEngine;

namespace Deadswitch.Game.Base
{
    /// <summary>
    /// Look of the 3D base (lighting, fog, camera, post). Loaded from Resources/Base/BaseLook.json, the same file
    /// tools/basepreview renders with, so headless previews and the game match. Colors are sRGB 0..1.
    /// </summary>
    [System.Serializable]
    public sealed class BaseLook
    {
        public float exposure = 1.3f;
        public float[] fogColor = { 0.17f, 0.19f, 0.19f };
        public float fogDensity = 0.0085f;
        public float[] skyColor = { 0.55f, 0.62f, 0.68f };
        public float[] groundColor = { 0.24f, 0.21f, 0.17f };
        public float ambient = 1.05f;
        public float[] sunColor = { 0.9f, 0.93f, 0.95f };
        public float sunIntensity = 2.7f;
        public float sunAzimuth = -38f;
        public float sunElevation = 36f;
        public float emissionScale = 1f;
        public float pointScale = 5f;
        public float rangeScale = 1.6f;
        public float fov = 30f;
        public float camElevation = 39f;
        public float camAzimuth;
        public float camDistance = 78f;
        public float[] camTarget = { 0f, 0f, -4.5f };
        public float bloomStrength = 0.55f;
        public float bloomRadius = 0.5f;
        public float bloomThreshold = 0.82f;
        public float tiltBlur = 2.2f;
        public float tiltFocus = 0.5f;
        public float grain = 0.035f;
        public float vignette = 0.45f;
        public float chromatic = 0.012f;
        public float envIntensity = 0.5f;

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

        /// <summary>
        /// Unity's camera looks from -Z (the preview's +Z): converts the target from preview (right-handed) space.
        /// </summary>
        public Vector3 Target => new Vector3(camTarget[0], camTarget[1], -camTarget[2]);
    }
}
