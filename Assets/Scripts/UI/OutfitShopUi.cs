using System.Collections.Generic;
using FootballRunner.Audio;
using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    /// <summary>
    /// The wardrobe, opened from the players screen: the chosen kid wearing what they
    /// own, and the things to buy with game coins (prices shown in advance), put on or
    /// take off. No real money, no ads.
    /// </summary>
    public class OutfitShopUi : MonoBehaviour
    {
        private static readonly Color PanelColour = new Color(0.12f, 0.24f, 0.4f, 1f);
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color Ink = new Color(0.16f, 0.2f, 0.3f, 1f);
        private static readonly Color Green = new Color(0.27f, 0.7f, 0.33f, 1f);
        private static readonly Color Orange = new Color(0.97f, 0.6f, 0.18f, 1f);
        private static readonly Color Grey = new Color(0.55f, 0.58f, 0.62f, 1f);

        private class Card
        {
            public Outfits.Item Item;
            public Image Button;
            public TMP_Text Label;
        }

        private readonly List<Card> _cards = new List<Card>();
        private readonly Dictionary<Outfits.Slot, Image> _worn = new Dictionary<Outfits.Slot, Image>();
        private GameObject _panel;
        private Image _kid;
        private TMP_Text _title;
        private TMP_Text _coins;
        private TMP_Text _backLabel;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public static OutfitShopUi EnsureOnCanvas(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var shop = canvas.GetComponent<OutfitShopUi>();

            if (shop == null)
            {
                shop = canvas.gameObject.AddComponent<OutfitShopUi>();
                shop.Build();
            }

            return shop;
        }

        public void Open()
        {
            Refresh();
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            AudioManager.Instance?.PlayButton();
            _panel.SetActive(false);
        }

        /// <summary>Same as tapping an item: buy it (if there are coins), put it on, or take it off.</summary>
        public bool Press(string id)
        {
            Outfits.Item item = Outfits.Find(id);
            bool done;

            if (!Outfits.IsOwned(id))
            {
                done = Outfits.Buy(id);

                if (done)
                {
                    AudioManager.Instance?.PlayCoin();
                }
                else
                {
                    AudioManager.Instance?.PlayButton();
                }
            }
            else if (Outfits.IsWorn(id))
            {
                AudioManager.Instance?.PlayButton();
                Outfits.TakeOff(item.Slot);
                done = true;
            }
            else
            {
                AudioManager.Instance?.PlayButton();
                done = Outfits.Wear(id);
            }

            Refresh();
            return done;
        }

        private void Refresh()
        {
            _title.text = LearningProgress.Ui("ODJEĆA", "OUTFITS", "KLEIDUNG");
            _backLabel.text = LearningProgress.Ui("NAZAD", "BACK", "ZURÜCK");
            _coins.text = SaveManager.Data.totalCoins.ToString();

            string kid = CharacterRoster.SelectedId;
            Sprite idle = CharacterRoster.Frame(kid, "Idle");
            _kid.sprite = idle;
            _kid.enabled = idle != null;

            foreach (Card card in _cards)
            {
                Outfits.Item item = card.Item;
                bool owned = Outfits.IsOwned(item.Id);
                bool worn = Outfits.IsWorn(item.Id);
                string name = LearningProgress.Ui(item.NameBs, item.NameEn, item.NameDe);
                card.Button.color = worn ? Green : owned ? Orange : SaveManager.Data.totalCoins >= item.Price ? Orange : Grey;
                card.Label.text = worn
                    ? name + "\n<size=70%>" + LearningProgress.Ui("SKINI", "TAKE OFF", "AUSZIEHEN") + "</size>"
                    : owned
                        ? name + "\n<size=70%>" + LearningProgress.Ui("OBUCI", "PUT ON", "ANZIEHEN") + "</size>"
                        : name + "\n<size=70%>" + item.Price + " " + LearningProgress.Ui("novčića", "coins", "Münzen") + "</size>";
            }

            // The kid wearing what they have on, placed as in the game.
            string key = kid + "/Idle";
            bool known = idle != null && Outfits.TryGetHead(key, out _);

            foreach (KeyValuePair<Outfits.Slot, Image> pair in _worn)
            {
                Outfits.Item item = Outfits.Worn(pair.Key);
                Image image = pair.Value;

                if (item == null || !known)
                {
                    image.enabled = false;
                    continue;
                }

                Outfits.TryGetHead(key, out Outfits.Head head);
                Outfits.Place(pair.Key, head, out Vector2 centre, out float size);
                Rect tex = idle.rect;
                float scale = _kid.rectTransform.rect.height / tex.height;
                image.sprite = Outfits.Picture(item.Id);
                image.enabled = image.sprite != null;
                image.rectTransform.anchoredPosition = _kid.rectTransform.anchoredPosition +
                                                       new Vector2((centre.x - tex.width * 0.5f) * scale,
                                                           (centre.y - tex.height * 0.5f) * scale);
                image.rectTransform.sizeDelta = new Vector2(size * scale, size * scale);
            }
        }

        private void Build()
        {
            _panel = new GameObject("OutfitShop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = PanelColour;

            _title = Text(_panel.transform, new Vector2(0f, 445f), new Vector2(900f, 90f), 60f, Color.white);
            Image coin = Picture(_panel.transform, Resources.Load<Sprite>("Menu/icon_coin"), new Vector2(640f, 445f), 64f);
            coin.raycastTarget = false;
            _coins = Text(_panel.transform, new Vector2(760f, 445f), new Vector2(160f, 70f), 48f, Color.white);
            _coins.alignment = TextAlignmentOptions.Left;

            Image stage = Box(_panel.transform, new Vector2(-520f, -40f), new Vector2(560f, 720f), CardColour);
            stage.raycastTarget = false;
            _kid = Picture(_panel.transform, null, new Vector2(-520f, -40f), 600f);
            _kid.rectTransform.sizeDelta = new Vector2(420f, 600f);

            foreach (Outfits.Slot slot in new[] { Outfits.Slot.Neck, Outfits.Slot.Head, Outfits.Slot.Eyes })
            {
                Image worn = Picture(_panel.transform, null, Vector2.zero, 100f);
                worn.raycastTarget = false;
                _worn[slot] = worn;
            }

            for (int i = 0; i < Outfits.All.Length; i++)
            {
                Outfits.Item item = Outfits.All[i];
                var position = new Vector2(120f + (i % 3) * 300f, 150f - (i / 3) * 380f);
                Image card = Box(_panel.transform, position, new Vector2(270f, 340f), CardColour);
                card.raycastTarget = false;
                Picture(card.transform, Outfits.Picture(item.Id), new Vector2(0f, 60f), 170f).raycastTarget = false;
                Image button = Box(card.transform, new Vector2(0f, -105f), new Vector2(240f, 110f), Orange);
                string id = item.Id;
                button.gameObject.AddComponent<Button>().onClick.AddListener(() => Press(id));
                TMP_Text label = Text(button.transform, Vector2.zero, new Vector2(225f, 100f), 34f, Color.white);
                _cards.Add(new Card { Item = item, Button = button, Label = label });
            }

            Image back = Box(_panel.transform, new Vector2(-790f, 445f), new Vector2(220f, 90f), new Color(0.06f, 0.16f, 0.3f, 1f));
            back.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _backLabel = Text(back.transform, Vector2.zero, new Vector2(200f, 80f), 36f, Color.white);

            _panel.SetActive(false);
        }

        private static Image Box(Transform parent, Vector2 position, Vector2 size, Color colour)
        {
            var go = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.2f;
            image.color = colour;
            return image;
        }

        private static Image Picture(Transform parent, Sprite sprite, Vector2 position, float size)
        {
            var go = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = sprite != null;
            return image;
        }

        private static TMP_Text Text(Transform parent, Vector2 position, Vector2 size, float fontSize, Color colour)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            GameFonts.UseDisplay(text);
            text.text = "";
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.45f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = colour;
            text.raycastTarget = false;
            return text;
        }
    }
}
