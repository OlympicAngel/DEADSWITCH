using UnityEngine;

namespace Deadswitch.Game.Core
{
    /// <summary>
    /// Player comfort and accessibility settings (doc 08 s6, doc 10: colorblind-safe means shape + color, always on).
    /// Persisted in PlayerPrefs; read by every presentation system.
    /// </summary>
    public sealed class GameSettings
    {
        private const string KeyEffects = "ds.effects_pct";
        private const string KeyReducedMotion = "ds.reduced_motion";
        private const string KeyHaptics = "ds.haptics";
        private const string KeyTimeScale = "ds.dev_time_scale";
        private const string KeyTextScale = "ds.text_scale_pct";

        /// <summary>Allowed text sizes (doc 10 assists: scalable text and touch targets).</summary>
        public static readonly int[] TextScales = { 100, 115, 130 };

        /// <summary>0..100. Scales glitch, scanline, noise and shake effects. 0 removes them entirely.</summary>
        public int EffectIntensityPct { get; private set; } = 100;

        public bool ReducedMotion { get; private set; }

        public bool Haptics { get; private set; } = true;

        /// <summary>100, 115 or 130: scales type and touch targets.</summary>
        public int TextScalePct { get; private set; } = 100;

        /// <summary>Development only: game minutes per real minute. 1 in release builds.</summary>
        public float DevTimeScale { get; private set; } = 1f;

        public event System.Action Changed;

        public static GameSettings Load()
        {
            var s = new GameSettings
            {
                EffectIntensityPct = Mathf.Clamp(PlayerPrefs.GetInt(KeyEffects, 100), 0, 100),
                ReducedMotion = PlayerPrefs.GetInt(KeyReducedMotion, 0) == 1,
                Haptics = PlayerPrefs.GetInt(KeyHaptics, 1) == 1,
                TextScalePct = System.Array.IndexOf(TextScales, PlayerPrefs.GetInt(KeyTextScale, 100)) >= 0 ? PlayerPrefs.GetInt(KeyTextScale, 100) : 100,
                DevTimeScale = Debug.isDebugBuild ? Mathf.Clamp(PlayerPrefs.GetFloat(KeyTimeScale, 1f), 1f, 600f) : 1f,
            };
            return s;
        }

        /// <summary>Effect strength 0..1 after the intensity setting.</summary>
        public float Effects => EffectIntensityPct / 100f;

        public void SetEffectIntensity(int pct)
        {
            EffectIntensityPct = Mathf.Clamp(pct, 0, 100);
            PlayerPrefs.SetInt(KeyEffects, EffectIntensityPct);
            Save();
        }

        public void SetReducedMotion(bool on)
        {
            ReducedMotion = on;
            PlayerPrefs.SetInt(KeyReducedMotion, on ? 1 : 0);
            Save();
        }

        public void SetHaptics(bool on)
        {
            Haptics = on;
            PlayerPrefs.SetInt(KeyHaptics, on ? 1 : 0);
            Save();
        }

        public void SetTextScale(int pct)
        {
            TextScalePct = System.Array.IndexOf(TextScales, pct) >= 0 ? pct : 100;
            PlayerPrefs.SetInt(KeyTextScale, TextScalePct);
            Save();
        }

        public void SetDevTimeScale(float scale)
        {
            DevTimeScale = Debug.isDebugBuild ? Mathf.Clamp(scale, 1f, 600f) : 1f;
            PlayerPrefs.SetFloat(KeyTimeScale, DevTimeScale);
            Save();
        }

        private void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
