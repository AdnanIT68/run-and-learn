using System.Collections;
using System.Collections.Generic;
using FootballRunner.Audio;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    /// <summary>
    /// The settings button opens this corner for parents. A small sum keeps young
    /// children out; behind it parents see what the child has learned, world by
    /// world, and switch sound and vibration. Store rules for family games expect
    /// such a gate in front of grown-up settings and, later, purchases.
    /// </summary>
    public class ParentsCornerUi : MonoBehaviour
    {
        private static readonly Color PanelColour = new Color(0.12f, 0.27f, 0.45f, 1f);
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color DarkText = new Color(0.16f, 0.2f, 0.26f, 1f);
        private static readonly Color MutedText = new Color(0.4f, 0.44f, 0.5f, 1f);
        private static readonly Color AnswerColour = new Color(0.98f, 0.62f, 0.2f, 1f);
        private static readonly Color OnColour = new Color(0.27f, 0.7f, 0.33f, 1f);
        private static readonly Color OffColour = new Color(0.55f, 0.58f, 0.62f, 1f);

        private readonly List<TMP_Text> _answerLabels = new List<TMP_Text>(3);
        private readonly List<TMP_Text[]> _worldRows = new List<TMP_Text[]>(8);
        private readonly int[] _answers = new int[3];
        private GameObject _panel;
        private GameObject _gate;
        private GameObject _corner;
        private TMP_Text _heading;
        private TMP_Text _backLabel;
        private TMP_Text _question;
        private TMP_Text _gateNote;
        private TMP_Text _summary;
        private TMP_Text[] _columnHeads;
        private Image _soundButton;
        private TMP_Text _soundLabel;
        private Image _vibrationButton;
        private TMP_Text _vibrationLabel;
        private TMP_Text _credits;
        private TMP_FontAsset _font;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public bool IsGateShowing => IsOpen && _gate.activeSelf;

        public bool IsCornerShowing => IsOpen && _corner.activeSelf;

        /// <summary>The right answer to the sum on the gate.</summary>
        public int GateAnswer { get; private set; }

        /// <summary>The progress line at the top of the corner, for tests.</summary>
        public string Summary => _summary != null ? _summary.text : string.Empty;

        public static ParentsCornerUi EnsureOnCanvas(Transform canvas, TMP_FontAsset font)
        {
            if (canvas == null)
            {
                return null;
            }

            var corner = canvas.GetComponent<ParentsCornerUi>();

            if (corner == null)
            {
                corner = canvas.gameObject.AddComponent<ParentsCornerUi>();
                corner._font = font;
                corner.Build();
            }

            return corner;
        }

        /// <summary>Opens on the gate; the corner itself shows once the sum is answered.</summary>
        public void Open()
        {
            if (_panel == null)
            {
                return;
            }

            NewQuestion();
            _gate.SetActive(true);
            _corner.SetActive(false);
            RefreshTexts();
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

        /// <summary>Same as tapping an answer on the gate. True when it opened the corner.</summary>
        public bool PressAnswer(int value)
        {
            if (!IsGateShowing)
            {
                return false;
            }

            if (value != GateAnswer)
            {
                AudioManager.Instance?.PlayButton();
                Haptics.Light();
                StartCoroutine(Wiggle(_question.rectTransform));
                NewQuestion();
                return false;
            }

            AudioManager.Instance?.PlayButton();
            _gate.SetActive(false);
            _corner.SetActive(true);
            RefreshTexts();
            return true;
        }

        public void ToggleSound()
        {
            bool enabled = !SaveManager.Data.soundEnabled;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSoundEnabled(enabled);
                AudioManager.Instance.PlayButton();
            }
            else
            {
                SaveManager.SetSoundEnabled(enabled);
            }

            RefreshTexts();
        }

        public void ToggleVibration()
        {
            Haptics.Enabled = !Haptics.Enabled;
            AudioManager.Instance?.PlayButton();
            Haptics.Light();
            RefreshTexts();
        }

        // ------------------------------------------------------------ gate

        private void NewQuestion()
        {
            // Sums past ten: easy for a parent, out of reach for most children this game is for.
            int a = Random.Range(5, 10);
            int b = Random.Range(4, 10);
            GateAnswer = a + b;
            _question.text = LearningProgress.Ui($"Koliko je {a} + {b}?", $"What is {a} + {b}?",
                $"Wie viel ist {a} + {b}?");

            // Two near misses around the answer, all three in a random order.
            var choices = new List<int> { GateAnswer - 2, GateAnswer - 1, GateAnswer + 1, GateAnswer + 2 };
            choices.RemoveAt(Random.Range(0, choices.Count));
            choices.RemoveAt(Random.Range(0, choices.Count));
            choices.Insert(Random.Range(0, choices.Count + 1), GateAnswer);

            for (int i = 0; i < _answers.Length; i++)
            {
                _answers[i] = choices[i];
                _answerLabels[i].text = _answers[i].ToString();
            }
        }

        // ------------------------------------------------------------ texts

        private void RefreshTexts()
        {
            _heading.text = LearningProgress.Ui("ZA RODITELJE", "FOR PARENTS", "FÜR ELTERN");
            _backLabel.text = LearningProgress.Ui("NAZAD", "BACK", "ZURÜCK");
            _gateNote.text = LearningProgress.Ui("Ovaj dio je za roditelje.", "This part is for grown-ups.",
                "Dieser Bereich ist für Eltern.");

            // The review world asks words from earlier worlds again; count each word once.
            var learnedIds = new HashSet<string>();
            int levels = 0;
            int stars = 0;
            string menuLanguage = LearningProgress.HomeLanguage;

            for (int i = 0; i < WordPack.PackOrder.Length; i++)
            {
                string name = WordPack.PackOrder[i];
                WordPack pack = WordPack.Load(name);
                int packLearned = LearningProgress.LearnedCount(pack);
                int packLevels = LearningProgress.LevelsDone(name);
                int packStars = LearningProgress.TotalStars(name);

                for (int w = 0; pack != null && w < pack.Words.Count; w++)
                {
                    if (LearningProgress.IsLearned(pack.Words[w].id))
                    {
                        learnedIds.Add(pack.Words[w].id);
                    }
                }

                levels += packLevels;
                stars += packStars;

                TMP_Text[] row = _worldRows[i];
                row[0].text = pack != null ? pack.Title(menuLanguage) : name;
                row[1].text = $"{packLearned}/{(pack != null ? pack.Words.Count : 0)}";
                row[2].text = $"{packLevels}/{LearningProgress.LevelsPerWorld}";
                row[3].text = $"{packStars}/{LearningProgress.LevelsPerWorld * 3}";
                row[0].color = LearningProgress.IsPackUnlocked(name) ? DarkText : MutedText;
            }

            int worlds = WordPack.PackOrder.Length;
            int learned = learnedIds.Count;
            _summary.text = LearningProgress.Ui(
                $"Uči: {LanguageName()}   ·   Naučene riječi: {learned}   ·   Nivoi: {levels}/{worlds * 5}   ·   Zvjezdice: {stars}/{worlds * 15}",
                $"Learning: {LanguageName()}   ·   Words learned: {learned}   ·   Levels: {levels}/{worlds * 5}   ·   Stars: {stars}/{worlds * 15}",
                $"Lernt: {LanguageName()}   ·   Gelernte Wörter: {learned}   ·   Level: {levels}/{worlds * 5}   ·   Sterne: {stars}/{worlds * 15}");

            _columnHeads[0].text = LearningProgress.Ui("SVIJET", "WORLD", "WELT");
            _columnHeads[1].text = LearningProgress.Ui("RIJEČI", "WORDS", "WÖRTER");
            _columnHeads[2].text = LearningProgress.Ui("NIVOI", "LEVELS", "LEVEL");
            _columnHeads[3].text = LearningProgress.Ui("ZVJEZDICE", "STARS", "STERNE");

            bool sound = SaveManager.Data.soundEnabled;
            _soundButton.color = sound ? OnColour : OffColour;
            _soundLabel.text = LearningProgress.Ui("ZVUK", "SOUND", "TON") + ": " + OnOff(sound);
            bool vibration = Haptics.Enabled;
            _vibrationButton.color = vibration ? OnColour : OffColour;
            _vibrationLabel.text = LearningProgress.Ui("VIBRACIJA", "VIBRATION", "VIBRATION") + ": " + OnOff(vibration);

            _credits.text = LearningProgress.Ui(
                "Slike: Twemoji © Twitter, Inc. i saradnici, CC BY 4.0\nZvukovi: Kenney, CC0  ·  Font: Titan One, OFL\nBez reklama, bez prikupljanja podataka.",
                "Pictures: Twemoji © Twitter, Inc. and contributors, CC BY 4.0\nSounds: Kenney, CC0  ·  Font: Titan One, OFL\nNo ads, no data collection.",
                "Bilder: Twemoji © Twitter, Inc. und Mitwirkende, CC BY 4.0\nTöne: Kenney, CC0  ·  Schrift: Titan One, OFL\nKeine Werbung, keine Datensammlung.");
        }

        private static string OnOff(bool on)
        {
            return on
                ? LearningProgress.Ui("UKLJUČEN", "ON", "AN")
                : LearningProgress.Ui("ISKLJUČEN", "OFF", "AUS");
        }

        private static string LanguageName()
        {
            switch (LearningProgress.Language)
            {
                case LearningProgress.English: return LearningProgress.Ui("engleski", "English", "Englisch");
                case LearningProgress.German: return LearningProgress.Ui("njemački", "German", "Deutsch");
                default: return LearningProgress.Ui("bosanski", "Bosnian", "Bosnisch");
            }
        }

        // ------------------------------------------------------------ layout

        private void Build()
        {
            _panel = new GameObject("ParentsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = PanelColour;

            _heading = CreateText(_panel.transform, "Heading", "", new Vector2(0f, 445f), new Vector2(1000f, 90f), 60f,
                Color.white);
            BuildBackButton();
            BuildGate();
            BuildCorner();
            _panel.SetActive(false);
        }

        private void BuildBackButton()
        {
            var back = new GameObject("Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            back.transform.SetParent(_panel.transform, false);
            var backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(60f, -50f);
            backRect.sizeDelta = new Vector2(220f, 90f);
            Image image = back.GetComponent<Image>();
            image.color = new Color(0.06f, 0.16f, 0.3f, 0.95f);
            Round(image, 1.6f);
            back.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                Close();
            });
            _backLabel = CreateText(back.transform, "Label", "", Vector2.zero, new Vector2(220f, 90f), 36f, Color.white);
        }

        private void BuildGate()
        {
            _gate = new GameObject("Gate", typeof(RectTransform));
            _gate.transform.SetParent(_panel.transform, false);
            Stretch(_gate.GetComponent<RectTransform>());

            _question = CreateText(_gate.transform, "Question", "", new Vector2(0f, 170f), new Vector2(1200f, 120f), 80f,
                Color.white);

            for (int i = 0; i < _answers.Length; i++)
            {
                int index = i;
                Image button = CreateButton(_gate.transform, "Answer" + i, new Vector2((i - 1) * 320f, -40f),
                    new Vector2(260f, 190f), AnswerColour, () => PressAnswer(_answers[index]));
                _answerLabels.Add(CreateText(button.transform, "Label", "", Vector2.zero, new Vector2(240f, 170f), 96f,
                    Color.white));
            }

            _gateNote = CreateText(_gate.transform, "Note", "", new Vector2(0f, -260f), new Vector2(1200f, 60f), 36f,
                new Color(1f, 1f, 1f, 0.75f));
        }

        private void BuildCorner()
        {
            _corner = new GameObject("Corner", typeof(RectTransform));
            _corner.transform.SetParent(_panel.transform, false);
            Stretch(_corner.GetComponent<RectTransform>());

            _summary = CreateText(_corner.transform, "Summary", "", new Vector2(0f, 345f), new Vector2(1700f, 60f), 38f,
                Color.white);

            // Progress, world by world.
            Image progress = CreateCard(_corner.transform, "Progress", new Vector2(-290f, -70f), new Vector2(1160f, 700f));
            float[] columns = { -330f, 110f, 290f, 470f };
            float[] widths = { 440f, 170f, 170f, 190f };
            _columnHeads = new TMP_Text[columns.Length];

            for (int c = 0; c < columns.Length; c++)
            {
                _columnHeads[c] = CreateText(progress.transform, "Head" + c, "", new Vector2(columns[c], 300f),
                    new Vector2(widths[c], 50f), 30f, MutedText);
            }

            _columnHeads[0].alignment = TextAlignmentOptions.Left;

            for (int i = 0; i < WordPack.PackOrder.Length; i++)
            {
                float y = 230f - i * 70f;
                var row = new TMP_Text[columns.Length];

                for (int c = 0; c < columns.Length; c++)
                {
                    row[c] = CreateText(progress.transform, $"World{i}_{c}", "", new Vector2(columns[c], y),
                        new Vector2(widths[c], 56f), 34f, DarkText);
                }

                row[0].alignment = TextAlignmentOptions.Left;
                _worldRows.Add(row);
            }

            // Settings and credits.
            Image settings = CreateCard(_corner.transform, "Settings", new Vector2(620f, -70f), new Vector2(560f, 700f));
            _soundButton = CreateButton(settings.transform, "Sound", new Vector2(0f, 230f), new Vector2(460f, 110f),
                OnColour, ToggleSound);
            _soundLabel = CreateText(_soundButton.transform, "Label", "", Vector2.zero, new Vector2(430f, 100f), 38f,
                Color.white);
            _vibrationButton = CreateButton(settings.transform, "Vibration", new Vector2(0f, 90f),
                new Vector2(460f, 110f), OnColour, ToggleVibration);
            _vibrationLabel = CreateText(_vibrationButton.transform, "Label", "", Vector2.zero, new Vector2(430f, 100f),
                38f, Color.white);

            _credits = CreateText(settings.transform, "Credits", "", new Vector2(0f, -200f), new Vector2(500f, 240f), 24f,
                MutedText);
            _credits.fontStyle = FontStyles.Normal;
            _credits.textWrappingMode = TextWrappingModes.Normal;
        }

        // ------------------------------------------------------------ helpers

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Round(Image image, float cornerScale = 1.2f)
        {
            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale;
        }

        private static Image CreateCard(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = CardColour;
            image.raycastTarget = false;
            Round(image);
            return image;
        }

        private static Image CreateButton(Transform parent, string name, Vector2 position, Vector2 size, Color colour,
            UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = colour;
            Round(image, 1.4f);
            go.GetComponent<Button>().onClick.AddListener(action);
            return image;
        }

        private TMP_Text CreateText(Transform parent, string name, string content, Vector2 position, Vector2 size,
            float fontSize, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
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
                float angle = Mathf.Sin(elapsed / seconds * Mathf.PI * 4f) * 4f * (1f - elapsed / seconds);
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
