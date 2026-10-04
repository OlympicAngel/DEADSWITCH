using System.Collections.Generic;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Host.Reports
{
    /// <summary>The Audit's Core Profile (doc 10 s1.4, SPEC-007): the truth as of the last Audit in the log.</summary>
    public sealed class CoreProfile
    {
        private CoreProfile()
        {
        }

        public long Tick { get; private set; }

        public ProjectStage Stage { get; private set; }

        public int ColdnessMilli { get; private set; }

        public int BoldnessMilli { get; private set; }

        public int TrueCorruptionMilli { get; private set; }

        /// <summary>Compute the AI skimmed in the period this Audit covered.</summary>
        public int Skimmed { get; private set; }

        /// <summary>Unverified reports that hide a lie.</summary>
        public int UnverifiedLies { get; private set; }

        /// <summary>The most recent Audit, or null if the handler never ran one.</summary>
        public static CoreProfile? Latest(IReadOnlyList<SimEvent> log)
        {
            for (int i = log.Count - 1; i >= 0; i--)
            {
                if (log[i].Kind != EventKind.AuditDrain)
                {
                    continue;
                }

                SimEvent drain = log[i];
                SimEvent run = i > 0 ? log[i - 1] : default;
                if (run.Kind != EventKind.AuditRun)
                {
                    return null;
                }

                return new CoreProfile
                {
                    Tick = run.Tick,
                    Stage = (ProjectStage)run.A,
                    ColdnessMilli = run.B,
                    BoldnessMilli = run.C,
                    TrueCorruptionMilli = run.D,
                    Skimmed = drain.A,
                    UnverifiedLies = drain.B,
                };
            }

            return null;
        }
    }
}
