using System.Collections;
using System.Collections.Generic;
using FootballRunner.Audio;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// World chooser on the main menu. Each world is a big picture card with five
    /// level dots; locked worlds say what opens them, in the language the child
    /// already speaks. Under the cards, the chosen world's five levels can be
    /// picked. The chosen world and level are used by the next run.
    /// </summary>
    public class WordWorldsUi : MonoBehaviour
    {
        private const float MaxCardSize = 420f;
        private const float MaxCardSizeInTwoRows = 235f;
        private const float CardGap = 60f;
        private const float RowWidth = 1720f;

        private static readonly Color PanelColour = new Color(0.2f, 0.42f, 0.62f, 1f);
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
            public Image[] Stars;
            public bool Unlocked;
        }

        private class LevelButton
        {
            public RectTransform Root;
            public Image Background;
            public TMP_Text Number;
            public Image Lock;
            public Image[] Stars;
        }

        private static readonly Color LevelColour = new Color(1f, 0.98f, 0.92f, 1f);

        private readonly List<WorldCard> _cards = new List<WorldCard>();
        private readonly List<LevelButton> _levels = new List<LevelButton>();
        private TMP_Text _levelsLabel;
        private GameObject _panel;
        private TMP_Text _heading;
        private TMP_Text _backLabel;
        private Image _buttonCover;
        private TMP_Text _buttonLabel;
        private TMP_FontAsset _font;
        private string _labelLanguage;
        private float _cardSize = MaxCardSize;

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

        public bool IsLevelUnlocked(int level)
        {
            return LearningProgress.IsLevelUnlocked(LearningProgress.SelectedPack, level);
        }

        /// <summary>Same as tapping a level button. Returns true when that level was chosen.</summary>
        public bool PressLevel(int level)
        {
            if (level < 0 || level >= _levels.Count)
            {
                return false;
            }

            AudioManager.Instance?.PlayButton();

            if (!LearningProgress.SelectLevel(level))
            {
                StartCoroutine(Wiggle(_levels[level].Root));
                return false;
            }

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
            // Two rows once there are more than four worlds, leaving room for the level row below.
            int columns = packs.Length > 4 ? Mathf.CeilToInt(packs.Length / 2f) : packs.Length;
            int rows = Mathf.CeilToInt(packs.Length / (float)columns);
            _cardSize = Mathf.Min(MaxCardSize, (RowWidth - (columns - 1) * CardGap) / columns,
                rows > 1 ? MaxCardSizeInTwoRows : MaxCardSize);
            float totalWidth = columns * _cardSize + (columns - 1) * CardGap;
            float rowGap = 30f;
            float topY = rows > 1 ? 125f + (_cardSize + rowGap) * 0.5f : 90f;

            for (int i = 0; i < packs.Length; i++)
            {
                int column = i % columns;
                int row = i / columns;
                float x = -totalWidth * 0.5f + _cardSize * 0.5f + column * (_cardSize + CardGap);
                float y = topY - row * (_cardSize + rowGap);
                _cards.Add(BuildCard(packs[i], i, new Vector2(x, y)));
            }

            // The chosen world's levels, as a row of numbered buttons under the cards.
            _levelsLabel = CreateText(_panel.transform, "LevelsLabel", "", new Vector2(0f, -205f),
                new Vector2(900f, 60f), 40f, Color.white);

            for (int level = 0; level < LearningProgress.LevelsPerWorld; level++)
            {
                float x = (level - (LearningProgress.LevelsPerWorld - 1) * 0.5f) * 190f;
                _levels.Add(BuildLevelButton(level, new Vector2(x, -340f)));
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
            Round(back.GetComponent<Image>(), 1.6f);
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
            rect.sizeDelta = new Vector2(_cardSize, _cardSize);
            float scale = _cardSize / MaxCardSize;
            go.GetComponent<Button>().onClick.AddListener(() => PressWorld(index));

            WordPack wordPack = WordPack.Load(pack);
            var card = new WorldCard
            {
                Pack = pack,
                Root = rect,
                Background = go.GetComponent<Image>(),
                Cover = CreateImage(go.transform, "Cover",
                    wordPack != null ? wordPack.Picture(WordPack.CoverWordId(pack)) : null,
                    new Vector2(0f, 40f * scale), new Vector2(200f, 200f) * scale),
                Lock = CreateImage(go.transform, "Lock", WordPack.Icon("lock"), new Vector2(150f, 160f) * scale,
                    new Vector2(80f, 80f)),
                Title = CreateText(go.transform, "Title", "", new Vector2(0f, -100f * scale), new Vector2(_cardSize - 30f, 60f),
                    40f, DarkText),
                Detail = CreateText(go.transform, "Detail", "", new Vector2(0f, -160f * scale),
                    new Vector2(_cardSize - 30f, 50f), 30f, DarkText)
            };

            // Best stars from the world's level, across the top of the card.
            // One star per level, filled as levels are finished.
            card.Stars = new Image[LearningProgress.LevelsPerWorld];
            float starStep = _cardSize / 6f;
            for (int s = 0; s < card.Stars.Length; s++)
            {
                card.Stars[s] = CreateImage(go.transform, "Star" + s, WordPack.Icon("star"),
                    new Vector2((s - (card.Stars.Length - 1) * 0.5f) * starStep, 168f * scale),
                    new Vector2(starStep * 0.9f, starStep * 0.9f));
                card.Stars[s].raycastTarget = false;
            }

            // One line each, shrinking to fit, so long names never run into the level count.
            card.Title.textWrappingMode = TextWrappingModes.NoWrap;
            card.Detail.textWrappingMode = TextWrappingModes.NoWrap;
            card.Cover.raycastTarget = false;
            card.Lock.raycastTarget = false;
            Round(card.Background);
            return card;
        }

        private LevelButton BuildLevelButton(int level, Vector2 position)
        {
            var go = new GameObject("Level" + (level + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(_panel.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(160f, 170f);
            go.GetComponent<Button>().onClick.AddListener(() => PressLevel(level));

            var button = new LevelButton
            {
                Root = rect,
                Background = go.GetComponent<Image>(),
                Number = CreateText(go.transform, "Number", (level + 1).ToString(), new Vector2(0f, 18f),
                    new Vector2(140f, 100f), 80f, DarkText),
                Lock = CreateImage(go.transform, "Lock", WordPack.Icon("lock"), new Vector2(0f, 18f),
                    new Vector2(70f, 70f)),
                Stars = new Image[3]
            };

            for (int s = 0; s < 3; s++)
            {
                button.Stars[s] = CreateImage(go.transform, "Star" + s, WordPack.Icon("star"),
                    new Vector2((s - 1) * 40f, -55f), new Vector2(36f, 36f));
                button.Stars[s].raycastTarget = false;
            }

            button.Lock.raycastTarget = false;
            Round(button.Background);
            return button;
        }

        private void Refresh()
        {
            string language = LearningProgress.Language;
            string selected = LearningProgress.SelectedPack;

            _heading.text = LearningProgress.Ui("SVJETOVI", "WORLDS", "WELTEN");
            _backLabel.text = LearningProgress.Ui("NAZAD", "BACK", "ZURÜCK");

            for (int i = 0; i < _cards.Count; i++)
            {
                WorldCard card = _cards[i];
                WordPack pack = WordPack.Load(card.Pack);
                card.Unlocked = LearningProgress.IsPackUnlocked(card.Pack);
                bool isSelected = card.Pack == selected;

                card.Background.color = !card.Unlocked ? LockedColour : isSelected ? SelectedColour : CardColour;
                card.Cover.color = card.Unlocked ? Color.white : new Color(0.1f, 0.12f, 0.12f, 0.55f);
                card.Lock.enabled = !card.Unlocked && card.Lock.sprite != null;

                // One star per finished level along the top of the card.
                int done = LearningProgress.LevelsDone(card.Pack);
                for (int s = 0; s < card.Stars.Length; s++)
                {
                    card.Stars[s].enabled = card.Unlocked && card.Stars[s].sprite != null;
                    card.Stars[s].color = s < done ? Color.white : new Color(0f, 0f, 0f, 0.15f);
                }
                card.Title.text = pack != null ? pack.Title(language) : card.Pack;

                if (card.Unlocked)
                {
                    card.Detail.text = LearningProgress.Ui("Nivo", "Level", "Level") +
                                       $" {done}/{LearningProgress.LevelsPerWorld}";
                }
                else
                {
                    int level = LearningProgress.LevelToOpenNextWorld;
                    card.Detail.text = LearningProgress.Ui($"Završi nivo {level}", $"Finish level {level}",
                        $"Schaffe Level {level}");
                }
            }

            int chosen = LearningProgress.SelectedLevel;
            _levelsLabel.text = LearningProgress.Ui("Izaberi nivo", "Pick a level", "Wähle ein Level");

            for (int level = 0; level < _levels.Count; level++)
            {
                LevelButton button = _levels[level];
                bool open = LearningProgress.IsLevelUnlocked(selected, level);
                int stars = LearningProgress.Stars(selected, level);
                button.Background.color = !open ? LockedColour : level == chosen ? SelectedColour : LevelColour;
                button.Number.gameObject.SetActive(open);
                button.Lock.enabled = !open && button.Lock.sprite != null;

                for (int s = 0; s < button.Stars.Length; s++)
                {
                    button.Stars[s].enabled = open && button.Stars[s].sprite != null;
                    button.Stars[s].color = s < stars ? Color.white : new Color(0f, 0f, 0f, 0.15f);
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
            _buttonLabel.text = LearningProgress.Ui("SVIJET", "WORLD", "WELT") + " " + number;
            _buttonCover.sprite = wordPack != null ? wordPack.Picture(WordPack.CoverWordId(pack)) : null;
            _buttonCover.enabled = _buttonCover.sprite != null;
        }

        /// <summary>Rounded corners to match the main menu.</summary>
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
