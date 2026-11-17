using System;
using System.Collections;
using System.Collections.Generic;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using FootballRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FootballRunner.Tests
{
    /// <summary>One word a day on the main menu, from the open worlds; catching it gives coins once a day.</summary>
    [Timeout(60000)]
    public class WordOfTheDayTests
    {
        private string _saveBefore;
        private Func<DateTime> _clockBefore;

        [SetUp]
        public void FreshSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            _clockBefore = DailyRewards.Clock;
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordOfDayClaimed = "";
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
            DailyRewards.Clock = () => new DateTime(2026, 11, 20, 9, 0, 0);
        }

        [TearDown]
        public void Restore()
        {
            DailyRewards.Clock = _clockBefore;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void TheWordStaysAllDayAndComesFromAnOpenWorld()
        {
            (string pack, WordEntry word) = WordOfTheDay.Today();
            Assert.IsNotNull(word);
            Assert.IsTrue(LearningProgress.IsPackUnlocked(pack), $"{pack} is locked.");

            DailyRewards.Clock = () => new DateTime(2026, 11, 20, 21, 30, 0);
            Assert.AreEqual(word.id, WordOfTheDay.Today().word.id, "The word changed during the day.");

            var seen = new HashSet<string>();
            for (int day = 0; day < 7; day++)
            {
                DateTime date = new DateTime(2026, 11, 20).AddDays(day);
                DailyRewards.Clock = () => date;
                (string p, WordEntry w) = WordOfTheDay.Today();
                Assert.IsTrue(LearningProgress.IsPackUnlocked(p));
                seen.Add(p + "/" + w.id);
            }

            Assert.Greater(seen.Count, 2, "A week should bring different words.");
        }

        [Test]
        public void CatchingItGivesCoinsOncePerDay()
        {
            (string pack, WordEntry word) = WordOfTheDay.Today();
            int coins = SaveManager.Data.totalCoins;

            Assert.IsFalse(WordOfTheDay.OnCaught(pack, "not_the_word"));
            Assert.IsTrue(WordOfTheDay.OnCaught(pack, word.id));
            Assert.AreEqual(coins + WordOfTheDay.Reward, SaveManager.Data.totalCoins);
            Assert.IsFalse(WordOfTheDay.OnCaught(pack, word.id), "Only once a day.");
            Assert.AreEqual(coins + WordOfTheDay.Reward, SaveManager.Data.totalCoins);

            DateTime tomorrow = new DateTime(2026, 11, 21, 9, 0, 0);
            DailyRewards.Clock = () => tomorrow;
            Assert.IsFalse(WordOfTheDay.IsClaimedToday, "A new day brings a new chance.");
        }

        [UnityTest]
        public IEnumerator TheMenuShowsItAndATapChoosesItsWorld()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            WordOfTheDayUi card = Object.FindFirstObjectByType<WordOfTheDayUi>();
            Assert.IsNotNull(card, "No word of the day on the menu.");
            Assert.IsTrue(card.IsShown);
            Assert.AreEqual(WordOfTheDay.Today().word.id, card.Word.id);

            card.Press();
            Assert.AreEqual(card.Pack, LearningProgress.SelectedPack);
        }
    }
}
