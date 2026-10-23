using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using UnityEngine;

namespace FootballRunner.Player
{
    /// <summary>
    /// Automatic forward running plus a jump tuned to feel responsive rather than
    /// physically accurate. Coyote time and jump buffering hide the small timing
    /// errors every player makes, which is most of what "tight controls" means.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Transform groundCheck;

        [Header("Ground Detection")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.48f, 0.14f);
        [SerializeField] private LayerMask groundLayers;

        [Header("Running")]
        [Tooltip("Used only when no DifficultyManager is present in the scene.")]
        [SerializeField] private float fallbackSpeed = 7f;

        [Header("Jumping")]
        [SerializeField] private float jumpVelocity = 14f;
        [Tooltip("1 = ground jump only. Values above 1 add extra air jumps.")]
        [SerializeField] private int maxJumps = 1;
        [Tooltip("Grace period after walking off an edge during which a jump still works.")]
        [SerializeField] private float coyoteTime = 0.12f;
        [Tooltip("A jump pressed this long before landing still fires on touchdown.")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        [Tooltip("Extra gravity while falling, so the arc feels snappy instead of floaty.")]
        [SerializeField] private float fallGravityMultiplier = 1.9f;
        [Tooltip("Extra gravity while rising after the player let go, giving short hops.")]
        [SerializeField] private float shortHopGravityMultiplier = 2.6f;

        [Header("Death")]
        [Tooltip("Falling below this height ends the run.")]
        [SerializeField] private float fallDeathHeight = -8f;

        private readonly Collider2D[] _groundHits = new Collider2D[4];
        private ContactFilter2D _groundFilter;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private int _airJumpsRemaining;
        private bool _isGrounded;
        private bool _jumpThisStep;
        private PlayerHealth _health;

        public bool IsGrounded => _isGrounded;

        public float CurrentSpeed =>
            DifficultyManager.Instance != null ? DifficultyManager.Instance.CurrentSpeed : fallbackSpeed;

        private void Awake()
        {
            Instance = this;

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            if (body.sharedMaterial == null || body.sharedMaterial.friction > 0.001f)
            {
                var material = new PhysicsMaterial2D("Player No Friction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
                body.sharedMaterial = material;

                var collider = GetComponent<Collider2D>();

                if (collider != null)
                {
                    collider.sharedMaterial = material;
                }
            }

            _health = GetComponent<PlayerHealth>();
            _airJumpsRemaining = Mathf.Max(0, maxJumps - 1);

            _groundFilter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                useDepth = false
            };
            _groundFilter.SetLayerMask(groundLayers);
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
            if (!CanControl())
            {
                return;
            }

            if (InputReader.JumpPressed())
            {
                _jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                _jumpBufferTimer -= Time.deltaTime;
            }
        }

        private void FixedUpdate()
        {
            if (!CanControl())
            {
                return;
            }

            _jumpThisStep = false;
            UpdateGrounded();
            ApplyForwardMotion();
            TryJump();
            ApplyBetterGravity();
            CheckFellOffWorld();
        }

        /// <summary>Stops forward travel so the death pose does not keep running into hazards.</summary>
        public void HaltMotion()
        {
            if (body == null)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = 0f;
            body.linearVelocity = velocity;
        }

        private bool CanControl()
        {
            if (_health != null && _health.IsDead)
            {
                return false;
            }

            return GameManager.Instance == null || GameManager.Instance.IsPlaying;
        }

        private void UpdateGrounded()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = ProbeGround();

            if (_isGrounded)
            {
                _coyoteTimer = coyoteTime;

                if (!wasGrounded && !_jumpThisStep)
                {
                    _airJumpsRemaining = Mathf.Max(0, maxJumps - 1);
                }
            }
            else
            {
                _coyoteTimer -= Time.fixedDeltaTime;
            }
        }

        /// <summary>
        /// Overlap that ignores triggers. The previous circle probe used the global
        /// Physics2D query setting, so coins and spike triggers could report the
        /// runner as grounded mid-air.
        /// </summary>
        private bool ProbeGround()
        {
            if (groundCheck == null)
            {
                return false;
            }

            int hits = Physics2D.OverlapBox(
                groundCheck.position, groundCheckSize, 0f, _groundFilter, _groundHits);

            return hits > 0;
        }

        private void ApplyForwardMotion()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = CurrentSpeed;

            // Kill the tiny downward bounce that box-collider seams produce, otherwise
            // the runner looks stuck or hops at every chunk join.
            if (_isGrounded && !_jumpThisStep && velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            body.linearVelocity = velocity;
        }

        private void TryJump()
        {
            if (_jumpBufferTimer <= 0f)
            {
                return;
            }

            bool canGroundJump = _isGrounded || _coyoteTimer > 0f;
            bool canAirJump = !_isGrounded && _coyoteTimer <= 0f && _airJumpsRemaining > 0;

            if (!canGroundJump && !canAirJump)
            {
                return;
            }

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _isGrounded = false;
            _jumpThisStep = true;

            if (canAirJump)
            {
                _airJumpsRemaining--;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.y = jumpVelocity;
            body.linearVelocity = velocity;

            AudioManager.Instance?.PlayJump();
        }

        /// <summary>
        /// Plain gravity makes a jump feel like floating. Falling faster than you
        /// rose, and cutting the rise short when the button is released, is the
        /// standard fix and costs nothing in performance.
        /// </summary>
        private void ApplyBetterGravity()
        {
            if (_isGrounded)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;

            if (velocity.y < 0f)
            {
                velocity += Vector2.up * (Physics2D.gravity.y * body.gravityScale *
                                          (fallGravityMultiplier - 1f) * Time.fixedDeltaTime);
            }
            else if (velocity.y > 0f && !InputReader.JumpHeld())
            {
                velocity += Vector2.up * (Physics2D.gravity.y * body.gravityScale *
                                          (shortHopGravityMultiplier - 1f) * Time.fixedDeltaTime);
            }

            body.linearVelocity = velocity;
        }

        private void CheckFellOffWorld()
        {
            if (transform.position.y > fallDeathHeight)
            {
                return;
            }

            if (_health != null)
            {
                _health.Kill();
            }
            else
            {
                GameManager.Instance?.GameOver();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
            {
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}
