using System.Collections;
using System.Linq;
using FootballRunner.Adventure;
using FootballRunner.Gameplay;
using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
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
        private string _packBefore;

        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
            Time.timeScale = 1f;
            AdventureSession.Clear();
            // These tests describe the garden's plain word challenges, whatever world the save had chosen.
            _packBefore = SaveManager.Data.selectedPack;
            SaveManager.Data.selectedPack = WordPack.DefaultPack;
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }

        private bool _tutorialDoneBefore;

        [SetUp]
        public void RememberTutorialFlag()
        {
            _tutorialDoneBefore = SaveManager.Data.wordTutorialDone;
        }

        [TearDown]
        public void RestoreTimeScale()
        {
            Time.timeScale = 1f;
            // Tests flip the guide flag; leave the developer's own save as it was.
            SaveManager.Data.wordTutorialDone = _tutorialDoneBefore;
            SaveManager.Data.selectedPack = _packBefore ?? WordPack.DefaultPack;
            SaveManager.Save();
        }

        [UnityTest]
        public IEnumerator FirstWordEverIsGuidedAndCatchingItEndsTheGuide()
        {
            SaveManager.Data.wordTutorialDone = false;
            yield return StartChallenge();

            WordChallengeDirector director = WordChallengeDirector.Instance;
            Assert.IsTrue(director.IsGuided, "The very first word should be guided.");

            director.OnBubbleTouched(director.Bubbles.First(b => b.Word == director.CurrentWord));

            Assert.IsTrue(SaveManager.Data.wordTutorialDone);
            Assert.AreEqual(1f, Time.timeScale, "The guide must hand normal speed back.");
        }

        [UnityTest]
        public IEnumerator GuideSlowsTheGameBeforeTheRightBubbleThenRestoresSpeed()
        {
            SaveManager.Data.wordTutorialDone = false;
            yield return StartChallenge();

            bool slowed = false;
            float waited = 0f;

            // The test never jumps, so the runner passes underneath every bubble.
            while (WordChallengeDirector.Instance.CurrentWord != null && waited < 20f)
            {
                slowed |= Time.timeScale < 1f && Time.timeScale > 0f;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(slowed, "The guide never slowed the game before the right bubble.");
            Assert.IsNull(WordChallengeDirector.Instance.CurrentWord, "The challenge never ended.");
            Assert.AreEqual(1f, Time.timeScale, "Normal speed was not restored after the bubbles.");
            Assert.IsFalse(SaveManager.Data.wordTutorialDone, "A missed guided word must be guided again.");
        }

        [UnityTest]
        public IEnumerator CatchingFiveWordsFillsTheGoalAndPaysCoins()
        {
            SaveManager.Data.wordTutorialDone = true;
            int goals = 0;
            System.Action<int> onGoal = size => goals++;
            WordChallengeDirector.GoalCompleted += onGoal;

            yield return StartChallenge();
            WordChallengeDirector director = WordChallengeDirector.Instance;
            int coinsBefore = ScoreManager.Instance.CoinsCollected;

            for (int i = 0; i < director.GoalSize; i++)
            {
                if (i > 0)
                {
                    Assert.IsTrue(director.ForceChallenge(), $"Challenge {i + 1} did not start.");
                }

                director.OnBubbleTouched(director.Bubbles.First(b => b.Word == director.CurrentWord));
                yield return null;
            }

            WordChallengeDirector.GoalCompleted -= onGoal;

            Assert.AreEqual(1, goals, "Filling the row should complete exactly one goal.");
            Assert.AreEqual(0, director.GoalCaught, "The row should start again after a goal.");
            Assert.Greater(ScoreManager.Instance.CoinsCollected, coinsBefore, "The goal paid no coins.");
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
