using FootballRunner.Adventure;
using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    public class MainMenuController : MonoBehaviour
    {
        private static string HowToPlayTitle =>
            LearningProgress.Ui("KAKO SE IGRA", "HOW TO PLAY", "SO WIRD GESPIELT");

        private static string HowToPlayBody => LearningProgress.Ui(
            "Slušaj riječ.\nSkoči u balon s pravom slikom!\n\n" +
            "Popuni krugove i završi nivo.\nDodirni ekran da preskočiš prepreke.",
            "Listen to the word.\nJump into the bubble with the right picture!\n\n" +
            "Fill the circles to finish the level.\nTap the screen to jump over spikes and gaps.",
            "Hör auf das Wort.\nSpring in die Blase mit dem richtigen Bild!\n\n" +
            "Füll die Kreise und schaffe das Level.\nTippe auf den Bildschirm, um über Hindernisse zu springen.");

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button howToPlayButton;

        [Header("Stats")]
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text totalCoinsText;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle soundToggle;

        [Header("How To Play")]
        [SerializeField] private GameObject howToPlayPanel;

        private Button _languageButton;
        private Toggle _vibrationToggle;
        private string _languageLabelPack;
        private bool _creditsBuilt;

        private void LateUpdate()
        {
            // Buying a player in the chooser spends coins; keep the chip honest.
            if (totalCoinsText != null)
            {
                string coins = SaveManager.Data.totalCoins.ToString();

                if (totalCoinsText.text != coins)
                {
                    totalCoinsText.text = coins;
                }
            }

            // The world chooser can switch worlds; the word count on the language button follows it.
            if (_languageButton != null && _languageLabelPack != LearningProgress.SelectedPack)
            {
                RefreshLanguageLabel();
            }
        }

        private void RefreshLanguageLabel()
        {
            _languageLabelPack = LearningProgress.SelectedPack;
            var label = _languageButton.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = LanguageLabel();
            }
        }

        private void Awake()
        {
            SafeAreaFitter.EnsureOnCanvas(transform, "SettingsPanel", "Scrim", "HowToPlayPanel");
            UiPointerRepair.DisableDecorativeRaycasts(transform);
        }

        private void Start()
        {
            EnsureHowToPlayUi();
            EnsureLanguageButton();
            EnsureVibrationToggle();
            EnsureCredits();
            WordAlbumUi.EnsureOnCanvas(transform, ResolveFont());
            WordWorldsUi.EnsureOnCanvas(transform, ResolveFont());
            CharacterChooserUi.EnsureOnCanvas(transform, ResolveFont());
            DailyUi.EnsureOnCanvas(transform);
            MainMenuStyle.Apply(transform, playButton, settingsButton, quitButton, howToPlayButton,
                highScoreText, totalCoinsText);

            playButton?.onClick.AddListener(OnPlay);
            settingsButton?.onClick.AddListener(() => ShowSettings(true));
            closeSettingsButton?.onClick.AddListener(() => ShowSettings(false));
            quitButton?.onClick.AddListener(OnQuit);
            howToPlayButton?.onClick.AddListener(() => ShowHowToPlay(true));

            SaveData data = SaveManager.Data;

            if (highScoreText != null)
            {
                highScoreText.text = data.highScore.ToString();
            }

            if (totalCoinsText != null)
            {
                totalCoinsText.text = data.totalCoins.ToString();
            }

            if (musicToggle != null)
            {
                musicToggle.isOn = data.musicEnabled;
                musicToggle.onValueChanged.AddListener(OnMusicToggled);
            }

            if (soundToggle != null)
            {
                soundToggle.isOn = data.soundEnabled;
                soundToggle.onValueChanged.AddListener(OnSoundToggled);
            }

            ShowSettings(false);
            ShowHowToPlay(false);

            foreach (GameObject sceneRoot in gameObject.scene.GetRootGameObjects())
            {
                SceneTextLocalizer.Apply(sceneRoot.transform);
            }
        }

        private void OnPlay()
        {
            AudioManager.Instance?.PlayButton();
            UiFeedback.Punch(this, playButton);
            // Play goes straight into a run in the chosen world. The old Green Hills
            // trail map is no longer part of the flow; worlds are picked with WORLD.
            AdventureSession.Clear();
            GameManager.LoadSceneClean(GameManager.GameSceneName);
        }

        private void ShowSettings(bool visible)
        {
            if (settingsPanel == null)
            {
                return;
            }

            settingsPanel.SetActive(visible);
            if (visible)
            {
                UiPointerRepair.RaiseOverlay(settingsPanel.transform);
            }
        }

        private void ShowHowToPlay(bool visible)
        {
            if (howToPlayPanel == null)
            {
                return;
            }

            howToPlayPanel.SetActive(visible);
            if (visible)
            {
                UiPointerRepair.RaiseOverlay(howToPlayPanel.transform);
            }
        }

        private void OnMusicToggled(bool enabled)
        {
            AudioManager.Instance?.SetMusicEnabled(enabled);
        }

        private void OnSoundToggled(bool enabled)
        {
            AudioManager.Instance?.SetSoundEnabled(enabled);
            AudioManager.Instance?.PlayButton();
        }

        private void OnQuit()
        {
            AudioManager.Instance?.PlayButton();
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>
        /// Builds a How to Play button and panel when the scene has none, so the
        /// existing Main Menu layout does not need a destructive rebuild.
        /// </summary>
        private void EnsureHowToPlayUi()
        {
            if (howToPlayPanel != null && howToPlayButton != null)
            {
                return;
            }

            Transform canvas = transform;
            Transform safe = canvas.Find("SafeArea");
            Transform buttonParent = safe != null ? safe : canvas;
            TMP_FontAsset font = ResolveFont();
            Sprite sprite = playButton != null ? playButton.GetComponent<Image>()?.sprite : null;
            Color buttonColour = new Color(0.18f, 0.42f, 0.28f, 1f);

            if (playButton != null)
            {
                var playImage = playButton.GetComponent<Image>();

                if (playImage != null)
                {
                    buttonColour = playImage.color;
                }
            }

            if (howToPlayButton == null)
            {
                howToPlayButton = CreateMenuButton(buttonParent, "HowToPlayButton", HowToPlayTitle,
                    new Vector2(0f, -360f), new Vector2(520f, 88f), buttonColour, 36f, font, sprite);
            }

            if (howToPlayPanel == null)
            {
                howToPlayPanel = CreateHowToPlayPanel(canvas, font, sprite);
            }
        }

        /// <summary>
        /// Steps through the ways to play (learning Bosnian from English or German,
        /// learning English or German from Bosnian). The menu reloads so every label
        /// follows the language the child already speaks.
        /// </summary>
        private void EnsureLanguageButton()
        {
            Transform safe = transform.Find("SafeArea");
            Transform parent = safe != null ? safe : transform;
            Sprite sprite = playButton != null ? playButton.GetComponent<Image>()?.sprite : null;

            _languageButton = CreateMenuButton(parent, "LanguageButton", LanguageLabel(),
                new Vector2(0f, -468f), new Vector2(520f, 76f), new Color(0.2f, 0.36f, 0.62f, 1f), 30f,
                ResolveFont(), sprite);
            _languageButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                LearningProgress.ToggleLanguage();
                RefreshLanguageLabel();
                GameManager.LoadSceneClean(GameManager.MainMenuSceneName);
            });
        }

        /// <summary>
        /// Copies the sound toggle so vibration gets the same look, placed just below it.
        /// Must run before the sound toggle's own listener is added.
        /// </summary>
        private void EnsureVibrationToggle()
        {
            if (soundToggle == null || _vibrationToggle != null)
            {
                return;
            }

            GameObject copy = Instantiate(soundToggle.gameObject, soundToggle.transform.parent);
            copy.name = "VibrationToggle";
            var rect = copy.GetComponent<RectTransform>();
            rect.anchoredPosition = soundToggle.GetComponent<RectTransform>().anchoredPosition + new Vector2(0f, -90f);

            _vibrationToggle = copy.GetComponent<Toggle>();
            _vibrationToggle.onValueChanged = new Toggle.ToggleEvent();
            _vibrationToggle.isOn = Haptics.Enabled;
            _vibrationToggle.onValueChanged.AddListener(enabled =>
            {
                Haptics.Enabled = enabled;
                AudioManager.Instance?.PlayButton();
                Haptics.Light();
            });

            var label = copy.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = "VIBRATION";
            }
        }

        /// <summary>
        /// Twemoji pictures are CC BY 4.0, which asks for a visible credit; it sits
        /// quietly at the bottom of Settings.
        /// </summary>
        private void EnsureCredits()
        {
            if (soundToggle == null || _creditsBuilt)
            {
                return;
            }

            _creditsBuilt = true;
            var go = new GameObject("Credits", typeof(RectTransform));
            go.transform.SetParent(soundToggle.transform.parent, false);
            var rect = go.GetComponent<RectTransform>();
            var sound = soundToggle.GetComponent<RectTransform>();
            rect.anchorMin = sound.anchorMin;
            rect.anchorMax = sound.anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, 44f);
            rect.anchoredPosition = new Vector2(0f, sound.anchoredPosition.y - 175f);

            // Along the bottom edge of the Settings card (the Close button's parent), under the button.
            Transform card = closeSettingsButton != null ? closeSettingsButton.transform.parent : null;
            if (card != null && card != settingsPanel?.transform)
            {
                go.transform.SetParent(card, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 6f);
            }

            var text = go.AddComponent<TextMeshProUGUI>();
            TMP_Text sample = soundToggle.GetComponentInChildren<TMP_Text>();
            if (sample != null)
            {
                text.font = sample.font;
            }

            text.text = "Pictures / Slike: Twemoji © Twitter, Inc. and contributors, CC BY 4.0\n" +
                        "Sounds / Zvukovi: Kenney, CC0  ·  Font: Titan One, OFL";
            text.fontSize = 20f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 1f, 1f, 0.7f);
            text.raycastTarget = false;
        }

        private static string LanguageLabel()
        {
            WordPack pack = WordPack.Load(LearningProgress.SelectedPack);
            int learned = LearningProgress.LearnedCount(pack);
            int total = pack != null ? pack.Words.Count : 0;

            string label;

            switch (LearningProgress.Language)
            {
                case LearningProgress.English: label = "UČIM ENGLESKI"; break;
                case LearningProgress.German: label = "UČIM NJEMAČKI"; break;
                default:
                    label = LearningProgress.HomeLanguage == LearningProgress.German
                        ? "ICH LERNE BOSNISCH"
                        : "LEARNING BOSNIAN";
                    break;
            }

            return $"{label}  ·  {learned}/{total}";
        }

        private static TMP_FontAsset ResolveFont()
        {
            TMP_Text existing = FindFirstObjectByType<TMP_Text>();
            return existing != null ? existing.font : TMP_Settings.defaultFontAsset;
        }

        private GameObject CreateHowToPlayPanel(Transform canvas, TMP_FontAsset font, Sprite sprite)
        {
            var panel = new GameObject("HowToPlayPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var dim = panel.GetComponent<Image>();
            dim.color = new Color(0.05f, 0.08f, 0.07f, 0.82f);
            dim.raycastTarget = true;
            panel.SetActive(false);

            var card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(panel.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(820f, 620f);
            var cardImage = card.GetComponent<Image>();
            cardImage.color = new Color(0.12f, 0.22f, 0.18f, 0.96f);
            if (sprite != null)
            {
                cardImage.sprite = sprite;
                cardImage.type = Image.Type.Sliced;
            }

            CreateLabel(card.transform, "Title", HowToPlayTitle, new Vector2(0f, 230f),
                new Vector2(720f, 70f), 46f, font, TextAlignmentOptions.Center);
            CreateLabel(card.transform, "Body", HowToPlayBody, new Vector2(0f, 20f),
                new Vector2(700f, 320f), 32f, font, TextAlignmentOptions.Center);

            Button close = CreateMenuButton(card.transform, "CloseHowToPlay", "GOT IT",
                new Vector2(0f, -230f), new Vector2(360f, 88f),
                new Color(0.22f, 0.55f, 0.34f, 1f), 36f, font, sprite);
            close.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                ShowHowToPlay(false);
            });

            return panel;
        }

        private static Button CreateMenuButton(Transform parent, string name, string label,
            Vector2 anchoredPosition, Vector2 size, Color colour, float fontSize,
            TMP_FontAsset font, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = colour;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            CreateLabel(go.transform, "Label", label, Vector2.zero, Vector2.zero, fontSize, font,
                TextAlignmentOptions.Center);

            var labelRect = go.transform.Find("Label") as RectTransform;
            if (labelRect != null)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            return go.GetComponent<Button>();
        }

        private static void CreateLabel(Transform parent, string name, string content,
            Vector2 anchoredPosition, Vector2 size, float fontSize, TMP_FontAsset font,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            if (font != null)
            {
                text.font = font;
            }
        }
    }
}
