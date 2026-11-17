using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;

namespace FootballRunner.Tests
{
    /// <summary>Swedish menus and Pepi for children in Sweden who learn Bosnian.</summary>
    public class SwedishTests
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
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void SwedishIsAMenuLanguageForBosnianLearners()
        {
            LearningProgress.SetLanguage(LearningProgress.Bosnian, LearningProgress.German);
            LearningProgress.ToggleLanguage();
            Assert.AreEqual(LearningProgress.Bosnian, LearningProgress.Language);
            Assert.AreEqual(LearningProgress.Swedish, LearningProgress.HomeLanguage, "The button goes German, then Swedish.");

            Assert.AreEqual("TILLBAKA", LearningProgress.Ui("NAZAD", "BACK", "ZURÜCK"));
            Assert.AreEqual("Mormors trädgård", WordPack.Load("World1").Title(LearningProgress.HomeLanguage));
            Assert.AreEqual("Kropp och kläder", WordPack.Load("World11").Title(LearningProgress.HomeLanguage));

            // Swedish is never learned: learning English still has Bosnian menus.
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Swedish);
            Assert.AreEqual(LearningProgress.Bosnian, LearningProgress.HomeLanguage);
        }

        [Test]
        public void NumbersAndNamesAreFilledIntoTheSwedish()
        {
            Assert.AreEqual("Fånga 8 ord", Swedish.Translate("Catch 8 words"));
            Assert.AreEqual("Barn 2", Swedish.Translate("Child 2"));
            Assert.AreEqual("Den här veckan: 12 min   ·   Idag: 3 min   ·   Svåra ord: lemon",
                Swedish.Translate("This week: 12 min   ·   Today: 3 min   ·   Hard words: lemon"));
            Assert.AreEqual("Something new", Swedish.Translate("Something new"), "Unknown texts stay in English.");
        }

        [Test]
        public void PepiSpeaksSwedish()
        {
            LearningProgress.SetLanguage(LearningProgress.Bosnian, LearningProgress.Swedish);
            Assert.AreEqual("Dags för en paus! Vi ses sen.", PepiVoice.Text("pepi_rest"));

            foreach (string id in new[] { "pepi_rest", "pepi_word_of_day", "story_intro_World1", "pepi_world_World11" })
            {
                Assert.IsNotNull(PepiVoice.Line(id), id);
            }

            foreach (string pack in WordPack.PackOrder)
            {
                Assert.IsNotNull(WordPack.Load(pack).Voice(LearningProgress.Swedish, "intro"), $"{pack} has no Swedish intro.");
            }
        }

        /// <summary>Every English menu text written in the code has its Swedish in the table.</summary>
        [Test]
        public void EveryMenuTextInTheCodeHasSwedish()
        {
            var call = new Regex(@"\bUi\(\s*\$?""(?:[^""\\]|\\.)*""\s*,\s*(\$?)""((?:[^""\\]|\\.)*)""");
            var hole = new Regex(@"\{[^{}]*\}");
            var missing = new List<string>();

            foreach (string path in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts"), "*.cs",
                         SearchOption.AllDirectories))
            {
                foreach (Match match in call.Matches(File.ReadAllText(path)))
                {
                    string english = Regex.Unescape(match.Groups[2].Value);

                    if (match.Groups[1].Value == "$")
                    {
                        int number = 0;
                        english = hole.Replace(english, _ => "{" + number++ + "}");
                    }

                    if (!Swedish.Knows(english))
                    {
                        missing.Add($"{Path.GetFileName(path)}: {english}");
                    }
                }
            }

            CollectionAssert.IsEmpty(missing, "No Swedish for:\n" + string.Join("\n", missing));
        }
    }
}
