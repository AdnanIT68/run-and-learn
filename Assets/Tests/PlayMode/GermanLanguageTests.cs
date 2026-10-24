using System.Collections;
using System.Linq;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>German as a language to learn, and as the menu language for children learning Bosnian.</summary>
    [Timeout(60000)]
    public class GermanLanguageTests
    {
        private string _saveBefore;

        [SetUp]
        public void KeepSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            FootballRunner.UI.DailyUi.AutoOpen = true;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void EveryWorldSpeaksGerman()
        {
            foreach (string name in WordPack.PackOrder)
            {
                WordPack pack = WordPack.Load(name);
                Assert.IsFalse(string.IsNullOrEmpty(pack.Title(LearningProgress.German)), $"{name} has no German title.");
                Assert.AreNotEqual(pack.Title(LearningProgress.English), pack.Title(LearningProgress.German));

                var entries = pack.Words.Concat(pack.Counts).Concat(pack.Commands).Concat(pack.Sizes)
                    .Concat(pack.Sentences).Concat(pack.Numbers);

                foreach (WordEntry entry in entries)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(entry.de), $"{name}/{entry.id} has no German text.");
                    Assert.IsNotNull(pack.Voice(LearningProgress.German, entry.id), $"{name}/{entry.id} has no German voice.");
                }

                foreach (string phrase in new[] { "praise_1", "praise_2", "praise_3", "intro", "jump", "goal" })
                {
                    Assert.IsNotNull(pack.Voice(LearningProgress.German, phrase), $"{name} has no German {phrase}.");
                }
            }
        }

        [Test]
        public void GermanNounsCarryTheirArticleButLettersSkipIt()
        {
            WordPack garden = WordPack.Load("World1");
            Assert.AreEqual("der Apfel", garden.Words.First(w => w.id == "apple").Text(LearningProgress.German));
            Assert.AreEqual("A", WordEntry.Initial("der Apfel"));
            Assert.AreEqual("S", WordEntry.Initial("die Schule"));
            Assert.AreEqual("D", WordEntry.Initial("dedo"), "Bosnian words must not lose letters.");

            WordPack school = WordPack.Load("World7");
            foreach (WordEntry word in school.Words)
            {
                string first = WordEntry.Initial(word.Text(LearningProgress.German));
                WordEntry letter = school.Letters.FirstOrDefault(l => l.lang == LearningProgress.German && l.letter == first);
                Assert.IsNotNull(letter, $"No German letter {first} for {word.id}.");
                Assert.IsNotNull(school.Voice(LearningProgress.German, letter.id));
            }
        }

        [Test]
        public void LanguageButtonStepsThroughEveryWayToPlay()
        {
            LearningProgress.SetLanguage(LearningProgress.Bosnian, LearningProgress.English);
            var seen = new System.Collections.Generic.List<(string, string)>();

            for (int i = 0; i < LearningProgress.Pairs.Length; i++)
            {
                seen.Add((LearningProgress.Language, LearningProgress.HomeLanguage));
                LearningProgress.ToggleLanguage();
            }

            CollectionAssert.AreEquivalent(LearningProgress.Pairs.Select(p => (p.learn, p.home)), seen);
            Assert.AreEqual(LearningProgress.Bosnian, LearningProgress.Language, "The button should come back round.");
            Assert.AreEqual(LearningProgress.English, LearningProgress.HomeLanguage);

            LearningProgress.SetLanguage(LearningProgress.German);
            Assert.AreEqual(LearningProgress.Bosnian, LearningProgress.HomeLanguage, "German learners read Bosnian menus.");
            Assert.AreEqual("MENI", LearningProgress.Ui("MENI", "MENU", "MENÜ"));

            LearningProgress.SetLanguage(LearningProgress.Bosnian, LearningProgress.German);
            Assert.AreEqual("MENÜ", LearningProgress.Ui("MENI", "MENU", "MENÜ"));
        }

        [UnityTest]
        public IEnumerator LearningGermanAsksGermanWords()
        {
            SaveData data = SaveManager.Data;
            data.wordTutorialDone = true;
            data.selectedPack = "World1";
            data.selectedLevel = 0;
            LearningProgress.SetLanguage(LearningProgress.German);

            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            yield return null;

            WordChallengeDirector director = WordChallengeDirector.Instance;
            float waited = 0f;
            while (director.CurrentWord == null && waited < 5f)
            {
                director.ForceChallenge(WordChallengeDirector.Kind.Word);
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(director.CurrentWord);
            yield return null;
            TMP_Text prompt = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == "Word" && t.transform.parent.name == "WordPrompt");
            Assert.IsNotNull(prompt);
            Assert.AreEqual(director.CurrentWord.de, prompt.text);
            StringAssert.IsMatch("^(der|die|das) ", prompt.text);
        }

        [UnityTest]
        public IEnumerator BosnianLearnersInGermanyGetGermanMenus()
        {
            FootballRunner.UI.DailyUi.AutoOpen = false;
            LearningProgress.SetLanguage(LearningProgress.Bosnian, LearningProgress.German);

            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;

            string[] labels = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Select(t => t.text).ToArray();
            Assert.Contains("SPIELEN", labels, "PLAY is not in German.");
            Assert.IsTrue(labels.Any(l => l.StartsWith("ICH LERNE BOSNISCH")), "The language button is not in German.");
        }
    }
}
