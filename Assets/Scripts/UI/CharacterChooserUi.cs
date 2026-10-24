using System.Collections;
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
    /// The team on the main menu: ten kids on cards. Open kids are chosen with a
    /// tap; locked ones show their price in coins and open with a tap once the
    /// child has saved enough. Tapping the big kid on the menu opens it too.
    /// </summary>
    public class CharacterChooserUi : MonoBehaviour
    {
        private const int Columns = 5;
        private const float CardWidth = 290f;
        private const float CardHeight = 340f;
        private const float Gap = 28f;

        private static readonly Color PanelColour = new Color(0.16f, 0.5f, 0.36f, 1f);
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color SelectedColour = new Color(1f, 0.85f, 0.35f, 1f);
        private static readonly Color LockedColour = new Color(1f, 1f, 1f, 0.4f);
        private static readonly Color DarkText = new Color(0.16f, 0.2f, 0.26f, 1f);
        private static readonly Color AffordColour = new Color(0.15f, 0.55f, 0.2f, 1f);
        private static readonly Color Silhouette = new Color(0.1f, 0.14f, 0.18f, 0.6f);

        private class Card
        {
            public CharacterRoster.Entry Entry;
            public RectTransform Root;
            public Image Background;
            public Image Picture;
            public Image Lock;
            public Image Coin;
            public TMP_Text Name;
            public TMP_Text Price;
        }

        private readonly List<Card> _cards = new List<Card>();
        private GameObject _panel;
        private TMP_Text _heading;
        private TMP_Text _backLabel;
        private TMP_Text _coins;
        private Image _buttonPicture;
        private TMP_Text _buttonLabel;
        private TMP_FontAsset _font;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public int CardCount => _cards.Count;

        public static CharacterChooserUi EnsureOnCanvas(Transform canvas, TMP_FontAsset font)
        {
            if (canvas == null)
            {
                return null;
            }

            var chooser = canvas.GetComponent<CharacterChooserUi>();

            if (chooser == null)
            {
                chooser = canvas.gameObject.AddComponent<CharacterChooserUi>();
                chooser._font = font;
                chooser.Build();
            }

            return chooser;
        }

        private void OnEnable()
        {
            CharacterRoster.Changed += OnRosterChanged;
        }

        private void OnDisable()
        {
            CharacterRoster.Changed -= OnRosterChanged;
        }

        private void Start()
        {
            // The menu style builds the big waving kid after this; make it ours and tappable.
            Image hero = FindHero();

            if (hero != null)
            {
                hero.raycastTarget = true;
                Button button = hero.GetComponent<Button>() ?? hero.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayButton();
                    Open();
                });
            }

            RefreshMenu();
        }

        public void Open()
        {
            if (_panel == null)
            {
                return;
            }

            Refresh();
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            RefreshMenu();
        }

        /// <summary>Same as tapping a card. Returns true when that kid is now the chosen one.</summary>
        public bool PressCard(int index)
        {
            if (index < 0 || index >= _cards.Count)
            {
                return false;
            }

            Card card = _cards[index];
            string id = card.Entry.Id;
            bool wasLocked = !CharacterRoster.IsUnlocked(id);

            if (!CharacterRoster.Unlock(id))
            {
                AudioManager.Instance?.PlayButton();
                StartCoroutine(Wiggle(card.Root));
                return false;
            }

            if (wasLocked)
            {
                AudioManager.Instance?.PlayCoin();
                Haptics.Medium();
            }
            else
            {
                AudioManager.Instance?.PlayButton();
            }

            StartCoroutine(Pop(card.Root));
            return true;
        }

        private void OnRosterChanged()
        {
            Refresh();
            RefreshMenu();
        }

        // ------------------------------------------------------------ layout

        private void Build()
        {
            Transform safe = transform.Find("SafeArea");
            BuildOpenButton(safe != null ? safe : transform);
            BuildPanel();
            _panel.SetActive(false);
        }

        private void BuildOpenButton(Transform parent)
        {
            var go = new GameObject("CharactersButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(280f, 150f);
            go.GetComponent<Image>().color = new Color(0.2f, 0.62f, 0.42f, 1f);
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                Open();
            });

            _buttonPicture = CreateImage(go.transform, "Picture", null, new Vector2(-90f, 0f), new Vector2(90f, 90f));
            _buttonPicture.raycastTarget = false;
            _buttonLabel = CreateText(go.transform, "Label", "", new Vector2(40f, 0f), new Vector2(190f, 100f), 34f,
                Color.white);
        }

        private void BuildPanel()
        {
            _panel = new GameObject("CharactersPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = PanelColour;

            _heading = CreateText(_panel.transform, "Heading", "", new Vector2(0f, 445f), new Vector2(1000f, 90f), 60f,
                Color.white);

            // Coins the child has, top right, so the prices make sense.
            Image coinIcon = CreateImage(_panel.transform, "CoinIcon", Resources.Load<Sprite>("Menu/icon_coin"),
                new Vector2(640f, 445f), new Vector2(64f, 64f));
            coinIcon.raycastTarget = false;
            _coins = CreateText(_panel.transform, "Coins", "", new Vector2(760f, 445f), new Vector2(160f, 70f), 48f,
                Color.white);
            _coins.alignment = TextAlignmentOptions.Left;

            CharacterRoster.Entry[] roster = CharacterRoster.All;
            int rows = Mathf.CeilToInt(roster.Length / (float)Columns);
            float width = Columns * CardWidth + (Columns - 1) * Gap;
            float height = rows * CardHeight + (rows - 1) * Gap;

            for (int i = 0; i < roster.Length; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var position = new Vector2(
                    -width * 0.5f + CardWidth * 0.5f + column * (CardWidth + Gap),
                    height * 0.5f - CardHeight * 0.5f - row * (CardHeight + Gap) - 40f);
                _cards.Add(BuildCard(roster[i], i, position));
            }

            var back = new GameObject("Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            back.transform.SetParent(_panel.transform, false);
            var backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(60f, -50f);
            backRect.sizeDelta = new Vector2(220f, 90f);
            back.GetComponent<Image>().color = new Color(0.08f, 0.25f, 0.18f, 0.95f);
            Round(back.GetComponent<Image>(), 1.6f);
            back.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                Close();
            });
            _backLabel = CreateText(back.transform, "Label", "", Vector2.zero, new Vector2(220f, 90f), 36f, Color.white);
        }

        private Card BuildCard(CharacterRoster.Entry entry, int index, Vector2 position)
        {
            var go = new GameObject("Kid_" + entry.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(_panel.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);
            go.GetComponent<Button>().onClick.AddListener(() => PressCard(index));

            var card = new Card
            {
                Entry = entry,
                Root = rect,
                Background = go.GetComponent<Image>(),
                // The drawn kids' frames leave room above the head for jumps; show them a bit larger.
                Picture = CreateImage(go.transform, "Picture", CharacterRoster.Frame(entry.Id, "Idle"),
                    entry.Id == CharacterRoster.DefaultId ? new Vector2(0f, 30f) : new Vector2(0f, 40f),
                    entry.Id == CharacterRoster.DefaultId ? new Vector2(190f, 250f) : new Vector2(240f, 320f)),
                Lock = CreateImage(go.transform, "Lock", WordPack.Icon("lock"), new Vector2(105f, 130f),
                    new Vector2(64f, 64f)),
                Coin = CreateImage(go.transform, "Coin", Resources.Load<Sprite>("Menu/icon_coin"),
                    new Vector2(-40f, -135f), new Vector2(46f, 46f)),
                Name = CreateText(go.transform, "Name", entry.Name, new Vector2(0f, -132f),
                    new Vector2(CardWidth - 20f, 56f), 40f, DarkText),
                Price = CreateText(go.transform, "Price", entry.Price.ToString(), new Vector2(30f, -135f),
                    new Vector2(110f, 56f), 40f, DarkText)
            };

            card.Picture.raycastTarget = false;
            card.Lock.raycastTarget = false;
            card.Coin.raycastTarget = false;
            Round(card.Background);
            return card;
        }

        private void Refresh()
        {
            string selected = CharacterRoster.SelectedId;
            int coins = SaveManager.Data.totalCoins;

            _heading.text = LearningProgress.Ui("IZABERI IGRAČA", "PICK A PLAYER", "WÄHLE EINEN SPIELER");
            _backLabel.text = LearningProgress.Ui("NAZAD", "BACK", "ZURÜCK");
            _coins.text = coins.ToString();

            foreach (Card card in _cards)
            {
                bool unlocked = CharacterRoster.IsUnlocked(card.Entry.Id);
                bool isSelected = card.Entry.Id == selected;

                card.Background.color = !unlocked ? LockedColour : isSelected ? SelectedColour : CardColour;
                // Locked kids show as a dark outline: a hint of who is waiting to join.
                card.Picture.color = unlocked ? Color.white : Silhouette;
                card.Lock.enabled = !unlocked && card.Lock.sprite != null;
                card.Name.gameObject.SetActive(unlocked);
                card.Coin.enabled = !unlocked && card.Coin.sprite != null;
                card.Price.gameObject.SetActive(!unlocked);
                card.Price.color = coins >= card.Entry.Price ? AffordColour : DarkText;
            }
        }

        /// <summary>The menu's own card and the big waving kid show whoever is chosen.</summary>
        private void RefreshMenu()
        {
            string id = CharacterRoster.SelectedId;
            if (_buttonLabel != null)
            {
                _buttonLabel.text = LearningProgress.Ui("IGRAČI", "PLAYERS", "SPIELER");
                _buttonPicture.sprite = CharacterRoster.Portrait(id);
                _buttonPicture.enabled = _buttonPicture.sprite != null;
            }

            Image hero = FindHero();

            if (hero != null)
            {
                // Rukajja keeps her waving picture; the others stand ready to run.
                Sprite picture = id == CharacterRoster.DefaultId
                    ? Resources.Load<Sprite>("Menu/rukajja_wave")
                    : CharacterRoster.Frame(id, "Idle");

                if (picture != null)
                {
                    hero.sprite = picture;
                }
            }
        }

        private Image FindHero()
        {
            Transform safe = transform.Find("SafeArea");
            Transform hero = (safe != null ? safe : transform).Find("Hero");
            return hero != null ? hero.GetComponent<Image>() : null;
        }

        // ------------------------------------------------------------ helpers

        private static void Round(Image image, float cornerScale = 1.2f)
        {
            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = sprite != null;
            return image;
        }

        private TMP_Text CreateText(Transform parent, string name, string content, Vector2 position, Vector2 size,
            float fontSize, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = colour;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.5f;
            text.fontSizeMax = fontSize;

            if (GameFonts.Display != null)
            {
                GameFonts.UseDisplay(text);
            }
            else if (_font != null)
            {
                text.font = _font;
            }

            return text;
        }

        private static IEnumerator Pop(RectTransform target)
        {
            const float seconds = 0.25f;
            float elapsed = 0f;

            while (elapsed < seconds && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.one * (1f + Mathf.Sin(elapsed / seconds * Mathf.PI) * 0.12f);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        private static IEnumerator Wiggle(RectTransform target)
        {
            const float seconds = 0.3f;
            float elapsed = 0f;

            while (elapsed < seconds && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float angle = Mathf.Sin(elapsed / seconds * Mathf.PI * 4f) * 5f * (1f - elapsed / seconds);
                target.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            if (target != null)
            {
                target.localRotation = Quaternion.identity;
            }
        }
    }
}
