namespace Deadswitch.Sim.Config
{
    /// <summary>Crewing rules (SPEC-002 rule 6, doc 02 s5).</summary>
    public sealed class CrewConfig : IConfigSection
    {
        public int UnmannedOutputPct = 50;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("crew", "Crewing: people run facilities; the AI runs the rest at reduced output (doc 02 s5).");
            v.Int("unmanned_output_pct", ref UnmannedOutputPct, 0, 100, "Output of a short-handed facility run by the AI, in percent of full output.");
            v.EndSection();
        }
    }
}
