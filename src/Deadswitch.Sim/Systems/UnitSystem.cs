using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Which defender family a unit belongs to (SPEC-035).</summary>
    public enum UnitFamily
    {
        Infantry = 0,
        Drones = 1,
        Vehicles = 2,
    }

    /// <summary>
    /// Unit families and counters (SPEC-035, doc 10 s4): every attack brings a force mix; garrison, Drone Bays and
    /// Motor Pools hold better or worse against it. Motor Pools burn fuel. Hash picks, no RNG draws.
    /// </summary>
    public static class UnitSystem
    {
        /// <summary>Facilities that fight (and can turn on the Hub when the AI runs them unmanned).</summary>
        public static bool IsWarMachine(FacilityKind kind)
        {
            return kind == FacilityKind.Turret || kind == FacilityKind.DroneBay || kind == FacilityKind.MotorPool;
        }

        /// <summary>True while an attack is under way with a known force mix.</summary>
        public static bool HasMix(GameState s)
        {
            return s.RaidId != 0 && (s.RaidInfantryPct + s.RaidDronePct + s.RaidVehiclePct) == 100;
        }

        /// <summary>Counter modifier in percent points for a defender family against the current mix (0 without an attack).</summary>
        public static int CounterPts(GameState s, SimConfig c, UnitFamily family)
        {
            if (!HasMix(s))
            {
                return 0;
            }

            return CounterPts(c, family, s.RaidInfantryPct, s.RaidDronePct, s.RaidVehiclePct);
        }

        public static int CounterPts(SimConfig c, UnitFamily family, int infantry, int drones, int vehicles)
        {
            int k = c.Units.CounterPct;
            switch (family)
            {
                case UnitFamily.Drones:
                    return ((k * infantry) - (k * vehicles)) / 100;
                case UnitFamily.Vehicles:
                    return ((k * drones) - (k * infantry)) / 100;
                default:
                    return ((k * vehicles) - (k * drones)) / 100;
            }
        }

        /// <summary>Rolls the force mix of a new attack from its faction's profile (hash jitter).</summary>
        public static void RollMix(SimContext ctx)
        {
            GameState s = ctx.State;
            UnitConfig u = ctx.Config.Units;
            int[] mix = u.Mix(s.RaidFaction);
            int span = (2 * u.MixJitterPct) + 1;
            uint h = SimMath.Hash((uint)s.RaidId * 0x9E37u, (uint)(s.Rng.State >> 32) ^ 0x51A7u);
            int drones = SimMath.Clamp(mix[1] + (int)(h % (uint)span) - u.MixJitterPct, 0, 100);
            int vehicles = SimMath.Clamp(mix[2] + (int)((h >> 12) % (uint)span) - u.MixJitterPct, 0, 100 - drones);
            s.RaidDronePct = drones;
            s.RaidVehiclePct = vehicles;
            s.RaidInfantryPct = 100 - drones - vehicles;
        }

        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            UnitConfig u = ctx.Config.Units;
            foreach (FacilitySlot slot in s.Slots)
            {
                if (slot.Kind == FacilityKind.MotorPool && Economy.IsRunning(slot))
                {
                    int burn = u.MotorPoolFuelPerHour[System.Math.Min(slot.Level, u.MotorPoolFuelPerHour.Length) - 1];
                    s.Fuel -= System.Math.Min(s.Fuel, burn);
                }
            }
        }

        /// <summary>The warning names the forces (rule 3), unless the AI predicts nothing.</summary>
        public static void Announce(SimContext ctx, bool known)
        {
            GameState s = ctx.State;
            if (known)
            {
                ctx.Emit(EventKind.RaidForces, s.RaidId, s.RaidInfantryPct, s.RaidDronePct, s.RaidVehiclePct);
            }
        }
    }
}
