using System.Collections;
using FootballRunner.Audio;
using FootballRunner.Learning;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    /// <summary>
    /// The word of the day on the main menu (top left): Pepi, the picture and the word in
    /// the language being learned. A tap: Pepi says "Riječ dana!", the word is said, and its
    /// world is chosen, so "Igraj" goes there. Catching it in a run gives a few coins once a day.
    /// </summary>
    public class WordOfTheDayUi : MonoBehaviour
    {
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.92f, 0.95f);
        private static readonly Color Ink = new Color(0.16f, 0.2f, 0.3f, 1f);
        private static readonly Color Accent = new Color(0.95f, 0.55f, 0.1f, 1f);
        private static readonly Color Green = new Color(0.2f, 0.6f, 0.3f, 1f);

        private GameObject _card;
        private Image _picture;
        private TMP_Text _heading;
        private TMP_Text _word;
        private TMP_Text _hint;

        /// <summary>The world of the word shown; for tests.</summary>
        public string Pack { get; private set; }

        /// <summary>The word shown; for tests.</summary>
        public WordEntry Word { get; private set; }

        public bool IsShown => _card != null && _card.activeSelf;

        public static WordOfTheDayUi EnsureOnCanvas(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var ui = canvas.GetComponent<WordOfTheDayUi>();

            if (ui == null)
            {
                ui = canvas.gameObject.AddComponent<WordOfTheDayUi>();
                ui.Build();
            }

            ui.Refresh();
            return ui;
        }

        private string _shownFor;

        private void LateUpdate()
        {
            // The language button on the menu can change what is learned (and so the word).
            if (_shownFor != LearningProgress.Language)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            _shownFor = LearningProgress.Language;
            (string pack, WordEntry word) = WordOfTheDay.Today();
            Pack = pack;
            Word = word;
            _card.SetActive(word != null);

            if (word == null)
            {
                return;
            }

            WordPack words = WordPack.Load(pack);
            _picture.sprite = words != null ? words.Picture(word.PictureId) : null;
            _picture.enabled = _picture.sprite != null;
            _heading.text = LearningProgress.Ui("RIJEČ DANA", "WORD OF THE DAY", "WORT DES TAGES");
            _word.text = word.CardText(LearningProgress.Language, LearningProgress.ShowTransliteration, 50);
            _hint.text = WordOfTheDay.IsClaimedToday
                ? LearningProgress.Ui("Uhvaćena!", "Caught!", "Gefangen!") + $" +{WordOfTheDay.Reward}"
                : LearningProgress.Ui("Uhvati je u igri", "Catch it in the game", "Fang es im Spiel") +
                  $": +{WordOfTheDay.Reward}";
            _hint.color = WordOfTheDay.IsClaimedToday ? Green : Accent;
        }

        /// <summary>Same as tapping the card: Pepi and the word are heard and the word's world is chosen.</summary>
        public void Press()
        {
            if (Word == null)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(Announce());

            if (LearningProgress.SelectPack(Pack))
            {
                // The worlds button on the menu shows the chosen world.
                Object.FindFirstObjectByType<WordWorldsUi>()?.Close();
            }
        }

        private IEnumerator Announce()
        {
            AudioClip pepi = PepiVoice.Line("pepi_word_of_day");
            AudioManager.Instance?.PlayVoice(pepi);
            yield return new WaitForSecondsRealtime(pepi != null ? pepi.length + 0.15f : 0.2f);
            WordPack words = WordPack.Load(Pack);
            AudioManager.Instance?.PlayVoice(words != null ? words.Voice(LearningProgress.Language, Word.id) : null);
        }

        private void Build()
        {
            Transform safe = transform.Find("SafeArea");
            _card = new GameObject("WordOfTheDay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            _card.transform.SetParent(safe != null ? safe : transform, false);
            var rect = _card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -36f);
            rect.sizeDelta = new Vector2(470f, 170f);
            Image card = _card.GetComponent<Image>();
            card.sprite = RuntimeSprites.RoundedRect();
            card.type = Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 1.2f;
            card.color = CardColour;
            _card.GetComponent<Button>().onClick.AddListener(Press);
            _card.AddComponent<Shadow>().effectDistance = new Vector2(0f, -5f);

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bubble.transform.SetParent(_card.transform, false);
            var bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(0f, 0.5f);
            bubbleRect.anchoredPosition = new Vector2(88f, 0f);
            bubbleRect.sizeDelta = new Vector2(140f, 140f);
            Image circle = bubble.GetComponent<Image>();
            circle.sprite = RuntimeSprites.Circle();
            circle.color = Color.white;
            circle.raycastTarget = false;

            _picture = Picture(bubble.transform, Vector2.zero, 104f);
            Image pepi = Picture(_card.transform, new Vector2(-170f, 62f), 70f);
            pepi.sprite = WordPack.Icon("parrot");
            pepi.enabled = pepi.sprite != null;

            _heading = Text(_card.transform, new Vector2(80f, 52f), new Vector2(290f, 38f), 28f, Accent);
            _word = Text(_card.transform, new Vector2(80f, 0f), new Vector2(290f, 70f), 48f, Ink);
            _hint = Text(_card.transform, new Vector2(80f, -55f), new Vector2(290f, 34f), 22f, Accent);
        }

        private static Image Picture(Transform parent, Vector2 position, float size)
        {
            var go = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            Image image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
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
