using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;

namespace FootballRunner.Tests
{
    /// <summary>Missed words come back often, known words rarely, separately for every learned language.</summary>
    public class SpacedRepetitionTests
    {
        private string _saveBefore;

        [SetUp]
        public void FreshProgress()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.askTick = 0;
            LearningProgress.SetLanguage(LearningProgress.Bosnian, LearningProgress.English);
        }

        [TearDown]
        public void Restore()
        {
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void RightAnswersMoveAWordUpAndAMissSendsItBack()
        {
            LearningProgress.RecordCorrect("apple");
            LearningProgress.RecordCorrect("apple");
            Assert.AreEqual(2, LearningProgress.Get("apple").box);

            LearningProgress.RecordMiss("apple");
            Assert.AreEqual(0, LearningProgress.Get("apple").box);

            for (int i = 0; i < 10; i++)
            {
                LearningProgress.RecordCorrect("apple");
            }

            Assert.AreEqual(LearningProgress.MaxBox, LearningProgress.Get("apple").box);
        }

        [Test]
        public void AMissedWordIsAskedFarMoreOftenThanAKnownOne()
        {
            LearningProgress.RecordMiss("pear");
            for (int i = 0; i < 4; i++)
            {
                LearningProgress.RecordCorrect("apple");
            }

            Assert.Greater(LearningProgress.Weight("pear"), 3f * LearningProgress.Weight("apple"));
        }

        [Test]
        public void OldMissesStopCountingOnceTheChildKnowsTheWord()
        {
            // Missed five times long ago, then right five times in a row.
            for (int i = 0; i < 5; i++)
            {
                LearningProgress.RecordMiss("lemon");
            }

            for (int i = 0; i < 5; i++)
            {
                LearningProgress.RecordCorrect("lemon");
            }

            LearningProgress.RecordMiss("pear");
            Assert.Less(LearningProgress.Weight("lemon"), LearningProgress.Weight("pear"));
            Assert.AreEqual(5, LearningProgress.Get("lemon").missed, "The miss count is kept for the parents' report.");
        }

        [Test]
        public void AWordThatWaitedPastItsGapIsDue()
        {
            LearningProgress.RecordCorrect("apple");
            LearningProgress.RecordAsked("apple");
            float justAsked = LearningProgress.Weight("apple");

            for (int i = 0; i < 5; i++)
            {
                LearningProgress.RecordAsked("other_" + i);
            }

            Assert.Greater(LearningProgress.Weight("apple"), justAsked);
        }

        [Test]
        public void EachLearnedLanguageKeepsItsOwnBoxes()
        {
            LearningProgress.RecordMiss("apple");
            LearningProgress.SetLanguage(LearningProgress.Arabic, LearningProgress.Bosnian);
            Assert.AreEqual(0, LearningProgress.Get("apple").missed, "A miss in Bosnian leaked into Arabic.");

            for (int i = 0; i < 3; i++)
            {
                LearningProgress.RecordCorrect("apple");
            }

            LearningProgress.SetLanguage(LearningProgress.Bosnian, LearningProgress.English);
            Assert.AreEqual(0, LearningProgress.Get("apple").box);
            Assert.AreEqual(1, LearningProgress.Get("apple").missed);
        }

        [Test]
        public void SavesFromBeforeBoxesGetABoxFromTheirHistory()
        {
            SaveManager.Data.wordProgress.Add(new WordProgress
            {
                id = "goat", language = LearningProgress.Bosnian, correct = 4, missed = 1
            });

            LearningProgress.Weight("goat");
            WordProgress goat = LearningProgress.Get("goat");
            Assert.IsTrue(goat.spaced);
            Assert.AreEqual(2, goat.box, "Four right and one miss start in box 2.");
        }
    }
}
