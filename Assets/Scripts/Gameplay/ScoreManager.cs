using System;
using FootballRunner.Core;
using FootballRunner.Player;
using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// Score comes from distance travelled plus bonuses from collectibles.
    /// Coins are tracked separately because they are persistent currency, not score.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public static event Action<int> ScoreChanged;
        public static event Action<int> CoinsChanged;

        [Tooltip("Score awarded per world unit travelled.")]
        [SerializeField] private float pointsPerUnit = 1f;

        private Transform _player;
        private float _startX;
        private int _distancePoints;
        private int _bonusPoints;
        private int _lastReportedScore = -1;

        public int Score => _distancePoints + _bonusPoints;

        public int CoinsCollected { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                _player = PlayerController.Instance.transform;
                _startX = _player.position.x;
            }

            CoinsChanged?.Invoke(CoinsCollected);
            RaiseScoreChanged();
        }

        private void Update()
        {
            if (_player == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            float distance = Mathf.Max(0f, _player.position.x - _startX);
            _distancePoints = Mathf.FloorToInt(distance * pointsPerUnit);
            RaiseScoreChanged();
        }

        public void AddPoints(int points)
        {
            if (points <= 0)
            {
                return;
            }

            _bonusPoints += points;
            RaiseScoreChanged();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CoinsCollected += amount;
            CoinsChanged?.Invoke(CoinsCollected);
        }

        /// <summary>Only fires the event when the displayed value actually changes.</summary>
        private void RaiseScoreChanged()
        {
            int score = Score;

            if (score == _lastReportedScore)
            {
                return;
            }

            _lastReportedScore = score;
            ScoreChanged?.Invoke(score);
        }
    }
}
