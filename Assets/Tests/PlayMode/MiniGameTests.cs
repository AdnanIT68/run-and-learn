using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FootballRunner.Adventure;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Parovi, Šta čuješ? and Složi riječ: short, optional games between levels.</summary>
    [Timeout(90000)]
    public class MiniGameTests
    {
        private string _saveBefore;

        [SetUp]
        public void KeepSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [TestCase("jabuka", "j,a,b,u,k,a")]
        [TestCase("džamija", "dž,a,m,i,j,a")]
        [TestCase("ljiljan", "lj,i,lj,a,n")]
        [TestCase("der Apfel", "a,p,f,e,l")]
        public void LettersAreTheOnesChildrenWrite(string word, string expected)
        {
            Assert.AreEqual(expected, string.Join(",", MiniGames.Letters(word)));
        }

        [Test]
        public void SpellingIsNotOfferedForArabic()
        {
            Assert.IsFalse(MiniGames.IsAvailable(MiniGames.Kind.Build, LearningProgress.Arabic));
            Assert.IsTrue(MiniGames.IsAvailable(MiniGames.Kind.Build, LearningProgress.Bosnian));
            Assert.IsTrue(MiniGames.IsAvailable(MiniGames.Kind.Memory, LearningProgress.Arabic));
        }

        [Test]
        public void PairsMatchAPictureWithItsWordAndTurnWrongPairsBack()
        {
            List<WordEntry> words = WordPack.Load("World1").Words.Take(4).ToList();
            var game = new MemoryGame(words);
            Assert.AreEqual(8, game.Cards.Count);

            int picture = game.Cards.FindIndex(c => c.IsPicture && c.Word == words[0]);
            int wrongWord = game.Cards.FindIndex(c => !c.IsPicture && c.Word == words[1]);
            int rightWord = game.Cards.FindIndex(c => !c.IsPicture && c.Word == words[0]);

            Assert.AreEqual(MemoryGame.Result.FirstOpened, game.Open(picture));
            Assert.AreEqual(MemoryGame.Result.NoMatch, game.Open(wrongWord));
            Assert.AreEqual(MemoryGame.Result.Ignored, game.Open(rightWord), "A third card opened before turning back.");
            game.TurnBack();
            Assert.IsFalse(game.Cards[picture].Open);

            game.Open(picture);
            Assert.AreEqual(MemoryGame.Result.Match, game.Open(rightWord));
            Assert.IsTrue(game.Cards[picture].Matched);
            Assert.IsFalse(game.IsDone);
        }

        [Test]
        public void WhatDoYouHearTakesFiveRightAnswers()
        {
            var game = new HearGame(WordPack.Load("World1").Words.Take(6).ToList());
            Assert.AreEqual(HearGame.Options, game.Choices.Count);
            Assert.AreEqual(game.Choices.Count, game.Choices.Distinct().Count());

            int wrong = game.Choices.FindIndex(w => w != game.Target);
            Assert.IsFalse(game.Answer(wrong));
            Assert.AreEqual(0, game.Round, "A wrong tap should not move on; the child tries again.");

            for (int i = 0; i < HearGame.Rounds; i++)
            {
                Assert.IsTrue(game.Answer(game.Choices.IndexOf(game.Target)));
            }

            Assert.IsTrue(game.IsDone);
            Assert.AreEqual(HearGame.Rounds, game.Right);
        }

        [Test]
        public void BuildTheWordTakesTheLettersInOrder()
        {
            var apple = new WordEntry { id = "apple", bs = "jabuka" };
            var game = new BuildWordGame(apple, LearningProgress.Bosnian);
            CollectionAssert.AreEquivalent(game.Letters, game.Tiles);

            int wrong = game.Tiles.FindIndex(t => t != "j");
            Assert.IsFalse(game.Tap(wrong));

            foreach (string letter in game.Letters)
            {
                Assert.IsTrue(game.Tap(game.Tiles.IndexOf(letter)), "Could not place " + letter);
            }

            Assert.IsTrue(game.IsDone);
            Assert.IsTrue(BuildWordGame.Suits(apple, LearningProgress.Bosnian));
            Assert.IsFalse(BuildWordGame.Suits(new WordEntry { id = "x", bs = "kišobran velik" }, LearningProgress.Bosnian));
        }

        [UnityTest]
        public IEnumerator TheLevelResultOpensMiniGamesAndAWinGivesCoins()
        {
            SaveManager.Data.wordTutorialDone = true;
            SaveManager.Data.selectedPack = WordPack.DefaultPack;
            SaveManager.Data.selectedLevel = 0;
            SaveManager.Data.pepiWelcomed.Add("World1:en");
            AdventureSession.Clear();
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Object.FindFirstObjectByType<LevelResultUi>().OpenMiniGames();
            MiniGameUi ui = MiniGameUi.Instance;
            Assert.IsTrue(ui.IsOpen, "MINI IGRE did not open.");

            int coins = SaveManager.Data.totalCoins;
            ui.Play(MiniGames.Kind.Build);
            BuildWordGame build = ui.Build;
            foreach (string letter in build.Letters)
            {
                Assert.IsTrue(ui.TapLetter(build.Tiles.IndexOf(letter)));
            }

            Assert.AreEqual(1, ui.Finished);
            Assert.AreEqual(coins + MiniGames.Reward, SaveManager.Data.totalCoins);

            ui.Play(MiniGames.Kind.Memory);
            Assert.AreEqual(8, ui.Memory.Cards.Count);

            // A wrong pair, then the screen is closed: nothing may carry on behind it.
            ui.OpenCard(0);
            int wrong = ui.Memory.Cards.FindIndex(c => c.Word != ui.Memory.Cards[0].Word);
            Assert.AreEqual(MemoryGame.Result.NoMatch, ui.OpenCard(wrong));
            ui.Close();
            Assert.IsFalse(ui.IsOpen);
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.IsTrue(ui.Memory.WaitingToTurnBack, "The closed screen kept playing.");
        }
    }
}
