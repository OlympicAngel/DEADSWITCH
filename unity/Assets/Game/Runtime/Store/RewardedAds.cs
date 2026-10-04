using Deadswitch.Game.Core;
using Deadswitch.Sim.State;

namespace Deadswitch.Game.Store
{
    /// <summary>
    /// The only things an ad may grant (ADR-0006, doc 10 s1.1): convenience and cosmetics. Never safety, defense,
    /// timer skips or combat power. Anything not listed here cannot be granted: the type is the whitelist.
    /// </summary>
    public enum ConvenienceGrant
    {
        /// <summary>Cold-blue HUD accent (cosmetic).</summary>
        ThemeCold = 1,

        /// <summary>Bone-white HUD accent (cosmetic).</summary>
        ThemeBone = 2,
    }

    /// <summary>
    /// Optional rewarded ads (free demo). No ad network is linked in this build: development builds simulate a
    /// completed view. Ads are never offered while an attack countdown, battle, purge or ultimatum is active.
    /// </summary>
    public static class RewardedAds
    {
        /// <summary>Why an ad cannot be offered now, or null when it can.</summary>
        public static string Blocked()
        {
            GameState s = GameHost.Instance.Sim.State;
            if (s.RaidId != 0 || s.PurgeStage != PurgeStage.None || s.Ultimatum == UltimatumStage.Issued || s.ClimaxAtTick != 0)
            {
                return "Not while the Hub is under threat.";
            }

            return Entitlements.StoreAvailable ? null : "Ads are not available in this build.";
        }

        /// <summary>Shows an ad and grants the whitelisted reward when it completes. Returns a player-facing line.</summary>
        public static string Watch(ConvenienceGrant grant)
        {
            string blocked = Blocked();
            if (blocked != null)
            {
                return blocked;
            }

            Entitlements.Instance.GrantCosmetic(Id(grant));
            return "Thanks for watching. " + Name(grant) + " unlocked.";
        }

        public static string Id(ConvenienceGrant grant)
        {
            return grant == ConvenienceGrant.ThemeCold ? "theme-cold" : "theme-bone";
        }

        public static string Name(ConvenienceGrant grant)
        {
            return grant == ConvenienceGrant.ThemeCold ? "COLD SIGNAL THEME" : "BONE THEME";
        }
    }
}
