using System.Collections;
using System.Linq;
using FootballRunner.Adventure;
using FootballRunner.Gameplay;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using FootballRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Easy mode for 3 and 4 year olds: slower, fewer hazards, two pictures to choose from.</summary>
    [Timeout(60000)]
    public class EasyModeTests
    {
        private string _saveBefore;

        [SetUp]
        public void KeepSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveManager.Data.wordTutorialDone = true;
            SaveManager.Data.selectedPack = WordPack.DefaultPack;
            SaveManager.Data.selectedLevel = 0;
            AdventureSession.Clear();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void RestoreSave()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void EasyModeIsOffForNewPlayers()
        {
            Assert.IsFalse(new SaveData().easyMode);
        }

        [UnityTest]
        public IEnumerator EasyModeRunsSlowerWithFewerHazards()
        {
            LearningProgress.EasyMode = false;
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            DifficultyManager normal = DifficultyManager.Instance;
            float normalSpeed = normal.CurrentSpeed;
            float normalChance = normal.ObstacleChanceAt(200f);
            int normalLate = normal.MaxObstaclesAt(200f);
            Assert.IsFalse(normal.IsEasy);

            LearningProgress.EasyMode = true;
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            DifficultyManager easy = DifficultyManager.Instance;

            Assert.IsTrue(easy.IsEasy);
            Assert.Less(easy.CurrentSpeed, normalSpeed * 0.8f, "Easy mode is not slower.");
            Assert.LessOrEqual(easy.ObstacleChanceAt(200f), normalChance * 0.5f + 0.001f, "Easy mode has as many hazards.");
            Assert.AreEqual(1, easy.MaxObstaclesAt(200f), "Easy mode still places two hazards together.");
            Assert.Greater(normalLate, 1);
        }

        [UnityTest]
        public IEnumerator EasyModeAsksWithTwoPictures()
        {
            LearningProgress.EasyMode = true;
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            Assert.AreEqual(2, director.OptionCount);

            float waited = 0f;
            while (!director.ForceChallenge(WordChallengeDirector.Kind.Word) && waited < 5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(2, director.Bubbles.Count(b => b != null), "Easy mode should show two pictures.");

            LearningProgress.EasyMode = false;
            Assert.AreEqual(3, director.OptionCount);
        }

        [UnityTest]
        public IEnumerator ParentsCornerSwitchesEasyMode()
        {
            LearningProgress.EasyMode = false;
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            ParentsCornerUi corner = Object.FindFirstObjectByType<ParentsCornerUi>();
            corner.Open();
            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));
            StringAssert.Contains("LAGANI", corner.EasyModeLabel);

            corner.ToggleEasyMode();
            Assert.IsTrue(LearningProgress.EasyMode);
            corner.ToggleEasyMode();
            Assert.IsFalse(LearningProgress.EasyMode);
            corner.Close();
        }
    }
}
