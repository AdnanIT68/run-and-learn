using FootballRunner.Audio;
using UnityEngine;

namespace FootballRunner.Core
{
    /// <summary>
    /// Spawns the systems that must exist in every scene before anything else runs,
    /// so neither the menu nor the game scene has to hold a reference to them.
    /// </summary>
    public static class GameBootstrap
    {
        private const string PersistentSystemsPrefab = "PersistentSystems";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialise()
        {
            ConfigureRuntime();

            if (AudioManager.Instance != null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(PersistentSystemsPrefab);

            if (prefab == null)
            {
                Debug.LogWarning($"[GameBootstrap] Resources/{PersistentSystemsPrefab} not found. Audio will be unavailable.");
                return;
            }

            Object.Instantiate(prefab).name = PersistentSystemsPrefab;
        }

        /// <summary>
        /// Frame pacing and screen behaviour for handheld devices. Android defaults
        /// let the GPU render as fast as it can, which on a runner this simple means
        /// burning battery for frames nobody sees.
        /// </summary>
        private static void ConfigureRuntime()
        {
            // vSyncCount must be 0 for targetFrameRate to be respected at all.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // The player may go several seconds without touching the screen while
            // running, and the OS would otherwise dim and lock the display.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
