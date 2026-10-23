using FootballRunner.Core;
using UnityEngine;

namespace FootballRunner.Player
{
    /// <summary>
    /// Feeds Endless Runner gameplay state into RukajjaCharacterAnimator.
    /// Lives on the Player root. Does not move, jump, or damage the character.
    /// Serialized sprites are fallbacks so the renderer is never left empty.
    /// </summary>
    public class RukajjaAnimDriver : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int HitHash = Animator.StringToHash("Hit");

        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] runSprites;
        [SerializeField] private Sprite jumpSprite;
        [SerializeField] private Sprite fallSprite;
        [SerializeField] private Sprite hitSprite;

        private PlayerController _controller;
        private PlayerHealth _health;
        private Rigidbody2D _body;
        private int _lastHealth = -1;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<PlayerHealth>();
            _body = GetComponent<Rigidbody2D>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (spriteRenderer == null && animator != null)
            {
                spriteRenderer = animator.GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null && idleSprite != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = idleSprite;
            }
            else
            {
                EnsureVisibleSprite();
            }
        }

        private void OnEnable()
        {
            PlayerHealth.HealthChanged += OnHealthChanged;
            EnsureVisibleSprite();
        }

        private void OnDisable()
        {
            PlayerHealth.HealthChanged -= OnHealthChanged;
        }

        private void Update()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
                if (spriteRenderer.sprite == null)
                {
                    EnsureVisibleSprite();
                }
            }

            if (animator == null || !animator.enabled)
            {
                return;
            }

            bool dead = _health != null && _health.IsDead;
            GameState state = GameManager.Instance != null ? GameManager.Instance.State : GameState.Playing;
            bool keepRunSpeed = state == GameState.Playing || state == GameState.Paused;

            if (dead)
            {
                animator.SetFloat(SpeedHash, 0f);
                animator.SetBool(GroundedHash, true);
                animator.SetFloat(VerticalSpeedHash, 0f);
                return;
            }

            float speed = 0f;
            if (keepRunSpeed && _controller != null)
            {
                speed = _controller.CurrentSpeed;
            }

            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(GroundedHash, _controller != null && _controller.IsGrounded);
            animator.SetFloat(VerticalSpeedHash, _body != null ? _body.linearVelocity.y : 0f);
        }

        private void EnsureVisibleSprite()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.enabled = true;

            if (spriteRenderer.sprite != null)
            {
                return;
            }

            Sprite fallback = ResolveFallbackSprite();
            if (fallback != null)
            {
                spriteRenderer.sprite = fallback;
            }
        }

        private Sprite ResolveFallbackSprite()
        {
            if (idleSprite != null)
            {
                return idleSprite;
            }

            if (runSprites != null)
            {
                for (int i = 0; i < runSprites.Length; i++)
                {
                    if (runSprites[i] != null)
                    {
                        return runSprites[i];
                    }
                }
            }

            if (jumpSprite != null)
            {
                return jumpSprite;
            }

            if (fallSprite != null)
            {
                return fallSprite;
            }

            return hitSprite;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (_lastHealth < 0)
            {
                _lastHealth = current;
                return;
            }

            if (current < _lastHealth && animator != null && animator.enabled)
            {
                animator.ResetTrigger(HitHash);
                animator.SetTrigger(HitHash);
            }

            _lastHealth = current;
        }
    }
}
