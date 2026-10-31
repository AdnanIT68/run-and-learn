using System.Collections;
using System.Linq;
using FootballRunner.Adventure;
using FootballRunner.Effects;
using FootballRunner.Gameplay;
using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using FootballRunner.Utilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Double jump, Pepi, particle effects and the vibration setting.</summary>
    [Timeout(60000)]
    public class FeelAndCompanionTests
    {
        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator LoadGameScene()
        {
            Debug.Log("[TEST] " + TestContext.CurrentContext.Test.Name);
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;
            AdventureSession.Clear();
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void RestoreState()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [UnityTest]
        public IEnumerator RunnerCanDoubleJump()
        {
            yield return null;
            Assert.GreaterOrEqual(PlayerController.Instance.MaxJumps, 2, "Double jump is not enabled.");
        }

        [UnityTest]
        public IEnumerator PepiFliesAlongWithTheRunner()
        {
            yield return new WaitForSeconds(2f);

            Assert.IsNotNull(PepiCompanion.Instance, "Pepi is missing from the Game scene.");
            float distance = Vector2.Distance(PepiCompanion.Instance.transform.position,
                PlayerController.Instance.transform.position);
            Assert.Less(distance, 4f, "Pepi fell behind the runner.");
        }

        [UnityTest]
        public IEnumerator PepiPointsAtTheRightBubbleOnTheGuidedWord()
        {
            SaveManager.Data.wordTutorialDone = false;
            float waited = 0f;

            while (WordChallengeDirector.Instance.CurrentWord == null && waited < 5f)
            {
                WordChallengeDirector.Instance.ForceChallenge();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(WordChallengeDirector.Instance.IsGuided, "The first word was not guided.");

            bool pointed = false;
            waited = 0f;

            while (!pointed && WordChallengeDirector.Instance.CurrentWord != null && waited < 15f)
            {
                pointed = PepiCompanion.Instance.IsPointing;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(pointed, "Pepi never flew to the right bubble.");
        }

        [UnityTest]
        public IEnumerator TapHandShowsOnlyWhileTheGuidedWordWaitsForAJump()
        {
            SaveManager.Data.wordTutorialDone = false;
            yield return null;

            Assert.IsNotNull(FootballRunner.UI.TapHint.Instance, "The Game scene has no tap hint.");
            Assert.IsFalse(FootballRunner.UI.TapHint.Instance.IsShowing, "The hand showed before any word.");

            float waited = 0f;

            while (WordChallengeDirector.Instance.CurrentWord == null && waited < 5f)
            {
                WordChallengeDirector.Instance.ForceChallenge();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            bool shown = false;
            waited = 0f;

            while (!shown && WordChallengeDirector.Instance.CurrentWord != null && waited < 15f)
            {
                shown = FootballRunner.UI.TapHint.Instance.IsShowing;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(shown, "The tapping hand never appeared on the guided word.");

            // Catching the word ends the guide, and the hand goes with it.
            WordBubble right = WordChallengeDirector.Instance.Bubbles
                .First(b => b != null && b.Word == WordChallengeDirector.Instance.CurrentWord);
            WordChallengeDirector.Instance.OnBubbleTouched(right);
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.IsFalse(FootballRunner.UI.TapHint.Instance.IsShowing, "The hand stayed after the word was caught.");
        }

        [UnityTest]
        public IEnumerator ThreeCleanCatchesMakeAStreakAndAMistakeBreaksIt()
        {
            SaveManager.Data.wordTutorialDone = true;
            yield return null;

            var feel = FootballRunner.Core.GameManager.Instance.GetComponent<GameFeel>();
            WordChallengeDirector director = WordChallengeDirector.Instance;

            for (int caught = 1; caught <= GameFeel.StreakLength; caught++)
            {
                yield return WaitForWord(director);
                director.OnBubbleTouched(director.Bubbles.First(b => b.Word == director.CurrentWord));
                yield return null;
                Assert.AreEqual(caught, feel.Streak);
            }

            Assert.Greater(FxPlayer.Instance.LiveCount, 0, "The streak burst left no confetti.");

            // A wrong bubble first, then the right one: practice, but the streak starts over.
            yield return WaitForWord(director);
            director.OnBubbleTouched(director.Bubbles.First(b => b.Word != director.CurrentWord));
            director.OnBubbleTouched(director.Bubbles.First(b => b.Word == director.CurrentWord));
            yield return null;
            Assert.AreEqual(0, feel.Streak, "A wrong bubble did not break the streak.");
        }

        private static IEnumerator WaitForWord(WordChallengeDirector director)
        {
            float waited = 0f;

            while (director.CurrentWord == null && waited < 10f)
            {
                director.ForceChallenge();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(director.CurrentWord, "No word was asked.");
        }

        [UnityTest]
        public IEnumerator SparklesAppearAndCleanUpAfterThemselves()
        {
            yield return null;

            FxPlayer.Instance.Sparkle(Vector3.zero);
            Assert.Greater(FxPlayer.Instance.LiveCount, 0, "No particles were emitted.");

            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0, FxPlayer.Instance.LiveCount, "Particles were not recycled.");
        }

        [UnityTest]
        public IEnumerator RunUsesTheChosenWorld()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            WordPack garden = WordPack.Load("World1");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(garden.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World2"));

            // The director reads the chosen world when the run starts.
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Assert.AreEqual("World2", WordChallengeDirector.Instance.Pack.Name);
        }

        [UnityTest]
        public IEnumerator FarmCountingAsksForOneAnimalInDifferentNumbers()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordTutorialDone = true;
            WordPack garden = WordPack.Load("World1");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(garden.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World2"));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            float waited = 0f;

            while (director.CurrentWord == null && waited < 5f)
            {
                director.ForceChallenge(true);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            WordEntry target = director.CurrentWord;
            Assert.IsNotNull(target, "No counting challenge started.");
            Assert.IsTrue(target.IsCounting, "The farm did not ask a counting question.");
            Assert.AreEqual(3, director.Bubbles.Count);

            var counts = new System.Collections.Generic.HashSet<int>();
            int answers = 0;

            foreach (WordBubble bubble in director.Bubbles)
            {
                Assert.AreEqual(target.PictureId, bubble.Word.PictureId, "Every bubble must show the same animal.");
                Assert.AreEqual(bubble.Word.Count, bubble.PictureCount, "The bubble shows the wrong number of pictures.");
                counts.Add(bubble.Word.Count);
                answers += bubble.Word == target ? 1 : 0;
            }

            Assert.AreEqual(3, counts.Count, "Two bubbles showed the same number.");
            Assert.AreEqual(1, answers, "There must be exactly one right answer.");
        }

        [UnityTest]
        public IEnumerator BazaarCommandsPutTheRightStarAtTheRightHeight()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordTutorialDone = true;
            WordPack home = WordPack.Load("World3");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(home.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World4"));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            var seen = new System.Collections.Generic.HashSet<string>();
            float waited = 0f;

            while (seen.Count < 3 && waited < 20f)
            {
                if (director.CurrentWord == null && !director.ForceChallenge(command: true))
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                    continue;
                }

                WordEntry target = director.CurrentWord;
                Assert.IsTrue(target.IsCommand, "The bazaar did not give a spoken command.");
                Assert.AreEqual(2, director.Bubbles.Count);

                WordBubble right = director.Bubbles.First(b => b.Word == target);
                WordBubble wrong = director.Bubbles.First(b => b.Word != target);
                Assert.AreEqual(right.transform.position.x, wrong.transform.position.x, 0.01f,
                    "Command stars must share one spot.");
                Assert.AreEqual("star", right.Word.PictureId);
                Assert.AreEqual("star", wrong.Word.PictureId, "Both stars must look the same.");

                float rightY = right.transform.position.y;
                float wrongY = wrong.transform.position.y;

                if (target.action == "stay")
                {
                    Assert.Less(rightY, wrongY, "\"Stay down\" must reward the low star.");
                }
                else
                {
                    Assert.Greater(rightY, wrongY, $"\"{target.action}\" must reward the higher star.");
                }

                seen.Add(target.action);
                director.OnBubbleTouched(right);
                yield return null;
            }

            CollectionAssert.AreEquivalent(new[] { "jump", "stay", "double" }, seen,
                "Not every command showed up.");
        }

        [UnityTest]
        public IEnumerator RiverAsksBigOrSmallWithTheSamePicture()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordTutorialDone = true;
            WordPack bazaar = WordPack.Load("World4");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(bazaar.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World5"));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            float waited = 0f;

            while (director.CurrentWord == null && waited < 5f)
            {
                director.ForceChallenge(size: true);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            WordEntry target = director.CurrentWord;
            Assert.IsNotNull(target, "No size challenge started.");
            Assert.IsTrue(target.IsSize, "The river did not ask for big or small.");
            Assert.AreEqual(2, director.Bubbles.Count);

            WordBubble right = director.Bubbles.First(b => b.Word == target);
            WordBubble wrong = director.Bubbles.First(b => b.Word != target);
            Assert.AreEqual(right.Word.PictureId, wrong.Word.PictureId, "Both bubbles must show the same thing.");
            Assert.AreNotEqual(right.Word.size, wrong.Word.size);

            WordBubble big = right.Word.size == "big" ? right : wrong;
            WordBubble small = big == right ? wrong : right;
            Assert.Greater(big.PictureSize, small.PictureSize * 2f, "Big and small must be easy to tell apart.");
        }

        [UnityTest]
        public IEnumerator MountainAsksWholeSentencesWithPicturesFromOneGroup()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordTutorialDone = true;
            WordPack river = WordPack.Load("World5");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(river.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World6"));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            float waited = 0f;

            while (director.CurrentWord == null && waited < 5f)
            {
                director.ForceChallenge(sentence: true);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            WordEntry target = director.CurrentWord;
            Assert.IsNotNull(target, "No sentence challenge started.");
            Assert.IsTrue(target.IsSentence, "The mountain did not ask a sentence.");
            Assert.AreEqual(2, director.Bubbles.Count);

            WordBubble right = director.Bubbles.First(b => b.Word == target);
            WordBubble wrong = director.Bubbles.First(b => b.Word != target);
            Assert.AreEqual(target.group, wrong.Word.group, "The wrong picture must come from the same group.");
            Assert.AreNotEqual(right.Word.PictureId, wrong.Word.PictureId, "The two pictures must differ.");
        }

        [Test]
        public void EveryWorldHasItsOwnSceneryArt()
        {
            foreach (string world in WordPack.PackOrder)
            {
                Assert.IsNotNull(Resources.Load<Sprite>($"Worlds/{world}/far"), $"{world} has no far backdrop.");
                Assert.IsNotNull(Resources.Load<Sprite>($"Worlds/{world}/mid"), $"{world} has no middle layer.");

                foreach (string kind in new[] { "spike", "crate", "saw" })
                {
                    Assert.IsNotNull(Resources.Load<Sprite>($"Worlds/Obstacles/{world}_{kind}"),
                        $"{world} has no {kind} obstacle picture.");
                }
            }

            foreach (string ground in new[] { "dirt", "stone", "sand", "snow" })
            {
                Assert.AreEqual(5, Resources.LoadAll<Sprite>($"Worlds/Ground/{ground}").Length,
                    $"Ground set {ground} is incomplete.");
            }
        }

        [UnityTest]
        public IEnumerator MountainRunHasSnowSkyTitleWeatherAndDressedObstacles()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            WordPack river = WordPack.Load("World5");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(river.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.SelectPack("World6"));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
            yield return null;

            WorldScenery scenery = WorldScenery.Instance;
            Assert.IsNotNull(scenery);
            Assert.AreEqual("World6", scenery.WorldName);
            Assert.AreEqual(0, Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None).Length,
                "The old jungle backdrop is still showing.");
            Assert.Greater(scenery.ParticleCount, 0, "No snow is falling.");
            StringAssert.StartsWith(WordPack.Load("World6").Title(LearningProgress.Language).ToUpperInvariant(),
                EnvironmentDirector.Instance.CurrentDisplayName);
            StringAssert.EndsWith(" 1", EnvironmentDirector.Instance.CurrentDisplayName, "The banner should name the level.");

            bool snowyGround = Object.FindObjectsByType<Chunk>(FindObjectsSortMode.None)
                .SelectMany(c => c.GetComponentsInChildren<SpriteRenderer>())
                .Any(r => r.sprite != null && r.sprite.name.StartsWith("terrain_snow_"));
            Assert.IsTrue(snowyGround, "The ground is not snow.");

            Obstacles.Obstacle obstacle = Object.FindObjectsByType<Obstacles.Obstacle>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            Collider2D hitBox = obstacle.GetComponent<Collider2D>();
            bool hitBoxWasOn = hitBox.enabled;
            scenery.SkinObstacle(obstacle.gameObject, obstacle.name);

            Transform skin = obstacle.transform.Find("WorldSkin");
            Assert.IsNotNull(skin, "The obstacle was not dressed for the mountain.");
            Assert.IsNotNull(skin.GetComponent<SpriteRenderer>().sprite);
            Assert.AreEqual(hitBoxWasOn, hitBox.enabled, "Dressing an obstacle must not touch its collider.");
        }

        [UnityTest]
        public IEnumerator FullGoalRowEndsTheLevelWithStarsResultsAndNextLevel()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordTutorialDone = true;
            SaveManager.Data.selectedPack = "World1";
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Assert.IsFalse(LearningProgress.IsPackUnlocked("World2"), "The farm should still be locked.");
            yield return CatchGoalRow(mistakes: 0);

            float waited = 0f;
            while (Core.GameManager.Instance.State != Core.GameState.LevelComplete && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(Core.GameState.LevelComplete, Core.GameManager.Instance.State, "The level did not end.");
            Assert.AreEqual(0f, Time.timeScale, "The run should stop on the result screen.");
            Assert.AreEqual(3, WordChallengeDirector.Instance.LevelStars, "No mistakes should earn three stars.");
            Assert.AreEqual(5, WordChallengeDirector.Instance.LevelWords.Count);
            Assert.AreEqual(3, LearningProgress.Stars("World1", 0), "The stars were not saved.");
            Assert.IsTrue(LearningProgress.IsLevelUnlocked("World1", 1), "Finishing level 1 should open level 2.");
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World2"), "The farm waits for level 3 of the garden.");

            LevelResultUi results = Object.FindFirstObjectByType<LevelResultUi>();
            yield return null;
            Assert.IsTrue(results.IsShown, "The result screen did not open.");
            Assert.IsTrue(results.HasNext, "Next should offer level 2.");

            results.Next();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual("World1", LearningProgress.SelectedPack);
            Assert.AreEqual(1, LearningProgress.SelectedLevel, "Next did not move on to level 2.");
        }

        [UnityTest]
        public IEnumerator MistakesCostStars()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordTutorialDone = true;
            SaveManager.Data.selectedPack = "World1";
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            yield return CatchGoalRow(mistakes: 1);

            Assert.IsTrue(WordChallengeDirector.Instance.LevelDone);
            Assert.AreEqual(2, WordChallengeDirector.Instance.LevelStars, "One mistake should earn two stars.");
        }

        /// <summary>Catches a full goal row, touching a wrong bubble first on the first few words.</summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator EveryResultButtonLeadsToARunThatReallyPlays()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordTutorialDone = true;
            SaveManager.Data.selectedPack = "World1";
            SaveManager.Data.selectedLevel = 0;
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            // Finish level 1, then DALJE.
            yield return FinishLevelAndWaitForResults();
            Object.FindFirstObjectByType<LevelResultUi>().Next();
            yield return AssertFreshRunThatPlays(expectedLevel: 1, "after NEXT");

            // Finish level 2, then PONOVO.
            yield return FinishLevelAndWaitForResults();
            Object.FindFirstObjectByType<LevelResultUi>().Again();
            yield return AssertFreshRunThatPlays(expectedLevel: 1, "after AGAIN");

            // Finish it again, then MENI and IGRAJ from the main menu.
            yield return FinishLevelAndWaitForResults();
            Object.FindFirstObjectByType<LevelResultUi>().Menu();
            yield return WaitForScene(Core.GameManager.MainMenuSceneName);
            yield return null;
            Assert.AreEqual(1f, Time.timeScale, "Time stayed frozen in the main menu.");

            var menu = Object.FindFirstObjectByType<UI.MainMenuController>();
            var play = (UnityEngine.UI.Button)typeof(UI.MainMenuController)
                .GetField("playButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(menu);
            Assert.IsTrue(play.interactable && play.isActiveAndEnabled, "Play is not tappable in the menu.");
            play.onClick.Invoke();
            yield return AssertFreshRunThatPlays(expectedLevel: 1, "after MENU then PLAY");
        }

        private static IEnumerator FinishLevelAndWaitForResults()
        {
            yield return CatchGoalRow(mistakes: 0);

            float deadline = Time.realtimeSinceStartup + 5f;
            while (Core.GameManager.Instance.State != Core.GameState.LevelComplete &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            yield return null;
            Assert.IsTrue(Object.FindFirstObjectByType<LevelResultUi>().IsShown, "The result screen did not open.");
        }

        /// <summary>A new Game scene whose runner moves and whose words come by themselves.</summary>
        private static IEnumerator AssertFreshRunThatPlays(int expectedLevel, string when)
        {
            Core.GameManager before = Core.GameManager.Instance;
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((Core.GameManager.Instance == null || Core.GameManager.Instance == before) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsNotNull(Core.GameManager.Instance, "No new run started " + when + ".");
            Assert.AreNotSame(before, Core.GameManager.Instance, "The old run is still on screen " + when + ".");
            yield return null;
            yield return null;

            Assert.AreEqual(Core.GameState.Playing, Core.GameManager.Instance.State, "The run is not playing " + when + ".");
            Assert.AreEqual(1f, Time.timeScale, "Time is frozen " + when + ".");
            Assert.IsFalse(Object.FindFirstObjectByType<LevelResultUi>().IsShown, "The old results are still up " + when + ".");

            WordChallengeDirector director = WordChallengeDirector.Instance;
            Assert.AreEqual(expectedLevel, director.Level, "Wrong level " + when + ".");
            Assert.IsFalse(director.LevelDone, "The new level starts already finished " + when + ".");

            // The runner moves and the first word arrives without any help.
            float startX = PlayerController.Instance.transform.position.x;
            deadline = Time.realtimeSinceStartup + 10f;
            while (director.CurrentWord == null && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsNotNull(director.CurrentWord, "No word came by itself " + when + ".");
            Assert.AreNotEqual(startX, PlayerController.Instance.transform.position.x, "The runner stands still " + when + ".");
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name);
        }

        private static IEnumerator CatchGoalRow(int mistakes)
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;

            for (int caught = 0; caught < director.GoalSize;)
            {
                if (director.CurrentWord == null && !director.ForceChallenge())
                {
                    yield return null;
                    continue;
                }

                if (mistakes > 0)
                {
                    mistakes--;
                    director.OnBubbleTouched(director.Bubbles.First(b => b.Word != director.CurrentWord));
                }

                director.OnBubbleTouched(director.Bubbles.First(b => b.Word == director.CurrentWord));
                caught++;
                yield return null;
            }
        }

        [Test]
        public void VibrationSettingIsSaved()
        {
            Haptics.Enabled = false;
            Assert.IsFalse(SaveManager.Data.vibrationEnabled);

            Haptics.Enabled = true;
            Assert.IsTrue(SaveManager.Data.vibrationEnabled);

            // Safe to call in the Editor: vibration is a no-op off device.
            Haptics.Light();
        }
    }
}
