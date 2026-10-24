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

        [Header("Counting")]
        [Tooltip("Share of challenges that ask for a number and a word, in worlds that have counting phrases.")]
        [Range(0f, 1f)]
        [SerializeField] private float countingChance = 0.4f;

        [Header("Sizes")]
        [Tooltip("Share of challenges that ask for big or small, in worlds that have size phrases.")]
        [Range(0f, 1f)]
        [SerializeField] private float sizeChance = 0.4f;

        [Header("School")]
        [Range(0f, 1f)] [SerializeField] private float letterChance = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float numberChance = 0.3f;
        [Tooltip("Share of plain words shown silently, to be read.")]
        [Range(0f, 1f)] [SerializeField] private float readingChance = 0.4f;

        [Header("Sentences")]
        [Tooltip("Share of challenges that are whole sentences, in worlds that have them.")]
        [Range(0f, 1f)]
        [SerializeField] private float sentenceChance = 0.45f;

        [Header("Commands")]
        [Tooltip("Share of challenges that are spoken commands, in worlds that have them.")]
        [Range(0f, 1f)]
        [SerializeField] private float commandChance = 0.4f;
        [Tooltip("Height above the standing runner's head that only a double jump reaches.")]
        [SerializeField] private float doubleJumpHeight = 3.4f;

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
        [Tooltip("Seconds of BRAVO and confetti before the result screen opens.")]
        [SerializeField] private float levelEndDelay = 1.8f;

        [Header("First-Time Guide")]
        [Tooltip("Game speed while the runner approaches the right bubble in the guided first word.")]
        [SerializeField] private float guidedTimeScale = 0.45f;
        [Tooltip("Distance before the right bubble at which the guide slows the game and says \"Jump!\".")]
        [SerializeField] private float guidedSlowDistance = 3.2f;

        private readonly List<WordBubble> _bubbles = new List<WordBubble>(4);
        private readonly List<WordEntry> _options = new List<WordEntry>(4);
        private readonly List<float> _obstacleXs = new List<float>(32);
        private WordPack _pack;
        private int _level;
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
        private int _runMisses;
        private WordEntry _spokenWord;
        private readonly List<WordEntry> _rowWords = new List<WordEntry>(8);
        private Coroutine _voiceRoutine;

        public WordPack Pack => _pack;

        /// <summary>The level being played in this world, 0 for level 1.</summary>
        public int Level => _level;

        public int GoalSize => goalSize;

        public int GoalCaught => _goalCaught;

        /// <summary>True once this run's goal row was filled; no new words are asked.</summary>
        public bool LevelDone { get; private set; }

        /// <summary>Stars for the finished level (1 to 3).</summary>
        public int LevelStars { get; private set; }

        /// <summary>The words caught to fill the goal row, for the result screen.</summary>
        public List<WordEntry> LevelWords { get; } = new List<WordEntry>(8);

        /// <summary>Mistakes so far in this run: wrong bubbles and words passed by.</summary>
        public int RunMisses => _runMisses;

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

            // Loaded here, before any Start, so the HUD sizes the goal row correctly.
            _pack = WordPack.Load(LearningProgress.SelectedPack);
            _level = Adventure.AdventureSession.IsActive ? 0 : LearningProgress.SelectedLevel;

            if (_pack != null && _pack.GoalSize > 0)
            {
                goalSize = _pack.GoalSize;
            }

            // Later levels ask a few more words: five to eight in the worlds, eight to ten in the finale.
            goalSize += _pack != null && _pack.IsReview ? _level / 2 : Mathf.Min(_level, 3);
            secondsBetweenChallenges *= 1f - 0.06f * _level;
        }

        /// <summary>Later levels lean more on the world's special challenges than on plain words.</summary>
        private float KindBoost => Mathf.Clamp(0.6f + 0.2f * _level, 0.6f, 1.4f);

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {

            if (_pack == null || _pack.ChallengeWords.Count < optionCount)
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

            if (LevelDone)
            {
                return;
            }

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

            if (_timer <= 0f && !TryStartChallenge(player, null))
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
        public bool ForceChallenge(bool counting = false, bool command = false, bool size = false,
            bool sentence = false)
        {
            Kind? kind = counting ? Kind.Counting
                : command ? Kind.Command
                : size ? Kind.Size
                : sentence ? Kind.Sentence
                : (Kind?)null;
            return ForceChallenge(kind);
        }

        /// <summary>Starts a challenge of the given kind (the usual pick for null) right now.</summary>
        public bool ForceChallenge(Kind? kind)
        {
            PlayerController player = PlayerController.Instance;

            if (_active || player == null)
            {
                return false;
            }

            CacheStandingHeight(player);
            return TryStartChallenge(player, kind);
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
                _runMisses++;
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
            _rowWords.Add(word);
            _goalCaught++;
            GoalProgress?.Invoke(word, _goalCaught, goalSize);
            bool goalReached = _goalCaught >= goalSize;

            // A letter challenge praises, then names the picture the child caught.
            PlayPraiseThenWord(_spokenWord ?? word, goalReached);
            EndChallenge(true);

            if (goalReached)
            {
                _goalCaught = 0;
                ScoreManager.Instance?.AddCoins(coinsForGoal);
                FinishLevel();
                GoalCompleted?.Invoke(goalSize);
            }
        }

        /// <summary>
        /// The goal row is full: the level is won. Stars come from this run's
        /// mistakes; the result screen opens once BRAVO and the confetti have played.
        /// </summary>
        private void FinishLevel()
        {
            LevelDone = true;
            LevelStars = LearningProgress.StarsForMisses(_runMisses);
            LevelWords.Clear();
            LevelWords.AddRange(_rowWords);
            _rowWords.Clear();
            LearningProgress.RecordLevelComplete(_pack.Name, _level, LevelStars);
            StartCoroutine(OpenResultsSoon());
        }

        private IEnumerator OpenResultsSoon()
        {
            yield return new WaitForSecondsRealtime(levelEndDelay);
            GameManager.Instance?.CompleteWordLevel();
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
                _runMisses++;
            }

            EndChallenge(false);
        }

        /// <summary>Drops the challenge on screen without a miss; tests use it to ask the next kind.</summary>
        public void CancelChallenge()
        {
            if (_active)
            {
                EndChallenge(false);
            }
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

        /// <summary>What a challenge asks for. Words are the default; the others depend on the world.</summary>
        public enum Kind
        {
            Word,
            Counting,
            Command,
            Size,
            Sentence,
            Letter,
            Number,
            Reading
        }

        /// <summary>The kind of the challenge on screen.</summary>
        public Kind CurrentKind { get; private set; }

        /// <summary>
        /// Picks what to ask. The guided first word is always a plain word; after that
        /// each kind this world offers gets its chance in turn.
        /// </summary>
        private Kind PickKind(bool guided, Kind? forced)
        {
            if (forced.HasValue)
            {
                return forced.Value;
            }

            if (guided)
            {
                return Kind.Word;
            }

            if (_pack.IsReview)
            {
                return PickReviewKind();
            }

            float boost = KindBoost;
            if (_pack.Counts.Count > 0 && Random.value < countingChance * boost) return Kind.Counting;
            if (_pack.Commands.Count > 0 && Random.value < commandChance * boost) return Kind.Command;
            if (_pack.Sizes.Count > 0 && Random.value < sizeChance * boost) return Kind.Size;
            if (_pack.Sentences.Count > 0 && Random.value < sentenceChance * boost) return Kind.Sentence;
            if (_pack.Letters.Count > 0 && Random.value < letterChance * boost) return Kind.Letter;
            if (_pack.Numbers.Count > 0 && Random.value < numberChance * boost) return Kind.Number;
            if (_pack.HasReading && Random.value < readingChance * boost) return Kind.Reading;
            return Kind.Word;
        }

        /// <summary>The finale mixes every kind it can offer evenly, so it revisits all of them.</summary>
        private Kind PickReviewKind()
        {
            var kinds = new List<Kind> { Kind.Word, Kind.Reading };
            if (_pack.Counts.Count > 0) kinds.Add(Kind.Counting);
            if (_pack.Commands.Count > 0) kinds.Add(Kind.Command);
            if (_pack.Sizes.Count > 0) kinds.Add(Kind.Size);
            if (_pack.Sentences.Count > 0) kinds.Add(Kind.Sentence);
            if (_pack.Letters.Count > 0) kinds.Add(Kind.Letter);
            if (_pack.Numbers.Count > 0) kinds.Add(Kind.Number);
            return kinds[Random.Range(0, kinds.Count)];
        }

        private bool TryStartChallenge(PlayerController player, Kind? forced)
        {
            if (float.IsNaN(_standingTop))
            {
                return false;
            }

            bool guided = !SaveManager.Data.wordTutorialDone;
            Kind kind = PickKind(guided, forced);
            _spokenWord = null;

            switch (kind)
            {
                case Kind.Counting:
                    ChooseCounting();
                    break;
                case Kind.Command:
                    ChooseCommand();
                    break;
                case Kind.Size:
                    ChooseSize();
                    break;
                case Kind.Sentence:
                    ChooseSentence();
                    break;
                case Kind.Letter:
                    if (!ChooseLetter())
                    {
                        kind = Kind.Word;
                        ChooseWords();
                    }

                    break;
                case Kind.Number:
                    ChooseFrom(_pack.Numbers);
                    break;
                default:
                    ChooseWords();
                    break;
            }

            CacheObstacles();

            AudioClip intro = guided ? _pack.Voice(LearningProgress.HomeLanguage, "intro") : null;
            // The guided intro is spoken first, so the bubbles start further ahead.
            float lead = leadSeconds + (intro != null ? intro.length + 0.4f : 0f);

            float speed = Mathf.Max(1f, player.CurrentSpeed);
            float spacing = Mathf.Max(bubbleRadius * 2f + 1.6f, speed * spacingSeconds);
            float searchFrom = player.transform.position.x + speed * lead;
            var positions = new List<Vector3>(_options.Count);

            if (kind == Kind.Command)
            {
                // Commands stack their targets in one spot: the child must act, not pick.
                if (!FindSlot(searchFrom, out float x))
                {
                    return false;
                }

                for (int i = 0; i < _options.Count; i++)
                {
                    positions.Add(new Vector3(x, CommandHeight(_options[i]), 0f));
                }
            }
            else
            {
                float y = _standingTop + heightAboveHead;

                for (int i = 0; i < _options.Count; i++)
                {
                    if (!FindSlot(searchFrom, out float x))
                    {
                        return false;
                    }

                    positions.Add(new Vector3(x, y, 0f));
                    searchFrom = x + spacing;
                }
            }

            _active = true;
            _answered = false;
            _missRecorded = false;
            _guided = guided;
            _jumpCuePlayed = false;

            for (int i = 0; i < _options.Count; i++)
            {
                WordEntry word = _options[i];
                WordBubble bubble = WordBubble.Create(transform, positions[i], word,
                    _pack.Picture(word.PictureId), bubbleRadius, _spriteMaterial, this);
                _bubbles.Add(bubble);

                if (_guided && word == _target)
                {
                    bubble.Highlight();
                }
            }

            _previousTarget = _target;
            CurrentKind = kind;

            if (_guided)
            {
                PlayInSequence(intro, _pack.Voice(LearningProgress.Language, _target.id));
            }
            else if (kind != Kind.Reading)
            {
                // Reading challenges stay silent: the child reads the card, the voice comes after.
                ReplayWord();
            }

            ChallengeStarted?.Invoke(_target);
            return true;
        }

        // Stand-ins for the wrong heights of a command challenge. They show the same
        // star as the right one, so only following the command finds the answer.
        private static readonly WordEntry LowSpot = new WordEntry { id = "spot_low", picture = "star", action = "stay" };
        private static readonly WordEntry HighSpot = new WordEntry { id = "spot_high", picture = "star", action = "jump" };

        /// <summary>
        /// "Slovo O": a letter on the card and pictures whose words start with other
        /// letters; the right one starts with it. False if the world has no letters
        /// for the language being learned.
        /// </summary>
        private bool ChooseLetter()
        {
            string language = LearningProgress.Language;
            IReadOnlyList<WordEntry> words = _pack.ChallengeWords;
            var candidates = new List<(WordEntry word, WordEntry letter)>(words.Count);

            for (int i = 0; i < words.Count; i++)
            {
                string first = WordEntry.Initial(words[i].Text(language));

                foreach (WordEntry letter in _pack.Letters)
                {
                    if (letter.lang == language && letter.letter == first)
                    {
                        candidates.Add((words[i], letter));
                        break;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            (WordEntry word, WordEntry letterEntry) = candidates[Random.Range(0, candidates.Count)];

            // The right bubble shows the word's picture but answers the letter.
            _target = new WordEntry
            {
                id = letterEntry.id,
                bs = letterEntry.bs,
                en = letterEntry.en,
                de = letterEntry.de,
                picture = word.PictureId,
                letter = letterEntry.letter,
                lang = letterEntry.lang
            };
            _spokenWord = word;
            _options.Clear();
            _options.Add(_target);

            var others = new List<WordEntry>(words.Count);
            for (int i = 0; i < words.Count; i++)
            {
                if (WordEntry.Initial(words[i].Text(language)) != letterEntry.letter)
                {
                    others.Add(words[i]);
                }
            }

            while (_options.Count < optionCount && others.Count > 0)
            {
                int pick = Random.Range(0, others.Count);
                _options.Add(others[pick]);
                others.RemoveAt(pick);
            }

            Shuffle();
            return true;
        }

        /// <summary>A target from a list (numbers) and random distinct others from the same list.</summary>
        private void ChooseFrom(IReadOnlyList<WordEntry> list)
        {
            int index = Random.Range(0, list.Count);

            if (list[index] == _previousTarget && list.Count > 1)
            {
                index = (index + 1) % list.Count;
            }

            _target = list[index];
            _options.Clear();
            _options.Add(_target);
            int guard = 0;

            while (_options.Count < optionCount && guard++ < 100)
            {
                WordEntry candidate = list[Random.Range(0, list.Count)];

                if (!_options.Contains(candidate))
                {
                    _options.Add(candidate);
                }
            }

            Shuffle();
        }

        private void Shuffle()
        {
            for (int i = _options.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_options[i], _options[j]) = (_options[j], _options[i]);
            }
        }

        /// <summary>
        /// "Pada snijeg.": a whole sentence and two pictures from the same group,
        /// so the child has to understand the sentence, not just one word in it.
        /// </summary>
        private void ChooseSentence()
        {
            IReadOnlyList<WordEntry> sentences = _pack.Sentences;
            int index = Random.Range(0, sentences.Count);

            if (sentences[index] == _previousTarget && sentences.Count > 1)
            {
                index = (index + 1) % sentences.Count;
            }

            _target = sentences[index];
            _options.Clear();
            _options.Add(_target);

            var others = new List<WordEntry>(4);

            for (int i = 0; i < sentences.Count; i++)
            {
                if (sentences[i] != _target && sentences[i].group == _target.group)
                {
                    others.Add(sentences[i]);
                }
            }

            if (others.Count > 0)
            {
                _options.Add(others[Random.Range(0, others.Count)]);
            }

            if (Random.value < 0.5f)
            {
                _options.Reverse();
            }
        }

        /// <summary>"Velika riba": the same picture big and small; only the adjective tells them apart.</summary>
        private void ChooseSize()
        {
            IReadOnlyList<WordEntry> sizes = _pack.Sizes;
            int index = Random.Range(0, sizes.Count);

            if (sizes[index] == _previousTarget && sizes.Count > 1)
            {
                index = (index + 1) % sizes.Count;
            }

            _target = sizes[index];
            _options.Clear();
            _options.Add(_target);

            for (int i = 0; i < sizes.Count; i++)
            {
                if (sizes[i].PictureId == _target.PictureId && sizes[i].size != _target.size)
                {
                    _options.Add(sizes[i]);
                    break;
                }
            }

            if (Random.value < 0.5f)
            {
                _options.Reverse();
            }
        }

        /// <summary>The spoken command plus one wrong height in the same spot.</summary>
        private void ChooseCommand()
        {
            IReadOnlyList<WordEntry> commands = _pack.Commands;
            int index = Random.Range(0, commands.Count);

            if (commands[index] == _previousTarget && commands.Count > 1)
            {
                index = (index + 1) % commands.Count;
            }

            _target = commands[index];
            _options.Clear();
            _options.Add(_target);
            // "Jump" pairs with a star at body height that a runner who does not jump
            // collects; "stay down" and "jump twice" pair with the single-jump star.
            _options.Add(_target.action == "jump" ? LowSpot : HighSpot);
        }

        /// <summary>
        /// Height of a command target: body height for "stay", jump height for
        /// "jump", out of reach of one jump for "double".
        /// </summary>
        private float CommandHeight(WordEntry entry)
        {
            switch (entry.action)
            {
                case "stay":
                    return (_groundY + _standingTop) * 0.5f;
                case "double":
                    return _standingTop + doubleJumpHeight;
                default:
                    return _standingTop + heightAboveHead;
            }
        }

        /// <summary>Weighted pick of the target, then random distinct distractors in random order.</summary>
        private void ChooseWords()
        {
            IReadOnlyList<WordEntry> words = _pack.ChallengeWords;
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

        /// <summary>
        /// "Three hens": one animal, three bubbles with different numbers of it.
        /// The child has to count, not just recognise the picture.
        /// </summary>
        private void ChooseCounting()
        {
            IReadOnlyList<WordEntry> counts = _pack.Counts;
            WordEntry target = counts[Random.Range(0, counts.Count)];
            int guard = 0;

            while (target == _previousTarget && guard++ < 10)
            {
                target = counts[Random.Range(0, counts.Count)];
            }

            _target = target;
            _options.Clear();
            _options.Add(_target);
            guard = 0;

            while (_options.Count < optionCount && guard++ < 100)
            {
                WordEntry candidate = counts[Random.Range(0, counts.Count)];

                if (candidate.PictureId == _target.PictureId && !_options.Contains(candidate))
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
                goalReached ? _pack.Voice(language, _pack.IsReview ? "finale" : "goal") : null);
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
