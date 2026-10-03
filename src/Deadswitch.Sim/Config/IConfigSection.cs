namespace Deadswitch.Sim.Config
{
    /// <summary>A group of related tunables. Implementations declare each value exactly once in <see cref="Visit"/>.</summary>
    public interface IConfigSection
    {
        void Visit(IConfigVisitor visitor);
    }
}
