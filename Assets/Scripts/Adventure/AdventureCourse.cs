using FootballRunner.Collectibles;
using FootballRunner.Gameplay;
using FootballRunner.Utilities;
using UnityEngine;

namespace FootballRunner.Adventure
{
    /// <summary>
    /// Builds the authored Green Hills Level 1 course once. This is not the
    /// endless generator: the track has a start, a middle, and a finish.
    /// </summary>
    public class AdventureCourse : MonoBehaviour
    {
        public const int SpecialStarCount = 3;

        public static AdventureCourse Instance { get; private set; }

        public float FinishX { get; private set; }

        public int StarsCollected { get; private set; }

        public static AdventureCourse EnsureOn(GameObject host)
        {
            if (Instance != null)
            {
                return Instance;
            }

            if (host == null)
            {
                return null;
            }

            var course = host.GetComponent<AdventureCourse>();
            return course != null ? course : host.AddComponent<AdventureCourse>();
        }

        private LevelGenerator _generator;
        private PoolManager _pool;
        private Transform _root;
        private AdventureVisuals _visuals;
        private CollectibleData _coin;
        private CollectibleData _gem;
        private CollectibleData _star;
        private GameObject _flat;
        private GameObject _gap;
        private GameObject _platform;
        private GameObject _spikes;
        private GameObject _crate;
        private GameObject _saw;
        private GameObject _pickup;
        private float _cursor;
        private int _starsPlaced;
        private Material _spriteMaterial;

        private void Awake()
        {
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
            if (!AdventureSession.IsActive)
            {
                enabled = false;
                return;
            }

            Bind();
            BuildLevelOne();
        }

        public void NotifyStarCollected()
        {
            StarsCollected = Mathf.Min(SpecialStarCount, StarsCollected + 1);
        }

        private void Bind()
        {
            _generator = FindFirstObjectByType<LevelGenerator>();
            _pool = PoolManager.Instance;
            _visuals = Resources.Load<AdventureVisuals>("Adventure/AdventureVisuals");
            _star = Resources.Load<CollectibleData>("Adventure/LostStar");

            var rootGo = new GameObject("GreenHills_Level1");
            _root = rootGo.transform;
            _root.SetParent(transform, false);

            if (_generator == null)
            {
                Debug.LogError("[AdventureCourse] LevelGenerator is missing; cannot reuse chunk prefabs.");
                enabled = false;
                return;
            }

            _flat = _generator.StartChunkPrefab != null ? _generator.StartChunkPrefab : Named(_generator.ChunkPrefabs, "Flat");
            _gap = Named(_generator.ChunkPrefabs, "Gap");
            _platform = Named(_generator.ChunkPrefabs, "Platform");
            _spikes = Named(_generator.ObstaclePrefabs, "Spike");
            _crate = Named(_generator.ObstaclePrefabs, "Crate");
            _saw = Named(_generator.ObstaclePrefabs, "Saw");
            _pickup = _generator.CollectiblePrefab;
            CacheCollectibleTypes();

            SpriteRenderer sample = _flat != null ? _flat.GetComponentInChildren<SpriteRenderer>() : null;
            if (sample != null)
            {
                _spriteMaterial = sample.sharedMaterial;
            }
        }

