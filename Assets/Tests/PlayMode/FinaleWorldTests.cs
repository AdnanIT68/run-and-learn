using System.Collections;
using System.Linq;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>World 8, Night Sky: the finale that revisits everything from the worlds before it.</summary>
    [Timeout(60000)]
    public class FinaleWorldTests
    {
        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator OpenNightSky()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;

            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.worldResults.Clear();
            data.wordTutorialDone = true;
            data.learningLanguage = LearningProgress.Bosnian;
            LearningProgress.RecordLevelComplete("World7", LearningProgress.LevelToOpenNextWorld - 1, 2);
            Assert.IsTrue(LearningProgress.SelectPack("World8"), "Finishing School should open the night sky.");

            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void FinaleBorrowsEveryEarlierChallengeWithPicturesAndVoices()
        {
            WordPack finale = WordPack.Load("World8");

            Assert.IsTrue(finale.IsReview);
            Assert.AreEqual(8, finale.GoalSize);
            Assert.AreEqual(10, finale.Words.Count, "The album keeps only the night words.");
            Assert.Greater(finale.Counts.Count, 0);
            Assert.Greater(finale.Commands.Count, 0);
            Assert.Greater(finale.Sizes.Count, 0);
            Assert.Greater(finale.Sentences.Count, 0);
            Assert.Greater(finale.Letters.Count, 0);
            Assert.Greater(finale.Numbers.Count, 0);
            Assert.Greater(finale.ChallengeWords.Count, finale.Words.Count);

            var borrowed = finale.Counts.Concat(finale.Commands).Concat(finale.Sizes)
                .Concat(finale.Sentences).Concat(finale.Numbers).Concat(finale.ChallengeWords);

            foreach (WordEntry entry in borrowed)
            {
                Assert.IsNotNull(finale.Picture(entry.PictureId), $"{entry.id} has no picture in the finale.");
                Assert.IsNotNull(finale.Voice(LearningProgress.Bosnian, entry.id), $"{entry.id} has no Bosnian voice.");
                Assert.IsNotNull(finale.Voice(LearningProgress.English, entry.id), $"{entry.id} has no English voice.");
            }

            foreach (WordEntry letter in finale.Letters)
            {
                Assert.IsNotNull(finale.Voice(letter.lang, letter.id), $"Letter {letter.letter} has no voice.");
            }

            Assert.IsNotNull(finale.Voice(LearningProgress.Bosnian, "finale"));
            Assert.IsNull(LearningProgress.NextPack("World8"), "The night sky is the last world.");
        }

        [Test]
        public void EarlierWorldsStayAsTheyWere()
        {
            WordPack garden = WordPack.Load("World1");

            Assert.IsFalse(garden.IsReview);
            Assert.AreEqual(0, garden.Counts.Count, "Only the finale merges other worlds' challenges.");
            Assert.AreSame(garden.Words, garden.ChallengeWords);
        }

        [UnityTest]
        public IEnumerator FinaleAsksEightWordsAndEveryKind()
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;
            Assert.AreEqual(8, director.GoalSize);

            WordChallengeDirector.Kind[] kinds =
            {
                WordChallengeDirector.Kind.Word, WordChallengeDirector.Kind.Counting,
                WordChallengeDirector.Kind.Command, WordChallengeDirector.Kind.Size,
                WordChallengeDirector.Kind.Sentence, WordChallengeDirector.Kind.Letter,
                WordChallengeDirector.Kind.Number, WordChallengeDirector.Kind.Reading
            };

            foreach (WordChallengeDirector.Kind kind in kinds)
            {
                float waited = 0f;

                while (director.CurrentWord == null && waited < 5f)
                {
                    director.ForceChallenge(kind);
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                Assert.IsNotNull(director.CurrentWord, $"No {kind} challenge started in the finale.");
                Assert.AreEqual(kind, director.CurrentKind);

                foreach (WordBubble bubble in director.Bubbles)
                {
                    Assert.IsNotNull(director.Pack.Picture(bubble.Word.PictureId),
                        $"{kind} bubble {bubble.Word.id} has no picture.");
                }

                director.CancelChallenge();
                yield return null;
            }
        }
    }
}
