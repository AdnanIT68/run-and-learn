using System.Collections;
using FootballRunner.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The card under the hearts that shows the word being asked. Tapping it
    /// replays the voice; the jump input ignores taps on UI, so it is safe mid-run.
    /// Built in code on the HUD canvas so the Game scene needs no edits.
    /// </summary>
    public class WordPromptUi : MonoBehaviour
    {
        private static readonly Color CardColour = new Color(1f, 1f, 1f, 0.94f);
        private static readonly Color CaughtColour = new Color(0.62f, 0.93f, 0.6f, 0.96f);
        private static readonly Color MissedColour = new Color(1f, 0.86f, 0.55f, 0.96f);
        private static readonly Color TextColour = new Color(0.16f, 0.2f, 0.26f, 1f);

        private CanvasGroup _group;
        private Image _card;
        private TMP_Text _word;
        private Image _result;
        private Coroutine _hideRoutine;

        public static void EnsureOnCanvas(Transform canvas)
        {
            if (canvas == null || canvas.GetComponent<WordPromptUi>() != null)
            {
                return;
            }

            canvas.gameObject.AddComponent<WordPromptUi>();
        }

        private void OnEnable()
        {
            WordChallengeDirector.ChallengeStarted += OnChallengeStarted;
            WordChallengeDirector.ChallengeEnded += OnChallengeEnded;
            GameManager.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            WordChallengeDirector.ChallengeStarted -= OnChallengeStarted;
            WordChallengeDirector.ChallengeEnded -= OnChallengeEnded;
            GameManager.StateChanged -= OnStateChanged;
        }

        private void Start()
        {
            Transform parent = transform.Find("SafeArea");
            Build(parent != null ? parent : transform);
            SetVisible(false);
        }

        private void Build(Transform parent)
        {
            var root = new GameObject("WordPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -100f);
            rect.sizeDelta = new Vector2(560f, 120f);

            _card = root.GetComponent<Image>();
            _card.color = CardColour;
            _group = root.GetComponent<CanvasGroup>();
            root.GetComponent<Button>().onClick.AddListener(OnCardPressed);

            Image speaker = CreateIcon(root.transform, "Speaker", WordPack.Icon("speaker"), new Vector2(-210f, 0f), 84f);
            speaker.preserveAspect = true;

            var wordGo = new GameObject("Word", typeof(RectTransform));
            wordGo.transform.SetParent(root.transform, false);
            var wordRect = wordGo.GetComponent<RectTransform>();
            wordRect.anchorMin = new Vector2(0.5f, 0.5f);
            wordRect.anchorMax = new Vector2(0.5f, 0.5f);
            wordRect.anchoredPosition = new Vector2(40f, 0f);
            wordRect.sizeDelta = new Vector2(400f, 100f);
            _word = wordGo.AddComponent<TextMeshProUGUI>();
            _word.fontSize = 60f;
            _word.fontStyle = FontStyles.Bold;
            _word.alignment = TextAlignmentOptions.Center;
            _word.color = TextColour;
            _word.raycastTarget = false;
            _word.enableAutoSizing = true;
            _word.fontSizeMin = 30f;
            _word.fontSizeMax = 60f;

            TMP_Text existing = parent.GetComponentInChildren<TMP_Text>(true);
            if (existing != null && existing != _word && existing.font != null)
            {
                _word.font = existing.font;
            }

            _result = CreateIcon(root.transform, "Result", WordPack.Icon("check"), new Vector2(230f, 0f), 70f);
            _result.gameObject.SetActive(false);
        }

        private static Image CreateIcon(Transform parent, string name, Sprite sprite, Vector2 position, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        private void OnCardPressed()
        {
            WordChallengeDirector.Instance?.ReplayWord();
        }

        private void OnChallengeStarted(WordEntry word)
        {
            if (_card == null)
            {
                return;
            }

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            _card.color = CardColour;
            _word.text = word.Text(LearningProgress.Language);
            _result.gameObject.SetActive(false);
            SetVisible(true);
        }

        private void OnChallengeEnded(WordEntry word, bool caught)
        {
            if (_card == null)
            {
                return;
            }

            _card.color = caught ? CaughtColour : MissedColour;
            _result.gameObject.SetActive(caught && _result.sprite != null);
            _hideRoutine = StartCoroutine(HideAfter(1.4f));
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.GameOver || state == GameState.LevelComplete)
            {
                SetVisible(false);
            }
        }

        private IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            SetVisible(false);
            _hideRoutine = null;
        }

        private void SetVisible(bool visible)
        {
            if (_group == null)
            {
                return;
            }

            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }
    }
}
