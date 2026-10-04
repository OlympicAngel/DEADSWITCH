using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Host.Dev
{
    /// <summary>
    /// A sensible scripted handler for balancing runs, previews and development fast-forward: keeps net power
    /// positive, builds storage, people, defense and compute, upgrades steadily, researches modules, tiers up
    /// when every gate is clear, and sets a defense posture when a raid is announced. Uses only public commands
    /// (no state edits).
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

            Care(sim);
            Research(sim);
            TierGates gates = Modules.Gates(s, sim.Config);
            if (gates.Build && gates.ModuleRestored && gates.PeopleAvailable)
            {
                sim.Execute(Command.TierUp());
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

            // plots beyond the first set (the district): alternate power and compute
            int free = FreeSlot(s);
            if (free >= 0 && f.NetEnergyPerHour > 300)
            {
                FacilityKind kind = Economy.CountOfKind(s, FacilityKind.Generator) <= Economy.CountOfKind(s, FacilityKind.ServerRack) ? FacilityKind.Generator : FacilityKind.ServerRack;
                if (sim.Execute(Command.Build(free, kind)).Accepted)
                {
                    return;
                }
            }

            FacilityKind[] upgrades = { FacilityKind.BatteryBank, FacilityKind.ServerRack, FacilityKind.Turret, FacilityKind.LifeSupport, FacilityKind.Generator };
            // spend when power is comfortable or storage is nearly full (a competent handler does not sit on a full cap)
            bool flush = f.NetEnergyPerHour > 260 || (f.NetEnergyPerHour > 120 && s.Energy * 10 >= f.EnergyCap * 7);
            foreach (FacilityKind kind in upgrades)
            {
                if (flush && TryUpgrade(sim, kind))
                {
                    return;
                }
            }
        }

        /// <summary>Keeps the Hub healthy: repairs scars, clears wrecks, flushes a core that slides into Unstable.</summary>
        private static void Care(Simulation sim)
        {
            GameState s = sim.State;
            SimConfig c = sim.Config;
            if (CorruptionSystem.Band(c, s.CorruptionMilli) >= CorruptionBand.Unstable && s.Energy >= c.Glitch.FlushEnergy * 2)
            {
                sim.Execute(Command.FlushCore());
            }

            for (int i = 0; i < s.Slots.Count; i++)
            {
                FacilitySlot f = s.Slots[i];
                if (f.Damage > 0 && ScarSystem.RepairCost(c, f) * 2 <= s.Energy && sim.Execute(Command.Repair(i)).Accepted)
                {
                    break;
                }
            }

            if (s.Wreckage >= 2 && c.Scars.ClearEnergyPerWreck * s.Wreckage * 3 <= s.Energy)
            {
                sim.Execute(Command.ClearWreckage());
            }
        }

        /// <summary>Restores the first available module (catalog order: trunk first) while energy is comfortable and the core is calm.</summary>
        private static void Research(Simulation sim)
        {
            GameState s = sim.State;
            if (s.Energy * 2 < Economy.Flows(s, sim.Config).EnergyCap || CorruptionSystem.Band(sim.Config, s.CorruptionMilli) >= CorruptionBand.Unstable)
            {
                return;
            }

            // one restoration per lane: the memory sector and a field module
            foreach (ModuleDef d in Modules.Catalog)
            {
                bool memory = d.Field == ModuleField.Trunk;
                if ((memory ? s.MemoryNode : s.ResearchNode) == 0 && Modules.Availability(s, d.Node) == RejectReason.None && sim.Execute(Command.StartResearch(d.Node)).Accepted)
                {
                    return;
                }
            }
        }

        private static int FreeSlot(GameState s)
        {
            for (int i = 0; i < s.Slots.Count; i++)
            {
                if (s.Slots[i].IsEmpty && s.JobForSlot(i) == null)
                {
                    return i;
                }
            }

            return -1;
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
