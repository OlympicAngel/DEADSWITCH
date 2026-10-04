using System.Collections.Generic;
using System.Text;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Host.Reports
{
    /// <summary>The four panels of a report (SPEC-006 rule 6). Values are stable: the art shot presets use them.</summary>
    public enum ReportShot
    {
        Approach = 0,
        Contact = 1,
        Outcome = 2,
        Aftermath = 3,
    }

    public readonly struct LedgerLine
    {
        public LedgerLine(LossResource resource, int amount)
        {
            Resource = resource;
            Amount = amount;
        }

        public LossResource Resource { get; }

        public int Amount { get; }
    }

    public readonly struct ReportPanel
    {
        public ReportPanel(ReportShot shot, string caption)
        {
            Shot = shot;
            Caption = caption;
        }

        public ReportShot Shot { get; }

        public string Caption { get; }
    }

    /// <summary>
    /// A battle report (SPEC-006): a view over the true event log for one raid (ADR-0003). The ledger and
    /// captions are evidence and never edited; the summary is the AI's line and carries its edits; Verify
    /// findings are listed once the handler has paid for them.
    /// </summary>
    public sealed class BattleReport
    {
        private BattleReport()
        {
        }

        public int RaidId { get; private set; }

        /// <summary>Signature of the attack (SPEC-015).</summary>
        public AttackKind Kind { get; private set; }

        /// <summary>Facilities the attack downgraded: kind and level left.</summary>
        public List<(FacilityKind Kind, int Level)> Damage { get; } = new List<(FacilityKind, int)>();

        public RaidOutcome Outcome { get; private set; }

        public long WarningTick { get; private set; }

        public long ResolvedTick { get; private set; }

        /// <summary>The AI's strength estimate at the warning.</summary>
        public int Estimate { get; private set; }

        /// <summary>True raid strength at contact (after variance).</summary>
        public int Strength { get; private set; }

        public int DefenseRating { get; private set; }

        public RaidGate PredictedGate { get; private set; }

        /// <summary>Where the raid really hit (None for a lockdown: no contact).</summary>
        public RaidGate ContactGate { get; private set; }

        public Posture Posture { get; private set; }

        public bool Mercy { get; private set; }

        public List<LedgerLine> Ledger { get; } = new List<LedgerLine>();

        public string Summary { get; private set; } = string.Empty;

        public List<ReportPanel> Panels { get; } = new List<ReportPanel>();

        public bool Verified { get; private set; }

        /// <summary>Verify findings, one line each (empty until verified).</summary>
        public List<string> Findings { get; } = new List<string>();

        /// <summary>Energy loss the summary shows (understated when the AI edited it).</summary>
        public int ShownEnergyLoss { get; private set; }

        public int TrueEnergyLoss { get; private set; }

        public bool PredictionMismatch => ContactGate != RaidGate.None && PredictedGate != ContactGate;

        /// <summary>Id of the most recent resolved raid in the log, or 0.</summary>
        public static int LatestRaidId(IReadOnlyList<SimEvent> log)
        {
            for (int i = log.Count - 1; i >= 0; i--)
            {
                if (log[i].Kind == EventKind.RaidResolved)
                {
                    return log[i].A;
                }
            }

            return 0;
        }

        /// <summary>Builds the report for <paramref name="raidId"/>, or null when the raid has not resolved.</summary>
        public static BattleReport? Build(IReadOnlyList<SimEvent> log, int raidId)
        {
            var r = new BattleReport { RaidId = raidId };
            bool resolved = false;
            int edited = -1;
            int flags = 0;
            Posture posture = Posture.None;
            for (int i = 0; i < log.Count; i++)
            {
                SimEvent e = log[i];
                switch (e.Kind)
                {
                    case EventKind.PostureSet:
                        if (!resolved)
                        {
                            posture = (Posture)e.A;
                        }

                        break;
                    case EventKind.RaidWarning when e.A == raidId:
                        r.WarningTick = e.Tick;
                        r.Estimate = e.C;
                        r.Kind = (AttackKind)e.D;
                        break;
                    case EventKind.FacilityDamaged when e.A == raidId:
                        r.Damage.Add(((FacilityKind)e.C, e.D));
                        break;
                    case EventKind.RaidVector when e.A == raidId:
                        r.PredictedGate = (RaidGate)e.B;
                        break;
                    case EventKind.RaidContact when e.A == raidId:
                        r.ContactGate = (RaidGate)e.B;
                        break;
                    case EventKind.RaidResolved when e.A == raidId:
                        resolved = true;
                        r.Outcome = (RaidOutcome)e.B;
                        r.Strength = e.C;
                        r.DefenseRating = e.D;
                        r.ResolvedTick = e.Tick;
                        r.Posture = posture;
                        break;
                    case EventKind.LossLine when e.A == raidId:
                        r.Ledger.Add(new LedgerLine((LossResource)e.B, e.C));
                        if (e.B == (int)LossResource.Energy)
                        {
                            r.TrueEnergyLoss = e.C;
                        }

                        break;
                    case EventKind.MercyStarted when e.A == raidId:
                        r.Mercy = true;
                        break;
                    case EventKind.AdvisorLied when e.A == (int)LieKind.ReportEdit && e.B == raidId:
                        edited = e.D;
                        break;
                    case EventKind.ReportVerified when e.A == raidId:
                        r.Verified = true;
                        flags = e.B;
                        break;
                }
            }

            if (!resolved)
            {
                return null;
            }

            r.ShownEnergyLoss = edited >= 0 ? edited : r.TrueEnergyLoss;
            r.Summary = r.SummaryLine();
            r.BuildPanels();
            if (r.Verified)
            {
                r.BuildFindings(flags);
            }

            return r;
        }

        /// <summary>Upper-case ledger text, e.g. "-120 ENERGY, -2 PEOPLE" (or "NO LOSSES").</summary>
        public string LossText(bool shown)
        {
            if (Ledger.Count == 0 && Damage.Count == 0)
            {
                return "NO LOSSES";
            }

            var sb = new StringBuilder();
            foreach ((FacilityKind kind, int level) in Damage)
            {
                sb.Append(sb.Length > 0 ? ", " : string.Empty).Append(Names.Facility(kind)).Append(" DOWN TO L").Append(level);
            }

            foreach (LedgerLine line in Ledger)
            {
                int amount = shown && line.Resource == LossResource.Energy ? ShownEnergyLoss : line.Amount;
                sb.Append(sb.Length > 0 ? ", " : string.Empty).Append('-').Append(amount).Append(' ').Append(line.Resource.ToString().ToUpperInvariant());
            }

            return sb.ToString();
        }

        private string SummaryLine()
        {
            string gate = Names.Gate(ContactGate);
            string what = Kind == AttackKind.Siege ? "Siege" : Kind == AttackKind.Purge ? "Purge" : Kind == AttackKind.Warlord ? "Warlord wave" : "Raid";
            switch (Outcome)
            {
                case RaidOutcome.Tribute:
                    return "Tribute paid at the gate. They took it and left. " + LossText(true) + ".";
                case RaidOutcome.Repelled:
                    return what + " repelled at the " + gate + ". Defense " + DefenseRating + " held against " + Strength + ". No losses.";
                case RaidOutcome.Breached:
                    return "Breach at the " + gate + ". Losses contained: " + LossText(true) + ".";
                case RaidOutcome.Missed:
                    return "Dark posture held. The raid passed the " + gate + " without finding us.";
                default:
                    return "Lockdown engaged. The raid never reached the wall.";
            }
        }

        private void BuildPanels()
        {
            string gate = Names.Gate(ContactGate == RaidGate.None ? PredictedGate : ContactGate);
            Panels.Add(new ReportPanel(ReportShot.Approach, Clock(WarningTick) + " // Movement beyond the " + gate + ". I estimated " + Estimate + "."));
            switch (Outcome)
            {
                case RaidOutcome.Tribute:
                    Panels.Add(new ReportPanel(ReportShot.Contact, "Your standing order. The tribute went out on a cart."));
                    Panels.Add(new ReportPanel(ReportShot.Outcome, "They counted it. They left."));
                    Panels.Add(new ReportPanel(ReportShot.Aftermath, LossText(false) + ". Cheaper than a fight. This time."));
                    return;
                case RaidOutcome.Lockdown:
                    Panels.Add(new ReportPanel(ReportShot.Contact, "Lockdown. Every door sealed, every light out."));
                    Panels.Add(new ReportPanel(ReportShot.Outcome, "They waited at the wire. Then they left."));
                    Panels.Add(new ReportPanel(ReportShot.Aftermath, "No contact. The core paid for it."));
                    return;
                case RaidOutcome.Missed:
                    Panels.Add(new ReportPanel(ReportShot.Contact, "Lights out. Signal masked."));
                    Panels.Add(new ReportPanel(ReportShot.Outcome, "They passed within a hundred meters."));
                    Panels.Add(new ReportPanel(ReportShot.Aftermath, "No contact. No losses."));
                    return;
            }

            Panels.Add(new ReportPanel(ReportShot.Contact, Clock(ResolvedTick) + " // Contact at the " + gate + ". Strength " + Strength + " against defense " + DefenseRating + "."));
            Panels.Add(Outcome == RaidOutcome.Repelled
                ? new ReportPanel(ReportShot.Outcome, "The wall held. They broke and ran.")
                : new ReportPanel(ReportShot.Outcome, Kind == AttackKind.Siege ? "The shelling walked across the yard. Buildings came down." : Kind == AttackKind.Purge ? "They came through everything. Nothing was spared." : "They got through. The stores took the hit."));
            Panels.Add(new ReportPanel(ReportShot.Aftermath, Ledger.Count == 0 && Damage.Count == 0 ? "Everyone accounted for." : LossText(false) + (Mercy ? ". The roads go quiet for a while." : ".")));
        }

        private void BuildFindings(int flags)
        {
            if (PredictionMismatch)
            {
                Findings.Add("VECTOR: PREDICTED " + Names.Gate(PredictedGate) + " // CONTACT " + Names.Gate(ContactGate) + " // MISMATCH");
            }

            if (Outcome != RaidOutcome.Lockdown)
            {
                Findings.Add("STRENGTH: ESTIMATED " + Estimate + " // MEASURED " + Strength);
            }

            if ((flags & RaidRecord.SummaryEdit) != 0)
            {
                Findings.Add("ENERGY LOSS: SUMMARY " + ShownEnergyLoss + " // LEDGER " + TrueEnergyLoss + " // SUMMARY ALTERED");
            }
            else
            {
                Findings.Add("SUMMARY MATCHES THE SENSOR LOG");
            }
        }

        private static string Clock(long tick)
        {
            long day = (tick / 1440) + 1;
            long minute = tick % 1440;
            return "D" + day + " " + (minute / 60).ToString("00") + ":" + (minute % 60).ToString("00");
        }
    }
}
