using FootballRunner.Core;
using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// Single place to tune how the run ramps up. Everything is time-based and
    /// interpolated, so difficulty rises smoothly instead of jumping at thresholds.
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [Header("Run Speed (world units per second)")]
        [SerializeField] private float startSpeed = 6.5f;
        [SerializeField] private float maxSpeed = 13f;
        [Tooltip("Seconds of survival before top speed is reached.")]
        [SerializeField] private float secondsToMaxSpeed = 180f;

        [Header("Obstacle Density")]
        [Range(0f, 1f)]
        [SerializeField] private float startObstacleChance = 0.18f;
        [Range(0f, 1f)]
        [SerializeField] private float maxObstacleChance = 0.5f;
        [SerializeField] private float secondsToMaxObstacleChance = 140f;

        [Header("Grace Period")]
        [Tooltip("Seconds at the start of a run with no obstacles at all, so the player can settle in.")]
        [SerializeField] private float warmUpSeconds = 24f;

        [Header("Fairness")]
        [Tooltip("Minimum reaction time between hazards. The generator converts this to distance using current speed.")]
        [SerializeField] private float minSecondsBetweenHazards = 1.1f;
        [SerializeField] private int maxObstaclesPerChunkEarly = 1;
        [SerializeField] private int maxObstaclesPerChunkLate = 2;
        [Tooltip("After this many seconds a chunk may place a second hazard, still respecting spacing.")]
        [SerializeField] private float secondsUntilLateDensity = 45f;

        public float ElapsedSeconds { get; private set; }

        /// <summary>The world level being played (0 for level 1); later levels start faster and busier.</summary>
        public int Level { get; private set; }

        public float WarmUpSeconds => warmUpSeconds;

        public bool IsWarmUp => ElapsedSeconds < warmUpSeconds;

        public float CurrentSpeed =>
            Adventure.AdventureSession.IsActive
                ? startSpeed
                : Mathf.Lerp(startSpeed, maxSpeed, Progress(secondsToMaxSpeed));

        public float CurrentObstacleChance => ObstacleChanceAt(ElapsedSeconds);

        public float MinSecondsBetweenHazards => minSecondsBetweenHazards;

        public int MaxObstaclesThisChunk => MaxObstaclesAt(ElapsedSeconds);

        /// <summary>
        /// 0 = warm-up, 1 = singles, 2 = spaced pairs, 3 = full pattern set.
        /// Uses the same elapsed clock as speed, not a second difficulty curve.
        /// </summary>
        public int PatternTier => PatternTierAt(ElapsedSeconds);

        public float ObstacleChanceAt(float elapsedSeconds)
        {
            if (elapsedSeconds < warmUpSeconds)
            {
                return 0f;
            }

            return Mathf.Lerp(startObstacleChance, maxObstacleChance, ProgressAt(elapsedSeconds, secondsToMaxObstacleChance));
        }

        public int PatternTierAt(float elapsedSeconds)
        {
            if (elapsedSeconds < warmUpSeconds)
            {
                return 0;
            }

            if (elapsedSeconds < secondsUntilLateDensity)
            {
                return 1;
            }

            if (elapsedSeconds < secondsUntilLateDensity + 45f)
            {
                return 2;
            }

            return 3;
        }

        public int MaxObstaclesAt(float elapsedSeconds)
        {
            return elapsedSeconds < secondsUntilLateDensity
                ? Mathf.Max(1, maxObstaclesPerChunkEarly)
                : Mathf.Max(1, maxObstaclesPerChunkLate);
        }

        /// <summary>
        /// When a world-space feature will reach the player at the current run speed.
        /// Used so spawn-ahead chunks do not dump pits into the warm-up window.
        /// </summary>
        public float EstimateArrivalSeconds(float worldX, float playerX)
        {
            float speed = Mathf.Max(0.01f, CurrentSpeed);
            return ElapsedSeconds + Mathf.Max(0f, worldX - playerX) / speed;
        }

        public bool WouldArriveDuringWarmUp(float worldX, float playerX)
        {
            return EstimateArrivalSeconds(worldX, playerX) < warmUpSeconds;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            if (!Adventure.AdventureSession.IsActive)
            {
                ApplyLevel(Learning.LearningProgress.SelectedLevel);
            }
        }

        /// <summary>
        /// Each level is a little faster from the start, settles in sooner and has a
        /// few more hazards. Level 5 starts about a quarter faster than level 1.
        /// </summary>
        public void ApplyLevel(int level)
        {
            Level = Mathf.Clamp(level, 0, 4);
            startSpeed *= 1f + 0.06f * Level;
            warmUpSeconds *= 1f - 0.15f * Level;
            startObstacleChance = Mathf.Min(maxObstacleChance, startObstacleChance + 0.04f * Level);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                ElapsedSeconds += Time.deltaTime;
            }
        }

        private float Progress(float duration)
        {
            return ProgressAt(ElapsedSeconds, duration);
        }

        private static float ProgressAt(float elapsedSeconds, float duration)
        {
            return duration <= 0f ? 1f : Mathf.Clamp01(elapsedSeconds / duration);
        }
    }
}
