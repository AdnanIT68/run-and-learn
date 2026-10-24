using System;
using System.Collections;
using System.Linq;
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
    /// <summary>Daily gift track and the day's three tasks.</summary>
    [Timeout(60000)]
    public class DailyRewardTests
    {
        private static readonly DateTime Monday = new DateTime(2026, 11, 2, 9, 0, 0);
        private string _saveBefore;
        private DateTime _now;

        [SetUp]
        public void FreshDay()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;
            _now = Monday;
            DailyRewards.Clock = () => _now;

            SaveData data = SaveManager.Data;
            data.totalCoins = 0;
            data.dailyGiftDate = "";
            data.dailyGiftStep = 0;
            data.missionDate = "";
            data.missions.Clear();
        }

        [TearDown]
        public void Restore()
        {
            DailyRewards.Clock = () => DateTime.Now;
            DailyUi.AutoOpen = true;
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void GiftOncePerDayAndAMissedDayCostsNothing()
        {
            Assert.IsTrue(DailyRewards.GiftReady);
            Assert.AreEqual(DailyRewards.GiftTrack[0], DailyRewards.ClaimGift());
            Assert.IsFalse(DailyRewards.GiftReady);
            Assert.AreEqual(0, DailyRewards.ClaimGift(), "The gift was taken twice on one day.");

            _now = Monday.AddDays(1);
            Assert.AreEqual(DailyRewards.GiftTrack[1], DailyRewards.ClaimGift());

            // Three days away: the track waits instead of starting over.
            _now = Monday.AddDays(4);
            Assert.AreEqual(DailyRewards.GiftTrack[2], DailyRewards.ClaimGift());
            Assert.AreEqual(DailyRewards.GiftTrack.Take(3).Sum(), SaveManager.Data.totalCoins);
        }

        [Test]
        public void GiftTrackStartsAgainAfterSevenDays()
        {
            for (int day = 0; day < DailyRewards.GiftTrack.Length; day++)
            {
                _now = Monday.AddDays(day);
                Assert.AreEqual(DailyRewards.GiftTrack[day], DailyRewards.ClaimGift());
            }

            _now = Monday.AddDays(DailyRewards.GiftTrack.Length);
            Assert.AreEqual(DailyRewards.GiftTrack[0], DailyRewards.NextGift);
        }

        [Test]
        public void ThreeTasksPayOnceAndRenewTomorrow()
        {
            Assert.AreEqual(3, DailyRewards.Missions.Count);
            DailyMission words = DailyRewards.Missions[0];
            Assert.AreEqual(nameof(DailyRewards.Kind.CatchWords), words.kind);

            int completions = 0;
            void Count(DailyMission _) => completions++;
            DailyRewards.MissionCompleted += Count;

            try
            {
                DailyRewards.Report(DailyRewards.Kind.CatchWords, words.target - 1);
                Assert.IsFalse(words.done);
                Assert.AreEqual(0, SaveManager.Data.totalCoins);

                DailyRewards.Report(DailyRewards.Kind.CatchWords, 5);
                Assert.IsTrue(words.done);
                Assert.AreEqual(words.target, words.progress);
                Assert.AreEqual(words.reward, SaveManager.Data.totalCoins);

                DailyRewards.Report(DailyRewards.Kind.CatchWords);
                Assert.AreEqual(1, completions, "A finished task paid twice.");
                Assert.AreEqual(words.reward, SaveManager.Data.totalCoins);
            }
            finally
            {
                DailyRewards.MissionCompleted -= Count;
            }

            _now = Monday.AddDays(1);
            Assert.AreEqual(3, DailyRewards.Missions.Count);
            Assert.IsTrue(DailyRewards.Missions.All(m => !m.done && m.progress == 0), "Yesterday's tasks carried over.");

            foreach (DailyMission mission in DailyRewards.Missions)
            {
                Assert.IsFalse(string.IsNullOrEmpty(DailyRewards.Describe(mission)));

            }
        }

        [UnityTest]
        public IEnumerator CatchingAWordInARunMovesTheTaskAndShowsTheToast()
        {
            SaveManager.Data.wordTutorialDone = true;
            LearningProgress.SelectPack("World1");
            DailyMission words = DailyRewards.Missions[0];
            words.progress = words.target - 1;

            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            float waited = 0f;

            while (director.CurrentWord == null && waited < 5f)
            {
                director.ForceChallenge(WordChallengeDirector.Kind.Word);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(director.CurrentWord);
            WordBubble right = director.Bubbles.First(b => b.Word == director.CurrentWord);
            director.OnBubbleTouched(right);
            yield return null;
            yield return null;

            Assert.IsTrue(words.done, "Catching a word did not count towards today's task.");
            Assert.AreEqual(words.reward, SaveManager.Data.totalCoins);

            MissionToastUi toast = Object.FindFirstObjectByType<MissionToastUi>();
            Assert.IsNotNull(toast);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(toast.IsShowing, "No message told the child the task was done.");
        }

        [UnityTest]
        public IEnumerator MenuOffersTodaysGift()
        {
            DailyUi.AutoOpen = false;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            DailyUi daily = Object.FindFirstObjectByType<DailyUi>();
            Assert.IsNotNull(daily, "The main menu has no TODAY screen.");
            Assert.IsFalse(daily.IsOpen);

            daily.Open();
            Assert.IsTrue(daily.IsOpen);
            Assert.AreEqual(DailyRewards.GiftTrack[0], daily.PressClaim());
            Assert.AreEqual(0, daily.PressClaim());
            daily.Close();
            Assert.IsFalse(daily.IsOpen);
        }
    }
}
