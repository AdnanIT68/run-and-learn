using UnityEngine;

namespace FootballRunner.Player
{
    /// <summary>
    /// Frame-swap animation for the runner. There is no Animator controller on
    /// purpose: a short sprite cycle is cheaper on mobile and easier to retune
    /// than Mecanim for these few states.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        private const float ReferenceRunSpeed = 7f;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Frames")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] runSprites;
        [SerializeField] private Sprite jumpSprite;
        [Tooltip("Optional. When empty, the jump sprite is also used while falling.")]
        [SerializeField] private Sprite fallSprite;
        [SerializeField] private Sprite hitSprite;

        [Header("Timing")]
        [SerializeField] private float runFramesPerSecond = 10f;

        [Header("Landing Squash")]
        [Tooltip("How far the sprite squashes on touchdown, as a fraction of its height.")]
        [SerializeField] private float landSquash = 0.18f;
        [Tooltip("How quickly the squash springs back. Higher is snappier.")]
        [SerializeField] private float squashRecovery = 7f;

        private PlayerController _controller;
        private PlayerHealth _health;
        private Rigidbody2D _body;
        private Vector3 _baseScale;
        private Sprite[] _runCycle;
        private float _frameTimer;
        private int _frameIndex;
        private bool _wasGrounded = true;
        private float _squash;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<PlayerHealth>();
            _body = GetComponent<Rigidbody2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                _baseScale = spriteRenderer.transform.localScale;
            }

            BuildRunCycle();
        }

        private void Update()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            bool grounded = _controller != null && _controller.IsGrounded;

            if (grounded && !_wasGrounded)
            {
                _squash = landSquash;
                _frameIndex = 0;
                _frameTimer = 0f;
            }

            _wasGrounded = grounded;

            UpdateFrame(grounded);
            UpdateSquash();
        }

        /// <summary>
        /// Two opposite walk poses read as a frozen waddle. Insert the idle
        /// (passing) pose between them so the legs actually change contact.
        /// </summary>
        private void BuildRunCycle()
        {
            if (runSprites == null || runSprites.Length == 0)
            {
                _runCycle = System.Array.Empty<Sprite>();
                return;
            }

            bool twoStep = runSprites.Length == 2 &&
                           idleSprite != null &&
                           runSprites[0] != null &&
                           runSprites[1] != null &&
                           runSprites[0] != idleSprite &&
                           runSprites[1] != idleSprite &&
                           runSprites[0] != runSprites[1];

            if (twoStep)
            {
                _runCycle = new[] { runSprites[0], idleSprite, runSprites[1], idleSprite };
                return;
            }

            _runCycle = runSprites;
        }

        private void UpdateFrame(bool grounded)
        {
            if (_health != null && _health.IsDead)
            {
                if (hitSprite != null)
                {
                    spriteRenderer.sprite = hitSprite;
                }

                return;
            }

            if (_health != null && _health.IsInvulnerable && hitSprite != null)
            {
                spriteRenderer.sprite = hitSprite;
                return;
            }

            if (!grounded)
            {
                bool falling = _body != null && _body.linearVelocity.y < 0f;
                Sprite airSprite = falling && fallSprite != null ? fallSprite : jumpSprite;

                if (airSprite != null)
                {
                    spriteRenderer.sprite = airSprite;
                }

                return;
            }

            if (_runCycle == null || _runCycle.Length == 0)
            {
                if (idleSprite != null)
                {
                    spriteRenderer.sprite = idleSprite;
                }

                return;
            }

            float speed = _controller != null ? _controller.CurrentSpeed : ReferenceRunSpeed;
            float rate = runFramesPerSecond * Mathf.Clamp(speed / ReferenceRunSpeed, 0.75f, 1.55f);
            _frameTimer += Time.deltaTime * Mathf.Max(0.01f, rate);

            if (_frameTimer >= 1f)
            {
                _frameTimer -= Mathf.Floor(_frameTimer);
                _frameIndex = (_frameIndex + 1) % _runCycle.Length;
            }

            Sprite frame = _runCycle[_frameIndex];

            if (frame != null)
            {
                spriteRenderer.sprite = frame;
            }
        }

        /// <summary>
        /// Widen and flatten briefly on landing. It is a small effect but it is
        /// most of what makes a jump read as having weight.
        /// </summary>
        private void UpdateSquash()
        {
            if (_squash <= 0.0001f)
            {
                spriteRenderer.transform.localScale = _baseScale;
                return;
            }

            _squash = Mathf.MoveTowards(_squash, 0f, squashRecovery * landSquash * Time.deltaTime);

            spriteRenderer.transform.localScale = new Vector3(
                _baseScale.x * (1f + _squash),
                _baseScale.y * (1f - _squash),
                _baseScale.z);
        }
    }
}
