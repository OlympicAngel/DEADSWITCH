namespace Deadswitch.Sim.Config
{
    /// <summary>Hazard zones and the drifting fallout front (SPEC-032, doc 05 s6-7). Placeholders: tune with play data.</summary>
    public sealed class HazardConfig : IConfigSection
    {
        public int ZoneCooldownHours = 48;
        public int PreparedRiskPct = 50;
        public int RadiationFuelPct = 50;
        public int RadiationSickPct = 40;
        public int RadiationCasualtyPct = 25;
        public int PlaguePeople = 3;
        public int PlagueInfectionPct = 35;
        public int PlagueInfected = 2;
        public int GraveyardRepairPoints = 2;
        public int GraveyardNestPct = 20;
        public int FalloutFirstDay = 3;
        public int FalloutDriftHours = 60;
        public int FalloutJitterHours = 24;
        public int FalloutFuelPct = 50;
        public int FalloutOutpostPct = 25;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("hazards", "Hazard zones (radiation, plague, machine graveyard) and the drifting fallout front (SPEC-032, doc 05 s6-7). All (tune).");
            v.Int("zone_cooldown_hours", ref ZoneCooldownHours, 0, 1_000, "A raided zone has nothing left to take for this long.");
            v.Int("prepared_risk_pct", ref PreparedRiskPct, 0, 100, "A scouted zone's sickness and infection chances are this % of normal.");
            v.Int("radiation_fuel_pct", ref RadiationFuelPct, 0, 1_000, "Extra fuel % to enter a radiation zone.");
            v.Int("radiation_sick_pct", ref RadiationSickPct, 0, 100, "Chance a squad back from radiation (or fallout) is sick.");
            v.Int("radiation_casualty_pct", ref RadiationCasualtyPct, 0, 100, "Share of a sick squad's survivors lost (at least 1).");
            v.Int("plague_people", ref PlaguePeople, 0, 100, "Survivors a won plague raid brings home (up to the population cap).");
            v.Int("plague_infection_pct", ref PlagueInfectionPct, 0, 100, "Chance a squad back from a plague zone carries infection home.");
            v.Int("plague_infected", ref PlagueInfected, 0, 100, "People at home lost to an infection (never under the people floor).");
            v.Int("graveyard_repair_points", ref GraveyardRepairPoints, 0, 20, "Damage points a won graveyard raid repairs for free (most damaged facility).");
            v.Int("graveyard_nest_pct", ref GraveyardNestPct, 0, 100, "Extra share of the squad lost to drone nests on a failed graveyard raid.");
            v.Int("fallout_first_day", ref FalloutFirstDay, 0, 1_000, "Day the fallout front first settles over a site.");
            v.Int("fallout_drift_hours", ref FalloutDriftHours, 1, 1_000, "Hours between fallout drifts.");
            v.Int("fallout_jitter_hours", ref FalloutJitterHours, 0, 1_000, "Up to this many extra hours before each drift (hash).");
            v.Int("fallout_fuel_pct", ref FalloutFuelPct, 0, 1_000, "Extra fuel % for an op to a site under fallout.");
            v.Int("fallout_outpost_pct", ref FalloutOutpostPct, 0, 100, "An outpost under fallout sends this % of its output.");
            v.EndSection();
        }
    }
}
