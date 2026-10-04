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

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            int fuel = FuelPerHour(s, c);
            bool fueled = s.Fuel >= fuel;
            if (fuel > 0 && fueled)
            {
                s.Fuel -= fuel;
            }

            if (fueled != s.ReactorFueled)
            {
                s.ReactorFueled = fueled;
                ctx.Emit(EventKind.ReactorFuel, fueled ? 1 : 0, fuel);
            }

            ReactorConfig r = c.ReactorRules;
            if (s.Tick % SimConfig.TicksPerDay == 0 && s.Tick > 0 && Leaking(s, c))
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
