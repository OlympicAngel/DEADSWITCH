namespace Deadswitch.Sim.State
{
    /// <summary>How much the AI runs on its own (doc 03 s2). Values are stored in saves: never renumber.</summary>
    public enum DelegationLevel
    {
        /// <summary>The AI only advises. Slow but safe, low reliance.</summary>
        Manual = 0,

        /// <summary>The AI handles the build queue and routine upkeep. Moderate reliance.</summary>
        Delegated = 1,

        /// <summary>The AI makes defense and retreat decisions while the handler is away. May get them wrong.</summary>
        Autopilot = 2,
    }
}
