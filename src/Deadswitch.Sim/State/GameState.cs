using System.Collections.Generic;
using Deadswitch.Sim.Rng;

namespace Deadswitch.Sim.State
{
    /// <summary>
    /// All mutable sim state. Every field must be declared in <see cref="Visit"/>, which drives both the
    /// state hash and the save format (ADR-0009). Public fields so the visitor can take them by ref.
    /// </summary>
    public sealed class GameState
    {
        /// <summary>Field layout version (save format). v2: AI dials, raid gates, lies, planner hold (SPEC-004).</summary>
        public const int LayoutVersion = 2;

        public long Tick;

        public int Energy;

        public int Fuel;

        public int Compute;

        public int People;

        /// <summary>0..100_000 (100_000 = 100%).</summary>
        public int CorruptionMilli;

        public int RaidsToday;

        public DelegationLevel Delegation;

        public Pcg32 Rng;

        /// <summary>True while the AI core itself is unpowered (SPEC-002 rule 5).</summary>
        public bool Blackout;

        /// <summary>Unmanned facilities run by the AI this tick (SPEC-002 rule 6). Feeds corruption in M2.</summary>
        public int AutomationLoad;

        public int OverrideCharges;

        public long OverrideNextChargeTick;

        public long OverrideCooldownUntil;

        public Posture Posture;

        /// <summary>People posted as defenders (not available as crew).</summary>
        public int Garrison;

        /// <summary>Handler is away (host sends SetPresence at logout/login).</summary>
        public bool Away;

        /// <summary>Id the next raid receives.</summary>
        public int NextRaidId = 1;

        /// <summary>Id of the incoming raid, or 0.</summary>
        public int RaidId;

        public long RaidArriveTick;

        /// <summary>True strength before variance.</summary>
        public int RaidStrength;

        /// <summary>What the AI told the handler (may be wrong when corrupted).</summary>
        public int RaidEstimate;

        public long MercyUntilTick;

        /// <summary>Hidden dial (doc 10 s1.4), 0..100_000. Raised by ruthless choices.</summary>
        public int ColdnessMilli;

        /// <summary>Hidden dial (doc 10 s1.4), 0..100_000. Raised by reliance on delegation.</summary>
        public int BoldnessMilli;

        /// <summary>Gate the incoming raid really uses.</summary>
        public RaidGate RaidGate;

        /// <summary>Gate the AI reported for the incoming raid.</summary>
        public RaidGate RaidGateReported;

        /// <summary>Lies told this run (the first lie is special, doc 10 s7.4).</summary>
        public int LiesTold;

        /// <summary>The delegated planner waits until this tick (set when the handler cancels a job).</summary>
        public long PlanHoldUntilTick;

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
            OverrideCharges = config.Override.StartCharges;
            OverrideNextChargeTick = config.Override.RegenMinutes;

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
            v.Int(ref CorruptionMilli);
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
            v.Int(ref OverrideCharges);
            v.Long(ref OverrideNextChargeTick);
            v.Long(ref OverrideCooldownUntil);
            int posture = (int)Posture;
            v.Int(ref posture);
            Posture = (Posture)posture;
            v.Int(ref Garrison);
            v.Bool(ref Away);
            v.Int(ref NextRaidId);
            v.Int(ref RaidId);
            v.Long(ref RaidArriveTick);
            v.Int(ref RaidStrength);
            v.Int(ref RaidEstimate);
            v.Long(ref MercyUntilTick);
            if (v.Version >= 2)
            {
                v.Int(ref ColdnessMilli);
                v.Int(ref BoldnessMilli);
                int gate = (int)RaidGate;
                v.Int(ref gate);
                RaidGate = (RaidGate)gate;
                int reported = (int)RaidGateReported;
                v.Int(ref reported);
                RaidGateReported = (RaidGate)reported;
                v.Int(ref LiesTold);
                v.Long(ref PlanHoldUntilTick);
            }
            else if (v.IsReading && RaidId != 0)
            {
                // v1 save with a raid incoming: give it a gate (no RNG draw) and report it truthfully.
                RaidGate = (RaidGate)(1 + (int)(Systems.SimMath.Hash((uint)RaidId, (uint)RaidArriveTick) % 4));
                RaidGateReported = RaidGate;
            }

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
