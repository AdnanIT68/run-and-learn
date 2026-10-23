using System;
using System.Collections;
using FootballRunner.Audio;
using FootballRunner.Core;
using UnityEngine;

namespace FootballRunner.Player
{
    /// <summary>
    /// Hit points, the invulnerability window after a hit, and the flicker that
    /// tells the player they are currently safe.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public static event Action<int, int> HealthChanged;
        public static event Action Died;

        [Header("Health")]
        [SerializeField] private int maxHealth = 3;

        [Header("After Taking Damage")]
        [SerializeField] private float invulnerabilitySeconds = 1.5f;
        [SerializeField] private float flickerInterval = 0.1f;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Coroutine _invulnerabilityRoutine;
        private PlayerController _controller;
        private Color _baseSpriteColour = Color.white;

        public int CurrentHealth { get; private set; }

        public int MaxHealth => maxHealth;

        public bool IsInvulnerable { get; private set; }

        public bool IsDead => CurrentHealth <= 0;

        public bool HasShield { get; set; }

        private void Awake()
        {
            CurrentHealth = maxHealth;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                _baseSpriteColour = spriteRenderer.color;
            }

            _controller = GetComponent<PlayerController>();
        }

        private void Start()
        {
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        private void OnDisable()
        {
            RestoreSprite();
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsInvulnerable || CurrentHealth <= 0)
            {
                return;
            }

            // A shield absorbs the hit entirely rather than reducing it.
            if (HasShield)
            {
                HasShield = false;
                BeginInvulnerability();
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            AudioManager.Instance?.PlayHurt();

            if (CurrentHealth == 0)
            {
                Die();
                return;
            }

            BeginInvulnerability();
        }

        /// <summary>Instant death, used when the player falls out of the world.</summary>
        public void Kill()
        {
            if (CurrentHealth <= 0)
            {
                return;
            }

            CurrentHealth = 0;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            Die();
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || CurrentHealth <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        private void Die()
        {
            StopInvulnerability();
            _controller?.HaltMotion();

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.45f, 0.45f, 1f);
            }

            Died?.Invoke();
            GameManager.Instance?.GameOver();
        }

        private void BeginInvulnerability()
        {
            StopInvulnerability();
            _invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine());
        }

        private void StopInvulnerability()
        {
            if (_invulnerabilityRoutine != null)
            {
                StopCoroutine(_invulnerabilityRoutine);
                _invulnerabilityRoutine = null;
            }

            IsInvulnerable = false;
            RestoreSprite();
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            IsInvulnerable = true;
            float remaining = invulnerabilitySeconds;

            while (remaining > 0f)
            {
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = Mathf.Approximately(color.a, 1f) ? 0.35f : 1f;
                    spriteRenderer.color = color;
                }

                yield return new WaitForSeconds(flickerInterval);
                remaining -= flickerInterval;
            }

            IsInvulnerable = false;
            RestoreSprite();
            _invulnerabilityRoutine = null;
        }

        private void RestoreSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            Color color = _baseSpriteColour;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }
}
