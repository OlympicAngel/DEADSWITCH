using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;

namespace Deadswitch.Game.Presentation
{
    /// <summary>Player-facing explanations in the advisor's register (ADVISOR_VOICE.md): status, then comment.</summary>
    public static class Texts
    {
        /// <summary>What a facility's output number means, e.g. "+480 ENERGY/H".</summary>
        public static string Output(FacilityKind kind, int value)
        {
            switch (kind)
            {
                case FacilityKind.Generator: return "+" + Fmt.Num(value) + " ENERGY/H";
                case FacilityKind.ServerRack: return "+" + Fmt.Num(value) + " COMPUTE/H";
                case FacilityKind.LifeSupport: return "+" + Fmt.Num(value) + " PEOPLE CAP";
                case FacilityKind.BatteryBank: return "+" + Fmt.Num(value) + " ENERGY STORAGE";
                case FacilityKind.Turret: return Fmt.Num(value) + " DEFENSE";
                default: return string.Empty;
            }
        }

        public static string Reason(RejectReason reason)
        {
            switch (reason)
            {
                case RejectReason.NotEnoughEnergy: return "Insufficient energy. Wait for the cells, or shed load.";
                case RejectReason.NotEnoughCompute: return "Insufficient compute. My racks need time.";
                case RejectReason.QueueFull: return "Construction crew is busy. One job at a time.";
                case RejectReason.JobInProgress: return "Work already underway on this plot.";
                case RejectReason.MaxLevel: return "This is as far as this design goes.";
                case RejectReason.SlotOccupied: return "Plot occupied.";
                case RejectReason.SlotEmpty: return "Nothing built here.";
                case RejectReason.NoCharges: return "No OVERRIDE charge left.";
                case RejectReason.OnCooldown: return "OVERRIDE cooling down.";
                case RejectReason.NoTarget: return "Nothing to act on.";
                case RejectReason.NotEnoughPeople: return "Not enough people.";
                case RejectReason.NoChange: return "Already set.";
                case RejectReason.NoReport: return "That record has been purged. I keep the last ten.";
                case RejectReason.AlreadyVerified: return "Already verified. The record stands.";
                default: return "Command refused.";
            }
        }

        /// <summary>Advisor confirmation after an accepted command.</summary>
        public static string Ack(Command c)
        {
            switch (c.Kind)
            {
                case CommandKind.Build: return "Construction started. " + Fmt.FacilityName((FacilityKind)c.B) + ".";
                case CommandKind.Upgrade: return "Upgrade underway.";
                case CommandKind.CancelJob: return "Job cancelled. Half the materials salvaged.";
                case CommandKind.Demolish: return "Demolished. Scrap recovered.";
                case CommandKind.SetFacilityPower: return c.B == 1 ? "Power restored to the unit." : "Unit powered down.";
                case CommandKind.SetPriority: return "Power order updated.";
                default: return "Done.";
            }
        }
    }
}
