namespace Deadswitch.Sim.State
{
    /// <summary>
    /// Walks every field of <see cref="GameState"/> in a fixed order. The same walk drives the state hash
    /// and the save serializer, so a field declared in <see cref="GameState.Visit"/> is automatically hashed
    /// and saved. Readers overwrite the referenced values; writers and hashers only read them.
    /// </summary>
    public interface IStateVisitor
    {
        /// <summary>True when values are being loaded (the visitor writes into the refs).</summary>
        bool IsReading { get; }

        /// <summary>State layout version being visited (<see cref="GameState.LayoutVersion"/> unless reading an older save).</summary>
        int Version { get; }

        void Int(ref int value);

        void Long(ref long value);

        void ULong(ref ulong value);

        void Bool(ref bool value);

        /// <summary>
        /// Length of the collection that follows. Writers record <paramref name="count"/> and return it;
        /// readers return the stored count so the caller can resize before visiting the elements.
        /// </summary>
        int Count(int count);
    }
}
