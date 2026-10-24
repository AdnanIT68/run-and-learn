using System.Collections;
using System.Collections.Generic;
using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The end of a level: BRAVO, one to three stars, the words that were caught
    /// (tap one to hear it again), the coins earned, and Again / Next / Menu.
    /// Labels use the language the child already speaks. The finale has no Next.
    /// </summary>
    public class LevelResultUi : MonoBehaviour
    {
        private static readonly Color DimColour = new Color(0.05f, 0.12f, 0.2f, 0.72f);
        private static readonly Color CardColour = new Color(1f, 0.98f, 0.93f, 1f);
        private static readonly Color InkColour = new Color(0.16f, 0.22f, 0.32f, 1f);
        private static readonly Color EmptyStar = new Color(0f, 0f, 0f, 0.15f);

        private readonly List<Image> _stars = new List<Image>(3);
        private readonly List<Image> _wordPictures = new List<Image>(5);
        private readonly List<WordEntry> _shownWords = new List<WordEntry>(5);
        private GameObject _panel;
        private TMP_Text _world;
        private TMP_Text _coins;
        private Button _next;
        private TMP_Text _againLabel;
        private TMP_Text _nextLabel;
        private TMP_Text _menuLabel;

        public bool IsShown => _panel != null && _panel.activeSelf;

        public bool HasNext => _next != null && _next.gameObject.activeSelf;

        public static void EnsureOnCanvas(Transform canvas)
        {
            if (canvas != null && canvas.GetComponent<LevelResultUi>() == null)
            {
                canvas.gameObject.AddComponent<LevelResultUi>();
            }
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
            Build();
            _panel.SetActive(false);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.LevelComplete && WordChallengeDirector.Instance != null &&
                WordChallengeDirector.Instance.LevelDone)
            {
                Show();
            }
        }

        // ------------------------------------------------------------ layout

        private void Build()
        {
            _panel = new GameObject("LevelResult", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _panel.transform.SetParent(transform, false);
            Stretch(_panel.GetComponent<RectTransform>());
            _panel.GetComponent<Image>().color = DimColour;

            Image card = CreateImage(_panel.transform, "Card", RuntimeSprites.RoundedRect(), Vector2.zero,
                new Vector2(1180f, 820f));
            card.type = Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 0.9f;
            card.color = CardColour;
            card.preserveAspect = false;
            Transform c = card.transform;

            TMP_Text bravo = CreateText(c, "Bravo", "BRAVO!", new Vector2(0f, 320f), new Vector2(900f, 130f), 110f,
                new Color(1f, 0.78f, 0.15f, 1f));
            bravo.outlineWidth = 0.18f;
            bravo.outlineColor = new Color32(120, 70, 0, 255);
            _world = CreateText(c, "World", "", new Vector2(0f, 235f), new Vector2(900f, 60f), 44f, InkColour);

            Sprite star = WordPack.Icon("star");
            for (int i = 0; i < 3; i++)
            {
                float y = i == 1 ? 120f : 100f;
                _stars.Add(CreateImage(c, "Star" + i, star, new Vector2((i - 1) * 170f, y), new Vector2(150f, 150f)));
            }

            int slots = WordChallengeDirector.Instance != null
                ? Mathf.Clamp(WordChallengeDirector.Instance.GoalSize, 1, 10)
                : 5;
            float spacing = slots > 8 ? 105f : slots > 5 ? 128f : 170f;
            float slotSize = slots > 8 ? 92f : slots > 5 ? 112f : 140f;

            for (int i = 0; i < slots; i++)
            {
                Image slot = CreateImage(c, "WordSlot" + i, RuntimeSprites.Circle(),
                    new Vector2((i - (slots - 1) * 0.5f) * spacing, -60f), new Vector2(slotSize, slotSize));
                slot.color = new Color(0.2f, 0.45f, 0.7f, 0.12f);
                slot.raycastTarget = true;
                int index = i;
                slot.gameObject.AddComponent<Button>().onClick.AddListener(() => SayWord(index));
                Image picture = CreateImage(slot.transform, "Picture", null, Vector2.zero,
                    new Vector2(slotSize * 0.74f, slotSize * 0.74f));
                picture.raycastTarget = false;
                _wordPictures.Add(picture);
            }

            Image coin = CreateImage(c, "CoinIcon", Resources.Load<Sprite>("Menu/icon_coin"), new Vector2(-70f, -185f),
                new Vector2(64f, 64f));
            coin.raycastTarget = false;
            _coins = CreateText(c, "Coins", "", new Vector2(110f, -185f), new Vector2(240f, 70f), 52f, InkColour);
            _coins.alignment = TextAlignmentOptions.Left;

            _againLabel = CreateButton(c, "Again", new Vector2(-370f, -310f), new Color(0.25f, 0.55f, 0.92f), Again);
            _next = CreateButton(c, "Next", new Vector2(0f, -310f), new Color(0.27f, 0.74f, 0.32f), Next)
                .GetComponentInParent<Button>();
            _nextLabel = _next.GetComponentInChildren<TMP_Text>();
            _menuLabel = CreateButton(c, "Menu", new Vector2(370f, -310f), new Color(0.97f, 0.55f, 0.18f), Menu);
        }

        private void Show()
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;
            WordPack pack = director.Pack;
            string language = LearningProgress.Language;
            bool homeIsBosnian = LearningProgress.HomeLanguage == LearningProgress.Bosnian;

            string levelWord = homeIsBosnian ? "Nivo" : "Level";
            _world.text = pack != null ? $"{pack.Title(language)}  ·  {levelWord} {director.Level + 1}" : "";
            int coins = ScoreManager.Instance != null ? ScoreManager.Instance.CoinsCollected : 0;
            _coins.text = "+" + coins;

            _shownWords.Clear();
            _shownWords.AddRange(director.LevelWords);

            for (int i = 0; i < _wordPictures.Count; i++)
            {
                WordEntry word = i < _shownWords.Count ? _shownWords[i] : null;
                _wordPictures[i].sprite = word != null && pack != null ? pack.Picture(word.PictureId) : null;
                _wordPictures[i].enabled = _wordPictures[i].sprite != null;
            }

            _next.gameObject.SetActive(pack != null &&
                                       LearningProgress.TryGetNext(pack.Name, director.Level, out _, out _));

            _againLabel.text = homeIsBosnian ? "PONOVO" : "AGAIN";
            _nextLabel.text = homeIsBosnian ? "DALJE" : "NEXT";
            _menuLabel.text = homeIsBosnian ? "MENI" : "MENU";

            _panel.SetActive(true);
            _panel.transform.SetAsLastSibling();
            StartCoroutine(RevealStars(director.LevelStars));
        }

        private IEnumerator RevealStars(int earned)
        {
            foreach (Image star in _stars)
            {
                star.color = EmptyStar;
                star.rectTransform.localScale = Vector3.one;
            }

            for (int i = 0; i < earned && i < _stars.Count; i++)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                _stars[i].color = Color.white;
                AudioManager.Instance?.PlayCoin();
                yield return Pop(_stars[i].rectTransform);
            }
        }

        // ------------------------------------------------------------ actions

        private void SayWord(int index)
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;

            if (director == null || index >= _shownWords.Count || director.Pack == null)
            {
                return;
            }

            AudioManager.Instance?.PlayVoice(director.Pack.Voice(LearningProgress.Language, _shownWords[index].id));
            StartCoroutine(Pop(_wordPictures[index].rectTransform));
        }

        /// <summary>Same as tapping Again.</summary>
        public void Again()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.RestartRun();
        }

        /// <summary>Same as tapping Next: the following level, or level 1 of the next world after the last.</summary>
        public void Next()
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;
            WordPack pack = director != null ? director.Pack : null;

            if (pack == null || !LearningProgress.TryGetNext(pack.Name, director.Level, out string nextPack,
                    out int nextLevel) || !LearningProgress.SelectPack(nextPack) ||
                !LearningProgress.SelectLevel(nextLevel))
            {
                return;
            }

            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.RestartRun();
        }

        public void Menu()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.GoToMainMenu();
        }

        // ------------------------------------------------------------ helpers

        private TMP_Text CreateButton(Transform parent, string name, Vector2 position, Color colour,
            UnityEngine.Events.UnityAction action)
        {
            Image image = CreateImage(parent, name, RuntimeSprites.RoundedRect(), position, new Vector2(330f, 120f));
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.3f;
            image.color = colour;
            image.preserveAspect = false;
            image.raycastTarget = true;
            image.gameObject.AddComponent<Button>().onClick.AddListener(action);

            var shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.1f, 0.25f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -8f);

            TMP_Text label = CreateText(image.transform, "Label", "", Vector2.zero, new Vector2(300f, 100f), 52f, Color.white);
            label.outlineWidth = 0.12f;
            label.outlineColor = new Color32(0, 0, 0, 70);
            return label;
        }

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
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, Vector2 position,
            Vector2 size, float fontSize, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            GameFonts.UseDisplay(text);
            text.text = content;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.5f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = colour;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static IEnumerator Pop(RectTransform target)
        {
            const float seconds = 0.25f;
            float elapsed = 0f;

            while (elapsed < seconds && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.one * (1f + Mathf.Sin(elapsed / seconds * Mathf.PI) * 0.3f);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }
    }
}
