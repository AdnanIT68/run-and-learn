using System.Collections.Generic;
using FootballRunner.SaveSystem;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// Which language the child is learning and how well each word is known.
    /// Words the child misses come back more often; known words fade out slowly.
    /// </summary>
    public static class LearningProgress
    {
        public const string Bosnian = "bs";
        public const string English = "en";

        /// <summary>The language being learned. Diaspora children learning Bosnian is the default.</summary>
        public static string Language =>
            SaveManager.Data.learningLanguage == English ? English : Bosnian;

        /// <summary>The language the child already speaks, used for menus and hints.</summary>
        public static string HomeLanguage => Language == English ? Bosnian : English;

        public static void SetLanguage(string language)
        {
            SaveManager.Data.learningLanguage = language == English ? English : Bosnian;
            SaveManager.Save();
        }

        public static void ToggleLanguage()
        {
            SetLanguage(Language == English ? Bosnian : English);
        }

        /// <summary>Words to learn in a world before the next one opens.</summary>
        public const int WordsToUnlockNext = 6;

        /// <summary>The world the next run uses. Falls back to the first if the saved one is locked.</summary>
        public static string SelectedPack
        {
            get
            {
                string saved = SaveManager.Data.selectedPack;
                return System.Array.IndexOf(WordPack.PackOrder, saved) >= 0 && IsPackUnlocked(saved)
                    ? saved
                    : WordPack.PackOrder[0];
            }
        }

        public static bool SelectPack(string pack)
        {
            if (!IsPackUnlocked(pack))
            {
                return false;
            }

            SaveManager.Data.selectedPack = pack;
            SaveManager.Save();
            return true;
        }

        /// <summary>The first world is always open; each later one needs words from the world before.</summary>
        public static bool IsPackUnlocked(string pack)
        {
            int index = System.Array.IndexOf(WordPack.PackOrder, pack);

            if (index <= 0)
            {
                return index == 0;
            }

            // Finishing the previous world's level opens the next one; so do six of its words.
            string previousName = WordPack.PackOrder[index - 1];
            WordPack previous = WordPack.Load(previousName);
            return Stars(previousName) > 0 || LearnedCount(previous) >= WordsToUnlockNext;
        }

        /// <summary>Best stars earned in a world's level for the current language (0 to 3).</summary>
        public static int Stars(string pack)
        {
            WorldResult result = FindResult(pack, create: false);
            return result != null ? result.stars : 0;
        }

        /// <summary>Three stars without a mistake, two for one or two, one otherwise.</summary>
        public static int StarsForMisses(int misses)
        {
            if (misses <= 0)
            {
                return 3;
            }

            return misses <= 2 ? 2 : 1;
        }

        public static void RecordLevelComplete(string pack, int stars)
        {
            WorldResult result = FindResult(pack, create: true);
            result.stars = Mathf.Max(result.stars, Mathf.Clamp(stars, 1, 3));
            result.completions++;
            SaveManager.Save();
        }

        /// <summary>The world after this one, or null for the last world.</summary>
        public static string NextPack(string pack)
        {
            int index = System.Array.IndexOf(WordPack.PackOrder, pack);
            return index >= 0 && index + 1 < WordPack.PackOrder.Length ? WordPack.PackOrder[index + 1] : null;
        }

        private static WorldResult FindResult(string pack, bool create)
        {
            List<WorldResult> results = SaveManager.Data.worldResults;

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].pack == pack && results[i].language == Language)
                {
                    return results[i];
                }
            }

            if (!create)
            {
                return null;
            }

            var result = new WorldResult { pack = pack, language = Language };
            results.Add(result);
            return result;
        }

        public static WordProgress Get(string wordId)
        {
            List<WordProgress> list = SaveManager.Data.wordProgress;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == wordId && list[i].language == Language)
                {
                    return list[i];
                }
            }

            var created = new WordProgress { id = wordId, language = Language };
            list.Add(created);
            return created;
        }

        public static void RecordCorrect(string wordId)
        {
            Get(wordId).correct++;
            SaveManager.Save();
        }

        public static void RecordMiss(string wordId)
        {
            Get(wordId).missed++;
            SaveManager.Save();
        }

        public static bool IsLearned(string wordId)
        {
            return Get(wordId).correct > 0;
        }

        public static int LearnedCount(WordPack pack)
        {
            if (pack == null)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < pack.Words.Count; i++)
            {
                if (IsLearned(pack.Words[i].id))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Relative chance of asking this word next.</summary>
        public static float Weight(string wordId)
        {
            WordProgress progress = Get(wordId);

            if (progress.correct == 0 && progress.missed == 0)
            {
                return 1.5f;
            }

            float weight = 1f + progress.missed * 1.5f - Mathf.Min(progress.correct, 5) * 0.15f;
            return Mathf.Max(0.3f, weight);
        }
    }
}
