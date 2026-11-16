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
    /// <summary>Boje i oblici: six colours and four shapes, a bonus world open from the start.</summary>
    [Timeout(60000)]
    public class ColoursWorldTests
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
        public void ColoursAndShapesHavePicturesAndVoices()
        {
            WordPack pack = WordPack.Load(WordPack.ColoursPack);
            Assert.IsNotNull(pack);
            Assert.AreEqual("Boje i oblici", pack.Title(LearningProgress.Bosnian));
            CollectionAssert.AreEquivalent(
                new[] { "red", "blue", "yellow", "green", "orange", "purple", "circle", "square", "triangle", "heart" },
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

            Assert.AreEqual("das Dreieck", pack.Words.First(w => w.id == "triangle").Text(LearningProgress.German));
            Assert.AreEqual("trougao", pack.Words.First(w => w.id == "triangle").Text(LearningProgress.Bosnian));
        }

        [Test]
        public void ItIsOpenFromTheStartAndOutsideTheChain()
        {
            Assert.IsTrue(WordPack.IsBonus(WordPack.ColoursPack));
            Assert.IsTrue(LearningProgress.IsPackUnlocked(WordPack.ColoursPack));
            Assert.IsNull(LearningProgress.NextPack(WordPack.BonusPack), "Lijepe riječi must not lead on.");
            Assert.IsNull(LearningProgress.NextPack(WordPack.ColoursPack));
            Assert.IsNull(LearningProgress.NextPack("World8"), "Noćno nebo must stay the finale.");
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World2"));
        }

        [UnityTest]
        public IEnumerator ARunUsesTheSchoolScenery()
        {
            LearningProgress.SelectPack(WordPack.ColoursPack);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Assert.AreEqual("World7", WorldScenery.Instance.WorldName);
            Assert.AreEqual(WordPack.ColoursPack, WordChallengeDirector.Instance.Pack.Name);
        }
    }
}
