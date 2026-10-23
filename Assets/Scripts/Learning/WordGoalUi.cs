using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The run's visible goal: a row of empty circles that fill with the picture
    /// of each word caught. A full row says "Bravo!" and starts again, so a child
    /// always knows what to do next without reading anything.
    /// </summary>
    public class WordGoalUi : MonoBehaviour
    {
        private float SlotSize => _goal > 5 ? 60f : 78f;
        private float SlotGap => _goal > 5 ? 6f : 10f;

        private static readonly Color EmptyColour = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color FilledColour = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color CelebrateColour = new Color(1f, 0.86f, 0.3f, 1f);

        private Image[] _slots;
        private Image[] _pictures;
        private TMP_Text _count;
        private TMP_Text _banner;
        private Coroutine _celebrateRoutine;
        private int _goal;

        public static void EnsureOnCanvas(Transform canvas)
        {
            if (canvas == null || canvas.GetComponent<WordGoalUi>() != null)
            {
                return;
            }

            canvas.gameObject.AddComponent<WordGoalUi>();
        }

        private void OnEnable()
        {
            WordChallengeDirector.GoalProgress += OnGoalProgress;
            WordChallengeDirector.GoalCompleted += OnGoalCompleted;
        }

        private void OnDisable()
        {
            WordChallengeDirector.GoalProgress -= OnGoalProgress;
            WordChallengeDirector.GoalCompleted -= OnGoalCompleted;
        }

        private void Start()
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;

            if (director == null || !director.enabled)
            {
                return;
            }

            _goal = Mathf.Max(1, director.GoalSize);
            Transform parent = transform.Find("SafeArea");
            Build(parent != null ? parent : transform);
            ShowCount(director.GoalCaught);
        }

        private void Build(Transform parent)
        {
            var row = new GameObject("WordGoal", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(60f, -232f);
            rowRect.sizeDelta = new Vector2(_goal * (SlotSize + SlotGap) + 120f, SlotSize);

            _slots = new Image[_goal];
            _pictures = new Image[_goal];
            Sprite circle = Utilities.RuntimeSprites.Circle();

            for (int i = 0; i < _goal; i++)
            {
                _slots[i] = CreateImage(row.transform, "Slot" + i, circle,
                    new Vector2(i * (SlotSize + SlotGap) + SlotSize * 0.5f, -SlotSize * 0.5f), SlotSize);
                _slots[i].color = EmptyColour;
                _pictures[i] = CreateImage(_slots[i].transform, "Picture", null, Vector2.zero, SlotSize * 0.72f);
                var pictureRect = _pictures[i].rectTransform;
                pictureRect.anchorMin = new Vector2(0.5f, 0.5f);
                pictureRect.anchorMax = new Vector2(0.5f, 0.5f);
                _pictures[i].enabled = false;
            }

            _count = CreateText(row.transform, "Count",
                new Vector2(_goal * (SlotSize + SlotGap) + 66f, -SlotSize * 0.5f), new Vector2(110f, SlotSize), 40f);
            _banner = CreateText(transform, "GoalBanner", Vector2.zero, new Vector2(900f, 180f), 120f);
            var bannerRect = _banner.rectTransform;
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.anchoredPosition = new Vector2(0f, 120f);
            _banner.color = CelebrateColour;
            _banner.outlineWidth = 0.2f;
            _banner.outlineColor = new Color32(60, 40, 0, 255);
            _banner.gameObject.SetActive(false);
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 position, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            // The rounded display font matches the menu; fall back to the HUD's own font.
            if (Utilities.GameFonts.Display != null)
            {
                Utilities.GameFonts.UseDisplay(text);
            }
            else
            {
                TMP_Text existing = transform.GetComponentInChildren<TMP_Text>(true);
                if (existing != null && existing != text && existing.font != null)
                {
                    text.font = existing.font;
                }
            }

            return text;
        }

        private void OnGoalProgress(WordEntry word, int caught, int goal)
        {
            if (_slots == null || caught < 1 || caught > _slots.Length)
            {
                return;
            }

            int index = caught - 1;
            WordPack pack = WordChallengeDirector.Instance != null ? WordChallengeDirector.Instance.Pack : null;
            _pictures[index].sprite = pack != null ? pack.Picture(word.PictureId) : null;
            _pictures[index].enabled = _pictures[index].sprite != null;
            _slots[index].color = FilledColour;
            StartCoroutine(Pop(_slots[index].rectTransform));
            ShowCount(caught);
        }

        private void OnGoalCompleted(int goal)
        {
            if (_slots == null)
            {
                return;
            }

            if (_celebrateRoutine != null)
            {
                StopCoroutine(_celebrateRoutine);
            }

            _celebrateRoutine = StartCoroutine(Celebrate());
        }

        private IEnumerator Celebrate()
        {
            _banner.text = "BRAVO!";
            _banner.gameObject.SetActive(true);
            StartCoroutine(Pop(_banner.rectTransform));

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].color = CelebrateColour;
            }

            // Realtime: the celebration must finish even if the guide slowed time.
            yield return new WaitForSecondsRealtime(1.8f);

            _banner.gameObject.SetActive(false);

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].color = EmptyColour;
                _pictures[i].enabled = false;
            }

            ShowCount(0);
            _celebrateRoutine = null;
        }

        private void ShowCount(int caught)
        {
            if (_count != null)
            {
                _count.text = $"{caught}/{_goal}";
            }
        }

        private static IEnumerator Pop(RectTransform target)
        {
            const float seconds = 0.25f;
            float elapsed = 0f;

            while (elapsed < seconds && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / seconds;
                target.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.35f);
                yield return null;
            }

            if (target != null)
            {
                target.localScale = Vector3.one;
            }
        }
    }
}
