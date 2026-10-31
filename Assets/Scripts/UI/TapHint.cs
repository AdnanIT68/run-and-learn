using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    /// <summary>
    /// A tapping hand for the guided first word. The game slows down and says
    /// "Jump!", but a four-year-old who cannot read still has to find out that a
    /// tap on the screen is the jump; the hand shows it without any text.
    /// </summary>
    public class TapHint : MonoBehaviour
    {
        private const float TapSeconds = 0.9f;

        public static TapHint Instance { get; private set; }

        private CanvasGroup _group;
        private RectTransform _hand;
        private RectTransform _ring;
        private Image _ringImage;
        private float _shownAt;

        /// <summary>True while the hand is on screen.</summary>
        public bool IsShowing => _group != null && _group.alpha > 0.5f;

        public static TapHint EnsureOn()
        {
            if (Instance != null)
            {
                return Instance;
            }

            return new GameObject("TapHint").AddComponent<TapHint>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _ringImage = CreateImage("Ring", RuntimeSprites.Circle(), new Vector2(220f, 220f));
            _ring = _ringImage.rectTransform;
            // Centred on the fingertip at the bottom of the press.
            _ring.anchoredPosition = new Vector2(125f, -215f);
            Image hand = CreateImage("Hand", WordPack.Icon("tap"), new Vector2(200f, 200f));
            // A soft shadow keeps the yellow hand readable on the orange ground.
            var shadow = hand.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.25f, 0.1f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(6f, -6f);
            _hand = hand.rectTransform;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private Image CreateImage(string name, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            // Lower middle of the screen, clear of the runner and the bubbles ahead of it.
            rect.anchoredPosition = new Vector2(120f, -250f);
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private void Update()
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;
            PlayerController player = PlayerController.Instance;
            // Shown only while the child should tap: once the runner is in the air the job is done.
            bool show = director != null && director.IsGuidingJump && player != null && player.IsGrounded;

            if (show && _group.alpha <= 0f)
            {
                _shownAt = Time.unscaledTime;
            }

            // The guide slows the game down, so the hand runs on unscaled time.
            float fade = Time.unscaledDeltaTime * 8f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, show ? 1f : 0f, fade);

            if (_group.alpha <= 0f)
            {
                return;
            }

            // Press, hold briefly, lift: the fingertip dips and the ring spreads from the touch.
            float t = (Time.unscaledTime - _shownAt) % TapSeconds / TapSeconds;
            float press = t < 0.25f ? t / 0.25f : t < 0.45f ? 1f : 1f - (t - 0.45f) / 0.55f;
            _hand.anchoredPosition = new Vector2(150f, -290f - 30f * press);
            _hand.localScale = Vector3.one * (1f - 0.1f * press);

            float spread = t < 0.25f ? 0f : (t - 0.25f) / 0.75f;
            _ring.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, spread);
            _ringImage.color = new Color(1f, 1f, 1f, t < 0.25f ? 0f : 0.6f * (1f - spread));
        }
    }
}
