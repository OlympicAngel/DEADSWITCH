using UnityEngine;

namespace Deadswitch.Game.UI
{
    /// <summary>Tactile feedback for confirmations and alerts, gated by the haptics setting (doc 08 s6).</summary>
    public static class Feedback
    {
        public static bool Enabled { get; set; } = true;

        /// <summary>Alert pulse (raid warnings). Uses the platform vibrator when available.</summary>
        public static void Alert()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Enabled)
            {
                Handheld.Vibrate();
            }
#endif
        }
    }
}
