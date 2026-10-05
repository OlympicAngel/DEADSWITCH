using Deadswitch.Game.Core;
using UnityEngine;

namespace Deadswitch.Game.Cloud
{
    /// <summary>Platform side of cloud backup. A service assembly replaces the default when its package is installed.</summary>
    public interface ICloudBackend
    {
        /// <summary>True when backups can reach a server in this build.</summary>
        bool Available { get; }

        /// <summary>Uploads the save; calls back with null on success or a player-facing problem.</summary>
        void Upload(byte[] save, System.Action<string> done);

        /// <summary>Downloads the last backup; calls back with the bytes (null if none) and a problem (null on success).</summary>
        void Download(System.Action<byte[], string> done);
    }

    /// <summary>
    /// Optional cloud backup (doc 10 s2: account link and backup, never required to play). Off by default; when on,
    /// the save is backed up on every pause. Restore replaces the local run after the handler confirms. The backup
    /// is a plain copy of the local save (ADR-0009), so a restored run verifies like any other.
    /// </summary>
    public static class CloudBackup
    {
        private const string KeyEnabled = "ds.cloud";
        private const string KeyLast = "ds.cloud.last";

        /// <summary>Set by the service assembly; reports "not available" otherwise.</summary>
        public static ICloudBackend Backend { get; set; } = new NoBackend();

        public static bool Enabled => PlayerPrefs.GetInt(KeyEnabled, 0) == 1;

        /// <summary>Unix ms of the last successful backup, 0 if none.</summary>
        public static long LastBackupMs => long.TryParse(PlayerPrefs.GetString(KeyLast, "0"), out long ms) ? ms : 0;

        public static void SetEnabled(bool on)
        {
            PlayerPrefs.SetInt(KeyEnabled, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>Backs the run up now (also on pause when enabled).</summary>
        public static void BackUp(System.Action<string> done = null)
        {
            GameHost host = GameHost.Instance;
            if (!Backend.Available || host == null || !host.IsReady)
            {
                done?.Invoke("Cloud backup is not available in this build.");
                return;
            }

            Backend.Upload(host.SaveBytes(), problem =>
            {
                if (problem == null)
                {
                    PlayerPrefs.SetString(KeyLast, System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
                    PlayerPrefs.Save();
                }

                done?.Invoke(problem);
            });
        }

        /// <summary>Downloads the backup and replaces the local run with it.</summary>
        public static void Restore(System.Action<string> done)
        {
            if (!Backend.Available)
            {
                done("Cloud backup is not available in this build.");
                return;
            }

            Backend.Download((bytes, problem) =>
            {
                if (problem != null || bytes == null)
                {
                    done(problem ?? "No backup found.");
                    return;
                }

                done(GameHost.Instance.RestoreFrom(bytes));
            });
        }

        private sealed class NoBackend : ICloudBackend
        {
            public bool Available => false;

            public void Upload(byte[] save, System.Action<string> done)
            {
                done("Cloud backup is not available in this build.");
            }

            public void Download(System.Action<byte[], string> done)
            {
                done(null, "Cloud backup is not available in this build.");
            }
        }
    }
}
