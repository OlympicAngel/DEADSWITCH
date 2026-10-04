using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Adaptive enemies (SPEC-027, doc 04 s9): every faction remembers which posture the Hub met its attacks with
    /// and brings a counter (breaching charges against Turtle, sweeps against going dark, cache hunters against
    /// evacuation). Raided factions fortify their sites. Both fade with time, so varied tactics stay strong.
    /// No RNG draws.
    /// </summary>
    public static class AdaptSystem
    {
        /// <summary>Learned postures, in Learned order: Turtle, Dark, Evacuate.</summary>
        public const int Tactics = 3;

        public static int Learned(GameState s, Faction f, Posture posture)
        {
            int t = (int)posture - 1;
            return t < 0 || t >= Tactics ? 0 : s.Learned[((int)f * Tactics) + t];
        }

        /// <summary>Counter points the incoming attacker has against a posture (0 without an attack).</summary>
        public static int Counter(GameState s, SimConfig c, Posture posture)
        {
            if (s.RaidId == 0)
            {
                return 0;
            }

            int per = posture == Posture.Turtle ? c.Adapt.TurtleCounterPts : posture == Posture.Dark ? c.Adapt.DarkCounterPts : posture == Posture.Evacuate ? c.Adapt.EvacuateCounterPts : 0;
            return Learned(s, s.RaidFaction, posture) * per;
        }

        /// <summary>A site's real defense, raised by its owner's fortification.</summary>
        public static int SiteDefense(GameState s, SimConfig c, int defense, Faction owner)
        {
            return SimMath.PctFloor(defense, 100 + (s.Fortified[(int)owner] * c.Adapt.FortifyPct));
        }

        /// <summary>The attacker saw how the Hub met it: that posture's counter grows.</summary>
        public static void Learn(SimContext ctx)
        {
            GameState s = ctx.State;
            int t = (int)s.Posture - 1;
            if (t < 0 || t >= Tactics)
            {
                return;
            }

            int i = ((int)s.RaidFaction * Tactics) + t;
            if (s.Learned[i] < ctx.Config.Adapt.LearnCap)
            {
                s.Learned[i]++;
                ctx.Emit(EventKind.TacticLearned, (int)s.RaidFaction, (int)s.Posture, s.Learned[i]);
            }
        }

        /// <summary>The Hub beat one of their sites: they dig in.</summary>
        public static void Fortify(SimContext ctx, Faction f)
        {
            GameState s = ctx.State;
            if (s.Fortified[(int)f] < ctx.Config.Adapt.FortifyCap)
            {
                s.Fortified[(int)f]++;
                ctx.Emit(EventKind.SiteFortified, (int)f, s.Fortified[(int)f]);
            }
        }

        /// <summary>At midnight on decay days, every counter and fortification drops a level.</summary>
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            AdaptConfig a = ctx.Config.Adapt;
            if (s.Tick % SimConfig.TicksPerDay != 0 || s.Tick == 0)
            {
                return;
            }

            long day = s.Tick / SimConfig.TicksPerDay;
            if (day % a.LearnDecayDays == 0)
            {
                for (int i = 0; i < s.Learned.Length; i++)
                {
                    s.Learned[i] = System.Math.Max(0, s.Learned[i] - 1);
                }
            }

            if (day % a.FortifyDecayDays == 0)
            {
                for (int f = 0; f < s.Fortified.Length; f++)
                {
                    s.Fortified[f] = System.Math.Max(0, s.Fortified[f] - 1);
                }
            }
        }
    }
}
