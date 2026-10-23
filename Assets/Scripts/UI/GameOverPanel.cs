using System.Collections;
using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text newBestLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private float fadeSeconds = 0.28f;

        private Coroutine _fadeRoutine;
        private Image _dimmer;
        private float _dimmerAlpha = 0.62f;

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
            restartButton?.onClick.AddListener(OnRestart);
            mainMenuButton?.onClick.AddListener(OnMainMenu);
            CacheDimmer();
            HideImmediate();
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.GameOver)
            {
                HideImmediate();
                return;
            }

            Show();
        }

        private void Show()
        {
            ScoreManager score = ScoreManager.Instance;
            int finalScore = score != null ? score.Score : 0;
            int coins = score != null ? score.CoinsCollected : 0;
            GameManager game = GameManager.Instance;
            int best = game != null ? game.LastRunBestScore : finalScore;
            bool isNewBest = game != null && game.LastRunWasNewBest;

            if (scoreText != null)
            {
                scoreText.text = finalScore.ToString();
            }

            if (bestText != null)
            {
                bestText.text = best.ToString();
            }

            if (coinsText != null)
            {
                coinsText.text = coins.ToString();
            }

            if (newBestLabel != null)
            {
                newBestLabel.gameObject.SetActive(isNewBest);
            }

            if (panel == null)
            {
                return;
            }

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            CacheDimmer();

            if (restartButton != null)
            {
                restartButton.interactable = true;
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.interactable = true;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeDimmer());
        }

        private void HideImmediate()
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            RestoreDimmerAlpha();

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void CacheDimmer()
        {
            if (panel == null)
            {
                return;
            }

            if (_dimmer == null)
            {
                _dimmer = panel.GetComponent<Image>();
            }

            if (_dimmer != null && _dimmer.color.a > 0.01f)
            {
                _dimmerAlpha = _dimmer.color.a;
            }
        }

        private void RestoreDimmerAlpha()
        {
            if (_dimmer == null)
            {
                return;
            }

            Color colour = _dimmer.color;
            colour.a = _dimmerAlpha;
            _dimmer.color = colour;
        }

        private IEnumerator FadeDimmer()
        {
            if (_dimmer == null)
            {
                _fadeRoutine = null;
                yield break;
            }

            // Fade only the backdrop. A CanvasGroup on this panel would also
            // fade the buttons and can swallow their first clicks.
            Color colour = _dimmer.color;
            colour.a = 0f;
            _dimmer.color = colour;
            float duration = Mathf.Max(0.01f, fadeSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                colour.a = Mathf.Lerp(0f, _dimmerAlpha, elapsed / duration);
                _dimmer.color = colour;
                yield return null;
            }

            colour.a = _dimmerAlpha;
            _dimmer.color = colour;
            _fadeRoutine = null;
        }

        private void OnRestart()
        {
            AudioManager.Instance?.PlayButton();
            UiFeedback.Punch(this, restartButton);
            GameManager.Instance?.RestartRun();
        }

        private void OnMainMenu()
        {
            AudioManager.Instance?.PlayButton();
            GameManager.Instance?.GoToMainMenu();
        }
    }
}
