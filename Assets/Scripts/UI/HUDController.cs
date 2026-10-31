using System.Collections;
using FootballRunner.Adventure;
using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using FootballRunner.Player;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    /// <summary>
    /// Draws score, coins and hearts. Updates are event driven rather than polled
    /// in Update, because rebuilding text meshes every frame is a real cost on mobile.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text coinText;

        [Header("Health")]
        [SerializeField] private Image[] hearts;
        [Tooltip("Optional. When both sprites are set the hearts swap art instead of tinting.")]
        [SerializeField] private Sprite heartFullSprite;
        [SerializeField] private Sprite heartEmptySprite;
        [SerializeField] private Color heartFullColour = Color.white;
        [SerializeField] private Color heartEmptyColour = new Color(1f, 1f, 1f, 0.22f);

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;

        [Header("Area Banner")]
        [SerializeField] private float bannerSeconds = 2.4f;

        private int _lastScore;
        private Coroutine _scorePunch;
        private Coroutine _coinPunch;
        private Coroutine _bannerRoutine;
        private Vector3 _scoreBaseScale = Vector3.one;
        private Vector3 _coinBaseScale = Vector3.one;
        private CanvasGroup _bannerGroup;
        private TMP_Text _bannerKicker;
        private TMP_Text _bannerTitle;

        private void OnEnable()
        {
            ScoreManager.ScoreChanged += OnScoreChanged;
            ScoreManager.CoinsChanged += OnCoinsChanged;
            PlayerHealth.HealthChanged += OnHealthChanged;
            GameManager.StateChanged += OnStateChanged;
            EnvironmentDirector.AreaChanged += OnAreaChanged;
        }

        private void OnDisable()
        {
            ScoreManager.ScoreChanged -= OnScoreChanged;
            ScoreManager.CoinsChanged -= OnCoinsChanged;
            PlayerHealth.HealthChanged -= OnHealthChanged;
            GameManager.StateChanged -= OnStateChanged;
            EnvironmentDirector.AreaChanged -= OnAreaChanged;
        }

        private void Awake()
        {
            MainMenuStyle.KeepHeight(transform);
            SafeAreaFitter.EnsureOnCanvas(transform, "PausePanel", "GameOverPanel");
            UiPointerRepair.DisableDecorativeRaycasts(transform);
            AdventureUi.EnsureOnCanvas(transform);
            Learning.WordPromptUi.EnsureOnCanvas(transform);
            Learning.WordGoalUi.EnsureOnCanvas(transform);
            Learning.LevelResultUi.EnsureOnCanvas(transform);
            Learning.MissionToastUi.EnsureOnCanvas(transform);
        }

        private void Start()
        {
            foreach (GameObject sceneRoot in gameObject.scene.GetRootGameObjects())
            {
                SceneTextLocalizer.Apply(sceneRoot.transform);
            }

            EnsureAreaBanner();
            StyleCorners();

            if (EnvironmentDirector.Instance != null && !AdventureSession.IsActive)
            {
                ShowAreaBanner(EnvironmentDirector.Instance.CurrentDisplayName);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPausePressed);
            }

            if (scoreText != null)
            {
                _scoreBaseScale = scoreText.transform.localScale;
            }

            if (coinText != null)
            {
                _coinBaseScale = coinText.transform.localScale;
            }

            _lastScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            OnScoreChanged(_lastScore);
            OnCoinsChanged(ScoreManager.Instance != null ? ScoreManager.Instance.CoinsCollected : 0);

            if (PlayerController.Instance != null)
            {
                var health = PlayerController.Instance.GetComponent<PlayerHealth>();

                if (health != null)
                {
                    OnHealthChanged(health.CurrentHealth, health.MaxHealth);
                }
            }
        }

        /// <summary>True once score and coins use the game's display font.</summary>
        public bool CornersStyled => scoreText != null && GameFonts.Display != null && scoreText.font == GameFonts.Display;

        /// <summary>
        /// Score, coins and pause in the game's own look: the rounded display font with a
        /// dark outline that stays readable over sky, clouds and sun, and a round pause button.
        /// </summary>
        private void StyleCorners()
        {
            // Only live texts: TMP cannot set an outline before its material exists.
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>())
            {
                bool corner = text == scoreText || text == coinText ||
                              text.name == "ScoreCaption" || text.name == "CoinsCaption";

                if (!corner || !text.isActiveAndEnabled)
                {
                    continue;
                }

                GameFonts.UseDisplay(text);
                text.outlineWidth = 0.22f;
                text.outlineColor = new Color32(20, 45, 80, 220);
                text.color = Color.white;
            }

            if (pauseButton == null)
            {
                return;
            }

            var image = pauseButton.GetComponent<Image>();

            if (image != null)
            {
                image.sprite = RuntimeSprites.Circle();
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = new Color(0.1f, 0.2f, 0.32f, 0.85f);
            }

            foreach (TMP_Text label in pauseButton.GetComponentsInChildren<TMP_Text>(true))
            {
                GameFonts.UseDisplay(label);
                label.color = Color.white;
            }
        }

        private void OnPausePressed()
        {
            AudioManager.Instance?.PlayButton();
            UiFeedback.Punch(this, pauseButton);
            GameManager.Instance?.Pause();
        }

        private void OnAreaChanged(AreaTheme theme, bool announce)
        {
            if (!announce || theme == null)
            {
                return;
            }

            ShowAreaBanner(theme.displayName);
        }

        private void EnsureAreaBanner()
        {
            if (_bannerGroup != null)
            {
                return;
            }

            Transform parent = transform.Find("SafeArea");
            if (parent == null)
            {
                parent = transform;
            }

            TMP_FontAsset font = scoreText != null ? scoreText.font : TMP_Settings.defaultFontAsset;
            var root = new GameObject("AreaBanner", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            // Below the hearts and the word card, so the world's name never covers them.
            rect.anchoredPosition = new Vector2(0f, -330f);
            rect.sizeDelta = new Vector2(1100f, 110f);

            _bannerGroup = root.GetComponent<CanvasGroup>();
            _bannerGroup.alpha = 0f;
            _bannerGroup.blocksRaycasts = false;
            _bannerGroup.interactable = false;
            _bannerGroup.ignoreParentGroups = true;

            _bannerKicker = CreateBannerLabel(root.transform, "Kicker", "NEW AREA",
                new Vector2(0f, -8f), 28f, font, new Color(1f, 1f, 1f, 0.82f));
            _bannerTitle = CreateBannerLabel(root.transform, "Title", "GREEN HILLS",
                new Vector2(0f, -52f), 44f, font, Color.white);
        }

        private static TMP_Text CreateBannerLabel(Transform parent, string name, string text,
            Vector2 anchoredPosition, float fontSize, TMP_FontAsset font, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(1080f, 50f);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = colour;
            label.raycastTarget = false;
            // One line in the game's display font; long world names shrink instead of
            // wrapping into the kicker above them.
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = fontSize * 0.55f;
            label.fontSizeMax = fontSize;
            if (GameFonts.Display != null)
            {
                GameFonts.UseDisplay(label);
            }
            else if (font != null)
            {
                label.font = font;
            }

            return label;
        }

        private void ShowAreaBanner(string areaName)
        {
            EnsureAreaBanner();

            if (_bannerKicker != null)
            {
                _bannerKicker.text = Learning.LearningProgress.Ui("IDEMO!", "LET'S GO!", "LOS GEHT'S!");
            }

            if (_bannerTitle != null)
            {
                _bannerTitle.text = areaName;
            }

            if (_bannerRoutine != null)
            {
                StopCoroutine(_bannerRoutine);
            }

            _bannerRoutine = StartCoroutine(BannerRoutine());
        }

        private IEnumerator BannerRoutine()
        {
            if (_bannerGroup == null)
            {
                yield break;
            }

            _bannerGroup.alpha = 0f;
            float fade = 0.28f;
            float elapsed = 0f;

            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                _bannerGroup.alpha = Mathf.Clamp01(elapsed / fade);
                yield return null;
            }

            _bannerGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.4f, bannerSeconds));

            elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.unscaledDeltaTime;
                _bannerGroup.alpha = 1f - Mathf.Clamp01(elapsed / fade);
                yield return null;
            }

            _bannerGroup.alpha = 0f;
            _bannerRoutine = null;
        }

        private void OnScoreChanged(int score)
        {
            int delta = score - _lastScore;
            _lastScore = score;

            if (scoreText != null)
            {
                scoreText.text = score.ToString();

                if (delta >= 5)
                {
                    _scorePunch = RestartPunch(_scorePunch, scoreText.transform, _scoreBaseScale);
                }
            }
        }

        private void OnCoinsChanged(int coins)
        {
            if (coinText == null)
            {
                return;
            }

            coinText.text = coins.ToString();

            if (coins > 0)
            {
                _coinPunch = RestartPunch(_coinPunch, coinText.transform, _coinBaseScale);
            }
        }

        private Coroutine RestartPunch(Coroutine running, Transform target, Vector3 baseScale)
        {
            if (running != null)
            {
                StopCoroutine(running);
                target.localScale = baseScale;
            }

            return StartCoroutine(PunchRoutine(target, baseScale));
        }

        private IEnumerator PunchRoutine(Transform target, Vector3 baseScale)
        {
            const float duration = 0.16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scale = 1f + (1f - t) * 0.18f;
                target.localScale = baseScale * scale;
                yield return null;
            }

            target.localScale = baseScale;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (hearts == null)
            {
                return;
            }

            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null)
                {
                    continue;
                }

                hearts[i].gameObject.SetActive(i < max);

                bool filled = i < current;

                if (heartFullSprite != null && heartEmptySprite != null)
                {
                    hearts[i].sprite = filled ? heartFullSprite : heartEmptySprite;
                    hearts[i].color = Color.white;
                }
                else
                {
                    hearts[i].color = filled ? heartFullColour : heartEmptyColour;
                }
            }
        }

        private void OnStateChanged(GameState state)
        {
            // Hide the pause button once the run is over so it cannot be pressed
            // behind the game over panel.
            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(state == GameState.Playing);
            }

            if ((state == GameState.GameOver || state == GameState.Dying) && _bannerGroup != null)
            {
                if (_bannerRoutine != null)
                {
                    StopCoroutine(_bannerRoutine);
                    _bannerRoutine = null;
                }

                _bannerGroup.alpha = 0f;
                _bannerGroup.blocksRaycasts = false;
                _bannerGroup.interactable = false;
            }
        }
    }
}
