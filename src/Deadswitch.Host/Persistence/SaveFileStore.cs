using System;
using System.Collections.Generic;
using System.IO;
using Deadswitch.Sim;
using Deadswitch.Sim.Persistence;

namespace Deadswitch.Host.Persistence
{
    /// <summary>Which copy a load came from.</summary>
    public enum SaveSlotSource
    {
        None = 0,
        Primary = 1,
        Backup = 2,
    }

    /// <summary>Result of <see cref="SaveFileStore.Load"/>: the game (or null for a fresh start) plus what went wrong on the way.</summary>
    public sealed class SaveLoadReport
    {
        internal SaveLoadReport(LoadedGame? game, SaveSlotSource source, List<string> problems)
        {
            Game = game;
            Source = source;
            Problems = problems;
        }

        /// <summary>Null when no readable save exists.</summary>
        public LoadedGame? Game { get; }

        public SaveSlotSource Source { get; }

        /// <summary>Human-readable reasons a copy was skipped (for logs and the AI's "memory fault" line).</summary>
        public IReadOnlyList<string> Problems { get; }

        /// <summary>True when a save existed but none could be read; the host should warn rather than silently restart.</summary>
        public bool AllCopiesUnreadable => Game == null && Problems.Count > 0;
    }

    /// <summary>
    /// Crash-safe save file with one backup (ADR-0009). Writes go to <c>.tmp</c>, are flushed to disk,
    /// then rotated: primary becomes <c>.bak</c>, tmp becomes primary. At every moment at least one
    /// complete copy exists. Loads try the primary, then the backup.
    /// </summary>
    public sealed class SaveFileStore
    {
        public SaveFileStore(string primaryPath)
        {
            PrimaryPath = primaryPath;
            BackupPath = primaryPath + ".bak";
            TempPath = primaryPath + ".tmp";
        }

        public string PrimaryPath { get; }

        public string BackupPath { get; }

        public string TempPath { get; }

        public bool Exists => File.Exists(PrimaryPath) || File.Exists(BackupPath);

        public void Save(Simulation sim)
        {
            Write(SaveGame.Write(sim));
        }

        public void Write(byte[] bytes)
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(PrimaryPath));
            if (dir != null)
            {
                Directory.CreateDirectory(dir);
            }

            using (var fs = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);
            }

            if (File.Exists(PrimaryPath))
            {
                if (File.Exists(BackupPath))
                {
                    File.Delete(BackupPath);
                }

                File.Move(PrimaryPath, BackupPath);
            }

            File.Move(TempPath, PrimaryPath);
        }

        public SaveLoadReport Load(SimConfig config)
        {
            var problems = new List<string>();
            LoadedGame? game = TryLoad(PrimaryPath, config, problems);
            if (game != null)
            {
                return new SaveLoadReport(game, SaveSlotSource.Primary, problems);
            }

            game = TryLoad(BackupPath, config, problems);
            if (game != null)
            {
                return new SaveLoadReport(game, SaveSlotSource.Backup, problems);
            }

            return new SaveLoadReport(null, SaveSlotSource.None, problems);
        }

        /// <summary>Removes every copy (new game). Irreversible: hosts must confirm with the player first.</summary>
        public void DeleteAll()
        {
            foreach (string p in new[] { PrimaryPath, BackupPath, TempPath })
            {
                if (File.Exists(p))
                {
                    File.Delete(p);
                }
            }
        }

        private static LoadedGame? TryLoad(string path, SimConfig config, List<string> problems)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return SaveGame.Load(File.ReadAllBytes(path), config);
            }
            catch (SaveLoadException ex)
            {
                problems.Add(Path.GetFileName(path) + ": " + ex.Error + " - " + ex.Message);
            }
            catch (IOException ex)
            {
                problems.Add(Path.GetFileName(path) + ": I/O - " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                problems.Add(Path.GetFileName(path) + ": access - " + ex.Message);
            }

            return null;
        }
    }
}
