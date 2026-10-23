using System.Collections;
using System.Collections.Generic;
using FootballRunner.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// World chooser on the main menu. Each world is a big picture card; locked
    /// worlds say how many more words open them, in the language the child
    /// already speaks. The chosen world is used by the next run and the album.
    /// </summary>
    public class WordWorldsUi : MonoBehaviour
    {
        private const float CardSize = 420f;
        private const float CardGap = 80f;

        private static readonly Color PanelColour = new Color(0.2f, 0.42f, 0.62f, 0.98f);
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color SelectedColour = new Color(1f, 0.85f, 0.35f, 1f);
        private static readonly Color LockedColour = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color DarkText = new Color(0.16f, 0.2f, 0.26f, 1f);

        private class WorldCard
        {
            public string Pack;
            public RectTransform Root;
            public Image Background;
            public Image Cover;
            public Image Lock;
            public TMP_Text Title;
            public TMP_Text Detail;
            public bool Unlocked;
        }

        private readonly List<WorldCard> _cards = new List<WorldCard>();
        private GameObject _panel;
        private TMP_Text _heading;
        private TMP_Text _backLabel;
        private Image _buttonCover;
        private TMP_Text _buttonLabel;
        private TMP_FontAsset _font;
        private string _labelLanguage;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public int CardCount => _cards.Count;

        public static WordWorldsUi EnsureOnCanvas(Transform canvas, TMP_FontAsset font)
        {
            if (canvas == null)
            {
                return null;
            }

            var worlds = canvas.GetComponent<WordWorldsUi>();

            if (worlds == null)
            {
                worlds = canvas.gameObject.AddComponent<WordWorldsUi>();
                worlds._font = font;
                worlds.Build();
            }

            return worlds;
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

            RefreshButton();
        }

        public bool IsWorldUnlocked(int index)
        {
            return index >= 0 && index < _cards.Count && _cards[index].Unlocked;
        }

        /// <summary>Same as tapping a world card. Returns true when the world was chosen.</summary>
        public bool PressWorld(int index)
        {
            if (index < 0 || index >= _cards.Count)
            {
                return false;
            }

            WorldCard card = _cards[index];

            if (!card.Unlocked || !LearningProgress.SelectPack(card.Pack))
            {
                AudioManager.Instance?.PlayButton();
                StartCoroutine(Wiggle(card.Root));
                return false;
            }

            AudioManager.Instance?.PlayButton();
            Refresh();
            return true;
        }

        private void LateUpdate()
        {
            // The language button on the menu can switch languages while this is closed.
            if (_labelLanguage != LearningProgress.Language)
            {
                RefreshButton();
            }
        }

        private void Build()
        {
            Transform safe = transform.Find("SafeArea");
            BuildOpenButton(safe != null ? safe : transform);
            BuildPanel();
            _panel.SetActive(false);
            RefreshButton();
        }

        private void BuildOpenButton(Transform parent)
        {
            var go = new GameObject("WorldsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-490f, 40f);
            rect.sizeDelta = new Vector2(300f, 130f);
            go.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.85f, 1f);
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                Open();
            });

            _buttonCover = CreateImage(go.transform, "Cover", null, new Vector2(-90f, 0f), new Vector2(90f, 90f));
            _buttonCover.raycastTarget = false;
            _buttonLabel = CreateText(go.transform, "Label", "", new Vector2(40f, 0f), new Vector2(190f, 100f), 34f,
                Color.white);
        }

        private void BuildPanel()
        {
            _panel = new GameObject("WorldsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = PanelColour;

            _heading = CreateText(_panel.transform, "Heading", "", new Vector2(0f, 420f), new Vector2(1000f, 90f),
                60f, Color.white);

            string[] packs = WordPack.PackOrder;
            float totalWidth = packs.Length * CardSize + (packs.Length - 1) * CardGap;

            for (int i = 0; i < packs.Length; i++)
            {
                float x = -totalWidth * 0.5f + CardSize * 0.5f + i * (CardSize + CardGap);
                _cards.Add(BuildCard(packs[i], i, new Vector2(x, -10f)));
            }

            var back = new GameObject("Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            back.transform.SetParent(_panel.transform, false);
            var backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(60f, -50f);
            backRect.sizeDelta = new Vector2(220f, 90f);
            back.GetComponent<Image>().color = new Color(0.1f, 0.2f, 0.32f, 0.95f);
            back.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                Close();
            });
            _backLabel = CreateText(back.transform, "Label", "", Vector2.zero, new Vector2(220f, 90f), 36f,
                Color.white);
        }

        private WorldCard BuildCard(string pack, int index, Vector2 position)
        {
            var go = new GameObject("World_" + pack, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(_panel.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(CardSize, CardSize);
            go.GetComponent<Button>().onClick.AddListener(() => PressWorld(index));

            WordPack wordPack = WordPack.Load(pack);
            var card = new WorldCard
            {
                Pack = pack,
                Root = rect,
                Background = go.GetComponent<Image>(),
                Cover = CreateImage(go.transform, "Cover",
                    wordPack != null ? wordPack.Picture(WordPack.CoverWordId(pack)) : null,
                    new Vector2(0f, 60f), new Vector2(220f, 220f)),
                Lock = CreateImage(go.transform, "Lock", WordPack.Icon("lock"), new Vector2(150f, 160f),
                    new Vector2(80f, 80f)),
                Title = CreateText(go.transform, "Title", "", new Vector2(0f, -100f), new Vector2(CardSize - 30f, 60f),
                    40f, DarkText),
                Detail = CreateText(go.transform, "Detail", "", new Vector2(0f, -160f),
                    new Vector2(CardSize - 30f, 50f), 30f, DarkText)
            };

            card.Cover.raycastTarget = false;
            card.Lock.raycastTarget = false;
            return card;
        }

        private void Refresh()
        {
            string language = LearningProgress.Language;
            bool homeIsBosnian = LearningProgress.HomeLanguage == LearningProgress.Bosnian;
            string selected = LearningProgress.SelectedPack;

            _heading.text = homeIsBosnian ? "SVJETOVI" : "WORLDS";
            _backLabel.text = homeIsBosnian ? "NAZAD" : "BACK";

            for (int i = 0; i < _cards.Count; i++)
            {
                WorldCard card = _cards[i];
                WordPack pack = WordPack.Load(card.Pack);
                card.Unlocked = LearningProgress.IsPackUnlocked(card.Pack);
                bool isSelected = card.Pack == selected;

                card.Background.color = !card.Unlocked ? LockedColour : isSelected ? SelectedColour : CardColour;
                card.Cover.color = card.Unlocked ? Color.white : new Color(0.1f, 0.12f, 0.12f, 0.55f);
                card.Lock.enabled = !card.Unlocked && card.Lock.sprite != null;
                card.Title.text = pack != null ? pack.Title(language) : card.Pack;

                if (card.Unlocked)
                {
                    int learned = LearningProgress.LearnedCount(pack);
                    int total = pack != null ? pack.Words.Count : 0;
                    card.Detail.text = $"{learned} / {total}";
                }
                else
                {
                    WordPack previous = WordPack.Load(WordPack.PackOrder[Mathf.Max(0, i - 1)]);
                    int missing = Mathf.Max(1, LearningProgress.WordsToUnlockNext - LearningProgress.LearnedCount(previous));
                    card.Detail.text = homeIsBosnian ? $"Nauči još {missing} riječi" : $"Learn {missing} more words";
                }
            }
        }

        private void RefreshButton()
        {
            if (_buttonLabel == null)
            {
                return;
            }

            string pack = LearningProgress.SelectedPack;
            WordPack wordPack = WordPack.Load(pack);
            int number = System.Array.IndexOf(WordPack.PackOrder, pack) + 1;
            _labelLanguage = LearningProgress.Language;
            _buttonLabel.text = LearningProgress.HomeLanguage == LearningProgress.Bosnian
                ? $"SVIJET {number}"
                : $"WORLD {number}";
            _buttonCover.sprite = wordPack != null ? wordPack.Picture(WordPack.CoverWordId(pack)) : null;
            _buttonCover.enabled = _buttonCover.sprite != null;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
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
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
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

            if (_font != null)
            {
                text.font = _font;
            }

            return text;
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
