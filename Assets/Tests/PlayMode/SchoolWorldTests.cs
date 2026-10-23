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
    /// <summary>World 7, School: letters, numbers one to ten and silent reading.</summary>
    [Timeout(60000)]
    public class SchoolWorldTests
    {
        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator OpenSchool()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;

            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.worldResults.Clear();
            data.wordTutorialDone = true;
            data.learningLanguage = LearningProgress.Bosnian;
            LearningProgress.RecordLevelComplete("World6", 1);
            Assert.IsTrue(LearningProgress.SelectPack("World7"), "Finishing the mountain should open School.");

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
        public void FirstLetterCountsBosnianDigraphsAsOne()
        {
            Assert.AreEqual("L", WordEntry.Initial("lenjir"));
            Assert.AreEqual("Lj", WordEntry.Initial("ljuljaška"));
            Assert.AreEqual("Nj", WordEntry.Initial("njuška"));
            Assert.AreEqual("Dž", WordEntry.Initial("džamija"));
            Assert.AreEqual("Š", WordEntry.Initial("škola"));
        }

        [Test]
        public void SchoolPackHasNumbersAndLettersInBothLanguages()
        {
            WordPack school = WordPack.Load("World7");

            Assert.AreEqual(10, school.Words.Count);
            Assert.AreEqual(10, school.Numbers.Count);
            Assert.IsTrue(school.HasReading);

            foreach (WordEntry entry in school.Words.Concat(school.Numbers))
            {
                Assert.IsNotNull(school.Picture(entry.PictureId), $"{entry.id} has no picture.");
                Assert.IsNotNull(school.Voice(LearningProgress.Bosnian, entry.id), $"{entry.id} has no Bosnian voice.");
                Assert.IsNotNull(school.Voice(LearningProgress.English, entry.id), $"{entry.id} has no English voice.");
            }

            foreach (string language in new[] { LearningProgress.Bosnian, LearningProgress.English })
            {
                foreach (WordEntry word in school.Words)
                {
                    string first = WordEntry.Initial(word.Text(language));
                    WordEntry letter = school.Letters.FirstOrDefault(l => l.lang == language && l.letter == first);
                    Assert.IsNotNull(letter, $"No {language} letter {first} for {word.id}.");
                    Assert.IsNotNull(school.Voice(language, letter.id), $"Letter {first} has no {language} voice.");
                }
            }
        }

        [UnityTest]
        public IEnumerator LetterChallengeRewardsThePictureThatStartsWithIt()
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;
            yield return Force(director, WordChallengeDirector.Kind.Letter);

            WordEntry target = director.CurrentWord;
            Assert.IsTrue(target.IsLetter, "School did not ask a letter.");

            WordPack school = director.Pack;
            foreach (WordBubble bubble in director.Bubbles)
            {
                WordEntry word = bubble.Word == target
                    ? school.Words.First(w => w.PictureId == target.PictureId)
                    : bubble.Word;
                string first = WordEntry.Initial(word.Text(LearningProgress.Language));

                if (bubble.Word == target)
                {
                    Assert.AreEqual(target.letter, first, "The right picture must start with the letter.");
                }
                else
                {
                    Assert.AreNotEqual(target.letter, first, "A wrong picture also starts with the letter.");
                }
            }
        }

        [UnityTest]
        public IEnumerator NumberChallengeOffersThreeDifferentNumerals()
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;
            yield return Force(director, WordChallengeDirector.Kind.Number);

            Assert.IsTrue(director.CurrentWord.id.StartsWith("num_"));
            Assert.AreEqual(3, director.Bubbles.Select(b => b.Word.PictureId).Distinct().Count());
            Assert.IsTrue(director.Bubbles.All(b => b.Word.PictureId.StartsWith("digit_")));
        }

        [UnityTest]
        public IEnumerator ReadingChallengeAsksAPlainWordSilently()
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;
            yield return Force(director, WordChallengeDirector.Kind.Reading);

            Assert.AreEqual(WordChallengeDirector.Kind.Reading, director.CurrentKind);
            Assert.IsTrue(director.Pack.Words.Contains(director.CurrentWord), "Reading should use the world's words.");
        }

        private static IEnumerator Force(WordChallengeDirector director, WordChallengeDirector.Kind kind)
        {
            float waited = 0f;

            while (director.CurrentWord == null && waited < 5f)
            {
                director.ForceChallenge(kind);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(director.CurrentWord, $"No {kind} challenge started.");
        }
    }
}
