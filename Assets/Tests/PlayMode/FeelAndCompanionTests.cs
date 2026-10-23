using System.Collections;
using FootballRunner.Adventure;
using FootballRunner.Effects;
using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using FootballRunner.Utilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Double jump, Pepi, particle effects and the vibration setting.</summary>
    [Timeout(60000)]
    public class FeelAndCompanionTests
    {
        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
            Debug.Log("[TEST] " + TestContext.CurrentContext.Test.Name);
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;
            AdventureSession.Clear();
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void RestoreState()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [UnityTest]
        public IEnumerator RunnerCanDoubleJump()
        {
            yield return null;
            Assert.GreaterOrEqual(PlayerController.Instance.MaxJumps, 2, "Double jump is not enabled.");
        }

        [UnityTest]
        public IEnumerator PepiFliesAlongWithTheRunner()
        {
            yield return new WaitForSeconds(2f);

            Assert.IsNotNull(PepiCompanion.Instance, "Pepi is missing from the Game scene.");
            float distance = Vector2.Distance(PepiCompanion.Instance.transform.position,
                PlayerController.Instance.transform.position);
            Assert.Less(distance, 4f, "Pepi fell behind the runner.");
        }

        [UnityTest]
        public IEnumerator PepiPointsAtTheRightBubbleOnTheGuidedWord()
        {
            SaveManager.Data.wordTutorialDone = false;
            float waited = 0f;

            while (WordChallengeDirector.Instance.CurrentWord == null && waited < 5f)
            {
                WordChallengeDirector.Instance.ForceChallenge();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(WordChallengeDirector.Instance.IsGuided, "The first word was not guided.");

            bool pointed = false;
            waited = 0f;

            while (!pointed && WordChallengeDirector.Instance.CurrentWord != null && waited < 15f)
            {
                pointed = PepiCompanion.Instance.IsPointing;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(pointed, "Pepi never flew to the right bubble.");
        }

        [UnityTest]
        public IEnumerator SparklesAppearAndCleanUpAfterThemselves()
        {
            yield return null;

            FxPlayer.Instance.Sparkle(Vector3.zero);
            Assert.Greater(FxPlayer.Instance.LiveCount, 0, "No particles were emitted.");

            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, FxPlayer.Instance.LiveCount, "Particles were not recycled.");
        }

        [UnityTest]
        public IEnumerator RunUsesTheChosenWorld()
        {
            SaveManager.Data.wordProgress.Clear();
            WordPack garden = WordPack.Load("World1");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(garden.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World2"));

            // The director reads the chosen world when the run starts.
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Assert.AreEqual("World2", WordChallengeDirector.Instance.Pack.Name);
        }

        [UnityTest]
        public IEnumerator FarmCountingAsksForOneAnimalInDifferentNumbers()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.wordTutorialDone = true;
            WordPack garden = WordPack.Load("World1");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(garden.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World2"));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            float waited = 0f;

            while (director.CurrentWord == null && waited < 5f)
            {
                director.ForceChallenge(true);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            WordEntry target = director.CurrentWord;
            Assert.IsNotNull(target, "No counting challenge started.");
            Assert.IsTrue(target.IsCounting, "The farm did not ask a counting question.");
            Assert.AreEqual(3, director.Bubbles.Count);

            var counts = new System.Collections.Generic.HashSet<int>();
            int answers = 0;

            foreach (WordBubble bubble in director.Bubbles)
            {
                Assert.AreEqual(target.PictureId, bubble.Word.PictureId, "Every bubble must show the same animal.");
                Assert.AreEqual(bubble.Word.Count, bubble.PictureCount, "The bubble shows the wrong number of pictures.");
                counts.Add(bubble.Word.Count);
                answers += bubble.Word == target ? 1 : 0;
            }

            Assert.AreEqual(3, counts.Count, "Two bubbles showed the same number.");
            Assert.AreEqual(1, answers, "There must be exactly one right answer.");
        }

        [Test]
        public void VibrationSettingIsSaved()
        {
            Haptics.Enabled = false;
            Assert.IsFalse(SaveManager.Data.vibrationEnabled);

            Haptics.Enabled = true;
            Assert.IsTrue(SaveManager.Data.vibrationEnabled);

            // Safe to call in the Editor: vibration is a no-op off device.
            Haptics.Light();
        }
    }
}
