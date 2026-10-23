using System.Collections;
using FootballRunner.Adventure;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    public class AdventureSliceTests
    {
        [UnitySetUp]
        public IEnumerator LoadAdventureLevel()
        {
            Time.timeScale = 1f;
            AdventureSession.BeginLevel(1);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void Cleanup()
        {
            Time.timeScale = 1f;
            AdventureSession.Clear();
        }

        [UnityTest]
        public IEnumerator AdventureStartsOnIntroNotEndless()
        {
            yield return null;

            Assert.IsTrue(AdventureSession.IsActive);
            Assert.AreEqual(GameState.Ready, GameManager.Instance.State);
            Assert.IsNotNull(AdventureCourse.Instance);
            Assert.IsFalse(Object.FindFirstObjectByType<LevelGenerator>().enabled,
                "Endless generator must stay off during the finite level.");
        }

        [UnityTest]
        public IEnumerator FiniteCourseHasAFinishAndThreeStars()
        {
            yield return null;

            Assert.Greater(AdventureCourse.Instance.FinishX, 200f, "The course is too short to feel like a level.");
            Assert.IsNotNull(Object.FindFirstObjectByType<AdventureFinishLine>(), "Finish line is missing.");
            Assert.AreEqual(3, Object.FindObjectsByType<AdventureStarMarker>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator StartingTheIntroBeginsTheRun()
        {
            yield return null;

            GameManager.Instance.BeginAdventureRun();
            Assert.AreEqual(GameState.Playing, GameManager.Instance.State);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void CompletingLevelOneUnlocksLevelTwo()
        {
            SaveManager.Load();
            int previousUnlocked = SaveManager.Data.greenHillsUnlockedLevel;
            int previousStars = SaveManager.Data.greenHillsLevel1Stars;
            int previousCoins = SaveManager.Data.totalCoins;
            try
            {
                SaveManager.Data.greenHillsUnlockedLevel = 1;
                SaveManager.Data.greenHillsLevel1Stars = 0;
                AdventureSession.BeginLevel(1);
                AdventureSession.RecordFinish(2, 0, 90f);

                Assert.AreEqual(2, SaveManager.Data.greenHillsUnlockedLevel);
                Assert.AreEqual(2, SaveManager.Data.greenHillsLevel1Stars);
                Assert.IsTrue(AdventureSession.IsLevelUnlocked(2));
                Assert.IsFalse(AdventureSession.IsLevelUnlocked(3));
            }
            finally
            {
                SaveManager.Data.greenHillsUnlockedLevel = previousUnlocked;
                SaveManager.Data.greenHillsLevel1Stars = previousStars;
                SaveManager.Data.totalCoins = previousCoins;
                SaveManager.Save();
                AdventureSession.Clear();
            }
        }
    }
}
