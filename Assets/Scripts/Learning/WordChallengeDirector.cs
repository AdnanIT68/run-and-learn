using System;
using System.Collections;
using System.Collections.Generic;
using FootballRunner.Audio;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using FootballRunner.Obstacles;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
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

        /// <summary>A word was caught towards the run goal: (word, caught so far, goal size).</summary>
        public static event Action<WordEntry, int, int> GoalProgress;

        /// <summary>The goal row filled up. The HUD celebrates and the row starts again.</summary>
        public static event Action<int> GoalCompleted;

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
        [Tooltip("Words to catch to fill the goal row shown on the HUD.")]
        [SerializeField] private int goalSize = 5;
        [SerializeField] private int coinsForGoal = 10;

        [Header("First-Time Guide")]
        [Tooltip("Game speed while the runner approaches the right bubble in the guided first word.")]
        [SerializeField] private float guidedTimeScale = 0.45f;
        [Tooltip("Distance before the right bubble at which the guide slows the game and says \"Jump!\".")]
        [SerializeField] private float guidedSlowDistance = 3.2f;

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
        private bool _guided;
        private bool _jumpCuePlayed;
        private bool _slowedByGuide;
        private int _goalCaught;
        private Coroutine _voiceRoutine;

        public WordPack Pack => _pack;

        public int GoalSize => goalSize;

        public int GoalCaught => _goalCaught;

        /// <summary>True while the first-ever word is being shown with the guide.</summary>
        public bool IsGuided => _active && _guided;

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
            _pack = WordPack.Load(LearningProgress.SelectedPack);

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
                if (_guided)
                {
                    GuideApproach(player.transform.position.x);
                }

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

            if (_guided)
            {
                SaveManager.Data.wordTutorialDone = true;
                SaveManager.Save();
            }

            ScoreManager.Instance?.AddPoints(pointsForCorrect);
            FloatingTextSpawner.Instance?.Show($"+{pointsForCorrect}", bubble.transform.position);

            WordEntry word = _target;
            _goalCaught++;
            GoalProgress?.Invoke(word, _goalCaught, goalSize);
            bool goalReached = _goalCaught >= goalSize;

            PlayPraiseThenWord(word, goalReached);
            EndChallenge(true);

            if (goalReached)
            {
                _goalCaught = 0;
                ScoreManager.Instance?.AddCoins(coinsForGoal);
                GoalCompleted?.Invoke(goalSize);
            }
        }

        /// <summary>
        /// First word ever: slow the game just before the right bubble and say
        /// "Jump!", so the child sees what the bubbles are for.
        /// </summary>
        private void GuideApproach(float playerX)
        {
            WordBubble right = null;

            for (int i = 0; i < _bubbles.Count; i++)
            {
                if (_bubbles[i] != null && _bubbles[i].Word == _target && !_bubbles[i].IsFinished)
                {
                    right = _bubbles[i];
                }
            }

            float distance = right != null ? right.transform.position.x - playerX : -1f;
            bool approaching = distance > 0f && distance < guidedSlowDistance;

            if (approaching && !_slowedByGuide)
            {
                _slowedByGuide = true;
                Time.timeScale = guidedTimeScale;

                if (!_jumpCuePlayed)
                {
                    _jumpCuePlayed = true;
                    PlayVoice(_pack.Voice(LearningProgress.Language, "jump"));
                }
            }
            else if (!approaching && _slowedByGuide)
            {
                RestoreGuideSpeed();
            }
        }

        private void RestoreGuideSpeed()
        {
            if (!_slowedByGuide)
            {
                return;
            }

            _slowedByGuide = false;

            // Pause and Game Over own timeScale; only undo the guide's own slow-down.
            if (Mathf.Approximately(Time.timeScale, guidedTimeScale))
            {
                Time.timeScale = 1f;
            }
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
            RestoreGuideSpeed();
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

            bool guided = !SaveManager.Data.wordTutorialDone;
            AudioClip intro = guided ? _pack.Voice(LearningProgress.HomeLanguage, "intro") : null;
            // The guided intro is spoken first, so the bubbles start further ahead.
            float lead = leadSeconds + (intro != null ? intro.length + 0.4f : 0f);

            float speed = Mathf.Max(1f, player.CurrentSpeed);
            float spacing = Mathf.Max(bubbleRadius * 2f + 1.6f, speed * spacingSeconds);
            float searchFrom = player.transform.position.x + speed * lead;
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
            _guided = guided;
            _jumpCuePlayed = false;

            for (int i = 0; i < _options.Count; i++)
            {
                WordEntry word = _options[i];
                WordBubble bubble = WordBubble.Create(transform, new Vector3(positions[i], y, 0f), word,
                    _pack.Picture(word.id), bubbleRadius, _spriteMaterial, this);
                _bubbles.Add(bubble);

                if (_guided && word == _target)
                {
                    bubble.Highlight();
                }
            }

            _previousTarget = _target;

            if (_guided)
            {
                PlayInSequence(intro, _pack.Voice(LearningProgress.Language, _target.id));
            }
            else
            {
                ReplayWord();
            }

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

        /// <summary>"Bravo!", then the word again, then the goal line when the row is full.</summary>
        private void PlayPraiseThenWord(WordEntry word, bool goalReached)
        {
            string language = LearningProgress.Language;
            PlayInSequence(
                _pack.RandomPraise(language),
                _pack.Voice(language, word.id),
                goalReached ? _pack.Voice(language, "goal") : null);
        }

        private void PlayInSequence(params AudioClip[] clips)
        {
            if (_voiceRoutine != null)
            {
                StopCoroutine(_voiceRoutine);
            }

            _voiceRoutine = StartCoroutine(Sequence(clips));
        }

        private IEnumerator Sequence(AudioClip[] clips)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                {
                    continue;
                }

                PlayVoice(clips[i]);
                // Realtime, so the guide's slow motion does not stretch the gaps.
                yield return new WaitForSecondsRealtime(clips[i].length + 0.15f);
            }

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
