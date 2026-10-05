namespace Deadswitch.Sim.Config
{
    /// <summary>Breakdown phases and their after-effects (SPEC-036, doc 07 s1, doc 05 s7). Placeholders: tune with play data.</summary>
    public sealed class PhaseConfig : IConfigSection
    {
        public int FalloutOutpostPct = 50;
        public int FalloutSalvagePct = 50;
        public int PlagueInfectionPct = 25;
        public int BlackoutGenerationPct = 15;
        public int SurgeDronePts = 20;
        public int SurgeDefectionPct = 15;
        public int PressureThreshold = 100;
        public int RadiationRaidPts = 35;
        public int LeakPtsPerHour = 5;
        public int PlagueRaidPts = 35;
        public int GraveyardRaidPts = 30;
        public int UnmannedPtsPerHour = 1;
        public int BlackoutPtsPerHour = 8;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("phases", "Breakdown phases (SPEC-036, doc 07 s1): fallout waves, plague outbreaks, rolling blackouts and machine surges join the world-event rotation; the handler's own actions build pressure that brings a matching phase next. All (tune).");
            v.Int("fallout_outpost_pct", ref FalloutOutpostPct, 0, 100, "Fallout wave: outposts send this % of their output.");
            v.Int("fallout_salvage_pct", ref FalloutSalvagePct, 0, 1_000, "Fallout wave: raids on the radiation zone bring back this % more.");
            v.Int("plague_infection_pct", ref PlagueInfectionPct, 0, 100, "Plague outbreak: any returning squad carries infection with at least this chance; regrowth stops.");
            v.Int("blackout_generation_pct", ref BlackoutGenerationPct, 0, 100, "Rolling blackouts: generators and reactors lose this % of output.");
            v.Int("surge_drone_pts", ref SurgeDronePts, 0, 100, "Machine surge: attackers bring this many more drone points in their mix.");
            v.Int("surge_defection_pct", ref SurgeDefectionPct, 0, 100, "Machine surge: added chance an unmanned war machine turns on the Hub.");
            v.Int("pressure_threshold", ref PressureThreshold, 1, 10_000, "Pressure at which the matching phase comes next (a warning at half).");
            v.Int("radiation_raid_pts", ref RadiationRaidPts, 0, 1_000, "Pressure toward a fallout wave per raid on the radiation zone.");
            v.Int("leak_pts_per_hour", ref LeakPtsPerHour, 0, 1_000, "Fallout pressure per hour a reactor leaks.");
            v.Int("plague_raid_pts", ref PlagueRaidPts, 0, 1_000, "Pressure toward an outbreak per raid on the plague zone.");
            v.Int("graveyard_raid_pts", ref GraveyardRaidPts, 0, 1_000, "Pressure toward a machine surge per raid on the graveyard.");
            v.Int("unmanned_pts_per_hour", ref UnmannedPtsPerHour, 0, 1_000, "Surge pressure per hour per unmanned war machine running.");
            v.Int("blackout_pts_per_hour", ref BlackoutPtsPerHour, 0, 1_000, "Pressure toward rolling blackouts per hour of grid blackout.");
            v.EndSection();
        }
    }
}
