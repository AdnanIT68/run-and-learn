using System.Collections;
using System.IO;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using FootballRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>Worksheets to print from a world's words: letters to trace and pictures to join with their words.</summary>
    [Timeout(60000)]
    public class WorksheetTests
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
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [TestCase("en")]
        [TestCase("bs")]
        [TestCase("ar")]
        public void TheLetterSheetTakesTheLettersTheWorldsWordsStartWith(string language)
        {
            WordPack garden = WordPack.Load("World1");
            var rows = Worksheets.LettersFor(garden, language);

            Assert.That(rows.Count, Is.InRange(1, Worksheets.LetterRows));
            foreach ((TracingLetters.Letter letter, WordEntry word) in rows)
            {
                Assert.IsNotNull(TracingLetters.Guide(letter.id), letter.id);
                Assert.IsNotNull(word, $"No word starts with {letter.letter}.");
                Assert.AreEqual(letter.letter, WordEntry.Initial(word.Text(language)));
            }
        }

        [UnityTest]
        public IEnumerator BothSheetsArePrintablePicturesSavedAsFiles()
        {
            yield return null;

            foreach (Worksheets.Kind kind in new[] { Worksheets.Kind.Letters, Worksheets.Kind.Match })
            {
                Texture2D sheet = Worksheets.Render("World2", kind);
                Assert.AreEqual(Worksheets.Width, sheet.width);
                Assert.AreEqual(Worksheets.Height, sheet.height);
                Assert.AreEqual(Color.white, sheet.GetPixel(5, 5), "A worksheet should be white for printing.");
                Assert.AreNotEqual(sheet.GetPixel(5, 5), sheet.GetPixel(Worksheets.Width / 2, Worksheets.Height - 10),
                    "The top band is missing.");
                Object.Destroy(sheet);

                string path = Worksheets.Save("World2", kind);
                Assert.IsTrue(File.Exists(path));
                StringAssert.Contains("World2_" + kind.ToString().ToLowerInvariant() + "_en", path);
                Assert.Greater(new FileInfo(path).Length, 20000, "The saved sheet is suspiciously small.");
                File.Delete(path);
            }
        }

        [UnityTest]
        public IEnumerator ParentsPrintAWorksheetForAnyWorldFromTheCorner()
        {
            SaveManager.Data.worldResults.Clear();
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            ParentsCornerUi corner = Object.FindFirstObjectByType<ParentsCornerUi>();
            corner.Open();
            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));
            corner.OpenDiplomas();
            DiplomasUi print = corner.Diplomas;
            Assert.AreEqual(DiplomasUi.Mode.Diplomas, print.Current);
            Assert.IsFalse(print.Show("World10"), "No diploma without finishing the world.");

            print.ShowMode(DiplomasUi.Mode.Worksheets);
            Assert.IsTrue(print.Show("World10"), "Worksheets need no finished world.");
            print.ShowSheetKind(Worksheets.Kind.Match);
            Assert.AreEqual("World10", print.Shown, "Changing the sheet keeps the world.");

            print.SaveAndShare();
            Assert.IsNotNull(print.LastSaved);
            StringAssert.Contains("World10_match", print.LastSaved);
            Assert.IsTrue(File.Exists(print.LastSaved));
            File.Delete(print.LastSaved);
            print.Close();
            corner.Close();
        }
    }
}
