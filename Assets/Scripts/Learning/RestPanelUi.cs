using System;
using FootballRunner.Audio;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// "Vrijeme je za odmor!": covers the whole screen when the daily time limit is used
    /// up, with Pepi and a single way out. In a run that way is the menu; in the menu
    /// it is the parents corner, behind its sum gate, where the limit can be changed.
    /// </summary>
    public class RestPanelUi : MonoBehaviour
    {
        private GameObject _panel;
        private TMP_Text _title;
        private TMP_Text _note;
        private TMP_Text _buttonLabel;
        private Action _onButton;

        public bool IsShowing => _panel != null && _panel.activeSelf;

        public static RestPanelUi EnsureOnCanvas(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var panel = canvas.GetComponent<RestPanelUi>();
            return panel != null ? panel : canvas.gameObject.AddComponent<RestPanelUi>();
        }

        /// <summary>Shows the panel, says Pepi's line, and gives one button.</summary>
        public void Show(string buttonText, Action onButton)
        {
            if (_panel == null)
            {
                Build();
            }

            _title.text = LearningProgress.Ui("Vrijeme je za odmor!", "Time for a break!", "Zeit für eine Pause!");
            _note.text = LearningProgress.Ui(
                "Danas je dosta igre. Vidimo se sutra!\nRoditelj može promijeniti vrijeme u kutku za roditelje.",
                "That's enough play for today. See you tomorrow!\nA parent can change the time in the parents corner.",
                "Genug gespielt für heute. Bis morgen!\nEltern können die Zeit im Elternbereich ändern.");
            _buttonLabel.text = buttonText;
            _onButton = onButton;
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            AudioManager.Instance?.PlayVoice(PepiVoice.Line(PepiVoice.Rest));
        }

        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        /// <summary>Same as tapping the panel's button.</summary>
        public void PressButton()
        {
            AudioManager.Instance?.PlayButton();
            _onButton?.Invoke();
        }

        private void Build()
        {
            _panel = new GameObject("RestPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var background = _panel.GetComponent<Image>();
            background.color = new Color(0.12f, 0.2f, 0.36f, 0.97f);
            background.raycastTarget = true;

            Picture(WordPack.Icon("parrot"), new Vector2(0f, 230f), 220f);
            _title = Text("Title", new Vector2(0f, 60f), new Vector2(1400f, 110f), 80f, Color.white);
            _note = Text("Note", new Vector2(0f, -70f), new Vector2(1400f, 130f), 36f, new Color(1f, 0.9f, 0.55f, 1f));

            var buttonGo = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            buttonGo.transform.SetParent(_panel.transform, false);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(0f, -250f);
            buttonRect.sizeDelta = new Vector2(460f, 120f);
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.sprite = RuntimeSprites.RoundedRect();
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = new Color(0.98f, 0.55f, 0.18f, 1f);
            buttonGo.GetComponent<Button>().onClick.AddListener(PressButton);
            _buttonLabel = Text("Label", Vector2.zero, new Vector2(430f, 100f), 44f, Color.white, buttonGo.transform);
        }

        private void Picture(Sprite sprite, Vector2 position, float size)
        {
            var go = new GameObject("Pepi", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_panel.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
        }

        private TMP_Text Text(string name, Vector2 position, Vector2 size, float fontSize, Color colour,
            Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : _panel.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            GameFonts.UseDisplay(text);
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.5f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = colour;
            text.raycastTarget = false;
            return text;
        }
    }
}
