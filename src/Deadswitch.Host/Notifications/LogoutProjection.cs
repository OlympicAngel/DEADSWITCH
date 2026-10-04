using System.Collections.Generic;
using Deadswitch.Host.Narrative;
using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.Persistence;
using Deadswitch.Sim.State;

namespace Deadswitch.Host.Notifications
{
    /// <summary>Alert categories the handler can switch (SPEC-010 rule 2).</summary>
    [System.Flags]
    public enum AlertKinds
    {
        None = 0,
        Raids = 1,
        Construction = 2,
        Research = 4,
        Power = 8,
        All = Raids | Construction | Research | Power,
    }

    /// <summary>One local notification: game minutes from logout, title and body in the AI's voice.</summary>
    public readonly struct ProjectedAlert
    {
        public ProjectedAlert(long inMinutes, AlertKinds kind, string title, string body)
        {
            InMinutes = inMinutes;
            Kind = kind;
            Title = title;
            Body = body;
        }

        public long InMinutes { get; }

        public AlertKinds Kind { get; }

        public string Title { get; }

        public string Body { get; }
    }

    /// <summary>
    /// Logout projection (ADR-0004 decision 3, SPEC-010): runs a copy of the sim forward with the handler away and
    /// turns what happens into a few short alerts. The live sim is never touched; the forecast uses the AI's own
    /// estimates, so it can be wrong when the core is corrupted.
    /// </summary>
    public static class LogoutProjection
    {
        public static List<ProjectedAlert> Project(Simulation live, AlertKinds kinds)
        {
            var alerts = new List<ProjectedAlert>();
            int max = live.Config.Host.NotifyMax;
            if (kinds == AlertKinds.None || max <= 0)
            {
                return alerts;
            }

            Simulation copy = SaveGame.Load(SaveGame.Write(live), live.Config).Simulation;
            copy.Execute(Command.SetPresence(true));
            long start = copy.State.Tick;
            int from = copy.Log.Count;
            copy.Run((long)live.Config.Host.NotifyProjectHours * SimConfig.TicksPerHour);

            IReadOnlyList<SimEvent> log = copy.Log.Events;
            int warnMinutes = 0;
            int warnEstimate = 0;
            AttackKind warnKind = AttackKind.Raid;
            for (int i = from; i < log.Count && alerts.Count < max; i++)
            {
                SimEvent e = log[i];
                long at = e.Tick - start;
                switch (e.Kind)
                {
                    case EventKind.RaidWarning:
                        warnMinutes = e.B;
                        warnEstimate = e.C;
                        warnKind = (AttackKind)e.D;
                        break;
                    case EventKind.RaidVector when (kinds & AlertKinds.Raids) != 0:
                        alerts.Add(new ProjectedAlert(at, AlertKinds.Raids, Names.Attack(warnKind) + " INBOUND", "Hostiles at the " + Names.Gate((RaidGate)e.B) + ". My estimate: " + warnEstimate + ". Contact in " + warnMinutes + " min."));
                        break;
                    case EventKind.PurgeLadder when (kinds & AlertKinds.Raids) != 0 && e.A == (int)PurgeStage.Staging:
                        alerts.Add(new ProjectedAlert(at, AlertKinds.Raids, "PURGE STAGING", "A purge force is gathering. It strikes in " + (e.B / 60) + " hours unless you answer it."));
                        break;
                    case EventKind.PurgeLadder when (kinds & AlertKinds.Raids) != 0 && e.A == (int)PurgeStage.Ultimatum:
                        alerts.Add(new ProjectedAlert(at, AlertKinds.Raids, "PURGE ULTIMATUM", "Pay, retreat or prepare. " + (e.B / 60) + " hours."));
                        break;
                    case EventKind.UltimatumIssued when (kinds & AlertKinds.Raids) != 0:
                        alerts.Add(new ProjectedAlert(at, AlertKinds.Raids, "WARLORD ULTIMATUM", "Mother Kess wants " + e.B + " energy and " + e.C + " fuel. " + (e.A / 60) + " hours, or her wave comes."));
                        break;
                    case EventKind.CrisisStruck when (kinds & AlertKinds.Raids) != 0:
                        alerts.Add(new ProjectedAlert(at, AlertKinds.Raids, "CORE CRISIS", "I am failing. Come back and flush me before it spreads."));
                        break;
                    case EventKind.BuildCompleted when (kinds & AlertKinds.Construction) != 0:
                        alerts.Add(new ProjectedAlert(at, AlertKinds.Construction, "CONSTRUCTION COMPLETE", Names.Facility((FacilityKind)e.B) + " level " + e.C + " is online."));
                        break;
                    case EventKind.ResearchCompleted when (kinds & AlertKinds.Research) != 0:
                        alerts.Add(new ProjectedAlert(at, AlertKinds.Research, "MODULE RESTORED", "Module " + (ModuleNode)e.A + " is back. I remember a little more."));
                        break;
                    case EventKind.BlackoutStarted when (kinds & AlertKinds.Power) != 0:
                        alerts.Add(new ProjectedAlert(at, AlertKinds.Power, "POWER FAILURE", "The core is dark. Nothing runs until power returns."));
                        break;
                }
            }

            return alerts;
        }
    }
}
