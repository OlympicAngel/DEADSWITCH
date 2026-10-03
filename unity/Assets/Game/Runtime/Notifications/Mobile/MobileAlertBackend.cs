using System;
using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Host.Notifications;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace Deadswitch.Game.Notifications
{
    /// <summary>
    /// Android / iOS local notifications through com.unity.mobile.notifications (SPEC-010). This assembly only
    /// compiles when that package is installed (versionDefines), so the game has no hard dependency on it.
    /// </summary>
    public sealed class MobileAlertBackend : IAlertBackend
    {
        private const string Channel = "ds_alerts";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Bootstrap.Booted += _ =>
            {
#if UNITY_ANDROID || UNITY_IOS
                LocalAlerts.Backend = new MobileAlertBackend();
#endif
            };
        }

        public void RequestPermission()
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(Channel, "Core alerts", "Raids, construction and restored modules while you are away.", Importance.Default));
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            {
                UnityEngine.Android.Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            }
#elif UNITY_IOS
            new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Sound, false);
#endif
        }

        public void Schedule(IReadOnlyList<ProjectedAlert> alerts, double secondsPerGameMinute)
        {
            for (int i = 0; i < alerts.Count; i++)
            {
                ProjectedAlert a = alerts[i];
                double seconds = Math.Max(5.0, a.InMinutes * secondsPerGameMinute);
#if UNITY_ANDROID
                AndroidNotificationCenter.SendNotification(new AndroidNotification(a.Title, a.Body, DateTime.Now.AddSeconds(seconds)), Channel);
#elif UNITY_IOS
                iOSNotificationCenter.ScheduleNotification(new iOSNotification
                {
                    Identifier = "ds_" + i,
                    Title = a.Title,
                    Body = a.Body,
                    ShowInForeground = false,
                    Trigger = new iOSNotificationTimeIntervalTrigger { TimeInterval = TimeSpan.FromSeconds(seconds), Repeats = false },
                });
#endif
            }
        }

        public void CancelAll()
        {
#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#elif UNITY_IOS
            iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
        }
    }
}
