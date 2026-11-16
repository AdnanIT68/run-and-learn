using System;
using System.Collections.Generic;
using FootballRunner.SaveSystem;
using UnityEngine;

namespace FootballRunner.Player
{
    /// <summary>
    /// Things to wear: a cap or a crown, glasses or sunglasses, a scarf. Bought only with
    /// the coins earned in the game, at a fixed price shown in advance: no real money,
    /// no ads, no lucky draws. One item per place (head, eyes, neck), each child their own.
    /// </summary>
    public static class Outfits
    {
        public enum Slot
        {
            Head,
            Eyes,
            Neck
        }

        public sealed class Item
        {
            public readonly string Id;
            public readonly Slot Slot;
            public readonly int Price;
            public readonly string NameBs;
            public readonly string NameEn;
            public readonly string NameDe;

            public Item(string id, Slot slot, int price, string bs, string en, string de)
            {
                Id = id;
                Slot = slot;
                Price = price;
                NameBs = bs;
                NameEn = en;
                NameDe = de;
            }
        }

        private const string OwnedPrefix = "outfit_";

        public static readonly Item[] All =
        {
            new Item("cap", Slot.Head, 30, "Kapa", "Cap", "Kappe"),
            new Item("crown", Slot.Head, 100, "Kruna", "Crown", "Krone"),
            new Item("glasses", Slot.Eyes, 40, "Naočale", "Glasses", "Brille"),
            new Item("sunglasses", Slot.Eyes, 60, "Sunčane naočale", "Sunglasses", "Sonnenbrille"),
            new Item("scarf", Slot.Neck, 40, "Šal", "Scarf", "Schal")
        };

        /// <summary>Raised after something was bought, put on or taken off.</summary>
        public static event Action Changed;

        public static Item Find(string id)
        {
            foreach (Item item in All)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }

            return null;
        }

        public static bool IsOwned(string id)
        {
            return SaveManager.Data.unlockedItems.Contains(OwnedPrefix + id);
        }

        /// <summary>Pays the price in coins. False if already owned or there are not enough coins.</summary>
        public static bool Buy(string id)
        {
            Item item = Find(id);
            SaveData data = SaveManager.Data;

            if (item == null || IsOwned(id) || data.totalCoins < item.Price)
            {
                return false;
            }

            data.totalCoins -= item.Price;
            data.unlockedItems.Add(OwnedPrefix + id);
            Wear(id);
            return true;
        }

        /// <summary>Puts an owned item on, replacing whatever was worn in the same place.</summary>
        public static bool Wear(string id)
        {
            Item item = Find(id);

            if (item == null || !IsOwned(id))
            {
                return false;
            }

            SetWorn(item.Slot, id);
            return true;
        }

        public static void TakeOff(Slot slot)
        {
            SetWorn(slot, "");
        }

        /// <summary>The item worn in a place, or null.</summary>
        public static Item Worn(Slot slot)
        {
            SaveData data = SaveManager.Data;
            string id = slot == Slot.Head ? data.outfitHead : slot == Slot.Eyes ? data.outfitEyes : data.outfitNeck;
            return string.IsNullOrEmpty(id) || !IsOwned(id) ? null : Find(id);
        }

        public static bool IsWorn(string id)
        {
            Item item = Find(id);
            return item != null && Worn(item.Slot) == item;
        }

        private static readonly Dictionary<string, Sprite> Pictures = new Dictionary<string, Sprite>();

        /// <summary>The item's picture, loaded once (the runner asks for it every frame).</summary>
        public static Sprite Picture(string id)
        {
            if (!Pictures.TryGetValue(id, out Sprite sprite) || sprite == null)
            {
                sprite = Resources.Load<Sprite>("Outfits/" + id);
                Pictures[id] = sprite;
            }

            return sprite;
        }

        private static void SetWorn(Slot slot, string id)
        {
            SaveData data = SaveManager.Data;

            switch (slot)
            {
                case Slot.Head:
                    data.outfitHead = id;
                    break;
                case Slot.Eyes:
                    data.outfitEyes = id;
                    break;
                default:
                    data.outfitNeck = id;
                    break;
            }

            SaveManager.Save();
            Changed?.Invoke();
        }

        // ------------------------------------------------------------ where things go

        /// <summary>Where a kid's head is in a frame, in texture pixels (y from the bottom), from Tools/make_outfits.py.</summary>
        public struct Head
        {
            public float X;
            public float Top;
            public float Width;
        }

        [Serializable]
        private class Anchor
        {
            public string key;
            public float x;
            public float top;
            public float width;
        }

        [Serializable]
        private class Anchors
        {
            public Anchor[] anchors;
        }

        private static Dictionary<string, Head> _heads;

        /// <summary>The head in "emir/Run_03"; false if the frame is unknown.</summary>
        public static bool TryGetHead(string key, out Head head)
        {
            if (_heads == null)
            {
                _heads = new Dictionary<string, Head>();
                var asset = Resources.Load<TextAsset>("Outfits/anchors");

                if (asset != null)
                {
                    foreach (Anchor a in JsonUtility.FromJson<Anchors>(asset.text).anchors)
                    {
                        _heads[a.key] = new Head { X = a.x, Top = a.top, Width = a.width };
                    }
                }
            }

            return _heads.TryGetValue(key, out head);
        }

        /// <summary>
        /// The centre and size of an item in texture pixels (y from the bottom), from the
        /// head: a hat sits on top, glasses across the eyes, a scarf under the chin.
        /// Matches place() in Tools/make_outfits.py.
        /// </summary>
        public static void Place(Slot slot, Head head, out Vector2 centre, out float size)
        {
            float w = head.Width;

            switch (slot)
            {
                case Slot.Head:
                    size = w;
                    centre = new Vector2(head.X, head.Top - size * 0.02f);
                    break;
                case Slot.Eyes:
                    size = w * 0.78f;
                    centre = new Vector2(head.X + w * 0.06f, head.Top - w * 0.55f);
                    break;
                default:
                    size = w * 0.6f;
                    centre = new Vector2(head.X, head.Top - w * 1.08f - size * 0.2f);
                    break;
            }
        }
    }
}
