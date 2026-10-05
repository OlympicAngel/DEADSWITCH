using Deadswitch.Sim.State;
using UnityEngine;

namespace Deadswitch.Game.UI
{
    /// <summary>
    /// Tactile feedback for confirmations and alerts, gated by the haptics setting (doc 08 s6). Attack warnings have
    /// their own patterns (doc 10 s4 alert table): raid a fast triple pulse, siege a slow heavy rumble, purge a
    /// continuous alarm. A virus is silent: it shows only as a screen glitch. Android plays the patterns; iOS has a
    /// single system pulse without a native plugin.
    /// </summary>
    public static class Feedback
    {
        private static readonly long[] Pulse = { 0, 120 };
        private static readonly long[] Triple = { 0, 80, 90, 80, 90, 80 };
        private static readonly long[] Rumble = { 0, 650, 260, 650 };
        private static readonly long[] Alarm = { 0, 900, 140, 900, 140, 900, 140, 900 };

        public static bool Enabled { get; set; } = true;

        /// <summary>Alert pulse (ultimatums, dilemmas, battle contact).</summary>
        public static void Alert()
        {
            Play(Pulse);
        }

        /// <summary>The warning's own pattern for an attack signature (doc 10 s4).</summary>
        public static void Signature(AttackKind kind)
        {
            switch (kind)
            {
                case AttackKind.Siege:
                    Play(Rumble);
                    break;
                case AttackKind.Purge:
                    Play(Alarm);
                    break;
                default:
                    Play(Triple);
                    break;
            }
        }

        private static void Play(long[] pattern)
        {
            if (!Enabled)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    vibrator.Call("vibrate", pattern, -1);
                }
            }
            catch (System.Exception)
            {
                Handheld.Vibrate();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
