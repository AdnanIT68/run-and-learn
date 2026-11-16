using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// The area over a letter's guide that a finger draws on. It paints the strokes into
    /// a small texture and reports each point (0..1, y up) to the tracing game.
    /// </summary>
    public class TracingSurface : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private const int Size = 256;
        private const int Brush = 13;
        private static readonly Color32 Ink = new Color32(255, 140, 40, 255);
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        private Texture2D _texture;
        private Color32[] _pixels;
        private RectTransform _rect;
        private Vector2? _last;

        public event Action<Vector2> Touched;

        public static TracingSurface Create(Transform parent, Vector2 position, float side)
        {
            var go = new GameObject("TracingSurface", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(side, side);
            var surface = go.AddComponent<TracingSurface>();
            surface._rect = rect;
            surface._texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            surface._pixels = new Color32[Size * Size];
            surface.Wipe();
            go.GetComponent<RawImage>().texture = surface._texture;
            return surface;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }

        public void Wipe()
        {
            for (int i = 0; i < _pixels.Length; i++)
            {
                _pixels[i] = Clear;
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply();
            _last = null;
        }

        /// <summary>The finger leaves the screen: the next point starts a new stroke.</summary>
        public void Lift()
        {
            _last = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _last = null;
            Handle(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Handle(eventData);
        }

        /// <summary>Draws at a point (0..1, y up) as a finger would; also used by tests.</summary>
        public void DrawAt(Vector2 point)
        {
            // Fill the gap from the previous point so fast swipes leave no holes.
            Vector2 from = _last ?? point;
            float distance = Vector2.Distance(from, point) * Size;
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / (Brush * 0.5f)));

            for (int i = 1; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(from, point, (float)i / steps);
                Stamp(p);
                Touched?.Invoke(p);
            }

            _last = point;
            _texture.SetPixels32(_pixels);
            _texture.Apply();
        }

        private void Handle(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position,
                    eventData.pressEventCamera, out Vector2 local))
            {
                Rect r = _rect.rect;
                var point = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);

                // A finger that slides off the board leaves no ink on its edge; coming back starts a new stroke.
                if (point.x < 0f || point.x > 1f || point.y < 0f || point.y > 1f)
                {
                    _last = null;
                    return;
                }

                DrawAt(point);
            }
        }

        private void Stamp(Vector2 point)
        {
            int cx = Mathf.RoundToInt(Mathf.Clamp01(point.x) * (Size - 1));
            int cy = Mathf.RoundToInt(Mathf.Clamp01(point.y) * (Size - 1));

            for (int y = Mathf.Max(0, cy - Brush); y <= Mathf.Min(Size - 1, cy + Brush); y++)
            {
                for (int x = Mathf.Max(0, cx - Brush); x <= Mathf.Min(Size - 1, cx + Brush); x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;

                    if (dx * dx + dy * dy <= Brush * Brush)
                    {
                        _pixels[y * Size + x] = Ink;
                    }
                }
            }
        }
    }
}
