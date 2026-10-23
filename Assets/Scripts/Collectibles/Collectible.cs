using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using FootballRunner.UI;
using UnityEngine;

namespace FootballRunner.Collectibles
{
    /// <summary>
    /// A single prefab drives every pickup type. It reconfigures itself from a
    /// <see cref="CollectibleData"/> asset when spawned, which means one pool
    /// serves coins, gems and stars alike.
    ///
    /// The spawning chunk owns the instance until it is recycled. Collection
    /// hides the pickup and disables its collider; it must not return the
    /// object to the pool.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private Collider2D _collider;
        private CollectibleData _data;
        private Vector3 _anchorPosition;
        private float _bobOffset;
        private bool _collected;
        private bool _rewarded;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            _collider = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            ResetForReuse();
            _anchorPosition = transform.position;
            _bobOffset = Random.Range(0f, Mathf.PI * 2f);
            transform.rotation = Quaternion.identity;
        }

        public void Configure(CollectibleData data)
        {
            _data = data;
            ResetForReuse();

            if (data == null)
            {
                return;
            }

            if (spriteRenderer != null)
            {
                if (data.sprite != null)
                {
                    spriteRenderer.sprite = data.sprite;
                }

                spriteRenderer.color = data.tint;
                spriteRenderer.enabled = true;
            }

            transform.localScale = Vector3.one * Mathf.Max(0.01f, data.scale);
            transform.rotation = Quaternion.identity;
            _anchorPosition = transform.position;
        }

        private void Update()
        {
            if (_collected)
            {
                return;
            }

            float bob = Mathf.Sin((Time.time + _bobOffset) * bobSpeed) * bobHeight;
            transform.position = _anchorPosition + Vector3.up * bob;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || _data == null)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            var controller = other.GetComponentInParent<Player.PlayerController>();

            if (controller == null)
            {
                return;
            }

            HideCollected();
            Collect(controller.GetComponent<Player.PlayerHealth>());
        }

        private void Collect(Player.PlayerHealth health)
        {
            if (_rewarded || _data == null)
            {
                return;
            }

            _rewarded = true;
            int points = Mathf.Max(0, _data.scorePoints);
            int coins = Mathf.Max(0, _data.coinValue);

            ScoreManager score = ScoreManager.Instance;

            if (score != null)
            {
                score.AddPoints(points);
                score.AddCoins(coins);
            }

            bool healed = false;

            if (_data.healAmount > 0 && health != null)
            {
                int before = health.CurrentHealth;
                health.Heal(_data.healAmount);
                healed = health.CurrentHealth > before;
            }

            if (healed)
            {
                AudioManager.Instance?.PlayPowerUp();
            }
            else if (_data.IsCoin)
            {
                AudioManager.Instance?.PlayCoin();
            }
            else
            {
                AudioManager.Instance?.PlayCollect();
            }

            string label = healed
                ? "+1 LIFE"
                : points > 0 ? $"+{points}" : coins > 0 ? $"+{coins}" : null;

            if (label != null)
            {
                FloatingTextSpawner.Instance?.Show(label, transform.position);
            }
        }

        private void HideCollected()
        {
            _collected = true;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        private void ResetForReuse()
        {
            _collected = false;
            _rewarded = false;

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
        }
    }
}
