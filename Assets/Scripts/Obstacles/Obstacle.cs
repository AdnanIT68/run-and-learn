using FootballRunner.Core;
using FootballRunner.Player;
using UnityEngine;

namespace FootballRunner.Obstacles
{
    /// <summary>
    /// Anything that hurts the player on contact. New obstacle types are just new
    /// prefabs with this component; no code changes are needed to add them.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Obstacle : MonoBehaviour
    {
        [Tooltip("Hit points removed on contact. Zero makes this a harmless prop.")]
        [SerializeField] private int damage = 1;

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Enter alone would ignore a player who is still overlapping after the
            // invulnerability window ends. Stay re-applies, and PlayerHealth rejects
            // hits during the cooldown, so one contact still costs exactly one heart.
            TryDamage(other);
        }

        private void TryDamage(Collider2D other)
        {
            if (damage <= 0)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

            if (health == null)
            {
                return;
            }

            health.TakeDamage(damage);
        }
    }
}
