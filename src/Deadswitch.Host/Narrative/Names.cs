using Deadswitch.Sim.State;

namespace Deadswitch.Host.Narrative
{
    /// <summary>Player-facing names (terminal register, upper case) shared by the advisor and the Unity UI.</summary>
    public static class Names
    {
        public static string Facility(FacilityKind kind)
        {
            switch (kind)
            {
                case FacilityKind.Generator: return "GENERATOR";
                case FacilityKind.ServerRack: return "SERVER RACK";
                case FacilityKind.LifeSupport: return "LIFE SUPPORT";
                case FacilityKind.BatteryBank: return "BATTERY BANK";
                case FacilityKind.Turret: return "TURRET";
                default: return "EMPTY SLOT";
            }
        }

        public static string Gate(RaidGate gate)
        {
            switch (gate)
            {
                case RaidGate.North: return "NORTH RIDGE";
                case RaidGate.South: return "SOUTH GATE";
                case RaidGate.East: return "EAST WALL";
                case RaidGate.West: return "WEST WALL";
                default: return "UNKNOWN VECTOR";
            }
        }

        /// <summary>Signature name (SPEC-015).</summary>
        public static string Attack(AttackKind kind)
        {
            switch (kind)
            {
                case AttackKind.Siege: return "SIEGE";
                case AttackKind.Purge: return "PURGE";
                default: return "RAID";
            }
        }

        public static string Posture(Posture posture)
        {
            switch (posture)
            {
                case Sim.State.Posture.Turtle: return "TURTLE, FULL GARRISON";
                case Sim.State.Posture.Dark: return "DARK";
                case Sim.State.Posture.Evacuate: return "EVACUATE";
                default: return "NONE";
            }
        }
    }
}
