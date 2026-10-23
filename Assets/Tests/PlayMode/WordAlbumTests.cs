using System.Collections;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FootballRunner.Tests
{
    /// <summary>
    /// The album is where a child sees what they have learned, so a caught word
    /// must show up there and speak when tapped, and an uncaught one must not.
    /// </summary>
    [Timeout(60000)]
    public class WordAlbumTests
    {
        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator LoadMainMenu()
        {
            Debug.Log("[TEST] " + TestContext.CurrentContext.Test.Name);
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void RestoreSave()
        {
            // Tests change word progress; leave the developer's own save as it was.
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void StarsGrowWithRepeatedCatches()
        {
            Assert.AreEqual(0, WordAlbumUi.StarsFor(0));
            Assert.AreEqual(1, WordAlbumUi.StarsFor(1));
            Assert.AreEqual(2, WordAlbumUi.StarsFor(3));
            Assert.AreEqual(3, WordAlbumUi.StarsFor(6));
        }

        [UnityTest]
        public IEnumerator MainMenuHasAnAlbumWithACardForEveryWord()
        {
            yield return null;

            WordAlbumUi album = Object.FindFirstObjectByType<WordAlbumUi>();
            Assert.IsNotNull(album, "The main menu has no album.");

            album.Open();

            Assert.IsTrue(album.IsOpen);
            Assert.AreEqual(WordPack.Load(WordPack.DefaultPack).Words.Count, album.CardCount);

            album.Close();
            Assert.IsFalse(album.IsOpen);
        }

        [UnityTest]
        public IEnumerator SettingsHaveAVibrationToggle()
        {
            yield return null;

            Toggle[] toggles = Resources.FindObjectsOfTypeAll<Toggle>();
            Assert.IsTrue(System.Array.Exists(toggles, t => t.name == "VibrationToggle" && t.gameObject.scene.IsValid()),
                "The settings panel has no vibration toggle.");
        }

        [UnityTest]
        public IEnumerator CaughtWordUnlocksItsCardAndSpeaks()
        {
            yield return null;

            WordPack pack = WordPack.Load(WordPack.DefaultPack);
            SaveManager.Data.wordProgress.Clear();
            LearningProgress.RecordCorrect(pack.Words[2].id);

            WordAlbumUi album = Object.FindFirstObjectByType<WordAlbumUi>();
            album.Open();

            Assert.IsTrue(album.IsCardUnlocked(2), "A caught word should be unlocked in the album.");
            Assert.IsFalse(album.IsCardUnlocked(0), "An uncaught word must stay locked.");
            Assert.IsTrue(album.PressCard(2), "Tapping a learned card should say the word.");
            Assert.IsFalse(album.PressCard(0), "A locked card must not give the word away.");
        }
    }
}
