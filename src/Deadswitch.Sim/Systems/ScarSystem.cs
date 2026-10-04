using System.Collections.Generic;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Battle scars (SPEC-018): breaches damage facilities and leave wrecks in the yard; both stay until repaired.
    /// Only producers (Generator, Server Rack, Turret) take damage, so storage and bed caps never drop under what is held.
    /// Hash picks, no RNG draws.
    /// </summary>
    public static class ScarSystem
    {
        /// <summary>Damage only slows facilities that produce (Generator, Server Rack, Turret).</summary>
        public static bool Affects(FacilityKind kind)
        {
            return kind == FacilityKind.Generator || kind == FacilityKind.ServerRack || kind == FacilityKind.Turret || kind == FacilityKind.Reactor;
        }

        public static bool Repairing(GameState s, FacilitySlot slot)
        {
            return slot.RepairUntilTick > s.Tick;
        }

        /// <summary>Output points lost to damage (the damage holds until the repair finishes).</summary>
        public static int PenaltyPct(GameState s, SimConfig c, FacilitySlot slot)
        {
            return Affects(slot.Kind) ? slot.Damage * c.Scars.OutputPctPerDamage : 0;
        }

        public static int RepairCost(SimConfig c, FacilitySlot slot)
        {
            return c.Scars.RepairEnergyPerPoint * System.Math.Max(1, slot.Level) * slot.Damage;
        }

        /// <summary>Regrowth share kept with wrecks in the yard.</summary>
        public static int RegrowthPct(GameState s, SimConfig c)
        {
            return System.Math.Max(0, 100 - (s.Wreckage * c.Scars.RegrowthPctPerWreck));
        }

        /// <summary>A repelled attack still leaves enemy hulks in the yard.</summary>
        public static void Repelled(SimContext ctx)
        {
            AddWreckage(ctx, ctx.Config.Scars.RepelWreckage);
        }

        /// <summary>A breach scars the Hub by attack kind and depth.</summary>
        public static void Breached(SimContext ctx, int attackId, AttackKind kind, int breachPermille)
        {
            GameState s = ctx.State;
            ScarConfig c = ctx.Config.Scars;
            int damage;
            int wrecks;
            switch (kind)
            {
                case AttackKind.Purge:
                    damage = c.PurgeDamage;
                    wrecks = c.PurgeWreckage;
                    break;
                case AttackKind.Siege:
                case AttackKind.Warlord:
                    damage = c.SiegeDamage;
                    wrecks = c.SiegeWreckage;
                    break;
                default:
                    // a light raid breach only leaves wrecks; a deep one damages
                    damage = breachPermille >= c.HeavyBreachPermille ? c.RaidDamage : 0;
                    wrecks = c.BreachWreckage;
                    break;
            }

            var candidates = new List<int>();
            for (int n = 0; n < damage; n++)
            {
                candidates.Clear();
                for (int i = 0; i < s.Slots.Count; i++)
                {
                    FacilitySlot f = s.Slots[i];
                    // only producers take damage, and never one already under repair (paid repairs are kept)
                    if (Affects(f.Kind) && f.Damage < c.MaxDamage && !Repairing(s, f))
                    {
                        candidates.Add(i);
                    }
                }

                if (candidates.Count == 0)
                {
                    break;
                }

                uint h = SimMath.Hash((uint)attackId * 0x5CA2u + (uint)n, (uint)(s.Rng.State >> 32));
                int slot = candidates[(int)(h % (uint)candidates.Count)];
                // raiders go for the reactor first (SPEC-029)
                int reactor = candidates.FindIndex(i => s.Slots[i].Kind == FacilityKind.Reactor);
                if (reactor >= 0 && (h >> 12) % 100 < (uint)ctx.Config.ReactorRules.TargetPct)
                {
                    slot = candidates[reactor];
                }
                FacilitySlot hit = s.Slots[slot];
                hit.Damage++;
                s.ScarredAtTick = s.Tick;
                ctx.Emit(EventKind.FacilityScarred, attackId, slot, (int)hit.Kind, hit.Damage);
            }

            AddWreckage(ctx, wrecks);
        }

        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot f = s.Slots[i];
                if (f.RepairUntilTick != 0 && f.RepairUntilTick <= s.Tick)
                {
                    f.RepairUntilTick = 0;
                    f.Damage = 0;
                    ctx.Emit(EventKind.RepairDone, i, (int)f.Kind);
                }
            }
        }

        public static CommandResult Repair(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            int slot = cmd.A;
            if (slot < 0 || slot >= s.Slots.Count || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            FacilitySlot f = s.Slots[slot];
            if (f.IsEmpty || f.Damage == 0 || Repairing(s, f))
            {
                return CommandResult.Reject(RejectReason.NotDamaged);
            }

            if (s.RaidId != 0)
            {
                return CommandResult.Reject(RejectReason.ThreatActive);
            }

            int cost = RepairCost(c, f);
            if (s.Energy < cost)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            s.Energy -= cost;
            int minutes = c.Scars.RepairMinutesPerPoint * f.Damage;
            f.RepairUntilTick = s.Tick + minutes;
            ctx.Emit(EventKind.RepairStarted, slot, cost, minutes);
            return CommandResult.Ok;
        }

        public static CommandResult ClearWreckage(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.Wreckage == 0)
            {
                return CommandResult.Reject(RejectReason.NothingPending);
            }

            if (s.RaidId != 0)
            {
                return CommandResult.Reject(RejectReason.ThreatActive);
            }

            int cost = ctx.Config.Scars.ClearEnergyPerWreck * s.Wreckage;
            if (s.Energy < cost)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            s.Energy -= cost;
            ctx.Emit(EventKind.WreckageCleared, s.Wreckage, cost);
            s.Wreckage = 0;
            return CommandResult.Ok;
        }

        private static void AddWreckage(SimContext ctx, int n)
        {
            GameState s = ctx.State;
            int before = s.Wreckage;
            s.Wreckage = SimMath.Clamp(s.Wreckage + n, 0, ctx.Config.Scars.MaxWreckage);
            if (n > 0)
            {
                s.ScarredAtTick = s.Tick;
            }

            if (s.Wreckage != before)
            {
                ctx.Emit(EventKind.WreckageAdded, s.Wreckage);
            }
        }
    }
}
