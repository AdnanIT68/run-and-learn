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
    /// <summary>A diploma with the child's name for every finished world, saved as a picture to share or print.</summary>
    [Timeout(60000)]
    public class DiplomaTests
    {
        private string _saveBefore;

        [SetUp]
        public void FreshResults()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordProgress.Clear();
            LearningProgress.SetLanguage(LearningProgress.English, LearningProgress.Bosnian);
        }

        [TearDown]
        public void Restore()
        {
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        private static void FinishWorld(string pack)
        {
            for (int level = 0; level < LearningProgress.LevelsPerWorld; level++)
            {
                LearningProgress.RecordLevelComplete(pack, level, 3);
            }
        }

        [Test]
        public void ADiplomaComesOnlyWithAllFiveLevels()
        {
            for (int level = 0; level < LearningProgress.LevelsPerWorld - 1; level++)
            {
                LearningProgress.RecordLevelComplete("World1", level, 2);
            }

            Assert.IsEmpty(Diplomas.Earned());
            LearningProgress.RecordLevelComplete("World1", LearningProgress.LevelsPerWorld - 1, 1);
            CollectionAssert.AreEqual(new[] { "World1" }, Diplomas.Earned());

            LearningProgress.SetLanguage(LearningProgress.Arabic, LearningProgress.Bosnian);
            Assert.IsEmpty(Diplomas.Earned(), "A diploma in English is not one in Arabic.");
        }

        [UnityTest]
        public IEnumerator TheDiplomaIsAPictureSavedAsAFile()
        {
            FinishWorld("World1");
            yield return null;

            Texture2D picture = Diplomas.Render("World1", "Amina");
            Assert.AreEqual(Diplomas.Width, picture.width);
            Assert.AreEqual(Diplomas.Height, picture.height);

            // Not blank: the frame, the text and the pictures change the colours across it.
            Color corner = picture.GetPixel(10, 10);
            Color frame = picture.GetPixel(Diplomas.Width / 2, 40);
            Color middle = picture.GetPixel(Diplomas.Width / 2, Diplomas.Height / 2);
            Assert.AreNotEqual(corner, frame, "The gold frame is missing.");
            Object.Destroy(picture);

            string path = Diplomas.Save("World1", "Amina");
            Assert.IsNotNull(path);
            Assert.IsTrue(File.Exists(path));
            StringAssert.Contains("Amina_World1_en", path);
            Assert.Greater(new FileInfo(path).Length, 20000, "The saved diploma is suspiciously small.");
            File.Delete(path);
            Assert.IsFalse(Diplomas.Share(path), "Sharing only works on a phone.");
            _ = middle;
        }

        [UnityTest]
        public IEnumerator ParentsSeeAndSaveDiplomasInTheCorner()
        {
            FinishWorld("World2");
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            ParentsCornerUi corner = Object.FindFirstObjectByType<ParentsCornerUi>();
            corner.Open();
            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));
            corner.OpenDiplomas();
            DiplomasUi diplomas = corner.Diplomas;
            Assert.IsTrue(diplomas.IsOpen);

            Assert.IsFalse(diplomas.Show("World1"), "A world not finished has no diploma.");
            Assert.IsTrue(diplomas.Show("World2"));
            diplomas.SaveAndShare();
            Assert.IsNotNull(diplomas.LastSaved);
            Assert.IsTrue(File.Exists(diplomas.LastSaved));
            File.Delete(diplomas.LastSaved);
            diplomas.Close();
            corner.Close();
        }
    }
}
