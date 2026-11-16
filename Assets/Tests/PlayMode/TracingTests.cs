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
    /// <summary>Piši slovo: trace Latin and Arabic letters with a finger; Pepi praises.</summary>
    [Timeout(90000)]
    public class TracingTests
    {
        private string _saveBefore;

        [SetUp]
        public void KeepSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [TestCase("bs", 30)]
        [TestCase("en", 26)]
        [TestCase("de", 29)]
        [TestCase("ar", 28)]
        public void EveryLetterHasAGuideAMaskAndAVoice(string language, int count)
        {
            IReadOnlyList<TracingLetters.Letter> alphabet = TracingLetters.AlphabetOf(language);
            Assert.AreEqual(count, alphabet.Count);

            foreach (TracingLetters.Letter letter in alphabet)
            {
                bool[] mask = TracingLetters.MaskOf(letter.id);
                Assert.IsNotNull(mask, letter.id + " has no mask");
                Assert.AreEqual(TracingLetters.Grid * TracingLetters.Grid, mask.Length);
                Assert.Greater(mask.Count(c => c), 15, letter.id + " covers almost nothing");
                Assert.IsNotNull(TracingLetters.Guide(letter.id), letter.id + " has no guide picture");
                Assert.IsNotNull(TracingLetters.Voice(language, letter.id), $"{language}/{letter.id} has no voice");
            }
        }

        [Test]
        public void TracingTheWholeLetterFinishesIt()
        {
            TracingGame game = NewGame("latin_A");
            foreach (Vector2 point in InsidePoints(game.LetterId))
            {
                game.Touch(point);
            }

            Assert.IsTrue(game.IsDone);
            Assert.Greater(game.Coverage, 0.95f);
        }

        [Test]
        public void ScribblingAroundTheLetterDoesNotCount()
        {
            TracingGame game = NewGame("latin_A");
            bool[] mask = TracingLetters.MaskOf("latin_A");
            int grid = TracingLetters.Grid;

            for (int y = 0; y < grid; y++)
            {
                for (int x = 0; x < grid; x++)
                {
                    game.Touch(new Vector2((x + 0.5f) / grid, 1f - (y + 0.5f) / grid));
                }
            }

            Assert.Greater(game.Coverage, 0.95f, "Filling the whole board covers the letter too...");
            Assert.IsFalse(game.IsDone, "...but painting the whole board must not count as tracing.");
        }

        [Test]
        public void HalfALetterIsNotEnough()
        {
            TracingGame game = NewGame("latin_B");
            List<Vector2> points = InsidePoints("latin_B");
            foreach (Vector2 point in points.Take(points.Count / 4))
            {
                game.Touch(point);
            }

            Assert.IsFalse(game.IsDone);
            game.Clear();
            Assert.AreEqual(0f, game.Coverage);
        }

        [Test]
        public void TheLettersComeFromTheWorldsWords()
        {
            List<string> bosnian = TracingLetters.ForWorld(WordPack.Load("World1"), LearningProgress.Bosnian)
                .Select(l => l.letter).ToList();
            CollectionAssert.Contains(bosnian, "J", "jabuka starts with J");

            List<string> arabic = TracingLetters.ForWorld(WordPack.Load("World1"), LearningProgress.Arabic)
                .Select(l => l.letter).ToList();
            CollectionAssert.Contains(arabic, "ت", "تُفَّاحَة starts with ت");
        }

        [UnityTest]
        public IEnumerator TracingInTheMiniGamesCountsTheLetterAndPepiPraises()
        {
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
            SaveManager.Data.wordTutorialDone = true;
            SaveManager.Data.selectedPack = WordPack.DefaultPack;
            SaveManager.Data.selectedLevel = 0;
            SaveManager.Data.pepiWelcomed.Add("World1:en");
            AdventureSession.Clear();
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            Object.FindFirstObjectByType<LevelResultUi>().OpenMiniGames();
            MiniGameUi ui = MiniGameUi.Instance;
            ui.Play(MiniGames.Kind.Trace);
            Assert.IsNotNull(ui.Trace);
            Assert.IsNotNull(ui.Surface);

            foreach (Vector2 point in InsidePoints(ui.Trace.LetterId))
            {
                ui.TraceAt(point);
                ui.Surface.Lift();
            }

            Assert.AreEqual(1, ui.TracedLetters, "A fully traced letter was not counted.");
            ui.Close();
        }

        private static TracingGame NewGame(string id)
        {
            return new TracingGame(id, TracingLetters.MaskOf(id), TracingLetters.Grid);
        }

        /// <summary>The centre of every cell the letter covers, as a finger would pass over it.</summary>
        private static List<Vector2> InsidePoints(string id)
        {
            bool[] mask = TracingLetters.MaskOf(id);
            int grid = TracingLetters.Grid;
            var points = new List<Vector2>();

            for (int y = 0; y < grid; y++)
            {
                for (int x = 0; x < grid; x++)
                {
                    if (mask[y * grid + x])
                    {
                        points.Add(new Vector2((x + 0.5f) / grid, 1f - (y + 0.5f) / grid));
                    }
                }
            }

            return points;
        }
    }
}
