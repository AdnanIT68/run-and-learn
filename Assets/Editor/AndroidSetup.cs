using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Applies the Android player settings required for a Google Play release.
    /// Kept in source rather than clicked once in the Inspector so the
    /// configuration is reviewable and survives a fresh clone of the project.
    /// </summary>
    internal static class AndroidSetup
    {
        private const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
        private const string GameScene = "Assets/Scenes/Game.unity";

        /// <summary>
        /// Google Play requires new releases to target a recent API level. Cast from
        /// an int rather than naming the enum member so this still compiles on an
        /// editor version whose enum predates the level.
        /// </summary>
        private const int TargetApiLevel = 36;

        public const string ProductName = "Run and Learn";
        public const string CompanyName = "Rukajja Games";
        public const string ApplicationId = "com.rukajja.runlearn";

        /// <summary>Shown to players. Raise with every Play upload, together with VersionCode.</summary>
        public const string VersionName = "1.1.0";

        /// <summary>Play's internal number; every upload must be higher than the last.</summary>
        public const int VersionCode = 3;

        [MenuItem("Tools/Football Runner/4 - Configure Android")]
        public static void Configure()
        {
            ConfigureScenes();
            ConfigurePlayerSettings();

            AssetDatabase.SaveAssets();
            Report();
        }

        /// <summary>
        /// Scene order decides what the app opens into. The menu must be index 0,
        /// or launching the game drops the player straight into a run.
        /// </summary>
        private static void ConfigureScenes()
        {
            var wanted = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainMenuScene, true),
                new EditorBuildSettingsScene(GameScene, true)
            };

            // Preserve any extra scenes the project may gain later, just after ours.
            IEnumerable<EditorBuildSettingsScene> extras = EditorBuildSettings.scenes
                .Where(scene => scene.path != MainMenuScene && scene.path != GameScene);

            wanted.AddRange(extras);
            EditorBuildSettings.scenes = wanted.ToArray();
        }

        private static void ConfigurePlayerSettings()
        {
            var android = NamedBuildTarget.Android;

            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            // Permanent once the app is on Google Play: never change after the first upload.
            PlayerSettings.SetApplicationIdentifier(android, ApplicationId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);

            // Low stripping only. Medium and High can remove members that are only
            // ever reached through reflection, which fails at runtime, not at build.
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);

            // Play refuses 32-bit-only uploads, and ARM64-only keeps the download small.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetApiLevel;

            // Smooths frame delivery on Android rather than raising the frame rate.
            PlayerSettings.Android.optimizedFramePacing = true;

            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.androidTVCompatibility = false;

            // An App Bundle is mandatory for new Play Store submissions.
            EditorUserBuildSettings.buildAppBundle = true;

            // Landscape only, matching the existing project configuration.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.bundleVersion = VersionName;
            PlayerSettings.Android.bundleVersionCode = VersionCode;

            // The game never goes online; RemoveInternetPermission strips what Unity adds anyway.
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;

            ConfigureIcons(android);
        }

        private const string IconFolder = "Assets/Art/AppIcon/";

        /// <summary>
        /// Launcher icon from Tools/make_store_art.py as Android adaptive layers.
        /// Unity 6 builds the fallback for pre-Android 8 launchers from these.
        /// </summary>
        private static void ConfigureIcons(NamedBuildTarget android)
        {
            var back = AssetDatabase.LoadAssetAtPath<Texture2D>(IconFolder + "adaptive_background.png");
            var front = AssetDatabase.LoadAssetAtPath<Texture2D>(IconFolder + "adaptive_foreground.png");

            if (back == null || front == null)
            {
                Debug.LogWarning("[AndroidSetup] App icon textures missing; run Tools/make_store_art.py.");
                return;
            }

            PlatformIcon[] adaptive = PlayerSettings.GetPlatformIcons(android,
                UnityEditor.Android.AndroidPlatformIconKind.Adaptive);

            foreach (PlatformIcon icon in adaptive)
            {
                icon.SetTextures(back, front);
            }

            PlayerSettings.SetPlatformIcons(android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, adaptive);
        }

        [MenuItem("Tools/Football Runner/5 - Report Android Config")]
        public static void Report()
        {
            var android = NamedBuildTarget.Android;

            string scenes = string.Join(", ", EditorBuildSettings.scenes
                .Select((scene, index) => $"{index}:{System.IO.Path.GetFileNameWithoutExtension(scene.path)}" +
                                          (scene.enabled ? "" : " (disabled)")));

            Debug.Log(
                "[AndroidSetup] Configuration\n" +
                $"  Product          : {PlayerSettings.productName} by {PlayerSettings.companyName}\n" +
                $"  Package          : {PlayerSettings.GetApplicationIdentifier(android)}\n" +
                $"  Version          : {PlayerSettings.bundleVersion} (code {PlayerSettings.Android.bundleVersionCode})\n" +
                $"  Scripting        : {PlayerSettings.GetScriptingBackend(android)}\n" +
                $"  Stripping        : {PlayerSettings.GetManagedStrippingLevel(android)}\n" +
                $"  Architectures    : {PlayerSettings.Android.targetArchitectures}\n" +
                $"  Min / Target SDK : {PlayerSettings.Android.minSdkVersion} / {PlayerSettings.Android.targetSdkVersion}\n" +
                $"  App Bundle       : {EditorUserBuildSettings.buildAppBundle}\n" +
                $"  Frame pacing     : {PlayerSettings.Android.optimizedFramePacing}\n" +
                $"  Keystore         : {(string.IsNullOrEmpty(PlayerSettings.Android.keystoreName) ? "NOT SET - required for release" : PlayerSettings.Android.keystoreName)}\n" +
                $"  Scenes           : {scenes}");
        }
    }
}
