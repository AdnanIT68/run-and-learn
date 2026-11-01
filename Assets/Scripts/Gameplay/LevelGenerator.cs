using System.Collections.Generic;
using FootballRunner.Collectibles;
using FootballRunner.Obstacles;
using FootballRunner.Utilities;
using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// Streams the endless track. Chunks are laid end to end ahead of the player
    /// and recycled once they fall behind, so memory use stays flat no matter how
    /// long the run lasts.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        private enum HazardPattern
        {
            None,
            Single,
            DoubleSpaced,
            SpikeThenBarrier,
            GuideThenHazard
        }

        private enum CollectiblePattern
        {
            Scattered,
            Line,
            Arc,
            RiseFall,
            Stair,
            PitGuide,
            RiskReward
        }

        private struct ActiveChunk
        {
            public GameObject Instance;
            public Chunk Chunk;
            public PooledObject Pooled;
            public float StartX;
        }

        [Header("Chunk Prefabs")]
        [Tooltip("Flat, empty chunk used at the very start so the run never opens with a hazard.")]
        [SerializeField] private GameObject startChunkPrefab;
        [SerializeField] private GameObject[] chunkPrefabs;

        [Header("Content Prefabs")]
        [SerializeField] private GameObject[] obstaclePrefabs;
        [SerializeField] private GameObject collectiblePrefab;
        [SerializeField] private CollectibleData[] collectibleTypes;

        [Header("Placement Rules")]
        [Range(0f, 1f)]
        [SerializeField] private float collectibleChance = 0.82f;
        [Tooltip("Minimum reaction time between consecutive obstacles. Converted to " +
                 "distance using the current run speed, so the gap grows as the game speeds up.")]
        [SerializeField] private float minSecondsBetweenObstacles = 1.1f;
        [Tooltip("Chunks at the start of the run that are guaranteed to be hazard free.")]
        [SerializeField] private int safeStartChunks = 3;
        [Tooltip("World units after a pit before the next hazard may appear.")]
        [SerializeField] private float landingClearance = 6f;
        [Tooltip("Flat, hazard-free chunks placed after a pit or a two-hazard chunk.")]
        [SerializeField] private int restChunksAfterHard = 1;

        [Header("Streaming")]
        [SerializeField] private Transform player;
        [SerializeField] private float spawnAheadDistance = 80f;
        [SerializeField] private float despawnBehindDistance = 30f;
        [SerializeField] private int prewarmPerPrefab = 3;

        private readonly Queue<ActiveChunk> _activeChunks = new Queue<ActiveChunk>();
        private float _nextChunkX;
        private int _chunksSpawned;
        private float _lastObstacleX = float.NegativeInfinity;
        private float _totalCollectibleWeight;
        private bool _lastChunkWasGap;
        private bool _priorChunkWasGap;
        private int _restChunksDue;
        private readonly List<float> _obstaclesThisChunk = new List<float>(8);
        private readonly List<float> _collectiblesThisChunk = new List<float>(8);
        private readonly List<Transform> _usableSlots = new List<Transform>(8);
        private PoolManager _pool;
        private GameObject _spikePrefab;
        private GameObject _cratePrefab;
        private CollectibleData _coinType;
        private bool _starThisChunk;

        private void OnEnable()
        {
            EnvironmentDirector.AreaChanged += OnAreaChanged;
        }

        private void OnDisable()
        {
            EnvironmentDirector.AreaChanged -= OnAreaChanged;
        }

        public GameObject StartChunkPrefab => startChunkPrefab;

        public GameObject[] ChunkPrefabs => chunkPrefabs;

        public GameObject[] ObstaclePrefabs => obstaclePrefabs;

        public GameObject CollectiblePrefab => collectiblePrefab;

        public CollectibleData[] CollectibleTypes => collectibleTypes;

        private void Start()
        {
            if (Adventure.AdventureSession.IsActive)
            {
                enabled = false;
                return;
            }
            if (player == null && Player.PlayerController.Instance != null)
            {
                player = Player.PlayerController.Instance.transform;
            }

            if (chunkPrefabs == null || chunkPrefabs.Length == 0)
            {
                Debug.LogError("[LevelGenerator] No chunk prefabs assigned. Disabling generator.");
                enabled = false;
                return;
            }

            _pool = PoolManager.Instance;
            CacheCollectibleWeights();
            CacheNamedPrefabs();
            Prewarm();

            _nextChunkX = 0f;

            float target = (player != null ? player.position.x : 0f) + spawnAheadDistance;

            while (_nextChunkX < target)
            {
                SpawnNextChunk();
            }
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            float playerX = player.position.x;

            while (_nextChunkX < playerX + spawnAheadDistance)
            {
                SpawnNextChunk();
            }

            RecycleChunksBehind(playerX - despawnBehindDistance);
        }

        private void CacheCollectibleWeights()
        {
            _totalCollectibleWeight = 0f;
            _coinType = null;

            if (collectibleTypes == null)
            {
                return;
            }

            for (int i = 0; i < collectibleTypes.Length; i++)
            {
                CollectibleData data = collectibleTypes[i];

                if (data == null)
                {
                    continue;
                }

                _totalCollectibleWeight += Mathf.Max(0f, data.spawnWeight);

                if (_coinType == null && data.IsCoin)
                {
                    _coinType = data;
                }
            }
        }

        private void CacheNamedPrefabs()
        {
            _spikePrefab = FindObstaclePrefab("Spike");
            _cratePrefab = FindObstaclePrefab("Crate");

            if (_spikePrefab == null)
            {
                _spikePrefab = obstaclePrefabs != null && obstaclePrefabs.Length > 0
                    ? obstaclePrefabs[0]
                    : null;
            }
        }

        private GameObject FindObstaclePrefab(string key)
        {
            if (obstaclePrefabs == null)
            {
                return null;
            }

            for (int i = 0; i < obstaclePrefabs.Length; i++)
            {
                GameObject prefab = obstaclePrefabs[i];

                if (prefab != null && prefab.name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return prefab;
                }
            }

            return null;
        }

        private void Prewarm()
        {
            PoolManager pool = PoolManager.Instance;

            if (pool == null)
            {
                return;
            }

            if (startChunkPrefab != null)
            {
                pool.Prewarm(startChunkPrefab, 1);
            }

            for (int i = 0; i < chunkPrefabs.Length; i++)
            {
                pool.Prewarm(chunkPrefabs[i], prewarmPerPrefab);
            }

            if (obstaclePrefabs != null)
            {
                for (int i = 0; i < obstaclePrefabs.Length; i++)
                {
                    pool.Prewarm(obstaclePrefabs[i], prewarmPerPrefab);
                }
            }

            if (collectiblePrefab != null)
            {
                pool.Prewarm(collectiblePrefab, 40);
            }
        }

        private void SpawnNextChunk()
        {
            GameObject prefab = ChooseChunkPrefab();

            if (prefab == null)
            {
                enabled = false;
                return;
            }

            GameObject instance = _pool != null
                ? _pool.Get(prefab, new Vector3(_nextChunkX, 0f, 0f), Quaternion.identity, transform)
                : Instantiate(prefab, new Vector3(_nextChunkX, 0f, 0f), Quaternion.identity, transform);

            var chunk = instance.GetComponent<Chunk>();

            if (chunk == null)
            {
                Debug.LogError($"[LevelGenerator] Prefab {prefab.name} has no Chunk component.");
                enabled = false;
                return;
            }

            chunk.ReleaseContent();
            _obstaclesThisChunk.Clear();
            _collectiblesThisChunk.Clear();
            _starThisChunk = false;
            chunk.ApplyTint(EnvironmentDirector.Instance != null
                ? EnvironmentDirector.Instance.CurrentGroundTint
                : Color.white);
            WorldScenery.Instance?.SkinChunk(chunk);

            bool isGap = IsGapChunk(prefab);
            bool themeSafe = EnvironmentDirector.Instance != null &&
                             EnvironmentDirector.Instance.ConsumeSafeChunk();
            bool restChunk = _restChunksDue > 0;
            bool arrivesDuringWarmUp = ChunkWouldArriveDuringWarmUp(_nextChunkX);
            bool allowHazards = _chunksSpawned >= safeStartChunks &&
                                !isGap &&
                                !themeSafe &&
                                !restChunk &&
                                !arrivesDuringWarmUp;

            if (allowHazards)
            {
                PopulateHazards(chunk, _priorChunkWasGap);
            }

            PopulateCollectibles(chunk, isGap);

            _activeChunks.Enqueue(new ActiveChunk
            {
                Instance = instance,
                Chunk = chunk,
                Pooled = instance.GetComponent<PooledObject>(),
                StartX = _nextChunkX
            });

            _nextChunkX += chunk.Length;
            _chunksSpawned++;
            _priorChunkWasGap = isGap;
            UpdateRestBudget(isGap);
        }

        private GameObject ChooseChunkPrefab()
        {
            if (_chunksSpawned < safeStartChunks && startChunkPrefab != null)
            {
                _lastChunkWasGap = false;
                return startChunkPrefab;
            }

            bool forceRest = _restChunksDue > 0;
            bool blockUnfairGap = ChunkWouldArriveDuringWarmUp(_nextChunkX);
            GameObject prefab = forceRest
                ? PreferRestPrefab()
                : chunkPrefabs[Random.Range(0, chunkPrefabs.Length)];

            if (IsGapChunk(prefab) && (forceRest || blockUnfairGap || _lastChunkWasGap))
            {
                prefab = PreferRestPrefab();
            }

            _lastChunkWasGap = IsGapChunk(prefab);
            return prefab;
        }

        private GameObject PreferRestPrefab()
        {
            if (startChunkPrefab != null)
            {
                return startChunkPrefab;
            }

            for (int i = 0; i < chunkPrefabs.Length; i++)
            {
                if (!IsGapChunk(chunkPrefabs[i]))
                {
                    return chunkPrefabs[i];
                }
            }

            return chunkPrefabs[0];
        }

        private void UpdateRestBudget(bool isGap)
        {
            if (_restChunksDue > 0)
            {
                _restChunksDue--;
                return;
            }

            if (isGap || _obstaclesThisChunk.Count >= 2)
            {
                _restChunksDue = Mathf.Max(0, restChunksAfterHard);
            }
        }

        private bool ChunkWouldArriveDuringWarmUp(float chunkStartX)
        {
            DifficultyManager difficulty = DifficultyManager.Instance;

            if (difficulty == null)
            {
                return false;
            }

            float playerX = player != null ? player.position.x : 0f;
            return difficulty.WouldArriveDuringWarmUp(chunkStartX, playerX);
        }

        private float EstimateArrivalSeconds(float worldX)
        {
            DifficultyManager difficulty = DifficultyManager.Instance;

            if (difficulty == null)
            {
                return 0f;
            }

            float playerX = player != null ? player.position.x : 0f;
            return difficulty.EstimateArrivalSeconds(worldX, playerX);
        }

        private static bool IsGapChunk(GameObject prefab)
        {
            return prefab != null && prefab.name.IndexOf("Gap", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void PopulateHazards(Chunk chunk, bool afterGap)
        {
            if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
            {
                return;
            }

            DifficultyManager difficulty = DifficultyManager.Instance;
            float arrival = EstimateArrivalSeconds(_nextChunkX);
            float chance = difficulty != null ? difficulty.ObstacleChanceAt(arrival) : 0.18f;

            if (Random.value > chance)
            {
                return;
            }

            CollectUsableSlots(chunk, afterGap);

            if (_usableSlots.Count == 0)
            {
                return;
            }

            HazardPattern pattern = ChooseHazardPattern(difficulty != null ? difficulty.PatternTierAt(arrival) : 1);
            PlaceHazardPattern(chunk, pattern, arrival);
        }

        private void CollectUsableSlots(Chunk chunk, bool afterGap)
        {
            _usableSlots.Clear();
            IReadOnlyList<Transform> slots = chunk.ObstacleSlots;

            if (slots == null)
            {
                return;
            }

            DifficultyManager difficulty = DifficultyManager.Instance;
            float speed = difficulty != null ? difficulty.CurrentSpeed : 6.5f;
            float spacingSeconds = difficulty != null
                ? difficulty.MinSecondsBetweenHazards
                : minSecondsBetweenObstacles;
            float minGap = speed * Mathf.Max(0.25f, spacingSeconds);

            for (int i = 0; i < slots.Count; i++)
            {
                Transform slot = slots[i];

                if (slot == null)
                {
                    continue;
                }

                if (afterGap && slot.localPosition.x < landingClearance)
                {
                    continue;
                }

                if (slot.position.x - _lastObstacleX < minGap)
                {
                    continue;
                }

                _usableSlots.Add(slot);
            }
        }

        private HazardPattern ChooseHazardPattern(int tier)
        {
            if (tier <= 1)
            {
                return HazardPattern.Single;
            }

            if (tier == 2)
            {
                float roll = Random.value;
                if (roll < 0.55f) return HazardPattern.Single;
                if (roll < 0.85f) return HazardPattern.DoubleSpaced;
                return HazardPattern.SpikeThenBarrier;
            }

            float late = Random.value;
            if (late < 0.35f) return HazardPattern.Single;
            if (late < 0.60f) return HazardPattern.DoubleSpaced;
            if (late < 0.80f) return HazardPattern.SpikeThenBarrier;
            return HazardPattern.GuideThenHazard;
        }

        private void PlaceHazardPattern(Chunk chunk, HazardPattern pattern, float arrivalSeconds)
        {
            DifficultyManager difficulty = DifficultyManager.Instance;
            int cap = difficulty != null ? difficulty.MaxObstaclesAt(arrivalSeconds) : 1;
            float speed = difficulty != null ? difficulty.CurrentSpeed : 6.5f;
            float minGap = speed * Mathf.Max(0.25f, difficulty != null
                ? difficulty.MinSecondsBetweenHazards
                : minSecondsBetweenObstacles);

            switch (pattern)
            {
                case HazardPattern.DoubleSpaced:
                    PlaceSpacedPair(chunk, _spikePrefab != null ? _spikePrefab : RandomObstacle(), minGap, cap);
                    break;
                case HazardPattern.SpikeThenBarrier:
                    PlaceAlternating(chunk, minGap, cap);
                    break;
                case HazardPattern.GuideThenHazard:
                    PlaceSingle(chunk, _spikePrefab != null ? _spikePrefab : RandomObstacle());
                    if (_obstaclesThisChunk.Count > 0)
                    {
                        float guideX = _obstaclesThisChunk[0] - 3.2f;
                        SpawnCollectible(chunk, new Vector3(guideX, 1.45f, 0f), PreferCoin());
                        SpawnCollectible(chunk, new Vector3(guideX + 0.85f, 1.95f, 0f), PreferCoin());
                        SpawnCollectible(chunk, new Vector3(guideX + 1.7f, 2.35f, 0f), PreferCoin());
                    }
                    break;
                default:
                    if (!TryPlaceSpecial(chunk, difficulty != null ? difficulty.PatternTierAt(arrivalSeconds) : 1, minGap))
                    {
                        PlaceSingle(chunk, RandomObstacle());
                    }
                    break;
            }
        }

        /// <summary>Chance, from tier 2, that a single hazard is a rolling football.</summary>
        public const float BallChance = 0.15f;

        /// <summary>Chance that a single hazard is a flock to pass under instead of a block.</summary>
        public const float FlyerChance = 0.2f;

        /// <summary>Chance that a single hazard is this world's own thing (a sheep, a snowman...).</summary>
        public const float PropChance = 0.2f;

        /// <summary>Chance of a trampoline up to a row of coins instead of a hazard.</summary>
        public const float TrampolineChance = 0.1f;

        /// <summary>Chance of a mud puddle that only slows the runner down.</summary>
        public const float MudChance = 0.1f;

        /// <summary>Airtime of a trampoline bounce, a little over the real arc (rise 0.43 s, fall 0.31 s).</summary>
        private const float BounceSeconds = 0.8f;

        /// <summary>
        /// Sometimes swaps a single ground hazard for something else: a flock at head
        /// height (stay down to pass), a football rolling towards the runner, the
        /// world's own hazard, a trampoline or a mud puddle. The last two never hurt.
        /// </summary>
        private bool TryPlaceSpecial(Chunk chunk, int tier, float minGap)
        {
            if (_usableSlots.Count == 0)
            {
                return false;
            }

            float roll = Random.value;
            Transform slot = _usableSlots[Random.Range(0, _usableSlots.Count)];
            string world = WorldScenery.Instance != null ? WorldScenery.Instance.WorldName : null;
            float runnerSpeed = DifficultyManager.Instance != null ? DifficultyManager.Instance.CurrentSpeed : 6.5f;
            float clearUntil = slot.position.x;
            GameObject instance;

            if (roll < BallChance)
            {
                // The ball meets the runner a little before its slot; keep that stretch clear too.
                float drift = RollingBall.StartDistance * RollingBall.Speed / (RollingBall.Speed + runnerSpeed);

                if (tier < 2 || slot.position.x - _lastObstacleX < minGap + drift)
                {
                    return false;
                }

                instance = SpecialHazards.CreateBall(slot.position);
            }
            else if ((roll -= BallChance) < FlyerChance)
            {
                instance = SpecialHazards.CreateFlyers(slot.position, world);
            }
            else if ((roll -= FlyerChance) < PropChance)
            {
                if (world == null)
                {
                    return false;
                }

                instance = SpecialHazards.CreateProp(slot.position, world);
            }
            else if ((roll -= PropChance) < TrampolineChance)
            {
                // Only on flat ground, landing well before the chunk ends, so the bounce
                // never drops the runner into a pit or under a platform.
                float landing = runnerSpeed * BounceSeconds;

                if (!IsFlatChunk(chunk) || slot.localPosition.x + landing + 2f > chunk.Length)
                {
                    return false;
                }

                instance = SpecialHazards.CreateTrampoline(slot.position);
                clearUntil = slot.position.x + landing;
                PlaceBounceCoins(chunk, slot.position, runnerSpeed);
            }
            else if ((roll -= TrampolineChance) < MudChance)
            {
                // Full speed is back well before the next chunk, which may start with a pit.
                if (!IsFlatChunk(chunk) || slot.localPosition.x + SpecialHazards.MudWidth * 0.5f + 4f > chunk.Length)
                {
                    return false;
                }

                instance = SpecialHazards.CreateMud(slot.position, world);
                clearUntil = slot.position.x + SpecialHazards.MudWidth * 0.5f;
            }
            else
            {
                return false;
            }

            chunk.RegisterContent(instance);
            _lastObstacleX = clearUntil;
            _obstaclesThisChunk.Add(slot.position.x);
            return true;
        }

        private static bool IsFlatChunk(Chunk chunk)
        {
            return chunk != null && chunk.name.IndexOf("Flat", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Coins along the trampoline arc, so the bounce pays off.</summary>
        private void PlaceBounceCoins(Chunk chunk, Vector3 groundPosition, float runnerSpeed)
        {
            // The runner's gravity scale is 4 and falling uses 1.9x gravity (PlayerController).
            float gravity = Mathf.Abs(Physics2D.gravity.y) * 4f;
            float rise = Trampoline.BounceVelocity / gravity;
            float apex = Trampoline.BounceVelocity * rise * 0.5f;
            // Runner's centre rides 0.75 above the feet.
            float[] times = { 0.18f, 0.3f, rise, 0.53f, 0.62f };

            foreach (float t in times)
            {
                float height = t <= rise
                    ? Trampoline.BounceVelocity * t - 0.5f * gravity * t * t
                    : apex - 0.5f * gravity * 1.9f * (t - rise) * (t - rise);
                SpawnCollectible(chunk, new Vector3(groundPosition.x + runnerSpeed * t, groundPosition.y + 0.75f + height, 0f),
                    PreferCoin());
            }
        }

        private void PlaceSingle(Chunk chunk, GameObject prefab)
        {
            if (_usableSlots.Count == 0)
            {
                return;
            }

            Transform slot = _usableSlots[Random.Range(0, _usableSlots.Count)];
            TryPlaceObstacle(chunk, prefab, slot);
        }

        private void PlaceSpacedPair(Chunk chunk, GameObject prefab, float minGap, int cap)
        {
            if (cap < 2 || _usableSlots.Count < 2)
            {
                PlaceSingle(chunk, prefab);
                return;
            }

            Transform first = _usableSlots[0];
            Transform second = null;

            for (int i = 1; i < _usableSlots.Count; i++)
            {
                if (_usableSlots[i].position.x - first.position.x >= minGap)
                {
                    second = _usableSlots[i];
                    break;
                }
            }

            TryPlaceObstacle(chunk, prefab, first);

            if (second != null)
            {
                TryPlaceObstacle(chunk, prefab, second);
            }
        }

        private void PlaceAlternating(Chunk chunk, float minGap, int cap)
        {
            GameObject firstPrefab = _spikePrefab != null ? _spikePrefab : RandomObstacle();
            GameObject secondPrefab = _cratePrefab != null ? _cratePrefab : RandomObstacle();

            if (cap < 2 || _usableSlots.Count < 2)
            {
                PlaceSingle(chunk, firstPrefab);
                return;
            }

            Transform first = _usableSlots[0];
            Transform second = null;

            for (int i = 1; i < _usableSlots.Count; i++)
            {
                if (_usableSlots[i].position.x - first.position.x >= minGap)
                {
                    second = _usableSlots[i];
                    break;
                }
            }

            TryPlaceObstacle(chunk, firstPrefab, first);

            if (second != null)
            {
                TryPlaceObstacle(chunk, secondPrefab, second);
            }
        }

        private bool TryPlaceObstacle(Chunk chunk, GameObject prefab, Transform slot)
        {
            if (prefab == null || slot == null)
            {
                return false;
            }

            GameObject instance = Spawn(prefab, slot.position);

            if (instance == null)
            {
                return false;
            }

            WorldScenery.Instance?.SkinObstacle(instance, prefab.name);

            chunk.RegisterContent(instance);
            _lastObstacleX = slot.position.x;
            _obstaclesThisChunk.Add(slot.position.x);
            return true;
        }

        private GameObject RandomObstacle()
        {
            return obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        }

        private void PopulateCollectibles(Chunk chunk, bool isGap)
        {
            if (collectiblePrefab == null || _totalCollectibleWeight <= 0f)
            {
                return;
            }

            IReadOnlyList<Transform> slots = chunk.CollectibleSlots;

            if (slots == null || slots.Count == 0)
            {
                return;
            }

            if (isGap)
            {
                PlaceCollectiblePattern(chunk, CollectiblePattern.PitGuide, slots);
                return;
            }

            if (Random.value > collectibleChance)
            {
                return;
            }

            DifficultyManager difficulty = DifficultyManager.Instance;
            int tier = difficulty != null
                ? difficulty.PatternTierAt(EstimateArrivalSeconds(_nextChunkX))
                : 1;
            CollectiblePattern pattern = ChooseCollectiblePattern(tier, _obstaclesThisChunk.Count > 0);
            PlaceCollectiblePattern(chunk, pattern, slots);
        }

        private CollectiblePattern ChooseCollectiblePattern(int tier, bool hasHazard)
        {
            if (hasHazard && tier >= 2 && Random.value < 0.45f)
            {
                return CollectiblePattern.RiskReward;
            }

            if (tier <= 1)
            {
                return Random.value < 0.55f ? CollectiblePattern.Line : CollectiblePattern.Scattered;
            }

            float roll = Random.value;

            if (roll < 0.22f) return CollectiblePattern.Line;
            if (roll < 0.42f) return CollectiblePattern.Arc;
            if (roll < 0.60f) return CollectiblePattern.RiseFall;
            if (roll < 0.78f) return CollectiblePattern.Stair;
            return CollectiblePattern.Scattered;
        }

        private void PlaceCollectiblePattern(Chunk chunk, CollectiblePattern pattern, IReadOnlyList<Transform> slots)
        {
            switch (pattern)
            {
                case CollectiblePattern.Line:
                    PlaceOffsetRow(chunk, FirstSafeSlot(slots), 4, 0.85f, 0f, 0f, true, 3);
                    break;
                case CollectiblePattern.Arc:
                    PlaceArc(chunk, FirstSafeSlot(slots), 4, 0.85f, 0.95f);
                    break;
                case CollectiblePattern.RiseFall:
                    PlaceRiseFall(chunk, FirstSafeSlot(slots), 5, 0.75f, 0.85f);
                    break;
                case CollectiblePattern.Stair:
                    PlaceOffsetRow(chunk, FirstSafeSlot(slots), 4, 0.75f, 0.42f, 0f);
                    break;
                case CollectiblePattern.PitGuide:
                    PlaceExistingSafeSlots(chunk, slots);
                    break;
                case CollectiblePattern.RiskReward:
                    PlaceRiskReward(chunk);
                    break;
                default:
                    PlaceScattered(chunk, slots);
                    break;
            }
        }

        private Transform FirstSafeSlot(IReadOnlyList<Transform> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && !IsTooCloseToObstacle(slots[i].position.x))
                {
                    return slots[i];
                }
            }

            return slots[0];
        }

        private void PlaceScattered(Chunk chunk, IReadOnlyList<Transform> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                Transform slot = slots[i];

                if (slot == null || Random.value > 0.45f || IsTooCloseToObstacle(slot.position.x))
                {
                    continue;
                }

                SpawnCollectible(chunk, slot.position, PickCollectibleType(false));
            }
        }

        private void PlaceExistingSafeSlots(Chunk chunk, IReadOnlyList<Transform> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                Transform slot = slots[i];

                if (slot == null || IsTooCloseToObstacle(slot.position.x))
                {
                    continue;
                }

                SpawnCollectible(chunk, slot.position, PreferCoin());
            }
        }

        private void PlaceOffsetRow(
            Chunk chunk,
            Transform start,
            int count,
            float stepX,
            float stepY,
            float yJitter,
            bool weighted = false,
            int starIndex = -1)
        {
            if (start == null)
            {
                return;
            }

            Vector3 origin = start.position;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = origin + new Vector3(stepX * i, stepY * i + yJitter, 0f);

                if (position.y < 1.15f || position.y > 3.4f || IsTooCloseToObstacle(position.x))
                {
                    continue;
                }

                CollectibleData data = weighted
                    ? PickCollectibleType(i == starIndex)
                    : PreferCoin();
                SpawnCollectible(chunk, position, data);
            }
        }

        private void PlaceArc(Chunk chunk, Transform start, int count, float stepX, float height)
        {
            if (start == null)
            {
                return;
            }

            Vector3 origin = start.position;
            float baseY = Mathf.Max(1.35f, origin.y);
            int peak = count > 1 ? (count - 1) / 2 : 0;

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (float)(count - 1);
                float y = baseY + Mathf.Sin(t * Mathf.PI) * height;
                Vector3 position = new Vector3(origin.x + stepX * i, Mathf.Min(3.3f, y), origin.z);

                if (IsTooCloseToObstacle(position.x))
                {
                    continue;
                }

                SpawnCollectible(chunk, position, PickCollectibleType(i == peak));
            }
        }

        private void PlaceRiseFall(Chunk chunk, Transform start, int count, float stepX, float height)
        {
            if (start == null)
            {
                return;
            }

            Vector3 origin = start.position;
            int mid = count / 2;

            for (int i = 0; i < count; i++)
            {
                int step = i <= mid ? i : count - 1 - i;
                float y = Mathf.Min(3.3f, Mathf.Max(1.35f, origin.y) + step * height * 0.55f);
                Vector3 position = new Vector3(origin.x + stepX * i, y, origin.z);

                if (IsTooCloseToObstacle(position.x))
                {
                    continue;
                }

                SpawnCollectible(chunk, position, i == mid ? PickCollectibleType(true) : PreferCoin());
            }
        }

        private void PlaceRiskReward(Chunk chunk)
        {
            if (_obstaclesThisChunk.Count == 0)
            {
                PlaceArc(chunk, FirstSafeSlot(chunk.CollectibleSlots), 3, 0.8f, 0.8f);
                return;
            }

            float hazardX = _obstaclesThisChunk[0];
            float[] offsetsX = { -1.15f, 0f, 1.15f };
            float[] offsetsY = { 2.15f, 2.75f, 2.15f };

            for (int i = 0; i < offsetsX.Length; i++)
            {
                Vector3 position = new Vector3(hazardX + offsetsX[i], offsetsY[i], 0f);
                SpawnCollectible(chunk, position, PreferCoin());
            }
        }

        private void SpawnCollectible(Chunk chunk, Vector3 position, CollectibleData data)
        {
            if (data == null)
            {
                return;
            }

            if (IsTooCloseToCollectible(position.x))
            {
                return;
            }

            if (IsStar(data))
            {
                if (_starThisChunk)
                {
                    data = PreferCoin();
                }
                else
                {
                    _starThisChunk = true;
                }
            }

            GameObject instance = Spawn(collectiblePrefab, position);

            if (instance == null)
            {
                if (IsStar(data))
                {
                    _starThisChunk = false;
                }

                return;
            }

            instance.GetComponent<Collectible>()?.Configure(data);
            chunk.RegisterContent(instance);
            _collectiblesThisChunk.Add(position.x);
        }

        private CollectibleData PreferCoin()
        {
            return _coinType != null ? _coinType : PickCollectibleType(false);
        }

        private CollectibleData PickCollectibleType(bool allowStar)
        {
            float roll = Random.value * _totalCollectibleWeight;

            for (int i = 0; i < collectibleTypes.Length; i++)
            {
                CollectibleData candidate = collectibleTypes[i];

                if (candidate == null)
                {
                    continue;
                }

                if (!allowStar && IsStar(candidate))
                {
                    continue;
                }

                if (_starThisChunk && IsStar(candidate))
                {
                    continue;
                }

                roll -= Mathf.Max(0f, candidate.spawnWeight);

                if (roll <= 0f)
                {
                    return candidate;
                }
            }

            return _coinType;
        }

        private static bool IsStar(CollectibleData data)
        {
            return data != null && data.displayName == "Star";
        }

        private bool IsTooCloseToCollectible(float x)
        {
            const float spacing = 0.7f;

            for (int i = 0; i < _collectiblesThisChunk.Count; i++)
            {
                if (Mathf.Abs(x - _collectiblesThisChunk[i]) < spacing)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsTooCloseToObstacle(float x)
        {
            const float clearance = 1.4f;

            if (Mathf.Abs(x - _lastObstacleX) < clearance)
            {
                return true;
            }

            for (int i = 0; i < _obstaclesThisChunk.Count; i++)
            {
                if (Mathf.Abs(x - _obstaclesThisChunk[i]) < clearance)
                {
                    return true;
                }
            }

            return false;
        }

        private GameObject Spawn(GameObject prefab, Vector3 position)
        {
            if (prefab == null)
            {
                return null;
            }

            return _pool != null
                ? _pool.Get(prefab, position, Quaternion.identity, transform)
                : Instantiate(prefab, position, Quaternion.identity, transform);
        }

        private void RecycleChunksBehind(float cutoffX)
        {
            while (_activeChunks.Count > 0)
            {
                ActiveChunk oldest = _activeChunks.Peek();

                if (oldest.StartX + oldest.Chunk.Length > cutoffX)
                {
                    return;
                }

                _activeChunks.Dequeue();
                oldest.Chunk.ReleaseContent();

                if (oldest.Pooled != null)
                {
                    oldest.Pooled.ReturnToPool();
                }
                else
                {
                    Destroy(oldest.Instance);
                }
            }
        }

        private void OnAreaChanged(AreaTheme theme, bool announce)
        {
            Color tint = EnvironmentDirector.Instance != null
                ? EnvironmentDirector.Instance.CurrentGroundTint
                : Color.white;

            foreach (ActiveChunk active in _activeChunks)
            {
                active.Chunk?.ApplyTint(tint);
            }
        }
    }
}
