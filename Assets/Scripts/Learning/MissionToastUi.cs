using System.Collections;
using System.Collections.Generic;
using FootballRunner.Audio;
using FootballRunner.SaveSystem;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// A small card that slides in at the top right when a daily task is done:
    /// a check, the task and the coins it gave. Several in a row queue up.
    /// </summary>
    public class MissionToastUi : MonoBehaviour
    {
        private const float ShowSeconds = 2.6f;

        private readonly Queue<DailyMission> _queue = new Queue<DailyMission>();
        private RectTransform _card;
        private CanvasGroup _group;
        private TMP_Text _task;
        private TMP_Text _coins;
        private Coroutine _routine;
        private Vector2 _home = new Vector2(-90f, -170f);

        public bool IsShowing => _group != null && _group.alpha > 0.01f;

        public static void EnsureOnCanvas(Transform canvas)
        {
            if (canvas != null && canvas.GetComponent<MissionToastUi>() == null)
            {
                canvas.gameObject.AddComponent<MissionToastUi>();
            }
        }

        private void OnEnable()
        {
            DailyRewards.MissionCompleted += OnMissionCompleted;
        }

        private void OnDisable()
        {
            DailyRewards.MissionCompleted -= OnMissionCompleted;
        }

        private void Start()
        {
            // On the canvas itself rather than the safe area, so it can rise above the result screen.
            Build(transform);
            _group.alpha = 0f;
        }

        private void Build(Transform parent)
        {
            var go = new GameObject("MissionToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            _card = go.GetComponent<RectTransform>();
            _card.anchorMin = _card.anchorMax = new Vector2(1f, 1f);
            _card.pivot = new Vector2(1f, 1f);
            _card.anchoredPosition = new Vector2(-90f, -170f);
            _card.sizeDelta = new Vector2(560f, 110f);

            var image = go.GetComponent<Image>();
            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.4f;
            image.color = new Color(0.62f, 0.93f, 0.6f, 0.97f);
            image.raycastTarget = false;
            _group = go.GetComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            Icon(go.transform, WordPack.Icon("check"), new Vector2(-220f, 0f), 70f);
            _task = Text(go.transform, new Vector2(-10f, 0f), new Vector2(330f, 90f), 34f);
            Icon(go.transform, Resources.Load<Sprite>("Menu/icon_coin"), new Vector2(185f, 0f), 46f);
            _coins = Text(go.transform, new Vector2(240f, 0f), new Vector2(90f, 70f), 38f);
        }

        private void OnMissionCompleted(DailyMission mission)
        {
            _queue.Enqueue(mission);

            if (_routine == null && _card != null)
            {
                _routine = StartCoroutine(ShowQueue());
            }
        }

        private IEnumerator ShowQueue()
        {
            while (_queue.Count > 0)
            {
                DailyMission mission = _queue.Dequeue();
                _task.text = DailyRewards.Describe(mission);
                _coins.text = "+" + mission.reward;
                // Above the result screen if a level just ended, under its card so nothing is hidden.
                _card.SetAsLastSibling();
                bool results = Core.GameManager.Instance != null &&
                               Core.GameManager.Instance.State == Core.GameState.LevelComplete;
                _card.anchorMin = _card.anchorMax = _card.pivot = results ? new Vector2(0.5f, 0f) : new Vector2(1f, 1f);
                _home = results ? new Vector2(0f, 14f) : new Vector2(-90f, -170f);
                AudioManager.Instance?.PlayCoin();
                Haptics.Light();

                yield return Fade(0f, 1f, 0.2f);
                yield return new WaitForSecondsRealtime(ShowSeconds);
                yield return Fade(1f, 0f, 0.25f);
            }

            _routine = null;
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                _group.alpha = Mathf.Lerp(from, to, t);
                _card.anchoredPosition = _home + new Vector2((1f - _group.alpha) * 80f, 0f);
                yield return null;
            }

            _group.alpha = to;
        }

        private static void Icon(Transform parent, Sprite sprite, Vector2 position, float size)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
        }

        private static TMP_Text Text(Transform parent, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject("Text", typeof(RectTransform));
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
            text.color = new Color(0.12f, 0.3f, 0.14f, 1f);
            text.raycastTarget = false;
            return text;
        }
    }
}
