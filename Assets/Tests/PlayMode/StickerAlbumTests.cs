using System.Collections;
using FootballRunner.Adventure;
using CharacterRoster = FootballRunner.Player.CharacterRoster;
using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Every learned word is a sticker; a full page gives a new player, once.</summary>
    [Timeout(60000)]
    public class StickerAlbumTests
    {
        private string _saveBefore;

        [SetUp]
        public void FreshAlbum()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.stickerPages.Clear();
            data.unlockedItems.RemoveAll(item => item.StartsWith("character_"));
            data.wordTutorialDone = true;
            data.selectedPack = WordPack.DefaultPack;
            data.selectedLevel = 0;
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
        public void AFullPageGivesTheCheapestLockedPlayerOnce()
        {
            WordPack garden = WordPack.Load("World1");
            CharacterRoster.Entry expected = StickerAlbum.NextGift();
            Assert.AreEqual("harun", expected.Id, "With nothing bought, Harun (40) is the first gift.");

            for (int i = 0; i < garden.Words.Count - 1; i++)
            {
                LearningProgress.RecordCorrect(garden.Words[i].id);
                Assert.IsNull(StickerAlbum.OnWordLearned(garden.Words[i].id), "A page that is not full gave a gift.");
            }

            string last = garden.Words[garden.Words.Count - 1].id;
            LearningProgress.RecordCorrect(last);
            Assert.AreEqual("World1", StickerAlbum.OnWordLearned(last));
            Assert.IsTrue(CharacterRoster.IsUnlocked("harun"));
            Assert.AreEqual("harun", StickerAlbum.GiftFor("World1").Id);

            int coins = SaveManager.Data.totalCoins;
            Assert.IsNull(StickerAlbum.OnWordLearned(last), "The same page gave a second gift.");
            Assert.AreEqual(coins, SaveManager.Data.totalCoins);
            Assert.AreEqual("lejla", StickerAlbum.NextGift().Id);
        }

        [Test]
        public void EachLearnedLanguageHasItsOwnPages()
        {
            FillPage("World1");
            Assert.IsTrue(StickerAlbum.WasRewarded("World1"));

            LearningProgress.SetLanguage(LearningProgress.Arabic, LearningProgress.Bosnian);
            Assert.IsFalse(StickerAlbum.WasRewarded("World1"), "An English page counted for Arabic.");
            Assert.IsFalse(StickerAlbum.IsPageFull(WordPack.Load("World1")));
        }

        [Test]
        public void OnceEveryPlayerIsOpenAFullPageGivesCoins()
        {
            foreach (CharacterRoster.Entry entry in CharacterRoster.All)
            {
                CharacterRoster.Gift(entry.Id);
            }

            int coins = SaveManager.Data.totalCoins;
            FillPage("World1");
            Assert.AreEqual(coins + StickerAlbum.CoinsWhenAllPlayersOpen, SaveManager.Data.totalCoins);
        }

        [Test]
        public void StickersTiltALittleEachTheirOwnWay()
        {
            foreach (string id in new[] { "apple", "pear", "banana", "cow", "moon" })
            {
                float tilt = WordAlbumUi.StickerTilt(id);
                Assert.That(tilt, Is.InRange(-4f, 4f));
                Assert.AreEqual(tilt, WordAlbumUi.StickerTilt(id), "The tilt must not change between visits.");
            }
        }

        [UnityTest]
        public IEnumerator AlbumNamesTheGiftAndTheGameAnnouncesIt()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            var album = Object.FindFirstObjectByType<WordAlbumUi>();
            album.Open();
            StringAssert.Contains("Harun", album.PageNote, "The album does not say what a full page gives.");
            album.Close();

            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            yield return null;
            var toast = Object.FindFirstObjectByType<MissionToastUi>();
            Assert.IsNotNull(toast, "The game has no toast for a full page.");
            FillPage("World1");
            StringAssert.Contains("Harun", toast.LastText);

            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            album = Object.FindFirstObjectByType<WordAlbumUi>();
            album.Open();
            StringAssert.Contains("POKLON: Harun", album.PageNote);
            album.Close();
        }

        private static void FillPage(string packName)
        {
            WordPack pack = WordPack.Load(packName);

            foreach (WordEntry word in pack.Words)
            {
                LearningProgress.RecordCorrect(word.id);
                StickerAlbum.OnWordLearned(word.id);
            }
        }
    }
}
