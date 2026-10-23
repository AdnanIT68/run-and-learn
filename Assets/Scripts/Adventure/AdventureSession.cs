using FootballRunner.SaveSystem;
using UnityEngine;

namespace FootballRunner.Adventure
{
    /// <summary>
    /// Cross-scene adventure context. Endless mode is the default whenever this
    /// is inactive, so existing Game-scene tests and direct Play still work.
    /// </summary>
    public static class AdventureSession
    {
        public const string WorldGreenHills = "green-hills";
        public const int LevelCount = 5;

        public static bool IsActive { get; private set; }

        public static string WorldId { get; private set; } = WorldGreenHills;

        public static int LevelIndex { get; private set; } = 1;

        public static string SelectedCharacterId { get; private set; } = CharacterRoster.RukajjaId;

        public static bool OpenWorldMapOnMenu { get; set; }

        public static int RunStars { get; set; }

        public static int RunCoins { get; set; }

        public static float RunSeconds { get; set; }

        public static void Clear()
        {
            IsActive = false;
            OpenWorldMapOnMenu = false;
            RunStars = 0;
            RunCoins = 0;
            RunSeconds = 0f;
        }

        public static void PrepareMenus()
        {
            SelectedCharacterId = string.IsNullOrEmpty(SaveManager.Data.selectedCharacterId)
                ? CharacterRoster.RukajjaId
                : SaveManager.Data.selectedCharacterId;
        }

        public static void SelectCharacter(string characterId)
        {
            if (!CharacterRoster.IsUnlocked(characterId))
            {
                return;
            }

            SelectedCharacterId = characterId;
            SaveManager.Data.selectedCharacterId = characterId;
            SaveManager.Save();
        }

        public static void BeginLevel(int levelIndex)
        {
            IsActive = true;
            OpenWorldMapOnMenu = false;
            WorldId = WorldGreenHills;
            LevelIndex = levelIndex;
            RunStars = 0;
            RunCoins = 0;
            RunSeconds = 0f;
        }

        public static void RecordFinish(int stars, int coins, float seconds)
        {
            RunStars = stars;
            RunCoins = coins;
            RunSeconds = seconds;

            SaveData data = SaveManager.Data;
            if (stars > data.greenHillsLevel1Stars)
            {
                data.greenHillsLevel1Stars = stars;
            }

            if (LevelIndex >= data.greenHillsUnlockedLevel)
            {
                data.greenHillsUnlockedLevel = Mathf.Min(LevelCount, LevelIndex + 1);
            }

            SaveManager.SubmitRunResult(0, coins);
        }

        public static bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex <= SaveManager.Data.greenHillsUnlockedLevel;
        }
    }
}
