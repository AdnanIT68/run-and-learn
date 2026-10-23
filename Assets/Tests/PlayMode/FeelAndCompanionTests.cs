using System.Collections;
using System.Linq;
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

        [UnityTest]
        public IEnumerator BazaarCommandsPutTheRightStarAtTheRightHeight()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.wordTutorialDone = true;
            WordPack home = WordPack.Load("World3");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(home.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World4"));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            var seen = new System.Collections.Generic.HashSet<string>();
            float waited = 0f;

            while (seen.Count < 3 && waited < 20f)
            {
                if (director.CurrentWord == null && !director.ForceChallenge(command: true))
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                    continue;
                }

                WordEntry target = director.CurrentWord;
                Assert.IsTrue(target.IsCommand, "The bazaar did not give a spoken command.");
                Assert.AreEqual(2, director.Bubbles.Count);

                WordBubble right = director.Bubbles.First(b => b.Word == target);
                WordBubble wrong = director.Bubbles.First(b => b.Word != target);
                Assert.AreEqual(right.transform.position.x, wrong.transform.position.x, 0.01f,
                    "Command stars must share one spot.");
                Assert.AreEqual("star", right.Word.PictureId);
                Assert.AreEqual("star", wrong.Word.PictureId, "Both stars must look the same.");

                float rightY = right.transform.position.y;
                float wrongY = wrong.transform.position.y;

                if (target.action == "stay")
                {
                    Assert.Less(rightY, wrongY, "\"Stay down\" must reward the low star.");
                }
                else
                {
                    Assert.Greater(rightY, wrongY, $"\"{target.action}\" must reward the higher star.");
                }

                seen.Add(target.action);
                director.OnBubbleTouched(right);
                yield return null;
            }

            CollectionAssert.AreEquivalent(new[] { "jump", "stay", "double" }, seen,
                "Not every command showed up.");
        }

        [UnityTest]
        public IEnumerator RiverAsksBigOrSmallWithTheSamePicture()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.wordTutorialDone = true;
            WordPack bazaar = WordPack.Load("World4");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(bazaar.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World5"));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            float waited = 0f;

            while (director.CurrentWord == null && waited < 5f)
            {
                director.ForceChallenge(size: true);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            WordEntry target = director.CurrentWord;
            Assert.IsNotNull(target, "No size challenge started.");
            Assert.IsTrue(target.IsSize, "The river did not ask for big or small.");
            Assert.AreEqual(2, director.Bubbles.Count);

            WordBubble right = director.Bubbles.First(b => b.Word == target);
            WordBubble wrong = director.Bubbles.First(b => b.Word != target);
            Assert.AreEqual(right.Word.PictureId, wrong.Word.PictureId, "Both bubbles must show the same thing.");
            Assert.AreNotEqual(right.Word.size, wrong.Word.size);

            WordBubble big = right.Word.size == "big" ? right : wrong;
            WordBubble small = big == right ? wrong : right;
            Assert.Greater(big.PictureSize, small.PictureSize * 2f, "Big and small must be easy to tell apart.");
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
