using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FootballRunner.SaveSystem
{
    /// <summary>
    /// JSON file persistence. Chosen over PlayerPrefs so the save format can grow
    /// into structured data (cosmetics, missions) without a migration headache.
    /// </summary>
    public static class SaveManager
    {
        private const string FileName = "footballrunner.save.json";

        private static SaveData _data;

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static SaveData Data
        {
            get
            {
                if (_data == null)
                {
                    Load();
                }

                return _data;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    _data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                }
                else
                {
                    _data = new SaveData();
                }

                if (_data.greenHillsUnlockedLevel < 1)
                {
                    _data.greenHillsUnlockedLevel = 1;
                }

                if (string.IsNullOrEmpty(_data.selectedCharacterId))
                {
                    _data.selectedCharacterId = "rukajja";
                }

                if (string.IsNullOrEmpty(_data.learningLanguage))
                {
                    _data.learningLanguage = "bs";
                }

                if (_data.wordProgress == null)
                {
                    _data.wordProgress = new List<WordProgress>();
                }

                if (_data.worldResults == null)
                {
                    _data.worldResults = new List<WorldResult>();
                }
            }
            catch (Exception e)
            {
                // A corrupt save must never prevent the game from starting.
                Debug.LogWarning($"[SaveManager] Could not read save file, starting fresh. {e.Message}");
                _data = new SaveData();
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Could not write save file. {e.Message}");
            }
        }

        /// <summary>
        /// Records the result of a finished run and persists it immediately.
        /// Returns true only when <paramref name="score"/> beat the previous best.
        /// </summary>
        public static bool SubmitRunResult(int score, int coinsCollected)
        {
            SaveData data = Data;
            bool isNewBest = score > data.highScore;

            if (isNewBest)
            {
                data.highScore = score;
            }

            if (coinsCollected > 0)
            {
                data.totalCoins += coinsCollected;
            }

            Save();
            return isNewBest;
        }

        public static void SetMusicEnabled(bool enabled)
        {
            Data.musicEnabled = enabled;
            Save();
        }

        public static void SetSoundEnabled(bool enabled)
        {
            Data.soundEnabled = enabled;
            Save();
        }
    }
}
