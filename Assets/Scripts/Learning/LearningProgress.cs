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

        /// <summary>Knowing every word of a world (it has ten) also opens the next world.</summary>
        public const int WordsToUnlockNext = 10;

        /// <summary>Levels in every world; each is a little faster and asks a little more.</summary>
        public const int LevelsPerWorld = 5;

        /// <summary>Finishing this level (counted from 1) opens the next world.</summary>
        public const int LevelToOpenNextWorld = 3;

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
            SaveManager.Data.selectedLevel = FirstOpenLevel(pack);
            SaveManager.Save();
            return true;
        }

        /// <summary>The level (0 for level 1) the next run in the chosen world uses.</summary>
        public static int SelectedLevel
        {
            get
            {
                string pack = SelectedPack;
                int saved = SaveManager.Data.selectedLevel;
                return IsLevelUnlocked(pack, saved) ? saved : FirstOpenLevel(pack);
            }
        }

        public static bool SelectLevel(int level)
        {
            if (!IsLevelUnlocked(SelectedPack, level))
            {
                return false;
            }

            SaveManager.Data.selectedLevel = level;
            SaveManager.Save();
            return true;
        }

        /// <summary>Level 1 of an open world is open; each later level needs the one before it.</summary>
        public static bool IsLevelUnlocked(string pack, int level)
        {
            return level >= 0 && level < LevelsPerWorld && IsPackUnlocked(pack) &&
                   (level == 0 || Stars(pack, level - 1) > 0);
        }

        /// <summary>The first level not finished yet, or the last level once all are done.</summary>
        public static int FirstOpenLevel(string pack)
        {
            for (int level = 0; level < LevelsPerWorld; level++)
            {
                if (Stars(pack, level) == 0)
                {
                    return level;
                }
            }

            return LevelsPerWorld - 1;
        }

        public static int LevelsDone(string pack)
        {
            int done = 0;

            for (int level = 0; level < LevelsPerWorld; level++)
            {
                if (Stars(pack, level) > 0)
                {
                    done++;
                }
            }

            return done;
        }

        /// <summary>
        /// Where Next on the result screen leads: the following level, then level 1
        /// of the next world once it is open. False after the very last level.
        /// </summary>
        public static bool TryGetNext(string pack, int level, out string nextPack, out int nextLevel)
        {
            if (level + 1 < LevelsPerWorld && IsLevelUnlocked(pack, level + 1))
            {
                nextPack = pack;
                nextLevel = level + 1;
                return true;
            }

            nextPack = NextPack(pack);
            nextLevel = 0;
            return nextPack != null && IsPackUnlocked(nextPack);
        }

        /// <summary>The first world is always open; each later one needs words from the world before.</summary>
        public static bool IsPackUnlocked(string pack)
        {
            int index = System.Array.IndexOf(WordPack.PackOrder, pack);

            if (index <= 0)
            {
                return index == 0;
            }

            // Finishing level 3 of the previous world opens the next one; so does knowing all its words.
            string previousName = WordPack.PackOrder[index - 1];
            WordPack previous = WordPack.Load(previousName);
            int allWords = previous != null ? Mathf.Min(WordsToUnlockNext, previous.Words.Count) : WordsToUnlockNext;
            return Stars(previousName, LevelToOpenNextWorld - 1) > 0 || LearnedCount(previous) >= allWords;
        }

        /// <summary>Best stars earned in one level of a world for the current language (0 to 3).</summary>
        public static int Stars(string pack, int level)
        {
            WorldResult result = FindResult(pack, level, create: false);
            return result != null ? result.stars : 0;
        }

        /// <summary>Best stars earned in any level of a world (0 to 3).</summary>
        public static int Stars(string pack)
        {
            int best = 0;

            for (int level = 0; level < LevelsPerWorld; level++)
            {
                best = Mathf.Max(best, Stars(pack, level));
            }

            return best;
        }

        /// <summary>All stars earned across a world's levels, out of LevelsPerWorld * 3.</summary>
        public static int TotalStars(string pack)
        {
            int total = 0;

            for (int level = 0; level < LevelsPerWorld; level++)
            {
                total += Stars(pack, level);
            }

            return total;
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

        public static void RecordLevelComplete(string pack, int level, int stars)
        {
            WorldResult result = FindResult(pack, Mathf.Clamp(level, 0, LevelsPerWorld - 1), create: true);
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

        private static WorldResult FindResult(string pack, int level, bool create)
        {
            List<WorldResult> results = SaveManager.Data.worldResults;

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].pack == pack && results[i].level == level && results[i].language == Language)
                {
                    return results[i];
                }
            }

            if (!create)
            {
                return null;
            }

            var result = new WorldResult { pack = pack, level = level, language = Language };
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
