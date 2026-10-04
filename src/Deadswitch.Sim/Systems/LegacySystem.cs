using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.Persistence;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// The cycle (SPEC-022, doc 06 s4, doc 10 s1.2 + s6): a reboot never happens randomly. The handler relocates by
    /// choice at a peak (100% of the legacy score as points), or the Hub is lost to failure (hub destroyed after
    /// ignored purge warnings, a total AI takeover, a population collapse) for 50%. The portable core carries legacy
    /// modules, faction scars, a few veterans and perks into a fresh site. No RNG draws.
    /// </summary>
    public static class LegacySystem
    {
        public const int PerkCount = 5;
        public const int MasteryCount = 7;

        public static int Veterans(GameState s, SimConfig c)
        {
            LegacyConfig l = c.Legacy;
            return System.Math.Max(0, l.BaseVeterans + s.Perks[(int)Perk.Veterans] + (s.People / l.VeteranPerPeople));
        }

        public static int MasteryDone(GameState s)
        {
            int n = 0;
            for (int i = 0; i < MasteryCount; i++)
            {
                n += (s.Mastery & (1 << i)) != 0 ? 1 : 0;
            }

            return n;
        }

        /// <summary>Legacy score of this cycle so far (doc 10 s6).</summary>
        public static int Score(GameState s, SimConfig c)
        {
            LegacyConfig l = c.Legacy;
            return (s.HighestTier * l.ScorePerTier) + (s.PeakPower / l.PowerDivisor) + (Veterans(s, c) * l.ScorePerVeteran) + (MasteryDone(s) * l.ScorePerMastery);
        }

        public static int PerkPrice(GameState s, SimConfig c, Perk p)
        {
            return c.Legacy.PerkCost * (s.Perks[(int)p] + 1);
        }

        public static void Earn(SimContext ctx, Mastery m)
        {
            GameState s = ctx.State;
            int bit = 1 << (int)m;
            if ((s.Mastery & bit) != 0)
            {
                return;
            }

            s.Mastery |= bit;
            ctx.Emit(EventKind.MasteryEarned, (int)m);
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            LegacyConfig l = c.Legacy;
            s.PeakPower = System.Math.Max(s.PeakPower, Defense.PowerRating(s));
            if (s.Tier > s.HighestTier)
            {
                // a tier completed: tier mastery checks
                if (s.HighestTier >= 1)
                {
                    if (s.TierManual)
                    {
                        Earn(ctx, Mastery.ManualTier);
                    }

                    if (s.TierMaxCorruption < l.MasteryCorruptionMilli)
                    {
                        Earn(ctx, Mastery.CleanCore);
                    }

                    if (s.Tier >= 2 && s.OutpostsLostThisCycle == 0)
                    {
                        Earn(ctx, Mastery.NoOutpostLost);
                    }
                }

                s.HighestTier = s.Tier;
                s.TierManual = true;
                s.TierMaxCorruption = 0;
            }

            s.TierManual &= s.Delegation == DelegationLevel.Manual;
            s.TierMaxCorruption = System.Math.Max(s.TierMaxCorruption, s.CorruptionMilli);

            // forced reboot triggers (doc 06 s4): never random, always a failure the handler could see coming
            s.CollapseHours = s.People <= c.PeopleChoices.MinPeople ? s.CollapseHours + 1 : 0;
            s.CriticalHours = CorruptionSystem.Band(c, s.CorruptionMilli) == CorruptionBand.Critical ? s.CriticalHours + 1 : 0;
            if (s.RaidId != 0)
            {
                return;
            }

            if (s.CollapseHours >= l.CollapseHours)
            {
                Reboot(ctx, RebootReason.PopulationCollapse);
            }
            else if (s.CriticalHours >= l.TakeoverCriticalHours)
            {
                Reboot(ctx, RebootReason.AiTakeover);
            }
            else if (s.HubFallen)
            {
                Reboot(ctx, RebootReason.HubDestroyed);
            }
        }

        public static CommandResult Relocate(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.HighestTier < ctx.Config.Legacy.RelocateMinTier)
            {
                return CommandResult.Reject(RejectReason.Locked);
            }

            if (s.RaidId != 0)
            {
                return CommandResult.Reject(RejectReason.ThreatActive);
            }

            if (Defense.PowerRating(s) * 10 >= s.PeakPower * 9)
            {
                Earn(ctx, Mastery.RelocatePeak);
            }

            Reboot(ctx, RebootReason.Relocation);
            return CommandResult.Ok;
        }

        public static CommandResult BuyPerk(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A < 0 || cmd.A >= PerkCount || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            var p = (Perk)cmd.A;
            if (s.Perks[cmd.A] >= ctx.Config.Legacy.PerkMaxLevel)
            {
                return CommandResult.Reject(RejectReason.MaxLevel);
            }

            int price = PerkPrice(s, ctx.Config, p);
            if (s.LegacyPoints < price)
            {
                return CommandResult.Reject(RejectReason.NotEnoughLegacy);
            }

            s.LegacyPoints -= price;
            s.Perks[cmd.A]++;
            ctx.Emit(EventKind.PerkBought, cmd.A, s.Perks[cmd.A], price);
            return CommandResult.Ok;
        }

        /// <summary>Ends the cycle: legacy points, then a fresh site with what the core carries (doc 10 s1.2).</summary>
        public static void Reboot(SimContext ctx, RebootReason reason)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            LegacyConfig l = c.Legacy;
            bool forced = reason != RebootReason.Relocation;
            int score = Score(s, c);
            int points = SimMath.PctFloor(score, forced ? l.ForcedBonusPct : l.VoluntaryBonusPct);
            int veterans = Veterans(s, c);

            // what the portable core carries
            long tick = s.Tick;
            Rng.Pcg32 rng = s.Rng;
            int nextRaid = s.NextRaidId;
            int nextOp = s.NextOpId;
            int cycle = s.Cycle + 1;
            int legacyPoints = s.LegacyPoints + points;
            int legacyTotal = s.LegacyTotal + score;
            int[] perks = (int[])s.Perks.Clone();
            int mastery = s.Mastery;
            int[] heat = new int[s.Heat.Length];
            for (int f = 0; f < heat.Length; f++)
            {
                heat[f] = SimMath.PctFloor(s.Heat[f], l.HeatKeptPct);
            }

            ulong kept = 0;
            int keep = l.KeptModules;
            foreach (ModuleDef d in Modules.Catalog)
            {
                if (keep > 0 && d.Field != ModuleField.Trunk && Modules.IsRestored(s, d.Node))
                {
                    kept |= 1UL << (int)d.Node;
                    keep--;
                }
            }

            int corruption = forced ? SimMath.PctFloor(s.CorruptionMilli, l.ForcedCorruptionKeptPct) : 0;
            int coldness = s.ColdnessMilli;
            int boldness = s.BoldnessMilli;
            DelegationLevel delegation = s.Delegation;

            SaveGame.CopyInto(new GameState(0UL, c), s);

            s.Tick = tick;
            s.Rng = rng;
            s.NextRaidId = nextRaid;
            s.NextOpId = nextOp;
            s.Cycle = cycle;
            s.CycleStartTick = tick;
            s.LegacyPoints = legacyPoints;
            s.LegacyTotal = legacyTotal;
            s.Perks = perks;
            s.Mastery = mastery;
            s.Heat = heat;
            s.Modules |= kept;
            s.CorruptionMilli = corruption;
            s.ColdnessMilli = coldness;
            s.BoldnessMilli = boldness;
            s.Delegation = delegation;
            s.HighestTier = 1;
            s.TierManual = true;

            // absolute schedules in a fresh state count from tick 0: move them to now
            s.OverrideNextChargeTick += tick;
            s.ShieldNextChargeTick += tick;
            s.Energy += perks[(int)Perk.StartResources] * l.PerkStartEnergy;
            s.Fuel = System.Math.Min(c.Fuel.Cap, s.Fuel + (perks[(int)Perk.StartResources] * l.PerkStartFuel));
            s.People = System.Math.Min(Economy.PopulationCap(s, c), s.People + veterans);
            s.Energy = System.Math.Min(Economy.EnergyCap(s, c), s.Energy);
            s.OverrideCharges = OverrideSystem.MaxCharges(s, c);

            // a fair start at the new site: the mercy window covers the first hours
            s.MercyUntilTick = tick + ((long)c.Raid.MercyHours * SimConfig.TicksPerHour);

            ctx.Emit(EventKind.CycleEnded, (int)reason, score, points, veterans);
        }
    }
}
