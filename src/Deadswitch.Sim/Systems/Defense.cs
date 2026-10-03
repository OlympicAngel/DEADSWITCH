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
            int total = garrison * c.Defense.DefensePerDefender;
            foreach (FacilitySlot slot in s.Slots)
            {
                if (turrets && slot.Kind == FacilityKind.Turret && Economy.IsRunning(slot))
                {
                    total += Economy.EffectiveOutput(s, c, slot);
                }
            }

            if (posture == Posture.Turtle)
            {
                total = SimMath.PctFloor(total, 100 + c.Defense.TurtleDefensePct);
            }

            return total;
        }

        /// <summary>True when the offline activity penalty applies (doc 04 s2).</summary>
        public static bool Unprepared(GameState s)
        {
            return s.Away && s.Posture == Posture.None && s.Garrison == 0;
        }
    }
}
