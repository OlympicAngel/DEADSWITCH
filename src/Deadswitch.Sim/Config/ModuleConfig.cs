namespace Deadswitch.Sim.Config
{
    /// <summary>Research costs (in catalog order, see Systems/Modules) and node effects (SPEC-008).</summary>
    public sealed class ModuleConfig : IConfigSection
    {
        public int[] ResearchEnergy = { 450, 1200, 2400, 180, 300, 300, 600, 700, 650, 650, 900 };
        public int[] ResearchCompute = { 90, 100, 100, 30, 60, 60, 80, 90, 80, 80, 100 };
        public int[] ResearchMinutes = { 240, 600, 1440, 90, 150, 150, 240, 300, 300, 300, 420 };
        public int CancelRefundPct = 50;
        public int LoadBalancingPct = 10;
        public int OverclockPct = 25;
        public int DeepCellsPct = 30;
        public int HabitatPop = 10;
        public int PrefabPct = 25;
        public int SalvagePts = 25;
        public int FuelCellsPct = 15;
        public int AutomationUnmannedPct = 75;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("modules", "AI module research (SPEC-008). Lists follow the catalog order: M1, M2, M3, LG1, LG2A, LG2B, LG3, LG4, LG5A, LG5B, LG6. All (tune).");
            v.IntList("research_energy", ref ResearchEnergy, 0, 100_000, 11, 11, "Energy cost per node.");
            v.IntList("research_compute", ref ResearchCompute, 0, 100_000, 11, 11, "Compute cost per node (compute caps at compute.cap).");
            v.IntList("research_minutes", ref ResearchMinutes, 1, 100_000, 11, 11, "Research time per node (game minutes).");
            v.Int("cancel_refund_pct", ref CancelRefundPct, 0, 100, "Share of the cost returned when research is cancelled.");
            v.Int("load_balancing_pct", ref LoadBalancingPct, 0, 100, "LG1 Load Balancing: facility upkeep reduction in percent.");
            v.Int("overclock_pct", ref OverclockPct, 0, 1_000, "LG2A Overclocked Racks: Server Rack output bonus in percent.");
            v.Int("deep_cells_pct", ref DeepCellsPct, 0, 1_000, "LG2B Deep Cells: Battery Bank capacity bonus in percent.");
            v.Int("habitat_pop", ref HabitatPop, 0, 1_000, "LG3 Habitat Management: population cap bonus.");
            v.Int("prefab_pct", ref PrefabPct, 0, 90, "LG4 Prefab Assembly: build time reduction in percent.");
            v.Int("salvage_pts", ref SalvagePts, 0, 100, "LG5A Salvage Doctrine: extra refund points on cancel and demolish.");
            v.Int("fuel_cells_pct", ref FuelCellsPct, 0, 1_000, "LG5B Fuel Cells: Generator output bonus in percent.");
            v.Int("automation_unmanned_pct", ref AutomationUnmannedPct, 0, 100, "LG6 Automation Protocols: output of unmanned facilities in percent.");
            v.EndSection();
        }
    }
}
