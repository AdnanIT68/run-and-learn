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
            PlayerSettings.Android.useCustomKeystore = false;

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

        /// <summary>
        /// The Google Play upload: an App Bundle signed with the upload key. The key and
        /// its passwords stay outside the project, in the folder named by the
        /// RUNLEARN_SIGNING environment variable or %USERPROFILE%\RunAndLearnSigning,
        /// whose signing.properties holds keystore=, alias=, storePassword=, keyPassword=.
        /// </summary>
        public static void BuildAndroidBundle()
        {
            AndroidSetup.Configure();
            bool signed = ApplySigning();

            if (!signed)
            {
                Debug.LogError("[CommandLineBuild] No upload key found; the bundle was not built.");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            EditorUserBuildSettings.buildAppBundle = true;
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Directory.CreateDirectory(OutputFolder);
            string output = Path.Combine(OutputFolder,
                $"RunAndLearn-{AndroidSetup.VersionName}-{AndroidSetup.VersionCode}.aab");

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            BuildSummary summary = report.summary;
            Debug.Log($"[CommandLineBuild] {summary.result}: {output}, {summary.totalSize / (1024f * 1024f):F1} MB, " +
                      $"{summary.totalErrors} errors, {summary.totalTime.TotalSeconds:F0}s");

            // Passwords are never left in the project settings.
            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;
            PlayerSettings.Android.useCustomKeystore = false;
            AssetDatabase.SaveAssets();

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        private static bool ApplySigning()
        {
            string folder = System.Environment.GetEnvironmentVariable("RUNLEARN_SIGNING");

            if (string.IsNullOrEmpty(folder))
            {
                folder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                    "RunAndLearnSigning");
            }

            string file = Path.Combine(folder, "signing.properties");

            if (!File.Exists(file))
            {
                return false;
            }

            var values = File.ReadAllLines(file)
                .Where(line => line.Contains("=") && !line.TrimStart().StartsWith("#"))
                .Select(line => line.Split(new[] { '=' }, 2))
                .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());

            string keystore = Path.Combine(folder, values["keystore"]);

            if (!File.Exists(keystore))
            {
                return false;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = values["storePassword"];
            PlayerSettings.Android.keyaliasName = values["alias"];
            PlayerSettings.Android.keyaliasPass = values["keyPassword"];
            return true;
        }
    }
}
