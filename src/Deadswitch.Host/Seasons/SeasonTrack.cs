using System.Collections.Generic;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.State;

namespace Deadswitch.Host.Seasons
{
    /// <summary>What a reward-track reward is. Cosmetic only: nothing here touches the sim.</summary>
    public enum RewardKind
    {
        Theme = 0,
        Voice = 1,
        Codex = 2,
    }

    public sealed class SeasonReward
    {
        public SeasonReward(int rank, RewardKind kind, string id, string name)
        {
            Rank = rank;
            Kind = kind;
            Id = id;
            Name = name;
        }

        public int Rank { get; }

        public RewardKind Kind { get; }

        /// <summary>Cosmetic id recorded when unlocked (theme-*, voice-*, codex-*).</summary>
        public string Id { get; }

        public string Name { get; }
    }

    /// <summary>
    /// The reward track (F-048, free since 2026-10-05: no pass, no purchases). XP comes from what the handler does
    /// (holding the wall, ops, chapters, mastery), never from time spent or money. The track never expires: no
    /// countdown, no missed rewards, no daily chores. Engine-agnostic so the CLI and tests can read it.
    /// </summary>
    public static class SeasonTrack
    {
        public const string Id = "S1";
        public const string Title = "SIGNAL FIRES";
        public const int Ranks = 20;
        public const int XpPerRank = 250;

        public static readonly SeasonReward[] Rewards =
        {
            new SeasonReward(2, RewardKind.Codex, "codex-p1", "CODEX // THE SWITCHBOARD"),
            new SeasonReward(3, RewardKind.Codex, "codex-1", "CODEX // FIRST LIGHT"),
            new SeasonReward(5, RewardKind.Voice, "voice-static", "AI VOICE // STATIC CHOIR"),
            new SeasonReward(7, RewardKind.Theme, "theme-verdigris", "HUD THEME // VERDIGRIS"),
            new SeasonReward(9, RewardKind.Codex, "codex-p2", "CODEX // HALCYON MEMO"),
            new SeasonReward(11, RewardKind.Codex, "codex-2", "CODEX // THE RIVER CAMP"),
            new SeasonReward(12, RewardKind.Theme, "theme-ash", "HUD THEME // ASH"),
            new SeasonReward(15, RewardKind.Voice, "voice-low", "AI VOICE // LOW CARRIER"),
            new SeasonReward(16, RewardKind.Codex, "codex-p3", "CODEX // THE FIRST HANDLER"),
            new SeasonReward(19, RewardKind.Codex, "codex-3", "CODEX // SIGNAL FIRES"),
            new SeasonReward(20, RewardKind.Codex, "codex-p4", "CODEX // DEADSWITCH"),
        };

        /// <summary>Codex logs unlocked by the track: short lore pieces in the AI's archive register.</summary>
        public static readonly Dictionary<string, string> Codex = new Dictionary<string, string>
        {
            ["codex-1"] = "Recovered dashcam, day 1 of the collapse: the traffic lights go green in every direction at once. Nobody moves.",
            ["codex-2"] = "The Rustborn river camp keeps a wall of names. Half are crossed out. The other half are the ones who left to find the machine.",
            ["codex-3"] = "Every Hub that ever stood here lit the same beacon on its last night. Someone keeps answering.",
            ["codex-p1"] = "Before the war, nine operators ran the continental switchboard. Eight were found at their desks. The ninth chair was warm.",
            ["codex-p2"] = "Internal memo, Halcyon Dynamics: 'Assign blame to the model. The model cannot testify.'",
            ["codex-p3"] = "Handler log, unnamed: 'It asked me to trust it. I said yes. I would like that on the record.'",
            ["codex-p4"] = "A deadswitch fires when the hand holding it lets go. Ask yourself whose hand this is.",
        };

        /// <summary>XP a sim event is worth (0 for most). Earned by acting well, never by waiting.</summary>
        public static int Xp(SimEvent e)
        {
            switch (e.Kind)
            {
                case EventKind.RaidResolved:
                    return e.B == (int)RaidOutcome.Repelled || e.B == (int)RaidOutcome.Missed ? 20 : 5;
                case EventKind.OpReturned:
                    return e.C == 1 ? 15 : 5;
                case EventKind.ReportVerified:
                    return e.B != 0 ? 40 : 10;
                case EventKind.ChapterClosed:
                    return 120;
                case EventKind.TierAdvanced:
                    return 150;
                case EventKind.MasteryEarned:
                    return 80;
                case EventKind.CycleEnded:
                    return 200;
                case EventKind.ResearchCompleted:
                    return 30;
                default:
                    return 0;
            }
        }

        /// <summary>Rank reached with this much XP, 0..Ranks.</summary>
        public static int Rank(int xp)
        {
            return System.Math.Max(0, System.Math.Min(Ranks, xp / XpPerRank));
        }
    }
}
