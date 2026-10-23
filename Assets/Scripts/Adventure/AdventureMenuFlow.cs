using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Adventure
{
    /// <summary>
    /// Full-screen character select and Green Hills world map, built on the
    /// existing Main Menu canvas so no extra scene is required for this slice.
    /// </summary>
    public class AdventureMenuFlow : MonoBehaviour
    {
        private static readonly string[] LevelNames =
        {
            "The Journey Begins",
            "Lost Stars",
            "Broken Path",
            "Forest Entrance",
            "Green Hills Finale"
        };

        private GameObject _characterPanel;
        private GameObject _worldPanel;
        private Transform _characterRow;
        private int _selectedSlot;
        private Button _chooseButton;
        private TMP_Text _chooseHint;
        private AdventureVisuals _visuals;
        private readonly Button[] _levelButtons = new Button[AdventureSession.LevelCount];
        private readonly TMP_Text[] _levelStars = new TMP_Text[AdventureSession.LevelCount];

        private TMP_Text _worldFooter;

        public static void Attach(MonoBehaviour host)
        {
            if (host == null)
            {
                return;
            }

            var flow = host.GetComponent<AdventureMenuFlow>();
            if (flow == null)
            {
                flow = host.gameObject.AddComponent<AdventureMenuFlow>();
            }

            flow.Build();
            if (AdventureSession.OpenWorldMapOnMenu)
            {
                flow.ShowWorldMap();
            }
        }

        public void OpenFromMainMenu()
        {
            AdventureSession.PrepareMenus();
            ShowCharacterSelect();
        }

        private void Build()
        {
            if (_characterPanel != null)
            {
                return;
            }

            _visuals = Resources.Load<AdventureVisuals>("Adventure/AdventureVisuals");
            AdventureSession.PrepareMenus();
            _selectedSlot = IndexOf(AdventureSession.SelectedCharacterId);
            BuildCharacterSelect();
            BuildWorldMap();
            _characterPanel.SetActive(false);
            _worldPanel.SetActive(false);
        }

        public void ShowCharacterSelect()
        {
            Build();
            _worldPanel.SetActive(false);
            _characterPanel.SetActive(true);
            _characterPanel.transform.SetAsLastSibling();
            RefreshCharacterCards();
        }

        public void ShowWorldMap()
        {
            Build();
            AdventureSession.OpenWorldMapOnMenu = false;
            _characterPanel.SetActive(false);
            _worldPanel.SetActive(true);
            _worldPanel.transform.SetAsLastSibling();
            RefreshWorldMap();
        }

        private void BuildCharacterSelect()
        {
            _characterPanel = CreateFullPanel("CharacterSelect", new Color(0.18f, 0.42f, 0.28f, 1f));
            CreateLabel(_characterPanel.transform, "Kicker", "CHOOSE YOUR RUNNER",
                new Vector2(0f, 280f), new Vector2(900f, 48f), 28f, new Color(1f, 1f, 1f, 0.85f));
            CreateLabel(_characterPanel.transform, "Title", "WHO WILL RUN GREEN HILLS?",
                new Vector2(0f, 220f), new Vector2(1100f, 64f), 44f, Color.white);

            var row = new GameObject("Slots", typeof(RectTransform));
            row.transform.SetParent(_characterPanel.transform, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = new Vector2(0f, 10f);
            rowRect.sizeDelta = new Vector2(1180f, 280f);
            _characterRow = row.transform;

            float[] xs = { -390f, 0f, 390f };
            for (int i = 0; i < CharacterRoster.Slots.Length; i++)
            {
                CreateCharacterCard(_characterRow, i, xs[i]);
            }

            _chooseHint = CreateLabel(_characterPanel.transform, "Hint", "",
                new Vector2(0f, -180f), new Vector2(900f, 40f), 24f, new Color(1f, 1f, 1f, 0.85f));
            _chooseButton = CreateButton(_characterPanel.transform, "Choose", "CHOOSE",
                new Vector2(0f, -250f), new Vector2(360f, 96f), new Color(0.99f, 0.76f, 0.18f, 1f), 36f);
            _chooseButton.onClick.AddListener(OnChooseCharacter);

            Button back = CreateButton(_characterPanel.transform, "Back", "BACK",
                new Vector2(-620f, 280f), new Vector2(180f, 72f), new Color(0.12f, 0.22f, 0.16f, 0.9f), 28f);
            back.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                _characterPanel.SetActive(false);
            });
        }

        private void CreateCharacterCard(Transform parent, int index, float x)
        {
            CharacterRoster.Slot slot = CharacterRoster.Slots[index];
            var card = new GameObject("Slot_" + slot.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            var rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(340f, 260f);
            var image = card.GetComponent<Image>();
            image.color = slot.Unlocked ? new Color(0.16f, 0.34f, 0.22f, 0.96f) : new Color(0.12f, 0.14f, 0.13f, 0.92f);

            if (slot.Unlocked && _visuals != null && _visuals.rukajjaPortrait != null)
            {
                var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                portrait.transform.SetParent(card.transform, false);
                var pRect = portrait.GetComponent<RectTransform>();
                pRect.anchorMin = new Vector2(0.5f, 0.5f);
                pRect.anchorMax = new Vector2(0.5f, 0.5f);
                pRect.anchoredPosition = new Vector2(0f, 36f);
                pRect.sizeDelta = new Vector2(140f, 160f);
                var pImage = portrait.GetComponent<Image>();
                pImage.sprite = _visuals.rukajjaPortrait;
                pImage.preserveAspect = true;
                pImage.raycastTarget = false;
            }

            CreateLabel(card.transform, "Name", slot.DisplayName,
                new Vector2(0f, slot.Unlocked ? -70f : 20f), new Vector2(300f, 40f), 26f, Color.white);
            CreateLabel(card.transform, "Blurb", slot.Unlocked ? slot.Blurb : "LOCKED",
                new Vector2(0f, slot.Unlocked ? -110f : -20f), new Vector2(300f, 36f), 20f,
                new Color(1f, 1f, 1f, 0.8f));

            int captured = index;
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                _selectedSlot = captured;
                RefreshCharacterCards();
            });
        }

        private void RefreshCharacterCards()
        {
            CharacterRoster.Slot slot = CharacterRoster.Slots[Mathf.Clamp(_selectedSlot, 0, CharacterRoster.Slots.Length - 1)];
            if (_chooseButton != null)
            {
                _chooseButton.interactable = slot.Unlocked;
            }

            if (_chooseHint != null)
            {
                _chooseHint.text = slot.Unlocked ? "Rukajja is ready to run." : "This runner is not available yet.";
            }

            if (_characterRow == null)
            {
                return;
            }

            for (int i = 0; i < _characterRow.childCount; i++)
            {
                var image = _characterRow.GetChild(i).GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                bool selected = i == _selectedSlot;
                bool unlocked = CharacterRoster.Slots[i].Unlocked;
                image.color = selected
                    ? new Color(0.24f, 0.52f, 0.30f, 1f)
                    : unlocked
                        ? new Color(0.16f, 0.34f, 0.22f, 0.96f)
                        : new Color(0.12f, 0.14f, 0.13f, 0.92f);
            }
        }

        private void OnChooseCharacter()
        {
            CharacterRoster.Slot slot = CharacterRoster.Slots[Mathf.Clamp(_selectedSlot, 0, CharacterRoster.Slots.Length - 1)];
            if (!slot.Unlocked)
            {
                return;
            }

            AudioManager.Instance?.PlayButton();
            AdventureSession.SelectCharacter(slot.Id);
            ShowWorldMap();
        }

        private void BuildWorldMap()
        {
            _worldPanel = CreateFullPanel("WorldMap", new Color(0.40f, 0.72f, 0.42f, 1f));
            CreateLabel(_worldPanel.transform, "World", "WORLD 1",
                new Vector2(0f, 300f), new Vector2(700f, 40f), 28f, new Color(1f, 1f, 1f, 0.86f));
            CreateLabel(_worldPanel.transform, "Title", "GREEN HILLS",
                new Vector2(0f, 240f), new Vector2(900f, 64f), 52f, Color.white);
            CreateLabel(_worldPanel.transform, "Prompt", "Choose a trail.",
                new Vector2(0f, 184f), new Vector2(700f, 36f), 24f, new Color(1f, 1f, 1f, 0.88f));
            _worldFooter = CreateLabel(_worldPanel.transform, "Footer", "Complete a trail to open the next one.",
                new Vector2(0f, -250f), new Vector2(1000f, 40f), 22f, new Color(1f, 1f, 1f, 0.9f));

            float[] xs = { -520f, -260f, 0f, 260f, 520f };
            float[] ys = { -20f, 40f, -30f, 50f, -10f };
            for (int i = 0; i < AdventureSession.LevelCount; i++)
            {
                CreateLevelNode(_worldPanel.transform, i, xs[i], ys[i]);
            }

            Button back = CreateButton(_worldPanel.transform, "Back", "BACK",
                new Vector2(-620f, 280f), new Vector2(180f, 72f), new Color(0.12f, 0.28f, 0.16f, 0.92f), 28f);
            back.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                ShowCharacterSelect();
            });
        }

        private void CreateLevelNode(Transform parent, int index, float x, float y)
        {
            var node = new GameObject("Level_" + (index + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            node.transform.SetParent(parent, false);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(210f, 210f);
            node.GetComponent<Image>().color = new Color(0.16f, 0.38f, 0.22f, 0.95f);

            CreateLabel(node.transform, "Number", (index + 1).ToString(),
                new Vector2(0f, 48f), new Vector2(80f, 48f), 40f, Color.white);
            CreateLabel(node.transform, "Name", LevelNames[index],
                new Vector2(0f, -8f), new Vector2(190f, 70f), 20f, Color.white);
            _levelStars[index] = CreateLabel(node.transform, "Stars", "",
                new Vector2(0f, -70f), new Vector2(180f, 28f), 18f, new Color(1f, 0.9f, 0.4f, 1f));
            _levelButtons[index] = node.GetComponent<Button>();
            int captured = index + 1;
            _levelButtons[index].onClick.AddListener(() => OnLevelPressed(captured));
        }

        private void RefreshWorldMap()
        {
            for (int i = 0; i < AdventureSession.LevelCount; i++)
            {
                int level = i + 1;
                bool unlocked = AdventureSession.IsLevelUnlocked(level);
                bool playable = unlocked && level == 1;
                if (_levelButtons[i] != null)
                {
                    _levelButtons[i].interactable = playable || (unlocked && level == 2);
                    var image = _levelButtons[i].GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = unlocked
                            ? new Color(0.20f, 0.48f, 0.26f, 0.96f)
                            : new Color(0.18f, 0.22f, 0.18f, 0.9f);
                    }
                }

                TMP_Text nameLabel = _levelButtons[i] != null
                    ? _levelButtons[i].transform.Find("Name")?.GetComponent<TMP_Text>()
                    : null;
                if (nameLabel != null && !unlocked)
                {
                    nameLabel.text = "LOCKED";
                }
                else if (nameLabel != null)
                {
                    nameLabel.text = LevelNames[i];
                }

                if (_levelStars[i] != null)
                {
                    if (level == 1 && SaveManager.Data.greenHillsLevel1Stars > 0)
                    {
                        _levelStars[i].text = $"{SaveManager.Data.greenHillsLevel1Stars} / 3 STARS";
                    }
                    else if (!unlocked)
                    {
                        _levelStars[i].text = "LOCKED";
                    }
                    else if (level > 1)
                    {
                        _levelStars[i].text = "OPEN TRAIL";
                    }
                    else
                    {
                        _levelStars[i].text = "NEW";
                    }
                }
            }
        }

        private void OnLevelPressed(int levelIndex)
        {
            if (!AdventureSession.IsLevelUnlocked(levelIndex))
            {
                return;
            }

            if (levelIndex == 2)
            {
                if (_worldFooter != null)
                {
                    _worldFooter.text = "Lost Stars is unlocked. That trail comes in the next adventure update.";
                }

                return;
            }

            if (levelIndex > 2)
            {
                return;
            }

            AudioManager.Instance?.PlayButton();
            AdventureSession.BeginLevel(levelIndex);
            GameManager.LoadSceneClean(GameManager.GameSceneName);
        }

        private GameObject CreateFullPanel(string name, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = colour;
            go.transform.SetAsLastSibling();
            return go;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size, Color colour, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = colour;
            CreateLabel(go.transform, "Label", label, Vector2.zero, Vector2.zero, fontSize, Color.white);
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

        private static TMP_Text CreateLabel(Transform parent, string name, string text,
            Vector2 position, Vector2 size, float fontSize, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = colour;
            label.raycastTarget = false;
            TMP_Text existing = parent.GetComponentInParent<Canvas>()?.GetComponentInChildren<TMP_Text>();
            if (existing != null && existing.font != null)
            {
                label.font = existing.font;
            }

            return label;
        }

        private static int IndexOf(string id)
        {
            for (int i = 0; i < CharacterRoster.Slots.Length; i++)
            {
                if (CharacterRoster.Slots[i].Id == id)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
