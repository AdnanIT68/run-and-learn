using UnityEngine;

namespace FootballRunner.Utilities
{
    /// <summary>
    /// Simple shapes drawn once at runtime, so bubbles, HUD slots and effect
    /// particles need no art files.
    /// </summary>
    public static class RuntimeSprites
    {
        private const int Resolution = 128;

        private static Sprite _circle;
        private static Sprite _softDot;

        /// <summary>A white disc with a slightly darker rim. One world unit across.</summary>
        public static Sprite Circle()
        {
            if (_circle == null)
            {
                _circle = Build((distance, radius) =>
                {
                    float alpha = Mathf.Clamp01(radius - distance);
                    float shade = distance > radius - 6f ? 0.82f : 1f;
                    return new Color(shade, shade, shade, alpha);
                });
            }

            return _circle;
        }

        /// <summary>A white dot that fades out towards its edge, for dust and sparks.</summary>
        public static Sprite SoftDot()
        {
            if (_softDot == null)
            {
                _softDot = Build((distance, radius) =>
                {
                    float t = Mathf.Clamp01(distance / radius);
                    return new Color(1f, 1f, 1f, (1f - t * t) * Mathf.Clamp01(radius - distance));
                });
            }

            return _softDot;
        }

        private static Sprite _roundedRect;

        /// <summary>
        /// White rounded rectangle with nine-slice borders, for modern buttons and
        /// cards. Use Image.Type.Sliced; pixelsPerUnitMultiplier sets corner size.
        /// </summary>
        public static Sprite RoundedRect()
        {
            if (_roundedRect != null)
            {
                return _roundedRect;
            }

            const int size = 128;
            const float radius = 48f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance outside the inner rectangle whose corners are rounded off.
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            var border = new Vector4(radius + 2f, radius + 2f, radius + 2f, radius + 2f);
            _roundedRect = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            return _roundedRect;
        }

        private static Sprite Build(System.Func<float, float, Color> shade)
        {
            var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float centre = (Resolution - 1) * 0.5f;
            float radius = Resolution * 0.5f - 1f;
            var pixels = new Color32[Resolution * Resolution];

            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centre, centre));
                    pixels[y * Resolution + x] = shade(distance, radius);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, Resolution, Resolution), new Vector2(0.5f, 0.5f),
                Resolution);
        }
    }
}
