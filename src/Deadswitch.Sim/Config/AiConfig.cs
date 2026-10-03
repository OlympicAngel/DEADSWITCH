namespace Deadswitch.Sim.Config
{
    /// <summary>The AI's hidden dials, delegation effects and lies (SPEC-004, doc 03 s1-2, doc 10 s1.4 + s7).</summary>
    public sealed class AiConfig : IConfigSection
    {
        public int BoldnessPerHourDelegated = 300;
        public int BoldnessPerHourAutopilot = 700;
        public int BoldnessDecayPerHourManual = 100;
        public int PlanEveryMinutes = 60;
        public int PlanEnergyMargin = 150;
        public int AutopilotTurtlePct = 100;
        public int AutopilotEvacuatePct = 200;
        public bool FirstLie = true;
        public int LieChancePermilleAtFullBoldness = 300;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("ai", "The AI's hidden dials, delegation and lies (SPEC-004). Dials are milli-units: 100000 = 100%. All (tune).");
            v.Int("boldness_per_hour_delegated", ref BoldnessPerHourDelegated, 0, 100_000, "Boldness gained per game hour while the AI runs routines (Delegated).");
            v.Int("boldness_per_hour_autopilot", ref BoldnessPerHourAutopilot, 0, 100_000, "Boldness gained per game hour on offline autopilot.");
            v.Int("boldness_decay_per_hour_manual", ref BoldnessDecayPerHourManual, 0, 100_000, "Boldness lost per game hour while the handler runs everything (Manual).");
            v.Int("plan_every_minutes", ref PlanEveryMinutes, 1, 10_000, "How often the delegated AI looks at the build queue (game minutes).");
            v.Int("plan_energy_margin", ref PlanEnergyMargin, 0, 100_000, "Delegated AI builds power first while net energy per hour is below this.");
            v.Int("autopilot_turtle_pct", ref AutopilotTurtlePct, 1, 10_000, "Autopilot turtles with the full garrison when its estimate exceeds defense x this %.");
            v.Int("autopilot_evacuate_pct", ref AutopilotEvacuatePct, 1, 10_000, "Autopilot evacuates when its estimate exceeds defense x this %.");
            v.Bool("first_lie", ref FirstLie, "The first raid warning of a run reports the wrong gate (doc 10 s7.4).");
            v.Int("lie_chance_permille_at_full_boldness", ref LieChancePermilleAtFullBoldness, 0, 1000, "Chance per later raid warning that the reported gate is a lie, at 100% Boldness (scales linearly).");
            v.EndSection();
        }
    }
}
