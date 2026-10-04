using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Luck swings (SPEC-028, doc 04 s9). Hidden streaks: every window is calm, normal or restless, felt in how
    /// often and how hard raids come but never shown as numbers. AI half-warnings: the AI sometimes voices a hunch
    /// about the window, right only some of the time. Opportunity windows: a faction crushed at the wall regroups,
    /// and its sites are weaker for a few hours. No RNG draws (hash rolls on the RNG state).
    /// </summary>
    public static class LuckSystem
    {
        /// <summary>The current window's hidden mood: -1 calm, 0 normal, 1 restless.</summary>
        public static int Mood(GameState s)
        {
            return s.Tick < s.MoodUntilTick ? s.Mood : 0;
        }

        /// <summary>Mean ticks between raid rolls in this window.</summary>
        public static int Interval(GameState s, SimConfig c, int interval)
        {
            int mood = Mood(s);
            return mood < 0 ? SimMath.PctFloor(interval, 100 + c.Luck.CalmIntervalPct)
                : mood > 0 ? System.Math.Max(1, SimMath.PctFloor(interval, 100 + c.Luck.RestlessIntervalPct)) : interval;
        }

        public static int Strength(GameState s, SimConfig c, int strength)
        {
            return Mood(s) > 0 ? SimMath.PctFloor(strength, 100 + c.Luck.RestlessStrengthPct) : strength;
        }

        public static bool Regrouping(GameState s, Faction f)
        {
            return s.RegroupFaction == (int)f && s.Tick < s.RegroupUntilTick;
        }

        /// <summary>A regrouping faction's sites are weaker: the opportunity window.</summary>
        public static int SiteDefense(GameState s, SimConfig c, int defense, Faction owner)
        {
            return Regrouping(s, owner) ? SimMath.PctFloor(defense, 100 - c.Luck.RegroupDefensePct) : defense;
        }

        /// <summary>Each window boundary rolls the next hidden mood, and the AI may voice a hunch about it.</summary>
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            LuckConfig l = ctx.Config.Luck;
            if (s.Tick < s.MoodUntilTick)
            {
                return;
            }

            uint h = SimMath.Hash((uint)(s.Tick / SimConfig.TicksPerHour) ^ 0x1C4Bu, (uint)(s.Rng.State >> 32));
            uint roll = h % 100;
            s.Mood = roll < (uint)l.CalmPct ? -1 : roll >= (uint)(100 - l.RestlessPct) ? 1 : 0;
            s.MoodUntilTick = s.Tick + ((long)l.StreakHours * SimConfig.TicksPerHour);
            if (s.Mood != 0 && (h >> 8) % 100 < (uint)l.HunchPct)
            {
                bool right = (h >> 16) % 100 < (uint)l.HunchAccuracyPct;
                ctx.Emit(EventKind.AiHunch, right ? s.Mood : -s.Mood, l.StreakHours);
            }
        }

        /// <summary>A crushing defense sends the attacker away to regroup.</summary>
        public static void Repelled(SimContext ctx, int strength, int defense)
        {
            GameState s = ctx.State;
            LuckConfig l = ctx.Config.Luck;
            if (strength > 0 && (long)defense * 100 >= (long)strength * l.RegroupMarginPct)
            {
                s.RegroupFaction = (int)s.RaidFaction;
                s.RegroupUntilTick = s.Tick + ((long)l.RegroupHours * SimConfig.TicksPerHour);
                ctx.Emit(EventKind.FactionRegrouping, (int)s.RaidFaction, l.RegroupHours);
            }
        }
    }
}
