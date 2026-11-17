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

        [Test]
        public void TheWordMinusIsVoicedInEveryLanguage()
        {
            WordPack school = WordPack.Load("World7");
            foreach (string language in new[] { "bs", "en", "de", "ar" })
            {
                Assert.IsNotNull(school.Voice(language, "op_minus"), language);
            }
        }

        [UnityTest]
        public IEnumerator TakingAwayStartsFromLevelThree()
        {
            yield return LoadSchool(level: 1);
            Assert.IsTrue(WordChallengeDirector.Instance.CanAskSums);
            Assert.IsFalse(WordChallengeDirector.Instance.CanAskMinus, "Level 2 should only add.");

            yield return LoadSchool(level: 2);
            Assert.IsTrue(WordChallengeDirector.Instance.CanAskMinus);
        }

        [UnityTest]
        public IEnumerator TakingAwayShowsTheQuestionAndNeverGoesBelowOne()
        {
            yield return LoadSchool(level: 2);
            WordChallengeDirector director = WordChallengeDirector.Instance;
            // Every sum takes away here, so the test does not depend on luck.
            typeof(WordChallengeDirector).GetField("minusChance",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(director, 1f);
            int minusSeen = 0;
            int plusSeen = 0;

            for (int round = 0; round < 6 && minusSeen < 3; round++)
            {
                float waited = 0f;
                while (!director.ForceChallenge(WordChallengeDirector.Kind.Sum) && waited < 5f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.AreEqual(WordChallengeDirector.Kind.Sum, director.CurrentKind);
                (int a, int b) = director.CurrentSum;

                if (director.CurrentIsMinus)
                {
                    minusSeen++;
                    Assert.That(a - b, Is.InRange(1, 9), "Taking away stays between one and ten.");
                    Assert.AreEqual($"{a} - {b} = ?", director.CurrentWord.Text(LearningProgress.English));
                    Assert.AreEqual("num_" + (a - b), director.CurrentWord.id);
                    Assert.AreEqual(a - b, director.CurrentAnswer);
                }
                else
                {
                    plusSeen++;
                    Assert.AreEqual(a + b, director.CurrentAnswer);
                }

                var options = director.Bubbles.Where(b2 => b2 != null).Select(b2 => b2.Word.id).ToList();
                CollectionAssert.Contains(options, "num_" + director.CurrentAnswer);
                Assert.AreEqual(options.Count, options.Distinct().Count(), "Two bubbles show the same number.");
                director.OnBubbleTouched(director.Bubbles.First(b2 => b2 != null && b2.Word == director.CurrentWord));
                yield return null;
            }

            Assert.Greater(minusSeen, 0, "No sum took away at level 3.");
            _ = plusSeen;
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
