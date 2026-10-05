using UnityEngine;

namespace Deadswitch.Game.Cosmetics
{
    /// <summary>
    /// Cosmetics earned on this device (HUD themes, AI voices, codex logs). Everything in the game is free
    /// (owner, 2026-10-05: no purchases, no ads); cosmetics come from the reward track only.
    /// </summary>
    public static class Unlocks
    {
        private const string Key = "ds.cosmetic.";

        public static bool Owns(string id)
        {
            return PlayerPrefs.GetInt(Key + id, 0) == 1;
        }

        public static void Grant(string id)
        {
            PlayerPrefs.SetInt(Key + id, 1);
            PlayerPrefs.Save();
        }
    }
}
