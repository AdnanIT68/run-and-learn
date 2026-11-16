using System.Collections;
using System.Linq;
using FootballRunner.Adventure;
using FootballRunner.Collectibles;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using FootballRunner.UI;
using FootballRunner.Utilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FootballRunner.Tests
{
    /// <summary>Look fixes: one language per title, real pictures on locked worlds, one coin, readable Latin.</summary>
    [Timeout(60000)]
    public class PolishTests
    {
        private string _saveBefore;

        [SetUp]
        public void FreshSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.worldResults.Clear();
            data.wordTutorialDone = true;
            data.selectedPack = WordPack.DefaultPack;
            data.selectedLevel = 0;
            AdventureSession.Clear();
            FootballRunner.UI.DailyUi.AutoOpen = false;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void Restore()
        {
            FootballRunner.UI.DailyUi.AutoOpen = true;
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [UnityTest]
        public IEnumerator WorldTitlesUseTheMenuLanguage()
        {
            // Learning English with Bosnian menus: the world is "Nenina bašta", not "Grandma's Garden".
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            var album = Object.FindFirstObjectByType<WordAlbumUi>();
            album.Open();
            TMPro.TMP_Text title = album.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t => t.text != null && t.text.StartsWith("ALBUM  ·"));
            StringAssert.Contains("Nenina bašta", title.text);
            StringAssert.DoesNotContain("Grandma", title.text);
            album.Close();
        }

        [UnityTest]
        public IEnumerator LockedWorldsShowTheirOwnPictureNotAGreyBlock()
        {
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            var worlds = Object.FindFirstObjectByType<WordWorldsUi>();
            worlds.Open();
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World5"));
            Color locked = worlds.CoverColour(4);
            Assert.GreaterOrEqual(locked.a, 0.7f, "A locked world's picture is too faint to recognise.");
            Assert.Less(locked.r, 1f, "A locked world should still look a little faded.");
            Assert.AreEqual(Color.white, worlds.CoverColour(0));
            worlds.Close();
        }

        [UnityTest]
        public IEnumerator TheSameGoldCoinIsUsedInTheRunAsInTheMenu()
        {
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Image hudCoin = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(i => i.name == "CoinIcon");
            Assert.IsNotNull(hudCoin, "The HUD has no coin icon.");
            Assert.AreEqual("icon_coin", hudCoin.sprite.name, "The HUD coin differs from the menu coin.");

            CollectibleData coin = Resources.FindObjectsOfTypeAll<CollectibleData>().FirstOrDefault(d => d.coinValue > 0);
            Assert.IsNotNull(coin, "No coin collectible is loaded.");
            Assert.AreEqual("coin_menu", coin.sprite.name, "Coins in the run should be the menu's gold coin.");
        }

        [Test]
        public void TheMenuNamesAllFourLanguages()
        {
            StringAssert.Contains("Deutsch", MainMenuStyle.Subtitle);
            StringAssert.Contains("ﺔﻴﺑﺮﻌﻟﺍ", MainMenuStyle.Subtitle, "العربية is missing.");
            foreach (char c in "ﺔﻴﺑﺮﻌﻟﺍ")
            {
                Assert.IsTrue(GameFonts.Arabic.HasCharacter(c, false, true), $"The Arabic font cannot draw U+{(int)c:X4}.");
            }
        }

        [Test]
        public void TheAlbumDrawsTheLatinLineLargerThanTheWordCard()
        {
            var word = new WordEntry { id = "apple", ar = "تُفَّاحَة", ar_shown = "x", ar_latin = "tuffâha" };
            Assert.Greater(WordAlbumUi.AlbumLatinPercent, 60);
            StringAssert.Contains($"<size={WordAlbumUi.AlbumLatinPercent}%>", word.CardText(LearningProgress.Arabic, true,
                WordAlbumUi.AlbumLatinPercent));
            StringAssert.Contains("<size=45%>", word.CardText(LearningProgress.Arabic, true));
        }

        [Test]
        public void LongVowelsUseACircumflexThatTitanOneDrawsAsALowercaseLetter()
        {
            foreach (string name in WordPack.PackOrder)
            {
                WordPack pack = WordPack.Load(name);
                var entries = pack.Words.Concat(pack.Phrases).Concat(pack.Counts).Concat(pack.Commands)
                    .Concat(pack.Sizes).Concat(pack.Sentences).Concat(pack.Numbers);

                foreach (WordEntry entry in entries)
                {
                    Assert.IsFalse(entry.ar_latin.IndexOfAny(new[] { 'ā', 'ī', 'ū' }) >= 0,
                        $"{name}/{entry.id}: '{entry.ar_latin}' still uses a macron.");
                }
            }

            Assert.IsTrue(GameFonts.Display.TryAddCharacters("âîû", out string missing),
                "Titan One lacks a circumflex vowel: " + missing);
        }
    }
}
