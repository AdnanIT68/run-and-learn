using System.Collections;
using FootballRunner.Adventure;
using FootballRunner.Collectibles;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>
    /// Runs the real Game scene and checks the core loop actually works.
    /// These exist because a runner that compiles is not the same as a runner
    /// that runs.
    /// </summary>
    [Timeout(60000)]
    public class GameLoopTests
    {
        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
            Debug.Log("[TEST] " + TestContext.CurrentContext.Test.Name);
            Time.timeScale = 1f;
            AdventureSession.Clear();
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator SceneContainsCoreSystems()
        {
            yield return null;

            Assert.IsNotNull(PlayerController.Instance, "No PlayerController in the scene.");
            Assert.IsNotNull(GameManager.Instance, "No GameManager in the scene.");
            Assert.IsNotNull(ScoreManager.Instance, "No ScoreManager in the scene.");
            Assert.IsNotNull(DifficultyManager.Instance, "No DifficultyManager in the scene.");
            Assert.IsNotNull(EnvironmentDirector.Instance, "No EnvironmentDirector in the scene.");
        }

        [UnityTest]
        public IEnumerator RunStartsInItsWorldWithNoMusic()
        {
            yield return null;

            // The run opens in the chosen world and is named after it.
            string world = FootballRunner.Learning.LearningProgress.SelectedPack;
            string title = FootballRunner.Learning.WordPack.Load(world)
                .Title(FootballRunner.Learning.LearningProgress.Language).ToUpperInvariant();
            StringAssert.StartsWith(title, EnvironmentDirector.Instance.CurrentDisplayName);

            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            for (int i = 0; i < sources.Length; i++)
            {
                Assert.IsFalse(sources[i].isPlaying && sources[i].loop,
                    "A looping audio source was playing. Background music must stay off.");
            }
        }

        [UnityTest]
        public IEnumerator GameStartsInPlayingState()
        {
            yield return null;

            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator WarmUpLastsTwentyFourSeconds()
        {
            yield return null;

            Assert.AreEqual(24f, DifficultyManager.Instance.WarmUpSeconds);
            Assert.IsTrue(DifficultyManager.Instance.IsWarmUp);
            Assert.AreEqual(0f, DifficultyManager.Instance.CurrentObstacleChance);
        }

        [UnityTest]
        public IEnumerator RukajjaSpriteIsPlantedWithoutMovingTheCollider()
        {
            yield return new WaitForSeconds(1.2f);

            Transform player = PlayerController.Instance.transform;
            Transform visual = player.Find("Visual_Rukajja");
            var capsule = player.GetComponent<CapsuleCollider2D>();

            Assert.IsNotNull(visual, "Visual_Rukajja child is missing.");
            Assert.Less(visual.localPosition.y, -0.88f, "Rukajja visual is still hovering above the ground.");
            Assert.Greater(visual.localPosition.y, -1.05f, "Rukajja visual was lowered far enough to sink into the ground.");
            Assert.AreEqual(1.5f, capsule.size.y, 0.01f, "Capsule height must stay at 1.5.");
            Assert.AreEqual(0f, capsule.offset.y, 0.01f, "Capsule offset must stay at zero.");
            Assert.IsTrue(PlayerController.Instance.IsGrounded);
        }

        [UnityTest]
        public IEnumerator IntroHorizonDoesNotSpawnGapsOrKenneyScenery()
        {
            yield return null;

            Chunk[] chunks = Object.FindObjectsByType<Chunk>(FindObjectsSortMode.None);
            for (int i = 0; i < chunks.Length; i++)
            {
                Assert.IsFalse(
                    chunks[i].name.IndexOf("Gap", System.StringComparison.OrdinalIgnoreCase) >= 0,
                    "A pit was spawned inside the warm-up arrival window.");
            }

            var scenic = Object.FindObjectsByType<ScenicSetDressingRecycler>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < scenic.Length; i++)
            {
                Assert.IsFalse(scenic[i].gameObject.activeInHierarchy,
                    "Kenney ScenicSetDressing is still visible.");
            }

            Transform distant = GameObject.Find("Background")?.transform.Find("DistantHills");
            if (distant == null)
            {
                Transform background = GameObject.Find("Background")?.transform;
                if (background != null)
                {
                    for (int i = 0; i < background.childCount; i++)
                    {
                        Transform child = background.GetChild(i);
                        if (child.name == "DistantHills")
                        {
                            distant = child;
                            break;
                        }
                    }
                }
            }

            if (distant != null)
            {
                Assert.IsFalse(distant.gameObject.activeInHierarchy,
                    "Kenney DistantHills is still visible.");
            }

            // Each word world draws its own backdrop in place of the old jungle layers.
            Assert.IsNotNull(GameObject.Find("WorldFar"), "The world's far backdrop is missing.");
            Assert.IsNotNull(GameObject.Find("WorldMid"), "The world's middle layer is missing.");
            Assert.IsNull(GameObject.Find("SunnyLand_Back"), "The old jungle backdrop is still showing.");
        }

        [UnityTest]
        public IEnumerator PlayerRunsForwardAutomatically()
        {
            yield return null;

            Transform player = PlayerController.Instance.transform;
            float startX = player.position.x;

            yield return new WaitForSeconds(2f);

            float travelled = player.position.x - startX;
            Assert.Greater(travelled, 8f, $"Player only travelled {travelled:F2} units in 2 seconds.");
        }

        [UnityTest]
        public IEnumerator PlayerLandsOnGroundAndStaysThere()
        {
            yield return new WaitForSeconds(1.5f);

            Transform player = PlayerController.Instance.transform;

            Assert.Greater(player.position.y, -1f, "Player fell through the ground.");
            Assert.IsTrue(PlayerController.Instance.IsGrounded, "Player never became grounded.");
        }

        [UnityTest]
        public IEnumerator TrackIsGeneratedAheadOfPlayer()
        {
            yield return null;

            Chunk[] chunks = Object.FindObjectsByType<Chunk>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(chunks.Length, 3, "Too few chunks generated at start.");
        }

        [UnityTest]
        public IEnumerator TrackKeepsGeneratingWithoutGrowingUnbounded()
        {
            yield return new WaitForSeconds(1f);
            int early = Object.FindObjectsByType<Chunk>(FindObjectsSortMode.None).Length;

            yield return new WaitForSeconds(6f);
            int later = Object.FindObjectsByType<Chunk>(FindObjectsSortMode.None).Length;

            Assert.Greater(later, 0, "All chunks disappeared.");
            Assert.Less(later, early + 10, $"Chunk count grew from {early} to {later}; recycling is not working.");
        }

        [UnityTest]
        public IEnumerator CollectiblesAppearInTheWorld()
        {
            yield return new WaitForSeconds(3f);

            Collectible[] collectibles = Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None);
            Assert.Greater(collectibles.Length, 0, "No collectibles were spawned.");
        }

        [UnityTest]
        public IEnumerator ScoreIncreasesWithDistance()
        {
            yield return new WaitForSeconds(2.5f);

            Assert.Greater(ScoreManager.Instance.Score, 0, "Score did not increase while running.");
        }

        [UnityTest]
        public IEnumerator DamageCostsOneHeartAndGrantsInvulnerability()
        {
            yield return null;

            var health = PlayerController.Instance.GetComponent<PlayerHealth>();
            int before = health.CurrentHealth;

            health.TakeDamage(1);

            Assert.AreEqual(before - 1, health.CurrentHealth, "Damage did not reduce health by one.");
            Assert.IsTrue(health.IsInvulnerable, "Player was not made invulnerable after a hit.");

            // A second hit during the window must be ignored.
            health.TakeDamage(1);
            Assert.AreEqual(before - 1, health.CurrentHealth, "Invulnerability did not block the second hit.");
        }

        [UnityTest]
        public IEnumerator RunningOutOfHealthEndsTheRun()
        {
            yield return null;

            var health = PlayerController.Instance.GetComponent<PlayerHealth>();
            health.Kill();

            yield return null;

            Assert.AreEqual(0, health.CurrentHealth);
            Assert.AreEqual(GameState.Dying, GameManager.Instance.State, "Death should play a short transition before Game Over.");
            Assert.AreEqual(1f, Time.timeScale, "Time should keep running during the death transition.");

            // Realtime: Game Over sets timeScale to 0, so a scaled wait would never finish.
            yield return new WaitForSecondsRealtime(1.2f);

            Assert.AreEqual(GameState.GameOver, GameManager.Instance.State);
            Assert.AreEqual(0f, Time.timeScale, "Gameplay did not stop on game over.");
        }

        [UnityTest]
        public IEnumerator NewBestFlagUsesScoreFromBeforeSave()
        {
            yield return null;

            int previousBest = SaveManager.Data.highScore;
            int score = ScoreManager.Instance.Score;
            var health = PlayerController.Instance.GetComponent<PlayerHealth>();
            health.Kill();

            yield return null;

            Assert.AreEqual(score > previousBest, GameManager.Instance.LastRunWasNewBest,
                "NEW BEST must compare against the high score from before this run was saved.");
        }

        [UnityTest]
        public IEnumerator PauseAndResumeToggleTimeScale()
        {
            yield return null;

            GameManager.Instance.Pause();
            Assert.AreEqual(GameState.Paused, GameManager.Instance.State);
            Assert.AreEqual(0f, Time.timeScale, "Pause did not stop time.");

            GameManager.Instance.Resume();
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
            Assert.AreEqual(1f, Time.timeScale, "Resume did not restore time.");
        }

        [UnityTest]
        public IEnumerator CollectiblesAwardPositivePointsExactlyOnce()
        {
            yield return new WaitForSeconds(1f);

            int before = ScoreManager.Instance.Score;
            ScoreManager.Instance.AddPoints(-10);
            Assert.AreEqual(before, ScoreManager.Instance.Score,
                "Negative pickup values must not reduce the score.");

            ScoreManager.Instance.AddPoints(10);
            Assert.AreEqual(before + 10, ScoreManager.Instance.Score,
                "Collectibles must add positive points.");
        }

        [UnityTest]
        public IEnumerator PlayWorksAgainAfterReturningToTheMenu()
        {
            yield return null;

            GameManager.Instance.GoToMainMenu();
            yield return WaitForActiveScene(GameManager.MainMenuSceneName);
            Assert.IsFalse(GameManager.IsLoadingScene, "The scene-load guard stayed locked in the main menu.");

            // The menu's Play button goes through the same guarded load.
            GameManager.LoadSceneClean(GameManager.GameSceneName);
            yield return WaitForActiveScene(GameManager.GameSceneName);
            yield return null;

            Assert.IsNotNull(GameManager.Instance, "Play from the menu did not start a new run.");
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
        }

        private static IEnumerator WaitForActiveScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name, "Scene did not load: " + sceneName);
        }

        [UnityTest]
        public IEnumerator DifficultyRaisesSpeedOverTime()
        {
            yield return null;

            float startSpeed = DifficultyManager.Instance.CurrentSpeed;

            yield return new WaitForSeconds(5f);

            float laterSpeed = DifficultyManager.Instance.CurrentSpeed;
            Assert.Greater(laterSpeed, startSpeed, "Run speed did not increase over time.");
        }
    }
}
