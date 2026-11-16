using System.Collections;
using System.Collections.Generic;
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
    /// <summary>Several children on one phone, each with their own progress; old saves become the first child.</summary>
    [Timeout(60000)]
    public class ProfileTests
    {
        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

        private static string Dir => Application.persistentDataPath;

        [SetUp]
        public void KeepRealFiles()
        {
            SaveManager.Save();
            _files.Clear();

            foreach (string path in Directory.GetFiles(Dir, "footballrunner.save*"))
            {
                _files[path] = File.ReadAllBytes(path);
            }

            if (File.Exists(ProfileStore.IndexPath))
            {
                _files[ProfileStore.IndexPath] = File.ReadAllBytes(ProfileStore.IndexPath);
            }
        }

        [TearDown]
        public void RestoreRealFiles()
        {
            foreach (string path in Directory.GetFiles(Dir, "footballrunner.save*"))
            {
                File.Delete(path);
            }

            if (File.Exists(ProfileStore.IndexPath))
            {
                File.Delete(ProfileStore.IndexPath);
            }

            foreach (KeyValuePair<string, byte[]> file in _files)
            {
                File.WriteAllBytes(file.Key, file.Value);
            }

            ProfileStore.ReloadIndex();
            SaveManager.Reload();
        }

        /// <summary>An old phone: a save with progress and no profile list yet.</summary>
        private static void OldSaveWithCoins(int coins)
        {
            if (File.Exists(ProfileStore.IndexPath))
            {
                File.Delete(ProfileStore.IndexPath);
            }

            foreach (string path in Directory.GetFiles(Dir, "footballrunner.save*"))
            {
                File.Delete(path);
            }

            var data = new SaveData { totalCoins = coins, learningLanguage = "en", homeLanguage = "bs" };
            data.wordProgress.Add(new WordProgress { id = "apple", language = "en", correct = 3, spaced = true, box = 3 });
            File.WriteAllText(Path.Combine(Dir, "footballrunner.save.json"), JsonUtility.ToJson(data));
            ProfileStore.ReloadIndex();
            SaveManager.Reload();
        }

        [Test]
        public void ProgressFromBeforeProfilesBecomesTheFirstChildUntouched()
        {
            OldSaveWithCoins(123);

            Assert.AreEqual(ProfileStore.FirstId, ProfileStore.ActiveId);
            Assert.AreEqual(1, ProfileStore.Profiles.Count);
            Assert.AreEqual(123, SaveManager.Data.totalCoins, "The old coins were lost.");
            Assert.AreEqual(3, SaveManager.Data.wordProgress.Find(w => w.id == "apple").correct, "Old word progress was lost.");
            Assert.IsTrue(File.Exists(ProfileStore.IndexPath), "The profile list was not written.");
            StringAssert.EndsWith("footballrunner.save.json", SaveManager.FilePath, "The first child must keep the original file.");
        }

        [Test]
        public void EachChildKeepsTheirOwnProgress()
        {
            OldSaveWithCoins(123);
            ChildProfile amina = ProfileStore.Add("Amina");
            Assert.IsNotNull(amina);

            Assert.IsTrue(ProfileStore.Switch(amina.id));
            Assert.AreEqual(0, SaveManager.Data.totalCoins, "A new child started with someone else's coins.");
            Assert.AreEqual(0, SaveManager.Data.wordProgress.Count);
            Assert.AreEqual("en", SaveManager.Data.learningLanguage, "A new child should start with the same languages.");
            SaveManager.Data.totalCoins = 7;
            SaveManager.Save();

            ProfileStore.Switch(ProfileStore.FirstId);
            Assert.AreEqual(123, SaveManager.Data.totalCoins);
            ProfileStore.Switch(amina.id);
            Assert.AreEqual(7, SaveManager.Data.totalCoins);
        }

        [Test]
        public void APhoneHoldsAtMostFourChildren()
        {
            OldSaveWithCoins(0);
            for (int i = 0; i < ProfileStore.MaxProfiles - 1; i++)
            {
                Assert.IsNotNull(ProfileStore.Add("Dijete " + i));
            }

            Assert.IsNull(ProfileStore.Add("Peto"), "A fifth child was added.");
        }

        [Test]
        public void RemovingAChildDeletesTheirSaveAndNeverTheLastOne()
        {
            OldSaveWithCoins(50);
            Assert.IsFalse(ProfileStore.Remove(ProfileStore.FirstId), "The only child was removed.");

            ChildProfile hana = ProfileStore.Add("Hana");
            ProfileStore.Switch(hana.id);
            SaveManager.Save();
            string hanaFile = SaveManager.FilePath;
            Assert.IsTrue(File.Exists(hanaFile));

            Assert.IsTrue(ProfileStore.Remove(hana.id));
            Assert.AreEqual(ProfileStore.FirstId, ProfileStore.ActiveId, "Removing the playing child should switch to another.");
            Assert.IsFalse(File.Exists(hanaFile), "The removed child's save is still there.");
            Assert.AreEqual(50, SaveManager.Data.totalCoins);
        }

        [Test]
        public void ABrokenProfileListFallsBackToTheFirstChild()
        {
            OldSaveWithCoins(77);
            File.WriteAllText(ProfileStore.IndexPath, "{ broken");
            ProfileStore.ReloadIndex();
            SaveManager.Reload();

            Assert.AreEqual(ProfileStore.FirstId, ProfileStore.ActiveId);
            Assert.AreEqual(77, SaveManager.Data.totalCoins, "A broken list hid the first child's progress.");
        }

        [Test]
        public void TheProfileListSurvivesARestart()
        {
            OldSaveWithCoins(0);
            ChildProfile emir = ProfileStore.Add("Emir");
            ProfileStore.Switch(emir.id);

            ProfileStore.ReloadIndex();
            SaveManager.Reload();
            Assert.AreEqual(emir.id, ProfileStore.ActiveId);
            Assert.AreEqual("Emir", ProfileStore.Active.name);
        }

        [UnityTest]
        public IEnumerator ParentsManageChildrenBehindTheGate()
        {
            OldSaveWithCoins(10);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            ParentsCornerUi corner = Object.FindFirstObjectByType<ParentsCornerUi>();
            corner.Open();
            Assert.IsTrue(corner.IsGateShowing, "The children's list must be behind the gate.");
            Assert.IsTrue(corner.PressAnswer(corner.GateAnswer));

            corner.OpenProfiles();
            ProfilesUi profiles = corner.Profiles;
            Assert.IsTrue(profiles.IsOpen);
            Assert.IsTrue(profiles.AddChild("Selma"));
            Assert.AreEqual(2, ProfileStore.Profiles.Count);

            string selma = ProfileStore.Profiles[1].id;
            Assert.IsTrue(profiles.RequestRemove(selma));
            Assert.AreEqual(selma, profiles.PendingRemove, "REMOVE should ask once more first.");
            Assert.AreEqual(2, ProfileStore.Profiles.Count);

            Assert.IsTrue(profiles.SelectChild(selma));
            Assert.AreEqual(selma, ProfileStore.ActiveId);
            Assert.IsTrue(profiles.SwitchedSinceOpen);

            profiles.Close();
            corner.Close();
            yield return null;
            yield return null;
            yield return null;

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu);
            StringAssert.Contains("SELMA", LanguageChipText(), "The menu should say who is playing.");
        }

        private static string LanguageChipText()
        {
            foreach (TMPro.TMP_Text text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            {
                if (text.text.Contains("UČIM") || text.text.Contains("LEARNING") || text.text.Contains("LERNE"))
                {
                    return text.text;
                }
            }

            return "";
        }
    }
}
