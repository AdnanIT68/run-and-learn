using System.Collections;
using System.Collections.Generic;
using FootballRunner.Audio;
using FootballRunner.Utilities;
using FootballRunner.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The sticker album: every word of the pack as a card. Caught words show in
    /// colour with their name and say themselves when tapped; the rest wait as
    /// silhouettes. Stars show how well a word is known. Built in code on the
    /// main menu canvas, so the MainMenu scene needs no edits.
    /// </summary>
    public class WordAlbumUi : MonoBehaviour
    {
        private const int Columns = 5;
        private const float CardWidth = 250f;
        private const float CardHeight = 300f;
        private const float CardGap = 30f;

        private static readonly Color PanelColour = new Color(0.16f, 0.36f, 0.27f, 1f);
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color LockedCardColour = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color SilhouetteColour = new Color(0.1f, 0.12f, 0.12f, 0.55f);
        private static readonly Color WordColour = new Color(0.16f, 0.2f, 0.26f, 1f);

        private class Card
        {
            public WordEntry Word;
            public RectTransform Root;
            public Image Background;
            public Image Picture;
            public Image Lock;
            public TMP_Text Label;
            public Image[] Stars;
            public bool Unlocked;
        }

        private readonly List<Card> _cards = new List<Card>();
        private WordPack _pack;
        private GameObject _panel;
        private TMP_Text _title;
        private TMP_Text _counter;
        private TMP_Text _backLabel;
        private TMP_FontAsset _font;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public int CardCount => _cards.Count;

        /// <summary>Adds the album button and its panel to a menu canvas once.</summary>
        public static WordAlbumUi EnsureOnCanvas(Transform canvas, TMP_FontAsset font)
        {
            if (canvas == null)
            {
                return null;
            }

            var album = canvas.GetComponent<WordAlbumUi>();

            if (album == null)
            {
                album = canvas.gameObject.AddComponent<WordAlbumUi>();
                album._font = font;
                album.Build();
            }

            return album;
        }

        public void Open()
        {
            if (_panel == null)
            {
                return;
            }

            WordPack selected = WordPack.Load(LearningProgress.SelectedPack);

            if (selected != null && selected != _pack)
            {
                _pack = selected;
                RebuildCards();
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
        }

        public bool IsCardUnlocked(int index)
        {
            return index >= 0 && index < _cards.Count && _cards[index].Unlocked;
        }

        /// <summary>Same as tapping a card. Returns true when the word was spoken.</summary>
        public bool PressCard(int index)
        {
            if (index < 0 || index >= _cards.Count)
            {
                return false;
            }

            Card card = _cards[index];

            if (!card.Unlocked)
            {
                AudioManager.Instance?.PlayButton();
                StartCoroutine(Wiggle(card.Root));
                return false;
            }

            AudioClip clip = _pack.Voice(LearningProgress.Language, card.Word.id);
            AudioManager.Instance?.PlayVoice(clip);
            StartCoroutine(Pop(card.Root));
            return clip != null;
        }

        private void Build()
        {
            _pack = WordPack.Load(LearningProgress.SelectedPack);

            if (_pack == null)
            {
                return;
            }

            Transform safe = transform.Find("SafeArea");
            BuildOpenButton(safe != null ? safe : transform);
            BuildPanel();
            _panel.SetActive(false);
        }

        private void BuildOpenButton(Transform parent)
        {
            var go = new GameObject("AlbumButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(490f, 40f);
            rect.sizeDelta = new Vector2(300f, 130f);
            go.GetComponent<Image>().color = new Color(0.95f, 0.55f, 0.2f, 1f);
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                Open();
            });

            Image icon = CreateImage(go.transform, "Icon", WordPack.Icon("album"), new Vector2(-80f, 0f),
                new Vector2(90f, 90f));
            icon.raycastTarget = false;
            CreateText(go.transform, "Label", "ALBUM", new Vector2(40f, 0f), new Vector2(180f, 80f), 44f,
                Color.white);
        }

        private void BuildPanel()
        {
            _panel = new GameObject("AlbumPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var background = _panel.GetComponent<Image>();
            background.color = PanelColour;
            background.raycastTarget = true;

            _title = CreateText(_panel.transform, "Title", "", new Vector2(0f, 440f), new Vector2(1200f, 80f), 56f,
                Color.white);
            _counter = CreateText(_panel.transform, "Counter", "", new Vector2(0f, 375f), new Vector2(800f, 50f),
                34f, new Color(1f, 0.9f, 0.45f, 1f));

            RebuildCards();

            var back = new GameObject("Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            back.transform.SetParent(_panel.transform, false);
            var backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(60f, -50f);
            backRect.sizeDelta = new Vector2(220f, 90f);
            back.GetComponent<Image>().color = new Color(0.1f, 0.2f, 0.15f, 0.95f);
            Round(back.GetComponent<Image>(), 1.6f);
            back.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                Close();
            });
            _backLabel = CreateText(back.transform, "Label", "", Vector2.zero, new Vector2(220f, 90f), 36f,
                Color.white);
        }

        /// <summary>One card per word of the current pack; called again when the chosen world changes.</summary>
        private void RebuildCards()
        {
            foreach (Card card in _cards)
            {
                if (card.Root != null)
                {
                    Destroy(card.Root.gameObject);
                }
            }

            _cards.Clear();
            float gridWidth = Columns * CardWidth + (Columns - 1) * CardGap;

            for (int i = 0; i < _pack.Words.Count; i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                var position = new Vector2(
                    -gridWidth * 0.5f + CardWidth * 0.5f + column * (CardWidth + CardGap),
                    170f - row * (CardHeight + CardGap));
                _cards.Add(BuildCard(_pack.Words[i], i, position));
            }
        }

        private Card BuildCard(WordEntry word, int index, Vector2 position)
        {
            var go = new GameObject("Card_" + word.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(_panel.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);
            go.GetComponent<Button>().onClick.AddListener(() => PressCard(index));

            var card = new Card
            {
                Word = word,
                Root = rect,
                Background = go.GetComponent<Image>(),
                Picture = CreateImage(go.transform, "Picture", _pack.Picture(word.id), new Vector2(0f, 40f),
                    new Vector2(170f, 170f)),
                Lock = CreateImage(go.transform, "Lock", WordPack.Icon("lock"), new Vector2(80f, 115f),
                    new Vector2(54f, 54f)),
                Label = CreateText(go.transform, "Word", "", new Vector2(0f, -78f), new Vector2(CardWidth - 20f, 56f),
                    38f, WordColour),
                Stars = new Image[3]
            };

            for (int s = 0; s < card.Stars.Length; s++)
            {
                card.Stars[s] = CreateImage(go.transform, "Star" + s, WordPack.Icon("star"),
                    new Vector2((s - 1) * 44f, -122f), new Vector2(38f, 38f));
            }

            card.Picture.raycastTarget = false;
            card.Lock.raycastTarget = false;
            Round(card.Background);
            return card;
        }

        private void Refresh()
        {
            string language = LearningProgress.Language;
            int learned = 0;

            foreach (Card card in _cards)
            {
                WordProgress progress = LearningProgress.Get(card.Word.id);
                card.Unlocked = progress.correct > 0;
                learned += card.Unlocked ? 1 : 0;

                card.Background.color = card.Unlocked ? CardColour : LockedCardColour;
                card.Picture.color = card.Unlocked ? Color.white : SilhouetteColour;
                card.Lock.enabled = !card.Unlocked && card.Lock.sprite != null;
                card.Label.text = card.Unlocked ? card.Word.Text(language) : "?";

                int stars = StarsFor(progress.correct);

                for (int s = 0; s < card.Stars.Length; s++)
                {
                    card.Stars[s].enabled = card.Stars[s].sprite != null;
                    card.Stars[s].color = s < stars ? Color.white : new Color(0f, 0f, 0f, 0.15f);
                }
            }

            _title.text = $"ALBUM  ·  {_pack.Title(language)}";
            _counter.text = $"{learned} / {_cards.Count}";
            _backLabel.text = LearningProgress.Ui("NAZAD", "BACK", "ZURÜCK");
        }

        /// <summary>One star for the first catch, more as the word keeps being caught.</summary>
        public static int StarsFor(int correct)
        {
            if (correct >= 6)
            {
                return 3;
            }

            if (correct >= 3)
            {
                return 2;
            }

            return correct >= 1 ? 1 : 0;
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
                float angle = Mathf.Sin(elapsed / seconds * Mathf.PI * 4f) * 6f * (1f - elapsed / seconds);
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
