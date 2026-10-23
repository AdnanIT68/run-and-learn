using System;
using System.Collections;
using FootballRunner.Audio;
using FootballRunner.Gameplay;
using FootballRunner.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FootballRunner.Core
{
    /// <summary>
    /// Owns the run's lifecycle. Everything else reacts to <see cref="StateChanged"/>
    /// rather than reaching into each other, which keeps the systems independent.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const string MainMenuSceneName = "MainMenu";
        public const string GameSceneName = "Game";

        public static GameManager Instance { get; private set; }

        /// <summary>True while a menu or restart is already loading a scene.</summary>
        public static bool IsLoadingScene { get; private set; }

        /// <summary>Raised whenever the run changes state. Always unsubscribe in OnDisable.</summary>
        public static event Action<GameState> StateChanged;

        [Header("Death")]
        [Tooltip("Lets the death pose read before the Game Over panel appears.")]
        [SerializeField] private float deathTransitionSeconds = 0.85f;

        public GameState State { get; private set; } = GameState.Ready;

        public bool IsPlaying => State == GameState.Playing;

        public bool IsDying => State == GameState.Dying;

        /// <summary>True only when this run beat the high score that existed before it was saved.</summary>
        public bool LastRunWasNewBest { get; private set; }

        public int LastRunBestScore { get; private set; }

        private Coroutine _gameOverRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetSceneLoadGuard()
        {
            IsLoadingScene = false;
        }

        /// <summary>
        /// Restores time and loads a scene once. Repeated taps on Restart or Play
        /// must not queue extra loads.
        /// </summary>
        public static void LoadSceneClean(string sceneName)
        {
            if (IsLoadingScene || string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            IsLoadingScene = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            IsLoadingScene = false;
            Time.timeScale = 1f;
            EnvironmentDirector.EnsureOn(gameObject);
            Learning.WordChallengeDirector.EnsureOn(gameObject);
            Learning.PepiCompanion.EnsureOn();
            Effects.GameFeel.EnsureOn(gameObject);
            if (Adventure.AdventureSession.IsActive)
            {
                Adventure.AdventureCourse.EnsureOn(gameObject);
            }
        }

        private void Start()
        {
            if (Adventure.AdventureSession.IsActive)
            {
                SetState(GameState.Ready);
                return;
            }

            SetState(GameState.Playing);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Pause()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void GameOver()
        {
            if (State == GameState.GameOver || State == GameState.Dying || _gameOverRoutine != null)
            {
                return;
            }

            _gameOverRoutine = StartCoroutine(GameOverRoutine());
        }

        private IEnumerator GameOverRoutine()
        {
            Time.timeScale = 1f;
            SetState(GameState.Dying);

            ScoreManager score = ScoreManager.Instance;
            int runScore = score != null ? score.Score : 0;
            int coins = score != null ? score.CoinsCollected : 0;
            int previousBest = SaveManager.Data.highScore;

            // The high score belongs to endless mode. An adventure course is a
            // different length and would distort it, so only its coins are kept.
            int scoreForBest = Adventure.AdventureSession.IsActive ? 0 : runScore;
            LastRunWasNewBest = scoreForBest > previousBest;
            LastRunBestScore = Mathf.Max(scoreForBest, previousBest);

            SaveManager.SubmitRunResult(scoreForBest, coins);

            if (deathTransitionSeconds > 0f)
            {
                yield return new WaitForSeconds(deathTransitionSeconds);
            }

            Time.timeScale = 0f;
            AudioManager.Instance?.PlayGameOver();
            SetState(GameState.GameOver);
            _gameOverRoutine = null;
        }

        /// <summary>
        /// Android backgrounds the app for calls, notifications and app switching.
        /// Without this the run carries on unwatched and the player comes back to a
        /// lost game. Resuming is left to the player via the pause menu.
        /// </summary>
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                PauseForInterruption();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Excluded in the editor, where clicking another window would otherwise
            // pause the game constantly during development and automated tests.
#if !UNITY_EDITOR
            if (!hasFocus)
            {
                PauseForInterruption();
            }
#endif
        }

        private void PauseForInterruption()
        {
            if (State == GameState.Playing)
            {
                Pause();
            }
        }

        public void BeginAdventureRun()
        {
            if (State != GameState.Ready && State != GameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void CompleteAdventureLevel()
        {
            if (State != GameState.Playing || !Adventure.AdventureSession.IsActive)
            {
                return;
            }

            FootballRunner.Player.PlayerController.Instance?.HaltMotion();
            int coins = ScoreManager.Instance != null ? ScoreManager.Instance.CoinsCollected : 0;
            int stars = Adventure.AdventureCourse.Instance != null
                ? Adventure.AdventureCourse.Instance.StarsCollected
                : 0;
            float seconds = Adventure.AdventureSession.RunSeconds;
            Adventure.AdventureSession.RecordFinish(stars, coins, seconds);
            Time.timeScale = 0f;
            SetState(GameState.LevelComplete);
        }

        public void RestartRun()
        {
            if (Adventure.AdventureSession.IsActive)
            {
                Adventure.AdventureSession.BeginLevel(Adventure.AdventureSession.LevelIndex);
            }

            LoadSceneClean(GameSceneName);
        }

        public void GoToMainMenu()
        {
            if (Adventure.AdventureSession.IsActive)
            {
                Adventure.AdventureSession.OpenWorldMapOnMenu = true;
            }

            LoadSceneClean(MainMenuSceneName);
        }

        private void SetState(GameState newState)
        {
            State = newState;
            StateChanged?.Invoke(newState);
        }
    }
}
