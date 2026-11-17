using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FootballRunner.SaveSystem;

namespace FootballRunner.Learning
{
    /// <summary>
    /// One word a day from the worlds the child can already play, the same all day (the
    /// date comes from DailyRewards.Clock so tests can move it). Pepi shows it on the main
    /// menu; catching it in a run the first time that day gives a few coins.
    /// </summary>
    public static class WordOfTheDay
    {
        /// <summary>Coins for catching the word of the day, once a day.</summary>
        public const int Reward = 5;

        private static readonly DateTime Start = new DateTime(2026, 1, 1);

        public static string TodayKey => DailyRewards.Clock().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>
        /// Today's word and its world, or (null, null) when no world has words. Chosen once
        /// a day (per language) and kept in the save, so opening a new world during the day
        /// does not change it.
        /// </summary>
        public static (string pack, WordEntry word) Today()
        {
            SaveData data = SaveManager.Data;
            string language = LearningProgress.Language;

            if (data.wordOfDayDate == TodayKey && data.wordOfDayLanguage == language)
            {
                WordEntry kept = Find(data.wordOfDayPack, data.wordOfDayId);

                if (kept != null)
                {
                    return (data.wordOfDayPack, kept);
                }
            }

            (string pack, WordEntry word) chosen = Choose();

            if (chosen.word != null)
            {
                data.wordOfDayDate = TodayKey;
                data.wordOfDayLanguage = language;
                data.wordOfDayPack = chosen.pack;
                data.wordOfDayId = chosen.word.id;
                SaveManager.Save();
            }

            return chosen;
        }

        private static WordEntry Find(string pack, string id)
        {
            WordPack words = string.IsNullOrEmpty(pack) ? null : WordPack.Load(pack);
            return words?.Words.FirstOrDefault(w => w.id == id);
        }

        private static (string pack, WordEntry word) Choose()
        {
            var candidates = new List<(string, WordEntry)>();

            foreach (string pack in WordPack.PackOrder)
            {
                // The finale's own night words count too (its Words are only those).
                WordPack words = LearningProgress.IsPackUnlocked(pack) ? WordPack.Load(pack) : null;

                if (words == null)
                {
                    continue;
                }

                foreach (WordEntry word in words.Words)
                {
                    candidates.Add((pack, word));
                }
            }

            if (candidates.Count == 0)
            {
                return (null, null);
            }

            // A fixed mix of the day number and the language, so each day (and each language) has its own word.
            long day = (long)(DailyRewards.Clock().Date - Start).TotalDays;
            long mixed = (day * 2654435761L + LanguageSeed(LearningProgress.Language)) & 0x7fffffff;
            mixed ^= mixed >> 13;
            return candidates[(int)(mixed % candidates.Count)];
        }

        public static bool IsClaimedToday => SaveManager.Data.wordOfDayClaimed == TodayKey;

        /// <summary>
        /// A word was caught in a run. When it is today's word and its coins were not given
        /// yet today, gives them and returns true.
        /// </summary>
        public static bool OnCaught(string pack, string wordId)
        {
            if (IsClaimedToday)
            {
                return false;
            }

            (string todayPack, WordEntry word) = Today();

            if (word == null || word.id != wordId || todayPack != pack)
            {
                return false;
            }

            SaveData data = SaveManager.Data;
            data.wordOfDayClaimed = TodayKey;
            data.totalCoins += Reward;
            SaveManager.Save();
            return true;
        }

        private static int LanguageSeed(string language)
        {
            int seed = 17;

            foreach (char c in language ?? "")
            {
                seed = seed * 31 + c;
            }

            return seed & 0xffff;
        }
    }
}
