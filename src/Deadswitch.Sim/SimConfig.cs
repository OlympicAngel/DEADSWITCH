using Deadswitch.Sim.Config;

namespace Deadswitch.Sim
{
    /// <summary>
    /// Every tunable number in the sim, grouped into sections. 1 tick = 1 game minute. Integers only.
    /// Code defaults are the doc 10 baseline; the game runs the shipped balance file
    /// (<c>Resources/DeadswitchBalance.toml</c>, read with <see cref="BalanceText"/>), which may diverge while tuning.
    /// Treat instances as immutable after constructing a Simulation.
    /// </summary>
    public sealed class SimConfig
    {
        public const int TicksPerHour = 60;
        public const int TicksPerDay = 1440;

        public EnergyConfig Energy = new EnergyConfig();
        public FuelConfig Fuel = new FuelConfig();
        public ComputeConfig Compute = new ComputeConfig();
        public PeopleConfig People = new PeopleConfig();
        public CorruptionConfig Corruption = new CorruptionConfig();
        public RaidConfig Raid = new RaidConfig();
        public HubConfig Hub = new HubConfig();
        public BuildConfig Build = new BuildConfig();
        public CrewConfig Crew = new CrewConfig();
        public OverrideConfig Override = new OverrideConfig();
        public DefenseConfig Defense = new DefenseConfig();
        public HostConfig Host = new HostConfig();

        // Facility tables (SPEC-002). Index 0 = level 1. All (tune).
        public FacilityConfig Generator = new FacilityConfig(
            "facility_generator",
            "Generator: scavenged power plant. Output = energy per game hour.",
            "Energy generated per game hour, per level.").Set(
            costEnergy: new[] { 300, 260, 450, 780, 1350 },
            costCompute: new[] { 0, 0, 10, 25, 50 },
            buildMinutes: new[] { 30, 45, 90, 180, 360 },
            upkeepPerHour: new[] { 0, 0, 0, 0, 0 },
            output: new[] { 480, 660, 900, 1200, 1560 },
            crew: new[] { 2, 2, 3, 3, 4 });

        public FacilityConfig Turret = new FacilityConfig(
            "facility_turret",
            "Turret: automated Hub defense (doc 02 s6 Military). Output = defense rating while powered.",
            "Defense rating per level.").Set(
            costEnergy: new[] { 150, 250, 420, 700, 1150 },
            costCompute: new[] { 0, 10, 20, 40, 70 },
            buildMinutes: new[] { 20, 45, 90, 180, 360 },
            upkeepPerHour: new[] { 60, 100, 150, 220, 300 },
            output: new[] { 20, 32, 48, 68, 92 },
            crew: new[] { 1, 1, 2, 2, 3 });

        public FacilityConfig ServerRack = new FacilityConfig(
            "facility_server_rack",
            "Server Rack: burns energy to produce compute for the AI (doc 02 s4). Output = compute per game hour.",
            "Compute produced per game hour, per level.").Set(
            costEnergy: new[] { 120, 220, 400, 700, 1200 },
            costCompute: new[] { 0, 15, 30, 60, 100 },
            buildMinutes: new[] { 15, 40, 80, 160, 320 },
            upkeepPerHour: new[] { 180, 300, 450, 660, 930 },
            output: new[] { 60, 100, 150, 210, 280 },
            crew: new[] { 1, 1, 2, 2, 3 });

        public FacilityConfig LifeSupport = new FacilityConfig(
            "facility_life_support",
            "Life Support Grid: raises the population cap while powered (doc 10 s1.3).",
            "Population cap bonus per level.").Set(
            costEnergy: new[] { 200, 340, 560, 900, 1400 },
            costCompute: new[] { 0, 10, 20, 40, 70 },
            buildMinutes: new[] { 60, 120, 240, 420, 720 },
            upkeepPerHour: new[] { 120, 210, 330, 480, 660 },
            output: new[] { 6, 12, 20, 30, 42 },
            crew: new[] { 1, 2, 2, 3, 3 });

