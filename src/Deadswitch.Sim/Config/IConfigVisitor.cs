namespace Deadswitch.Sim.Config
{
    /// <summary>
    /// Walks every tunable value in a <see cref="SimConfig"/> in a fixed order. One binding per value
    /// (key, range, description) drives the balance file writer, reader, validator and hasher, so a new
    /// tunable only has to be declared once.
    /// </summary>
    public interface IConfigVisitor
    {
        /// <summary>Starts a named section. Section and key names are lower_snake_case.</summary>
        void BeginSection(string name, string description);

        /// <summary>An integer value with an inclusive valid range.</summary>
        void Int(string key, ref int value, int min, int max, string description);

        /// <summary>A boolean switch.</summary>
        void Bool(string key, ref bool value, string description);

        /// <summary>An integer table (for example a value per tier). Every element must be in range.</summary>
        void IntList(string key, ref int[] values, int min, int max, int minCount, int maxCount, string description);

        void EndSection();
    }
}
