namespace Deadswitch.Sim.Config
{
    /// <summary>
    /// Per-level tables for one facility kind (SPEC-002). Index 0 is level 1; the table length is the max level.
    /// All tables of a kind must have the same length (checked by <see cref="SimConfig.Validate"/>).
    /// </summary>
    public sealed class FacilityConfig : IConfigSection
    {
        private readonly string _section;
        private readonly string _description;
        private readonly string _outputDescription;

        public FacilityConfig(string section, string description, string outputDescription)
        {
            _section = section;
            _description = description;
            _outputDescription = outputDescription;
        }

        public int[] CostEnergy = new int[0];
        public int[] CostCompute = new int[0];
        public int[] BuildMinutes = new int[0];
        public int[] UpkeepPerHour = new int[0];
        public int[] Output = new int[0];
        public int[] Crew = new int[0];

        public int MaxLevel => Output.Length;

        public string Section => _section;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection(_section, _description);
            v.IntList("cost_energy", ref CostEnergy, 0, 1_000_000, 1, 20, "Energy paid up front to build (level 1) or upgrade to each level.");
            v.IntList("cost_compute", ref CostCompute, 0, 1_000_000, 1, 20, "Compute paid up front per level.");
            v.IntList("build_minutes", ref BuildMinutes, 1, 1_000_000, 1, 20, "Construction time per level in game minutes (ticks).");
            v.IntList("upkeep_per_hour", ref UpkeepPerHour, 0, 1_000_000, 1, 20, "Energy upkeep per game hour while powered, per level.");
            v.IntList("output", ref Output, 0, 1_000_000, 1, 20, _outputDescription);
            v.IntList("crew", ref Crew, 0, 1_000, 1, 20, "People needed to run the facility at full output, per level.");
            v.EndSection();
        }

        internal FacilityConfig Set(int[] costEnergy, int[] costCompute, int[] buildMinutes, int[] upkeepPerHour, int[] output, int[] crew)
        {
            CostEnergy = costEnergy;
            CostCompute = costCompute;
            BuildMinutes = buildMinutes;
            UpkeepPerHour = upkeepPerHour;
            Output = output;
            Crew = crew;
            return this;
        }
    }
}
