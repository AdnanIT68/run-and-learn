using FootballRunner.Player;
using UnityEngine;

namespace FootballRunner.Learning
{
    /// <summary>
    /// Pepi the parrot flies with Rukajja and speaks both languages. He reacts to
    /// every word: a hop when one is asked, a somersault when it is caught, a head
    /// shake when it is missed. On the guided first word he flies to the right
    /// bubble, so the child sees where to jump.
    /// </summary>
    public class PepiCompanion : MonoBehaviour
    {
        private const int SortingOrder = 26;

        public static PepiCompanion Instance { get; private set; }

        [SerializeField] private Vector2 followOffset = new Vector2(-1.4f, 2.4f);
        [SerializeField] private float followSharpness = 5f;
        [SerializeField] private float size = 1.1f;
        [Tooltip("How far ahead of the runner a guided bubble may be before Pepi flies to it.")]
        [SerializeField] private float guideReach = 12f;

        private Transform _body;
        private SpriteRenderer _renderer;
        private float _hopTime = -1f;
        private float _spinTime = -1f;
        private float _spinTurns;
        private float _shakeTime = -1f;
        private float _fittedScale = 1f;

        /// <summary>True while Pepi is pointing at the right bubble for the guided word.</summary>
        public bool IsPointing { get; private set; }

        public static PepiCompanion EnsureOn()
        {
            if (Instance != null)
            {
                return Instance;
            }

            return new GameObject("Pepi").AddComponent<PepiCompanion>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            _body = body.transform;
            _renderer = body.AddComponent<SpriteRenderer>();
            _renderer.sprite = WordPack.Icon("parrot");
            // The parrot art faces left; the runner travels right.
            _renderer.flipX = true;
            _renderer.sortingOrder = SortingOrder;

            if (_renderer.sprite != null)
            {
                _fittedScale = size / Mathf.Max(0.01f, _renderer.sprite.bounds.size.x);
            }

            _body.localScale = Vector3.one * _fittedScale;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            WordChallengeDirector.ChallengeStarted += OnChallengeStarted;
            WordChallengeDirector.ChallengeEnded += OnChallengeEnded;
            WordChallengeDirector.GoalCompleted += OnGoalCompleted;
            PlayerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            WordChallengeDirector.ChallengeStarted -= OnChallengeStarted;
            WordChallengeDirector.ChallengeEnded -= OnChallengeEnded;
            WordChallengeDirector.GoalCompleted -= OnGoalCompleted;
            PlayerHealth.Died -= OnPlayerDied;
        }

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                transform.position = PlayerController.Instance.transform.position + (Vector3)followOffset;
            }
        }

        private void LateUpdate()
        {
            PlayerController player = PlayerController.Instance;

            if (player == null)
            {
                return;
            }

            Vector3 target = player.transform.position + (Vector3)followOffset;
            IsPointing = TryGetGuideTarget(player.transform.position.x, ref target);

            float blend = 1f - Mathf.Exp(-followSharpness * (IsPointing ? 1.6f : 1f) * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, blend);
            Animate();
        }

        private bool TryGetGuideTarget(float playerX, ref Vector3 target)
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;

            if (director == null || !director.IsGuided || director.CurrentWord == null)
            {
                return false;
            }

            foreach (WordBubble bubble in director.Bubbles)
            {
                if (bubble == null || bubble.IsFinished || bubble.Word != director.CurrentWord)
                {
                    continue;
                }

                Vector3 bubblePosition = bubble.transform.position;
                float ahead = bubblePosition.x - playerX;

                if (ahead > 0f && ahead < guideReach)
                {
                    target = bubblePosition + new Vector3(-0.2f, 1.1f, 0f);
                    return true;
                }
            }

            return false;
        }

        private void Animate()
        {
            float time = Time.time;
            float bob = Mathf.Sin(time * 3f) * 0.15f;
            float flap = 1f + Mathf.Sin(time * 14f) * 0.05f;
            float hop = 0f;
            float angle = 0f;

            if (_hopTime >= 0f)
            {
                _hopTime += Time.deltaTime;
                hop = Mathf.Sin(Mathf.Clamp01(_hopTime / 0.35f) * Mathf.PI) * 0.45f;

                if (_hopTime >= 0.35f)
                {
                    _hopTime = -1f;
                }
            }

            if (_spinTime >= 0f)
            {
                _spinTime += Time.deltaTime;
                float duration = 0.5f * _spinTurns;
                angle = Mathf.SmoothStep(0f, 360f * _spinTurns, Mathf.Clamp01(_spinTime / duration));

                if (_spinTime >= duration)
                {
                    _spinTime = -1f;
                }
            }
            else if (_shakeTime >= 0f)
            {
                _shakeTime += Time.deltaTime;
                angle = Mathf.Sin(_shakeTime * 40f) * 16f * (1f - _shakeTime / 0.45f);

                if (_shakeTime >= 0.45f)
                {
                    _shakeTime = -1f;
                }
            }

            _body.localPosition = new Vector3(0f, bob + hop, 0f);
            _body.localRotation = Quaternion.Euler(0f, 0f, -angle);
            _body.localScale = new Vector3(_fittedScale, _fittedScale * flap, 1f);
        }

        private void OnChallengeStarted(WordEntry word)
        {
            _hopTime = 0f;
        }

        private void OnChallengeEnded(WordEntry word, bool caught)
        {
            if (caught)
            {
                Spin(1f);
            }
            else
            {
                _shakeTime = 0f;
            }
        }

        private void OnGoalCompleted(int goal)
        {
            Spin(2f);
        }

        private void OnPlayerDied()
        {
            _shakeTime = 0f;
        }

        private void Spin(float turns)
        {
            _spinTurns = turns;
            _spinTime = 0f;
            _shakeTime = -1f;
        }
    }
}
