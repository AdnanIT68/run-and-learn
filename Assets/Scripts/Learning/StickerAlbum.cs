using System;
using System.Collections.Generic;
using FootballRunner.Player;
using FootballRunner.SaveSystem;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The album is a sticker book: every learned word is a sticker on its world's page.
    /// A full page (all words of a world known, in the language being learned) gives a
    /// new player as a gift: the cheapest one not opened yet, or coins once all are open.
    /// The gift is fixed and shown in advance, never a lucky draw.
    /// </summary>
    public static class StickerAlbum
    {
        /// <summary>Coins given for a full page once every player is already open.</summary>
        public const int CoinsWhenAllPlayersOpen = 50;

        /// <summary>A page was just filled: (world, gifted player or null for coins).</summary>
        public static event Action<string, CharacterRoster.Entry> PageCompleted;

        public static bool IsPageFull(WordPack pack)
        {
            return pack != null && pack.Words.Count > 0 && LearningProgress.LearnedCount(pack) >= pack.Words.Count;
        }

        /// <summary>The player a full page of this world gave, or null if it has not given one.</summary>
        public static CharacterRoster.Entry GiftFor(string pack)
        {
            string prefix = Key(pack) + "=";

            foreach (string record in SaveManager.Data.stickerPages)
            {
                if (record.StartsWith(prefix))
                {
                    return CharacterRoster.Find(record.Substring(prefix.Length));
                }
            }

            return null;
        }

        public static bool WasRewarded(string pack)
        {
            string prefix = Key(pack) + "=";
            return SaveManager.Data.stickerPages.Exists(record => record.StartsWith(prefix));
        }

        /// <summary>The player the next full page will give, or null when all are open.</summary>
        public static CharacterRoster.Entry NextGift()
        {
            CharacterRoster.Entry next = null;

            foreach (CharacterRoster.Entry entry in CharacterRoster.All)
            {
                if (!CharacterRoster.IsUnlocked(entry.Id) && (next == null || entry.Price < next.Price))
                {
                    next = entry;
                }
            }

            return next;
        }

        /// <summary>
        /// Called when a word is answered right. If that filled its world's page for the
        /// first time, the gift is given and PageCompleted is raised. Returns the world, or null.
        /// </summary>
        public static string OnWordLearned(string wordId)
        {
            foreach (string name in WordPack.PackOrder)
            {
                WordPack pack = WordPack.Load(name);

                if (pack == null || !Contains(pack, wordId) || WasRewarded(name) || !IsPageFull(pack))
                {
                    continue;
                }

                CharacterRoster.Entry gift = NextGift();

                if (gift != null)
                {
                    CharacterRoster.Gift(gift.Id);
                }
                else
                {
                    SaveManager.Data.totalCoins += CoinsWhenAllPlayersOpen;
                }

                SaveManager.Data.stickerPages.Add(Key(name) + "=" + (gift != null ? gift.Id : "coins"));
                SaveManager.Save();
                PageCompleted?.Invoke(name, gift);
                return name;
            }

            return null;
        }

        private static bool Contains(WordPack pack, string wordId)
        {
            IReadOnlyList<WordEntry> words = pack.Words;

            for (int i = 0; i < words.Count; i++)
            {
                if (words[i].id == wordId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Key(string pack)
        {
            return pack + ":" + LearningProgress.Language;
        }
    }
}
