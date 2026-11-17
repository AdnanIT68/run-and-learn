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
    /// The picture book screen, opened from the world map: one page at a time with the
    /// world's scenery, the player, Pepi and the page's words as pictures. The page is
    /// read aloud in the language being learned; tapping a picture says its word. The
    /// sentence shows in the learned language, with the home language small underneath
    /// for the grown-up reading along. No running, no score: a calm moment, e.g. at bedtime.
    /// </summary>
    public class PictureBookUi : MonoBehaviour
    {
        private static readonly Color PanelColour = new Color(0.1f, 0.2f, 0.36f, 1f);
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color Ink = new Color(0.16f, 0.2f, 0.3f, 1f);
        private static readonly Color Muted = new Color(0.42f, 0.46f, 0.54f, 1f);
        private static readonly Color Orange = new Color(0.97f, 0.6f, 0.18f, 1f);

        private readonly List<GameObject> _pictures = new List<GameObject>();
        private GameObject _panel;
        private RectTransform _stage;
        private Image _background;
        private Image _kid;
        private TMP_Text _title;
        private TMP_Text _pageNumber;
        private TMP_Text _text;
        private TMP_Text _translation;
        private TMP_Text _latin;
        private TMP_Text _backLabel;
        private TMP_Text _nextLabel;
        private GameObject _previous;
        private PictureBook _book;
        private WordPack _words;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        /// <summary>The page shown, from 0.</summary>
        public int PageIndex { get; private set; }

        public int PageCount => _book != null ? _book.Pages.Count : 0;

        /// <summary>The sentence on screen; for tests.</summary>
        public string ShownText => _text != null ? _text.text : "";

        /// <summary>The words drawn on the page shown; for tests.</summary>
        public IReadOnlyList<string> ShownWords => _book != null ? _book.Pages[PageIndex].Words : new List<string>();

        public static PictureBookUi EnsureOnCanvas(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var ui = canvas.GetComponent<PictureBookUi>();

            if (ui == null)
            {
                ui = canvas.gameObject.AddComponent<PictureBookUi>();
                ui.Build();
            }

            return ui;
        }

        /// <summary>Opens a world's book on its first page. False when the world has none.</summary>
        public bool Open(string pack)
        {
            PictureBook book = PictureBook.Load(pack);

            if (book == null || book.Pages.Count == 0)
            {
                return false;
            }

            _book = book;
            _words = WordPack.Load(pack);
            _background.sprite = Resources.Load<Sprite>("Worlds/" + Gameplay.WorldScenery.SceneryFor(pack) + "/far");
            _background.enabled = _background.sprite != null;
            _kid.sprite = Player.CharacterRoster.Frame(Player.CharacterRoster.SelectedId, "Idle");
            _kid.enabled = _kid.sprite != null;
            _title.text = (_words != null ? _words.Title(LearningProgress.HomeLanguage) + "  ·  " : "") +
                          LearningProgress.Ui("SLIKOVNICA", "PICTURE BOOK", "BILDERBUCH");
            _backLabel.text = LearningProgress.Ui("NAZAD", "BACK", "ZURÜCK");
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            ShowPage(0);
            return true;
        }

        public void Close()
        {
            AudioManager.Instance?.PlayButton();
            StopAllCoroutines();
            _panel.SetActive(false);
        }

        /// <summary>Same as tapping the arrow on the right; on the last page it closes the book.</summary>
        public void Next()
        {
            if (PageIndex + 1 >= PageCount)
            {
                Close();
                return;
            }

            AudioManager.Instance?.PlayButton();
            ShowPage(PageIndex + 1);
        }

        public void Previous()
        {
            if (PageIndex == 0)
            {
                return;
            }

            AudioManager.Instance?.PlayButton();
            ShowPage(PageIndex - 1);
        }

        /// <summary>Reads the page aloud again, as the speaker button does.</summary>
        public void SayPage()
        {
            AudioManager.Instance?.PlayVoice(_book.Voice(LearningProgress.Language, PageIndex));
        }

        /// <summary>Same as tapping a picture on the page: says its word. Returns the word's id.</summary>
        public string TapWord(int index)
        {
            if (index < 0 || index >= ShownWords.Count)
            {
                return null;
            }

            string id = ShownWords[index];
            AudioManager.Instance?.PlayVoice(_words != null ? _words.Voice(LearningProgress.Language, id) : null);

            if (index < _pictures.Count)
            {
                StartCoroutine(Pop(_pictures[index].transform));
            }

            return id;
        }

        private void ShowPage(int index)
        {
            StopAllCoroutines();
            PageIndex = index;
            PictureBook.Page page = _book.Pages[index];
            string language = LearningProgress.Language;
            string home = LearningProgress.HomeLanguage;

            _pageNumber.text = $"{index + 1} / {PageCount}";
            _text.text = page.Text.Display(language);
            string latin = LearningProgress.ShowTransliteration ? page.Text.Latin(language) : null;
            _latin.text = latin ?? "";
            // Arabic comes one sentence per line, already in order; wrapping would scramble it.
            _text.textWrappingMode = language == LearningProgress.Arabic ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            _translation.text = home != language ? page.Text.Text(home) : "";
            _previous.SetActive(index > 0);
            _nextLabel.text = index + 1 < PageCount ? ">" : LearningProgress.Ui("KRAJ", "END", "ENDE");

            foreach (GameObject picture in _pictures)
            {
                Destroy(picture);
            }

            _pictures.Clear();
            int count = page.Words.Count;

            for (int i = 0; i < count; i++)
            {
                int wordIndex = i;
                float x = count == 1 ? 300f : 150f + i * 320f;
                var bubble = new GameObject("Word_" + page.Words[i], typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(Button));
                bubble.transform.SetParent(_stage, false);
                var rect = bubble.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(x, -20f);
                rect.sizeDelta = new Vector2(290f, 290f);
                Image circle = bubble.GetComponent<Image>();
                circle.sprite = RuntimeSprites.Circle();
                circle.color = new Color(1f, 1f, 1f, 0.92f);
                bubble.GetComponent<Button>().onClick.AddListener(() => TapWord(wordIndex));
                Image art = Picture(bubble.transform, _words != null ? _words.Picture(page.Words[i]) : null,
                    Vector2.zero, 210f);
                art.raycastTarget = false;
                _pictures.Add(bubble);
            }

            StartCoroutine(ReadSoon());
        }

        private IEnumerator ReadSoon()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            SayPage();
        }

        private static IEnumerator Pop(Transform target)
        {
            float t = 0f;

            while (t < 0.3f && target != null)
            {
                t += Time.unscaledDeltaTime;
                float scale = 1f + 0.15f * Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }

        // ------------------------------------------------------------ layout

        private void Build()
        {
            _panel = new GameObject("PictureBook", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = PanelColour;

            _title = Text(_panel.transform, "", new Vector2(0f, 450f), new Vector2(1100f, 80f), 52f, Color.white);
            _pageNumber = Text(_panel.transform, "", new Vector2(0f, 392f), new Vector2(300f, 44f), 32f,
                new Color(1f, 0.9f, 0.45f, 1f));

            Image back = Box(_panel.transform, new Vector2(-760f, 445f), new Vector2(240f, 90f), new Color(0.06f, 0.16f, 0.3f, 1f));
            back.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _backLabel = Text(back.transform, "", Vector2.zero, new Vector2(220f, 80f), 36f, Color.white);

            // The page's picture: the world's scenery with the player, Pepi and the words.
            Image frame = Box(_panel.transform, new Vector2(0f, 70f), new Vector2(1520f, 580f), Color.white);
            frame.raycastTarget = false;
            var stageGo = new GameObject("Stage", typeof(RectTransform), typeof(RectMask2D));
            stageGo.transform.SetParent(frame.transform, false);
            _stage = stageGo.GetComponent<RectTransform>();
            _stage.sizeDelta = new Vector2(1490f, 550f);

            _background = Picture(_stage, null, Vector2.zero, 0f);
            _background.rectTransform.sizeDelta = new Vector2(1490f, 760f);
            _background.preserveAspect = false;
            _kid = Picture(_stage, null, new Vector2(-520f, -60f), 380f);
            Image pepi = Picture(_stage, WordPack.Icon("parrot"), new Vector2(-300f, 150f), 150f);
            pepi.raycastTarget = false;

            Image speaker = Box(_stage, new Vector2(660f, 200f), new Vector2(120f, 120f), CardColour);
            speaker.gameObject.AddComponent<Button>().onClick.AddListener(SayPage);
            Picture(speaker.transform, WordPack.Icon("speaker"), Vector2.zero, 90f).raycastTarget = false;

            // The sentence, with the home language small underneath.
            Image card = Box(_panel.transform, new Vector2(0f, -355f), new Vector2(1520f, 220f), CardColour);
            card.raycastTarget = false;
            _text = Text(card.transform, "", new Vector2(0f, 30f), new Vector2(1440f, 130f), 56f, Ink);
            // Arabic's Latin letters on their own line, so they stay readable.
            _latin = Text(card.transform, "", new Vector2(0f, -50f), new Vector2(1440f, 36f), 30f, Ink);
            _translation = Text(card.transform, "", new Vector2(0f, -84f), new Vector2(1440f, 34f), 26f, Muted);

            Image previous = Box(_panel.transform, new Vector2(-855f, -355f), new Vector2(130f, 130f), Orange);
            previous.gameObject.AddComponent<Button>().onClick.AddListener(Previous);
            Text(previous.transform, "<", Vector2.zero, new Vector2(110f, 110f), 80f, Color.white);
            _previous = previous.gameObject;

            Image next = Box(_panel.transform, new Vector2(855f, -355f), new Vector2(150f, 130f), Orange);
            next.gameObject.AddComponent<Button>().onClick.AddListener(Next);
            _nextLabel = Text(next.transform, ">", Vector2.zero, new Vector2(140f, 110f), 80f, Color.white);

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
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        private static TMP_Text Text(Transform parent, string content, Vector2 position, Vector2 size, float fontSize,
            Color colour)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            GameFonts.UseDisplay(text);
            text.text = content;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.4f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = colour;
            text.raycastTarget = false;
            return text;
        }
    }
}
