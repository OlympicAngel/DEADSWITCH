using Deadswitch.Sim;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Persistence;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;

namespace Deadswitch.Host.Online
{
    /// <summary>What a run claims, read from its save (F-049): enough to rank it once a leaderboard service exists.</summary>
    public sealed class RunSubmission
    {
        public RunSubmission(SaveInfo info, int legacyTotal, int legacyScore, int highestTier, int cycle, int fragments, bool ironman)
        {
            Info = info;
            LegacyTotal = legacyTotal;
            LegacyScore = legacyScore;
            HighestTier = highestTier;
            Cycle = cycle;
            Fragments = fragments;
            Ironman = ironman;
        }

        public SaveInfo Info { get; }

        /// <summary>Legacy recorded by finished cycles.</summary>
        public int LegacyTotal { get; }

        /// <summary>The current cycle's score so far.</summary>
        public int LegacyScore { get; }

        public int HighestTier { get; }

        public int Cycle { get; }

        public int Fragments { get; }

        public bool Ironman { get; }

        /// <summary>Leaderboard score: everything on record plus the live cycle.</summary>
        public int Score => LegacyTotal + LegacyScore;
    }

    public enum VerifyResult
    {
        Verified = 0,

        /// <summary>The save is damaged or not a DEADSWITCH save.</summary>
        Unreadable = 1,

        /// <summary>Written by another build or balance file: it cannot be replayed here.</summary>
        OtherVersion = 2,

        /// <summary>Seed + commands do not reproduce the saved state: edited, or a determinism bug.</summary>
        Diverged = 3,
    }

    /// <summary>
    /// Deterministic run verification (doc 08: "deterministic server verification", ADR-0003). A save already
    /// carries its seed, config hash and every accepted command, so a server replays it from the seed and
    /// compares state hashes. Edited states, impossible commands and tampered logs all fail. Shared by the CLI
    /// (`verify`), tests and a future leaderboard backend.
    /// </summary>
    public static class RunVerifier
    {
        public static RunSubmission Read(Simulation sim)
        {
            GameState s = sim.State;
            var info = new SaveInfo(SaveGame.FormatVersion, sim.Seed, sim.Config.ComputeHash(), s.Tick, StateHasher.Hash(s));
            return new RunSubmission(info, s.LegacyTotal, LegacySystem.Score(s, sim.Config), System.Math.Max(s.HighestTier, s.Tier), s.Cycle, ChapterSystem.FragmentsKnown(s), s.Ironman);
        }

        /// <summary>Replays the save from its seed under <paramref name="config"/>; the submission is null unless verified.</summary>
        public static VerifyResult Verify(byte[] save, SimConfig config, out RunSubmission? submission)
        {
            submission = null;
            LoadedGame game;
            try
            {
                SaveInfo info = SaveGame.ReadInfo(save);
                if (info.FormatVersion != SaveGame.FormatVersion || info.ConfigHash != config.ComputeHash())
                {
                    return VerifyResult.OtherVersion;
                }

                game = SaveGame.Load(save, config);
            }
            catch (SaveLoadException)
            {
                return VerifyResult.Unreadable;
            }

            Simulation saved = game.Simulation;
            try
            {
                Simulation replay = Replay.Run(saved.Seed, config, saved.Commands.Commands, saved.State.Tick);
                if (StateHasher.Hash(replay.State) != StateHasher.Hash(saved.State))
                {
                    return VerifyResult.Diverged;
                }
            }
            catch (ReplayDivergenceException)
            {
                return VerifyResult.Diverged;
            }

            submission = Read(saved);
            return VerifyResult.Verified;
        }
    }
}
