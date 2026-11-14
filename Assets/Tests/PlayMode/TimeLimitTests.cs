using System;
using System.Collections;
using FootballRunner.Adventure;
using FootballRunner.Core;
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
    /// <summary>A parent's daily time limit stops play, and only the parents corner can change it.</summary>
    [Timeout(60000)]
    public class TimeLimitTests
    {
        private static readonly DateTime Monday = new DateTime(2026, 11, 16, 17, 0, 0);
        private string _saveBefore;
        private Func<DateTime> _clockBefore;

        [SetUp]
        public void FreshDay()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            _clockBefore = DailyRewards.Clock;
            DailyRewards.Clock = () => Monday;
            SaveData data = SaveManager.Data;
            data.playDays.Clear();
            data.dailyLimitMinutes = 0;
            data.wordTutorialDone = true;
            data.selectedPack = WordPack.DefaultPack;
            data.selectedLevel = 0;
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
            AdventureSession.Clear();
            FootballRunner.UI.DailyUi.AutoOpen = false;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void Restore()
        {
            DailyRewards.Clock = _clockBefore;
            FootballRunner.UI.DailyUi.AutoOpen = true;
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void TheLimitStepsThroughItsChoicesAndIsOffByDefault()
        {
            Assert.AreEqual(0, new SaveData().dailyLimitMinutes);
            TimeLimit.Minutes = 0;
            TimeLimit.Next();
            Assert.AreEqual(10, TimeLimit.Minutes);
            TimeLimit.Minutes = 60;
            TimeLimit.Next();
            Assert.AreEqual(0, TimeLimit.Minutes, "After an hour it goes back to no limit.");
        }

        [Test]
        public void TheLimitIsReachedOnceTodaysPlayUsesItUp()
        {
            TimeLimit.Minutes = 15;
            PlayTime.Add(14f * 60f);
            Assert.IsFalse(TimeLimit.IsReached);
            PlayTime.Add(60f);
            Assert.IsTrue(TimeLimit.IsReached);

            DailyRewards.Clock = () => Monday.AddDays(1);
            Assert.IsFalse(TimeLimit.IsReached, "A new day starts with fresh time.");
        }

        [Test]
        public void PepiHasALineForTheBreakInEveryMenuLanguage()
        {
            foreach (string language in new[] { "bs", "en", "de" })
            {
                Assert.IsNotNull(Resources.Load<AudioClip>($"Words/Pepi/Audio/{language}/{PepiVoice.Rest}"), language);
            }
        }

        [UnityTest]
        public IEnumerator UsingUpTheTimeStopsTheRun()
        {
            TimeLimit.Minutes = 10;
            PlayTime.Add(10f * 60f - 0.5f);
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return new WaitForSecondsRealtime(1.2f);

            Assert.AreEqual(GameState.Resting, GameManager.Instance.State);
            Assert.AreEqual(0f, Time.timeScale, "The run kept going after the time was up.");
            RestPanelUi rest = Object.FindFirstObjectByType<RestPanelUi>();
            Assert.IsNotNull(rest);
            Assert.IsTrue(rest.IsShowing, "The rest panel is not on screen.");
            Assert.IsFalse(GameManager.KeepsScreenAwake(GameState.Resting));
        }

        [UnityTest]
        public IEnumerator PlayStaysBlockedInTheMenuUntilAParentChangesTheLimit()
        {
            TimeLimit.Minutes = 10;
            PlayTime.Add(11f * 60f);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            var play = (UnityEngine.UI.Button)typeof(MainMenuController)
                .GetField("playButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(menu);
            play.onClick.Invoke();
            yield return null;
            yield return null;

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "Play started a run past the limit.");
            RestPanelUi rest = Object.FindFirstObjectByType<RestPanelUi>();
            Assert.IsTrue(rest.IsShowing);

            // The panel's only button leads to the parents corner, still behind its gate.
            rest.PressButton();
            ParentsCornerUi corner = Object.FindFirstObjectByType<ParentsCornerUi>();
            Assert.IsTrue(corner.IsGateShowing, "The rest panel skipped the parents' gate.");
            Assert.IsFalse(corner.PressAnswer(corner.GateAnswer + 1));
            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));

            StringAssert.Contains("10 MIN", corner.TimeLimitLabel);
            corner.NextTimeLimit();
            Assert.AreEqual(15, TimeLimit.Minutes);
            Assert.IsFalse(TimeLimit.IsReached, "Raising the limit should let play go on.");
            corner.Close();
        }
    }
}
