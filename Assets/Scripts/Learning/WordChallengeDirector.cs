using System;
using System.Collections;
using System.Collections.Generic;
using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using FootballRunner.Obstacles;
using FootballRunner.Player;
using FootballRunner.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FootballRunner.Learning
{
    /// <summary>
    /// "Catch the word": a voice says a word, picture bubbles float ahead, and the
    /// runner jumps into the matching one. Learning happens inside the jump the
    /// game already teaches, so the run never stops for a quiz.
    /// </summary>
    public class WordChallengeDirector : MonoBehaviour
    {
        public static WordChallengeDirector Instance { get; private set; }

        /// <summary>A new word was asked. The HUD shows it and offers a replay button.</summary>
        public static event Action<WordEntry> ChallengeStarted;

        /// <summary>The word was caught (true) or passed without catching it (false).</summary>
        public static event Action<WordEntry, bool> ChallengeEnded;

        [Header("Timing")]
        [SerializeField] private float firstChallengeSeconds = 4f;
        [SerializeField] private float secondsBetweenChallenges = 8f;
        [Tooltip("How far ahead, in seconds of running, the first bubble appears.")]
        [SerializeField] private float leadSeconds = 2.4f;
        [Tooltip("Running time between neighbouring bubbles.")]
        [SerializeField] private float spacingSeconds = 0.75f;

        [Header("Layout")]
        [SerializeField] private int optionCount = 3;
        [SerializeField] private float bubbleRadius = 0.6f;
        [Tooltip("Bubble centre above the top of the standing runner. Out of reach without a jump.")]
        [SerializeField] private float heightAboveHead = 1.05f;
        [Tooltip("No bubble this close to a hazard, so jumping a spike never grabs a word by accident.")]
        [SerializeField] private float obstacleClearance = 3.5f;

        [Header("Reward")]
        [SerializeField] private int pointsForCorrect = 50;

        private readonly List<WordBubble> _bubbles = new List<WordBubble>(4);
        private readonly List<WordEntry> _options = new List<WordEntry>(4);
        private readonly List<float> _obstacleXs = new List<float>(32);
        private WordPack _pack;
        private WordEntry _target;
        private WordEntry _previousTarget;
        private Collider2D _playerCollider;
        private Material _spriteMaterial;
        private int _groundMask;
        private float _timer;
        private float _standingTop = float.NaN;
        private float _groundY = float.NaN;
        private bool _active;
        private bool _answered;
        private bool _missRecorded;
        private Coroutine _voiceRoutine;

        public WordPack Pack => _pack;

        public WordEntry CurrentWord => _active ? _target : null;

        public IReadOnlyList<WordBubble> Bubbles => _bubbles;

        public static WordChallengeDirector EnsureOn(GameObject host)
        {
            if (Instance != null)
            {
                return Instance;
            }

            if (host == null)
            {
                return null;
            }

            var director = host.GetComponent<WordChallengeDirector>();
            return director != null ? director : host.AddComponent<WordChallengeDirector>();
        }

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
            _pack = WordPack.Load(WordPack.DefaultPack);

            if (_pack == null || _pack.Words.Count < optionCount)
            {
                Debug.LogWarning("[WordChallengeDirector] Not enough words to run challenges. Disabled.");
                enabled = false;
                return;
            }

            _groundMask = LayerMask.GetMask("Ground");
            _timer = firstChallengeSeconds;

            if (PlayerController.Instance != null)
            {
                _playerCollider = PlayerController.Instance.GetComponent<Collider2D>();
                var renderer = PlayerController.Instance.GetComponentInChildren<SpriteRenderer>();
                _spriteMaterial = renderer != null ? renderer.sharedMaterial : null;
            }
        }

        private void Update()
        {
            PlayerController player = PlayerController.Instance;

            if (player == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                return;
            }

            CacheStandingHeight(player);

            if (_active)
            {
                CheckPassed(player.transform.position.x);
                return;
            }

            _timer -= Time.deltaTime;

            if (_timer <= 0f && !TryStartChallenge(player))
            {
                // Somewhere unsuitable right now (pit, hazards). Try again shortly.
                _timer = 0.5f;
            }
        }

        /// <summary>Replays the current word, for the speaker button on the HUD.</summary>
        public void ReplayWord()
        {
            if (_active && _target != null)
            {
                PlayVoice(_pack.Voice(LearningProgress.Language, _target.id));
            }
        }

        /// <summary>Starts a challenge immediately. Used by tests and the first-run tutorial.</summary>
        public bool ForceChallenge()
        {
            PlayerController player = PlayerController.Instance;

            if (_active || player == null)
            {
                return false;
            }

            CacheStandingHeight(player);
            return TryStartChallenge(player);
        }

        public void OnBubbleTouched(WordBubble bubble)
        {
            if (!_active || bubble == null || bubble.IsFinished)
            {
                return;
            }

            if (bubble.Word == _target)
            {
                if (!_answered)
                {
                    HandleCorrect(bubble);
                }

                return;
            }

            bubble.PopWrong();

            if (!_missRecorded)
            {
                _missRecorded = true;
                LearningProgress.RecordMiss(_target.id);
            }

            for (int i = 0; i < _bubbles.Count; i++)
            {
                if (_bubbles[i] != null && _bubbles[i].Word == _target)
                {
                    _bubbles[i].Highlight();
                }
            }

            ReplayWord();
        }

        private void HandleCorrect(WordBubble bubble)
        {
            _answered = true;
            bubble.PopCorrect();

            for (int i = 0; i < _bubbles.Count; i++)
            {
                if (_bubbles[i] != null && _bubbles[i] != bubble)
                {
                    _bubbles[i].FadeAway();
                }
            }

            // A word caught after a wrong try still counts as practice, not as known.
            if (!_missRecorded)
            {
                LearningProgress.RecordCorrect(_target.id);
            }

            ScoreManager.Instance?.AddPoints(pointsForCorrect);
            FloatingTextSpawner.Instance?.Show($"+{pointsForCorrect}", bubble.transform.position);
            PlayPraiseThenWord(_target);
            EndChallenge(true);
        }

        private void CheckPassed(float playerX)
        {
            float lastX = float.NegativeInfinity;

            for (int i = 0; i < _bubbles.Count; i++)
            {
                if (_bubbles[i] != null)
                {
                    lastX = Mathf.Max(lastX, _bubbles[i].transform.position.x);
                }
            }

            if (playerX < lastX + bubbleRadius + 1f)
            {
                return;
            }

            if (!_missRecorded)
            {
                _missRecorded = true;
                LearningProgress.RecordMiss(_target.id);
            }

            EndChallenge(false);
        }

        private void EndChallenge(bool caught)
        {
            _active = false;
            _timer = secondsBetweenChallenges;
            WordEntry word = _target;

            for (int i = 0; i < _bubbles.Count; i++)
            {
                if (_bubbles[i] != null && !_bubbles[i].IsFinished)
                {
                    _bubbles[i].FadeAway();
                }
            }

            _bubbles.Clear();
            ChallengeEnded?.Invoke(word, caught);
        }

        private bool TryStartChallenge(PlayerController player)
        {
            if (float.IsNaN(_standingTop))
            {
                return false;
            }

            ChooseWords();
            CacheObstacles();

            float speed = Mathf.Max(1f, player.CurrentSpeed);
            float spacing = Mathf.Max(bubbleRadius * 2f + 1.6f, speed * spacingSeconds);
            float searchFrom = player.transform.position.x + speed * leadSeconds;
            float y = _standingTop + heightAboveHead;
            var positions = new List<float>(_options.Count);

            for (int i = 0; i < _options.Count; i++)
            {
                if (!FindSlot(searchFrom, out float x))
                {
                    return false;
                }

                positions.Add(x);
                searchFrom = x + spacing;
            }

            _active = true;
            _answered = false;
            _missRecorded = false;

            for (int i = 0; i < _options.Count; i++)
            {
                WordEntry word = _options[i];
                _bubbles.Add(WordBubble.Create(transform, new Vector3(positions[i], y, 0f), word,
                    _pack.Picture(word.id), bubbleRadius, _spriteMaterial, this));
            }

            _previousTarget = _target;
            ReplayWord();
            ChallengeStarted?.Invoke(_target);
            return true;
        }

        /// <summary>Weighted pick of the target, then random distinct distractors in random order.</summary>
        private void ChooseWords()
        {
            IReadOnlyList<WordEntry> words = _pack.Words;
            float total = 0f;

            for (int i = 0; i < words.Count; i++)
            {
                total += words[i] == _previousTarget ? 0f : LearningProgress.Weight(words[i].id);
            }

            float roll = Random.value * total;
            _target = words[0];

            for (int i = 0; i < words.Count; i++)
            {
                if (words[i] == _previousTarget)
                {
                    continue;
                }

                roll -= LearningProgress.Weight(words[i].id);

                if (roll <= 0f)
                {
                    _target = words[i];
                    break;
                }
            }

            _options.Clear();
            _options.Add(_target);
            int guard = 0;

            while (_options.Count < optionCount && guard++ < 100)
            {
                WordEntry candidate = words[Random.Range(0, words.Count)];

                if (!_options.Contains(candidate))
                {
                    _options.Add(candidate);
                }
            }

            for (int i = _options.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_options[i], _options[j]) = (_options[j], _options[i]);
            }
        }

        private void CacheObstacles()
        {
            _obstacleXs.Clear();
            Obstacle[] obstacles = FindObjectsByType<Obstacle>(FindObjectsSortMode.None);

            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i].isActiveAndEnabled)
                {
                    _obstacleXs.Add(obstacles[i].transform.position.x);
                }
            }
        }

        /// <summary>
        /// First x at or after <paramref name="fromX"/> with solid ground at running
        /// height below and no hazard nearby. Pits and raised platforms are skipped
        /// because a jump there is needed for other reasons.
        /// </summary>
        private bool FindSlot(float fromX, out float x)
        {
            const float step = 0.5f;
            const float searchDistance = 24f;

            for (float candidate = fromX; candidate < fromX + searchDistance; candidate += step)
            {
                if (IsNearObstacle(candidate) || !HasFlatGround(candidate))
                {
                    continue;
                }

                x = candidate;
                return true;
            }

            x = 0f;
            return false;
        }

        private bool IsNearObstacle(float x)
        {
            for (int i = 0; i < _obstacleXs.Count; i++)
            {
                if (Mathf.Abs(_obstacleXs[i] - x) < obstacleClearance)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasFlatGround(float x)
        {
            if (_groundMask == 0 || float.IsNaN(_groundY))
            {
                return true;
            }

            // Check both edges of the bubble's footprint so it never hangs over a pit edge.
            for (int side = -1; side <= 1; side++)
            {
                var origin = new Vector2(x + side * (bubbleRadius + 0.6f), _standingTop + 6f);
                RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 20f, _groundMask);

                if (hit.collider == null || Mathf.Abs(hit.point.y - _groundY) > 0.35f)
                {
                    return false;
                }
            }

            return true;
        }

        private void CacheStandingHeight(PlayerController player)
        {
            if (!float.IsNaN(_standingTop) || !player.IsGrounded || _playerCollider == null)
            {
                return;
            }

            Bounds bounds = _playerCollider.bounds;
            _standingTop = bounds.max.y;
            _groundY = bounds.min.y;
        }

        private void PlayPraiseThenWord(WordEntry word)
        {
            if (_voiceRoutine != null)
            {
                StopCoroutine(_voiceRoutine);
            }

            _voiceRoutine = StartCoroutine(PraiseThenWord(word));
        }

        private IEnumerator PraiseThenWord(WordEntry word)
        {
            string language = LearningProgress.Language;
            IReadOnlyList<WordEntry> phrases = _pack.Phrases;

            if (phrases.Count > 0)
            {
                AudioClip praise = _pack.Voice(language, phrases[Random.Range(0, phrases.Count)].id);
                PlayVoice(praise);

                if (praise != null)
                {
                    yield return new WaitForSeconds(praise.length + 0.15f);
                }
            }

            PlayVoice(_pack.Voice(language, word.id));
            _voiceRoutine = null;
        }

        private static void PlayVoice(AudioClip clip)
        {
            if (clip != null)
            {
                AudioManager.Instance?.PlayVoice(clip);
            }
        }
    }
}
