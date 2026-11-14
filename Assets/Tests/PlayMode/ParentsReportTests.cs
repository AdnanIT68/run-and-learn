using System;
using System.Collections;
using FootballRunner.Adventure;
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
    /// <summary>The parents corner reports play time this week and the words the child finds hardest.</summary>
    [Timeout(60000)]
    public class ParentsReportTests
    {
        private static readonly DateTime Friday = new DateTime(2026, 11, 13, 18, 0, 0);
        private string _saveBefore;
        private Func<DateTime> _clockBefore;
        private DateTime _now;

        [SetUp]
        public void FreshWeek()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            _clockBefore = DailyRewards.Clock;
            _now = Friday;
            DailyRewards.Clock = () => _now;
            SaveData data = SaveManager.Data;
            data.playDays.Clear();
            data.wordProgress.Clear();
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
            DailyRewards.Clock = _clockBefore;
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void TheWeekCountsTodayAndTheSixDaysBefore()
        {
            PlayTime.Add(600f);
            _now = Friday.AddDays(-3);
            PlayTime.Add(300f);
            _now = Friday.AddDays(-7);
            PlayTime.Add(900f);

            _now = Friday;
            Assert.AreEqual(600f, PlayTime.TodaySeconds, 0.01f);
            Assert.AreEqual(900f, PlayTime.WeekSeconds, 0.01f, "A day a week ago must not count.");
            Assert.AreEqual(15, PlayTime.Minutes(PlayTime.WeekSeconds));
            Assert.AreEqual(1, PlayTime.Minutes(5f), "Any play shows as at least a minute.");
        }

        [Test]
        public void OnlyTheLastTwoWeeksAreKept()
        {
            for (int day = 0; day < 20; day++)
            {
                _now = Friday.AddDays(-day);
                PlayTime.Add(60f);
            }

            Assert.LessOrEqual(SaveManager.Data.playDays.Count, 14);
        }

        [Test]
        public void HardestWordsAreTheOnesMissedAndNotYetCaught()
        {
            LearningProgress.RecordMiss("lemon");
            LearningProgress.RecordMiss("lemon");
            LearningProgress.RecordMiss("lemon");
            LearningProgress.RecordMiss("pear");
            LearningProgress.RecordMiss("apple");
            for (int i = 0; i < 3; i++)
            {
                LearningProgress.RecordCorrect("apple");
            }

            var hard = LearningProgress.HardestWords(4).ConvertAll(w => w.id);
            Assert.AreEqual("lemon", hard[0], "The most missed word should come first.");
            Assert.Contains("pear", hard);
            Assert.IsFalse(hard.Contains("apple"), "A word now caught right three times is not hard any more.");
        }

        [UnityTest]
        public IEnumerator RunningCountsAsPlayTime()
        {
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.Greater(PlayTime.TodaySeconds, 0.5f);
        }

        [UnityTest]
        public IEnumerator TheCornerShowsTheWeekAndTheHardWords()
        {
            // A little under 42 minutes, so a few seconds of the previous test's run cannot tip it over.
            PlayTime.Add(41.5f * 60f);
            LearningProgress.RecordMiss("lemon");
            LearningProgress.RecordMiss("lemon");

            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            ParentsCornerUi corner = Object.FindFirstObjectByType<ParentsCornerUi>();
            corner.Open();
            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));

            StringAssert.Contains("Ove sedmice: 42 min", corner.Report);
            StringAssert.Contains("Danas: 42 min", corner.Report);
            StringAssert.Contains("lemon", corner.Report, "The hard word is shown in the language being learned.");
            corner.Close();
        }
    }
}
