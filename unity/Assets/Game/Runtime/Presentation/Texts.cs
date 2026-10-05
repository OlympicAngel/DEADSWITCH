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
                case FacilityKind.Reactor: return "+" + Fmt.Num(value) + " ENERGY/H";
                case FacilityKind.ServerRack: return "+" + Fmt.Num(value) + " COMPUTE/H";
                case FacilityKind.LifeSupport: return "+" + Fmt.Num(value) + " PEOPLE CAP";
                case FacilityKind.BatteryBank: return "+" + Fmt.Num(value) + " ENERGY STORAGE";
                case FacilityKind.Turret: return Fmt.Num(value) + " DEFENSE";
                case FacilityKind.DroneBay: return Fmt.Num(value) + " DRONE DEFENSE";
                case FacilityKind.MotorPool: return Fmt.Num(value) + " VEHICLE DEFENSE";
                case FacilityKind.SolarField: return "+" + Fmt.Num(value) + " ENERGY/H BY DAY";
                case FacilityKind.FuelDepot: return "+" + Fmt.Num(value) + " FUEL STORAGE";
                case FacilityKind.CoolingTower: return "-" + Fmt.Num(value) + "% COMPUTE STRAIN";
                case FacilityKind.MemoryChamber: return Fmt.Num(value) + "% FASTER MEMORY";
                default: return string.Empty;
            }
        }

        public static string Reason(RejectReason reason)
        {
            switch (reason)
            {
                case RejectReason.NotEnoughEnergy: return "Insufficient energy. Wait for the cells, or shed load.";
                case RejectReason.NotEnoughCompute: return "Insufficient compute. My racks need time.";
                case RejectReason.NotEnoughFuel: return "Not enough fuel.";
                case RejectReason.NothingPending: return "Nothing is waiting on that.";
                case RejectReason.FactionHostile: return "They have marked us. They will not trade.";
                case RejectReason.TradeCap: return "They have traded enough with us today.";
                case RejectReason.StorageFull: return "No room for it. Storage or beds are full.";
                case RejectReason.NotDamaged: return "Nothing there needs repair.";
                case RejectReason.NeedsFragment: return "That one I cannot rebuild from nothing. Bring me a data fragment from a dead data center.";
                case RejectReason.AiTakeover: return "I have the controls. Flush me if you want them back.";
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
                case RejectReason.Locked: return "Locked. It needs a higher tier or the module before it.";
                case RejectReason.Excluded: return "You chose the other path. I cannot hold both.";
                case RejectReason.AlreadyRestored: return "Already restored.";
                case RejectReason.ResearchBusy: return "One restoration at a time. My memory is fragile.";
                case RejectReason.GateBuild: return "The Hub is not ready: more facility levels or more surplus power.";
                case RejectReason.GateModule: return "My memory is not ready. Restore the trunk module first.";
                case RejectReason.GatePeople: return "Not enough free people to send out.";
                case RejectReason.MaxTier: return "This is as far as I can see.";
                case RejectReason.Silenced: return "You silenced me. I will not run anything until it wears off.";
                case RejectReason.NeedsAudit: return "Run an Audit first. You cannot cancel what you have not seen.";
                case RejectReason.LoyaltyHolds: return "Loyalty holds. There is no unrest to put down.";
                case RejectReason.NoShield: return "The shield is spent. It recharges within the month.";
                case RejectReason.ThreatActive: return "Too late for the shield. They are already coming.";
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
