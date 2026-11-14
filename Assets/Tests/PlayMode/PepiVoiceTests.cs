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
    /// <summary>Pepi praises a run of right answers, warns before hard words, welcomes and announces worlds.</summary>
    [Timeout(90000)]
    public class PepiVoiceTests
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
        public void EveryPepiLineIsVoicedInEveryMenuLanguage()
        {
            string[] ids = { "pepi_praise_1", "pepi_praise_2", "pepi_praise_3", "pepi_remind", "pepi_unlocked" };
            ids = ids.Concat(WordPack.PackOrder.Select(PepiVoice.WelcomeFor)).ToArray();

            foreach (string language in HomeLanguages)
            {
                foreach (string id in ids)
                {
                    Assert.IsNotNull(Resources.Load<AudioClip>($"Words/Pepi/Audio/{language}/{id}"), $"{language}/{id}");
                }
            }
        }

        [Test]
        public void AWordMissedTwiceIsTroubleUntilCaughtRight()
        {
            Assert.IsFalse(PepiVoice.IsTroubleWord("apple"));
            LearningProgress.RecordMiss("apple");
            LearningProgress.RecordMiss("apple");
            Assert.IsTrue(PepiVoice.IsTroubleWord("apple"));
            LearningProgress.RecordCorrect("apple");
            Assert.IsFalse(PepiVoice.IsTroubleWord("apple"));
        }

        [Test]
        public void EachWorldIsWelcomedOncePerLanguage()
        {
            Assert.IsTrue(PepiVoice.TakeWelcome("World1"));
            Assert.IsFalse(PepiVoice.TakeWelcome("World1"));
            LearningProgress.SetLanguage(LearningProgress.Arabic, LearningProgress.Bosnian);
            Assert.IsTrue(PepiVoice.TakeWelcome("World1"));
        }

        [UnityTest]
        public IEnumerator PepiWelcomesTheChildToANewWorld()
        {
            yield return LoadGame();
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreEqual("pepi_world_World1", WordChallengeDirector.Instance.LastPepiLine);
        }

        [UnityTest]
        public IEnumerator ThreeRightInARowEarnsPepisPraise()
        {
            SaveManager.Data.pepiWelcomed.Add("World1:en");
            yield return LoadGame();
            WordChallengeDirector director = WordChallengeDirector.Instance;

            for (int i = 0; i < 3; i++)
            {
                yield return CatchOne(director);
            }

            Assert.AreEqual(PepiVoice.StreakPraise, director.LastPepiLine);
        }

        [UnityTest]
        public IEnumerator PepiWarnsBeforeAWordTheChildKeepsMissing()
        {
            SaveManager.Data.pepiWelcomed.Add("World1:en");
            foreach (WordEntry word in WordPack.Load("World1").Words)
            {
                LearningProgress.RecordMiss(word.id);
                LearningProgress.RecordMiss(word.id);
            }

            yield return LoadGame();
            WordChallengeDirector director = WordChallengeDirector.Instance;
            yield return Start(director);
            Assert.AreEqual(PepiVoice.Remind, director.LastPepiLine);
        }

        [UnityTest]
        public IEnumerator FinishingLevelThreeAnnouncesTheNewWorld()
        {
            SaveManager.Data.pepiWelcomed.Add("World1:en");
            LearningProgress.RecordLevelComplete("World1", 0, 1);
            LearningProgress.RecordLevelComplete("World1", 1, 1);
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World2"));
            SaveManager.Data.selectedLevel = LearningProgress.LevelToOpenNextWorld - 1;

            yield return LoadGame();
            WordChallengeDirector director = WordChallengeDirector.Instance;

            while (!director.LevelDone)
            {
                yield return CatchOne(director);
            }

            Assert.IsTrue(director.OpenedNewWorld);
            Assert.IsTrue(LearningProgress.IsPackUnlocked("World2"));
            yield return null;
            Assert.AreEqual(PepiVoice.Unlocked, director.LastPepiLine);
        }

        private static IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }

        private static IEnumerator Start(WordChallengeDirector director)
        {
            float waited = 0f;

            while (director.CurrentWord == null && waited < 5f)
            {
                director.ForceChallenge(WordChallengeDirector.Kind.Word);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(director.CurrentWord, "No challenge started.");
        }

        private static IEnumerator CatchOne(WordChallengeDirector director)
        {
            // Let the previous challenge clear before asking the next word.
            float waited = 0f;
            while (director.CurrentWord != null && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return Start(director);
            director.OnBubbleTouched(director.Bubbles.First(b => b != null && b.Word == director.CurrentWord));
            yield return null;
        }
    }
}
