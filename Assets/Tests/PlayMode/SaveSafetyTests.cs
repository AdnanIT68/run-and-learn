using System.IO;
using System.Text;
using FootballRunner.SaveSystem;
using NUnit.Framework;

namespace FootballRunner.Tests
{
    /// <summary>
    /// The save file must survive the app being killed mid-write: a cut-off or
    /// zero-filled main file falls back to the backup instead of wiping progress.
    /// </summary>
    public class SaveSafetyTests
    {
        private byte[] _main;
        private byte[] _backup;
        private byte[] _temp;

        [SetUp]
        public void KeepRealSave()
        {
            _main = ReadOrNull(SaveManager.FilePath);
            _backup = ReadOrNull(SaveManager.BackupPath);
            _temp = ReadOrNull(SaveManager.TempPath);
            DeleteAll();
            SaveManager.Load();
        }

        [TearDown]
        public void RestoreRealSave()
        {
            DeleteAll();
            WriteIfNotNull(SaveManager.FilePath, _main);
            WriteIfNotNull(SaveManager.BackupPath, _backup);
            WriteIfNotNull(SaveManager.TempPath, _temp);
            SaveManager.Load();
        }

        [Test]
        public void SavingKeepsThePreviousSaveAsBackup()
        {
            SaveCoins(111);
            SaveCoins(222);

            Assert.AreEqual(222, CoinsIn(SaveManager.FilePath));
            Assert.AreEqual(111, CoinsIn(SaveManager.BackupPath));
            Assert.IsFalse(File.Exists(SaveManager.TempPath), "The temporary file is swapped in, not left behind.");
        }

        [Test]
        public void CutOffSaveFallsBackToTheBackup()
        {
            SaveCoins(111);
            SaveCoins(222);
            string json = File.ReadAllText(SaveManager.FilePath);
            File.WriteAllText(SaveManager.FilePath, json.Substring(0, json.Length / 2));

            SaveManager.Load();

            Assert.AreEqual(111, SaveManager.Data.totalCoins);
        }

        [Test]
        public void ZeroFilledSaveFallsBackToTheBackup()
        {
            SaveCoins(111);
            SaveCoins(222);
            File.WriteAllBytes(SaveManager.FilePath, new byte[4096]);

            SaveManager.Load();

            Assert.AreEqual(111, SaveManager.Data.totalCoins);
        }

        [Test]
        public void EmptySaveFallsBackToTheBackup()
        {
            SaveCoins(111);
            SaveCoins(222);
            File.WriteAllText(SaveManager.FilePath, string.Empty);

            SaveManager.Load();

            Assert.AreEqual(111, SaveManager.Data.totalCoins);
        }

        [Test]
        public void SavingAfterARestoreNeverOverwritesTheGoodBackup()
        {
            SaveCoins(111);
            SaveCoins(222);
            File.WriteAllText(SaveManager.FilePath, "{\"highScore\": 5, \"totalCo");
            SaveManager.Load();

            SaveCoins(333);

            Assert.AreEqual(333, CoinsIn(SaveManager.FilePath));
            Assert.AreEqual(111, CoinsIn(SaveManager.BackupPath), "The broken file must not become the backup.");
        }

        [Test]
        public void BrokenSaveAndBackupStartFreshWithoutErrors()
        {
            File.WriteAllText(SaveManager.FilePath, "{\"totalCoins\": 9");
            File.WriteAllText(SaveManager.BackupPath, "garbage");

            SaveManager.Load();

            Assert.AreEqual(0, SaveManager.Data.totalCoins);
            Assert.AreEqual(1, SaveManager.Data.greenHillsUnlockedLevel);
            Assert.IsNotNull(SaveManager.Data.wordProgress);
        }

        [Test]
        public void LeftoverPartialTempFileIsIgnored()
        {
            SaveCoins(111);
            File.WriteAllText(SaveManager.TempPath, "{\"totalCoins\": 99");

            SaveManager.Load();
            Assert.AreEqual(111, SaveManager.Data.totalCoins);

            SaveCoins(444);
            Assert.AreEqual(444, CoinsIn(SaveManager.FilePath));
        }

        private static void SaveCoins(int coins)
        {
            SaveManager.Data.totalCoins = coins;
            SaveManager.Save();
        }

        private static int CoinsIn(string path)
        {
            return UnityEngine.JsonUtility.FromJson<SaveData>(File.ReadAllText(path, Encoding.UTF8)).totalCoins;
        }

        private static byte[] ReadOrNull(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        private static void WriteIfNotNull(string path, byte[] bytes)
        {
            if (bytes != null)
            {
                File.WriteAllBytes(path, bytes);
            }
        }

        private static void DeleteAll()
        {
            foreach (string path in new[] { SaveManager.FilePath, SaveManager.BackupPath, SaveManager.TempPath })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
