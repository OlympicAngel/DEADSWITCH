using Deadswitch.Sim.Config;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Sim.Systems
{
    /// <summary>
    /// Chapter arcs (SPEC-024, doc 07 s4): one short story per tier with a villain, a twist and a payoff. A closed
    /// chapter recovers a memory fragment of the hidden truth; fragments survive every reboot. No RNG draws.
    /// </summary>
    public static class ChapterSystem
    {
        public const int FragmentsPerChapter = 3;
        public const int FragmentCount = 12;

        /// <summary>The chapter's villain: First Boot Rustborn, Foothold Vanguard, The Cult Church, Fork Halcyon.</summary>
        public static Faction Villain(int tier)
        {
            return (Faction)SimMath.Clamp(tier - 1, 0, WorldSystem.FactionCount - 1);
        }

        public static int FragmentsKnown(GameState s)
        {
            int n = 0;
            for (int i = 0; i < FragmentCount; i++)
            {
                n += (s.Fragments >> i) & 1;
            }

            return n;
        }

        /// <summary>Opens the tier's chapter and lands the twist when its story trigger fires or the clock runs out.</summary>
        public static void Hourly(SimContext ctx)
        {
            GameState s = ctx.State;
            ChapterConfig c = ctx.Config.Chapters;
            if (s.ChapterTier != s.Tier)
            {
                s.ChapterTier = s.Tier;
                s.ChapterBeat = 0;
                s.ChapterPoints = 0;
                s.ChapterOpenedTick = s.Tick;
                ctx.Emit(EventKind.ChapterOpened, s.Tier, (int)Villain(s.Tier));
                return;
            }

            if (s.ChapterBeat != 0)
            {
                return;
            }

            if (StoryTrigger(s, ctx.Config))
            {
                Twist(ctx, true);
            }
            else if (s.Tick - s.ChapterOpenedTick >= (long)c.TwistFallbackHours * 60)
            {
                Twist(ctx, false);
            }
        }

        /// <summary>A report verified with a lie in it: First Boot's twist.</summary>
        public static void LieCaught(SimContext ctx)
        {
            GameState s = ctx.State;
            if (s.ChapterTier == 1 && s.ChapterBeat == 0)
            {
                Twist(ctx, true);
            }
        }

        /// <summary>An attack the Hub came through (repelled or missed) moves the chapter toward its payoff.</summary>
        public static void Survived(SimContext ctx)
        {
            GameState s = ctx.State;
            ChapterConfig c = ctx.Config.Chapters;
            if (s.ChapterBeat != 1)
            {
                return;
            }

            s.ChapterPoints += s.RaidFaction == Villain(s.ChapterTier) ? c.VillainPoints : 1;
            if (s.ChapterPoints >= c.PayoffPoints)
            {
                Close(ctx);
            }
        }

        private static bool StoryTrigger(GameState s, SimConfig c)
        {
            switch (s.ChapterTier)
            {
                case 2:
                    return WorldSystem.Level(s.Heat[(int)Faction.Vanguard]) >= HeatLevel.Watched || s.Spies[(int)Faction.Vanguard] != SpyState.None;
                case 3:
                    return WorldSystem.Level(s.Heat[(int)Faction.Church]) >= HeatLevel.Watched;
                case 4:
                    return s.ProjectMilli >= c.Chapters.ForkProjectMilli || s.ClimaxAtTick != 0;
                default:
                    return false;
            }
        }

        private static void Twist(SimContext ctx, bool story)
        {
            GameState s = ctx.State;
            s.ChapterBeat = 1;
            s.ChapterPoints = 0;
            ctx.Emit(EventKind.ChapterTwist, s.ChapterTier, story ? 1 : 0);
        }

        private static void Close(SimContext ctx)
        {
            GameState s = ctx.State;
            SimConfig c = ctx.Config;
            ChapterConfig ch = c.Chapters;
            int tier = s.ChapterTier;
            int index = SimMath.Clamp(tier, 1, 4) - 1;
            s.ChapterBeat = 2;

            int fragment = -1;
            for (int k = 0; k < FragmentsPerChapter; k++)
            {
                int id = index * FragmentsPerChapter + k;
                if ((s.Fragments & (1 << id)) == 0)
                {
                    fragment = id;
                    s.Fragments |= 1 << id;
                    break;
                }
            }

            int energy = System.Math.Max(0, System.Math.Min(ch.PayoffEnergy[index], Economy.EnergyCap(s, c) - s.Energy));
            int compute = System.Math.Max(0, System.Math.Min(ch.PayoffCompute[index], c.Compute.Cap - s.Compute));
            s.Energy += energy;
            s.Compute += compute;
            CorruptionSystem.Add(ctx, -System.Math.Min(s.CorruptionMilli, ch.PayoffCorruptionDrop));
            WorldSystem.AddHeat(ctx, Villain(tier), -ch.PayoffHeatDrop);
            ctx.Emit(EventKind.ChapterClosed, tier, fragment, energy, compute);
        }
    }
}
