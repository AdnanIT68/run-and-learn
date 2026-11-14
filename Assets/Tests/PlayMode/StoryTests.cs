using System.Collections;
using System.Linq;
using FootballRunner.Adventure;
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
    /// The story of Brbljavka: Pepi tells at the start of each world that the magpie
    /// stole its words, and the world's last level ends with catching her.
    /// </summary>
    [Timeout(120000)]
    public class StoryTests
    {
        private static readonly string[] HomeLanguages = { "bs", "en", "de" };
        private string _saveBefore;

        [SetUp]
        public void FreshSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.worldResults.Clear();
            data.pepiWelcomed.Clear();
            data.wordTutorialDone = true;
            data.selectedPack = WordPack.DefaultPack;
            data.selectedLevel = 0;
            data.easyMode = false;
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
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
        public void EveryWorldHasItsStoryVoicedAndWritten()
        {
            foreach (string language in HomeLanguages)
            {
                LearningProgress.SetLanguage(language == "bs" ? LearningProgress.English : LearningProgress.Bosnian,
                    language == "bs" ? LearningProgress.Bosnian : language);

                foreach (string pack in WordPack.PackOrder)
                {
                    foreach (string id in new[] { PepiVoice.StoryIntroFor(pack), PepiVoice.StoryOutroFor(pack) })
                    {
                        Assert.IsNotNull(PepiVoice.Line(id), $"{language}/{id} has no voice");
                        StringAssert.Contains("Brbljavka", PepiVoice.Text(id), $"{language}/{id} has no text");
                    }
                }
            }
        }

        [Test]
        public void TheMagpieIsDrawnInEveryPose()
        {
            Assert.IsNotNull(StoryArt.Magpie(carryingLetters: true));
            Assert.IsNotNull(StoryArt.Magpie(carryingLetters: false));
            Assert.IsNotNull(StoryArt.MagpieWingsDown());
        }

        [UnityTest]
        public IEnumerator TheFirstRunInAWorldTellsTheStory()
        {
            yield return LoadGame();
            yield return new WaitForSecondsRealtime(1.3f);

            WordChallengeDirector director = WordChallengeDirector.Instance;
            Assert.AreEqual("story_intro_World1", director.LastStoryLine);
            Assert.IsTrue(StoryCardUi.Instance.IsShowing, "The story card is not on screen.");
            StringAssert.Contains("Brbljavka", StoryCardUi.Instance.LastText);
            Assert.IsNull(director.CurrentWord, "A word was asked while Pepi was telling the story.");
        }

        [UnityTest]
        public IEnumerator OnlyTheLastLevelEndsWithTheMagpie()
        {
            SaveManager.Data.pepiWelcomed.Add("World1:en");
            yield return LoadGame();
            WordChallengeDirector director = WordChallengeDirector.Instance;
            Assert.IsFalse(director.EndsWithMagpie);

            yield return CatchGoal(director);
            Assert.IsTrue(director.LevelDone, "Level 1 should end as soon as the goal is full.");
            Assert.IsFalse(director.IsChasingMagpie);
        }

        [UnityTest]
        public IEnumerator TheLastLevelEndsWhenTheChildJumpsIntoTheMagpie()
        {
            SaveManager.Data.pepiWelcomed.Add("World1:en");
            for (int level = 0; level < LearningProgress.LevelsPerWorld - 1; level++)
            {
                LearningProgress.RecordLevelComplete("World1", level, 1);
            }

            SaveManager.Data.selectedLevel = LearningProgress.LevelsPerWorld - 1;
            yield return LoadGame();
            WordChallengeDirector director = WordChallengeDirector.Instance;
            Assert.IsTrue(director.EndsWithMagpie);

            bool caughtEvent = false;
            void OnCaught() => caughtEvent = true;
            WordChallengeDirector.MagpieCaught += OnCaught;

            try
            {
                yield return CatchGoal(director);
                Assert.IsTrue(director.IsChasingMagpie, "The full goal did not send Brbljavka out.");
                Assert.IsFalse(director.LevelDone, "The level ended before the magpie was caught.");

                Transform runner = PlayerController.Instance.transform;
                Assert.Greater(director.Magpie.transform.position.x, runner.position.x, "She should fly ahead.");

                // Put her right where the runner is, as if the child jumped into her.
                director.Magpie.PlaceAt(PlayerController.Instance.GetComponent<Collider2D>().bounds.center);
                yield return null;
                yield return null;

                Assert.IsFalse(director.IsChasingMagpie);
                Assert.IsTrue(director.LevelDone);
                Assert.IsTrue(caughtEvent);
                Assert.AreEqual("story_outro_World1", director.LastStoryLine);
                StringAssert.Contains("Brbljavka", StoryCardUi.Instance.LastText);
            }
            finally
            {
                WordChallengeDirector.MagpieCaught -= OnCaught;
            }
        }

        private static IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }

        private static IEnumerator CatchGoal(WordChallengeDirector director)
        {
            int guard = 0;

            while (!director.LevelDone && !director.IsChasingMagpie && guard++ < 40)
            {
                float waited = 0f;
                while (director.CurrentWord != null && waited < 5f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                waited = 0f;
                while (director.CurrentWord == null && waited < 5f)
                {
                    director.ForceChallenge(WordChallengeDirector.Kind.Word);
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                director.OnBubbleTouched(director.Bubbles.First(b => b != null && b.Word == director.CurrentWord));
                yield return null;
            }
        }
    }
}
