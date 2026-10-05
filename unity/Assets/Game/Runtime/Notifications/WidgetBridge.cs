using System.Collections.Generic;
using Deadswitch.Game.Core;
using Deadswitch.Host.Narrative;
using Deadswitch.Host.Notifications;
using Deadswitch.Sim.State;
using Deadswitch.Sim.Systems;
using UnityEngine;

namespace Deadswitch.Game.Notifications
{
    /// <summary>
    /// Feeds the home / lock-screen widget (doc 08 s5: live threat level, heat and next timer). On leaving, a
    /// snapshot is written where the native widget reads it: Android SharedPreferences "deadswitch_widget", iOS an
    /// App Group (with the DS_IOS_WIDGET define). The native widgets live in unity/NativeWidgets~ until the owner
    /// installs them; without them this writes harmlessly and the refresh call is skipped.
    /// </summary>
    public static class WidgetBridge
    {
        public const string Prefs = "deadswitch_widget";

#if UNITY_IOS && DS_IOS_WIDGET && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _DsWidgetWrite(string threat, string heat, string next);
#endif

        /// <summary>The three lines the widget shows (also used by the Editor log).</summary>
        public static void Snapshot(GameHost host, out string threat, out string heat, out string next)
        {
            GameState s = host.Sim.State;
            Faction hottest = WorldSystem.Hottest(s);
            int pct = WorldSystem.Percent(s.Heat[(int)hottest]);
            threat = s.RaidId != 0 ? "ATTACK INBOUND" : s.PurgeStage != PurgeStage.None ? "PURGE ON THE MAP" : WorldSystem.Level(s.Heat[(int)hottest]) >= HeatLevel.Hunted ? "HUNTED" : "QUIET";
            heat = Names.Faction(hottest) + " " + WorldSystem.Level(s.Heat[(int)hottest]).ToString().ToUpperInvariant() + " " + pct;
            List<ProjectedAlert> alerts = LogoutProjection.Project(host.Sim, AlertKinds.All);
            next = "NOTHING FORECAST";
            if (alerts.Count > 0)
            {
                double seconds = alerts[0].InMinutes * 60.0 / host.Settings.DevTimeScale;
                next = alerts[0].Title + " ~" + System.DateTime.Now.AddSeconds(seconds).ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Called when the app goes to the background or quits.</summary>
        public static void OnLeave(GameHost host)
        {
            if (host == null || !host.IsReady)
            {
                return;
            }

            Snapshot(host, out string threat, out string heat, out string next);
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject prefs = activity.Call<AndroidJavaObject>("getSharedPreferences", Prefs, 0))
                using (AndroidJavaObject edit = prefs.Call<AndroidJavaObject>("edit"))
                {
                    edit.Call<AndroidJavaObject>("putString", "threat", threat).Dispose();
                    edit.Call<AndroidJavaObject>("putString", "heat", heat).Dispose();
                    edit.Call<AndroidJavaObject>("putString", "next", next).Dispose();
                    edit.Call("apply");
                    try
                    {
                        using (var widget = new AndroidJavaClass("com.deadswitch.widget.DeadswitchWidget"))
                        {
                            widget.CallStatic("refresh", activity);
                        }
                    }
                    catch (AndroidJavaException)
                    {
                        // the widget plugin is not installed in this build
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[DEADSWITCH] Widget snapshot failed: " + ex.Message);
            }
#elif UNITY_IOS && DS_IOS_WIDGET && !UNITY_EDITOR
            _DsWidgetWrite(threat, heat, next);
#else
            Debug.Log("[DEADSWITCH] widget // " + threat + " // " + heat + " // " + next);
#endif
        }
    }
}
