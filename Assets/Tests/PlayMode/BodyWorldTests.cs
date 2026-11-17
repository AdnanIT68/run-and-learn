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
    /// <summary>Tijelo i odjeća: six body parts and four clothes, a bonus world open from the start.</summary>
    [Timeout(60000)]
    public class BodyWorldTests
    {
        private static readonly string[] Languages = { "bs", "en", "de", "ar" };
        private string _saveBefore;

        [SetUp]
        public void FreshSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordTutorialDone = true;
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
        public void BodyAndClothesHavePicturesAndVoices()
        {
            WordPack pack = WordPack.Load(WordPack.BodyPack);
            Assert.IsNotNull(pack);
            Assert.AreEqual("Tijelo i odjeća", pack.Title(LearningProgress.Bosnian));
            CollectionAssert.AreEquivalent(
                new[] { "hand", "foot", "eye", "ear", "nose", "mouth", "shirt", "dress", "shoe", "sock" },
                pack.Words.Select(w => w.id));

            foreach (WordEntry word in pack.Words)
            {
                Assert.IsNotNull(pack.Picture(word.PictureId), word.id + " picture");

                foreach (string language in Languages)
                {
                    Assert.IsNotNull(pack.Voice(language, word.id), $"{word.id} {language}");
                    Assert.IsNotNull(pack.SlowVoice(language, word.id), $"{word.id} {language} slow");
                }
            }

            Assert.AreEqual("der Fuß", pack.Words.First(w => w.id == "foot").Text(LearningProgress.German));
            Assert.AreEqual("čarapa", pack.Words.First(w => w.id == "sock").Text(LearningProgress.Bosnian));
        }

        [Test]
        public void ItIsOpenFromTheStartAndOutsideTheChain()
        {
            Assert.IsTrue(WordPack.IsBonus(WordPack.BodyPack));
            Assert.IsTrue(LearningProgress.IsPackUnlocked(WordPack.BodyPack));
            Assert.IsNull(LearningProgress.NextPack(WordPack.BodyPack));
            Assert.IsNull(LearningProgress.NextPack("World8"), "Noćno nebo must stay the finale.");
            Assert.AreEqual(WordPack.BodyPack, WordPack.PackOrder.Last());
        }

        [Test]
        public void PepiWelcomesAndTellsTheStoryOfTheNewWorld()
        {
            foreach (string id in new[] { "pepi_world_World11", "story_intro_World11", "story_outro_World11" })
            {
                foreach (string language in new[] { "bs", "en", "de" })
                {
                    Assert.IsNotNull(Resources.Load<AudioClip>($"Words/Pepi/Audio/{language}/{id}"), $"{id} {language}");
                }
            }
        }

        [UnityTest]
        public IEnumerator ARunUsesTheRiverScenery()
        {
            LearningProgress.SelectPack(WordPack.BodyPack);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Assert.AreEqual("World5", WorldScenery.Instance.WorldName);
            Assert.AreEqual(WordPack.BodyPack, WordChallengeDirector.Instance.Pack.Name);
        }
    }
}
