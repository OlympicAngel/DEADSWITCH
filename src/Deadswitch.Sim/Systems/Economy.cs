using Deadswitch.Sim.Config;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Hub totals for the HUD and the rules, computed from the current flags. All rates per game hour.</summary>
    public readonly struct EconomyFlows
    {
        public EconomyFlows(int generation, int coreUpkeep, int facilityUpkeep, int compute, int energyCap, int populationCap, int crewNeeded, int crewAssigned, int automationLoad)
        {
            GenerationPerHour = generation;
            CoreUpkeepPerHour = coreUpkeep;
            FacilityUpkeepPerHour = facilityUpkeep;
            ComputePerHour = compute;
            EnergyCap = energyCap;
            PopulationCap = populationCap;
            CrewNeeded = crewNeeded;
            CrewAssigned = crewAssigned;
            AutomationLoad = automationLoad;
        }

        public int GenerationPerHour { get; }

        public int CoreUpkeepPerHour { get; }

        /// <summary>Upkeep of powered facilities only (shed and switched-off ones cost nothing).</summary>
        public int FacilityUpkeepPerHour { get; }

        public int NetEnergyPerHour => GenerationPerHour - CoreUpkeepPerHour - FacilityUpkeepPerHour;

        public int ComputePerHour { get; }

        public int EnergyCap { get; }

        public int PopulationCap { get; }

        public int CrewNeeded { get; }

        public int CrewAssigned { get; }

        public int AutomationLoad { get; }
    }

    /// <summary>Pure queries over the economy (no state changes). Used by systems, commands and the UI.</summary>
    public static class Economy
    {
        /// <summary>Output share for crew: full when staffed, reduced when the AI runs it (LG6 raises it).</summary>
        public static int OutputPct(GameState s, SimConfig c, FacilitySlot slot)
        {
            if (slot.Staffed)
            {
                return 100;
            }

            return Modules.Has(s, ModuleNode.LG6) ? c.Modules.AutomationUnmannedPct : c.Crew.UnmannedOutputPct;
        }

        /// <summary>Effective per-hour output of a running facility, after crew and module bonuses (SPEC-008).</summary>
        public static int EffectiveOutput(GameState s, SimConfig c, FacilitySlot slot)
        {
            FacilityConfig? f = c.Facility(slot.Kind);
            if (f == null || slot.Level <= 0)
            {
                return 0;
            }

            return SimMath.PctFloor(f.Output[slot.Level - 1], System.Math.Max(0, OutputPct(s, c, slot) + BonusPct(s, c, slot.Kind)));
        }

        /// <summary>Module output bonus for a facility kind, in percentage points.</summary>
        public static int BonusPct(GameState s, SimConfig c, FacilityKind kind)
        {
            switch (kind)
            {
                case FacilityKind.ServerRack:
                    return (Modules.Has(s, ModuleNode.LG2A) ? c.Modules.OverclockPct : 0) + PeopleChoices.OutputPts(s, c);
                case FacilityKind.BatteryBank:
                    return Modules.Has(s, ModuleNode.LG2B) ? c.Modules.DeepCellsPct : 0;
                case FacilityKind.Generator:
                    return (Modules.Has(s, ModuleNode.LG5B) ? c.Modules.FuelCellsPct : 0) + PeopleChoices.OutputPts(s, c);
                default:
                    return 0;
            }
        }

        /// <summary>Build minutes for a target level after LG4 Prefab Assembly (at least one minute).</summary>
        public static int BuildMinutes(GameState s, SimConfig c, FacilityKind kind, int targetLevel)
        {
            int minutes = c.Facility(kind)!.BuildMinutes[targetLevel - 1];
            return Modules.Has(s, ModuleNode.LG4) ? System.Math.Max(1, SimMath.PctFloor(minutes, 100 - c.Modules.PrefabPct)) : minutes;
        }

        /// <summary>Refund percent for cancel or demolish after LG5A Salvage Doctrine.</summary>
        public static int RefundPct(GameState s, SimConfig c, int basePct)
        {
            return Modules.Has(s, ModuleNode.LG5A) ? System.Math.Min(100, basePct + c.Modules.SalvagePts) : basePct;
        }

        /// <summary>Upkeep per hour after LG1 Load Balancing (generators excluded: their upkeep is net in output).</summary>
        public static int UpkeepPerHour(GameState s, SimConfig c, FacilitySlot slot)
        {
            FacilityConfig? f = c.Facility(slot.Kind);
            if (f == null || slot.Level <= 0)
            {
                return 0;
            }

            int upkeep = f.UpkeepPerHour[slot.Level - 1];
            if (ThreatSystem.Shielded(s))
            {
                // doc 10: upkeep halved while the vacation shield holds
                upkeep = SimMath.PctCeil(upkeep, c.Threats.ShieldUpkeepPct);
            }

            return slot.Kind != FacilityKind.Generator && Modules.Has(s, ModuleNode.LG1) ? SimMath.PctCeil(upkeep, 100 - c.Modules.LoadBalancingPct) : upkeep;
        }

        public static int CrewNeeded(SimConfig c, FacilitySlot slot)
        {
            FacilityConfig? f = c.Facility(slot.Kind);
            return f == null || slot.Level <= 0 ? 0 : f.Crew[slot.Level - 1];
        }

        /// <summary>Whether a slot's facility is contributing right now.</summary>
        public static bool IsRunning(FacilitySlot slot)
        {
            return !slot.IsEmpty && slot.Enabled && slot.Powered;
        }

        /// <summary>Net generation per hour (output minus own upkeep) of enabled generators.</summary>
        public static int GenerationPerHour(GameState s, SimConfig c)
        {
            int total = 0;
            foreach (FacilitySlot slot in s.Slots)
            {
                if (slot.Kind == FacilityKind.Generator && slot.Enabled)
                {
                    total += EffectiveOutput(s, c, slot) - UpkeepPerHour(s, c, slot);
                }
            }

            return total;
        }

        public static int EnergyCap(GameState s, SimConfig c)
        {
            int cap = c.Energy.Cap;
            foreach (FacilitySlot slot in s.Slots)
            {
                if (slot.Kind == FacilityKind.BatteryBank && IsRunning(slot))
                {
                    cap += EffectiveOutput(s, c, slot);
                }
            }

            return cap;
        }

        public static int PopulationCap(GameState s, SimConfig c)
        {
            int cap = c.People.Cap + c.Tier.PopBonus[SimMath.Clamp(s.Tier - 1, 0, c.Tier.PopBonus.Length - 1)] + (Modules.HasHabitat(s) ? c.Modules.HabitatPop : 0);
            foreach (FacilitySlot slot in s.Slots)
            {
                if (slot.Kind == FacilityKind.LifeSupport && IsRunning(slot))
                {
                    cap += EffectiveOutput(s, c, slot);
                }
            }

            return cap;
        }

        /// <summary>Built facilities of a kind plus new builds of it in the queue (for diminishing returns).</summary>
        public static int CountOfKind(GameState s, FacilityKind kind)
        {
            int n = 0;
            foreach (FacilitySlot slot in s.Slots)
            {
                if (slot.Kind == kind)
                {
                    n++;
                }
            }

            foreach (BuildJob job in s.Jobs)
            {
                if (job.Kind == kind && job.TargetLevel == 1)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>Cost of building a new facility (level 1), including the duplicate surcharge.</summary>
        public static void BuildCost(GameState s, SimConfig c, FacilityKind kind, out int energy, out int compute)
        {
            FacilityConfig f = c.Facility(kind)!;
            int pct = 100 + (c.Build.DuplicateCostPct * CountOfKind(s, kind));
            energy = SimMath.PctCeil(f.CostEnergy[0], pct);
            compute = SimMath.PctCeil(f.CostCompute[0], pct);
        }

        /// <summary>Cost of upgrading from <paramref name="level"/> to level + 1.</summary>
        public static void UpgradeCost(SimConfig c, FacilityKind kind, int level, out int energy, out int compute)
        {
            FacilityConfig f = c.Facility(kind)!;
            energy = f.CostEnergy[level];
            compute = f.CostCompute[level];
        }

        public static EconomyFlows Flows(GameState s, SimConfig c)
        {
            int upkeep = 0;
            int compute = 0;
            int crewNeeded = 0;
            int crewAssigned = 0;
            foreach (FacilitySlot slot in s.Slots)
            {
                if (slot.IsEmpty || !slot.Enabled)
                {
                    continue;
                }

                int need = CrewNeeded(c, slot);
                crewNeeded += need;
                if (slot.Staffed)
                {
                    crewAssigned += need;
                }

                if (slot.Kind == FacilityKind.Generator || !slot.Powered)
                {
                    continue;
                }

                upkeep += UpkeepPerHour(s, c, slot);
                if (slot.Kind == FacilityKind.ServerRack)
                {
                    compute += EffectiveOutput(s, c, slot);
                }
            }

            return new EconomyFlows(
                GenerationPerHour(s, c),
                c.Energy.CoreUpkeepPerHour + (s.Posture == Posture.Dark ? c.Defense.DarkUpkeepPerHour : 0),
                upkeep,
                compute,
                EnergyCap(s, c),
                PopulationCap(s, c),
                crewNeeded,
                crewAssigned,
                s.AutomationLoad);
        }
    }
}
