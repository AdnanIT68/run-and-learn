using System.Collections;
using System.Linq;
using FootballRunner.Adventure;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>"2 + 3 = ?": sums up to ten at school, answered by jumping into the right number.</summary>
    [Timeout(60000)]
    public class SumTests
    {
        private string _saveBefore;

        [SetUp]
        public void FreshSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.worldResults.Clear();
            data.wordTutorialDone = true;
            data.easyMode = false;
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
            foreach (string pack in new[] { "World1", "World2", "World3", "World4", "World5", "World6" })
            {
                LearningProgress.RecordLevelComplete(pack, LearningProgress.LevelToOpenNextWorld - 1, 1);
            }

            data.pepiWelcomed.Add("World7:en");
            AdventureSession.Clear();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void TheWordPlusIsVoicedInEveryLanguage()
        {
            WordPack school = WordPack.Load("World7");
            foreach (string language in new[] { "bs", "en", "de", "ar" })
            {
                Assert.IsNotNull(school.Voice(language, "op_plus"), language);
            }
        }

        [UnityTest]
        public IEnumerator SumsStartFromLevelTwoAtSchool()
        {
            yield return LoadSchool(level: 0);
            Assert.IsFalse(WordChallengeDirector.Instance.CanAskSums, "Level 1 should not ask sums yet.");

            yield return LoadSchool(level: 1);
            Assert.IsTrue(WordChallengeDirector.Instance.CanAskSums);
        }

        [UnityTest]
        public IEnumerator ASumShowsTheQuestionAndTheRightNumberIsTheAnswer()
        {
            yield return LoadSchool(level: 1);
            WordChallengeDirector director = WordChallengeDirector.Instance;

            float waited = 0f;
            while (!director.ForceChallenge(WordChallengeDirector.Kind.Sum) && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(WordChallengeDirector.Kind.Sum, director.CurrentKind);
            (int a, int b) = director.CurrentSum;
            Assert.That(a, Is.InRange(1, 9));
            Assert.That(a + b, Is.InRange(2, 10), "Sums stay within ten.");
            Assert.AreEqual($"{a} + {b} = ?", director.CurrentWord.Text(LearningProgress.English));
            Assert.AreEqual("num_" + (a + b), director.CurrentWord.id);

            var options = director.Bubbles.Where(b2 => b2 != null).Select(b2 => b2.Word.id).ToList();
            Assert.AreEqual(options.Count, options.Distinct().Count(), "Two bubbles show the same number.");
            CollectionAssert.Contains(options, "num_" + (a + b));

            // Asking again repeats the question; the slow copy would say the answer.
            director.ReplayFromSpeaker();
            director.ReplayFromSpeaker();
            Assert.IsFalse(director.LastReplayWasSlow);

            director.OnBubbleTouched(director.Bubbles.First(b2 => b2 != null && b2.Word == director.CurrentWord));
            Assert.AreEqual(1, director.GoalCaught, "The right answer was not counted.");
        }

        private static IEnumerator LoadSchool(int level)
        {
            for (int l = 0; l < level; l++)
            {
                LearningProgress.RecordLevelComplete("World7", l, 1);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World7"));
            Assert.IsTrue(LearningProgress.SelectLevel(level));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }
    }
}
