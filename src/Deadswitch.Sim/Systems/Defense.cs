using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>Pure defense and threat queries (SPEC-001 rules 4-5). Used by the raid system and the defense screen.</summary>
    public static class Defense
    {
        /// <summary>Tall-poppy power rating: 10 per facility level plus population.</summary>
        public static int PowerRating(GameState s)
        {
            int levels = 0;
            foreach (FacilitySlot slot in s.Slots)
            {
                levels += slot.Level;
            }

            return (levels * 10) + s.People;
        }

        /// <summary>Raid strength before variance and the offline penalty.</summary>
        public static int BaseRaidStrength(GameState s, SimConfig c)
        {
            long scaled = FixedMath.PowPermille(PowerRating(s), c.Raid.PowerExponentPermille) * c.Raid.PowerCoeffPermille / 1000;
            int pct = c.Tier.RaidStrengthPct[System.Math.Min(s.Tier, c.Tier.RaidStrengthPct.Length) - 1];
            return SimMath.PctFloor((int)(c.Raid.BaseStrength + scaled), pct);
        }

        /// <summary>Current defense rating: powered turrets + garrison, with the posture modifier.</summary>
        public static int Rating(GameState s, SimConfig c)
        {
            return Rating(s, c, s.Posture, s.Garrison);
        }

        /// <summary>Defense rating the Hub would have with another posture and garrison (setup previews, advice).</summary>
        public static int Rating(GameState s, SimConfig c, Posture posture, int garrison, bool turrets = true)
        {
            // unit families (SPEC-035): garrison = infantry, Drone Bays, Motor Pools, each countered by the attack's mix
            int total = Family(s, c, UnitFamily.Infantry, garrison) + Family(s, c, UnitFamily.Drones, garrison) + Family(s, c, UnitFamily.Vehicles, garrison);
            int fireControl = Modules.Has(s, ModuleNode.WF1) ? c.Modules.FireControlPct : 0;
            foreach (FacilitySlot slot in s.Slots)
            {
                if (turrets && slot.Kind == FacilityKind.Turret && Economy.IsRunning(slot))
                {
                    total += SimMath.PctFloor(Economy.EffectiveOutput(s, c, slot), 100 + fireControl);
                }
            }

            // the ally's fighters man the wall too (SPEC-025)
            total += DiplomacySystem.AllyDefense(s, c);

            if (posture == Posture.Turtle)
            {
                // a faction that learned we turtle brings breaching charges (SPEC-027)
                int turtle = c.Defense.TurtleDefensePct + (Modules.Has(s, ModuleNode.WF2B) ? c.Modules.KillZonePts : 0) - AdaptSystem.Counter(s, c, Posture.Turtle);
                total = SimMath.PctFloor(total, 100 + System.Math.Max(0, turtle));
            }

            return total;
        }

        /// <summary>One family's share of the defense, after counters against the current attack's mix (SPEC-035).</summary>
        public static int Family(GameState s, SimConfig c, UnitFamily family, int garrison)
        {
            int raw = 0;
            if (family == UnitFamily.Infantry)
            {
                raw = garrison * (c.Defense.DefensePerDefender + (Modules.Has(s, ModuleNode.WF2A) ? c.Modules.MilitiaPerDefender : 0));
            }
            else
            {
                FacilityKind kind = family == UnitFamily.Drones ? FacilityKind.DroneBay : FacilityKind.MotorPool;
                foreach (FacilitySlot slot in s.Slots)
                {
                    if (slot.Kind == kind && Economy.IsRunning(slot))
                    {
                        raw += Economy.EffectiveOutput(s, c, slot);
                    }
                }
            }

            return SimMath.PctFloor(raw, System.Math.Max(0, 100 + UnitSystem.CounterPts(s, c, family)));
        }

        /// <summary>True when the offline activity penalty applies (doc 04 s2).</summary>
        public static bool Unprepared(GameState s)
        {
            return s.Away && s.Posture == Posture.None && s.Garrison == 0;
        }
    }
}
