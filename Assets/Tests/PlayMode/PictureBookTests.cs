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
    /// <summary>A narrated picture book in every world, opened from the world map.</summary>
    [Timeout(60000)]
    public class PictureBookTests
    {
        private static readonly string[] Languages = { "bs", "en", "de", "ar" };
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

        [Test]
        public void EveryWorldHasFourNarratedPagesWithItsOwnWords()
        {
            foreach (string pack in WordPack.PackOrder)
            {
                PictureBook book = PictureBook.Load(pack);
                Assert.IsNotNull(book, $"{pack} has no picture book.");
                Assert.AreEqual(4, book.Pages.Count, pack);
                WordPack words = WordPack.Load(pack);

                for (int p = 0; p < book.Pages.Count; p++)
                {
                    PictureBook.Page page = book.Pages[p];
                    Assert.IsNotEmpty(page.Words, $"{pack} page {p + 1} draws no word.");

                    foreach (string language in Languages)
                    {
                        Assert.IsNotEmpty(page.Text.Text(language), $"{pack} page {p + 1} has no {language} text.");
                        Assert.IsNotNull(book.Voice(language, p), $"{pack} page {p + 1} is not read in {language}.");
                    }

                    foreach (string id in page.Words)
                    {
                        Assert.IsTrue(words.Words.Any(w => w.id == id), $"{id} on {pack} page {p + 1} is not a word of the world.");
                        Assert.IsNotNull(words.Picture(id), id);
                    }
                }
            }
        }

        [Test]
        public void ArabicPagesShowOneSentencePerLine()
        {
            PictureBook book = PictureBook.Load("World1");
            WordEntry text = book.Pages[0].Text;
            int sentences = text.ar.Count(c => c == '.' || c == '!' || c == '?');
            Assert.AreEqual(sentences, text.Display(LearningProgress.Arabic).Split('\n').Length,
                "A wrapped Arabic paragraph would read in the wrong order.");
        }

        [UnityTest]
        public IEnumerator TheWorldMapOpensTheBookAndPagesTurn()
        {
            Assert.IsTrue(LearningProgress.SelectPack("World10"), "The colours world is open from the start.");
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            WordWorldsUi worlds = Object.FindFirstObjectByType<WordWorldsUi>();
            worlds.Open();
            Assert.IsTrue(worlds.OpenBook());
            PictureBookUi book = Object.FindFirstObjectByType<PictureBookUi>();
            Assert.IsTrue(book.IsOpen);
            Assert.AreEqual(0, book.PageIndex);
            StringAssert.Contains("red circle", book.ShownText.ToLowerInvariant());
            Assert.AreEqual("red", book.TapWord(0));

            book.Next();
            book.Next();
            book.Previous();
            Assert.AreEqual(1, book.PageIndex);
            book.Next();
            book.Next();
            Assert.AreEqual(3, book.PageIndex);
            book.Next();
            Assert.IsFalse(book.IsOpen, "The last page's arrow closes the book.");
            Assert.IsFalse(FootballRunner.Audio.AudioManager.Instance != null &&
                           FootballRunner.Audio.AudioManager.Instance.IsNarrating, "The page kept talking after the book closed.");
            worlds.Close();
        }
    }
}
