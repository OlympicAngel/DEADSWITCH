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
        public static readonly ModuleDef[] Catalog =
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
        };

        /// <summary>Trunk module that gates leaving each tier (index = current tier - 1).</summary>
        private static readonly ModuleNode[] TierModule = { ModuleNode.M1, ModuleNode.M2, ModuleNode.M3 };

        public static bool Has(GameState s, ModuleNode node)
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

            if (Has(s, node))
            {
                return RejectReason.AlreadyRestored;
            }

            if (Has(s, d.Pair))
            {
                return RejectReason.Excluded;
            }

            if (s.Tier < d.Tier || (d.Prereq != ModuleNode.None && !Has(s, d.Prereq)))
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

            if (s.ResearchNode != 0)
            {
                return CommandResult.Reject(RejectReason.ResearchBusy);
            }

            TryDef(node, out ModuleDef d);
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
            s.ResearchNode = (int)node;
            s.ResearchStartTick = s.Tick;
            s.ResearchCompleteTick = s.Tick + minutes;
            s.ResearchPaidEnergy = energy;
            s.ResearchPaidCompute = compute;
            ctx.Emit(EventKind.ResearchStarted, (int)node, minutes);
            return CommandResult.Ok;
        }

        public static CommandResult Cancel(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.ResearchNode == 0)
            {
                return CommandResult.Reject(RejectReason.NoTarget);
            }

            int energy = SimMath.PctFloor(s.ResearchPaidEnergy, ctx.Config.Modules.CancelRefundPct);
            int compute = SimMath.PctFloor(s.ResearchPaidCompute, ctx.Config.Modules.CancelRefundPct);
            s.Energy = System.Math.Min(Economy.EnergyCap(s, ctx.Config), s.Energy + energy);
            s.Compute = System.Math.Min(ctx.Config.Compute.Cap, s.Compute + compute);
            ctx.Emit(EventKind.ResearchCancelled, s.ResearchNode, energy, compute);
            ClearResearch(s);
            return CommandResult.Ok;
        }

        /// <summary>Completes research whose time is up.</summary>
        public static void Tick(SimContext ctx)
        {
            GameState s = ctx.State;
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
            if (i >= c.Tier.LevelsToAdvance.Length)
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
            return new TierGates(build, levels, c.Tier.LevelsToAdvance[i], net, c.Tier.NetEnergyToAdvance[i], module, Has(s, module), people, s.People - s.Garrison > people);
        }

        public static CommandResult TierUp(SimContext ctx, Command cmd)
        {
            GameState s = ctx.State;
            if (cmd.A != 0 || cmd.B != 0 || cmd.C != 0)
            {
                return CommandResult.Reject(RejectReason.InvalidArgument);
            }

            if (s.Tier - 1 >= ctx.Config.Tier.LevelsToAdvance.Length)
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
            s.Tier++;
            ctx.Emit(EventKind.TierAdvanced, s.Tier, g.PeopleCost);
            return CommandResult.Ok;
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
