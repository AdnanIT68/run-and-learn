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
    /// The children on this phone, inside the parents corner (so only past its gate):
    /// choose who plays, rename, remove (with a second tap to confirm) and add a child.
    /// Each child keeps their own progress, coins, stickers and settings.
    /// </summary>
    public class ProfilesUi : MonoBehaviour
    {
        private static readonly Color PanelColour = new Color(0.1f, 0.2f, 0.36f, 1f);
        private static readonly Color RowColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color ActiveRowColour = new Color(1f, 0.9f, 0.5f, 1f);
        private static readonly Color DarkText = new Color(0.16f, 0.2f, 0.26f, 1f);
        private static readonly Color Green = new Color(0.27f, 0.7f, 0.33f, 1f);
        private static readonly Color Blue = new Color(0.25f, 0.5f, 0.85f, 1f);
        private static readonly Color Red = new Color(0.85f, 0.3f, 0.28f, 1f);
        private static readonly Color Grey = new Color(0.55f, 0.58f, 0.62f, 1f);

        private class Row
        {
            public GameObject Root;
            public Image Background;
            public Image Portrait;
            public TMP_Text Name;
            public GameObject Choose;
            public TMP_Text ChooseLabel;
            public TMP_Text RenameLabel;
            public Image Remove;
            public TMP_Text RemoveLabel;
        }

        private readonly List<Row> _rows = new List<Row>();
        private GameObject _panel;
        private TMP_Text _title;
        private TMP_Text _hint;
        private Image _addButton;
        private TMP_Text _addLabel;
        private GameObject _editor;
        private TMP_InputField _input;
        private TMP_Text _saveLabel;
        private TMP_Text _cancelLabel;
        private TMP_Text _closeLabel;

        /// <summary>The child whose name is being typed; null when adding a new child.</summary>
        private string _editingId;

        private bool _editing;

        /// <summary>A child waiting for the second tap on REMOVE.</summary>
        private string _pendingRemove;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        /// <summary>True once a different child was chosen since the corner opened; the menu then reloads.</summary>
        public bool SwitchedSinceOpen { get; set; }

        public string PendingRemove => _pendingRemove;

        public static ProfilesUi Create(Transform parent)
        {
            var go = new GameObject("Profiles", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var ui = go.AddComponent<ProfilesUi>();
            ui.Build();
            return ui;
        }

        public void Open()
        {
            _pendingRemove = null;
            _editing = false;
            Refresh();
            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            _panel.SetActive(false);
        }

        /// <summary>Makes a child the one who plays.</summary>
        public bool SelectChild(string id)
        {
            AudioManager.Instance?.PlayButton();
            bool changed = id != ProfileStore.ActiveId;

            if (!ProfileStore.Switch(id))
            {
                return false;
            }

            SwitchedSinceOpen |= changed;
            _pendingRemove = null;
            Refresh();
            return true;
        }

        /// <summary>Adds a child with this name. False when the phone already has four children.</summary>
        public bool AddChild(string childName)
        {
            AudioManager.Instance?.PlayButton();
            bool added = ProfileStore.Add(childName) != null;
            _editing = false;
            Refresh();
            return added;
        }

        public bool RenameChild(string id, string childName)
        {
            AudioManager.Instance?.PlayButton();
            bool renamed = ProfileStore.Rename(id, childName);
            _editing = false;
            Refresh();
            return renamed;
        }

        /// <summary>First tap on REMOVE: the row asks "SIGURNO?". The last child cannot be removed.</summary>
        public bool RequestRemove(string id)
        {
            AudioManager.Instance?.PlayButton();

            if (ProfileStore.Profiles.Count <= 1 || ProfileStore.Find(id) == null)
            {
                return false;
            }

            _pendingRemove = id;
            Refresh();
            return true;
        }

        /// <summary>Second tap: the child and their progress are deleted.</summary>
        public bool ConfirmRemove()
        {
            if (_pendingRemove == null)
            {
                return false;
            }

            bool wasActive = _pendingRemove == ProfileStore.ActiveId;
            bool removed = ProfileStore.Remove(_pendingRemove);
            SwitchedSinceOpen |= removed && wasActive;
            _pendingRemove = null;
            Refresh();
            return removed;
        }

        /// <summary>"Dijete 2" for a child without a name.</summary>
        public static string DisplayName(ChildProfile profile)
        {
            if (profile == null)
            {
                return "";
            }

            if (!string.IsNullOrEmpty(profile.name))
            {
                return profile.name;
            }

            int number = ProfileStore.Number(profile.id);
            return LearningProgress.Ui($"Dijete {number}", $"Child {number}", $"Kind {number}");
        }

        // ------------------------------------------------------------ view

        private void Refresh()
        {
            IReadOnlyList<ChildProfile> profiles = ProfileStore.Profiles;
            _title.text = LearningProgress.Ui("DJECA", "CHILDREN", "KINDER");
            _hint.text = LearningProgress.Ui("Svako dijete ima svoj napredak, novčiće i naljepnice.",
                "Each child has their own progress, coins and stickers.",
                "Jedes Kind hat eigenen Fortschritt, Münzen und Sticker.");
            _closeLabel.text = LearningProgress.Ui("GOTOVO", "DONE", "FERTIG");

            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                bool used = i < profiles.Count;
                row.Root.SetActive(used);

                if (!used)
                {
                    continue;
                }

                ChildProfile profile = profiles[i];
                bool active = profile.id == ProfileStore.ActiveId;
                string character = active ? SaveManager.Data.selectedCharacterId : profile.characterId;
                row.Background.color = active ? ActiveRowColour : RowColour;
                row.Portrait.sprite = Player.CharacterRoster.Portrait(character);
                row.Portrait.enabled = row.Portrait.sprite != null;
                row.Name.text = DisplayName(profile) + (active ? LearningProgress.Ui("  ·  IGRA", "  ·  PLAYING", "  ·  SPIELT") : "");
                row.Choose.SetActive(!active);
                row.ChooseLabel.text = LearningProgress.Ui("IZABERI", "CHOOSE", "WÄHLEN");
                row.RenameLabel.text = LearningProgress.Ui("IME", "NAME", "NAME");

                bool confirming = _pendingRemove == profile.id;
                row.Remove.gameObject.SetActive(profiles.Count > 1);
                row.Remove.color = confirming ? Red : Grey;
                row.RemoveLabel.text = confirming
                    ? LearningProgress.Ui("SIGURNO?", "SURE?", "SICHER?")
                    : LearningProgress.Ui("OBRIŠI", "REMOVE", "LÖSCHEN");
            }

            bool roomForMore = profiles.Count < ProfileStore.MaxProfiles;
            _addButton.gameObject.SetActive(roomForMore && !_editing);
            _addLabel.text = LearningProgress.Ui("+ DODAJ DIJETE", "+ ADD A CHILD", "+ KIND HINZUFÜGEN");
            // The name field is only made when a parent starts typing (see StartEditing).
            if (_editor != null)
            {
                _editor.SetActive(_editing);
                _saveLabel.text = LearningProgress.Ui("SPREMI", "SAVE", "SPEICHERN");
                _cancelLabel.text = LearningProgress.Ui("ODUSTANI", "CANCEL", "ABBRECHEN");
                ((TMP_Text)_input.placeholder).text = LearningProgress.Ui("Ime djeteta", "Child's name", "Name des Kindes");
            }
        }

        private void StartEditing(string id)
        {
            AudioManager.Instance?.PlayButton();

            if (_editor == null)
            {
                BuildEditor();
            }

            _editingId = id;
            _editing = true;
            _pendingRemove = null;
            ChildProfile profile = id != null ? ProfileStore.Find(id) : null;
            _input.text = profile != null ? profile.name : "";
            Refresh();
            _input.Select();
            _input.ActivateInputField();
        }

        private void SaveEditing()
        {
            if (_editingId == null)
            {
                AddChild(_input.text);
            }
            else
            {
                RenameChild(_editingId, _input.text);
            }
        }

        // ------------------------------------------------------------ layout

        private void Build()
        {
            _panel = new GameObject("ProfilesPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image background = _panel.GetComponent<Image>();
            background.color = PanelColour;
            background.raycastTarget = true;

            _title = Text(_panel.transform, "Title", new Vector2(0f, 420f), new Vector2(900f, 90f), 60f, Color.white);
            _hint = Text(_panel.transform, "Hint", new Vector2(0f, 350f), new Vector2(1400f, 50f), 30f,
                new Color(1f, 0.9f, 0.45f, 1f));

            for (int i = 0; i < ProfileStore.MaxProfiles; i++)
            {
                _rows.Add(BuildRow(i, new Vector2(0f, 230f - i * 140f)));
            }

            _addButton = Button(_panel.transform, "Add", new Vector2(0f, -340f), new Vector2(560f, 100f), Blue,
                () => StartEditing(null));
            _addLabel = Text(_addButton.transform, "Label", Vector2.zero, new Vector2(520f, 90f), 38f, Color.white);

            Image close = Button(_panel.transform, "Done", new Vector2(780f, 420f), new Vector2(240f, 90f), Green, () =>
            {
                AudioManager.Instance?.PlayButton();
                Close();
            });
            _closeLabel = Text(close.transform, "Label", Vector2.zero, new Vector2(220f, 80f), 36f, Color.white);

            _panel.SetActive(false);
        }

        private Row BuildRow(int index, Vector2 position)
        {
            var row = new Row();
            Image card = Card(_panel.transform, "Child" + index, position, new Vector2(1400f, 120f));
            row.Root = card.gameObject;
            row.Background = card;

            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            portraitGo.transform.SetParent(card.transform, false);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchoredPosition = new Vector2(-620f, 0f);
            portraitRect.sizeDelta = new Vector2(100f, 100f);
            row.Portrait = portraitGo.GetComponent<Image>();
            row.Portrait.preserveAspect = true;
            row.Portrait.raycastTarget = false;

            row.Name = Text(card.transform, "Name", new Vector2(-300f, 0f), new Vector2(520f, 90f), 42f, DarkText);
            row.Name.alignment = TextAlignmentOptions.Left;

            int captured = index;
            Image choose = Button(card.transform, "Choose", new Vector2(160f, 0f), new Vector2(240f, 86f), Green,
                () => SelectChild(ProfileStore.Profiles[captured].id));
            row.Choose = choose.gameObject;
            row.ChooseLabel = Text(choose.transform, "Label", Vector2.zero, new Vector2(220f, 76f), 32f, Color.white);

            Image rename = Button(card.transform, "Rename", new Vector2(410f, 0f), new Vector2(200f, 86f), Blue,
                () => StartEditing(ProfileStore.Profiles[captured].id));
            row.RenameLabel = Text(rename.transform, "Label", Vector2.zero, new Vector2(180f, 76f), 32f, Color.white);

            row.Remove = Button(card.transform, "Remove", new Vector2(600f, 0f), new Vector2(160f, 86f), Grey, () =>
            {
                string id = ProfileStore.Profiles[captured].id;

                if (_pendingRemove == id)
                {
                    ConfirmRemove();
                }
                else
                {
                    RequestRemove(id);
                }
            });
            row.RemoveLabel = Text(row.Remove.transform, "Label", Vector2.zero, new Vector2(150f, 76f), 26f, Color.white);
            return row;
        }

        private void BuildEditor()
        {
            _editor = new GameObject("NameEditor", typeof(RectTransform));
            _editor.transform.SetParent(_panel.transform, false);
            var rect = _editor.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, -340f);
            rect.sizeDelta = new Vector2(1400f, 110f);

            GameObject field = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            field.name = "NameField";
            field.transform.SetParent(_editor.transform, false);
            var fieldRect = field.GetComponent<RectTransform>();
            fieldRect.anchoredPosition = new Vector2(-230f, 0f);
            fieldRect.sizeDelta = new Vector2(700f, 96f);
            _input = field.GetComponent<TMP_InputField>();
            _input.characterLimit = 16;
            _input.lineType = TMP_InputField.LineType.SingleLine;
            _input.onSubmit.AddListener(_ => SaveEditing());

            foreach (TMP_Text text in field.GetComponentsInChildren<TMP_Text>(true))
            {
                GameFonts.UseDisplay(text);
                text.fontSize = 44f;
                text.alignment = TextAlignmentOptions.MidlineLeft;
            }

            Image save = Button(_editor.transform, "Save", new Vector2(290f, 0f), new Vector2(260f, 96f), Green, SaveEditing);
            _saveLabel = Text(save.transform, "Label", Vector2.zero, new Vector2(240f, 86f), 36f, Color.white);
            Image cancel = Button(_editor.transform, "Cancel", new Vector2(570f, 0f), new Vector2(260f, 96f), Grey, () =>
            {
                AudioManager.Instance?.PlayButton();
                _editing = false;
                Refresh();
            });
            _cancelLabel = Text(cancel.transform, "Label", Vector2.zero, new Vector2(240f, 86f), 36f, Color.white);
        }

        private static Image Card(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = RowColour;
            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.4f;
            image.raycastTarget = false;
            return image;
        }

        private static Image Button(Transform parent, string name, Vector2 position, Vector2 size, Color colour,
            UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = colour;
            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.4f;

            if (action != null)
            {
                go.GetComponent<Button>().onClick.AddListener(action);
            }

            return image;
        }

        private static TMP_Text Text(Transform parent, string name, Vector2 position, Vector2 size, float fontSize,
            Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
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