        private void CacheCollectibleTypes()
        {
            CollectibleData[] types = _generator.CollectibleTypes;
            if (types == null)
            {
                return;
            }

            for (int i = 0; i < types.Length; i++)
            {
                CollectibleData data = types[i];
                if (data == null)
                {
                    continue;
                }

                if (_coin == null && data.IsCoin)
                {
                    _coin = data;
                }

                if (_gem == null && data.displayName != null &&
                    data.displayName.IndexOf("Gem", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _gem = data;
                }

                if (_star == null && data.displayName == "Star")
                {
                    _star = data;
                }
            }

            if (_star == null)
            {
                _star = _gem != null ? _gem : _coin;
            }
        }

        private void BuildLevelOne()
        {
            _cursor = 0f;
            BuildMeadow();
            BuildHill();
            BuildBrokenPath();
            BuildForest();
            BuildFinale();
            PlaceFinish();
        }

        private void BuildMeadow()
        {
            // Safe, inviting opening. Coins teach the lane. Star 1 is on the path.
            for (int i = 0; i < 8; i++)
            {
                PlaceChunk(_flat);
            }

            CoinRow(18f, 5, 1.1f, 1.35f);
            CoinRow(48f, 4, 0.9f, 1.35f);
            PlaceStar(new Vector3(72f, 1.4f, 0f));
            CoinRow(88f, 3, 1f, 1.35f);

            Decor(_visuals != null ? _visuals.palm : null, 8f, 0f, 1.15f, 3);
            Decor(_visuals != null ? _visuals.grassTuft : null, 14f, 0f, 1f, 2);
            Decor(_visuals != null ? _visuals.bush : null, 22f, 0f, 1f, 3);
            Decor(_visuals != null ? _visuals.kenneyBush : null, 36f, 0f, 1.1f, 3);
            Decor(_visuals != null ? _visuals.palm : null, 52f, 0f, 1.05f, 3);
            Decor(_visuals != null ? _visuals.grassTuft : null, 64f, 0f, 1f, 2);
            Decor(_visuals != null ? _visuals.cloud : null, 40f, 4.8f, 1.2f, -6);
            Decor(_visuals != null ? _visuals.sign : null, 6f, 0f, 1f, 4);
            Decor(_visuals != null ? _visuals.plantHouse : null, 100f, 0f, 1f, 2);
        }

        private void BuildHill()
        {
            PlaceChunk(_flat);
            PlaceChunk(_platform);
            PlaceChunk(_flat);
            PlaceChunk(_platform);
            PlaceChunk(_flat);
            PlaceChunk(_flat);
            PlaceChunk(_platform);

            CoinArc(168f, 5, 0.85f, 1.5f, 1.1f);
            CoinArc(210f, 4, 0.9f, 1.45f, 1.2f);
            PlaceObstacle(_spikes, new Vector3(248f, 0.55f, 0f));
            CoinRow(258f, 4, 0.9f, 2.4f);

            Decor(_visuals != null ? _visuals.pine : null, 175f, 0f, 1.2f, 2);
            Decor(_visuals != null ? _visuals.pine : null, 188f, 0f, 1.05f, 2);
            Decor(_visuals != null ? _visuals.rock : null, 200f, 0f, 1f, 3);
            Decor(_visuals != null ? _visuals.kenneyRock : null, 236f, 0f, 1f, 3);
            Decor(_visuals != null ? _visuals.bush : null, 270f, 0f, 1.1f, 3);
        }

        private void BuildBrokenPath()
        {
            PlaceChunk(_flat);
            PlaceChunk(_gap);
            PlaceChunk(_flat);
            PlaceChunk(_platform);
            PlaceChunk(_flat);
            PlaceChunk(_gap);
            PlaceChunk(_flat);

            CoinRow(290f, 3, 1f, 1.35f);
            // High route: extra platform and the second lost star.
            PlaceExtraPlatform(348f, 2.35f, 3.2f);
            PlaceStar(new Vector3(348f, 3.35f, 0f));
            CoinRow(360f, 3, 0.8f, 2.6f);
            PlaceObstacle(_crate, new Vector3(392f, 0.7f, 0f));

            Decor(_visuals != null ? _visuals.rock : null, 300f, 0f, 1.15f, 3);
            Decor(_visuals != null ? _visuals.kenneyRock : null, 318f, 0f, 1.2f, 3);
            Decor(_visuals != null ? _visuals.sign : null, 332f, 0f, 1f, 4);
            Decor(_visuals != null ? _visuals.pine : null, 400f, 0f, 1.1f, 2);
        }

        private void BuildForest()
        {
            PlaceChunk(_flat);
            PlaceChunk(_flat);
            PlaceChunk(_platform);
            PlaceChunk(_flat);
            PlaceChunk(_flat);
            PlaceChunk(_platform);
            PlaceChunk(_flat);
            PlaceChunk(_flat);
            PlaceChunk(_platform);
            PlaceChunk(_flat);
            PlaceChunk(_flat);

            CoinArc(448f, 4, 0.85f, 1.4f, 0.9f);
            PlaceObstacle(_spikes, new Vector3(472f, 0.55f, 0f));
            CoinRow(486f, 5, 0.85f, 1.35f);
            PlaceObstacle(_crate, new Vector3(520f, 0.7f, 0f));
            if (_gem != null)
            {
                PlaceCollectible(_gem, new Vector3(538f, 2.5f, 0f));
            }

            CoinArc(560f, 5, 0.8f, 1.5f, 1.15f);
            PlaceObstacle(_spikes, new Vector3(602f, 0.55f, 0f));
            CoinRow(614f, 4, 0.9f, 1.35f);

            float[] pines = { 430f, 444f, 458f, 490f, 508f, 528f, 552f, 574f, 596f, 618f, 640f };
            for (int i = 0; i < pines.Length; i++)
            {
                Sprite tree = i % 3 == 0 && _visuals != null ? _visuals.palm : _visuals != null ? _visuals.pine : null;
                Decor(tree, pines[i], 0f, 1f + (i % 3) * 0.08f, 2);
            }

            Decor(_visuals != null ? _visuals.bush : null, 470f, 0f, 1.1f, 3);
            Decor(_visuals != null ? _visuals.plantHouse : null, 546f, 0f, 1.05f, 2);
            Decor(_visuals != null ? _visuals.cloud : null, 500f, 5.1f, 1.15f, -6);
        }

        private void BuildFinale()
        {
            PlaceChunk(_flat);
            PlaceChunk(_platform);
            PlaceChunk(_flat);
            PlaceChunk(_gap);
            PlaceChunk(_flat);
            PlaceChunk(_platform);
            PlaceChunk(_flat);

            PlaceObstacle(_spikes, new Vector3(668f, 0.55f, 0f));
            CoinArc(680f, 4, 0.8f, 1.55f, 1.1f);
            PlaceObstacle(_saw != null ? _saw : _spikes, new Vector3(708f, 0.55f, 0f));
            PlaceExtraPlatform(742f, 2.55f, 2.8f);
            PlaceStar(new Vector3(742f, 3.55f, 0f));
            CoinRow(754f, 4, 0.85f, 1.4f);
            PlaceObstacle(_crate, new Vector3(776f, 0.7f, 0f));

            Decor(_visuals != null ? _visuals.flagGreen : null, 650f, 0f, 1.15f, 5);
            Decor(_visuals != null ? _visuals.flagYellow : null, 790f, 0f, 1.15f, 5);
            Decor(_visuals != null ? _visuals.pine : null, 720f, 0f, 1.2f, 2);
            Decor(_visuals != null ? _visuals.bush : null, 770f, 0f, 1f, 3);
        }

        private void PlaceFinish()
        {
            PlaceChunk(_flat);
            PlaceChunk(_flat);
            FinishX = _cursor - 28f;

            Decor(_visuals != null ? _visuals.flagGreen : null, FinishX - 1.2f, 0f, 1.35f, 6);
            Decor(_visuals != null ? _visuals.flagYellow : null, FinishX + 1.2f, 0f, 1.35f, 6);
            Decor(_visuals != null ? _visuals.signExit : null, FinishX + 3.2f, 0f, 1.2f, 6);

            var finish = new GameObject("FinishLine");
            finish.transform.SetParent(_root, false);
            finish.transform.position = new Vector3(FinishX, 1.2f, 0f);
            var box = finish.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.6f, 4.5f);
            finish.AddComponent<AdventureFinishLine>();

            CoinRow(FinishX - 8f, 3, 0.9f, 1.5f);
        }

