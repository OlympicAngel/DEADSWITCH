using Deadswitch.Sim.Rng;

namespace Deadswitch.Sim.State
{
    /// <summary>
    /// All mutable sim state. Every field must be declared in <see cref="Visit"/>, which drives both the
    /// state hash and the save format (ADR-0008). Public fields so the visitor can take them by ref.
    /// </summary>
    public sealed class GameState
    {
        public long Tick;

        public int Energy;

        public int Fuel;

        public int Compute;

        public int People;

        public int Corruption;

        public int RaidsToday;

        public DelegationLevel Delegation;

        public Pcg32 Rng;

        public GameState(ulong seed, SimConfig config)
        {
            Rng = Pcg32.Create(seed);
            Energy = config.Energy.Start;
            Fuel = config.Fuel.Start;
            Compute = config.Compute.Start;
            People = config.People.Start;
        }

        /// <summary>
        /// Visits every field in a fixed order. Append new fields at the end of their group and bump
        /// <c>SaveGame.FormatVersion</c> with a migration when the layout changes.
        /// </summary>
        public void Visit(IStateVisitor v)
        {
            v.Long(ref Tick);
            v.Int(ref Energy);
            v.Int(ref Fuel);
            v.Int(ref Compute);
            v.Int(ref People);
            v.Int(ref Corruption);
            v.Int(ref RaidsToday);

            int delegation = (int)Delegation;
            v.Int(ref delegation);
            Delegation = (DelegationLevel)delegation;

            ulong rngState = Rng.State;
            ulong rngInc = Rng.Inc;
            v.ULong(ref rngState);
            v.ULong(ref rngInc);
            if (v.IsReading)
            {
                Rng = Pcg32.Restore(rngState, rngInc);
            }
        }
    }
}
