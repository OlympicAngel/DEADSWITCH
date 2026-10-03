using System;
using Deadswitch.Sim.Commands;
using Deadswitch.Sim.Events;
using Deadswitch.Sim.Persistence;
using Deadswitch.Sim.State;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public class SaveGameTests
    {
        [Theory]
        [InlineData(1UL, 0L)]
        [InlineData(2UL, 1L)]
        [InlineData(3UL, 59L)]
        [InlineData(4UL, 1439L)]
        [InlineData(5UL, 1440L)]
        [InlineData(6UL, 5000L)]
        public void SaveLoadContinue_EqualsContinuousRun(ulong seed, long splitTick)
        {
            const long total = 4L * SimConfig.TicksPerDay;
            var continuous = new Simulation(seed);
            continuous.Run(splitTick);
            continuous.Execute(Command.SetDelegation(DelegationLevel.Autopilot));
            continuous.Execute(Command.SetPresence(true));
            continuous.Execute(Command.StartResearch(ModuleNode.LG1));
            continuous.Execute(Command.ForcedLabor());
            continuous.Run(total - splitTick);

            var first = new Simulation(seed);
            first.Run(splitTick);
            first.Execute(Command.SetDelegation(DelegationLevel.Autopilot));
            first.Execute(Command.SetPresence(true));
            first.Execute(Command.StartResearch(ModuleNode.LG1));
            first.Execute(Command.ForcedLabor());
            byte[] bytes = SaveGame.Write(first);
            LoadedGame loaded = SaveGame.Load(bytes, SimConfig.Tier1());
            loaded.Simulation.Run(total - splitTick);

            Assert.False(loaded.ConfigChanged);
            Assert.Equal(StateHasher.Hash(continuous.State), StateHasher.Hash(loaded.Simulation.State));
            Assert.Equal(continuous.Log.Events, loaded.Simulation.Log.Events);
            Assert.Equal(continuous.Log.NextSeq, loaded.Simulation.Log.NextSeq);
            Assert.Equal(continuous.Commands.Commands, loaded.Simulation.Commands.Commands);
            Assert.Contains(continuous.Log.Events, e => e.Kind == EventKind.AiActed && e.A == (int)AiActionKind.Defend);
        }

        [Fact]
        public void FormatV1Save_StillLoads_AndContinues()
        {
            // Written by the build before SPEC-004 (save format v1). Guards the versioned visitor migration.
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(TestConfigs.ShippedPath)!, "..", "..", "Deadswitch.Sim.Tests", "Fixtures", "save_v1.dsws");
            LoadedGame loaded = SaveGame.Load(System.IO.File.ReadAllBytes(path), SimConfig.Tier1());

            Assert.Equal(1, loaded.Info.FormatVersion);
            loaded.Simulation.Run(SimConfig.TicksPerDay);
            Assert.Equal(0, loaded.Simulation.State.BoldnessMilli);
            Assert.Equal(GameState.LayoutVersion, SaveGame.ReadInfo(SaveGame.Write(loaded.Simulation)).FormatVersion);
        }

        [Fact]
        public void SaveIsByteStable()
        {
            Simulation a = Played(11UL);
            Simulation b = Played(11UL);

            Assert.Equal(SaveGame.Write(a), SaveGame.Write(b));
        }

        [Fact]
        public void LoadedRun_CanBeReplayedFromItsCommandLog()
        {
            Simulation original = Played(12UL);
            LoadedGame loaded = SaveGame.Load(SaveGame.Write(original), SimConfig.Tier1());

            Simulation replay = Replay.Run(loaded.Info.Seed, SimConfig.Tier1(), loaded.Simulation.Commands.Commands, loaded.Info.Tick);

            Assert.Equal(loaded.Info.StateHash, StateHasher.Hash(replay.State));
        }

        [Fact]
        public void ReadInfo_ReturnsHeader()
        {
            Simulation sim = Played(13UL);

            SaveInfo info = SaveGame.ReadInfo(SaveGame.Write(sim));

            Assert.Equal(SaveGame.FormatVersion, info.FormatVersion);
            Assert.Equal(13UL, info.Seed);
            Assert.Equal(sim.State.Tick, info.Tick);
            Assert.Equal(sim.Config.ComputeHash(), info.ConfigHash);
            Assert.Equal(StateHasher.Hash(sim.State), info.StateHash);
        }

        [Fact]
        public void ChangedConfig_StillLoads_AndIsFlagged()
        {
            byte[] bytes = SaveGame.Write(Played(14UL));
            SimConfig tuned = SimConfig.Tier1();
            tuned.Raid.LootCap = 10;

            LoadedGame loaded = SaveGame.Load(bytes, tuned);

            Assert.True(loaded.ConfigChanged);
            loaded.Simulation.Run(SimConfig.TicksPerDay);
        }

        [Fact]
        public void EveryFlippedByte_IsDetected()
        {
            byte[] good = SaveGame.Write(Played(15UL));
            for (int i = 0; i < good.Length; i++)
            {
                byte[] bad = (byte[])good.Clone();
                bad[i] ^= 0x5A;

                var ex = Assert.Throws<SaveLoadException>(() => SaveGame.Load(bad, SimConfig.Tier1()));
                Assert.True(ex.Error == SaveLoadError.Corrupted || ex.Error == SaveLoadError.NotASave, "byte " + i + ": " + ex.Error);
            }
        }

        [Fact]
        public void EveryTruncation_IsDetected()
        {
            byte[] good = SaveGame.Write(Played(16UL));
            for (int len = 0; len < good.Length; len++)
            {
                byte[] bad = new byte[len];
                Array.Copy(good, bad, len);

                Assert.Throws<SaveLoadException>(() => SaveGame.Load(bad, SimConfig.Tier1()));
            }
        }

        [Fact]
        public void ForeignBytes_AreNotASave()
        {
            var ex = Assert.Throws<SaveLoadException>(() => SaveGame.Load(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, SimConfig.Tier1()));

            Assert.Equal(SaveLoadError.NotASave, ex.Error);
        }

        [Fact]
        public void NewerFormatVersion_IsRejected()
        {
            byte[] bytes = SaveGame.Write(Played(17UL));
            bytes[4] = 0xFF;
            bytes[5] = 0x7F;
            Reseal(bytes);

            var ex = Assert.Throws<SaveLoadException>(() => SaveGame.Load(bytes, SimConfig.Tier1()));

            Assert.Equal(SaveLoadError.UnsupportedVersion, ex.Error);
        }

        [Fact]
        public void TamperedStateWithValidChecksum_IsRejectedByStateHash()
        {
            byte[] bytes = SaveGame.Write(Played(18UL));
            const int energyOffset = 4 + 2 + 2 + 8 + 8 + 8 + 8 + 8; // header, then Tick (8), then Energy
            bytes[energyOffset] ^= 0x01;
            Reseal(bytes);

            var ex = Assert.Throws<SaveLoadException>(() => SaveGame.Load(bytes, SimConfig.Tier1()));

            Assert.Equal(SaveLoadError.Corrupted, ex.Error);
        }

        private static Simulation Played(ulong seed)
        {
            var sim = new Simulation(seed);
            sim.Run(700);
            sim.Execute(Command.SetDelegation(DelegationLevel.Delegated));
            sim.Run(SimConfig.TicksPerDay * 2);
            return sim;
        }

        private static void Reseal(byte[] bytes)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong h = offset;
            int end = bytes.Length - 8;
            unchecked
            {
                for (int i = 0; i < end; i++)
                {
                    h ^= bytes[i];
                    h *= prime;
                }
            }

            for (int i = 0; i < 8; i++)
            {
                bytes[end + i] = (byte)(h >> (i * 8));
            }
        }
    }
}
