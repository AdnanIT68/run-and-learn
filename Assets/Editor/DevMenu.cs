using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using UnityEditor;
using UnityEngine;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Shortcuts for trying later worlds in the Editor without playing through
    /// the earlier ones. Editor only: never part of a phone build.
    /// </summary>
    internal static class DevMenu
    {
        [MenuItem("Tools/Run and Learn/Otključaj sve svjetove (unlock all worlds)")]
        private static void UnlockAllWorlds()
        {
            SaveData data = SaveManager.Data;
            data.wordTutorialDone = true;

            foreach (string name in WordPack.PackOrder)
            {
                WordPack pack = WordPack.Load(name);

                for (int i = 0; pack != null && i < LearningProgress.WordsToUnlockNext && i < pack.Words.Count; i++)
                {
                    if (!LearningProgress.IsLearned(pack.Words[i].id))
                    {
                        LearningProgress.RecordCorrect(pack.Words[i].id);
                    }
                }
            }

            SaveManager.Save();
            Debug.Log($"[DevMenu] All {WordPack.PackOrder.Length} worlds unlocked for {LearningProgress.Language}. " +
                      "Choose one with the WORLD button.");
        }

        [MenuItem("Tools/Run and Learn/Resetuj napredak (reset progress)")]
        private static void ResetProgress()
        {
            if (!EditorUtility.DisplayDialog("Reset progress",
                    "Delete all learned words, coins and the high score in the Editor save?", "Reset", "Cancel"))
            {
                return;
            }

            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.wordTutorialDone = false;
            data.selectedPack = WordPack.DefaultPack;
            data.highScore = 0;
            data.totalCoins = 0;
            SaveManager.Save();
            Debug.Log("[DevMenu] Progress reset. The next run starts with the guided first word.");
        }
    }
}
