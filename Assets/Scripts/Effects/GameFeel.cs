using FootballRunner.Learning;
using FootballRunner.Player;
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
        private int _lastHealth = int.MaxValue;

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
            if (!caught || PlayerController.Instance == null)
            {
                return;
            }

            // The word is caught at the top of the jump, just above the runner's head.
            FxPlayer.Instance.Sparkle(HeadPosition());
            Haptics.Light();
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
