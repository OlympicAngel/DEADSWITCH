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
                case FacilityKind.Reactor: return "REACTOR";
                case FacilityKind.DroneBay: return "DRONE BAY";
                case FacilityKind.MotorPool: return "MOTOR POOL";
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

        /// <summary>Faction display name (doc 10 s5 working names).</summary>
        public static string Faction(Faction faction)
        {
            switch (faction)
            {
                case Sim.State.Faction.Vanguard: return "VANGUARD COMMAND";
                case Sim.State.Faction.Church: return "CHURCH OF THE LAST SIGNAL";
                case Sim.State.Faction.Holdouts: return "HALCYON DYNAMICS";
                default: return "RUSTBORN";
            }
        }

        /// <summary>Signature name (SPEC-015).</summary>
        public static string Attack(AttackKind kind)
        {
            switch (kind)
            {
                case AttackKind.Siege: return "SIEGE";
                case AttackKind.Purge: return "PURGE";
                case AttackKind.Warlord: return "WARLORD WAVE";
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
