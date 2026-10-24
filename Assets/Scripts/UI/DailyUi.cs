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
    /// TODAY on the main menu: the seven-day gift track with a big Take button and
    /// the day's three tasks with their progress. A gift icon among the round menu
    /// buttons opens it and wears a red dot while today's gift is waiting; the
    /// first menu visit of the day opens it by itself.
    /// </summary>
    public class DailyUi : MonoBehaviour
    {
        private const float TileWidth = 150f;
        private const float TileGap = 20f;

        private static readonly Color PanelColour = new Color(0.42f, 0.27f, 0.62f, 1f);
        private static readonly Color TileColour = new Color(1f, 0.98f, 0.92f, 1f);
        private static readonly Color TakenColour = new Color(0.62f, 0.9f, 0.6f, 1f);
        private static readonly Color TodayColour = new Color(1f, 0.85f, 0.35f, 1f);
        private static readonly Color DoneRowColour = new Color(0.62f, 0.9f, 0.6f, 1f);
        private static readonly Color DarkText = new Color(0.16f, 0.2f, 0.26f, 1f);

        /// <summary>Tests and screenshots switch off the automatic opening.</summary>
        public static bool AutoOpen = true;

        private static bool _offeredThisSession;

        private class Tile
        {
            public Image Background;
            public Image Check;
            public TMP_Text Day;
            public TMP_Text Coins;
        }

        private class Row
        {
            public Image Background;
            public Image Icon;
            public TMP_Text Task;
            public TMP_Text Progress;
            public TMP_Text Reward;
        }

        private readonly List<Tile> _tiles = new List<Tile>();
        private readonly List<Row> _rows = new List<Row>();
        private GameObject _panel;
        private TMP_Text _heading;
        private TMP_Text _giftLabel;
        private TMP_Text _tasksLabel;
        private TMP_Text _backLabel;
        private TMP_Text _coins;
        private Button _claim;
        private Image _claimImage;
        private TMP_Text _claimLabel;
        private Image _badge;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public static DailyUi EnsureOnCanvas(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var daily = canvas.GetComponent<DailyUi>();

            if (daily == null)
            {
                daily = canvas.gameObject.AddComponent<DailyUi>();
                daily.Build();
            }

            return daily;
        }

        private void OnEnable()
        {
            DailyRewards.Changed += Refresh;
        }

        private void OnDisable()
        {
            DailyRewards.Changed -= Refresh;
        }

        private void Start()
        {
            BuildBadge();
            Refresh();

            if (AutoOpen && !_offeredThisSession && DailyRewards.GiftReady)
            {
                _offeredThisSession = true;
                StartCoroutine(OpenSoon());
            }
        }

        private IEnumerator OpenSoon()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Open();
        }

        public void Open()
        {
            if (_panel == null)
            {
                return;
            }

            Refresh();
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

        /// <summary>Same as tapping the Take button. Returns the coins given.</summary>
        public int PressClaim()
        {
            int coins = DailyRewards.ClaimGift();

            if (coins > 0)
            {
                AudioManager.Instance?.PlayCoin();
                Haptics.Medium();
                StartCoroutine(Pop(_claim.GetComponent<RectTransform>()));
            }

            return coins;
        }

        // ------------------------------------------------------------ layout

        private void Build()
        {
            Transform safe = transform.Find("SafeArea");
            Transform layer = safe != null ? safe : transform;

            // Styled with the other round buttons by MainMenuStyle.
            var button = new GameObject("TodayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            button.transform.SetParent(layer, false);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 120f);
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                Open();
            });

            _panel = new GameObject("DailyPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = PanelColour;

            _heading = CreateText(_panel.transform, "Heading", new Vector2(0f, 445f), new Vector2(900f, 90f), 60f,
                Color.white);
            CreateImage(_panel.transform, "CoinIcon", Resources.Load<Sprite>("Menu/icon_coin"), new Vector2(640f, 445f),
                new Vector2(64f, 64f));
            _coins = CreateText(_panel.transform, "Coins", new Vector2(760f, 445f), new Vector2(160f, 70f), 48f,
                Color.white);
            _coins.alignment = TextAlignmentOptions.Left;

            // Gift track: seven days, each with its coins shown in advance.
            _giftLabel = CreateText(_panel.transform, "GiftLabel", new Vector2(0f, 345f), new Vector2(900f, 60f), 40f,
                Color.white);
            int days = DailyRewards.GiftTrack.Length;
            float width = days * TileWidth + (days - 1) * TileGap;

            for (int i = 0; i < days; i++)
            {
                float x = -width * 0.5f + TileWidth * 0.5f + i * (TileWidth + TileGap);
                _tiles.Add(BuildTile(i, new Vector2(x, 210f)));
            }

            Image claim = CreateImage(_panel.transform, "Claim", RuntimeSprites.RoundedRect(), new Vector2(0f, 60f),
                new Vector2(460f, 110f));
            claim.type = Image.Type.Sliced;
            claim.pixelsPerUnitMultiplier = 1.3f;
            claim.preserveAspect = false;
            claim.raycastTarget = true;
            _claimImage = claim;
            _claim = claim.gameObject.AddComponent<Button>();
            _claim.onClick.AddListener(() => PressClaim());
            _claimLabel = CreateText(claim.transform, "Label", Vector2.zero, new Vector2(420f, 90f), 46f, Color.white);

            // The day's tasks.
            _tasksLabel = CreateText(_panel.transform, "TasksLabel", new Vector2(0f, -70f), new Vector2(900f, 60f), 40f,
                Color.white);

            for (int i = 0; i < 3; i++)
            {
                _rows.Add(BuildRow(new Vector2(0f, -160f - i * 112f)));
            }

            var back = CreateImage(_panel.transform, "Back", RuntimeSprites.RoundedRect(), Vector2.zero,
                new Vector2(220f, 90f));
            RectTransform backRect = back.rectTransform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(60f, -50f);
            back.type = Image.Type.Sliced;
            back.pixelsPerUnitMultiplier = 1.6f;
            back.preserveAspect = false;
            back.color = new Color(0.2f, 0.12f, 0.32f, 0.95f);
            back.raycastTarget = true;
            back.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButton();
                Close();
            });
            _backLabel = CreateText(back.transform, "Label", Vector2.zero, new Vector2(220f, 90f), 36f, Color.white);

            _panel.SetActive(false);
        }

        private Tile BuildTile(int index, Vector2 position)
        {
            Image background = CreateImage(_panel.transform, "Day" + (index + 1), RuntimeSprites.RoundedRect(),
                position, new Vector2(TileWidth, 170f));
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 1.4f;
            background.preserveAspect = false;

            var tile = new Tile
            {
                Background = background,
                Day = CreateText(background.transform, "Day", new Vector2(0f, 55f), new Vector2(TileWidth - 10f, 40f),
                    28f, DarkText),
                Coins = CreateText(background.transform, "Coins", new Vector2(0f, -50f), new Vector2(TileWidth - 10f, 50f),
                    38f, DarkText),
                Check = CreateImage(background.transform, "Check", WordPack.Icon("check"), new Vector2(50f, 62f),
                    new Vector2(40f, 40f))
            };
            CreateImage(background.transform, "Coin", Resources.Load<Sprite>("Menu/icon_coin"), new Vector2(0f, 4f),
                new Vector2(56f, 56f));
            tile.Coins.text = DailyRewards.GiftTrack[index].ToString();
            return tile;
        }

        private Row BuildRow(Vector2 position)
        {
            Image background = CreateImage(_panel.transform, "Task", RuntimeSprites.RoundedRect(), position,
                new Vector2(1100f, 96f));
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 1.4f;
            background.preserveAspect = false;

            var row = new Row
            {
                Background = background,
                Icon = CreateImage(background.transform, "Icon", WordPack.Icon("target"), new Vector2(-495f, 0f),
                    new Vector2(64f, 64f)),
                Task = CreateText(background.transform, "Task", new Vector2(-130f, 0f), new Vector2(640f, 80f), 38f,
                    DarkText),
                Progress = CreateText(background.transform, "Progress", new Vector2(285f, 0f), new Vector2(140f, 70f),
                    38f, DarkText),
                Reward = CreateText(background.transform, "Reward", new Vector2(480f, 0f), new Vector2(100f, 70f), 38f,
                    DarkText)
            };
            row.Task.alignment = TextAlignmentOptions.Left;
            CreateImage(background.transform, "Coin", Resources.Load<Sprite>("Menu/icon_coin"), new Vector2(405f, 0f),
                new Vector2(50f, 50f));
            return row;
        }

        private void BuildBadge()
        {
            Transform safe = transform.Find("SafeArea");
            Transform button = (safe != null ? safe : transform).Find("TodayButton");

            if (button == null)
            {
                return;
            }

            _badge = CreateImage(button, "Badge", RuntimeSprites.Circle(), Vector2.zero, new Vector2(38f, 38f));
            RectTransform rect = _badge.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-14f, -14f);
            _badge.color = new Color(0.92f, 0.22f, 0.2f, 1f);
        }

        // ------------------------------------------------------------ state

        private void Refresh()
        {
            if (_panel == null)
            {
                return;
            }

            bool bosnian = LearningProgress.HomeLanguage == LearningProgress.Bosnian;
            bool ready = DailyRewards.GiftReady;
            int step = DailyRewards.GiftStep;
            // Gifts already taken in this round of seven; a finished round shows all seven.
            int taken = ready ? step : step == 0 ? DailyRewards.GiftTrack.Length : step;

            _heading.text = bosnian ? "DANAS" : "TODAY";
            _giftLabel.text = bosnian ? "Dnevni poklon" : "Daily gift";
            _tasksLabel.text = bosnian ? "Zadaci za danas" : "Today's tasks";
            _backLabel.text = bosnian ? "NAZAD" : "BACK";
            _coins.text = SaveManager.Data.totalCoins.ToString();

            for (int i = 0; i < _tiles.Count; i++)
            {
                Tile tile = _tiles[i];
                bool isTaken = i < taken;
                bool isToday = ready && i == step;
                tile.Background.color = isTaken ? TakenColour : isToday ? TodayColour : TileColour;
                tile.Check.enabled = isTaken && tile.Check.sprite != null;
                tile.Day.text = (bosnian ? "Dan " : "Day ") + (i + 1);
            }

            _claim.interactable = ready;
            _claimImage.color = ready ? new Color(0.27f, 0.74f, 0.32f) : new Color(0.55f, 0.5f, 0.62f);
            _claimLabel.text = ready
                ? (bosnian ? "UZMI +" : "TAKE +") + DailyRewards.NextGift
                : bosnian ? "Sutra novi poklon" : "New gift tomorrow";

            IReadOnlyList<DailyMission> missions = DailyRewards.Missions;

            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                bool exists = i < missions.Count;
                row.Background.gameObject.SetActive(exists);

                if (!exists)
                {
                    continue;
                }

                DailyMission mission = missions[i];
                row.Background.color = mission.done ? DoneRowColour : TileColour;
                row.Icon.sprite = WordPack.Icon(mission.done ? "check" : "target");
                row.Icon.enabled = row.Icon.sprite != null;
                row.Task.text = DailyRewards.Describe(mission, bosnian);
                row.Progress.text = $"{mission.progress}/{mission.target}";
                row.Reward.text = "+" + mission.reward;
            }

            if (_badge != null)
            {
                _badge.enabled = ready;
            }
        }

        // ------------------------------------------------------------ helpers

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 position, Vector2 size,
            float fontSize, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
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

        private static IEnumerator Pop(RectTransform target)
        {
            const float seconds = 0.25f;
            float elapsed = 0f;

            while (elapsed < seconds && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.one * (1f + Mathf.Sin(elapsed / seconds * Mathf.PI) * 0.15f);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }
    }
}
