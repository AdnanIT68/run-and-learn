using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Adventure
{
    /// <summary>
    /// Intro card, in-run objective, and Level Complete overlay for adventure.
    /// </summary>
    public class AdventureUi : MonoBehaviour
    {
        public static AdventureUi Instance { get; private set; }

        private TMP_Text _starHud;
        private TMP_Text _objectiveHud;
        private GameObject _intro;
        private GameObject _complete;
        private TMP_Text _completeTime;
        private TMP_Text _completeCoins;
        private TMP_Text _completeStars;
        private TMP_Text _completeRating;
        private float _runStartTime;
        private bool _timing;

        public static void EnsureOnCanvas(Transform canvas)
        {
            if (Instance != null || canvas == null || !AdventureSession.IsActive)
            {
                return;
            }

            var existing = canvas.GetComponent<AdventureUi>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            canvas.gameObject.AddComponent<AdventureUi>();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            GameManager.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GameManager.StateChanged -= OnStateChanged;
        }

        private void Start()
        {
            Transform parent = transform.Find("SafeArea");
            if (parent == null)
            {
                parent = transform;
            }

            BuildHud(parent);
            BuildIntro(transform);
            BuildComplete(transform);
            ShowIntro(true);
        }

        private void Update()
        {
            if (_starHud != null && AdventureCourse.Instance != null)
            {
                _starHud.text = $"STARS  {AdventureCourse.Instance.StarsCollected} / {AdventureCourse.SpecialStarCount}";
            }

            if (_timing && GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                AdventureSession.RunSeconds = Time.time - _runStartTime;
            }
        }

        public void BeginRun()
        {
            ShowIntro(false);
            _runStartTime = Time.time;
            _timing = true;
            GameManager.Instance?.BeginAdventureRun();
        }

        public void ShowLevelComplete(int stars, int coins, float seconds)
        {
            _timing = false;

            if (_completeTime != null)
            {
                int whole = Mathf.FloorToInt(seconds);
                int minutes = whole / 60;
                int remainder = whole % 60;
                _completeTime.text = $"TIME  {minutes}:{remainder:00}";
            }

            if (_completeCoins != null)
            {
                _completeCoins.text = $"COINS  {coins}";
            }

            if (_completeStars != null)
            {
                _completeStars.text = $"STARS  {stars} / {AdventureCourse.SpecialStarCount}";
            }

            if (_completeRating != null)
            {
                _completeRating.text = stars <= 0 ? "FINISHED!" : stars == 1 ? "NICE RUN!" : stars == 2 ? "GREAT RUN!" : "PERFECT!";
            }

            if (_complete != null)
            {
                _complete.SetActive(true);
                _complete.transform.SetAsLastSibling();
            }
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.LevelComplete)
            {
                int coins = ScoreManager.Instance != null ? ScoreManager.Instance.CoinsCollected : 0;
                int stars = AdventureCourse.Instance != null ? AdventureCourse.Instance.StarsCollected : 0;
                ShowLevelComplete(stars, coins, AdventureSession.RunSeconds);
            }

            if (_objectiveHud != null)
            {
                _objectiveHud.gameObject.SetActive(state == GameState.Playing);
            }

            if (_starHud != null)
            {
                _starHud.gameObject.SetActive(state == GameState.Playing);
            }
        }

        private void BuildHud(Transform parent)
        {
            _objectiveHud = CreateLabel(parent, "AdventureObjective", "REACH THE FINISH",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f),
                new Vector2(720f, 40f), 28f, new Color(1f, 1f, 1f, 0.92f));
            _starHud = CreateLabel(parent, "AdventureStars", "STARS  0 / 3",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f),
                new Vector2(420f, 36f), 26f, new Color(1f, 0.92f, 0.45f, 1f));
        }

        private void BuildIntro(Transform canvas)
        {
            _intro = CreatePanel(canvas, "AdventureIntro", new Color(0.05f, 0.09f, 0.07f, 0.82f));
            Transform card = CreateCard(_intro.transform, new Vector2(860f, 460f), new Color(0.14f, 0.28f, 0.18f, 0.96f));
            CreateLabel(card, "Title", "THE JOURNEY BEGINS",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f),
                new Vector2(780f, 70f), 46f, Color.white);
            CreateLabel(card, "Body",
                "A new path lies beyond Green Hills.\nCollect the lost stars and reach the end of the trail.",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f),
                new Vector2(740f, 160f), 30f, new Color(1f, 1f, 1f, 0.92f));
            Button start = CreateButton(card, "StartLevel", "START", new Vector2(0f, -150f), new Vector2(320f, 88f),
                new Color(0.29f, 0.72f, 0.35f, 1f));
            start.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                BeginRun();
            });
        }

        private void BuildComplete(Transform canvas)
        {
            _complete = CreatePanel(canvas, "LevelCompletePanel", new Color(0.05f, 0.08f, 0.07f, 0.86f));
            _complete.SetActive(false);
            Transform card = CreateCard(_complete.transform, new Vector2(860f, 560f), new Color(0.14f, 0.26f, 0.18f, 0.97f));
            CreateLabel(card, "Title", "LEVEL COMPLETE",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 210f),
                new Vector2(780f, 64f), 48f, Color.white);
            _completeRating = CreateLabel(card, "Rating", "FINISHED!",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f),
                new Vector2(720f, 40f), 30f, new Color(1f, 0.9f, 0.4f, 1f));
            _completeTime = CreateLabel(card, "Time", "TIME  0:00",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f),
                new Vector2(720f, 40f), 32f, Color.white);
            _completeCoins = CreateLabel(card, "Coins", "COINS  0",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 46f),
                new Vector2(720f, 40f), 32f, Color.white);
            _completeStars = CreateLabel(card, "Stars", "STARS  0 / 3",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f),
                new Vector2(720f, 40f), 32f, new Color(1f, 0.92f, 0.45f, 1f));

            Button continueButton = CreateButton(card, "Continue", "CONTINUE", new Vector2(-180f, -180f),
                new Vector2(300f, 88f), new Color(0.29f, 0.72f, 0.35f, 1f));
            continueButton.onClick.AddListener(OnContinue);
            Button replay = CreateButton(card, "Replay", "REPLAY", new Vector2(180f, -180f),
                new Vector2(300f, 88f), new Color(0.25f, 0.55f, 0.90f, 1f));
            replay.onClick.AddListener(OnReplay);
        }

        private void OnContinue()
        {
            AudioManager.Instance?.PlayButton();
            AdventureSession.OpenWorldMapOnMenu = true;
            GameManager.LoadSceneClean(GameManager.MainMenuSceneName);
        }

        private void OnReplay()
        {
            AudioManager.Instance?.PlayButton();
            AdventureSession.BeginLevel(AdventureSession.LevelIndex);
            GameManager.LoadSceneClean(GameManager.GameSceneName);
        }

        private void ShowIntro(bool visible)
        {
            if (_intro != null)
            {
                _intro.SetActive(visible);
                if (visible)
                {
                    _intro.transform.SetAsLastSibling();
                }
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = colour;
            image.raycastTarget = true;
            return go;
        }

        private static Transform CreateCard(Transform parent, Vector2 size, Color colour)
        {
            var go = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = colour;
            return go.transform;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = colour;
            CreateLabel(go.transform, "Label", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 36f, Color.white);
            var labelRect = go.transform.Find("Label") as RectTransform;
            if (labelRect != null)
            {
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            return go.GetComponent<Button>();
        }

        private static TMP_Text CreateLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
            float fontSize, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = colour;
            label.raycastTarget = false;
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            TMP_Text existing = parent.GetComponentInParent<Canvas>()?.GetComponentInChildren<TMP_Text>();
            if (existing != null && existing.font != null)
            {
                font = existing.font;
            }

            if (font != null)
            {
                label.font = font;
            }

            return label;
        }
    }
}
