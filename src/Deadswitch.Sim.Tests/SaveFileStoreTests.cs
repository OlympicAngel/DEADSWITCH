using System;
using System.IO;
using Deadswitch.Host.Persistence;
using Deadswitch.Sim.State;
using Xunit;

namespace Deadswitch.Sim.Tests
{
    public sealed class SaveFileStoreTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "ds-save-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        [Fact]
        public void NoSave_LoadsNothingWithoutProblems()
        {
            var store = new SaveFileStore(Path.Combine(_dir, "slot.dsav"));

            SaveLoadReport report = store.Load(SimConfig.Tier1());

            Assert.Null(report.Game);
            Assert.False(report.AllCopiesUnreadable);
            Assert.False(store.Exists);
        }

        [Fact]
        public void SaveThenLoad_RoundTrips_AndKeepsPreviousAsBackup()
        {
            var store = new SaveFileStore(Path.Combine(_dir, "nested", "slot.dsav"));
            var sim = new Simulation(3UL);
            sim.Run(100);
            store.Save(sim);
            sim.Run(100);
            store.Save(sim);

            SaveLoadReport report = store.Load(SimConfig.Tier1());

            Assert.Equal(SaveSlotSource.Primary, report.Source);
            Assert.Equal(StateHasher.Hash(sim.State), StateHasher.Hash(report.Game!.Simulation.State));
            Assert.True(File.Exists(store.BackupPath));
            Assert.False(File.Exists(store.TempPath));
        }

        [Fact]
        public void CorruptPrimary_FallsBackToBackup()
        {
            var store = new SaveFileStore(Path.Combine(_dir, "slot.dsav"));
            var sim = new Simulation(4UL);
            sim.Run(100);
            store.Save(sim);
            ulong backupHash = StateHasher.Hash(sim.State);
            sim.Run(100);
            store.Save(sim);
            byte[] bytes = File.ReadAllBytes(store.PrimaryPath);
            File.WriteAllBytes(store.PrimaryPath, new ArraySegment<byte>(bytes, 0, bytes.Length / 2).ToArray());

            SaveLoadReport report = store.Load(SimConfig.Tier1());

            Assert.Equal(SaveSlotSource.Backup, report.Source);
            Assert.Equal(backupHash, StateHasher.Hash(report.Game!.Simulation.State));
            Assert.Single(report.Problems);
        }

        [Fact]
        public void InterruptedRotation_LeavesBackupLoadable()
        {
            // Crash between "primary -> bak" and "tmp -> primary": only the backup (and a stale tmp) exist.
            var store = new SaveFileStore(Path.Combine(_dir, "slot.dsav"));
            var sim = new Simulation(5UL);
            sim.Run(50);
            store.Save(sim);
            File.Move(store.PrimaryPath, store.BackupPath);
            File.WriteAllBytes(store.TempPath, new byte[] { 1, 2, 3 });

            SaveLoadReport report = store.Load(SimConfig.Tier1());

            Assert.Equal(SaveSlotSource.Backup, report.Source);

            sim.Run(10);
            store.Save(sim);
            Assert.Equal(SaveSlotSource.Primary, store.Load(SimConfig.Tier1()).Source);
        }

        [Fact]
        public void AllCopiesBroken_IsReported()
        {
            var store = new SaveFileStore(Path.Combine(_dir, "slot.dsav"));
            Directory.CreateDirectory(_dir);
            File.WriteAllBytes(store.PrimaryPath, new byte[] { 9, 9, 9 });
            File.WriteAllBytes(store.BackupPath, new byte[] { 8, 8, 8 });

            SaveLoadReport report = store.Load(SimConfig.Tier1());

            Assert.Null(report.Game);
            Assert.True(report.AllCopiesUnreadable);
            Assert.Equal(2, report.Problems.Count);
        }

        [Fact]
        public void DeleteAll_RemovesEveryCopy()
        {
            var store = new SaveFileStore(Path.Combine(_dir, "slot.dsav"));
            var sim = new Simulation(6UL);
            store.Save(sim);
            store.Save(sim);

            store.DeleteAll();

            Assert.False(store.Exists);
        }
    }
}
