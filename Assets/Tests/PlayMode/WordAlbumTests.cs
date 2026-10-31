using System.Collections;
using System.Linq;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FootballRunner.Tests
{
    /// <summary>
    /// The album is where a child sees what they have learned, so a caught word
    /// must show up there and speak when tapped, and an uncaught one must not.
    /// </summary>
    [Timeout(60000)]
    public class WordAlbumTests
    {
        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator LoadMainMenu()
        {
            Debug.Log("[TEST] " + TestContext.CurrentContext.Test.Name);
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;
        }

        [TearDown]
        public void RestoreSave()
        {
            // Tests change word progress; leave the developer's own save as it was.
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [Test]
        public void StarsGrowWithRepeatedCatches()
        {
            Assert.AreEqual(0, WordAlbumUi.StarsFor(0));
            Assert.AreEqual(1, WordAlbumUi.StarsFor(1));
            Assert.AreEqual(2, WordAlbumUi.StarsFor(3));
            Assert.AreEqual(3, WordAlbumUi.StarsFor(6));
        }

        [UnityTest]
        public IEnumerator MainMenuHasAnAlbumWithACardForEveryWord()
        {
            yield return null;

            WordAlbumUi album = Object.FindFirstObjectByType<WordAlbumUi>();
            Assert.IsNotNull(album, "The main menu has no album.");

            album.Open();

            Assert.IsTrue(album.IsOpen);
            Assert.AreEqual(WordPack.Load(WordPack.DefaultPack).Words.Count, album.CardCount);

            album.Close();
            Assert.IsFalse(album.IsOpen);
        }

        [Test]
        public void FarmPackIsCompleteAndHasNoPig()
        {
            WordPack farm = WordPack.Load("World2");

            Assert.IsNotNull(farm, "World2 pack.json did not load.");
            Assert.AreEqual(10, farm.Words.Count);

            foreach (WordEntry word in farm.Words)
            {
                Assert.AreNotEqual("pig", word.id, "Pigs do not belong in this game.");
                Assert.IsNotNull(farm.Picture(word.id), $"{word.id} has no picture.");
                Assert.IsNotNull(farm.Voice(LearningProgress.Bosnian, word.id), $"{word.id} has no Bosnian voice.");
                Assert.IsNotNull(farm.Voice(LearningProgress.English, word.id), $"{word.id} has no English voice.");
            }

            Assert.AreEqual(25, farm.Counts.Count, "Five animals counted one to five.");

            foreach (WordEntry phrase in farm.Counts)
            {
                Assert.IsTrue(phrase.Count >= 1 && phrase.Count <= 5, $"{phrase.id} counts outside one to five.");
                Assert.IsNotNull(farm.Picture(phrase.PictureId), $"{phrase.id} has no picture.");
                Assert.IsNotNull(farm.Voice(LearningProgress.Bosnian, phrase.id), $"{phrase.id} has no Bosnian voice.");
                Assert.IsNotNull(farm.Voice(LearningProgress.English, phrase.id), $"{phrase.id} has no English voice.");
            }

            // Bosnian number agreement is the point of this step; spot-check the tricky forms.
            Assert.IsTrue(System.Array.TrueForAll(new[] { "pet ovaca", "pet zečeva", "dvije kokoši" },
                form => FindCount(farm, form)), "A Bosnian counting form is missing or misspelled.");
        }

        [Test]
        public void HomeWorldIsCompleteAndOpensAfterTheFarm()
        {
            WordPack home = WordPack.Load("World3");

            Assert.IsNotNull(home, "World3 pack.json did not load.");
            Assert.AreEqual(10, home.Words.Count);

            foreach (WordEntry word in home.Words)
            {
                Assert.IsNotNull(home.Picture(word.id), $"{word.id} has no picture.");
                Assert.IsNotNull(home.Voice(LearningProgress.Bosnian, word.id), $"{word.id} has no Bosnian voice.");
                Assert.IsNotNull(home.Voice(LearningProgress.English, word.id), $"{word.id} has no English voice.");
            }

            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            WordPack farm = WordPack.Load("World2");
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World3"), "Our Home opened with no farm words learned.");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(farm.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.IsPackUnlocked("World3"), "Six farm words should open Our Home.");
        }

        [Test]
        public void BazaarIsCompleteWithCommandsAndOpensAfterHome()
        {
            WordPack bazaar = WordPack.Load("World4");

            Assert.IsNotNull(bazaar, "World4 pack.json did not load.");
            Assert.AreEqual(10, bazaar.Words.Count);
            Assert.AreEqual(3, bazaar.Commands.Count, "Jump, stay down and jump twice.");

            foreach (WordEntry entry in bazaar.Words.Concat(bazaar.Commands))
            {
                Assert.IsNotNull(bazaar.Picture(entry.PictureId), $"{entry.id} has no picture.");
                Assert.IsNotNull(bazaar.Voice(LearningProgress.Bosnian, entry.id), $"{entry.id} has no Bosnian voice.");
                Assert.IsNotNull(bazaar.Voice(LearningProgress.English, entry.id), $"{entry.id} has no English voice.");
            }

            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            WordPack home = WordPack.Load("World3");
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World4"), "The bazaar opened with no home words learned.");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(home.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.IsPackUnlocked("World4"), "Six home words should open the bazaar.");
        }

        [Test]
        public void RiverHasWordsAndAgreeingAdjectivesAndOpensAfterTheBazaar()
        {
            WordPack river = WordPack.Load("World5");

            Assert.IsNotNull(river, "World5 pack.json did not load.");
            Assert.AreEqual(10, river.Words.Count);
            Assert.AreEqual(10, river.Sizes.Count, "Five things, each big and small.");

            foreach (WordEntry entry in river.Words.Concat(river.Sizes))
            {
                Assert.IsNotNull(river.Picture(entry.PictureId), $"{entry.id} has no picture.");
                Assert.IsNotNull(river.Voice(LearningProgress.Bosnian, entry.id), $"{entry.id} has no Bosnian voice.");
                Assert.IsNotNull(river.Voice(LearningProgress.English, entry.id), $"{entry.id} has no English voice.");
            }

            // The adjective follows the noun's gender: the point of this world.
            string[] forms = { "velika riba", "veliki čamac", "veliko drvo", "mala žaba", "mali cvijet", "malo drvo" };
            foreach (string form in forms)
            {
                Assert.IsTrue(river.Sizes.Any(z => z.bs == form), $"Missing or misspelled: {form}");
            }

            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            WordPack bazaar = WordPack.Load("World4");
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World5"));

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(bazaar.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.IsPackUnlocked("World5"), "Six bazaar words should open the river.");
        }

        [Test]
        public void MountainHasWordsAndSentencesAndOpensAfterTheRiver()
        {
            WordPack mountain = WordPack.Load("World6");

            Assert.IsNotNull(mountain, "World6 pack.json did not load.");
            Assert.AreEqual(10, mountain.Words.Count);
            Assert.AreEqual(9, mountain.Sentences.Count);

            foreach (WordEntry entry in mountain.Words.Concat(mountain.Sentences))
            {
                Assert.IsNotNull(mountain.Picture(entry.PictureId), $"{entry.id} has no picture.");
                Assert.IsNotNull(mountain.Voice(LearningProgress.Bosnian, entry.id), $"{entry.id} has no Bosnian voice.");
                Assert.IsNotNull(mountain.Voice(LearningProgress.English, entry.id), $"{entry.id} has no English voice.");
            }

            foreach (var group in mountain.Sentences.GroupBy(z => z.group))
            {
                Assert.GreaterOrEqual(group.Count(), 2, $"Group {group.Key} needs a wrong answer to pair with.");
            }

            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            WordPack river = WordPack.Load("World5");
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World6"));

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(river.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.IsPackUnlocked("World6"), "Six river words should open the mountain.");
        }

        private static bool FindCount(WordPack pack, string bosnian)
        {
            foreach (WordEntry phrase in pack.Counts)
            {
                if (phrase.bs == bosnian)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void FarmOpensAfterSixGardenWords()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            WordPack garden = WordPack.Load("World1");

            Assert.IsTrue(LearningProgress.IsPackUnlocked("World1"), "The first world must always be open.");
            Assert.IsFalse(LearningProgress.IsPackUnlocked("World2"), "The farm opened with no words learned.");
            Assert.IsFalse(LearningProgress.SelectPack("World2"), "A locked world could be chosen.");

            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(garden.Words[i].id);
            }

            Assert.IsTrue(LearningProgress.IsPackUnlocked("World2"), "Six garden words should open the farm.");
        }

        [UnityTest]
        public IEnumerator WorldChooserOnlyPicksOpenWorlds()
        {
            yield return null;

            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.selectedPack = "World1";
            WordWorldsUi worlds = Object.FindFirstObjectByType<WordWorldsUi>();
            Assert.IsNotNull(worlds, "The main menu has no world chooser.");

            worlds.Open();
            Assert.AreEqual(WordPack.PackOrder.Length, worlds.CardCount);
            Assert.IsFalse(worlds.IsWorldUnlocked(1));
            Assert.IsFalse(worlds.PressWorld(1), "A locked world was chosen.");
            Assert.AreEqual("World1", LearningProgress.SelectedPack);

            WordPack garden = WordPack.Load("World1");
            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(garden.Words[i].id);
            }

            worlds.Close();
            worlds.Open();
            Assert.IsTrue(worlds.PressWorld(1), "An open world could not be chosen.");
            Assert.AreEqual("World2", LearningProgress.SelectedPack);
        }

        [UnityTest]
        public IEnumerator WorldMapTrailAndPlayerFollowTheChild()
        {
            yield return null;

            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.selectedPack = "World1";
            WordWorldsUi worlds = Object.FindFirstObjectByType<WordWorldsUi>();

            worlds.Open();
            Assert.AreEqual(0, worlds.MarkerWorld, "The player should stand on the garden at the start.");
            Assert.IsFalse(worlds.IsTrailOpen(0), "The trail to a locked world should stay dim.");

            WordPack garden = WordPack.Load("World1");
            for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
            {
                LearningProgress.RecordCorrect(garden.Words[i].id);
            }

            worlds.Close();
            worlds.Open();
            Assert.IsTrue(worlds.IsTrailOpen(0), "Opening the farm should light the trail to it.");
            Assert.IsFalse(worlds.IsTrailOpen(1));

            Assert.IsTrue(worlds.PressWorld(1));
            Assert.AreEqual(1, worlds.MarkerWorld, "The player should walk to the chosen world.");

            // Every stop sits inside the 1920x1080 frame, and each next stop is a neighbour:
            // along the same row, or straight below at the turn.
            int count = WordPack.PackOrder.Length;
            for (int i = 0; i < count; i++)
            {
                Vector2 stop = WordWorldsUi.MapPosition(i, count);
                Assert.Less(Mathf.Abs(stop.x), 960f - 100f);
                Assert.Less(Mathf.Abs(stop.y), 540f - 100f);
                if (i > 0)
                {
                    Vector2 previous = WordWorldsUi.MapPosition(i - 1, count);
                    Assert.IsTrue(Mathf.Approximately(stop.y, previous.y) != Mathf.Approximately(stop.x, previous.x),
                        $"World {i + 1} is not next to world {i} on the trail.");
                }
            }
        }

        [UnityTest]
        public IEnumerator SettingsHaveAVibrationToggle()
        {
            yield return null;

            Toggle[] toggles = Resources.FindObjectsOfTypeAll<Toggle>();
            Assert.IsTrue(System.Array.Exists(toggles, t => t.name == "VibrationToggle" && t.gameObject.scene.IsValid()),
                "The settings panel has no vibration toggle.");
        }

        [UnityTest]
        public IEnumerator CaughtWordUnlocksItsCardAndSpeaks()
        {
            yield return null;

            WordPack pack = WordPack.Load(WordPack.DefaultPack);
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            LearningProgress.RecordCorrect(pack.Words[2].id);

            WordAlbumUi album = Object.FindFirstObjectByType<WordAlbumUi>();
            album.Open();

            Assert.IsTrue(album.IsCardUnlocked(2), "A caught word should be unlocked in the album.");
            Assert.IsFalse(album.IsCardUnlocked(0), "An uncaught word must stay locked.");
            Assert.IsTrue(album.PressCard(2), "Tapping a learned card should say the word.");
            Assert.IsFalse(album.PressCard(0), "A locked card must not give the word away.");
        }
    }
}
