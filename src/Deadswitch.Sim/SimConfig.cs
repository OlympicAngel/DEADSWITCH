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
        public AiConfig Ai = new AiConfig();
        public ReportConfig Report = new ReportConfig();
        public ProjectConfig Project = new ProjectConfig();
        public ModuleConfig Modules = new ModuleConfig();
        public TierConfig Tier = new TierConfig();
        public OpeningConfig Opening = new OpeningConfig();
        public ClimaxConfig Climax = new ClimaxConfig();
        public PeopleChoiceConfig PeopleChoices = new PeopleChoiceConfig();
        public ThreatConfig Threats = new ThreatConfig();
        public WorldConfig World = new WorldConfig();
        public LivingConfig Living = new LivingConfig();
        public ScarConfig Scars = new ScarConfig();
        public IntelConfig Intel = new IntelConfig();
        public BattleConfig Battle = new BattleConfig();
        public GlitchConfig Glitch = new GlitchConfig();
        public LegacyConfig Legacy = new LegacyConfig();
        public DiplomacyConfig Diplomacy = new DiplomacyConfig();
        public ChapterConfig Chapters = new ChapterConfig();
        public AdaptConfig Adapt = new AdaptConfig();
        public LuckConfig Luck = new LuckConfig();
        public HazardConfig Hazards = new HazardConfig();
        public UnitConfig Units = new UnitConfig();
        public PhaseConfig Phases = new PhaseConfig();
        public SecretConfig Secrets = new SecretConfig();
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

        public FacilityConfig Reactor = new FacilityConfig(
            "facility_reactor",
            "Reactor: compact fission plant from Tier 3 (doc 02 s3 risky power, SPEC-029). Output = energy per game hour while fueled.",
            "Energy generated per game hour while fueled, per level.").Set(
            costEnergy: new[] { 2400, 3600, 5200 },
            costCompute: new[] { 80, 140, 220 },
            buildMinutes: new[] { 720, 1440, 2160 },
            upkeepPerHour: new[] { 0, 0, 0 },
            output: new[] { 1800, 2600, 3600 },
            crew: new[] { 4, 6, 8 });

        public ReactorConfig ReactorRules = new ReactorConfig();

        public FacilityConfig DroneBay = new FacilityConfig(
            "facility_drone_bay",
            "Drone Bay: reprogrammed rogue machines (doc 02 s6, SPEC-035). Output = drone defense rating while powered. Drones beat infantry, lose to vehicles.",
            "Drone defense rating per level.").Set(
            costEnergy: new[] { 220, 380, 620, 1000 },
            costCompute: new[] { 20, 40, 70, 110 },
            buildMinutes: new[] { 40, 90, 180, 360 },
            upkeepPerHour: new[] { 70, 110, 160, 230 },
            output: new[] { 24, 38, 56, 80 },
            crew: new[] { 2, 3, 4, 5 });

        public FacilityConfig MotorPool = new FacilityConfig(
            "facility_motor_pool",
            "Motor Pool: armour and gun trucks (doc 02 s6 vehicle factory, SPEC-035). Output = vehicle defense rating while powered and fuelled. Vehicles beat drones, lose to infantry in the ruins.",
            "Vehicle defense rating per level.").Set(
            costEnergy: new[] { 500, 850, 1350 },
            costCompute: new[] { 30, 60, 100 },
            buildMinutes: new[] { 120, 240, 480 },
            upkeepPerHour: new[] { 40, 60, 90 },
            output: new[] { 45, 70, 100 },
            crew: new[] { 3, 4, 6 });

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
            Ai.Visit(visitor);
            Report.Visit(visitor);
            Project.Visit(visitor);
            Modules.Visit(visitor);
            Tier.Visit(visitor);
            Opening.Visit(visitor);
            Climax.Visit(visitor);
            PeopleChoices.Visit(visitor);
            Threats.Visit(visitor);
            World.Visit(visitor);
            Living.Visit(visitor);
            Scars.Visit(visitor);
            Intel.Visit(visitor);
            Battle.Visit(visitor);
            Glitch.Visit(visitor);
            Legacy.Visit(visitor);
            Diplomacy.Visit(visitor);
            Chapters.Visit(visitor);
            Adapt.Visit(visitor);
            Luck.Visit(visitor);
            Hazards.Visit(visitor);
            Units.Visit(visitor);
            Phases.Visit(visitor);
            Secrets.Visit(visitor);
            Host.Visit(visitor);
            Generator.Visit(visitor);
            ServerRack.Visit(visitor);
            LifeSupport.Visit(visitor);
            Battery.Visit(visitor);
            Turret.Visit(visitor);
            Reactor.Visit(visitor);
            ReactorRules.Visit(visitor);
            DroneBay.Visit(visitor);
            MotorPool.Visit(visitor);
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
                case State.FacilityKind.Reactor:
                    return Reactor;
                case State.FacilityKind.DroneBay:
                    return DroneBay;
                case State.FacilityKind.MotorPool:
                    return MotorPool;
                default:
                    return null;
            }
        }

        /// <summary>Cross-value rules the per-key ranges cannot express. Returns human-readable problems (empty = valid).</summary>
        public System.Collections.Generic.List<string> Validate()
        {
            var problems = new System.Collections.Generic.List<string>();
            foreach (FacilityConfig f in new[] { Generator, ServerRack, LifeSupport, Battery, Turret, Reactor, DroneBay, MotorPool })
            {
                int n = f.Output.Length;
                if (f.CostEnergy.Length != n || f.CostCompute.Length != n || f.BuildMinutes.Length != n || f.UpkeepPerHour.Length != n || f.Crew.Length != n)
                {
                    problems.Add(f.Section + ": every per-level table must have the same length as 'output' (" + n + ").");
                }
            }

            if (Luck.CalmPct + Luck.RestlessPct > 100)
            {
                problems.Add("luck.calm_pct + luck.restless_pct must not exceed 100.");
            }

            if (ReactorRules.FuelPerHour.Length != Reactor.MaxLevel)
            {
                problems.Add("reactor.fuel_per_hour must have one entry per reactor level (" + Reactor.MaxLevel + ").");
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

            if (!(Project.ActiveFrom < Project.AdvancedFrom && Project.AdvancedFrom < Project.ImminentFrom))
            {
                problems.Add("project stages must increase: active_from < advanced_from < imminent_from.");
            }

            if (PeopleChoices.MutinousBelow >= PeopleChoices.StrainedBelow)
            {
                problems.Add("people_choices.mutinous_below must be below strained_below.");
            }

            if (PeopleChoices.SurgeCooldownHours < PeopleChoices.SurgeHours)
            {
                problems.Add("people_choices.surge_cooldown_hours must be at least surge_hours (no chained surges).");
            }

            int slots = Hub.Slots;
            foreach (int added in Tier.SlotsAdded)
            {
                slots += added;
            }

            if (slots > 64)
            {
                problems.Add("hub.slots plus every tier.slots_added must not exceed 64.");
            }

            if (Threats.ShieldStartCharges > Threats.ShieldMaxCharges)
            {
                problems.Add("threats.shield_start_charges must not exceed shield_max_charges.");
            }

            if (Threats.PurgeUltimatumHours >= Threats.PurgeStagingHours)
            {
                problems.Add("threats.purge_ultimatum_hours must be below purge_staging_hours.");
            }

            if (Climax.CancelToPct >= Project.ImminentFrom)
            {
                problems.Add("climax.cancel_to_pct must be below project.imminent_from.");
            }

            return problems;
        }
    }
}
