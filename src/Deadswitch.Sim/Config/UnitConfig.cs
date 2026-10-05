namespace Deadswitch.Sim.Config
{
    /// <summary>Unit families and counters (SPEC-035, doc 10 s4 damage model). Placeholders: tune with play data.</summary>
    public sealed class UnitConfig : IConfigSection
    {
        public int CounterPct = 40;
        public int MixJitterPct = 15;
        public int MotorPoolMinTier = 2;
        public int[] MotorPoolFuelPerHour = { 2, 3, 4 };
        public int[] MixRustborn = { 60, 10, 30 };
        public int[] MixVanguard = { 40, 15, 45 };
        public int[] MixChurch = { 25, 65, 10 };
        public int[] MixHoldouts = { 15, 55, 30 };

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("units", "Unit families (SPEC-035): infantry (garrison), drones (Drone Bays), vehicles (Motor Pools). Drones beat infantry, vehicles beat drones, infantry beat vehicles (doc 10 s4). Mixes are infantry / drones / vehicles %. All (tune).");
            v.Int("counter_pct", ref CounterPct, 0, 100, "A defender family gains this % x the share of attackers it beats, and loses it x the share that beats it.");
            v.Int("mix_jitter_pct", ref MixJitterPct, 0, 50, "Each attack's drone and vehicle shares vary by up to this many points from the faction profile.");
            v.Int("motor_pool_min_tier", ref MotorPoolMinTier, 1, 4, "Tier needed to build a Motor Pool.");
            v.IntList("motor_pool_fuel_per_hour", ref MotorPoolFuelPerHour, 0, 1_000, 1, 20, "Fuel a running Motor Pool burns per game hour, per level; without fuel its vehicles do not fight.");
            v.IntList("mix_rustborn", ref MixRustborn, 0, 100, 3, 3, "Rustborn force mix: scavenger militia and technicals.");
            v.IntList("mix_vanguard", ref MixVanguard, 0, 100, 3, 3, "Vanguard Command force mix: soldiers and armour.");
            v.IntList("mix_church", ref MixChurch, 0, 100, 3, 3, "Church of the Last Signal force mix: machine swarms.");
            v.IntList("mix_holdouts", ref MixHoldouts, 0, 100, 3, 3, "Halcyon Dynamics force mix: corporate drones and escorts.");
            v.EndSection();
        }

        public int[] Mix(State.Faction f)
        {
            switch (f)
            {
                case State.Faction.Vanguard: return MixVanguard;
                case State.Faction.Church: return MixChurch;
                case State.Faction.Holdouts: return MixHoldouts;
                default: return MixRustborn;
            }
        }
    }
}
