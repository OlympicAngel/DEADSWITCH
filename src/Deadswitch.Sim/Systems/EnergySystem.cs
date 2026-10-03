using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Per-tick power (SPEC-002 rule 5): generation, then the AI core, then facilities in the handler's
    /// priority order. Facilities that cannot be paid are shed and restart only with one hour of upkeep
    /// in hand (no flicker). Leftover energy is stored up to the cap.
    /// </summary>
    public static class EnergySystem
    {
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;

            long available = s.Energy + (long)Rates.PerTick(Economy.GenerationPerHour(s, c), s.Tick);
            int coreRate = c.Energy.CoreUpkeepPerHour + (s.Posture == Posture.Dark ? c.Defense.DarkUpkeepPerHour : 0);
            int core = Rates.PerTick(coreRate, s.Tick);

            bool wasBlackout = s.Blackout;
            if (available >= core)
            {
                available -= core;
                s.Blackout = false;
            }
            else
            {
                available = 0;
                s.Blackout = true;
            }

            if (s.Blackout && !wasBlackout)
            {
                ctx.Emit(EventKind.BlackoutStarted);
            }
            else if (!s.Blackout && wasBlackout)
            {
                ctx.Emit(EventKind.BlackoutEnded);
            }

            foreach (int id in s.PowerPriority)
            {
                FacilitySlot slot = s.Slots[id];
                if (slot.IsEmpty)
                {
                    continue;
                }

                if (slot.Kind == FacilityKind.Generator)
                {
                    // Generators are the source; their own upkeep is netted from their output.
                    slot.Powered = slot.Enabled;
                    continue;
                }

                if (!slot.Enabled)
                {
                    slot.Powered = false;
                    continue;
                }

                bool powered;
                if (s.Blackout)
                {
                    powered = false;
                }
                else
                {
                    int perHour = Economy.UpkeepPerHour(c, slot);
                    int need = Rates.PerTick(perHour, s.Tick);
                    long threshold = slot.Powered ? need : (long)need + perHour;
                    powered = available >= threshold;
                    if (powered)
                    {
                        available -= need;
                    }
                }

                if (slot.Powered && !powered && !s.Blackout)
                {
                    ctx.Emit(EventKind.FacilityShed, id, (int)slot.Kind);
                }
                else if (!slot.Powered && powered)
                {
                    ctx.Emit(EventKind.FacilityRestored, id, (int)slot.Kind);
                }

                slot.Powered = powered;
            }

            int cap = Economy.EnergyCap(s, c);
            s.Energy = available > cap ? cap : (int)available;
        }
    }
}
