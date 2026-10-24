using System.Collections;
using FootballRunner.Gameplay;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Five levels per world: they open one by one, get harder, and level 3 opens the next world.</summary>
    [Timeout(60000)]
    public class LevelProgressionTests
    {
        private string _saveBefore;

        [SetUp]
        public void FreshProgress()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;
            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.worldResults.Clear();
            data.wordTutorialDone = true;
            data.learningLanguage = LearningProgress.Bosnian;
            data.selectedPack = "World1";
            data.selectedLevel = 0;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void LevelsOpenOneByOneAndLevelThreeOpensTheNextWorld()
        {
            Assert.IsTrue(LearningProgress.IsLevelUnlocked("World1", 0));
            Assert.IsFalse(LearningProgress.IsLevelUnlocked("World1", 1));
            Assert.IsFalse(LearningProgress.SelectLevel(1), "A locked level was chosen.");

            LearningProgress.RecordLevelComplete("World1", 0, 2);
            Assert.IsTrue(LearningProgress.SelectLevel(1));
            Assert.AreEqual(1, LearningProgress.SelectedLevel);
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World2"), "One level should not open the farm yet.");

            LearningProgress.RecordLevelComplete("World1", 1, 3);
            LearningProgress.RecordLevelComplete("World1", 2, 1);
            Assert.IsTrue(LearningProgress.IsPackUnlocked("World2"), "Level 3 of the garden should open the farm.");
            Assert.AreEqual(3, LearningProgress.LevelsDone("World1"));
            Assert.AreEqual(6, LearningProgress.TotalStars("World1"));
            Assert.AreEqual(3, LearningProgress.FirstOpenLevel("World1"));

            // Choosing a world starts at its first unfinished level.
            Assert.IsTrue(LearningProgress.SelectPack("World2"));
            Assert.AreEqual(0, LearningProgress.SelectedLevel);
            Assert.IsTrue(LearningProgress.SelectPack("World1"));
            Assert.AreEqual(3, LearningProgress.SelectedLevel);
        }

        [Test]
        public void NextLeadsThroughTheLevelsThenToTheNextWorld()
        {
            for (int level = 0; level < LearningProgress.LevelsPerWorld; level++)
            {
                LearningProgress.RecordLevelComplete("World1", level, 3);
            }

            Assert.IsTrue(LearningProgress.TryGetNext("World1", 1, out string pack, out int next));
            Assert.AreEqual("World1", pack);
            Assert.AreEqual(2, next);

            Assert.IsTrue(LearningProgress.TryGetNext("World1", LearningProgress.LevelsPerWorld - 1, out pack, out next));
            Assert.AreEqual("World2", pack);
            Assert.AreEqual(0, next);

            for (int level = 0; level < LearningProgress.LevelsPerWorld; level++)
            {
                LearningProgress.RecordLevelComplete("World8", level, 1);
            }

            Assert.IsFalse(LearningProgress.TryGetNext("World8", LearningProgress.LevelsPerWorld - 1, out _, out _),
                "There is nothing after the last level of the last world.");
        }

        [Test]
        public void SavesFromBeforeLevelsCountAsLevelOne()
        {
            // An old save has no level field, which reads as 0: level 1.
            SaveManager.Data.worldResults.Add(new WorldResult
                { pack = "World1", language = LearningProgress.Bosnian, stars = 2, completions = 1 });

            Assert.AreEqual(2, LearningProgress.Stars("World1", 0));
            Assert.IsTrue(LearningProgress.IsLevelUnlocked("World1", 1));
        }

        [UnityTest]
        public IEnumerator LaterLevelsAreFasterAndAskMoreWords()
        {
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            int firstGoal = WordChallengeDirector.Instance.GoalSize;
            float firstSpeed = DifficultyManager.Instance.CurrentSpeed;
            float firstWarmUp = DifficultyManager.Instance.WarmUpSeconds;
            Assert.AreEqual(5, firstGoal);

            for (int level = 0; level < LearningProgress.LevelsPerWorld - 1; level++)
            {
                LearningProgress.RecordLevelComplete("World1", level, 2);
            }

            Assert.IsTrue(LearningProgress.SelectLevel(4));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Assert.AreEqual(4, WordChallengeDirector.Instance.Level);
            Assert.AreEqual(4, DifficultyManager.Instance.Level);
            Assert.AreEqual(8, WordChallengeDirector.Instance.GoalSize, "Level 5 should ask eight words.");
            Assert.Greater(DifficultyManager.Instance.CurrentSpeed, firstSpeed * 1.15f);
            Assert.Less(DifficultyManager.Instance.WarmUpSeconds, firstWarmUp);
        }

        [UnityTest]
        public IEnumerator WorldChooserPicksOnlyOpenLevels()
        {
            FootballRunner.UI.DailyUi.AutoOpen = false;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            WordWorldsUi worlds = Object.FindFirstObjectByType<WordWorldsUi>();
            worlds.Open();
            Assert.IsTrue(worlds.IsLevelUnlocked(0));
            Assert.IsFalse(worlds.PressLevel(2), "A locked level was chosen.");

            LearningProgress.RecordLevelComplete("World1", 0, 3);
            LearningProgress.RecordLevelComplete("World1", 1, 3);
            Assert.IsTrue(worlds.PressLevel(2));
            Assert.AreEqual(2, LearningProgress.SelectedLevel);
            worlds.Close();
            FootballRunner.UI.DailyUi.AutoOpen = true;
        }
    }
}
