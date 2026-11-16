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
        private static SaveData _data;

        /// <summary>True when the main file could not be read, so the next save must not rotate it into the backup.</summary>
        private static bool _mainFileBroken;

        /// <summary>The active child's save file (the original file for the first child).</summary>
        public static string FilePath =>
            Path.Combine(Application.persistentDataPath, ProfileStore.SaveFileName(ProfileStore.ActiveId));

        /// <summary>Drops the loaded save and reads the active child's file again.</summary>
        public static void Reload()
        {
            _data = null;
            _mainFileBroken = false;
            Load();
        }

        /// <summary>The previous good save, used when the main file is broken.</summary>
        public static string BackupPath => FilePath + ".bak";

        /// <summary>Where a save is written before it replaces the main file.</summary>
        public static string TempPath => FilePath + ".tmp";

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

        /// <summary>
        /// Reads the main file, falling back to the backup when the main file is
        /// missing, empty or cut off (for example when Android killed the app mid-write).
        /// </summary>
        public static void Load()
        {
            _mainFileBroken = false;
            SaveData loaded = TryRead(FilePath);

            if (loaded == null)
            {
                _mainFileBroken = File.Exists(FilePath);
                loaded = TryRead(BackupPath);

                if (loaded != null)
                {
                    Debug.LogWarning("[SaveManager] Save file was unreadable; restored the backup copy.");
                }
                else if (_mainFileBroken)
                {
                    // A corrupt save must never prevent the game from starting.
                    Debug.LogWarning("[SaveManager] Save file and backup were unreadable; starting fresh.");
                }
            }

            _data = loaded ?? new SaveData();
            Normalize(_data);
        }

        /// <summary>The save in <paramref name="path"/>, or null when it is missing or not valid save JSON.</summary>
        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                string json = File.ReadAllText(path).Trim('\0', ' ', '\t', '\r', '\n', '﻿');

                if (json.Length < 2 || json[0] != '{' || json[json.Length - 1] != '}')
                {
                    return null;
                }

                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void Normalize(SaveData data)
        {
            if (data.greenHillsUnlockedLevel < 1)
            {
                data.greenHillsUnlockedLevel = 1;
            }

            if (string.IsNullOrEmpty(data.selectedCharacterId))
            {
                data.selectedCharacterId = "rukajja";
            }

            if (string.IsNullOrEmpty(data.learningLanguage))
            {
                data.learningLanguage = "bs";
            }

            if (data.wordProgress == null)
            {
                data.wordProgress = new List<WordProgress>();
            }

            if (data.worldResults == null)
            {
                data.worldResults = new List<WorldResult>();
            }

            if (data.stickerPages == null)
            {
                data.stickerPages = new List<string>();
            }

            if (data.pepiWelcomed == null)
            {
                data.pepiWelcomed = new List<string>();
            }

            if (data.playDays == null)
            {
                data.playDays = new List<PlayDay>();
            }
        }

        /// <summary>
        /// Writes the whole save to a temporary file first and only then swaps it in,
        /// keeping the previous good save as the backup. If the app is killed
        /// halfway, the main file is still the last complete save.
        /// </summary>
        public static void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Data, true);

                using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(FilePath) && !_mainFileBroken)
                {
                    try
                    {
                        File.Replace(TempPath, FilePath, BackupPath, true);
                    }
                    catch (Exception)
                    {
                        // File.Replace is not available on every file system; do the same in steps.
                        File.Copy(FilePath, BackupPath, true);
                        File.Delete(FilePath);
                        File.Move(TempPath, FilePath);
                    }
                }
                else
                {
                    // A broken main file is dropped so it never overwrites the good backup.
                    if (File.Exists(FilePath))
                    {
                        File.Delete(FilePath);
                    }

                    File.Move(TempPath, FilePath);
                }

                _mainFileBroken = false;
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
