using System.Collections.Generic;
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

        /// <summary>True while the AI core itself is unpowered (SPEC-002 rule 5).</summary>
        public bool Blackout;

        /// <summary>Unmanned facilities run by the AI this tick (SPEC-002 rule 6). Feeds corruption in M2.</summary>
        public int AutomationLoad;

        /// <summary>Hub slots; the index is the slot id.</summary>
        public List<FacilitySlot> Slots = new List<FacilitySlot>();

        /// <summary>Construction queue in start order.</summary>
        public List<BuildJob> Jobs = new List<BuildJob>();

        /// <summary>Slot ids from highest to lowest power and crew priority. Always a permutation of all slots.</summary>
        public List<int> PowerPriority = new List<int>();

        public GameState(ulong seed, SimConfig config)
        {
            Rng = Pcg32.Create(seed);
            Energy = config.Energy.Start;
            Fuel = config.Fuel.Start;
            Compute = config.Compute.Start;
            People = config.People.Start;

            for (int i = 0; i < config.Hub.Slots; i++)
            {
                Slots.Add(new FacilitySlot());
                PowerPriority.Add(i);
            }

            // Starting layout (SPEC-002 rule 2): reproduces the doc 10 s3 starting rates.
            Slots[0].Kind = FacilityKind.Generator;
            Slots[0].Level = 1;
            Slots[0].Powered = true;
            Slots[0].Staffed = true;
            Slots[1].Kind = FacilityKind.ServerRack;
            Slots[1].Level = 1;
            Slots[1].Powered = true;
            Slots[1].Staffed = true;
        }

        /// <summary>Job working on a slot, or null.</summary>
        public BuildJob? JobForSlot(int slot)
        {
            foreach (BuildJob job in Jobs)
            {
                if (job.Slot == slot)
                {
                    return job;
                }
            }

            return null;
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

            v.Bool(ref Blackout);
            v.Int(ref AutomationLoad);

            int slotCount = v.Count(Slots.Count);
            Resize(Slots, slotCount);
            foreach (FacilitySlot slot in Slots)
            {
                slot.Visit(v);
            }

            int jobCount = v.Count(Jobs.Count);
            Resize(Jobs, jobCount);
            foreach (BuildJob job in Jobs)
            {
                job.Visit(v);
            }

            int priorityCount = v.Count(PowerPriority.Count);
            while (PowerPriority.Count < priorityCount)
            {
                PowerPriority.Add(0);
            }

            if (PowerPriority.Count > priorityCount)
            {
                PowerPriority.RemoveRange(priorityCount, PowerPriority.Count - priorityCount);
            }

            for (int i = 0; i < PowerPriority.Count; i++)
            {
                int slotId = PowerPriority[i];
                v.Int(ref slotId);
                PowerPriority[i] = slotId;
            }
        }

        private static void Resize<T>(List<T> list, int count)
            where T : new()
        {
            while (list.Count < count)
            {
                list.Add(new T());
            }

            if (list.Count > count)
            {
                list.RemoveRange(count, list.Count - count);
            }
        }
    }
}