        private void PlaceChunk(GameObject prefab)
        {
            if (prefab == null)
            {
                prefab = _flat;
            }

            if (prefab == null)
            {
                _cursor += 20f;
                return;
            }

            GameObject instance = Instantiate(prefab, new Vector3(_cursor, 0f, 0f), Quaternion.identity, _root);
            instance.name = prefab.name;
            var chunk = instance.GetComponent<Chunk>();
            _cursor += chunk != null ? chunk.Length : 20f;
        }

        private void PlaceObstacle(GameObject prefab, Vector3 position)
        {
            if (prefab == null)
            {
                return;
            }

            Spawn(prefab, position);
        }

        private void PlaceStar(Vector3 position)
        {
            if (_starsPlaced >= SpecialStarCount || _star == null || _pickup == null)
            {
                return;
            }

            GameObject instance = PlaceCollectible(_star, position);
            if (instance != null)
            {
                instance.AddComponent<AdventureStarMarker>();
                _starsPlaced++;
            }
        }

        private GameObject PlaceCollectible(CollectibleData data, Vector3 position)
        {
            if (_pickup == null || data == null)
            {
                return null;
            }

            GameObject instance = Spawn(_pickup, position);
            instance.GetComponent<Collectible>()?.Configure(data);
            return instance;
        }

        private void CoinRow(float startX, int count, float step, float y)
        {
            for (int i = 0; i < count; i++)
            {
                PlaceCollectible(_coin, new Vector3(startX + step * i, y, 0f));
            }
        }

        private void CoinArc(float startX, int count, float step, float baseY, float height)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (float)(count - 1);
                float y = baseY + Mathf.Sin(t * Mathf.PI) * height;
                PlaceCollectible(_coin, new Vector3(startX + step * i, y, 0f));
            }
        }

        private void PlaceExtraPlatform(float x, float y, float width)
        {
            var go = new GameObject("HillPlatform");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(x, y, 0f);
            int ground = LayerMask.NameToLayer("Ground");
            if (ground >= 0)
            {
                go.layer = ground;
            }

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.45f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _visuals != null && _visuals.smallPlatform != null
                ? _visuals.smallPlatform
                : _visuals != null ? _visuals.groundTop : null;
            renderer.sortingOrder = 1;
            if (_spriteMaterial != null)
            {
                renderer.sharedMaterial = _spriteMaterial;
            }
        }

        private void Decor(Sprite sprite, float x, float y, float scale, int sorting)
        {
            if (sprite == null)
            {
                return;
            }

            var go = new GameObject(sprite.name);
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sorting;
            if (_spriteMaterial != null)
            {
                renderer.sharedMaterial = _spriteMaterial;
            }
        }

        private GameObject Spawn(GameObject prefab, Vector3 position)
        {
            if (_pool != null)
            {
                return _pool.Get(prefab, position, Quaternion.identity, _root);
            }

            return Instantiate(prefab, position, Quaternion.identity, _root);
        }

        private static GameObject Named(GameObject[] prefabs, string key)
        {
            if (prefabs == null)
            {
                return null;
            }

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] != null &&
                    prefabs[i].name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return prefabs[i];
                }
            }

            return null;
        }
    }
}
