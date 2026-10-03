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

            // SPEC-009: the opening raid comes on cue; nothing else before the protection window ends.
            bool opening = c.Opening.Enabled && c.Raid.MaxPerDay > 0 && s.NextRaidId == 1 && s.Tick == c.Opening.RaidAtMinute;
            bool protectedNow = c.Opening.Enabled && s.Tick < (long)c.Opening.ProtectionHours * SimConfig.TicksPerHour;
            if (opening || (spawnRoll && !protectedNow && s.RaidsToday < c.Raid.MaxPerDay && s.Tick >= s.MercyUntilTick))
            {
                Spawn(ctx, estimateRoll, missRoll, opening ? c.Opening.RaidStrength : 0, c.Raid.WarningMinutes);
            }
        }

        /// <summary>Ends the incoming raid without a fight (OVERRIDE lockdown).</summary>
        public static void Lockdown(SimContext ctx)
        {
            GameState s = ctx.State;
            ctx.Emit(EventKind.RaidResolved, s.RaidId, (int)RaidOutcome.Lockdown, s.RaidStrength, 0);
            Record(ctx, s.RaidId, 0);
            ClearIncoming(s);
        }

        /// <summary>
        /// Betrayal (SPEC-011): the AI lets a raid in at once at the given strength, with turrets offline against
        /// it. An incoming raid is the one it lets in. Uses no RNG draws. Returns the raid id.
        /// </summary>
        public static int Betrayal(SimContext ctx, int strengthPct)
        {
            GameState s = ctx.State;
            if (s.RaidId == 0)
            {
                Spawn(ctx, 0, (int)(SimMath.Hash((uint)s.Tick, (uint)s.NextRaidId) % 4), 0, 1);
            }

            s.RaidStrength = SimMath.PctFloor(s.RaidStrength, strengthPct);
            s.RaidArriveTick = s.Tick + 1;
            s.BetrayalRaidId = s.RaidId;
            return s.RaidId;
        }

        private static void Spawn(SimContext ctx, int estimateRoll, int gateRoll, int fixedStrength, int warningMinutes)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            s.RaidId = s.NextRaidId++;
            s.RaidsToday++;
            s.RaidArriveTick = s.Tick + warningMinutes;
            s.RaidStrength = fixedStrength > 0 ? fixedStrength : Defense.BaseRaidStrength(s, c);

            int band = (int)CorruptionSystem.Band(c, s.CorruptionMilli);
            int errorPct = c.Raid.EstimateErrorPctByBand[band];
            long estimate = (long)s.RaidStrength * ((100_000L + (errorPct * estimateRoll)) / 1000) / 100;
            s.RaidEstimate = (int)(estimate < 1 ? 1 : estimate);
            if (ClimaxSystem.Silenced(s))
            {
                // A silenced AI predicts nothing (SPEC-011 rule 3).
                s.RaidEstimate = 0;
            }

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
            if (ClimaxSystem.Silenced(s))
            {
                s.RaidGateReported = RaidGate.None;
                ctx.Emit(EventKind.RaidVector, s.RaidId, (int)RaidGate.None);
                return;
            }

            bool lie;
            if (s.RaidId == ai.FirstLieRaid)
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

            int defense = id == s.BetrayalRaidId ? Defense.Rating(s, c, s.Posture, s.Garrison, false) : Defense.Rating(s, c);
            ctx.Emit(EventKind.RaidContact, id, (int)s.RaidGate);
            int lies = s.RaidGate != s.RaidGateReported ? RaidRecord.GateLie : 0;

            if (s.Posture == Posture.Dark && missRoll < c.Defense.DarkMissPct)
            {
                ctx.Emit(EventKind.RaidResolved, id, (int)RaidOutcome.Missed, strength, defense);
                Record(ctx, id, lies);
                ClearIncoming(s);
                return;
            }

            if (defense >= strength || strength <= 0)
            {
                ctx.Emit(EventKind.RaidResolved, id, (int)RaidOutcome.Repelled, strength, defense);
                Record(ctx, id, lies);
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

            // SPEC-006 rule 4: a corrupted AI may understate the breach in its summary (the ledger stays true).
            if (energy > 0 && CorruptionSystem.Band(c, s.CorruptionMilli) >= CorruptionBand.Unstable
                && SimMath.Hash((uint)id ^ (uint)s.Rng.Inc, (uint)s.Tick + 7u) % 100 < (uint)c.Report.EditChancePct)
            {
                s.LiesTold++;
                ctx.Emit(EventKind.AdvisorLied, (int)LieKind.ReportEdit, id, energy, SimMath.PctFloor(energy, c.Report.EditShownPct));
                lies |= RaidRecord.SummaryEdit;
            }

            Record(ctx, id, lies);
            ClearIncoming(s);
        }

        /// <summary>Keeps the report record of a resolved raid; only the most recent ones stay verifiable.</summary>
        private static void Record(SimContext ctx, int raidId, int lies)
        {
            System.Collections.Generic.List<RaidRecord> records = ctx.State.RaidRecords;
            records.Add(new RaidRecord { RaidId = raidId, LieFlags = lies });
            while (records.Count > ctx.Config.Report.KeepRaids)
            {
                records.RemoveAt(0);
            }
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
            s.BetrayalRaidId = 0;
        }
    }
}
