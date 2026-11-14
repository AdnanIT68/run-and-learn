using System.Collections;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The story of Brbljavka the magpie, told by Pepi: a card with the magpie and the
    /// line he is saying. It shows while the run keeps going (the calm start has no
    /// hazards), fades out by itself, and a tap closes it early.
    /// </summary>
    public class StoryCardUi : MonoBehaviour
    {
        public static StoryCardUi Instance { get; private set; }

        private RectTransform _card;
        private CanvasGroup _group;
        private Image _picture;
        private TMP_Text _text;
        private Coroutine _routine;

        public bool IsShowing => _group != null && _group.alpha > 0.01f;

        /// <summary>The line shown now or last; for tests.</summary>
        public string LastText { get; private set; }

        public static void EnsureOnCanvas(Transform canvas)
        {
            if (canvas != null && canvas.GetComponent<StoryCardUi>() == null)
            {
                canvas.gameObject.AddComponent<StoryCardUi>();
            }
        }

        private void Awake()
        {
            Instance = this;
            Build(transform);
            _group.alpha = 0f;
            _card.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Shows the magpie and a line of the story for about <paramref name="seconds"/>.</summary>
        public void Show(string text, Sprite picture, float seconds)
        {
            LastText = text;
            _text.text = text;
            _picture.sprite = picture;
            _picture.enabled = picture != null;

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _card.gameObject.SetActive(true);
            _card.SetAsLastSibling();
            _routine = StartCoroutine(Run(Mathf.Max(2f, seconds)));
        }

        public void Hide()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _group.alpha = 0f;
            _card.gameObject.SetActive(false);
        }

        private IEnumerator Run(float seconds)
        {
            yield return Fade(0f, 1f, 0.25f);
            float shown = 0f;

            while (shown < seconds)
            {
                shown += Time.unscaledDeltaTime;
                // The magpie bobs while Pepi tells the story.
                _picture.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(shown * 5f) * 6f);
                yield return null;
            }

            yield return Fade(1f, 0f, 0.3f);
            _card.gameObject.SetActive(false);
            _routine = null;
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            _group.alpha = to;
        }

        private void Build(Transform parent)
        {
            var go = new GameObject("StoryCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(CanvasGroup), typeof(Button));
            go.transform.SetParent(parent, false);
            _card = go.GetComponent<RectTransform>();
            _card.anchorMin = _card.anchorMax = _card.pivot = new Vector2(0.5f, 1f);
            _card.anchoredPosition = new Vector2(0f, -300f);
            _card.sizeDelta = new Vector2(1100f, 230f);

            var background = go.GetComponent<Image>();
            background.sprite = RuntimeSprites.RoundedRect();
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 1.2f;
            background.color = new Color(1f, 0.98f, 0.92f, 0.97f);
            go.GetComponent<Button>().onClick.AddListener(Hide);
            _group = go.GetComponent<CanvasGroup>();

            var pictureGo = new GameObject("Magpie", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pictureGo.transform.SetParent(go.transform, false);
            var pictureRect = pictureGo.GetComponent<RectTransform>();
            pictureRect.anchoredPosition = new Vector2(-420f, 0f);
            pictureRect.sizeDelta = new Vector2(220f, 220f);
            _picture = pictureGo.GetComponent<Image>();
            _picture.preserveAspect = true;
            _picture.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(110f, 0f);
            textRect.sizeDelta = new Vector2(820f, 190f);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            GameFonts.UseDisplay(_text);
            _text.fontSize = 44f;
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 26f;
            _text.fontSizeMax = 44f;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = new Color(0.16f, 0.2f, 0.3f, 1f);
            _text.raycastTarget = false;
        }
    }
}
