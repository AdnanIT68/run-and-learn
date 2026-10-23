using System.Collections;
using System.Linq;
using FootballRunner.Adventure;
using FootballRunner.Learning;
using FootballRunner.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>
    /// The word game is the product. These check that content loads in both
    /// languages and that a challenge is fair and records what the child learned.
    /// </summary>
    [Timeout(60000)]
    public class WordChallengeTests
    {
        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
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

        [Test]
        public void WorldOnePackHasPictureAndBothVoicesForEveryWord()
        {
            WordPack pack = WordPack.Load(WordPack.DefaultPack);

            Assert.IsNotNull(pack, "World1 pack.json did not load.");
            Assert.AreEqual(10, pack.Words.Count);

            foreach (WordEntry word in pack.Words)
            {
                Assert.IsFalse(string.IsNullOrEmpty(word.bs), $"{word.id} has no Bosnian text.");
                Assert.IsFalse(string.IsNullOrEmpty(word.en), $"{word.id} has no English text.");
                Assert.IsNotNull(pack.Picture(word.id), $"{word.id} has no picture.");
                Assert.IsNotNull(pack.Voice(LearningProgress.Bosnian, word.id), $"{word.id} has no Bosnian voice.");
                Assert.IsNotNull(pack.Voice(LearningProgress.English, word.id), $"{word.id} has no English voice.");
            }
        }

        [UnityTest]
        public IEnumerator ChallengeOffersThreeDistinctWordsWithExactlyOneAnswer()
        {
            yield return StartChallenge();

            WordChallengeDirector director = WordChallengeDirector.Instance;
            var words = director.Bubbles.Select(b => b.Word).ToList();

            Assert.AreEqual(3, words.Count);
            Assert.AreEqual(3, words.Distinct().Count(), "A word was offered twice.");
            Assert.AreEqual(1, words.Count(w => w == director.CurrentWord), "There must be exactly one right answer.");
        }

        [UnityTest]
        public IEnumerator BubblesFloatOutOfReachOfARunnerWhoDoesNotJump()
        {
            yield return StartChallenge();

            Bounds player = PlayerController.Instance.GetComponent<Collider2D>().bounds;

            foreach (WordBubble bubble in WordChallengeDirector.Instance.Bubbles)
            {
                Bounds bounds = bubble.GetComponent<Collider2D>().bounds;
                Assert.Greater(bounds.min.y, player.max.y,
                    "A bubble hangs low enough to be grabbed without jumping.");
                Assert.Greater(bounds.min.x, player.max.x, "A bubble spawned on or behind the runner.");
            }
        }

        [UnityTest]
        public IEnumerator CatchingTheRightWordMarksItLearned()
        {
            yield return StartChallenge();

            WordChallengeDirector director = WordChallengeDirector.Instance;
            WordEntry target = director.CurrentWord;
            WordBubble right = director.Bubbles.First(b => b.Word == target);
            int before = LearningProgress.Get(target.id).correct;
            bool? caught = null;
            System.Action<WordEntry, bool> listener = (word, result) => caught = result;
            WordChallengeDirector.ChallengeEnded += listener;

            director.OnBubbleTouched(right);
            WordChallengeDirector.ChallengeEnded -= listener;

            Assert.AreEqual(true, caught);
            Assert.AreEqual(before + 1, LearningProgress.Get(target.id).correct);
            Assert.IsNull(director.CurrentWord, "The challenge should be over after the right answer.");
        }

        [UnityTest]
        public IEnumerator WrongWordRecordsAMissButKeepsTheRightOneCatchable()
        {
            yield return StartChallenge();

            WordChallengeDirector director = WordChallengeDirector.Instance;
            WordEntry target = director.CurrentWord;
            WordBubble wrong = director.Bubbles.First(b => b.Word != target);
            WordBubble right = director.Bubbles.First(b => b.Word == target);
            int missedBefore = LearningProgress.Get(target.id).missed;

            director.OnBubbleTouched(wrong);

            Assert.AreEqual(missedBefore + 1, LearningProgress.Get(target.id).missed);
            Assert.AreSame(target, director.CurrentWord, "A wrong answer must not end the challenge.");
            Assert.IsFalse(right.IsFinished, "The right bubble must stay catchable.");
        }

        private static IEnumerator StartChallenge()
        {
            float waited = 0f;

            // The director measures the runner's standing height on the first grounded frame.
            while (waited < 5f)
            {
                if (WordChallengeDirector.Instance != null &&
                    (WordChallengeDirector.Instance.CurrentWord != null || WordChallengeDirector.Instance.ForceChallenge()))
                {
                    yield break;
                }

                waited += Time.deltaTime;
                yield return null;
            }

            PlayerController player = PlayerController.Instance;
            Assert.Fail("No word challenge could be started within five seconds. " +
                        $"director={WordChallengeDirector.Instance != null}, " +
                        $"grounded={(player != null && player.IsGrounded)}, timeScale={Time.timeScale}");
        }
    }
}
