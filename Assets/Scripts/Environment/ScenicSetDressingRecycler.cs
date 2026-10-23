using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// Recycles a fixed set of authored scenic panels horizontally. Child props
    /// keep the local pose they were given in the scene. Nothing is spawned or
    /// destroyed during a run.
    ///
    /// Theme tint uses EnvironmentDirector's serialized backgroundTint. That
    /// director has no renderer list; this component applies the same colour to
    /// the SpriteRenderers assigned here so EnvironmentDirector.cs is untouched.
    /// </summary>
    public class ScenicSetDressingRecycler : MonoBehaviour
    {
        private const float MinimumBlendSeconds = 0.15f;

        [Header("Camera")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform cameraTransform;

        [Header("Panels")]
        [SerializeField] private Transform[] panels;
        [SerializeField] private SpriteRenderer[] scenicRenderers;

        [Header("Recycling")]
        [SerializeField] private float panelWidth = 16f;
        [SerializeField] private float recycleMargin = 4f;
        [SerializeField] private float parallaxFactor = 0.48f;

        [Header("Theme")]
        [Tooltip("Must match EnvironmentDirector blendSeconds on Systems.")]
        [SerializeField] private float themeBlendSeconds = 2.4f;

        private Transform[] _panels;
        private SpriteRenderer[] _renderers;
        private Color[] _baseColours;
        private int _panelCount;
        private int _rendererCount;
        private float _cycleLength;
        private float _lastCameraX;
        private bool _valid;
        private bool _loggedConfigurationError;
        private bool _tintInitialised;
        private Color _tintFrom = Color.white;
        private Color _tintTo = Color.white;
        private float _tintBlend = 1f;

        private void Awake()
        {
            _valid = TryBind();
            if (!_valid)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_valid)
            {
                EnvironmentDirector.AreaChanged += OnAreaChanged;
            }
        }

        private void OnDisable()
        {
            EnvironmentDirector.AreaChanged -= OnAreaChanged;
        }

        private void LateUpdate()
        {
            if (!_valid)
            {
                return;
            }

            ApplyThemeTint();

            float cameraX = cameraTransform.position.x;

            if (Time.timeScale <= 0f)
            {
                _lastCameraX = cameraX;
                return;
            }

            float dx = cameraX - _lastCameraX;
            _lastCameraX = cameraX;

            if (dx != 0f)
            {
                float follow = dx * (1f - parallaxFactor);
                for (int i = 0; i < _panelCount; i++)
                {
                    Vector3 position = _panels[i].position;
                    position.x += follow;
                    _panels[i].position = position;
                }
            }

            float cameraLeft = cameraX - targetCamera.orthographicSize * targetCamera.aspect;
            float recycleLine = cameraLeft - recycleMargin;

            for (int i = 0; i < _panelCount; i++)
            {
                Transform panel = _panels[i];
                Vector3 position = panel.position;
                float rightEdge = position.x + panelWidth;

                while (rightEdge < recycleLine)
                {
                    position.x += _cycleLength;
                    rightEdge = position.x + panelWidth;
                }

                panel.position = position;
            }
        }

        private bool TryBind()
        {
            if (cameraTransform == null && targetCamera != null)
            {
                cameraTransform = targetCamera.transform;
            }

            if (targetCamera == null && cameraTransform != null)
            {
                targetCamera = cameraTransform.GetComponent<Camera>();
            }

            if (targetCamera == null || cameraTransform == null)
            {
                LogConfigurationError("ScenicSetDressingRecycler needs a Camera and camera Transform assigned in the scene.");
                return false;
            }

            if (panels == null || panels.Length == 0)
            {
                LogConfigurationError("ScenicSetDressingRecycler needs an ordered panels array assigned in the scene.");
                return false;
            }

            if (panelWidth <= 0.01f)
            {
                LogConfigurationError("ScenicSetDressingRecycler panelWidth must be greater than zero.");
                return false;
            }

            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null)
                {
                    LogConfigurationError("ScenicSetDressingRecycler panels array contains a missing Transform.");
                    return false;
                }
            }

            _panelCount = panels.Length;
            _panels = panels;
            _cycleLength = panelWidth * _panelCount;
            _lastCameraX = cameraTransform.position.x;

            if (scenicRenderers != null && scenicRenderers.Length > 0)
            {
                int assigned = 0;
                for (int i = 0; i < scenicRenderers.Length; i++)
                {
                    if (scenicRenderers[i] != null)
                    {
                        assigned++;
                    }
                }

                _renderers = new SpriteRenderer[assigned];
                _baseColours = new Color[assigned];
                int write = 0;
                for (int i = 0; i < scenicRenderers.Length; i++)
                {
                    SpriteRenderer renderer = scenicRenderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    _renderers[write] = renderer;
                    _baseColours[write] = renderer.color;
                    write++;
                }

                _rendererCount = assigned;
            }
            else
            {
                _renderers = null;
                _baseColours = null;
                _rendererCount = 0;
            }

            return true;
        }

        private void ApplyThemeTint()
        {
            if (_rendererCount == 0)
            {
                return;
            }

            EnvironmentDirector director = EnvironmentDirector.Instance;
            if (director == null || director.CurrentTheme == null)
            {
                return;
            }

            if (!_tintInitialised)
            {
                _tintTo = director.CurrentTheme.backgroundTint;
                _tintFrom = _tintTo;
                _tintBlend = 1f;
                _tintInitialised = true;
                WriteTint(_tintTo);
                return;
            }

            if (_tintBlend >= 1f)
            {
                return;
            }

            _tintBlend = Mathf.MoveTowards(
                _tintBlend,
                1f,
                Time.deltaTime / Mathf.Max(MinimumBlendSeconds, themeBlendSeconds));
            WriteTint(Color.Lerp(_tintFrom, _tintTo, _tintBlend));
        }

        private void WriteTint(Color tint)
        {
            for (int i = 0; i < _rendererCount; i++)
            {
                SpriteRenderer renderer = _renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.color = _baseColours[i] * tint;
            }
        }

        private void OnAreaChanged(AreaTheme theme, bool _)
        {
            if (!_valid || theme == null || _rendererCount == 0)
            {
                return;
            }

            _tintFrom = Color.Lerp(_tintFrom, _tintTo, _tintBlend);
            _tintTo = theme.backgroundTint;
            _tintBlend = 0f;
        }

        private void LogConfigurationError(string message)
        {
            if (_loggedConfigurationError)
            {
                return;
            }

            _loggedConfigurationError = true;
            Debug.LogError(message, this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (panelWidth < 0.01f)
            {
                panelWidth = 0.01f;
            }

            if (recycleMargin < 0f)
            {
                recycleMargin = 0f;
            }

            parallaxFactor = Mathf.Clamp01(parallaxFactor);

            if (themeBlendSeconds < MinimumBlendSeconds)
            {
                themeBlendSeconds = MinimumBlendSeconds;
            }
        }
#endif
    }
}
