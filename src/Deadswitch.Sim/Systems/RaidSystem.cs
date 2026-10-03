using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Raid lifecycle (SPEC-001): spawn -> warning with the AI's estimate -> resolution against defense,
    /// loot and casualties scaled by the breach, loss ledger, mercy window; gates and the AI's gate report
    /// (SPEC-004). Exactly four RNG draws per tick.
    /// </summary>
    public static class RaidSystem
    {
        public static void StartOfTick(SimContext ctx)
        {
            if (ctx.State.Tick % SimConfig.TicksPerDay == 0)
            {
                ctx.State.RaidsToday = 0;
            }
        }

        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;

            // Fixed draw count per tick so no branch can shift the stream.
            bool spawnRoll = s.Rng.NextBelow((uint)c.Raid.MeanIntervalTicks) == 0;
            int variance = (int)s.Rng.NextBelow((uint)((2 * c.Raid.VariancePct) + 1)) - c.Raid.VariancePct;
            int missRoll = (int)s.Rng.NextBelow(100);
            int estimateRoll = (int)s.Rng.NextBelow(2001) - 1000;

            if (s.RaidId != 0)
            {
                if (s.Tick >= s.RaidArriveTick)
                {
                    Resolve(ctx, variance, missRoll);
                }

                return;
            }

            if (spawnRoll && s.RaidsToday < c.Raid.MaxPerDay && s.Tick >= s.MercyUntilTick)
            {
                Spawn(ctx, estimateRoll, missRoll);
            }
        }

        /// <summary>Ends the incoming raid without a fight (OVERRIDE lockdown).</summary>
        public static void Lockdown(SimContext ctx)
        {
            GameState s = ctx.State;
            ctx.Emit(EventKind.RaidResolved, s.RaidId, (int)RaidOutcome.Lockdown, s.RaidStrength, 0);
            ClearIncoming(s);
        }

        private static void Spawn(SimContext ctx, int estimateRoll, int gateRoll)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            s.RaidId = s.NextRaidId++;
            s.RaidsToday++;
            s.RaidArriveTick = s.Tick + c.Raid.WarningMinutes;
            s.RaidStrength = Defense.BaseRaidStrength(s, c);

            int band = (int)CorruptionSystem.Band(c, s.CorruptionMilli);
            int errorPct = c.Raid.EstimateErrorPctByBand[band];
            long estimate = (long)s.RaidStrength * ((100_000L + (errorPct * estimateRoll)) / 1000) / 100;
            s.RaidEstimate = (int)(estimate < 1 ? 1 : estimate);

            ctx.Emit(EventKind.RaidWarning, s.RaidId, (int)(s.RaidArriveTick - s.Tick), s.RaidEstimate);
            ReportGate(ctx, gateRoll);
            AiSystem.OnRaidWarning(ctx);
        }

        /// <summary>
        /// SPEC-004 rules 5-6: the raid's true gate comes from the spawn tick's otherwise unused miss draw; the AI
        /// reports it, except for the first warning of a run and for Boldness-scaled lies decided by a hash.
        /// </summary>
        private static void ReportGate(SimContext ctx, int gateRoll)
        {
            GameState s = ctx.State;
            var ai = ctx.Config.Ai;
            s.RaidGate = (RaidGate)(1 + (gateRoll % 4));
            bool lie;
            if (s.RaidId == 1 && ai.FirstLie)
            {
                lie = true;
            }
            else
            {
                long chance = (long)ai.LieChancePermilleAtFullBoldness * s.BoldnessMilli / 100_000;
                lie = SimMath.Hash((uint)s.RaidId ^ (uint)s.Rng.Inc, (uint)s.Tick) % 1000 < chance;
            }

            s.RaidGateReported = lie ? Opposite(s.RaidGate) : s.RaidGate;
            ctx.Emit(EventKind.RaidVector, s.RaidId, (int)s.RaidGateReported);
            if (lie)
            {
                s.LiesTold++;
                ctx.Emit(EventKind.AdvisorLied, (int)LieKind.RaidGate, s.RaidId, (int)s.RaidGate, (int)s.RaidGateReported);
            }
        }

        private static RaidGate Opposite(RaidGate g)
        {
            switch (g)
            {
                case RaidGate.North:
                    return RaidGate.South;
                case RaidGate.South:
                    return RaidGate.North;
                case RaidGate.East:
                    return RaidGate.West;
                default:
                    return RaidGate.East;
            }
        }

        private static void Resolve(SimContext ctx, int variance, int missRoll)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            int id = s.RaidId;

            int strength = SimMath.PctFloor(s.RaidStrength, 100 + variance);
            if (Defense.Unprepared(s))
            {
                strength = SimMath.PctFloor(strength, c.Defense.OfflineUnpreparedPct);
            }

            int defense = Defense.Rating(s, c);
            ctx.Emit(EventKind.RaidContact, id, (int)s.RaidGate);

            if (s.Posture == Posture.Dark && missRoll < c.Defense.DarkMissPct)
            {
                ctx.Emit(EventKind.RaidResolved, id, (int)RaidOutcome.Missed, strength, defense);
                ClearIncoming(s);
                return;
            }

            if (defense >= strength || strength <= 0)
            {
                ctx.Emit(EventKind.RaidResolved, id, (int)RaidOutcome.Repelled, strength, defense);
                ClearIncoming(s);
                return;
            }

            // Breach share in permille: how much of the raid got through.
            int breach = (int)(((long)(strength - defense) * 1000) / strength);
            int lootPct = s.Posture == Posture.Evacuate ? c.Defense.EvacuateLootPct : 100;

            int energy = Loot(s.Energy, c.Raid.LootPctOfEnergy, breach, lootPct, c.Raid.LootCap);
            int compute = Loot(s.Compute, c.Raid.ComputeLootPct, breach, lootPct, c.Raid.ComputeLootCap);
            int casualties = s.Posture == Posture.Evacuate ? 0 : (int)((long)s.Garrison * c.Defense.CasualtyPct * breach / 100_000);
            int populationBefore = s.People;

            s.Energy -= energy;
            s.Compute -= compute;
            s.People -= casualties;
            s.Garrison -= casualties;

            ctx.Emit(EventKind.RaidResolved, id, (int)RaidOutcome.Breached, strength, defense);
            if (energy > 0)
            {
                ctx.Emit(EventKind.LossLine, id, (int)LossResource.Energy, energy);
            }

            if (compute > 0)
            {
                ctx.Emit(EventKind.LossLine, id, (int)LossResource.Compute, compute);
            }

            if (casualties > 0)
            {
                ctx.Emit(EventKind.LossLine, id, (int)LossResource.People, casualties);
            }

            if (populationBefore > 0 && (long)casualties * 100 > (long)populationBefore * c.Raid.DevastatingPopLossPct)
            {
                s.MercyUntilTick = s.Tick + (c.Raid.MercyHours * SimConfig.TicksPerHour);
                ctx.Emit(EventKind.MercyStarted, id, (int)(s.MercyUntilTick - s.Tick));
            }

            ClearIncoming(s);
        }

        private static int Loot(int stock, int pct, int breachPermille, int multiplierPct, int cap)
        {
            long amount = (long)stock * pct * breachPermille * multiplierPct / (100L * 1000 * 100);
            if (amount > cap)
            {
                amount = cap;
            }

            return (int)(amount > stock ? stock : amount);
        }

        private static void ClearIncoming(GameState s)
        {
            s.RaidId = 0;
            s.RaidArriveTick = 0;
            s.RaidStrength = 0;
            s.RaidEstimate = 0;
            s.RaidGate = RaidGate.None;
            s.RaidGateReported = RaidGate.None;
        }
    }
}
