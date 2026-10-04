using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Threat signatures beyond the raid (SPEC-015): siege scheduling, silent viruses, the purge warning ladder,
    /// the vacation shield and tribute standing orders. Uses no RNG draws (decisions come from hashes) so the
    /// raid system keeps its fixed four draws per tick. Sieges and purges become incoming attacks through
    /// <see cref="RaidSystem"/>.
    /// </summary>
    public static class ThreatSystem
    {
        /// <summary>True while the vacation shield holds (raised and the handler is away).</summary>
        public static bool Shielded(GameState s)
        {
            return s.Away && s.ShieldUntilTick > s.Tick;
        }

        /// <summary>A siege is due: the next raid spawn becomes a siege (Tier 2+).</summary>
        public static bool SiegeDue(GameState s)
        {
            return s.Tier >= 2 && s.NextSiegeTick > 0 && s.Tick >= s.NextSiegeTick;
        }

        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            ThreatConfig c = ctx.Config.Threats;

            // shield: expiry and charge regrowth
            if (s.ShieldUntilTick > 0 && s.Tick >= s.ShieldUntilTick)
            {
                s.ShieldUntilTick = 0;
                ctx.Emit(EventKind.ShieldChanged, 0, 0);
            }

            if (s.ShieldCharges < 1 && s.ShieldNextChargeTick > 0 && s.Tick >= s.ShieldNextChargeTick)
            {
                s.ShieldCharges = 1;
                s.ShieldNextChargeTick = 0;
            }

            // schedules start when their signature unlocks
            if (s.Tier >= 2 && s.NextSiegeTick == 0)
            {
                s.NextSiegeTick = s.Tick + Interval(s, c.SiegeIntervalHours, c.SiegeJitterPct, 11u);
            }

            if (s.Tier >= 2 && s.NextPurgeTick == 0)
            {
                s.NextPurgeTick = s.Tick + Interval(s, c.PurgeIntervalHours, 0, 13u);
            }

            if (s.NextVirusTick == 0 && Modules.IsRestored(s, ModuleNode.M1))
            {
                s.NextVirusTick = s.Tick + Interval(s, c.VirusIntervalHours, c.VirusJitterPct, 17u);
            }

            if (s.LockedModule != 0 && s.Tick >= s.LockedUntilTick)
            {
                s.LockedModule = 0;
                s.LockedUntilTick = 0;
            }

            if (s.NextVirusTick > 0 && s.Tick >= s.NextVirusTick)
            {
                s.NextVirusTick = s.Tick + Interval(s, c.VirusIntervalHours, c.VirusJitterPct, 17u);
                if (!Shielded(s))
                {
                    Virus(ctx);
                }
            }

            PurgeLadder(ctx);
        }

        /// <summary>The handler came back: the shield drops (it only covers absence).</summary>
        public static void OnReturn(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.ShieldUntilTick > 0)
            {
                s.ShieldUntilTick = 0;
                ctx.Emit(EventKind.ShieldChanged, 0, 0);
            }
        }

        public static CommandResult ActivateShield(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            ThreatConfig c = ctx.Config.Threats;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.ShieldUntilTick > s.Tick)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            if (s.ShieldCharges < 1)
            {
                return CommandResult.Reject(RejectReason.NoShield);
            }

            if (s.RaidId != 0 || s.PurgeStage >= PurgeStage.Staging)
            {
                return CommandResult.Reject(RejectReason.ThreatActive);
            }

            s.ShieldCharges--;
            s.ShieldNextChargeTick = s.Tick + ((long)c.ShieldRegenDays * SimConfig.TicksPerDay);
            s.ShieldUntilTick = s.Tick + ((long)c.ShieldMaxHours * SimConfig.TicksPerHour);
            ctx.Emit(EventKind.ShieldChanged, 1, c.ShieldMaxHours * SimConfig.TicksPerHour);
            return CommandResult.Ok;
        }

        public static CommandResult SetTributeOrder(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if ((cmd.A != 0 && cmd.A != 1) || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            bool on = cmd.A == 1;
            if (s.TributeOrder == on)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            s.TributeOrder = on;
            ctx.Emit(EventKind.TributeOrderSet, cmd.A);
            return CommandResult.Ok;
        }

        public static CommandResult PayPurgeTribute(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            ThreatConfig c = ctx.Config.Threats;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.PurgeStage != PurgeStage.Ultimatum)
            {
                return CommandResult.Reject(RejectReason.NoTarget);
            }

            if (s.Energy < c.PurgeTributeEnergy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            if (s.Compute < c.PurgeTributeCompute)
            {
                return CommandResult.Reject(RejectReason.NotEnoughCompute);
            }

            s.Energy -= c.PurgeTributeEnergy;
            s.Compute -= c.PurgeTributeCompute;
            ctx.Emit(EventKind.TributePaid, 0, c.PurgeTributeEnergy, c.PurgeTributeCompute);
            EndLadder(ctx, 2);
            return CommandResult.Ok;
        }

        /// <summary>
        /// Tribute standing order (SPEC-015 rule 6): an arriving raid is paid off while the handler is away.
        /// Returns true when it was paid (the raid then ends without a fight).
        /// </summary>
        public static bool TryStandingTribute(SimContext ctx)
        {
            GameState s = ctx.State;
            ThreatConfig c = ctx.Config.Threats;
            if (!s.TributeOrder || !s.Away || s.RaidKind != AttackKind.Raid || s.RaidId == s.BetrayalRaidId)
            {
                return false;
            }

            int pay = System.Math.Max(c.TributeMinEnergy, SimMath.PctFloor(s.Energy, c.TributePct));
            if (s.Energy < pay || pay <= 0)
            {
                return false;
            }

            s.Energy -= pay;
            ctx.Emit(EventKind.TributePaid, s.RaidId, pay, 0);
            ctx.Emit(EventKind.LossLine, s.RaidId, (int)LossResource.Energy, pay);
            return true;
        }

        /// <summary>
        /// Siege and purge damage: downgrades the strongest facilities (highest level, then lowest slot), never
        /// below level 1 and never one under construction. Returns how many were downgraded.
        /// </summary>
        public static int Downgrade(SimContext ctx, int attackId, int count)
        {
            GameState s = ctx.State;
            int done = 0;
            for (int n = 0; n < count; n++)
            {
                int best = -1;
                for (int i = 0; i < s.Slots.Count; i++)
                {
                    FacilitySlot f = s.Slots[i];
                    if (f.IsEmpty || f.Level < 2 || s.JobForSlot(i) != null)
                    {
                        continue;
                    }

                    if (best < 0 || f.Level > s.Slots[best].Level)
                    {
                        best = i;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                FacilitySlot hit = s.Slots[best];
                hit.Level--;
                done++;
                ctx.Emit(EventKind.FacilityDamaged, attackId, best, (int)hit.Kind, hit.Level);
            }

            return done;
        }

        private static void Virus(SimContext ctx)
        {
            GameState s = ctx.State;
            ThreatConfig c = ctx.Config.Threats;
            int strength = c.VirusStrength + (c.VirusStrengthPerTier * (s.Tier - 1));
            if (s.Compute >= strength)
            {
                // the firewall burns compute to purge the intrusion
                s.Compute -= strength;
                ctx.Emit(EventKind.VirusStruck, 0, strength, 0, 0);
                return;
            }

            int burned = s.Compute;
            s.Compute = 0;
            ModuleNode locked = PickLock(s);
            if (locked != ModuleNode.None && c.VirusLockHours > 0)
            {
                s.LockedModule = (int)locked;
                s.LockedUntilTick = s.Tick + ((long)c.VirusLockHours * SimConfig.TicksPerHour);
            }

            s.FalseIntel = true;
            CorruptionSystem.Add(ctx, c.VirusCorruption);
            ctx.Emit(EventKind.VirusStruck, 1, burned, (int)locked, c.VirusCorruption);
        }

        /// <summary>A restored field module (never the trunk), chosen by hash.</summary>
        private static ModuleNode PickLock(GameState s)
        {
            int count = 0;
            foreach (ModuleDef d in Modules.Catalog)
            {
                if (d.Field != ModuleField.Trunk && Modules.IsRestored(s, d.Node))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return ModuleNode.None;
            }

            int pick = (int)(SimMath.Hash((uint)(s.Tick / SimConfig.TicksPerHour), (uint)(s.Rng.State >> 32) ^ 0x5EEDu) % (uint)count);
            foreach (ModuleDef d in Modules.Catalog)
            {
                if (d.Field != ModuleField.Trunk && Modules.IsRestored(s, d.Node) && pick-- == 0)
                {
                    return d.Node;
                }
            }

            return ModuleNode.None;
        }

        private static void PurgeLadder(SimContext ctx)
        {
            GameState s = ctx.State;
            ThreatConfig c = ctx.Config.Threats;
            long hour = SimConfig.TicksPerHour;
            switch (s.PurgeStage)
            {
                case PurgeStage.None:
                    if (s.NextPurgeTick > 0 && s.Tick >= s.NextPurgeTick && !Shielded(s) && s.ClimaxAtTick == 0)
                    {
                        s.PurgeStage = PurgeStage.Rumor;
                        s.PurgeAtTick = s.Tick + ((c.PurgeRumorHours + c.PurgeStagingHours) * hour);
                        s.PurgeReal = SimMath.Hash((uint)s.Tick, (uint)(s.Rng.State >> 32) ^ 0x9A26u) % 100 >= (uint)c.RumorFalsePct;
                        ctx.Emit(EventKind.PurgeLadder, (int)PurgeStage.Rumor, (int)(s.PurgeAtTick - s.Tick));
                    }

                    break;
                case PurgeStage.Rumor:
                    if (s.Tick >= s.PurgeAtTick - (c.PurgeStagingHours * hour))
                    {
                        if (!s.PurgeReal)
                        {
                            EndLadder(ctx, 1);
                            break;
                        }

                        s.PurgeStage = PurgeStage.Staging;
                        ctx.Emit(EventKind.PurgeLadder, (int)PurgeStage.Staging, (int)(s.PurgeAtTick - s.Tick));
                    }

                    break;
                case PurgeStage.Staging:
                    if (s.Tick >= s.PurgeAtTick - (c.PurgeUltimatumHours * hour))
                    {
                        s.PurgeStage = PurgeStage.Ultimatum;
                        ctx.Emit(EventKind.PurgeLadder, (int)PurgeStage.Ultimatum, (int)(s.PurgeAtTick - s.Tick));
                    }

                    break;
                case PurgeStage.Ultimatum:
                    if (s.Tick >= s.PurgeAtTick)
                    {
                        if (s.RaidId != 0)
                        {
                            // another attack is still inbound: the purge force waits for it to clear
                            s.PurgeAtTick = s.Tick + hour;
                            break;
                        }

                        RaidSystem.SpawnPurge(ctx);
                        EndLadder(ctx, 3);
                    }

                    break;
            }
        }

        private static void EndLadder(SimContext ctx, int reason)
        {
            GameState s = ctx.State;
            s.PurgeStage = PurgeStage.None;
            s.PurgeAtTick = 0;
            s.PurgeReal = false;
            s.NextPurgeTick = s.Tick + Interval(s, ctx.Config.Threats.PurgeIntervalHours, 0, 13u);
            ctx.Emit(EventKind.PurgeLadder, (int)PurgeStage.None, 0, reason);
        }

        /// <summary>Ticks until the next occurrence: the interval with hash jitter (no RNG draw).</summary>
        private static long Interval(GameState s, int hours, int jitterPct, uint salt)
        {
            long ticks = (long)hours * SimConfig.TicksPerHour;
            if (jitterPct <= 0)
            {
                return ticks;
            }

            long span = ticks * jitterPct / 100;
            long offset = (long)(SimMath.Hash((uint)s.Tick ^ salt, (uint)(s.Rng.State >> 32)) % (uint)((2 * span) + 1)) - span;
            return System.Math.Max(SimConfig.TicksPerHour, ticks + offset);
        }
    }
}
