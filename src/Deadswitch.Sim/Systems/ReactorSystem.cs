using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Reactor (SPEC-029, doc 02 s3 risky power sources): burns fuel every hour for huge output and scrams without
    /// it; raiders aim for it (ScarSystem); while badly damaged it leaks radiation and people sicken each night
    /// until it is repaired. No RNG draws.
    /// </summary>
    public static class ReactorSystem
    {
        public static int FuelPerHour(GameState s, SimConfig c)
        {
            int fuel = 0;
            foreach (FacilitySlot slot in s.Slots)
            {
                if (slot.Kind == FacilityKind.Reactor && slot.Enabled && slot.Level > 0)
                {
                    int[] table = c.ReactorRules.FuelPerHour;
                    fuel += table[System.Math.Min(slot.Level, table.Length) - 1];
                }
            }

            return fuel;
        }

        public static bool Leaking(GameState s, SimConfig c)
        {
            foreach (FacilitySlot slot in s.Slots)
            {
                if (slot.Kind == FacilityKind.Reactor && slot.Damage >= c.ReactorRules.LeakDamage)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fuel burns every minute a reactor runs (an hour's need spread over its 60 ticks, exact in integers), so
        /// switching it off around a check gains nothing. Out of fuel it scrams at once; any fuel restarts it.
        /// </summary>
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            int need = FuelPerHour(s, ctx.Config);
            if (need == 0)
            {
                return;
            }

            if (!s.ReactorFueled)
            {
                if (s.Fuel > 0)
                {
                    s.ReactorFueled = true;
                    ctx.Emit(EventKind.ReactorFuel, 1, need);
                }

                return;
            }

            s.ReactorFuelTicks += need;
            int units = s.ReactorFuelTicks / SimConfig.TicksPerHour;
            if (units == 0)
            {
                return;
            }

            if (s.Fuel < units)
            {
                s.Fuel = 0;
                s.ReactorFuelTicks = 0;
                s.ReactorFueled = false;
                ctx.Emit(EventKind.ReactorFuel, 0, need);
                return;
            }

            s.Fuel -= units;
            s.ReactorFuelTicks -= units * SimConfig.TicksPerHour;
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            ReactorConfig r = c.ReactorRules;

            // the leak only harms a Hub its handler is watching over: no punitive absence (doc 10 s4)
            if (s.Tick % SimConfig.TicksPerDay == 0 && s.Tick > 0 && !s.Away && Leaking(s, c))
            {
                int lost = System.Math.Min(r.LeakPeoplePerDay, System.Math.Max(0, s.People - s.Garrison - c.PeopleChoices.MinPeople));
                if (lost > 0)
                {
                    s.People -= lost;
                    ctx.Emit(EventKind.RadiationLeak, lost);
                }
            }
        }
    }
}
