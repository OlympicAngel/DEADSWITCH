using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Attack lifecycle (SPEC-001, SPEC-015): spawn -> warning with the AI's estimate -> resolution against
    /// defense, losses by signature (raid: loot; siege: building downgrades; purge: everything) scaled by the
    /// breach, loss ledger, mercy window; gates and the AI's gate report (SPEC-004). Exactly four RNG draws per tick.
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
            int interval = LivingSystem.Active(s, WorldEventKind.DeadWeek) ? SimMath.PctFloor(c.Raid.MeanIntervalTicks, 100 + c.Living.DeadWeekIntervalPct) : c.Raid.MeanIntervalTicks;
            bool spawnRoll = s.Rng.NextBelow((uint)System.Math.Max(1, interval)) == 0;
            int variance = (int)s.Rng.NextBelow((uint)((2 * c.Raid.VariancePct) + 1)) - c.Raid.VariancePct;
            int missRoll = (int)s.Rng.NextBelow(100);
            int estimateRoll = (int)s.Rng.NextBelow(2001) - 1000;

            if (s.RaidId != 0)
            {
                // a live battle (SPEC-020) holds resolution while the handler commands at the wall
                if (s.Tick >= s.RaidArriveTick && !BattleSystem.Holds(ctx))
                {
                    Resolve(ctx, variance, missRoll);
                }

                return;
            }

            // SPEC-009: the opening raid comes on cue; nothing else before the protection window ends.
            bool opening = c.Opening.Enabled && c.Raid.MaxPerDay > 0 && s.NextRaidId == 1 && s.Tick == c.Opening.RaidAtMinute;
            bool protectedNow = c.Opening.Enabled && s.Tick < (long)c.Opening.ProtectionHours * SimConfig.TicksPerHour;
            if ((opening && !ThreatSystem.Shielded(s)) || (spawnRoll && !protectedNow && !ThreatSystem.Shielded(s) && s.RaidsToday < MaxPerDay(s, c) && s.Tick >= s.MercyUntilTick))
            {
                if (!opening && ThreatSystem.SiegeDue(s))
                {
                    // a due siege takes the next attack slot (SPEC-015 rule 2)
                    s.NextSiegeTick = 0;
                    Spawn(ctx, estimateRoll, missRoll, 0, c.Threats.SiegeWarningMinutes, AttackKind.Siege);
                    return;
                }

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

        /// <summary>Attack cap per 24h at the current tier (doc 10 s4). Zero disables raids at every tier.</summary>
        public static int MaxPerDay(GameState s, SimConfig c)
        {
            int[] extra = c.Tier.ExtraRaidsPerDay;
            return c.Raid.MaxPerDay <= 0 ? 0 : c.Raid.MaxPerDay + extra[System.Math.Min(s.Tier, extra.Length) - 1];
        }

        /// <summary>The purge force moves in at the end of the warning ladder (SPEC-015 rule 4). Uses no RNG draws.</summary>
        public static void SpawnPurge(SimContext ctx)
        {
            GameState s = ctx.State;
            Spawn(ctx, 0, (int)(SimMath.Hash((uint)s.Tick, (uint)s.NextRaidId ^ 0x9A26u) % 4), 0, ctx.Config.Threats.PurgeStrikeWarningMinutes, AttackKind.Purge);
        }

        /// <summary>Rival swarm (SPEC-021): machines sense a Critical core and converge. Uses no RNG draws.</summary>
        public static void SpawnSwarm(SimContext ctx)
        {
            GameState s = ctx.State;
            Spawn(ctx, 0, (int)(SimMath.Hash((uint)s.Tick, (uint)s.NextRaidId ^ 0x5A44u) % 4), 0, ctx.Config.Raid.WarningMinutes);
            s.RaidStrength = SimMath.PctFloor(s.RaidStrength, ctx.Config.Glitch.SwarmStrengthPct);
            s.RaidEstimate = SimMath.PctFloor(s.RaidEstimate, ctx.Config.Glitch.SwarmStrengthPct);
        }

        /// <summary>The Warlord Ultimatum's wave (F-034): a heavy Rustborn raid. Uses no RNG draws. Returns the attack id.</summary>
        public static int SpawnWarlord(SimContext ctx)
        {
            GameState s = ctx.State;
            Spawn(ctx, 0, (int)(SimMath.Hash((uint)s.Tick, (uint)s.NextRaidId ^ 0x3A11u) % 4), 0, ctx.Config.Raid.WarningMinutes, AttackKind.Warlord);
            return s.RaidId;
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
            s.RaidEstimate = SimMath.PctFloor(s.RaidEstimate, strengthPct);
            s.RaidArriveTick = s.Tick + 1;
            s.BetrayalRaidId = s.RaidId;
            return s.RaidId;
        }

        private static void Spawn(SimContext ctx, int estimateRoll, int gateRoll, int fixedStrength, int warningMinutes, AttackKind kind = AttackKind.Raid)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            s.RaidId = s.NextRaidId++;
            s.RaidsToday++;
            s.RaidKind = kind;
            BattleSystem.Clear(s);
            // the big ones are commanded live by default; raids when the handler asks (SPEC-020)
            s.BattleLive = kind != AttackKind.Raid && warningMinutes > 1;
            if (warningMinutes > 1)
            {
                // WF5B Rapid Response, ST5A Prediction Engine: earlier warnings (never for a betrayal)
                warningMinutes += (Modules.Has(s, ModuleNode.WF5B) ? c.Modules.RapidResponseMinutes : 0) + (Modules.Has(s, ModuleNode.ST5A) ? c.Modules.PredictionMinutes : 0);
            }

            s.RaidArriveTick = s.Tick + warningMinutes;
            s.RaidStrength = fixedStrength > 0 ? fixedStrength : Defense.BaseRaidStrength(s, c);
            // the purge comes from the faction that marked us; the opening raid stays the fixed tutorial hit
            s.RaidFaction = kind == AttackKind.Purge ? WorldSystem.Hottest(s) : kind == AttackKind.Warlord ? Faction.Rustborn : WorldSystem.PickAttacker(s, c);
            if (fixedStrength <= 0)
            {
                s.RaidStrength = WorldSystem.ScaleByHeat(s, c, s.RaidFaction, s.RaidStrength);
                if (kind != AttackKind.Raid && Modules.Has(s, ModuleNode.WF4))
                {
                    s.RaidStrength = SimMath.PctFloor(s.RaidStrength, 100 - c.Modules.HardenedPct);
                }

                if (Modules.Has(s, ModuleNode.ST4))
                {
                    s.RaidStrength = SimMath.PctFloor(s.RaidStrength, 100 - c.Modules.DecoyPct);
                }
            }
            if (kind == AttackKind.Siege)
            {
                s.RaidStrength = SimMath.PctFloor(s.RaidStrength, c.Threats.SiegeStrengthPct);
            }
            else if (kind == AttackKind.Purge)
            {
                s.RaidStrength = SimMath.PctFloor(s.RaidStrength, c.Threats.PurgeStrengthPct);
            }
            else if (kind == AttackKind.Warlord)
            {
                s.RaidStrength = SimMath.PctFloor(s.RaidStrength, c.Living.UltimatumStrengthPct);
            }

            int band = (int)CorruptionSystem.Band(c, s.CorruptionMilli);
            int errorPct = c.Raid.EstimateErrorPctByBand[band];
            if (Modules.Has(s, ModuleNode.CY2B))
            {
                errorPct = SimMath.PctFloor(errorPct, 100 - c.Modules.InterceptionPct);
            }
            long estimate = (long)s.RaidStrength * ((100_000L + (errorPct * estimateRoll)) / 1000) / 100;
            s.RaidEstimate = (int)(estimate < 1 ? 1 : estimate);
            if (s.FalseIntel)
            {
                // a virus fed the AI bad data (SPEC-015 rule 3)
                s.RaidEstimate = System.Math.Max(1, SimMath.PctFloor(s.RaidEstimate, c.Threats.FalseIntelPct));
                s.FalseIntel = false;
            }

            // a spy in the attacker's camp (SPEC-019): loyal = exact and early, double agent = low
            int spy = IntelSystem.AttackEstimate(s, c, s.RaidFaction, s.RaidStrength);
            if (spy > 0 && s.RaidId != s.BetrayalRaidId)
            {
                s.RaidEstimate = spy;
                if (IntelSystem.Loyal(s, s.RaidFaction) && warningMinutes > 1)
                {
                    s.RaidArriveTick += c.Intel.SpyWarningMinutes;
                }
            }

            // ambush (doc 10 s2): a hunting faction's raid shows nothing until it hits
            bool ambush = kind == AttackKind.Raid && warningMinutes > 1 && !IntelSystem.Loyal(s, s.RaidFaction) && WorldSystem.Level(s.Heat[(int)s.RaidFaction]) >= HeatLevel.Hunted
                && SimMath.Hash((uint)s.RaidId * 0xA3B1u, (uint)(s.Rng.State >> 32)) % 100 < (uint)c.Threats.AmbushPct;
            if (ambush)
            {
                s.RaidArriveTick = s.Tick + 1;
            }

            if (ClimaxSystem.Silenced(s) || GlitchSystem.Flushing(s) || ambush)
            {
                // A silenced (or flushed, SPEC-021) AI predicts nothing (SPEC-011 rule 3).
                s.RaidEstimate = 0;
            }

            ctx.Emit(EventKind.RaidWarning, s.RaidId, (int)(s.RaidArriveTick - s.Tick), s.RaidEstimate, (int)kind);
            ctx.Emit(EventKind.AttackerIdentified, s.RaidId, (int)s.RaidFaction);
            ReportGate(ctx, gateRoll, ambush);
            AiSystem.OnRaidWarning(ctx);
        }

        /// <summary>
        /// SPEC-004 rules 5-6: the raid's true gate comes from the spawn tick's otherwise unused miss draw; the AI
        /// reports it, except for the first warning of a run and for Boldness-scaled lies decided by a hash.
        /// </summary>
        private static void ReportGate(SimContext ctx, int gateRoll, bool ambush)
        {
            GameState s = ctx.State;
            var ai = ctx.Config.Ai;
            s.RaidGate = (RaidGate)(1 + (gateRoll % 4));
            if (ClimaxSystem.Silenced(s) || ambush)
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
                lie = SimMath.Hash((uint)s.RaidId ^ (uint)(s.Rng.State >> 32), (uint)s.Tick) % 1000 < chance;
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

            if (ThreatSystem.TryStandingTribute(ctx))
            {
                ctx.Emit(EventKind.RaidResolved, id, (int)RaidOutcome.Tribute, s.RaidStrength, 0);
                Record(ctx, id, s.RaidGateReported != RaidGate.None && s.RaidGate != s.RaidGateReported ? RaidRecord.GateLie : 0);
                ClearIncoming(s);
                return;
            }

            int strength = SimMath.PctFloor(s.RaidStrength, 100 + variance);
            if (Defense.Unprepared(s))
            {
                strength = SimMath.PctFloor(strength, c.Defense.OfflineUnpreparedPct);
            }

            int defense = id == s.BetrayalRaidId ? Defense.Rating(s, c, s.Posture, s.Garrison, false) : Defense.Rating(s, c);
            BattleSystem.Apply(s, ref strength, ref defense);
            GlitchSystem.Defect(ctx, id, ref strength, ref defense);
            ctx.Emit(EventKind.RaidContact, id, (int)s.RaidGate);
            int lies = s.RaidGateReported != RaidGate.None && s.RaidGate != s.RaidGateReported ? RaidRecord.GateLie : 0;

            if (s.Posture == Posture.Dark && missRoll < c.Defense.DarkMissPct + (Modules.Has(s, ModuleNode.ST1) ? c.Modules.MaskingPts : 0))
            {
                ctx.Emit(EventKind.RaidResolved, id, (int)RaidOutcome.Missed, strength, defense);
                Survived(ctx);
                Record(ctx, id, lies);
                ClearIncoming(s);
                return;
            }

            if (defense >= strength || strength <= 0)
            {
                ctx.Emit(EventKind.RaidResolved, id, (int)RaidOutcome.Repelled, strength, defense);
                Survived(ctx);
                if (s.BattleEndTick != 0)
                {
                    // a live battle held with no casualties (mastery, SPEC-022)
                    LegacySystem.Earn(ctx, Mastery.CleanBattle);
                }
                ScarSystem.Repelled(ctx);
                Record(ctx, id, lies);
                ClearIncoming(s);
                return;
            }

            // Breach share in permille: how much of the raid got through.
            int breach = (int)(((long)(strength - defense) * 1000) / strength);
            int lootPct = s.Posture == Posture.Evacuate ? c.Defense.EvacuateLootPct : 100;
            if (s.RaidKind == AttackKind.Siege)
            {
                lootPct = SimMath.PctFloor(lootPct, c.Threats.SiegeLootPct);
            }

            int energy = Loot(s.Energy, c.Raid.LootPctOfEnergy, breach, lootPct, c.Raid.LootCap);
            int compute = Loot(s.Compute, c.Raid.ComputeLootPct, breach, lootPct, c.Raid.ComputeLootCap);
            int defenders = s.Posture == Posture.Evacuate ? 0 : (int)((long)s.Garrison * c.Defense.CasualtyPct * breach / 100_000);
            int civilians = 0;
            if (s.RaidKind == AttackKind.Purge && s.Posture != Posture.Evacuate)
            {
                // a purge also kills people who were never on the wall (SPEC-015 rule 4), never below the floor
                civilians = (int)((long)(s.People - s.Garrison) * c.Threats.PurgePopulationPct * breach / 100_000);
                civilians = System.Math.Min(civilians, System.Math.Max(0, s.People - s.Garrison - c.PeopleChoices.MinPeople));
            }

            int casualties = defenders + civilians;
            int populationBefore = s.People;

            s.Energy -= energy;
            s.Compute -= compute;
            s.People -= casualties;
            s.Garrison -= defenders;

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

            int downgrades = 0;
            if (s.RaidKind == AttackKind.Siege)
            {
                downgrades = ThreatSystem.Downgrade(ctx, id, System.Math.Max(1, breach / c.Threats.SiegeDamagePerBreachPermille));
            }
            else if (s.RaidKind == AttackKind.Purge)
            {
                downgrades = ThreatSystem.Downgrade(ctx, id, c.Threats.PurgeDowngrades);
            }

            ScarSystem.Breached(ctx, id, s.RaidKind, breach);
            if (s.RaidKind == AttackKind.Purge && s.Posture == Posture.None && s.Garrison == 0)
            {
                // doc 06 s4: the Hub falls to a purge it met undefended after ignoring the ladder; the cycle ends
                s.HubFallen = true;
            }

            // devastating loss (doc 10 s4): a Hub building downgraded, or too many people lost
            if (downgrades > 0 || (populationBefore > 0 && (long)casualties * 100 > (long)populationBefore * c.Raid.DevastatingPopLossPct))
            {
                s.MercyUntilTick = s.Tick + SimMath.PctFloor(c.Raid.MercyHours * SimConfig.TicksPerHour, s.Ironman ? c.Legacy.IronmanMercyPct : 100);
                ctx.Emit(EventKind.MercyStarted, id, (int)(s.MercyUntilTick - s.Tick));
            }

            // SPEC-006 rule 4: a corrupted AI may understate the breach in its summary (the ledger stays true).
            if (energy > 0 && CorruptionSystem.Band(c, s.CorruptionMilli) >= CorruptionBand.Unstable
                && SimMath.Hash((uint)id ^ (uint)(s.Rng.State >> 32), (uint)s.Tick + 7u) % 100 < (uint)c.Report.EditChancePct)
            {
                s.LiesTold++;
                ctx.Emit(EventKind.AdvisorLied, (int)LieKind.ReportEdit, id, energy, SimMath.PctFloor(energy, c.Report.EditShownPct));
                lies |= RaidRecord.SummaryEdit;
            }

            Record(ctx, id, lies);
            ClearIncoming(s);
        }

        /// <summary>A purge held without leaning on the AI (Manual delegation): mastery (SPEC-022).</summary>
        private static void Survived(SimContext ctx)
        {
            if (ctx.State.RaidKind == AttackKind.Purge && ctx.State.Delegation == DelegationLevel.Manual)
            {
                LegacySystem.Earn(ctx, Mastery.PurgeNoAi);
            }
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
            s.RaidKind = AttackKind.Raid;
            BattleSystem.Clear(s);
        }
    }
}
