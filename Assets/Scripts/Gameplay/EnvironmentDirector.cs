using System;
using System.Collections.Generic;
using FootballRunner.Core;
using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// Cycles visual areas during a single run. Score, coins, lives and the
    /// generator keep going; only colours and the area banner change.
    /// </summary>
    public class EnvironmentDirector : MonoBehaviour
    {
        public static EnvironmentDirector Instance { get; private set; }

        public static event Action<AreaTheme, bool> AreaChanged;

        [Header("Areas")]
        [Tooltip("Played in order, then repeated. Empty uses the built-in three areas.")]
        [SerializeField] private AreaTheme[] themes;

        [Header("Transition")]
        [SerializeField] private float blendSeconds = 2.4f;
        [SerializeField] private int safeChunksAfterChange = 2;

        [Header("Parallax")]
        [SerializeField] private float distantParallaxFactor = 0.08f;

        private readonly List<ParallaxLayer> _layers = new List<ParallaxLayer>(4);
        private readonly List<SpriteRenderer> _layerRenderers = new List<SpriteRenderer>(4);
        private readonly List<Color> _layerBaseColours = new List<Color>(4);

        private Camera _camera;
        private Color _cameraFrom;
        private Color _cameraTo;
        private Color _backgroundFrom = Color.white;
        private Color _backgroundTo = Color.white;
        private Color _groundFrom = Color.white;
        private Color _groundTo = Color.white;
        private float _blend;
        private int _themeIndex = -1;
        private int _safeChunksRemaining;
        private bool _clonedDistantLayer;

        public AreaTheme CurrentTheme { get; private set; }

        public string CurrentDisplayName =>
            CurrentTheme != null ? CurrentTheme.displayName : "GREEN HILLS";

        public Color CurrentGroundTint { get; private set; } = Color.white;

        public int SafeChunksRemaining => _safeChunksRemaining;

        public static EnvironmentDirector EnsureOn(GameObject host)
        {
            if (Instance != null)
            {
                return Instance;
            }

            if (host == null)
            {
                return null;
            }

            var director = host.GetComponent<EnvironmentDirector>();
            return director != null ? director : host.AddComponent<EnvironmentDirector>();
        }

        public bool ConsumeSafeChunk()
        {
            if (_safeChunksRemaining <= 0)
            {
                return false;
            }

            _safeChunksRemaining--;
            return true;
        }

        private void Reset()
        {
            blendSeconds = 2.4f;
            safeChunksAfterChange = 2;
            distantParallaxFactor = 0.08f;
            themes = CreateBuiltInThemes();
        }

        private void OnValidate()
        {
            EnsureDefaultThemes();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureDefaultThemes();

            if (themes == null || themes.Length == 0 || themes[0] == null || themes[0].IsUnconfigured())
            {
                themes = CreateBuiltInThemes();
            }

            ResolveCamera();
            CollectLayers();
            EnsureDistantLayer();
            ApplyTheme(0, instant: true);
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
            if (themes == null || themes.Length == 0)
            {
                return;
            }

            if (Adventure.AdventureSession.IsActive)
            {
                if (_blend < 1f)
                {
                    _blend = Mathf.MoveTowards(_blend, 1f, Time.deltaTime / Mathf.Max(0.15f, blendSeconds));
                    ApplyBlend(_blend);
                }

                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                int next = ThemeIndexForElapsed(DifficultyManager.Instance != null
                    ? DifficultyManager.Instance.ElapsedSeconds
                    : 0f);

                if (next != _themeIndex)
                {
                    ApplyTheme(next, instant: false);
                }
            }

            if (_blend >= 1f)
            {
                return;
            }

            _blend = Mathf.MoveTowards(_blend, 1f, Time.deltaTime / Mathf.Max(0.15f, blendSeconds));
            ApplyBlend(_blend);
        }

        /// <summary>
        /// Fills an empty or Unity-blank Themes list. Already named and timed
        /// entries are left alone so Inspector work is not wiped.
        /// </summary>
        private void EnsureDefaultThemes()
        {
            AreaTheme[] builtIn = CreateBuiltInThemes();

            if (themes == null || themes.Length == 0 || !HasAnyConfiguredTheme())
            {
                themes = builtIn;
                return;
            }

            for (int i = 0; i < themes.Length; i++)
            {
                if (themes[i] != null && !themes[i].IsUnconfigured())
                {
                    continue;
                }

                if (themes[i] == null)
                {
                    themes[i] = new AreaTheme();
                }

                themes[i].CopyFrom(builtIn[Mathf.Clamp(i, 0, builtIn.Length - 1)]);
            }
        }

        private bool HasAnyConfiguredTheme()
        {
            if (themes == null)
            {
                return false;
            }

            for (int i = 0; i < themes.Length; i++)
            {
                if (themes[i] != null && !themes[i].IsUnconfigured())
                {
                    return true;
                }
            }

            return false;
        }

        public static AreaTheme[] CreateBuiltInThemes()
        {
            return new[]
            {
                new AreaTheme
                {
                    displayName = "GREEN HILLS",
                    durationSeconds = 60f,
                    cameraColor = new Color(0.45f, 0.75f, 0.95f, 1f),
                    backgroundTint = Color.white,
                    groundTint = Color.white
                },
                new AreaTheme
                {
                    displayName = "SUNSET",
                    durationSeconds = 60f,
                    cameraColor = new Color(0.95f, 0.40f, 0.36f, 1f),
                    backgroundTint = new Color(1f, 0.68f, 0.58f, 1f),
                    groundTint = new Color(1f, 0.86f, 0.76f, 1f)
                },
                new AreaTheme
                {
                    displayName = "NIGHT",
                    durationSeconds = 60f,
                    cameraColor = new Color(0.07f, 0.11f, 0.22f, 1f),
                    backgroundTint = new Color(0.55f, 0.66f, 0.95f, 1f),
                    groundTint = new Color(0.78f, 0.84f, 0.96f, 1f)
                }
            };
        }

        private int ThemeIndexForElapsed(float elapsed)
        {
            float cycle = 0f;

            for (int i = 0; i < themes.Length; i++)
            {
                cycle += ThemeDuration(themes[i]);
            }

            if (cycle <= 0f)
            {
                return 0;
            }

            float wrapped = elapsed % cycle;
            float cursor = 0f;

            for (int i = 0; i < themes.Length; i++)
            {
                cursor += ThemeDuration(themes[i]);

                if (wrapped < cursor)
                {
                    return i;
                }
            }

            return 0;
        }

        private static float ThemeDuration(AreaTheme theme)
        {
            return theme != null && theme.durationSeconds > 0.001f
                ? theme.durationSeconds
                : 60f;
        }

        private void ApplyTheme(int index, bool instant)
        {
            if (themes == null || index < 0 || index >= themes.Length || themes[index] == null)
            {
                return;
            }

            bool first = _themeIndex < 0;
            _themeIndex = index;
            CurrentTheme = themes[index];

            _cameraFrom = _camera != null ? _camera.backgroundColor : CurrentTheme.cameraColor;
            _backgroundFrom = instant ? CurrentTheme.backgroundTint : _backgroundTo;
            _groundFrom = instant ? CurrentTheme.groundTint : CurrentGroundTint;
            _cameraTo = CurrentTheme.cameraColor;
            _backgroundTo = CurrentTheme.backgroundTint;
            _groundTo = CurrentTheme.groundTint;
            _blend = instant ? 1f : 0f;

            if (!first)
            {
                _safeChunksRemaining = Mathf.Max(0, safeChunksAfterChange);
            }

            ApplyBlend(_blend);

            if (!instant)
            {
                AreaChanged?.Invoke(CurrentTheme, true);
            }
        }

        private void ApplyBlend(float t)
        {
            Color cameraColor = Color.Lerp(_cameraFrom, _cameraTo, t);
            Color background = Color.Lerp(_backgroundFrom, _backgroundTo, t);
            CurrentGroundTint = Color.Lerp(_groundFrom, _groundTo, t);

            if (_camera != null)
            {
                _camera.backgroundColor = cameraColor;
            }

            for (int i = 0; i < _layerRenderers.Count; i++)
            {
                if (_layerRenderers[i] == null)
                {
                    continue;
                }

                Color baseColour = i < _layerBaseColours.Count ? _layerBaseColours[i] : Color.white;
                _layerRenderers[i].color = baseColour * background;
            }
        }

        private void ResolveCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void CollectLayers()
        {
            _layers.Clear();
            _layerRenderers.Clear();
            _layerBaseColours.Clear();

            ParallaxLayer[] found = FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None);

            for (int i = 0; i < found.Length; i++)
            {
                RegisterLayer(found[i]);
            }
        }

        private void RegisterLayer(ParallaxLayer layer)
        {
            if (layer == null || _layers.Contains(layer))
            {
                return;
            }

            _layers.Add(layer);
            SpriteRenderer renderer = layer.Renderer;

            if (renderer != null)
            {
                _layerRenderers.Add(renderer);
                _layerBaseColours.Add(Color.white);
            }
        }

        /// <summary>
        /// Adds a slower copy of the existing hills so the backdrop has two depths
        /// without importing a second scene object or extra art. SunnyLand already
        /// ships three bands, so a clone would only duplicate the sky.
        /// </summary>
        private void EnsureDistantLayer()
        {
            if (_clonedDistantLayer || _layers.Count == 0)
            {
                return;
            }

            if (_layers.Count >= 2)
            {
                _clonedDistantLayer = true;
                return;
            }

            ParallaxLayer source = _layers[0];
            Transform parent = source.transform.parent;

            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i] != null && _layers[i].name.IndexOf("Distant", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _clonedDistantLayer = true;
                    return;
                }
            }

            var clone = Instantiate(source.gameObject, parent);
            clone.name = "DistantHills";
            var layer = clone.GetComponent<ParallaxLayer>();
            var renderer = clone.GetComponent<SpriteRenderer>();

            if (layer != null)
            {
                layer.SetFactor(distantParallaxFactor);
            }

            if (renderer != null)
            {
                renderer.sortingOrder -= 8;
                renderer.color = new Color(0.92f, 0.95f, 1f, 1f);
            }

            RegisterLayer(layer);
            _clonedDistantLayer = true;
        }
    }
}
