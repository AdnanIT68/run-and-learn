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
    /// "Za štampu" in the parents corner: the child's diplomas (one per finished world)
    /// and worksheets for any world (letters to trace, pictures to join with their words).
    /// A parent picks one, sees it, and saves and shares it (to print or send to family).
    /// </summary>
    public class DiplomasUi : MonoBehaviour
    {
        public enum Mode
        {
            Diplomas,
            Worksheets
        }

        private static readonly Color PanelColour = new Color(0.12f, 0.24f, 0.4f, 1f);
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color Ink = new Color(0.16f, 0.2f, 0.3f, 1f);
        private static readonly Color Gold = new Color(0.98f, 0.72f, 0.2f, 1f);
        private static readonly Color Green = new Color(0.27f, 0.7f, 0.33f, 1f);
        private static readonly Color Blue = new Color(0.25f, 0.5f, 0.85f, 1f);
        private static readonly Color Idle = new Color(0.2f, 0.32f, 0.5f, 1f);

        /// <summary>A portrait worksheet's height in the preview, clear of the share button below it.</summary>
        private const float PreviewHeight = 690f;

        private readonly List<GameObject> _worldButtons = new List<GameObject>();
        private GameObject _panel;
        private Transform _list;
        private RawImage _preview;
        private RectTransform _previewRect;
        private Texture2D _texture;
        private TMP_Text _title;
        private TMP_Text _note;
        private TMP_Text _shareLabel;
        private TMP_Text _closeLabel;
        private GameObject _share;
        private Image _diplomasTab;
        private TMP_Text _diplomasTabLabel;
        private Image _sheetsTab;
        private TMP_Text _sheetsTabLabel;
        private GameObject _kinds;
        private Image _lettersButton;
        private TMP_Text _lettersLabel;
        private Image _matchButton;
        private TMP_Text _matchLabel;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public Mode Current { get; private set; }

        /// <summary>The worksheet drawn when a world is picked in the worksheets tab.</summary>
        public Worksheets.Kind SheetKind { get; private set; }

        /// <summary>The world shown in the preview, or null.</summary>
        public string Shown { get; private set; }

        /// <summary>The picture in the preview; for tests.</summary>
        public Texture2D Preview => _texture;

        /// <summary>The last file written; for tests.</summary>
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
            Current = Mode.Diplomas;
            ClearPreview();
            Refresh();
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            AudioManager.Instance?.PlayButton();
            _panel.SetActive(false);
        }

        /// <summary>Same as tapping a tab: the diplomas or the worksheets.</summary>
        public void ShowMode(Mode mode)
        {
            AudioManager.Instance?.PlayButton();
            Current = mode;
            ClearPreview();
            Refresh();
        }

        /// <summary>Same as tapping LETTERS or MATCH in the worksheets tab; redraws the world shown.</summary>
        public void ShowSheetKind(Worksheets.Kind kind)
        {
            AudioManager.Instance?.PlayButton();
            SheetKind = kind;
            string shown = Shown;
            Refresh();

            if (shown != null)
            {
                Show(shown);
            }
        }

        /// <summary>Shows a world's diploma or worksheet, as tapping it in the list does.</summary>
        public bool Show(string pack)
        {
            if (Current == Mode.Diplomas && !Diplomas.Earned().Contains(pack))
            {
                return false;
            }

            AudioManager.Instance?.PlayButton();

            if (_texture != null)
            {
                Destroy(_texture);
            }

            _texture = Current == Mode.Diplomas ? Diplomas.Render(pack, ChildName()) : Worksheets.Render(pack, SheetKind);
            float aspect = (float)_texture.width / _texture.height;
            _previewRect.sizeDelta = aspect >= 1f
                ? new Vector2(1100f, 1100f / aspect)
                : new Vector2(PreviewHeight * aspect, PreviewHeight);
            _previewRect.anchoredPosition = new Vector2(240f, aspect >= 1f ? 20f : 42f);
            _preview.texture = _texture;
            _preview.enabled = true;
            _share.SetActive(true);
            Shown = pack;
            return true;
        }

        /// <summary>Saves what is shown and opens the phone's share sheet where possible.</summary>
        public void SaveAndShare()
        {
            if (Shown == null)
            {
                return;
            }

            AudioManager.Instance?.PlayButton();
            // The picture on screen itself: a new drawing could differ (the match sheet mixes its words).
            string path = Current == Mode.Diplomas ? Diplomas.FilePath(Shown, ChildName()) : Worksheets.FilePath(Shown, SheetKind);
            LastSaved = _texture != null ? OffscreenPicture.WritePng(_texture, path) : null;

            if (LastSaved == null)
            {
                _note.text = LearningProgress.Ui("Sliku nije bilo moguće sačuvati.", "The picture could not be saved.",
                    "Das Bild konnte nicht gespeichert werden.");
                return;
            }

            bool shared = OffscreenPicture.Share(LastSaved);
            _note.text = shared
                ? LearningProgress.Ui("Izaberite gdje ćete je poslati ili odštampati.", "Choose where to send or print it.",
                    "Wählen Sie, wohin Sie es senden oder drucken.")
                : LearningProgress.Ui("Sačuvano u igri.", "Saved in the game.", "Im Spiel gespeichert.");
        }

        private static string ChildName()
        {
            return ProfilesUi.DisplayName(ProfileStore.Active);
        }

        private void ClearPreview()
        {
            Shown = null;
            _preview.enabled = false;
            _share.SetActive(false);
        }

        private void Refresh()
        {
            bool sheets = Current == Mode.Worksheets;
            _title.text = LearningProgress.Ui("ZA ŠTAMPU", "TO PRINT", "ZUM DRUCKEN");
            _closeLabel.text = LearningProgress.Ui("GOTOVO", "DONE", "FERTIG");
            _shareLabel.text = LearningProgress.Ui("SAČUVAJ I PODIJELI", "SAVE AND SHARE", "SPEICHERN UND TEILEN");
            _diplomasTabLabel.text = LearningProgress.Ui("DIPLOME", "DIPLOMAS", "DIPLOME");
            _sheetsTabLabel.text = LearningProgress.Ui("RADNI LISTOVI", "WORKSHEETS", "ARBEITSBLÄTTER");
            _diplomasTab.color = sheets ? Idle : Gold;
            _sheetsTab.color = sheets ? Blue : Idle;
            _kinds.SetActive(sheets);
            _lettersLabel.text = LearningProgress.Ui("SLOVA", "LETTERS", "BUCHSTABEN");
            _matchLabel.text = LearningProgress.Ui("POVEŽI", "MATCH", "VERBINDEN");
            _lettersButton.color = SheetKind == Worksheets.Kind.Letters ? Green : Idle;
            _matchButton.color = SheetKind == Worksheets.Kind.Match ? Green : Idle;

            foreach (GameObject button in _worldButtons)
            {
                Destroy(button);
            }

            _worldButtons.Clear();
            List<string> worlds = sheets ? new List<string>(WordPack.PackOrder) : Diplomas.Earned();

            if (sheets)
            {
                _note.text = LearningProgress.Ui("Izaberite list i svijet, pa ga odštampajte.",
                    "Pick a sheet and a world, then print it.", "Wählen Sie Blatt und Welt und drucken Sie es.");
            }
            else
            {
                _note.text = worlds.Count == 0
                    ? LearningProgress.Ui("Diploma se dobije kad dijete završi svih 5 nivoa jednog svijeta.",
                        "A diploma comes when the child finishes all 5 levels of a world.",
                        "Ein Diplom gibt es, wenn das Kind alle 5 Level einer Welt schafft.")
                    : LearningProgress.Ui("Izaberite svijet.", "Pick a world.", "Wählen Sie eine Welt.");
            }

            // Ten worlds need smaller rows than a few diplomas.
            float rowStep = worlds.Count > 7 ? 66f : 80f;
            float rowHeight = rowStep - 10f;
            float firstY = sheets ? 210f : 280f;

            for (int i = 0; i < worlds.Count; i++)
            {
                string pack = worlds[i];
                WordPack words = WordPack.Load(pack);
                Image button = Box(_list, new Vector2(0f, firstY - i * rowStep), new Vector2(440f, rowHeight),
                    sheets ? CardColour : Gold);
                button.gameObject.AddComponent<Button>().onClick.AddListener(() => Show(pack));
                Text(button.transform, words != null ? words.Title(LearningProgress.HomeLanguage) : pack, Vector2.zero,
                    new Vector2(420f, rowHeight - 10f), 34f, Ink);
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

            _title = Text(_panel.transform, "", new Vector2(0f, 445f), new Vector2(700f, 90f), 60f, Color.white);
            _note = Text(_panel.transform, "", new Vector2(240f, 375f), new Vector2(1180f, 50f), 30f,
                new Color(1f, 0.9f, 0.45f, 1f));

            _diplomasTab = Box(_panel.transform, new Vector2(-760f, 445f), new Vector2(250f, 84f), Gold);
            _diplomasTab.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowMode(Mode.Diplomas));
            _diplomasTabLabel = Text(_diplomasTab.transform, "", Vector2.zero, new Vector2(230f, 74f), 34f, Color.white);
            _sheetsTab = Box(_panel.transform, new Vector2(-490f, 445f), new Vector2(270f, 84f), Idle);
            _sheetsTab.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowMode(Mode.Worksheets));
            _sheetsTabLabel = Text(_sheetsTab.transform, "", Vector2.zero, new Vector2(250f, 74f), 34f, Color.white);

            var list = new GameObject("List", typeof(RectTransform));
            list.transform.SetParent(_panel.transform, false);
            list.GetComponent<RectTransform>().anchoredPosition = new Vector2(-620f, -40f);
            _list = list.transform;

            // The two kinds of worksheet, above the list in the worksheets tab.
            _kinds = new GameObject("Kinds", typeof(RectTransform));
            _kinds.transform.SetParent(_panel.transform, false);
            _kinds.GetComponent<RectTransform>().anchoredPosition = new Vector2(-620f, 300f);
            _lettersButton = Box(_kinds.transform, new Vector2(-115f, 0f), new Vector2(210f, 76f), Green);
            _lettersButton.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowSheetKind(Worksheets.Kind.Letters));
            _lettersLabel = Text(_lettersButton.transform, "", Vector2.zero, new Vector2(195f, 66f), 32f, Color.white);
            _matchButton = Box(_kinds.transform, new Vector2(115f, 0f), new Vector2(210f, 76f), Idle);
            _matchButton.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowSheetKind(Worksheets.Kind.Match));
            _matchLabel = Text(_matchButton.transform, "", Vector2.zero, new Vector2(195f, 66f), 32f, Color.white);

            Image stage = Box(_panel.transform, new Vector2(240f, -20f), new Vector2(1180f, 836f), CardColour);
            stage.raycastTarget = false;
            var previewGo = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            previewGo.transform.SetParent(_panel.transform, false);
            _previewRect = previewGo.GetComponent<RectTransform>();
            _previewRect.anchoredPosition = new Vector2(240f, 20f);
            _previewRect.sizeDelta = new Vector2(1100f, 1100f * Diplomas.Height / Diplomas.Width);
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
