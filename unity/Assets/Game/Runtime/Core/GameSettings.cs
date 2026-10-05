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
        private const string KeySound = "ds.sound_pct";
        private const string KeyMusic = "ds.music";
        private const string KeyVoice = "ds.voice_pack";
        private const string KeyCinematics = "ds.cinematics";
        private const string KeyFlyIn = "ds.focus_fly_in";

        /// <summary>AI voice packs (F-048, cosmetic): 0 standard, then the season rewards in this order.</summary>
        public static readonly string[] VoicePacks = { "standard", "voice-low", "voice-static" };

        /// <summary>Sound volume steps (F-035).</summary>
        public static readonly int[] SoundSteps = { 0, 25, 50, 75, 100 };

        /// <summary>Allowed text sizes (doc 10 assists: scalable text and touch targets).</summary>
        public static readonly int[] TextScales = { 100, 115, 130 };

        /// <summary>0..100. Scales glitch, scanline, noise and shake effects. 0 removes them entirely.</summary>
        public int EffectIntensityPct { get; private set; } = 100;

        public bool ReducedMotion { get; private set; }

        public bool Haptics { get; private set; } = true;

        /// <summary>100, 115 or 130: scales type and touch targets.</summary>
        public int TextScalePct { get; private set; } = 100;

        /// <summary>Master volume 0..100 (F-035).</summary>
        public int SoundPct { get; private set; } = 75;

        /// <summary>Tension music on or off; alarms and the AI's voice stay.</summary>
        public bool Music { get; private set; } = true;

        /// <summary>Index into <see cref="VoicePacks"/>; only owned packs can be chosen.</summary>
        public int VoicePack { get; private set; }

        /// <summary>Attack cinematics (SPEC-039 D): letterboxed shots when an attack makes contact. Off under reduced motion.</summary>
        public bool Cinematics { get; private set; } = true;

        /// <summary>The drone flies in and frames a selected facility (SPEC-039 C).</summary>
        public bool FocusFlyIn { get; private set; } = true;

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
                SoundPct = Mathf.Clamp(PlayerPrefs.GetInt(KeySound, 75), 0, 100),
                Music = PlayerPrefs.GetInt(KeyMusic, 1) == 1,
                VoicePack = Mathf.Clamp(PlayerPrefs.GetInt(KeyVoice, 0), 0, VoicePacks.Length - 1),
                Cinematics = PlayerPrefs.GetInt(KeyCinematics, 1) == 1,
                FocusFlyIn = PlayerPrefs.GetInt(KeyFlyIn, 1) == 1,
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

        public void SetSound(int pct)
        {
            SoundPct = Mathf.Clamp(pct, 0, 100);
            PlayerPrefs.SetInt(KeySound, SoundPct);
            Save();
        }

        public void SetMusic(bool on)
        {
            Music = on;
            PlayerPrefs.SetInt(KeyMusic, on ? 1 : 0);
            Save();
        }

        public void SetVoicePack(int pack)
        {
            VoicePack = Mathf.Clamp(pack, 0, VoicePacks.Length - 1);
            PlayerPrefs.SetInt(KeyVoice, VoicePack);
            Save();
        }

        public void SetCinematics(bool on)
        {
            Cinematics = on;
            PlayerPrefs.SetInt(KeyCinematics, on ? 1 : 0);
            Save();
        }

        public void SetFocusFlyIn(bool on)
        {
            FocusFlyIn = on;
            PlayerPrefs.SetInt(KeyFlyIn, on ? 1 : 0);
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
