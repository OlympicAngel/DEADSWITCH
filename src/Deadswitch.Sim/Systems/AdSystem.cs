using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>What a rewarded ad may grant (doc 10 s1.1, ADR-0006). Stored in commands: never renumber.</summary>
    public enum AdGrant
    {
        /// <summary>One extra salvage roll per day: a little energy and fuel.</summary>
        SalvageRoll = 0,

        /// <summary>A small idle-cap extension: more energy storage for a while.</summary>
        IdleCap = 1,
    }

    /// <summary>
    /// Rewarded-ad convenience grants as sim commands (deterministic and replayable; the ad itself is the host's
    /// business). Tier 1 only (the free demo), never while an attack, purge, ultimatum or climax is under way.
    /// </summary>
    public static class AdSystem
    {
        /// <summary>The reason a grant cannot be claimed now, or None.</summary>
        public static RejectReason Available(GameState s, AdGrant grant)
        {
            if (s.Tier > 1)
            {
                return RejectReason.Locked;
            }

            if (s.RaidId != 0 || s.PurgeStage != PurgeStage.None || s.Ultimatum == UltimatumStage.Issued || s.ClimaxAtTick != 0)
            {
                return RejectReason.ThreatActive;
            }

            if (grant == AdGrant.SalvageRoll && s.AdSalvageDay == (int)(s.Tick / SimConfig.TicksPerDay) + 1)
            {
                return RejectReason.OnCooldown;
            }

            if (grant == AdGrant.IdleCap && s.Tick < s.AdCapUntilTick)
            {
                return RejectReason.OnCooldown;
            }

            return RejectReason.None;
        }

        public static CommandResult Claim(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            AdConfig a = ctx.Config.Ads;
            if (cmd.A < 0 || cmd.A > (int)AdGrant.IdleCap || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            var grant = (AdGrant)cmd.A;
            RejectReason why = Available(s, grant);
            if (why != RejectReason.None)
            {
                return CommandResult.Reject(why);
            }

            if (grant == AdGrant.SalvageRoll)
            {
                s.AdSalvageDay = (int)(s.Tick / SimConfig.TicksPerDay) + 1;
                s.Energy = System.Math.Max(s.Energy, System.Math.Min(Economy.EnergyCap(s, ctx.Config), s.Energy + a.SalvageEnergy));
                s.Fuel = System.Math.Max(s.Fuel, System.Math.Min(Economy.FuelCap(s, ctx.Config), s.Fuel + a.SalvageFuel));
            }
            else
            {
                s.AdCapUntilTick = s.Tick + ((long)a.CapHours * SimConfig.TicksPerHour);
            }

            ctx.Emit(EventKind.AdGranted, cmd.A);
            return CommandResult.Ok;
        }
    }
}
