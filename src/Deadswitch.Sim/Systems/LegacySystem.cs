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
        public const int MasteryCount = 8;

        public static int Veterans(GameState s, SimConfig c)
        {
            LegacyConfig l = c.Legacy;
            return System.Math.Max(0, l.BaseVeterans + s.Perks[(int)Perk.Veterans] + (s.People / l.VeteranPerPeople));
        }

        /// <summary>Mastery earned in this cycle (each challenge scores once per cycle, so it cannot be farmed).</summary>
        public static int MasteryDone(GameState s)
        {
            int n = 0;
            for (int i = 0; i < MasteryCount; i++)
            {
                n += (s.CycleMastery & (1 << i)) != 0 ? 1 : 0;
            }

            return n;
        }

        public static int HighestTier(GameState s)
        {
            return System.Math.Max(s.HighestTier, s.Tier);
        }

        /// <summary>Legacy score of this cycle so far (doc 10 s6).</summary>
        public static int Score(GameState s, SimConfig c)
        {
            LegacyConfig l = c.Legacy;
            int power = System.Math.Max(s.PeakPower, Defense.PowerRating(s));
            return (HighestTier(s) * l.ScorePerTier) + (power / l.PowerDivisor) + (Veterans(s, c) * l.ScorePerVeteran) + (MasteryDone(s) * l.ScorePerMastery);
        }

        public static int PerkPrice(GameState s, SimConfig c, Perk p)
        {
            return c.Legacy.PerkCost * (s.Perks[(int)p] + 1);
        }

        public static void Earn(SimContext ctx, Mastery m)
        {
            GameState s = ctx.State;
            int bit = 1 << (int)m;
            if ((s.CycleMastery & bit) != 0)
            {
                return;
            }

            s.CycleMastery |= bit;
            s.Mastery |= bit;
            ctx.Emit(EventKind.MasteryEarned, (int)m);
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            LegacyConfig l = c.Legacy;
            s.PeakPower = System.Math.Max(s.PeakPower, Defense.PowerRating(s));
            if (s.Region == Region.River)
            {
                // barges at the crossing (SPEC-031)
                s.Fuel = System.Math.Max(s.Fuel, System.Math.Min(Economy.FuelCap(s, c), s.Fuel + l.RiverFuelPerHour));
            }

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

            // weathered a collapse without a rollback or a forced reboot (doc 10 s6)
            if (s.CollapseWatchUntilTick > 0 && s.Tick >= s.CollapseWatchUntilTick)
            {
                s.CollapseWatchUntilTick = 0;
                Earn(ctx, Mastery.CollapseSurvived);
            }
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
            if (cmd.A < 0 || cmd.A > (int)Region.Ruins || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (HighestTier(s) < ctx.Config.Legacy.RelocateMinTier)
            {
                return CommandResult.Reject(RejectReason.Locked);
            }

            // no running from a threat already in motion: an attack, the purge ladder, the Warlord's terms, the climax
            if (s.RaidId != 0 || s.PurgeStage != PurgeStage.None || s.Ultimatum == UltimatumStage.Issued || s.ClimaxAtTick != 0)
            {
                return CommandResult.Reject(RejectReason.ThreatActive);
            }

            if (Defense.PowerRating(s) * 10 >= s.PeakPower * 9)
            {
                Earn(ctx, Mastery.RelocatePeak);
            }

            Reboot(ctx, RebootReason.Relocation, (Region)cmd.A);
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

        public static CommandResult SetIronman(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A < 0 || cmd.A > 1 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            bool on = cmd.A == 1;
            if (s.Ironman == on)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            // chosen at the start of a site, and never switched off once on (there is no way out of Ironman)
            if (!on || s.Tick - s.CycleStartTick >= (long)ctx.Config.Legacy.IronmanChooseHours * SimConfig.TicksPerHour)
            {
                return CommandResult.Reject(RejectReason.Locked);
            }

            s.Ironman = on;
            if (on)
            {
                // no shield in Ironman: an armed one drops and its charge comes back
                if (s.ShieldUntilTick > s.Tick)
                {
                    s.ShieldCharges = System.Math.Min(ctx.Config.Threats.ShieldMaxCharges, s.ShieldCharges + 1);
                }

                s.ShieldFromTick = 0;
                s.ShieldUntilTick = 0;
            }

            ctx.Emit(EventKind.IronmanSet, cmd.A);
            return CommandResult.Ok;
        }

        /// <summary>Ends the cycle: legacy points, then a fresh site with what the core carries (doc 10 s1.2).</summary>
        public static void Reboot(SimContext ctx, RebootReason reason)
        {
            // a lost Hub flees wherever it can (SPEC-031): the region is not the handler's choice
            uint h = SimMath.Hash((uint)ctx.State.Tick ^ 0x2E61u, (uint)(ctx.State.Rng.State >> 32));
            Reboot(ctx, reason, (Region)(h % 4));
        }

        /// <summary>Ends the cycle and settles the core at <paramref name="region"/>.</summary>
        public static void Reboot(SimContext ctx, RebootReason reason, Region region)
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

            // Ironman (doc 10 s1.2): losing the core ends the run; only the recorded legacy and its bonus survive
            bool ironmanEnd = s.Ironman && forced;
            if (ironmanEnd)
            {
                perks = new int[PerkCount];
                kept = 0;
                veterans = 0;
                for (int f = 0; f < heat.Length; f++)
                {
                    heat[f] = 0;
                }
            }

            bool ironman = s.Ironman;
            int corruption = forced && !ironmanEnd ? SimMath.PctFloor(s.CorruptionMilli, l.ForcedCorruptionKeptPct) : 0;
            int coldness = s.ColdnessMilli;
            int boldness = s.BoldnessMilli;
            DelegationLevel delegation = s.Delegation;

            // the handler, the day and the slow clocks do not reset with the site
            bool away = s.Away;
            bool tribute = s.TributeOrder;
            int raidsToday = s.RaidsToday;
            int[] tradesToday = (int[])s.TradesToday.Clone();
            int shieldCharges = s.ShieldCharges;
            long shieldFrom = s.ShieldFromTick;
            long shieldUntil = s.ShieldUntilTick;
            long shieldNext = s.ShieldNextChargeTick;
            int charges = s.OverrideCharges;
            long overrideNext = s.OverrideNextChargeTick;
            long overrideCooldown = s.OverrideCooldownUntil;
            int overridePenalty = s.OverrideMaxPenalty;
            long auditReady = s.AuditReadyTick;
            long surgeReady = s.SurgeReadyTick;
            int lies = s.LiesTold;
            int fragments = s.Fragments;

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
            s.Ironman = ironman;
            s.Away = away;
            s.TributeOrder = tribute;
            s.RaidsToday = raidsToday;
            s.TradesToday = tradesToday;
            s.ShieldCharges = ironman ? 0 : shieldCharges;
            s.ShieldFromTick = ironman ? 0 : shieldFrom;
            s.ShieldUntilTick = ironman ? 0 : shieldUntil;
            s.ShieldNextChargeTick = shieldNext;
            s.OverrideMaxPenalty = overridePenalty;
            s.OverrideCharges = System.Math.Min(charges, OverrideSystem.MaxCharges(s, c));
            s.OverrideNextChargeTick = overrideNext;
            s.OverrideCooldownUntil = overrideCooldown;
            s.AuditReadyTick = auditReady;
            s.SurgeReadyTick = surgeReady;
            s.LiesTold = lies;
            s.Fragments = fragments;
            s.Region = region;
            s.RebuildingSurge = forced && !ironmanEnd;

            // first-time schedules in a fresh state count from tick 0: start them from the new site
            s.NextDilemmaTick = tick + ((long)c.Living.DilemmaFirstHour * SimConfig.TicksPerHour);
            s.NextWorldEventTick = tick + ((long)c.Living.WorldEventFirstDay * SimConfig.TicksPerDay);
            s.Energy += perks[(int)Perk.StartResources] * l.PerkStartEnergy;
            s.Fuel = System.Math.Min(Economy.FuelCap(s, c), s.Fuel + (perks[(int)Perk.StartResources] * l.PerkStartFuel));
            s.People = System.Math.Min(Economy.PopulationCap(s, c), s.People + veterans);
            s.Energy = System.Math.Min(Economy.EnergyCap(s, c), s.Energy);

            // a fair start at the new site: the mercy window covers the first hours
            s.MercyUntilTick = tick + ((long)c.Raid.MercyHours * SimConfig.TicksPerHour);

            ctx.Emit(EventKind.CycleEnded, (int)reason, score, points, veterans);
            ctx.Emit(EventKind.RegionSettled, (int)region);
        }
    }
}
