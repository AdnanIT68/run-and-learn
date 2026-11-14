using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using FootballRunner.UI;
using FootballRunner.Utilities;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>
    /// Arabic as a language to learn from Bosnian: words and voice in Arabic, menus in
    /// Bosnian, joined right-to-left text and a Latin transliteration parents can hide.
    /// </summary>
    [Timeout(120000)]
    public class ArabicLanguageTests
    {
        private string _saveBefore;

        [SetUp]
        public void KeepSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
        }

        [TearDown]
        public void RestoreSave()
        {
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void ArabicIsLearnedFromBosnianOnly()
        {
            var arabicPairs = LearningProgress.Pairs.Where(p => p.learn == LearningProgress.Arabic).ToList();
            Assert.AreEqual(1, arabicPairs.Count);
            Assert.AreEqual(LearningProgress.Bosnian, arabicPairs[0].home);

            LearningProgress.SetLanguage(LearningProgress.Arabic, LearningProgress.Bosnian);
            Assert.AreEqual(LearningProgress.Arabic, LearningProgress.Language);
            Assert.AreEqual(LearningProgress.Bosnian, LearningProgress.HomeLanguage);
            Assert.IsTrue(LearningProgress.IsArabic);
            Assert.AreEqual("bs", LearningProgress.Ui("bs", "en", "de"), "Menus stay in Bosnian.");
        }

        [Test]
        public void TheLanguageButtonReachesArabicAndWrapsAround()
        {
            LearningProgress.SetLanguage(LearningProgress.German, LearningProgress.Bosnian);
            LearningProgress.ToggleLanguage();
            Assert.AreEqual(LearningProgress.Arabic, LearningProgress.Language);

            LearningProgress.ToggleLanguage();
            Assert.AreEqual(LearningProgress.Bosnian, LearningProgress.Language);
            Assert.AreEqual(LearningProgress.English, LearningProgress.HomeLanguage);
        }

        [Test]
        public void TransliterationIsOnForNewPlayers()
        {
            Assert.IsTrue(new SaveData().showTransliteration);
        }

        [Test]
        public void ArabicWordsShowShapedTextWithLatinUnderneath()
        {
            var word = new WordEntry { id = "apple", bs = "jabuka", ar = "تُفَّاحَة", ar_shown = "ﺔﺣَﺎﻔَّﺗُ", ar_latin = "tuffâha" };

            Assert.AreEqual("تُفَّاحَة", word.Text(LearningProgress.Arabic), "Voices and letters use reading order.");
            Assert.AreEqual("ﺔﺣَﺎﻔَّﺗُ", word.Display(LearningProgress.Arabic), "The screen gets the joined form.");
            Assert.AreEqual("tuffâha", word.Latin(LearningProgress.Arabic));
            Assert.IsNull(word.Latin(LearningProgress.Bosnian));

            string withLatin = word.CardText(LearningProgress.Arabic, true);
            StringAssert.StartsWith("ﺔﺣَﺎﻔَّﺗُ", withLatin);
            StringAssert.Contains("tuffâha", withLatin);
            Assert.AreEqual("ﺔﺣَﺎﻔَّﺗُ", word.CardText(LearningProgress.Arabic, false));
            Assert.AreEqual("jabuka", word.CardText(LearningProgress.Bosnian, true), "Other languages never get a second line.");
        }

        [TestCase("أُمّ", "ا")]
        [TestCase("إِبْرِيق", "ا")]
        [TestCase("قَلَمُ رَصَاص", "ق")]
        [TestCase("ثَلْج", "ث")]
        [TestCase("jabuka", "J")]
        public void FirstLetterSkipsHarakatAndTreatsHamzaAsAlif(string word, string expected)
        {
            Assert.AreEqual(expected, WordEntry.Initial(word));
        }

        [Test]
        public void EveryEntryInEveryWorldHasArabicAndAVoice()
        {
            int checkedEntries = 0;

            foreach (string packName in WordPack.PackOrder)
            {
                WordPack pack = WordPack.Load(packName);
                var entries = pack.Words.Concat(pack.Phrases).Concat(pack.Counts).Concat(pack.Commands)
                    .Concat(pack.Sizes).Concat(pack.Sentences).Concat(pack.Numbers);

                foreach (WordEntry entry in entries)
                {
                    string where = $"{packName}/{entry.id}";
                    Assert.IsFalse(string.IsNullOrEmpty(entry.ar), where + " has no Arabic");
                    Assert.IsTrue(WordEntry.IsArabicText(entry.ar), where + " Arabic is not in Arabic script");
                    Assert.IsFalse(string.IsNullOrEmpty(entry.ar_latin), where + " has no transliteration");
                    AssertShaped(entry.ar_shown, where);
                    Assert.IsNotNull(pack.Voice(LearningProgress.Arabic, entry.id), where + " has no Arabic voice");
                    checkedEntries++;
                }
            }

            Assert.GreaterOrEqual(checkedEntries, 180);
        }

        [Test]
        public void SchoolWorldHasArabicLettersForItsWords()
        {
            WordPack school = WordPack.Load("World7");
            List<WordEntry> letters = school.Letters.Where(l => l.lang == LearningProgress.Arabic).ToList();
            Assert.Greater(letters.Count, 2, "World 7 has no Arabic letters.");

            var initials = new HashSet<string>(school.Words.Select(w => WordEntry.Initial(w.Text(LearningProgress.Arabic))));

            foreach (WordEntry letter in letters)
            {
                Assert.IsTrue(initials.Contains(letter.letter), $"{letter.id} starts none of the words.");
                Assert.IsNotNull(school.Voice(LearningProgress.Arabic, letter.id), $"{letter.id} has no voice.");
            }

            foreach (string initial in initials)
            {
                Assert.IsTrue(letters.Any(l => l.letter == initial), $"No letter entry for {initial}.");
            }
        }

        [Test]
        public void TheArabicFontHasEveryLetterTheGameShows()
        {
            TMP_FontAsset arabic = GameFonts.Arabic;
            Assert.IsNotNull(arabic, "Resources/Fonts/NotoSansArabic-Bold is missing.");
            Assert.IsTrue(GameFonts.Display.fallbackFontAssetTable.Contains(arabic), "Titan One has no Arabic fallback.");

            var shown = new HashSet<char>();
            foreach (string packName in WordPack.PackOrder)
            {
                WordPack pack = WordPack.Load(packName);
                var entries = pack.Words.Concat(pack.Phrases).Concat(pack.Counts).Concat(pack.Commands)
                    .Concat(pack.Sizes).Concat(pack.Sentences).Concat(pack.Numbers)
                    .Concat(pack.Letters.Where(l => l.lang == LearningProgress.Arabic));

                foreach (WordEntry entry in entries)
                {
                    foreach (char c in entry.ar_shown ?? string.Empty)
                    {
                        if (!char.IsWhiteSpace(c))
                        {
                            shown.Add(c);
                        }
                    }
                }
            }

            bool all = arabic.TryAddCharacters(new string(shown.ToArray()), out string missing);
            Assert.IsTrue(all, "Glyphs missing from the Arabic font: " + missing);
        }

        [UnityTest]
        public IEnumerator ParentsCornerShowsTheLatinSwitchOnlyForArabic()
        {
            SceneManager.LoadScene("MainMenu");
            yield return null;
            yield return null;
            ParentsCornerUi corner = Object.FindFirstObjectByType<ParentsCornerUi>();

            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
            corner.Open();
            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));
            Assert.IsFalse(corner.IsTransliterationSwitchShowing, "The switch shows for English.");
            corner.Close();

            LearningProgress.SetLanguage(LearningProgress.Arabic, LearningProgress.Bosnian);
            LearningProgress.ShowTransliteration = true;
            corner.Open();
            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));
            Assert.IsTrue(corner.IsTransliterationSwitchShowing, "The switch is missing for Arabic.");
            StringAssert.Contains("LATINICA", corner.TransliterationLabel);
            StringAssert.Contains("arapski", corner.Summary);

            corner.ToggleTransliteration();
            Assert.IsFalse(LearningProgress.ShowTransliteration);
            StringAssert.Contains("SAMO ARAPSKO", corner.TransliterationLabel);
            corner.ToggleTransliteration();
            Assert.IsTrue(LearningProgress.ShowTransliteration);
            corner.Close();
        }

        /// <summary>Drawn text must use joined presentation forms, never bare base letters.</summary>
        private static void AssertShaped(string shown, string where)
        {
            Assert.IsFalse(string.IsNullOrEmpty(shown), where + " has no shaped text");

            foreach (char c in shown)
            {
                bool baseLetter = c >= 'ء' && c <= 'ي';
                Assert.IsFalse(baseLetter, $"{where}: '{c}' was not joined (U+{(int)c:X4}).");
            }
        }
    }
}
