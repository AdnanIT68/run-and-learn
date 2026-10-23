using System.Collections.Generic;
using FootballRunner.Learning;
using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// Gives each word world its own place: a two-layer backdrop drawn for that
    /// world, its sky, ground tiles, obstacle look and something in the air
    /// (petals, fluff, leaves, birds, rain or snow). Gameplay is untouched:
    /// ground and obstacles keep their colliders, only the pictures change.
    /// </summary>
    public class WorldScenery : MonoBehaviour
    {
        private const string Root = "Worlds/";

        private enum Weather
        {
            Petals,
            Fluff,
            Leaves,
            Birds,
            Rain,
            Snow
        }

        private struct Look
        {
            public Color Sky;
            public string Ground;
            public Color GroundTint;
            public Weather Weather;
            public int Particles;
        }

        private static readonly Dictionary<string, Look> Looks = new Dictionary<string, Look>
        {
            { "World1", new Look { Sky = new Color32(120, 200, 255, 255), Ground = null, GroundTint = Color.white, Weather = Weather.Petals, Particles = 14 } },
            { "World2", new Look { Sky = new Color32(125, 195, 245, 255), Ground = null, GroundTint = new Color(1f, 0.92f, 0.72f), Weather = Weather.Fluff, Particles = 14 } },
            { "World3", new Look { Sky = new Color32(135, 190, 250, 255), Ground = null, GroundTint = Color.white, Weather = Weather.Leaves, Particles = 12 } },
            { "World4", new Look { Sky = new Color32(140, 195, 250, 255), Ground = "stone", GroundTint = Color.white, Weather = Weather.Birds, Particles = 4 } },
            { "World5", new Look { Sky = new Color32(125, 195, 245, 255), Ground = "sand", GroundTint = Color.white, Weather = Weather.Rain, Particles = 45 } },
            { "World6", new Look { Sky = new Color32(165, 205, 240, 255), Ground = "snow", GroundTint = Color.white, Weather = Weather.Snow, Particles = 40 } }
        };

        private class Layer
        {
            public SpriteRenderer Renderer;
            public float Factor;
            public float TileWidth;
        }

        private struct Particle
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Vector2 Velocity;
            public float Phase;
            public float Spin;
        }

        public static WorldScenery Instance { get; private set; }

        private readonly List<Layer> _layers = new List<Layer>(2);
        private readonly List<Particle> _particles = new List<Particle>(48);
        private readonly Dictionary<string, Sprite> _groundTiles = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> _obstacleSkins = new Dictionary<string, Sprite>();
        private Look _look;
        private Sprite _far;
        private Sprite _mid;
        private Sprite _particleSprite;
        private Camera _camera;

        /// <summary>The world whose scenery is shown; null when the old look is kept.</summary>
        public string WorldName { get; private set; }

        public int ParticleCount => _particles.Count;

        public static WorldScenery EnsureOn(GameObject host)
        {
            if (Instance != null || host == null)
            {
                return Instance;
            }

            var scenery = host.GetComponent<WorldScenery>();
            return scenery != null ? scenery : host.AddComponent<WorldScenery>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            if (Adventure.AdventureSession.IsActive)
            {
                return;
            }

            string world = LearningProgress.SelectedPack;

            if (!Looks.TryGetValue(world, out _look))
            {
                return;
            }

            _far = Resources.Load<Sprite>(Root + world + "/far");
            _mid = Resources.Load<Sprite>(Root + world + "/mid");

            if (_far == null)
            {
                Debug.LogWarning($"[WorldScenery] No scenery for {world}; keeping the default backdrop.");
                return;
            }

            WorldName = world;
            LoadGround();
            LoadObstacleSkins(world);
            _particleSprite = Resources.Load<Sprite>(Root + "Ambient/" + WeatherSprite(_look.Weather));

            // One fixed area named after the world, instead of the green/sunset/night cycle.
            WordPack pack = WordPack.Load(world);
            var theme = new AreaTheme
            {
                displayName = pack != null ? pack.Title(LearningProgress.Language).ToUpperInvariant() : world,
                durationSeconds = 100000f,
                cameraColor = _look.Sky,
                backgroundTint = Color.white,
                groundTint = _look.GroundTint
            };
            EnvironmentDirector.WorldOverride = theme;
            EnvironmentDirector.Instance?.UseSingleTheme(theme);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                EnvironmentDirector.WorldOverride = null;
            }
        }

        private void Start()
        {
            if (WorldName == null)
            {
                return;
            }

            _camera = Camera.main;

            // The old jungle backdrop and its clones step aside for this world's own layers.
            foreach (ParallaxLayer old in FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None))
            {
                old.gameObject.SetActive(false);
            }

            foreach (ScenicSetDressingRecycler props in FindObjectsByType<ScenicSetDressingRecycler>(FindObjectsSortMode.None))
            {
                props.gameObject.SetActive(false);
            }

            AddLayer("WorldFar", _far, 0.08f, -60);

            if (_mid != null)
            {
                AddLayer("WorldMid", _mid, 0.3f, -38);
            }

            BuildParticles();
        }

        // ------------------------------------------------------------ ground

        private void LoadGround()
        {
            if (string.IsNullOrEmpty(_look.Ground))
            {
                return;
            }

            foreach (Sprite tile in Resources.LoadAll<Sprite>(Root + "Ground/" + _look.Ground))
            {
                // "terrain_snow_block_top" -> "block_top"
                string part = tile.name.Substring(("terrain_" + _look.Ground + "_").Length);
                _groundTiles[part] = tile;
            }
        }

        /// <summary>Swaps grass tiles for this world's ground (dirt, stone, sand or snow).</summary>
        public void SkinChunk(Chunk chunk)
        {
            if (chunk == null || _groundTiles.Count == 0)
            {
                return;
            }

            foreach (SpriteRenderer tile in chunk.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Sprite current = tile.sprite;

                if (current == null || !current.name.StartsWith("terrain_grass_"))
                {
                    continue;
                }

                if (_groundTiles.TryGetValue(current.name.Substring("terrain_grass_".Length), out Sprite replacement))
                {
                    tile.sprite = replacement;
                }
            }
        }

        // ------------------------------------------------------------ obstacles

        private void LoadObstacleSkins(string world)
        {
            foreach (string kind in new[] { "spike", "crate", "saw" })
            {
                Sprite skin = Resources.Load<Sprite>(Root + "Obstacles/" + world + "_" + kind);

                if (skin != null)
                {
                    _obstacleSkins[kind] = skin;
                }
            }
        }

        private static string ObstacleKind(string prefabName)
        {
            string name = prefabName.ToLowerInvariant();

            if (name.Contains("spike"))
            {
                return "spike";
            }

            return name.Contains("saw") ? "saw" : "crate";
        }

        /// <summary>
        /// Dresses an obstacle in this world's picture. The original sprite is hidden,
        /// not replaced, so the collider and the difficulty stay exactly the same.
        /// </summary>
        public void SkinObstacle(GameObject obstacle, string prefabName)
        {
            if (obstacle == null || _obstacleSkins.Count == 0 || obstacle.transform.Find("WorldSkin") != null)
            {
                return;
            }

            if (!_obstacleSkins.TryGetValue(ObstacleKind(prefabName), out Sprite skin))
            {
                return;
            }

            SpriteRenderer original = obstacle.GetComponentInChildren<SpriteRenderer>(true);

            if (original == null || original.sprite == null)
            {
                return;
            }

            Bounds bounds = original.bounds;
            original.enabled = false;

            var go = new GameObject("WorldSkin");
            go.transform.SetParent(obstacle.transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = skin;
            renderer.sortingLayerID = original.sortingLayerID;
            renderer.sortingOrder = original.sortingOrder;
            renderer.sharedMaterial = original.sharedMaterial;

            float target = Mathf.Max(bounds.size.x, bounds.size.y) * 1.15f;
            float scale = target / Mathf.Max(0.01f, Mathf.Max(skin.bounds.size.x, skin.bounds.size.y));
            float parentScale = Mathf.Max(0.01f, Mathf.Abs(obstacle.transform.lossyScale.x));
            go.transform.localScale = Vector3.one * (scale / parentScale);
            go.transform.position = new Vector3(bounds.center.x,
                bounds.min.y + skin.bounds.size.y * scale * 0.5f, obstacle.transform.position.z);
        }

        // ------------------------------------------------------------ backdrop

        private void AddLayer(string name, Sprite sprite, float factor, int order)
        {
            if (_camera == null)
            {
                return;
            }

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;

            // Fill the view height; the art's ground line then meets the track.
            float viewHeight = _camera.orthographicSize * 2f;
            float viewWidth = viewHeight * Mathf.Max(0.01f, _camera.aspect);
            float spriteWidth = sprite.bounds.size.x;
            float spriteHeight = sprite.bounds.size.y;
            float scale = viewHeight * 1.02f / spriteHeight;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.size = new Vector2(viewWidth / scale + spriteWidth * 2f, spriteHeight);

            _layers.Add(new Layer { Renderer = renderer, Factor = factor, TileWidth = spriteWidth * scale });
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            Vector3 cameraPosition = _camera.transform.position;

            foreach (Layer layer in _layers)
            {
                float scrolled = cameraPosition.x * (1f - layer.Factor);
                float wrapped = Mathf.Repeat(scrolled - cameraPosition.x, layer.TileWidth);
                layer.Renderer.transform.position = new Vector3(cameraPosition.x + wrapped, cameraPosition.y, 5f);
            }

            UpdateParticles(cameraPosition);
        }

        // ------------------------------------------------------------ weather

        private static string WeatherSprite(Weather weather)
        {
            switch (weather)
            {
                case Weather.Fluff: return "fluff";
                case Weather.Leaves: return "leaf";
                case Weather.Birds: return "bird";
                case Weather.Rain: return "rain";
                case Weather.Snow: return "snow";
                default: return "petal";
            }
        }

        private void BuildParticles()
        {
            if (_particleSprite == null || _camera == null)
            {
                return;
            }

            bool inFront = _look.Weather == Weather.Rain || _look.Weather == Weather.Snow;

            for (int i = 0; i < _look.Particles; i++)
            {
                var go = new GameObject("Weather");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = _particleSprite;
                renderer.sortingOrder = inFront ? 35 : -30;
                renderer.color = new Color(1f, 1f, 1f, inFront ? 0.75f : 0.95f);

                float size = ParticleSize(_look.Weather) * Random.Range(0.75f, 1.25f);
                float largest = Mathf.Max(_particleSprite.bounds.size.x, _particleSprite.bounds.size.y);
                go.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, largest));

                var particle = new Particle
                {
                    Transform = go.transform,
                    Renderer = renderer,
                    Velocity = ParticleVelocity(_look.Weather),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    Spin = _look.Weather == Weather.Leaves || _look.Weather == Weather.Petals
                        ? Random.Range(-90f, 90f)
                        : 0f
                };

                particle.Transform.position = RandomPointInView(_camera.transform.position);
                _particles.Add(particle);
            }
        }

        private static float ParticleSize(Weather weather)
        {
            switch (weather)
            {
                case Weather.Birds: return 0.7f;
                case Weather.Rain: return 0.45f;
                case Weather.Snow: return 0.22f;
                case Weather.Fluff: return 0.35f;
                default: return 0.28f;
            }
        }

        private static Vector2 ParticleVelocity(Weather weather)
        {
            switch (weather)
            {
                case Weather.Birds: return new Vector2(Random.Range(2.5f, 3.5f), Random.Range(-0.1f, 0.2f));
                case Weather.Rain: return new Vector2(-1.8f, Random.Range(-13f, -10f));
                case Weather.Snow: return new Vector2(-0.4f, Random.Range(-1.4f, -0.8f));
                case Weather.Fluff: return new Vector2(Random.Range(0.3f, 0.7f), Random.Range(0.05f, 0.25f));
                case Weather.Leaves: return new Vector2(Random.Range(-0.6f, -0.2f), Random.Range(-1.1f, -0.6f));
                default: return new Vector2(Random.Range(-0.6f, 0.2f), Random.Range(-0.7f, -0.4f));
            }
        }

        private Vector3 RandomPointInView(Vector3 cameraPosition)
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float minY = _look.Weather == Weather.Birds ? cameraPosition.y + halfHeight * 0.2f : cameraPosition.y - halfHeight;
            return new Vector3(
                cameraPosition.x + Random.Range(-halfWidth, halfWidth),
                Random.Range(minY, cameraPosition.y + halfHeight),
                0f);
        }

        private void UpdateParticles(Vector3 cameraPosition)
        {
            if (_particles.Count == 0)
            {
                return;
            }

            float dt = Time.deltaTime;
            float time = Time.time;
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            for (int i = 0; i < _particles.Count; i++)
            {
                Particle particle = _particles[i];
                Vector3 position = particle.Transform.position;
                float sway = _look.Weather == Weather.Rain || _look.Weather == Weather.Birds
                    ? 0f
                    : Mathf.Sin(time * 1.6f + particle.Phase) * 0.6f;
                position += new Vector3(particle.Velocity.x + sway, particle.Velocity.y, 0f) * dt;

                if (_look.Weather == Weather.Birds)
                {
                    // A slow wing beat, so the flock reads as birds rather than ticks.
                    float flap = 1f + Mathf.Sin(time * 9f + particle.Phase) * 0.25f;
                    Vector3 scale = particle.Transform.localScale;
                    particle.Transform.localScale = new Vector3(scale.x, Mathf.Abs(scale.x) * flap * 0.67f, 1f);
                }

                particle.Transform.Rotate(0f, 0f, particle.Spin * dt);

                // Leave the view on one side, come back in on the other.
                if (position.y < cameraPosition.y - halfHeight - 0.5f)
                {
                    position.y = cameraPosition.y + halfHeight + 0.5f;
                    position.x = cameraPosition.x + Random.Range(-halfWidth, halfWidth + 3f);
                }
                else if (position.y > cameraPosition.y + halfHeight + 0.5f)
                {
                    position.y = cameraPosition.y - halfHeight * 0.2f;
                }

                if (position.x < cameraPosition.x - halfWidth - 1f)
                {
                    position.x = cameraPosition.x + halfWidth + Random.Range(0.5f, 3f);
                }
                else if (position.x > cameraPosition.x + halfWidth + 3.5f)
                {
                    position.x = cameraPosition.x - halfWidth - 0.5f;
                }

                particle.Transform.position = position;
            }
        }
    }
}
