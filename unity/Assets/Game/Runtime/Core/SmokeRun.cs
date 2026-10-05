using System.Collections;
using System.Collections.Generic;
using System.Text;
using Deadswitch.Game.Base;
using Deadswitch.Game.UI;
using Deadswitch.Game.UI.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deadswitch.Game.Core
{
    /// <summary>
    /// Self-running smoke check for the Editor pass and the phone run (F-099). Launch with <c>-ds-smoke</c> (Android:
    /// <c>adb shell am start -n &lt;pkg&gt;/com.unity3d.player.UnityPlayerActivity -e unity -ds-smoke</c>): it opens every
    /// screen in turn, renders the sector map, counts the map pins, reads which owner services have a backend, and
    /// writes PASS/FAIL lines plus every error logged on the way to <c>smoke.txt</c> in the persistent data path and the
    /// log. <c>-ds-smoke-quit</c> also quits when done. It never sends commands to the sim, so the save is untouched.
    /// </summary>
    public sealed class SmokeRun : MonoBehaviour
    {
        private readonly List<string> _errors = new List<string>();
        private readonly StringBuilder _report = new StringBuilder();
        private int _failed;

        public static bool Requested()
        {
            return Has("-ds-smoke") || Has("-ds-smoke-quit");
        }

        private static bool Has(string arg)
        {
            foreach (string a in System.Environment.GetCommandLineArgs())
            {
                if (a == arg)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLog;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _errors.Add(type + ": " + message);
            }
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            float until = Time.realtimeSinceStartup + 30f;
            while ((GameHost.Instance == null || !GameHost.Instance.IsReady || HudController.Instance == null || HudController.Instance.Router == null) && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            Check("boot", GameHost.Instance != null && GameHost.Instance.IsReady && HudController.Instance != null && HudController.Instance.Router != null, "host or HUD missing after 30 s");
            if (_failed == 0)
            {
                yield return Screens(HudController.Instance.Router);
            }

            Check("base view", BaseView.Instance != null, "BaseView missing");
            Note("cloud backend", Cloud.CloudBackup.Backend != null && Cloud.CloudBackup.Backend.Available);
            Note("store backend", Store.Entitlements.Backend != null && Store.Entitlements.Backend.Available);
            Note("ad backend", Store.RewardedAds.Backend != null);
            Check("no errors logged", _errors.Count == 0, _errors.Count + " error(s), listed below");
            foreach (string e in _errors)
            {
                _report.AppendLine("  " + e);
            }

            _report.Insert(0, (_failed == 0 ? "SMOKE PASS" : "SMOKE FAIL (" + _failed + ")") + " " + Application.version + "\n");
            string path = System.IO.Path.Combine(Application.persistentDataPath, "smoke.txt");
            System.IO.File.WriteAllText(path, _report.ToString());
            Debug.Log("[smoke] report at " + path + "\n" + _report);
            if (Has("-ds-smoke-quit"))
            {
                Application.Quit(_failed == 0 ? 0 : 1);
            }
        }

        private IEnumerator Screens(ScreenRouter router)
        {
            foreach (string id in router.Ids)
            {
                int before = _errors.Count;
                router.Show(id);
                for (int f = 0; f < 20; f++)
                {
                    yield return null;
                }

                VisualElement root = router.Get(id).Root;
                bool laidOut = root.resolvedStyle.display == DisplayStyle.Flex && root.layout.width > 1f && root.layout.height > 1f;
                Check("screen " + id, laidOut && _errors.Count == before, laidOut ? (_errors.Count - before) + " error(s) while open" : "not laid out");
                if (id == "map")
                {
                    MapView view = MapView.Instance;
                    Check("map render", view != null && view.Texture != null, "no map texture (check Rendering/MapRender and layer 30)");
                    int pins = root.Query<VisualElement>(className: "map-site").ToList().Count;
                    Check("map pins", pins > 0, "no pins found");
                }
            }

            router.Show("base");
        }

        private void Check(string name, bool ok, string why)
        {
            if (!ok)
            {
                _failed++;
            }

            _report.AppendLine((ok ? "PASS " : "FAIL ") + name + (ok ? string.Empty : ": " + why));
        }

        private void Note(string name, bool present)
        {
            _report.AppendLine("INFO " + name + ": " + (present ? "present" : "none (package not installed)"));
        }
    }
}
