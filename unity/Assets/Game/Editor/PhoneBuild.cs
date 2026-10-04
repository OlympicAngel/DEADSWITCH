using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Deadswitch.Game.Editor
{
    /// <summary>
    /// One-click phone builds for playtesting: DEADSWITCH > Build Android APK, or Build And Run with the phone on
    /// USB (USB debugging on). Sets the few player settings a sideloaded test build needs (identifier, IL2CPP
    /// ARM64, portrait) and writes the APK to unity/Builds/Android (git-ignored).
    /// </summary>
    public static class PhoneBuild
    {
        private const string Identifier = "com.deadswitch.game";
        private const string OutputDir = "Builds/Android";

        [MenuItem("DEADSWITCH/Build Android APK (dev)")]
        public static void BuildApk()
        {
            Build(false);
        }

        [MenuItem("DEADSWITCH/Build And Run on Android Phone (dev)")]
        public static void BuildAndRun()
        {
            Build(true);
        }

        private static void Build(bool run)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("DEADSWITCH", "Android Build Support is not installed for this Editor. Add it in Unity Hub > Installs > Add modules.", "OK");
                return;
            }

            PlayerSettings.companyName = "Deadswitch";
            PlayerSettings.productName = "DEADSWITCH";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, Identifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = false;

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutputDir, "DEADSWITCH-dev.apk"),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | (run ? BuildOptions.AutoRunPlayer : BuildOptions.None),
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[DEADSWITCH] Android build OK: " + options.locationPathName + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
                if (!run)
                {
                    EditorUtility.RevealInFinder(options.locationPathName);
                }
            }
            else
            {
                Debug.LogError("[DEADSWITCH] Android build " + report.summary.result + " with " + report.summary.totalErrors + " errors. See the Console.");
            }
        }
    }
}