        public FacilityConfig Battery = new FacilityConfig(
            "facility_battery_bank",
            "Battery Bank: raises the energy storage cap while powered.",
            "Energy cap bonus per level.").Set(
            costEnergy: new[] { 100, 200, 380, 650, 1050 },
            costCompute: new[] { 0, 0, 10, 20, 40 },
            buildMinutes: new[] { 15, 40, 80, 150, 300 },
            upkeepPerHour: new[] { 0, 0, 30, 60, 120 },
            output: new[] { 250, 600, 1100, 1800, 2800 },
            crew: new[] { 0, 0, 1, 1, 1 });

        /// <summary>The doc 10 baseline values.</summary>
        public static SimConfig Tier1()
        {
            return new SimConfig();
        }

        /// <summary>Deep copy (round-trips through the balance text, which also proves the writer and reader agree).</summary>
        public SimConfig Clone()
        {
            return BalanceText.Parse(BalanceText.Write(this));
        }

        /// <summary>Stable 64-bit identity of every value. See <see cref="ConfigHasher"/>.</summary>
        public ulong ComputeHash()
        {
            return ConfigHasher.Hash(this);
        }

        /// <summary>Visits every section in a fixed order. The order defines the balance file layout and the config hash.</summary>
        public void Visit(IConfigVisitor visitor)
        {
            Energy.Visit(visitor);
            Fuel.Visit(visitor);
            Compute.Visit(visitor);
            People.Visit(visitor);
            Corruption.Visit(visitor);
            Raid.Visit(visitor);
            Hub.Visit(visitor);
            Build.Visit(visitor);
            Crew.Visit(visitor);
            Override.Visit(visitor);
            Defense.Visit(visitor);
            Host.Visit(visitor);
            Generator.Visit(visitor);
            ServerRack.Visit(visitor);
            LifeSupport.Visit(visitor);
            Battery.Visit(visitor);
            Turret.Visit(visitor);
        }

        /// <summary>Table for a facility kind, or null for <see cref="State.FacilityKind.None"/> and unknown values.</summary>
        public FacilityConfig? Facility(State.FacilityKind kind)
        {
            switch (kind)
            {
                case State.FacilityKind.Generator:
                    return Generator;
                case State.FacilityKind.ServerRack:
                    return ServerRack;
                case State.FacilityKind.LifeSupport:
                    return LifeSupport;
                case State.FacilityKind.BatteryBank:
                    return Battery;
                case State.FacilityKind.Turret:
                    return Turret;
                default:
                    return null;
            }
        }

        /// <summary>Cross-value rules the per-key ranges cannot express. Returns human-readable problems (empty = valid).</summary>
        public System.Collections.Generic.List<string> Validate()
        {
            var problems = new System.Collections.Generic.List<string>();
            foreach (FacilityConfig f in new[] { Generator, ServerRack, LifeSupport, Battery, Turret })
            {
                int n = f.Output.Length;
                if (f.CostEnergy.Length != n || f.CostCompute.Length != n || f.BuildMinutes.Length != n || f.UpkeepPerHour.Length != n || f.Crew.Length != n)
                {
                    problems.Add(f.Section + ": every per-level table must have the same length as 'output' (" + n + ").");
                }
            }

            if (Energy.Start > Energy.Cap)
            {
                problems.Add("energy.start must not exceed energy.cap.");
            }

            if (Compute.Start > Compute.Cap)
            {
                problems.Add("compute.start must not exceed compute.cap.");
            }

            if (People.Start > People.Cap)
            {
                problems.Add("people.start must not exceed people.cap.");
            }

            if (!(Corruption.GlitchyFrom < Corruption.UnstableFrom && Corruption.UnstableFrom < Corruption.CriticalFrom))
            {
                problems.Add("corruption bands must increase: glitchy_from < unstable_from < critical_from.");
            }

            if (Override.StartCharges > Override.MaxCharges)
            {
                problems.Add("override.start_charges must not exceed override.max_charges.");
            }

            return problems;
        }
    }
}
