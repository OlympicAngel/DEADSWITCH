using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Host.Notifications;
using UnityEngine;

namespace Deadswitch.Game.Notifications
{
    /// <summary>Platform side of local notifications. The mobile assembly replaces the default when available.</summary>
    public interface IAlertBackend
    {
        /// <summary>Asks the OS for permission where needed (called when the handler opts in).</summary>
        void RequestPermission();

        void Schedule(IReadOnlyList<ProjectedAlert> alerts, double secondsPerGameMinute);

        void CancelAll();
    }

    /// <summary>
    /// Opt-in local notifications (SPEC-010): on pause/quit the logout projection's alerts are scheduled, on
    /// return everything is cancelled. Off by default; nothing is scheduled while off.
    /// </summary>
    public static class LocalAlerts
    {
        private const string KeyEnabled = "ds.notify";

        /// <summary>Set by the mobile assembly; logs in the Editor and on platforms without a backend.</summary>
        public static IAlertBackend Backend { get; set; } = new LogBackend();

        public static bool Enabled => PlayerPrefs.GetInt(KeyEnabled, 0) == 1;

        public static void SetEnabled(bool on)
        {
            PlayerPrefs.SetInt(KeyEnabled, on ? 1 : 0);
            PlayerPrefs.Save();
            if (on)
            {
                Backend.RequestPermission();
            }
            else
            {
                Backend.CancelAll();
            }
        }

        /// <summary>Called when the app goes to the background or quits.</summary>
        public static void OnLeave(GameHost host)
        {
            Backend.CancelAll();
            if (!Enabled || host == null || !host.IsReady)
            {
                return;
            }

            List<ProjectedAlert> alerts = LogoutProjection.Project(host.Sim, AlertKinds.All);
            Backend.Schedule(alerts, 60.0 / host.Settings.DevTimeScale);
        }

        /// <summary>Called when the app returns: the forecast is stale and the handler is here.</summary>
        public static void OnReturn()
        {
            Backend.CancelAll();
        }

        private sealed class LogBackend : IAlertBackend
        {
            public void RequestPermission()
            {
            }

            public void Schedule(IReadOnlyList<ProjectedAlert> alerts, double secondsPerGameMinute)
            {
                foreach (ProjectedAlert a in alerts)
                {
                    Debug.Log("[DEADSWITCH] alert in " + (a.InMinutes * secondsPerGameMinute).ToString("0") + " s: " + a.Title + " // " + a.Body);
                }
            }

            public void CancelAll()
            {
            }
        }
    }
}
