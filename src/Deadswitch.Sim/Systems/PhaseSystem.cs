using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Breakdown phases (SPEC-036, doc 07 s1): the war's damage comes back in waves. Four phases join the world-event
    /// rotation, and the handler's own actions (radiation and plague raids, a leaking reactor, unmanned war machines,
    /// grid blackouts) build pressure that brings the matching phase next, with a warning at half. No RNG draws.
    /// </summary>
    public static class PhaseSystem
    {
        /// <summary>Pressure slot of a phase (index into <see cref="GameState.Aftershock"/>), or -1.</summary>
        public static int Slot(WorldEventKind kind)
        {
            switch (kind)
            {
                case WorldEventKind.FalloutWave: return 0;
                case WorldEventKind.PlagueOutbreak: return 1;
                case WorldEventKind.RollingBlackouts: return 2;
                case WorldEventKind.MachineSurge: return 3;
                default: return -1;
            }
        }

        public static WorldEventKind Phase(int slot)
        {
            return (WorldEventKind)((int)WorldEventKind.FalloutWave + slot);
        }

        public static void Add(SimContext ctx, WorldEventKind phase, int pts)
        {
            GameState s = ctx.State;
            int i = Slot(phase);
            if (i < 0 || pts <= 0 || LivingSystem.Active(s, phase))
            {
                return;
            }

            int half = ctx.Config.Phases.PressureThreshold / 2;
            int before = s.Aftershock[i];
            s.Aftershock[i] = System.Math.Min(ctx.Config.Phases.PressureThreshold * 2, before + pts);
            if (before < half && s.Aftershock[i] >= half)
            {
                ctx.Emit(EventKind.AftershockBuilding, (int)phase, s.Aftershock[i]);
            }
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            PhaseConfig p = c.Phases;
            if (ReactorSystem.Leaking(s, c))
            {
                Add(ctx, WorldEventKind.FalloutWave, p.LeakPtsPerHour);
            }

            if (s.Blackout)
            {
                Add(ctx, WorldEventKind.RollingBlackouts, p.BlackoutPtsPerHour);
            }

            int unmanned = 0;
            foreach (FacilitySlot f in s.Slots)
            {
                if (UnitSystem.IsWarMachine(f.Kind) && Economy.IsRunning(f) && !f.Staffed)
                {
                    unmanned++;
                }
            }

            Add(ctx, WorldEventKind.MachineSurge, unmanned * p.UnmannedPtsPerHour);
        }

        /// <summary>A raid came back from a hazard zone: the wastes remember.</summary>
        public static void ZoneRaided(SimContext ctx, SiteKind kind)
        {
            PhaseConfig p = ctx.Config.Phases;
            switch (kind)
            {
                case SiteKind.Radiation:
                    Add(ctx, WorldEventKind.FalloutWave, p.RadiationRaidPts);
                    break;
                case SiteKind.Plague:
                    Add(ctx, WorldEventKind.PlagueOutbreak, p.PlagueRaidPts);
                    break;
                case SiteKind.Graveyard:
                    Add(ctx, WorldEventKind.MachineSurge, p.GraveyardRaidPts);
                    break;
            }
        }

        /// <summary>The next world event: the phase whose pressure is due (highest first), or none (a normal roll).</summary>
        public static WorldEventKind Due(SimContext ctx)
        {
            GameState s = ctx.State;
            int best = -1;
            for (int i = 0; i < s.Aftershock.Length; i++)
            {
                if (s.Aftershock[i] >= ctx.Config.Phases.PressureThreshold && (best < 0 || s.Aftershock[i] > s.Aftershock[best]))
                {
                    best = i;
                }
            }

            if (best < 0)
            {
                return WorldEventKind.None;
            }

            s.Aftershock[best] = 0;
            return Phase(best);
        }

        /// <summary>Output percent of a power source (rolling blackouts cut generation).</summary>
        public static int GenerationPct(GameState s, SimConfig c)
        {
            return LivingSystem.Active(s, WorldEventKind.RollingBlackouts) ? 100 - c.Phases.BlackoutGenerationPct : 100;
        }

        /// <summary>Regrowth stops while a plague runs.</summary>
        public static bool RegrowthHalted(GameState s)
        {
            return LivingSystem.Active(s, WorldEventKind.PlagueOutbreak);
        }
    }
}
