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
    /// <summary>Tapping the word card says the word again; the second tap says it slowly.</summary>
    [Timeout(60000)]
    public class SayItAgainTests
    {
        private static readonly string[] Languages = { "bs", "en", "de", "ar" };
        private string _saveBefore;

        [SetUp]
        public void KeepSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
        }

        [TearDown]
        public void RestoreSave()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void EveryAskedEntryHasASlowerCopyInEveryLanguage()
        {
            foreach (string packName in WordPack.PackOrder)
            {
                WordPack pack = WordPack.Load(packName);
                var asked = pack.Words.Concat(pack.Counts).Concat(pack.Commands).Concat(pack.Sizes)
                    .Concat(pack.Sentences).Concat(pack.Numbers);

                foreach (WordEntry entry in asked)
                {
                    foreach (string language in Languages)
                    {
                        AudioClip normal = pack.Voice(language, entry.id);
                        AudioClip slow = pack.SlowVoice(language, entry.id);
                        string where = $"{packName}/{language}/{entry.id}";
                        Assert.IsNotNull(slow, where + " has no slow voice");
                        Assert.Greater(slow.length, normal.length * 1.2f, where + " is not slower");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator SecondTapPlaysTheWordSlowlyAndTapsTakeTurns()
        {
            SaveManager.Data.wordTutorialDone = true;
            SaveManager.Data.selectedPack = WordPack.DefaultPack;
            SaveManager.Data.selectedLevel = 0;
            LearningProgress.SetLanguage(LearningProgress.Bosnian, LearningProgress.English);
            AdventureSession.Clear();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            float waited = 0f;
            while (!director.ForceChallenge(WordChallengeDirector.Kind.Word) && waited < 5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.IsNotNull(director.CurrentWord, "No challenge started.");

            director.ReplayFromSpeaker();
            Assert.IsFalse(director.LastReplayWasSlow, "The first tap should repeat at normal speed.");
            director.ReplayFromSpeaker();
            Assert.IsTrue(director.LastReplayWasSlow, "The second tap should be slow.");
            director.ReplayFromSpeaker();
            Assert.IsFalse(director.LastReplayWasSlow, "Taps should take turns.");
        }
    }
}
