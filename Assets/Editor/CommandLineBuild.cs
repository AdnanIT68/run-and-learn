using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Batch-mode entry points, so builds run from a terminal without opening the Editor:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod FootballRunner.EditorTools.CommandLineBuild.BuildAndroidApk
    /// </summary>
    public static class CommandLineBuild
    {
        private const string OutputFolder = "Builds";

        /// <summary>
        /// A debug-signed APK for installing straight onto a test phone. Store
        /// uploads use an App Bundle signed with the upload key instead.
        /// </summary>
        public static void BuildAndroidApk()
        {
            AndroidSetup.Configure();
            EditorUserBuildSettings.buildAppBundle = false;

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Directory.CreateDirectory(OutputFolder);
            string output = Path.Combine(OutputFolder, "RunAndLearn-test.apk");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"[CommandLineBuild] {summary.result}: {output}, {summary.totalSize / (1024f * 1024f):F1} MB, " +
                      $"{summary.totalErrors} errors, {summary.totalTime.TotalSeconds:F0}s");

            // Restore the store default so a later manual build still produces a bundle.
            EditorUserBuildSettings.buildAppBundle = true;

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
