namespace Deadswitch.Sim.Config
{
    /// <summary>Chapter arcs and memory fragments (SPEC-024, doc 07 s4). Placeholders: tune with play data.</summary>
    public sealed class ChapterConfig : IConfigSection
    {
        public int TwistFallbackHours = 96;
        public int ForkProjectMilli = 75_000;
        public int VillainPoints = 2;
        public int PayoffPoints = 4;
        public int[] PayoffEnergy = { 150, 300, 500, 800 };
        public int[] PayoffCompute = { 20, 40, 80, 120 };
        public int PayoffHeatDrop = 20_000;
        public int PayoffCorruptionDrop = 3_000;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("chapters", "Chapter arcs: one per tier with a villain, a twist and a payoff; each payoff recovers a memory fragment that survives reboots (doc 07 s4). Heat and corruption are milli. All (tune).");
            v.Int("twist_fallback_hours", ref TwistFallbackHours, 1, 10_000, "Hours after a chapter opens when its twist lands anyway.");
            v.Int("fork_project_milli", ref ForkProjectMilli, 0, 100_000, "Chapter 4 twists when the AI's project reaches this (or its final window opens).");
            v.Int("villain_points", ref VillainPoints, 1, 100, "Payoff points for repelling the chapter villain's attack (any other attack: 1).");
            v.Int("payoff_points", ref PayoffPoints, 1, 100, "Points after the twist that close the chapter.");
            v.IntList("payoff_energy", ref PayoffEnergy, 0, 100_000, 4, 4, "Energy paid when a chapter closes, by tier (capped by storage).");
            v.IntList("payoff_compute", ref PayoffCompute, 0, 100_000, 4, 4, "Compute paid when a chapter closes, by tier (capped by storage).");
            v.Int("payoff_heat_drop", ref PayoffHeatDrop, 0, 100_000, "The villain's heat taken off when its chapter closes.");
            v.Int("payoff_corruption_drop", ref PayoffCorruptionDrop, 0, 100_000, "Corruption cleared by the recovered memory fragment.");
            v.EndSection();
        }
    }
}
