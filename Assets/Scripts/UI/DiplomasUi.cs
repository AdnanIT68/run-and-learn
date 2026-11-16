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
    /// The child's diplomas, in the parents corner: one per finished world. A parent
    /// picks one, sees it, and saves and shares it (to print or send to family).
    /// </summary>
    public class DiplomasUi : MonoBehaviour
    {
        private static readonly Color PanelColour = new Color(0.12f, 0.24f, 0.4f, 1f);
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color Ink = new Color(0.16f, 0.2f, 0.3f, 1f);
        private static readonly Color Gold = new Color(0.98f, 0.72f, 0.2f, 1f);
        private static readonly Color Green = new Color(0.27f, 0.7f, 0.33f, 1f);

        private readonly List<GameObject> _worldButtons = new List<GameObject>();
        private GameObject _panel;
        private Transform _list;
        private RawImage _preview;
        private Texture2D _texture;
        private TMP_Text _title;
        private TMP_Text _note;
        private TMP_Text _shareLabel;
        private TMP_Text _closeLabel;
        private GameObject _share;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        /// <summary>The world shown in the preview, or null.</summary>
        public string Shown { get; private set; }

        /// <summary>The last diploma file written; for tests.</summary>
        public string LastSaved { get; private set; }

        public static DiplomasUi Create(Transform parent)
        {
            var go = new GameObject("Diplomas", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var ui = go.AddComponent<DiplomasUi>();
            ui.Build();
            return ui;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }

        public void Open()
        {
            Shown = null;
            _preview.enabled = false;
            _share.SetActive(false);
            Refresh();
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            AudioManager.Instance?.PlayButton();
            _panel.SetActive(false);
        }

        /// <summary>Shows a world's diploma, as tapping it in the list does.</summary>
        public bool Show(string pack)
        {
            if (!Diplomas.Earned().Contains(pack))
            {
                return false;
            }

            AudioManager.Instance?.PlayButton();

            if (_texture != null)
            {
                Destroy(_texture);
            }

            _texture = Diplomas.Render(pack, ChildName());
            _preview.texture = _texture;
            _preview.enabled = true;
            _share.SetActive(true);
            Shown = pack;
            return true;
        }

        /// <summary>Saves the shown diploma and opens the phone's share sheet where possible.</summary>
        public void SaveAndShare()
        {
            if (Shown == null)
            {
                return;
            }

            AudioManager.Instance?.PlayButton();
            LastSaved = Diplomas.Save(Shown, ChildName());

            if (LastSaved == null)
            {
                _note.text = LearningProgress.Ui("Diplomu nije bilo moguće sačuvati.", "The diploma could not be saved.",
                    "Das Diplom konnte nicht gespeichert werden.");
                return;
            }

            bool shared = Diplomas.Share(LastSaved);
            _note.text = shared
                ? LearningProgress.Ui("Izaberite gdje ćete je poslati ili odštampati.", "Choose where to send or print it.",
                    "Wählen Sie, wohin Sie es senden oder drucken.")
                : LearningProgress.Ui("Diploma je sačuvana u igri.", "The diploma is saved in the game.",
                    "Das Diplom ist im Spiel gespeichert.");
        }

        private static string ChildName()
        {
            return ProfilesUi.DisplayName(ProfileStore.Active);
        }

        private void Refresh()
        {
            _title.text = LearningProgress.Ui("DIPLOME", "DIPLOMAS", "DIPLOME");
            _closeLabel.text = LearningProgress.Ui("GOTOVO", "DONE", "FERTIG");
            _shareLabel.text = LearningProgress.Ui("SAČUVAJ I PODIJELI", "SAVE AND SHARE", "SPEICHERN UND TEILEN");

            foreach (GameObject button in _worldButtons)
            {
                Destroy(button);
            }

            _worldButtons.Clear();
            List<string> earned = Diplomas.Earned();
            _note.text = earned.Count == 0
                ? LearningProgress.Ui("Diploma se dobije kad dijete završi svih 5 nivoa jednog svijeta.",
                    "A diploma comes when the child finishes all 5 levels of a world.",
                    "Ein Diplom gibt es, wenn das Kind alle 5 Level einer Welt schafft.")
                : LearningProgress.Ui("Izaberite svijet.", "Pick a world.", "Wählen Sie eine Welt.");

            for (int i = 0; i < earned.Count; i++)
            {
                string pack = earned[i];
                WordPack words = WordPack.Load(pack);
                Image button = Box(_list, new Vector2(0f, 280f - i * 80f), new Vector2(440f, 70f), Gold);
                button.gameObject.AddComponent<Button>().onClick.AddListener(() => Show(pack));
                Text(button.transform, words != null ? words.Title(LearningProgress.HomeLanguage) : pack, Vector2.zero,
                    new Vector2(420f, 60f), 34f, Ink);
                _worldButtons.Add(button.gameObject);
            }
        }

        private void Build()
        {
            _panel = new GameObject("DiplomasPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = PanelColour;

            _title = Text(_panel.transform, "", new Vector2(0f, 445f), new Vector2(900f, 90f), 60f, Color.white);
            _note = Text(_panel.transform, "", new Vector2(0f, 375f), new Vector2(1500f, 50f), 30f, new Color(1f, 0.9f, 0.45f, 1f));

            var list = new GameObject("List", typeof(RectTransform));
            list.transform.SetParent(_panel.transform, false);
            list.GetComponent<RectTransform>().anchoredPosition = new Vector2(-620f, -40f);
            _list = list.transform;

            Image stage = Box(_panel.transform, new Vector2(240f, -20f), new Vector2(1180f, 836f), CardColour);
            stage.raycastTarget = false;
            var previewGo = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            previewGo.transform.SetParent(_panel.transform, false);
            var previewRect = previewGo.GetComponent<RectTransform>();
            previewRect.anchoredPosition = new Vector2(240f, 20f);
            previewRect.sizeDelta = new Vector2(1100f, 1100f * Diplomas.Height / Diplomas.Width);
            _preview = previewGo.GetComponent<RawImage>();
            _preview.enabled = false;

            Image share = Box(_panel.transform, new Vector2(240f, -400f), new Vector2(520f, 100f), Green);
            share.gameObject.AddComponent<Button>().onClick.AddListener(SaveAndShare);
            _shareLabel = Text(share.transform, "", Vector2.zero, new Vector2(500f, 90f), 38f, Color.white);
            _share = share.gameObject;

            Image close = Box(_panel.transform, new Vector2(780f, 445f), new Vector2(240f, 90f), Green);
            close.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _closeLabel = Text(close.transform, "", Vector2.zero, new Vector2(220f, 80f), 36f, Color.white);

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
            text.fontSizeMin = fontSize * 0.45f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = colour;
            text.raycastTarget = false;
            return text;
        }
    }
}
