namespace Deadswitch.Sim.Config
{
    /// <summary>Research costs (in catalog order, see Systems/Modules) and node effects (SPEC-008).</summary>
    public sealed class ModuleConfig : IConfigSection
    {
        public int[] ResearchEnergy = { 450, 1200, 2400, 180, 300, 300, 600, 700, 650, 650, 900, 180, 300, 300, 600, 700, 650, 650, 900, 180, 300, 300, 600, 700, 650, 650, 900, 180, 300, 300, 600, 700, 650, 650, 900 };
        public int[] ResearchCompute = { 90, 100, 100, 30, 60, 60, 80, 90, 80, 80, 100, 30, 60, 60, 80, 90, 80, 80, 100, 30, 60, 60, 80, 90, 80, 80, 100, 30, 60, 60, 80, 90, 80, 80, 100 };
        public int[] ResearchMinutes = { 240, 600, 1440, 90, 150, 150, 240, 300, 300, 300, 420, 90, 150, 150, 240, 300, 300, 300, 420, 90, 150, 150, 240, 300, 300, 300, 420, 90, 150, 150, 240, 300, 300, 300, 420 };
        public int CancelRefundPct = 50;
        public int LoadBalancingPct = 10;
        public int OverclockPct = 25;
        public int DeepCellsPct = 30;
        public int HabitatPop = 10;
        public int PrefabPct = 25;
        public int SalvagePts = 25;
        public int FuelCellsPct = 15;
        public int AutomationUnmannedPct = 75;
        public int FireControlPct = 20;
        public int MilitiaPerDefender = 2;
        public int KillZonePts = 25;
        public int CombatSimsPts = 10;
        public int HardenedPct = 15;
        public int AssaultCasualtyPct = 50;
        public int RapidResponseMinutes = 15;
        public int VeteranStrength = 2;
        public int FirewallPct = 30;
        public int CounterIntrusionPts = 15;
        public int InterceptionPct = 50;
        public int CleanRoomMilli = 200;
        public int WormPct = 100;
        public int PartitionPermille = 10;
        public int MaskingPts = 15;
        public int QuietRoutesPct = 50;
        public int HeatSinkPct = 100;
        public int DecoyPct = 10;
        public int PredictionMinutes = 30;
        public int FalseTrailsPct = 50;
        public int GhostOps = 1;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("modules", "AI module research (SPEC-008). Lists follow the catalog order: M1-M3, then each field (LG, WF, CY, ST) as 1, 2A, 2B, 3, 4, 5A, 5B, 6. All (tune).");
            v.IntList("research_energy", ref ResearchEnergy, 0, 100_000, 35, 35, "Energy cost per node.");
            v.IntList("research_compute", ref ResearchCompute, 0, 100_000, 35, 35, "Compute cost per node (compute caps at compute.cap).");
            v.IntList("research_minutes", ref ResearchMinutes, 1, 100_000, 35, 35, "Research time per node (game minutes).");
            v.Int("cancel_refund_pct", ref CancelRefundPct, 0, 100, "Share of the cost returned when research is cancelled.");
            v.Int("load_balancing_pct", ref LoadBalancingPct, 0, 100, "LG1 Load Balancing: facility upkeep reduction in percent.");
            v.Int("overclock_pct", ref OverclockPct, 0, 1_000, "LG2A Overclocked Racks: Server Rack output bonus in percent.");
            v.Int("deep_cells_pct", ref DeepCellsPct, 0, 1_000, "LG2B Deep Cells: Battery Bank capacity bonus in percent.");
            v.Int("habitat_pop", ref HabitatPop, 0, 1_000, "LG3 Habitat Management: population cap bonus.");
            v.Int("prefab_pct", ref PrefabPct, 0, 90, "LG4 Prefab Assembly: build time reduction in percent.");
            v.Int("salvage_pts", ref SalvagePts, 0, 100, "LG5A Salvage Doctrine: extra refund points on cancel and demolish.");
            v.Int("fuel_cells_pct", ref FuelCellsPct, 0, 1_000, "LG5B Fuel Cells: Generator output bonus in percent.");
            v.Int("automation_unmanned_pct", ref AutomationUnmannedPct, 0, 100, "LG6 Automation Protocols: output of unmanned facilities in percent.");
            v.Int("fire_control_pct", ref FireControlPct, 0, 1_000, "WF1 Fire Control: turret output bonus in percent.");
            v.Int("militia_per_defender", ref MilitiaPerDefender, 0, 1_000, "WF2A Drilled Militia: extra defense per garrison defender.");
            v.Int("kill_zone_pts", ref KillZonePts, 0, 1_000, "WF2B Kill Zones: extra Turtle defense points.");
            v.Int("combat_sims_pts", ref CombatSimsPts, 0, 100, "WF3 Combat Simulations: raid operation odds bonus (points).");
            v.Int("hardened_pct", ref HardenedPct, 0, 90, "WF4 Hardened Walls: siege and purge strength reduction in percent.");
            v.Int("assault_casualty_pct", ref AssaultCasualtyPct, 0, 100, "WF5A Assault Doctrine: raid operation casualties in percent of normal.");
            v.Int("rapid_response_minutes", ref RapidResponseMinutes, 0, 1_000, "WF5B Rapid Response: extra attack warning (minutes).");
            v.Int("veteran_strength", ref VeteranStrength, 0, 1_000, "WF6 Veteran Cadre: extra squad strength per person.");
            v.Int("firewall_pct", ref FirewallPct, 0, 90, "CY1 Firewall: virus strength reduction in percent.");
            v.Int("counter_intrusion_pts", ref CounterIntrusionPts, 0, 100, "CY2A Counter-intrusion: hack odds bonus (points).");
            v.Int("interception_pct", ref InterceptionPct, 0, 100, "CY2B Signal Interception: attack estimate error reduction in percent.");
            v.Int("clean_room_milli", ref CleanRoomMilli, 0, 100_000, "CY3 Clean Room: extra corruption decay per hour (milli).");
            v.Int("worm_pct", ref WormPct, 0, 1_000, "CY5A Worm Factory: extra hack compute loot in percent.");
            v.Int("partition_permille", ref PartitionPermille, 0, 1_000, "CY6 Core Partition: extra proportional corruption decay per hour (per mille).");
            v.Int("masking_pts", ref MaskingPts, 0, 100, "ST1 Signal Masking: extra Dark posture miss chance (points).");
            v.Int("quiet_routes_pct", ref QuietRoutesPct, 0, 100, "ST2B Quiet Routes: operation fuel cost reduction in percent.");
            v.Int("heat_sink_pct", ref HeatSinkPct, 0, 1_000, "ST3 Heat Sink: extra faction heat decay in percent.");
            v.Int("decoy_pct", ref DecoyPct, 0, 90, "ST4 Decoys: strength reduction for every attack in percent.");
            v.Int("prediction_minutes", ref PredictionMinutes, 0, 1_000, "ST5A Prediction Engine: extra attack warning (minutes).");
            v.Int("false_trails_pct", ref FalseTrailsPct, 0, 100, "ST5B False Trails: operation heat reduction in percent.");
            v.Int("ghost_ops", ref GhostOps, 0, 10, "ST6 Ghost Network: extra operation slots.");
            v.EndSection();
        }
    }
}
