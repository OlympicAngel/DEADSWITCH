using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Host.Dev
{
    /// <summary>
    /// A sensible scripted handler for balancing runs, previews and development fast-forward: keeps net power
    /// positive, builds storage, people, defense and compute, upgrades steadily, and sets a defense posture
    /// when a raid is announced. Uses only public commands (no state edits).
    /// </summary>
    public static class ScriptedPlayer
    {
        /// <summary>Runs <paramref name="ticks"/> minutes, deciding every <paramref name="thinkEvery"/> minutes.</summary>
        public static void Play(Simulation sim, long ticks, int thinkEvery = 20)
        {
            long end = sim.State.Tick + ticks;
            while (sim.State.Tick < end)
            {
                Think(sim);
                long step = System.Math.Min(thinkEvery, end - sim.State.Tick);
                sim.Run(step);
            }
        }

        public static void Think(Simulation sim)
        {
            GameState s = sim.State;
            if (s.RaidId != 0)
            {
                sim.Execute(Command.SetGarrison(System.Math.Min(sim.Config.Defense.GarrisonSlots, s.People / 4)));
                sim.Execute(Command.SetPosture(Posture.Turtle));
            }
            else if (s.Posture != Posture.None)
            {
                sim.Execute(Command.SetPosture(Posture.None));
                sim.Execute(Command.SetGarrison(0));
            }

            if (s.Jobs.Count >= sim.Config.Build.QueueSlots)
            {
                return;
            }

            EconomyFlows f = Economy.Flows(s, sim.Config);
            if (f.NetEnergyPerHour < 150 && TryUpgrade(sim, FacilityKind.Generator))
            {
                return;
            }

            FacilityKind[] wanted = { FacilityKind.BatteryBank, FacilityKind.Turret, FacilityKind.LifeSupport, FacilityKind.Generator };
            foreach (FacilityKind kind in wanted)
            {
                if (Economy.CountOfKind(s, kind) == 0 && f.NetEnergyPerHour > Upkeep(sim, kind) + 40)
                {
                    int slot = s.Slots.FindIndex(x => x.IsEmpty && s.JobForSlot(s.Slots.IndexOf(x)) == null);
                    if (slot >= 0 && sim.Execute(Command.Build(slot, kind)).Accepted)
                    {
                        return;
                    }
                }
            }

            FacilityKind[] upgrades = { FacilityKind.BatteryBank, FacilityKind.ServerRack, FacilityKind.Turret, FacilityKind.LifeSupport, FacilityKind.Generator };
            foreach (FacilityKind kind in upgrades)
            {
                if (f.NetEnergyPerHour > 260 && TryUpgrade(sim, kind))
                {
                    return;
                }
            }
        }

        private static int Upkeep(Simulation sim, FacilityKind kind)
        {
            return sim.Config.Facility(kind)!.UpkeepPerHour[0];
        }

        private static bool TryUpgrade(Simulation sim, FacilityKind kind)
        {
            GameState s = sim.State;
            int best = -1;
            for (int i = 0; i < s.Slots.Count; i++)
            {
                if (s.Slots[i].Kind == kind && (best < 0 || s.Slots[i].Level < s.Slots[best].Level))
                {
                    best = i;
                }
            }

            return best >= 0 && sim.Execute(Command.Upgrade(best)).Accepted;
        }
    }
}
