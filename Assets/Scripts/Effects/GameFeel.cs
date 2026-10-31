using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.UI;
using FootballRunner.Utilities;
using UnityEngine;

namespace FootballRunner.Effects
{
    /// <summary>
    /// Turns game events into effects and vibration in one place, so gameplay
    /// code only raises events and never has to know about feedback.
    /// </summary>
    public class GameFeel : MonoBehaviour
    {
        /// <summary>Words caught in a row without a mistake that earn a SUPER! burst.</summary>
        public const int StreakLength = 3;

        private int _lastHealth = int.MaxValue;
        private int _streak;
        private int _missesSeen;

        /// <summary>Words caught in a row without a wrong bubble or a missed word.</summary>
        public int Streak => _streak;

        public static GameFeel EnsureOn(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            var feel = host.GetComponent<GameFeel>();
            return feel != null ? feel : host.AddComponent<GameFeel>();
        }

        private void OnEnable()
        {
            PlayerController.Jumped += OnJumped;
            PlayerController.Landed += OnLanded;
            PlayerHealth.HealthChanged += OnHealthChanged;
            WordChallengeDirector.ChallengeEnded += OnChallengeEnded;
            WordChallengeDirector.GoalCompleted += OnGoalCompleted;
        }

        private void OnDisable()
        {
            PlayerController.Jumped -= OnJumped;
            PlayerController.Landed -= OnLanded;
            PlayerHealth.HealthChanged -= OnHealthChanged;
            WordChallengeDirector.ChallengeEnded -= OnChallengeEnded;
            WordChallengeDirector.GoalCompleted -= OnGoalCompleted;
        }

        private void OnJumped(Vector3 feet, bool airJump)
        {
            if (airJump)
            {
                FxPlayer.Instance.AirRing(feet);
                Haptics.Light();
                return;
            }

            FxPlayer.Instance.Dust(feet);
        }

        private void OnLanded(Vector3 feet)
        {
            FxPlayer.Instance.Dust(feet);
        }

        private void OnHealthChanged(int current, int max)
        {
            if (current < _lastHealth && _lastHealth != int.MaxValue && PlayerController.Instance != null)
            {
                FxPlayer.Instance.Hit(PlayerController.Instance.transform.position);
                Haptics.Strong();
            }

            _lastHealth = current;
        }

        private void OnChallengeEnded(WordEntry word, bool caught)
        {
            // Any wrong bubble or missed word since the last catch breaks the streak.
            WordChallengeDirector director = WordChallengeDirector.Instance;
            int misses = director != null ? director.RunMisses : 0;
            bool clean = caught && misses == _missesSeen;
            _missesSeen = misses;
            _streak = clean ? _streak + 1 : 0;

            if (!caught || PlayerController.Instance == null)
            {
                return;
            }

            // The word is caught at the top of the jump, just above the runner's head.
            FxPlayer.Instance.Sparkle(HeadPosition());
            Haptics.Light();

            // The goal row's own confetti takes over on the last word of the level.
            bool goalReached = director != null && director.GoalCaught >= director.GoalSize;

            if (_streak > 0 && _streak % StreakLength == 0 && !goalReached)
            {
                FxPlayer.Instance.Confetti(HeadPosition() + Vector3.up * 0.8f, 18);
                FloatingTextSpawner.Instance?.Show("SUPER!", HeadPosition() + Vector3.up * 1.4f);
                Haptics.Medium();
            }
        }

        private void OnGoalCompleted(int goal)
        {
            if (PlayerController.Instance == null)
            {
                return;
            }

            FxPlayer.Instance.Confetti(HeadPosition() + Vector3.up * 1.5f);
            Haptics.Medium();
        }

        private static Vector3 HeadPosition()
        {
            var collider = PlayerController.Instance.GetComponent<Collider2D>();
            return collider != null
                ? new Vector3(collider.bounds.center.x, collider.bounds.max.y + 0.6f, 0f)
                : PlayerController.Instance.transform.position + Vector3.up * 2f;
        }
    }
}
