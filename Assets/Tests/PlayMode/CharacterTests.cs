using System.Collections;
using System.Linq;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using FootballRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>The team of ten kids: three open, the rest bought with coins, and the runner wears the chosen one.</summary>
    [Timeout(60000)]
    public class CharacterTests
    {
        private static readonly string[] Frames =
            { "Idle", "Run_01", "Run_02", "Run_03", "Run_04", "Run_05", "Jump_Up", "Fall_Air", "Hit" };

        private string _saveBefore;

        [SetUp]
        public void KeepSave()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;
            SaveManager.Data.unlockedItems.Clear();
            SaveManager.Data.selectedCharacterId = CharacterRoster.DefaultId;
            SaveManager.Data.totalCoins = 0;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void TenKidsThreeOpenEveryFrameDrawn()
        {
            Assert.AreEqual(10, CharacterRoster.All.Length);
            Assert.AreEqual(3, CharacterRoster.All.Count(e => e.IsFree));
            Assert.AreEqual(CharacterRoster.All.Length, CharacterRoster.All.Select(e => e.Id).Distinct().Count());

            foreach (CharacterRoster.Entry kid in CharacterRoster.All)
            {
                Assert.IsNotNull(CharacterRoster.Frame(kid.Id, "Idle"), $"{kid.Name} has no standing picture.");
                Assert.IsNotNull(CharacterRoster.Portrait(kid.Id), $"{kid.Name} has no portrait.");

                if (kid.Id == CharacterRoster.DefaultId)
                {
                    continue;
                }

                foreach (string frame in Frames)
                {
                    Assert.IsNotNull(CharacterRoster.Frame(kid.Id, frame), $"{kid.Name} has no {frame} frame.");
                }
            }
        }

        [Test]
        public void LockedKidsCostCoinsAndStayBought()
        {
            CharacterRoster.Entry harun = CharacterRoster.Find("harun");

            Assert.IsFalse(CharacterRoster.IsUnlocked("harun"));
            Assert.IsFalse(CharacterRoster.Select("harun"), "A locked kid was chosen for free.");
            Assert.IsFalse(CharacterRoster.Unlock("harun"), "A kid was bought without coins.");
            Assert.AreEqual(CharacterRoster.DefaultId, CharacterRoster.SelectedId);

            SaveManager.Data.totalCoins = harun.Price + 5;
            Assert.IsTrue(CharacterRoster.Unlock("harun"));
            Assert.AreEqual(5, SaveManager.Data.totalCoins);
            Assert.AreEqual("harun", CharacterRoster.SelectedId);

            // Choosing again later costs nothing.
            Assert.IsTrue(CharacterRoster.Select("amina"));
            Assert.IsTrue(CharacterRoster.Unlock("harun"));
            Assert.AreEqual(5, SaveManager.Data.totalCoins);
        }

        [UnityTest]
        public IEnumerator RunnerWearsTheChosenKid()
        {
            Assert.IsTrue(CharacterRoster.Select("amina"));
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);

            for (int i = 0; i < 20; i++)
            {
                yield return null;
            }

            var driver = Object.FindFirstObjectByType<RukajjaAnimDriver>();
            Assert.IsNotNull(driver);
            var renderer = driver.GetComponentInChildren<Animator>(true).GetComponent<SpriteRenderer>();
            Sprite shown = renderer.sprite;

            Assert.IsNotNull(shown);
            Assert.IsFalse(shown.name.StartsWith("Rukajja_"), $"The runner still shows {shown.name}.");
            Assert.IsTrue(Frames.Contains(shown.name), $"Unexpected frame {shown.name}.");
            Assert.AreSame(CharacterRoster.Frame("amina", shown.name), shown);

            // Drawn at twice Rukajja's pixel size with more headroom above the kid, so the
            // frame is a little taller than hers while the kid inside matches her height.
            Assert.AreEqual(112f, shown.pixelsPerUnit, 0.01f);
            Assert.AreEqual(Vector2.zero.y, shown.pivot.y, 0.5f, "Kids must stand on their feet like Rukajja.");
        }

        [UnityTest]
        public IEnumerator ChooserOnMenuBuysAndPicks()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            CharacterChooserUi chooser = Object.FindFirstObjectByType<CharacterChooserUi>();
            Assert.IsNotNull(chooser, "The main menu has no player chooser.");

            chooser.Open();
            Assert.IsTrue(chooser.IsOpen);
            Assert.AreEqual(10, chooser.CardCount);

            int harun = System.Array.FindIndex(CharacterRoster.All, e => e.Id == "harun");
            Assert.IsFalse(chooser.PressCard(harun), "A locked player was picked without coins.");

            SaveManager.Data.totalCoins = 100;
            Assert.IsTrue(chooser.PressCard(harun));
            Assert.AreEqual("harun", CharacterRoster.SelectedId);
            Assert.AreEqual(100 - CharacterRoster.Find("harun").Price, SaveManager.Data.totalCoins);

            Assert.IsTrue(chooser.PressCard(1), "An open player could not be picked.");
            Assert.AreEqual("emir", CharacterRoster.SelectedId);

            chooser.Close();
            Assert.IsFalse(chooser.IsOpen);
        }
    }
}
