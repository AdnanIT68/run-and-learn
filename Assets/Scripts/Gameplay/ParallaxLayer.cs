using UnityEngine;

namespace FootballRunner.Gameplay
{
    /// <summary>
    /// An endlessly repeating backdrop layer that drifts at a fraction of the
    /// camera's speed. Nothing is ever spawned or destroyed: a single tiled
    /// sprite is re-anchored each frame, and because the pattern repeats every
    /// tile width the seam is never visible.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("0 keeps the layer pinned to the camera (infinitely distant). " +
                 "1 leaves it fixed in the world, like ordinary scenery.")]
        [Range(0f, 1f)]
        [SerializeField] private float parallaxFactor = 0.5f;

        [Tooltip("Vertical shift from the camera centre, in world units.")]
        [SerializeField] private float verticalOffset = 0f;

        [Tooltip("Extra height beyond the camera view, so the edges never show " +
                 "when the camera drifts up during a jump.")]
        [SerializeField] private float heightMargin = 1.4f;

        [Tooltip("When enabled, the layer is scaled so one tile covers the camera " +
                 "height. Disable for art that must keep its native pixel size.")]
        [SerializeField] private bool fitToViewHeight = true;

        private Camera _camera;
        private SpriteRenderer _renderer;
        private float _tileWidth;

        public SpriteRenderer Renderer
        {
            get
            {
                if (_renderer == null)
                {
                    _renderer = GetComponent<SpriteRenderer>();
                }

                return _renderer;
            }
        }

        public void SetFactor(float factor)
        {
            parallaxFactor = Mathf.Clamp01(factor);
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            ResolveCamera();
            Configure();
        }

        private void OnEnable()
        {
            ResolveCamera();
        }

        private void ResolveCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void Configure()
        {
            if (_camera == null || _renderer == null || _renderer.sprite == null)
            {
                return;
            }

            Sprite sprite = _renderer.sprite;
            float spriteWidth = sprite.rect.width / sprite.pixelsPerUnit;
            float spriteHeight = sprite.rect.height / sprite.pixelsPerUnit;

            float viewHeight = _camera.orthographicSize * 2f;
            float viewWidth = viewHeight * Mathf.Max(0.01f, _camera.aspect);

            float scale;
            if (fitToViewHeight)
            {
                // Uniform scale so one tile covers the view vertically, then
                // widen the tiled area enough to survive any horizontal drift.
                float margin = Mathf.Max(1f, heightMargin);
                scale = viewHeight * margin / Mathf.Max(0.01f, spriteHeight);
                transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                scale = Mathf.Abs(transform.localScale.y);
                if (scale < 0.01f)
                {
                    scale = 1f;
                    transform.localScale = Vector3.one;
                }
            }

            _renderer.drawMode = SpriteDrawMode.Tiled;
            _renderer.tileMode = SpriteTileMode.Continuous;
            _renderer.size = new Vector2(viewWidth / scale + spriteWidth * 2f, spriteHeight);

            _tileWidth = spriteWidth * scale;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                ResolveCamera();

                if (_camera == null)
                {
                    return;
                }

                Configure();
            }

            if (_tileWidth <= 0f)
            {
                return;
            }

            Vector3 cameraPosition = _camera.transform.position;

            // Where the layer would sit if it scrolled at its own rate.
            float scrolledX = cameraPosition.x * (1f - parallaxFactor);

            // Snap that back to within one tile of the camera. The pattern is
            // periodic, so this is visually identical and keeps the renderer
            // centred on screen no matter how far the run goes.
            float wrapped = Mathf.Repeat(scrolledX - cameraPosition.x, _tileWidth);

            transform.position = new Vector3(
                cameraPosition.x + wrapped,
                cameraPosition.y * (1f - parallaxFactor) + verticalOffset,
                transform.position.z);
        }
    }
}
