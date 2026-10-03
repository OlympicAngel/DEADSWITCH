namespace Deadswitch.Sim.Config
{
    /// <summary>The Hub layout (SPEC-002 rule 2).</summary>
    public sealed class HubConfig : IConfigSection
    {
        public int Slots = 6;

        public void Visit(IConfigVisitor v)
        {
            v.BeginSection("hub", "Hub layout: facility slots (SPEC-002).");
            v.Int("slots", ref Slots, 2, 64, "Facility slots in the Tier 1 Hub. Slot 0 starts with a Generator, slot 1 with a Server Rack.");
            v.EndSection();
        }
    }
}
