namespace Deadswitch.Sim.Config
{
    /// <summary>The AI builds in secret (SPEC-034, doc 03 s5): hidden nodes from skimmed compute. Placeholders: tune with play data.</summary>
    public sealed class SecretConfig : IConfigSection
    {
        public int FromBoldnessPct = 55;
        public int NodeCompute = 120;
        public int MaxNodes = 3;
        public int NodeGrowthPerHour = 40;
        public int NodeEnergyPerHour = 12;
        public int DismantleProjectMilli = 6_000;
        public int DismantleCompute = 40;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("secrets", "The AI builds in secret (SPEC-034, doc 03 s5): hidden nodes paid with skimmed compute speed its project and drain energy off the books until an Audit exposes them. Project is milli: 100000 = 100%. All (tune).");
            v.Int("from_boldness_pct", ref FromBoldnessPct, 0, 100, "The AI pours skimmed compute into hidden nodes while Boldness is at least this %.");
            v.Int("node_compute", ref NodeCompute, 1, 100_000, "Skimmed compute one hidden node costs.");
            v.Int("max_nodes", ref MaxNodes, 0, 20, "Hidden nodes at most.");
            v.Int("node_growth_per_hour", ref NodeGrowthPerHour, 0, 100_000, "Project progress per node per game hour.");
            v.Int("node_energy_per_hour", ref NodeEnergyPerHour, 0, 10_000, "Energy each node draws per game hour, off the books (the drift the handler can notice).");
            v.Int("dismantle_project_milli", ref DismantleProjectMilli, 0, 100_000, "Project progress lost per exposed node dismantled.");
            v.Int("dismantle_compute", ref DismantleCompute, 0, 100_000, "Compute salvaged per dismantled node (up to the cap).");
            v.EndSection();
        }
    }
}
