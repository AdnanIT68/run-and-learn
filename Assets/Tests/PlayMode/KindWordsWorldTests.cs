using System.Collections;
using System.Linq;
using FootballRunner.Adventure;
using FootballRunner.Gameplay;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Lijepe riječi: a bonus world of kind words, open from the start and outside the chain.</summary>
    [Timeout(60000)]
    public class KindWordsWorldTests
    {
        private static readonly string[] Languages = { "bs", "en", "de", "ar" };
        private static readonly string[] Ids = { "selam", "thanks", "here_you_are", "sorry", "bismillah", "mashallah" };
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
        public void TheKindWordsAreThereWithPicturesAndVoices()
        {
            WordPack pack = WordPack.Load(WordPack.BonusPack);
            Assert.IsNotNull(pack);
            Assert.AreEqual("Lijepe riječi", pack.Title(LearningProgress.Bosnian));
            CollectionAssert.AreEquivalent(Ids, pack.Words.Select(w => w.id));

            foreach (WordEntry word in pack.Words)
            {
                Assert.IsNotNull(pack.Picture(word.PictureId), word.id + " picture");

                foreach (string language in Languages)
                {
                    Assert.IsNotNull(pack.Voice(language, word.id), $"{word.id} {language}");
                    Assert.IsNotNull(pack.SlowVoice(language, word.id), $"{word.id} {language} slow");
                }
            }

            Assert.AreEqual("hvala", pack.Words.First(w => w.id == "thanks").Text(LearningProgress.Bosnian));
            Assert.AreEqual("danke", pack.Words.First(w => w.id == "thanks").Text(LearningProgress.German));
        }

        [Test]
        public void TheBonusWorldIsOpenFromTheStartAndStaysOutOfTheChain()
        {
            Assert.IsTrue(LearningProgress.IsPackUnlocked(WordPack.BonusPack), "Lijepe riječi should be open at once.");
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World2"), "The chain must stay as it was.");
            Assert.IsNull(LearningProgress.NextPack("World8"), "Noćno nebo must stay the finale.");
            Assert.IsNull(LearningProgress.NextPack(WordPack.BonusPack));
            Assert.IsTrue(LearningProgress.SelectPack(WordPack.BonusPack));
            Assert.AreEqual(WordPack.BonusPack, LearningProgress.SelectedPack);
        }

        [UnityTest]
        public IEnumerator ARunInTheBonusWorldUsesTheHomeSceneryAndItsOwnTitle()
        {
            LearningProgress.SelectPack(WordPack.BonusPack);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Assert.AreEqual("World3", WorldScenery.Instance.WorldName, "The scenery should come from Naša kuća.");
            Assert.AreEqual(WordPack.BonusPack, WordChallengeDirector.Instance.Pack.Name);
        }
    }
}
