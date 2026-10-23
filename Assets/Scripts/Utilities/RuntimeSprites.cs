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
