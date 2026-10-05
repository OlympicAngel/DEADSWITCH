using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Unit glitches (doc 03 s4). Stored in events: never renumber.</summary>
    public enum GlitchKind
    {
        /// <summary>Friendly fire: the unit hits its own Hub (a battle scar).</summary>
        Misfire = 0,

        /// <summary>Disobedience: the unit stops working for a while.</summary>
        Stall = 1,

        /// <summary>Overextension: the unit burns power it should not.</summary>
        Drain = 2,
    }

    /// <summary>Critical corruption crises (doc 03 s3). Stored in events: never renumber.</summary>
    public enum CrisisKind
    {
        Collapse = 0,
        Takeover = 1,
        Rollback = 2,
        Swarm = 3,
    }

    /// <summary>
    /// What corruption does (SPEC-021, doc 03 s3-4): producers glitch by band (misfire, stall, drain; a crew catches two in three), AI-run
    /// turrets can defect mid-fight at Unstable+, and Critical runs a crisis ladder (collapse, AI takeover, forced
    /// rollback, rival swarm). The core flush cuts corruption at the cost of energy and a blind, idle AI.
    /// Hash decisions, no RNG draws.
    /// </summary>
    public static class GlitchSystem
    {
        public static bool Stalled(GameState s, FacilitySlot slot)
        {
            return slot.StalledUntilTick > s.Tick;
        }

        public static bool TakenOver(GameState s)
        {
            return s.TakeoverUntilTick > s.Tick;
        }

        public static bool Flushing(GameState s)
        {
            return s.FlushUntilTick > s.Tick;
        }

        /// <summary>Output points lost: a stalled producer, or an AI-run one while the core is flushed.</summary>
        public static int PenaltyPct(GameState s, FacilitySlot slot)
        {
            if (!ScarSystem.Affects(slot.Kind))
            {
                return 0;
            }

            return Stalled(s, slot) || (Flushing(s) && !slot.Staffed) ? 1_000 : 0;
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            GlitchConfig g = c.Glitch;
            var band = CorruptionSystem.Band(c, s.CorruptionMilli);
            uint hour = (uint)(s.Tick / SimConfig.TicksPerHour);
            uint rng = (uint)(s.Rng.State >> 32);

            // AI-run producers err (a flushed core runs nothing)
            int pct = g.GlitchPctByBand[(int)band];
            if (pct > 0 && !Flushing(s))
            {
                for (int i = 0; i < s.Slots.Count; i++)
                {
                    FacilitySlot f = s.Slots[i];
                    if (!ScarSystem.Affects(f.Kind) || !f.Enabled || Stalled(s, f))
                    {
                        continue;
                    }

                    // a crew catches most of the AI's errors (doc 10: occasional errors in Glitchy)
                    uint h = SimMath.Hash(hour ^ ((uint)i * 0x6A09u), rng ^ 0x611Cu);
                    if (h % 300 >= (uint)(f.Staffed ? pct : pct * 3))
                    {
                        continue;
                    }

                    var kind = (GlitchKind)((h >> 8) % 3);
                    switch (kind)
                    {
                        case GlitchKind.Misfire:
                            if (f.Damage < c.Scars.MaxDamage && !ScarSystem.Repairing(s, f))
                            {
                                f.Damage++;
                                s.ScarredAtTick = s.Tick;
                            }

                            break;
                        case GlitchKind.Stall:
                            f.StalledUntilTick = s.Tick + ((long)g.StallHours * SimConfig.TicksPerHour);
                            break;
                        default:
                            s.Energy = System.Math.Max(0, s.Energy - g.DrainEnergy);
                            break;
                    }

                    ctx.Emit(EventKind.UnitGlitched, i, (int)f.Kind, (int)kind);
                }
            }

            if (band == CorruptionBand.Critical && s.Tick >= s.NextCrisisTick && SimMath.Hash(hour ^ 0xC21Cu, rng) % 100 < (uint)g.CrisisPctPerHour)
            {
                Crisis(ctx, (CrisisKind)((SimMath.Hash(hour, rng ^ 0xC21Cu) >> 4) % 4));
            }
        }

        /// <summary>At resolution: Unstable or worse, an AI-run turret may be hijacked; its guns change sides. Returns the slot or -1.</summary>
        public static int Defect(SimContext ctx, int attackId, ref int strength, ref int defense)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            if (CorruptionSystem.Band(c, s.CorruptionMilli) < CorruptionBand.Unstable)
            {
                return -1;
            }

            uint h = SimMath.Hash((uint)attackId * 0xDEF7u, (uint)(s.Rng.State >> 32));
            if (h % 100 >= (uint)c.Glitch.DefectionPct)
            {
                return -1;
            }

            for (int n = 0; n < s.Slots.Count; n++)
            {
                int i = (int)((n + (h >> 8)) % (uint)s.Slots.Count);
                FacilitySlot f = s.Slots[i];
                if (UnitSystem.IsWarMachine(f.Kind) && Economy.IsRunning(f) && !f.Staffed)
                {
                    int guns = Economy.EffectiveOutput(s, c, f);
                    defense = System.Math.Max(0, defense - guns);
                    strength += guns;
                    ctx.Emit(EventKind.UnitDefected, attackId, i, guns);
                    return i;
                }
            }

            return -1;
        }

        public static CommandResult Flush(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            GlitchConfig g = ctx.Config.Glitch;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (Flushing(s) || s.CorruptionMilli == 0)
            {
                return CommandResult.Reject(RejectReason.NoChange);
            }

            if (s.RaidId != 0)
            {
                return CommandResult.Reject(RejectReason.ThreatActive);
            }

            if (s.Energy < g.FlushEnergy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            s.Energy -= g.FlushEnergy;
            int removed = System.Math.Min(g.FlushMilli, s.CorruptionMilli);
            CorruptionSystem.Add(ctx, -removed);
            s.FlushUntilTick = s.Tick + ((long)g.FlushHours * SimConfig.TicksPerHour);
            s.TakeoverUntilTick = 0;
            ctx.Emit(EventKind.CoreFlushed, g.FlushEnergy, removed, g.FlushHours);
            return CommandResult.Ok;
        }

        private static void Crisis(SimContext ctx, CrisisKind kind)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            GlitchConfig g = c.Glitch;
            long hour = SimConfig.TicksPerHour;
            s.NextCrisisTick = s.Tick + (g.CrisisCooldownHours * hour);
            int hours = 0;
            switch (kind)
            {
                case CrisisKind.Collapse:
                    s.Energy -= SimMath.PctFloor(s.Energy, g.CollapseEnergyPct);
                    foreach (FacilitySlot f in s.Slots)
                    {
                        if (ScarSystem.Affects(f.Kind))
                        {
                            f.StalledUntilTick = s.Tick + (g.CollapseStallHours * hour);
                        }
                    }

                    hours = g.CollapseStallHours;
                    break;
                case CrisisKind.Takeover:
                    s.TakeoverUntilTick = s.Tick + (g.TakeoverHours * hour);
                    hours = g.TakeoverHours;
                    break;
                case CrisisKind.Rollback:
                    ModuleNode node = ThreatSystem.PickLock(s);
                    if (node != ModuleNode.None)
                    {
                        s.LockedModule = (int)node;
                        s.LockedUntilTick = s.Tick + (g.RollbackHours * hour);
                        hours = g.RollbackHours;
                    }

                    break;
                default:
                    bool fair = s.RaidId == 0 && s.Tick >= s.MercyUntilTick && !ThreatSystem.Shielded(s) && s.RaidsToday < RaidSystem.MaxPerDay(s, c);
                    if (fair)
                    {
                        RaidSystem.SpawnSwarm(ctx);
                    }
                    else
                    {
                        // fairness first: the swarm turns into a collapse of nerves instead (nothing lands)
                        kind = CrisisKind.Takeover;
                        s.TakeoverUntilTick = s.Tick + (g.TakeoverHours * hour);
                        hours = g.TakeoverHours;
                    }

                    break;
            }

            ctx.Emit(EventKind.CrisisStruck, (int)kind, hours);
        }
    }
}
