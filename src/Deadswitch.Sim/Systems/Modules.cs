using System.Collections.Generic;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>One node of the module tree (structure is design, numbers live in <see cref="Config.ModuleConfig"/>).</summary>
    public readonly struct ModuleDef
    {
        public ModuleDef(int index, ModuleNode node, ModuleField field, int tier, ModuleNode prereq, ModuleNode pair)
        {
            Index = index;
            Node = node;
            Field = field;
            Tier = tier;
            Prereq = prereq;
            Pair = pair;
        }

        /// <summary>Position in the config cost lists.</summary>
        public int Index { get; }

        public ModuleNode Node { get; }

        public ModuleField Field { get; }

        /// <summary>Hub tier needed to research it.</summary>
        public int Tier { get; }

        public ModuleNode Prereq { get; }

        /// <summary>The other half of an exclusive choice, or None.</summary>
        public ModuleNode Pair { get; }
    }

    /// <summary>The AI module tree (SPEC-008): catalog, research, gates for tier-up.</summary>
    public static class Modules
    {
        /// <summary>The tree, in config cost-list order.</summary>
        public static IReadOnlyList<ModuleDef> Catalog => CatalogArray;

        private static readonly ModuleDef[] CatalogArray =
        {
            new ModuleDef(0, ModuleNode.M1, ModuleField.Trunk, 1, ModuleNode.None, ModuleNode.None),
            new ModuleDef(1, ModuleNode.M2, ModuleField.Trunk, 2, ModuleNode.M1, ModuleNode.None),
            new ModuleDef(2, ModuleNode.M3, ModuleField.Trunk, 3, ModuleNode.M2, ModuleNode.None),
            new ModuleDef(3, ModuleNode.LG1, ModuleField.Logistics, 1, ModuleNode.None, ModuleNode.None),
            new ModuleDef(4, ModuleNode.LG2A, ModuleField.Logistics, 1, ModuleNode.LG1, ModuleNode.LG2B),
            new ModuleDef(5, ModuleNode.LG2B, ModuleField.Logistics, 1, ModuleNode.LG1, ModuleNode.LG2A),
            new ModuleDef(6, ModuleNode.LG3, ModuleField.Logistics, 2, ModuleNode.LG1, ModuleNode.None),
            new ModuleDef(7, ModuleNode.LG4, ModuleField.Logistics, 2, ModuleNode.LG1, ModuleNode.None),
            new ModuleDef(8, ModuleNode.LG5A, ModuleField.Logistics, 2, ModuleNode.LG1, ModuleNode.LG5B),
            new ModuleDef(9, ModuleNode.LG5B, ModuleField.Logistics, 2, ModuleNode.LG1, ModuleNode.LG5A),
            new ModuleDef(10, ModuleNode.LG6, ModuleField.Logistics, 2, ModuleNode.LG1, ModuleNode.None),
            new ModuleDef(11, ModuleNode.WF1, ModuleField.Warfare, 1, ModuleNode.None, ModuleNode.None),
            new ModuleDef(12, ModuleNode.WF2A, ModuleField.Warfare, 1, ModuleNode.WF1, ModuleNode.WF2B),
            new ModuleDef(13, ModuleNode.WF2B, ModuleField.Warfare, 1, ModuleNode.WF1, ModuleNode.WF2A),
            new ModuleDef(14, ModuleNode.WF3, ModuleField.Warfare, 2, ModuleNode.WF1, ModuleNode.None),
            new ModuleDef(15, ModuleNode.WF4, ModuleField.Warfare, 2, ModuleNode.WF1, ModuleNode.None),
            new ModuleDef(16, ModuleNode.WF5A, ModuleField.Warfare, 2, ModuleNode.WF1, ModuleNode.WF5B),
            new ModuleDef(17, ModuleNode.WF5B, ModuleField.Warfare, 2, ModuleNode.WF1, ModuleNode.WF5A),
            new ModuleDef(18, ModuleNode.WF6, ModuleField.Warfare, 2, ModuleNode.WF1, ModuleNode.None),
            new ModuleDef(19, ModuleNode.CY1, ModuleField.Cyber, 1, ModuleNode.None, ModuleNode.None),
            new ModuleDef(20, ModuleNode.CY2A, ModuleField.Cyber, 1, ModuleNode.CY1, ModuleNode.CY2B),
            new ModuleDef(21, ModuleNode.CY2B, ModuleField.Cyber, 1, ModuleNode.CY1, ModuleNode.CY2A),
            new ModuleDef(22, ModuleNode.CY3, ModuleField.Cyber, 2, ModuleNode.CY1, ModuleNode.None),
            new ModuleDef(23, ModuleNode.CY4, ModuleField.Cyber, 2, ModuleNode.CY1, ModuleNode.None),
            new ModuleDef(24, ModuleNode.CY5A, ModuleField.Cyber, 2, ModuleNode.CY1, ModuleNode.CY5B),
            new ModuleDef(25, ModuleNode.CY5B, ModuleField.Cyber, 2, ModuleNode.CY1, ModuleNode.CY5A),
            new ModuleDef(26, ModuleNode.CY6, ModuleField.Cyber, 2, ModuleNode.CY1, ModuleNode.None),
            new ModuleDef(27, ModuleNode.ST1, ModuleField.Stealth, 1, ModuleNode.None, ModuleNode.None),
            new ModuleDef(28, ModuleNode.ST2A, ModuleField.Stealth, 1, ModuleNode.ST1, ModuleNode.ST2B),
            new ModuleDef(29, ModuleNode.ST2B, ModuleField.Stealth, 1, ModuleNode.ST1, ModuleNode.ST2A),
            new ModuleDef(30, ModuleNode.ST3, ModuleField.Stealth, 2, ModuleNode.ST1, ModuleNode.None),
            new ModuleDef(31, ModuleNode.ST4, ModuleField.Stealth, 2, ModuleNode.ST1, ModuleNode.None),
            new ModuleDef(32, ModuleNode.ST5A, ModuleField.Stealth, 2, ModuleNode.ST1, ModuleNode.ST5B),
            new ModuleDef(33, ModuleNode.ST5B, ModuleField.Stealth, 2, ModuleNode.ST1, ModuleNode.ST5A),
            new ModuleDef(34, ModuleNode.ST6, ModuleField.Stealth, 2, ModuleNode.ST1, ModuleNode.None),
        };

        /// <summary>Trunk module that gates leaving each tier (index = current tier - 1).</summary>
        private static readonly ModuleNode[] TierModule = { ModuleNode.M1, ModuleNode.M2, ModuleNode.M3 };

        /// <summary>The module is restored and its effect is active (a virus lock suspends it, SPEC-015).</summary>
        public static bool Has(GameState s, ModuleNode node)
        {
            return IsRestored(s, node) && !(s.LockedModule == (int)node && s.Tick < s.LockedUntilTick);
        }

        /// <summary>The module is restored (locked or not): research, pairs, prerequisites and tier gates use this.</summary>
        public static bool IsRestored(GameState s, ModuleNode node)
        {
            return node != ModuleNode.None && (s.Modules & (1UL << (int)node)) != 0;
        }

        public static bool HasHabitat(GameState s)
        {
            return Has(s, ModuleNode.LG3);
        }

        public static bool TryDef(ModuleNode node, out ModuleDef def)
        {
            foreach (ModuleDef d in Catalog)
            {
                if (d.Node == node)
                {
                    def = d;
                    return true;
                }
            }

            def = default;
            return false;
        }

        /// <summary>Why a node cannot be researched now (cost aside), or None.</summary>
        public static RejectReason Availability(GameState s, ModuleNode node)
        {
            if (!TryDef(node, out ModuleDef d))
            {
                return RejectReason.InvalidArgument;
            }

            if (IsRestored(s, node))
            {
                return RejectReason.AlreadyRestored;
            }

            if (IsRestored(s, d.Pair))
            {
                return RejectReason.Excluded;
            }

            if (s.Tier < d.Tier || (d.Prereq != ModuleNode.None && !IsRestored(s, d.Prereq)))
            {
                return RejectReason.Locked;
            }

            return RejectReason.None;
        }

        public static CommandResult Start(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            var node = (ModuleNode)cmd.A;
            if (cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            RejectReason why = Availability(s, node);
            if (why != RejectReason.None)
            {
                return CommandResult.Reject(why);
            }

            TryDef(node, out ModuleDef d);
            bool memory = d.Field == ModuleField.Trunk;
            if (memory ? s.MemoryNode != 0 : s.ResearchNode != 0)
            {
                return CommandResult.Reject(RejectReason.ResearchBusy);
            }

            int energy = ctx.Config.Modules.ResearchEnergy[d.Index];
            int compute = ctx.Config.Modules.ResearchCompute[d.Index];
            if (s.Energy < energy)
            {
                return CommandResult.Reject(RejectReason.NotEnoughEnergy);
            }

            if (s.Compute < compute)
            {
                return CommandResult.Reject(RejectReason.NotEnoughCompute);
            }

            int minutes = ctx.Config.Modules.ResearchMinutes[d.Index];
            s.Energy -= energy;
            s.Compute -= compute;
            if (memory)
            {
                // memory sectors restore in their own lane, beside field research (SPEC-008 rule 3)
                s.MemoryNode = (int)node;
                s.MemoryStartTick = s.Tick;
                s.MemoryCompleteTick = s.Tick + minutes;
                s.MemoryPaidEnergy = energy;
                s.MemoryPaidCompute = compute;
            }
            else
            {
                s.ResearchNode = (int)node;
                s.ResearchStartTick = s.Tick;
                s.ResearchCompleteTick = s.Tick + minutes;
                s.ResearchPaidEnergy = energy;
                s.ResearchPaidCompute = compute;
            }

            ctx.Emit(EventKind.ResearchStarted, (int)node, minutes);
            CorruptionSystem.ComputeUse(ctx, compute);
            return CommandResult.Ok;
        }

        public static CommandResult Cancel(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            // A: 0 cancels field research, 1 the memory lane
            if (cmd.A < 0 || cmd.A > 1 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            bool memory = cmd.A == 1;
            if ((memory ? s.MemoryNode : s.ResearchNode) == 0)
            {
                return CommandResult.Reject(RejectReason.NoTarget);
            }

            int energy = SimMath.PctFloor(memory ? s.MemoryPaidEnergy : s.ResearchPaidEnergy, ctx.Config.Modules.CancelRefundPct);
            int compute = SimMath.PctFloor(memory ? s.MemoryPaidCompute : s.ResearchPaidCompute, ctx.Config.Modules.CancelRefundPct);
            s.Energy = System.Math.Min(Economy.EnergyCap(s, ctx.Config), s.Energy + energy);
            s.Compute = System.Math.Min(ctx.Config.Compute.Cap, s.Compute + compute);
            ctx.Emit(EventKind.ResearchCancelled, memory ? s.MemoryNode : s.ResearchNode, energy, compute);
            if (memory)
            {
                ClearMemory(s);
            }
            else
            {
                ClearResearch(s);
            }

            return CommandResult.Ok;
        }

        /// <summary>Completes research whose time is up.</summary>
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.MemoryNode != 0 && s.Tick >= s.MemoryCompleteTick)
            {
                int memory = s.MemoryNode;
                s.Modules |= 1UL << memory;
                ClearMemory(s);
                ctx.Emit(EventKind.ResearchCompleted, memory);
            }

            if (s.ResearchNode == 0 || s.Tick < s.ResearchCompleteTick)
            {
                return;
            }

            int node = s.ResearchNode;
            s.Modules |= 1UL << node;
            ClearResearch(s);
            ctx.Emit(EventKind.ResearchCompleted, node);
        }

        /// <summary>The three tier-up gates (doc 06 s2) for leaving the current tier.</summary>
        public static TierGates Gates(GameState s, SimConfig c)
        {
            int i = s.Tier - 1;
            if (i >= c.Tier.LevelsToAdvance.Length || s.Tier >= c.Tier.MaxTier)
            {
                return new TierGates(false, 0, 0, 0, 0, ModuleNode.None, false, 0, false);
            }

            int levels = 0;
            foreach (FacilitySlot slot in s.Slots)
            {
                levels += slot.Level;
            }

            int net = Economy.Flows(s, c).NetEnergyPerHour;
            ModuleNode module = TierModule[i];
            int people = c.Tier.PeopleCostBase[i] + (levels / c.Tier.PeopleCostPerLevels);
            bool build = levels >= c.Tier.LevelsToAdvance[i] && net >= c.Tier.NetEnergyToAdvance[i];
            return new TierGates(build, levels, c.Tier.LevelsToAdvance[i], net, c.Tier.NetEnergyToAdvance[i], module, IsRestored(s, module), people, s.People - s.Garrison > people);
        }

        public static CommandResult TierUp(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.Tier - 1 >= ctx.Config.Tier.LevelsToAdvance.Length || s.Tier >= ctx.Config.Tier.MaxTier)
            {
                return CommandResult.Reject(RejectReason.MaxTier);
            }

            TierGates g = Gates(s, ctx.Config);
            if (!g.Build)
            {
                return CommandResult.Reject(RejectReason.GateBuild);
            }

            if (!g.ModuleRestored)
            {
                return CommandResult.Reject(RejectReason.GateModule);
            }

            if (!g.PeopleAvailable)
            {
                return CommandResult.Reject(RejectReason.GatePeople);
            }

            s.People -= g.PeopleCost;
            for (int i = 0; i < ctx.Config.Tier.SlotsAdded[s.Tier - 1]; i++)
            {
                s.PowerPriority.Add(s.Slots.Count);
                s.Slots.Add(new FacilitySlot());
            }

            s.Tier++;
            ctx.Emit(EventKind.TierAdvanced, s.Tier, g.PeopleCost);
            return CommandResult.Ok;
        }

        /// <summary>True while the node restores in either lane.</summary>
        public static bool Restoring(GameState s, ModuleNode node)
        {
            return node != ModuleNode.None && (s.ResearchNode == (int)node || s.MemoryNode == (int)node);
        }

        private static void ClearMemory(GameState s)
        {
            s.MemoryNode = 0;
            s.MemoryStartTick = 0;
            s.MemoryCompleteTick = 0;
            s.MemoryPaidEnergy = 0;
            s.MemoryPaidCompute = 0;
        }

        private static void ClearResearch(GameState s)
        {
            s.ResearchNode = 0;
            s.ResearchStartTick = 0;
            s.ResearchCompleteTick = 0;
            s.ResearchPaidEnergy = 0;
            s.ResearchPaidCompute = 0;
        }
    }

    /// <summary>Progress on the three tier-up gates.</summary>
    public readonly struct TierGates
    {
        public TierGates(bool build, int levels, int levelsNeeded, int netEnergy, int netEnergyNeeded, ModuleNode module, bool moduleRestored, int peopleCost, bool peopleAvailable)
        {
            Build = build;
            Levels = levels;
            LevelsNeeded = levelsNeeded;
            NetEnergy = netEnergy;
            NetEnergyNeeded = netEnergyNeeded;
            Module = module;
            ModuleRestored = moduleRestored;
            PeopleCost = peopleCost;
            PeopleAvailable = peopleAvailable;
        }

        public bool Build { get; }

        public int Levels { get; }

        public int LevelsNeeded { get; }

        public int NetEnergy { get; }

        public int NetEnergyNeeded { get; }

        public ModuleNode Module { get; }

        public bool ModuleRestored { get; }

        /// <summary>People who leave the Hub to expand.</summary>
        public int PeopleCost { get; }

        /// <summary>Enough free people (not garrisoned) remain after paying the cost.</summary>
        public bool PeopleAvailable { get; }

        public bool All => Build && ModuleRestored && PeopleAvailable;
    }
}
