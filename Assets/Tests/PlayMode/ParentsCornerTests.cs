using System.Collections;
using System.Linq;
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
    /// <summary>The settings button leads to a gated corner for parents with the child's progress.</summary>
    [Timeout(60000)]
    public class ParentsCornerTests
    {
        private string _saveBefore;
        private bool _hapticsBefore;

        [UnitySetUp]
        public IEnumerator LoadMenu()
        {
            Debug.Log("[TEST] " + TestContext.CurrentContext.Test.Name);
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            _hapticsBefore = Haptics.Enabled;
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void Restore()
        {
            Haptics.Enabled = _hapticsBefore;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [UnityTest]
        public IEnumerator SettingsButtonOpensTheGateAndOnlyTheRightSumLetsParentsIn()
        {
            ParentsCornerUi corner = Object.FindFirstObjectByType<ParentsCornerUi>();
            Assert.IsNotNull(corner, "The main menu has no parents' corner.");

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            var settingsButton = (Button)typeof(MainMenuController)
                .GetField("settingsButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(menu);
            settingsButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(corner.IsGateShowing, "The settings button did not open the parents' gate.");
            Assert.IsFalse(corner.IsCornerShowing);
            GameObject oldSettings = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(go => go.name == "SettingsPanel" && go.scene.IsValid());
            Assert.IsTrue(oldSettings == null || !oldSettings.activeSelf, "The old settings opened without the gate.");

            Assert.GreaterOrEqual(corner.GateAnswer, 9, "The sum is too easy for the gate.");
            Assert.IsFalse(corner.PressAnswer(corner.GateAnswer + 1), "A wrong answer opened the corner.");
            Assert.IsTrue(corner.IsGateShowing, "A wrong answer closed the gate.");

            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));
            Assert.IsTrue(corner.IsCornerShowing, "The right answer did not open the corner.");

            corner.Close();
            Assert.IsFalse(corner.IsOpen);
        }

        [UnityTest]
        public IEnumerator CornerShowsProgressAndSwitchesSoundAndVibration()
        {
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordProgress.Clear();
            WordPack garden = WordPack.Load("World1");
            LearningProgress.RecordCorrect(garden.Words[0].id);
            LearningProgress.RecordCorrect(garden.Words[1].id);
            LearningProgress.RecordLevelComplete("World1", 0, 3);
            yield return null;

            ParentsCornerUi corner = Object.FindFirstObjectByType<ParentsCornerUi>();
            corner.Open();
            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));

            StringAssert.Contains(": 2 ", corner.Summary, "Learned words are not counted.");
            StringAssert.Contains(" 1/40", corner.Summary, "Finished levels are not counted.");
            StringAssert.Contains(" 3/120", corner.Summary, "Stars are not counted.");

            bool sound = SaveManager.Data.soundEnabled;
            corner.ToggleSound();
            Assert.AreEqual(!sound, SaveManager.Data.soundEnabled, "The sound switch did nothing.");
            corner.ToggleSound();
            Assert.AreEqual(sound, SaveManager.Data.soundEnabled);

            bool vibration = Haptics.Enabled;
            corner.ToggleVibration();
            Assert.AreEqual(!vibration, Haptics.Enabled, "The vibration switch did nothing.");
        }
    }
}
