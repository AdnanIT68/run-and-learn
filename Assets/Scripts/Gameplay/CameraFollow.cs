using FootballRunner.Player;
using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// Keeps the player in the left third of the screen so there is room to see
    /// what is coming. Tracking is horizontal only: following jump height made
    /// the whole world pump up and down on every hop.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Offset")]
        [SerializeField] private float horizontalOffset = 6f;
        [SerializeField] private float baseHeight = 2.5f;

        [Header("Damage Feedback")]
        [Tooltip("Tiny horizontal punch on hit. Keep this small to avoid discomfort.")]
        [SerializeField] private float damagePunch = 0.18f;
        [SerializeField] private float punchRecovery = 8f;

        private float _punch;
        private int _lastHealth = int.MaxValue;
        private Camera _camera;
        private float _baseOrtho;
        private float _phoneOffset;

        /// <summary>The camera's lead ahead of the runner now (larger on tablets).</summary>
        public float HorizontalOffset => horizontalOffset;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _phoneOffset = horizontalOffset;

            if (_camera != null && _camera.orthographic)
            {
                _baseOrtho = _camera.orthographicSize;
                FitTo(_camera.aspect);
            }
        }

        /// <summary>
        /// Zooms out a little on tablets and keeps the runner at the same share of the
        /// screen as on a phone (see UI.ScreenFit). Phones keep the scene's settings.
        /// </summary>
        public void FitTo(float aspect)
        {
            if (_camera == null || _baseOrtho <= 0f)
            {
                return;
            }

            _camera.orthographicSize = _baseOrtho * UI.ScreenFit.ZoomOut(aspect);
            horizontalOffset = UI.ScreenFit.RunnerOffset(_baseOrtho, _phoneOffset, aspect);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void OnEnable()
        {
            PlayerHealth.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            PlayerHealth.HealthChanged -= OnHealthChanged;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                if (PlayerController.Instance != null)
                {
                    target = PlayerController.Instance.transform;
                }
                else
                {
                    return;
                }
            }

            if (_punch > 0f)
            {
                _punch = Mathf.MoveTowards(_punch, 0f, punchRecovery * Time.unscaledDeltaTime);
            }

            transform.position = new Vector3(
                target.position.x + horizontalOffset - _punch,
                baseHeight,
                transform.position.z);
        }

        private void OnHealthChanged(int current, int max)
        {
            if (current < _lastHealth && current < max && current >= 0)
            {
                _punch = damagePunch;
            }

            _lastHealth = current;
        }
    }
}
