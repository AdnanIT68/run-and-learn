using System;
using FootballRunner.SaveSystem;
using UnityEngine;

namespace FootballRunner.Player
{
    /// <summary>
    /// The playable kids. Three are open from the start, the rest are bought with
    /// the coins earned while playing: a fixed, visible price, never a lucky draw.
    /// Frames live in Resources/Characters/&lt;id&gt;/ and are made by Tools/make_characters.py.
    /// </summary>
    public static class CharacterRoster
    {
        public const string DefaultId = "rukajja";
        private const string UnlockPrefix = "character_";
        private const string Root = "Characters/";

        public sealed class Entry
        {
            public readonly string Id;
            public readonly string Name;
            public readonly int Price;

            public Entry(string id, string name, int price)
            {
                Id = id;
                Name = name;
                Price = price;
            }

            public bool IsFree => Price <= 0;
        }

        public static readonly Entry[] All =
        {
            new Entry("rukajja", "Rukajja", 0),
            new Entry("emir", "Emir", 0),
            new Entry("amina", "Amina", 0),
            new Entry("harun", "Harun", 40),
            new Entry("lejla", "Lejla", 60),
            new Entry("tarik", "Tarik", 90),
            new Entry("selma", "Selma", 120),
            new Entry("kenan", "Kenan", 160),
            new Entry("hana", "Hana", 200),
            new Entry("adam", "Adam", 250),
            new Entry("tavus", "Tavus", 300),
            new Entry("alkame", "Alkame", 350),
            new Entry("hind", "Hind", 400)
        };

        /// <summary>Raised after a kid is chosen or bought.</summary>
        public static event Action Changed;

        public static Entry Find(string id)
        {
            foreach (Entry entry in All)
            {
                if (entry.Id == id)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>The chosen kid, falling back to Rukajja for an unknown or locked id.</summary>
        public static string SelectedId
        {
            get
            {
                string id = SaveManager.Data.selectedCharacterId;
                return Find(id) != null && IsUnlocked(id) ? id : DefaultId;
            }
        }

        public static bool IsUnlocked(string id)
        {
            Entry entry = Find(id);
            return entry != null && (entry.IsFree || SaveManager.Data.unlockedItems.Contains(UnlockPrefix + id));
        }

        public static bool CanAfford(string id)
        {
            Entry entry = Find(id);
            return entry != null && SaveManager.Data.totalCoins >= entry.Price;
        }

        public static bool Select(string id)
        {
            if (!IsUnlocked(id))
            {
                return false;
            }

            SaveManager.Data.selectedCharacterId = id;
            SaveManager.Save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Pays the price in coins, opens the kid and chooses them. False if there are not enough coins.</summary>
        public static bool Unlock(string id)
        {
            Entry entry = Find(id);

            if (entry == null)
            {
                return false;
            }

            if (!IsUnlocked(id))
            {
                SaveData data = SaveManager.Data;

                if (data.totalCoins < entry.Price)
                {
                    return false;
                }

                data.totalCoins -= entry.Price;
                data.unlockedItems.Add(UnlockPrefix + id);
            }

            return Select(id);
        }

        /// <summary>One animation frame, e.g. "Run_03", or the standing "Idle" for menus.</summary>
        public static Sprite Frame(string id, string frame)
        {
            return Resources.Load<Sprite>(Root + id + "/" + frame);
        }

        public static Sprite Portrait(string id)
        {
            return Resources.Load<Sprite>(Root + id + "/portrait");
        }
    }
}
