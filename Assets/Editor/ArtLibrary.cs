using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Typed access to the imported art, plus the measurement helpers the builder
    /// needs to line colliders up with the drawn pixels.
    /// </summary>
    internal static class ArtLibrary
    {
        public const string CharacterFolder = "Assets/Art/Characters";
        public const string TileFolder = "Assets/Art/Tiles";
        public const string BackgroundFolder = "Assets/Art/Backgrounds";
        public const string EnemyFolder = "Assets/Art/Enemies";
        public const string SfxFolder = "Assets/Audio/SFX";

        public static Sprite Character(string name) => Load(CharacterFolder, name);

        public static Sprite Tile(string name) => Load(TileFolder, name);

        public static Sprite Background(string name) => Load(BackgroundFolder, name);

        public static Sprite Enemy(string name) => Load(EnemyFolder, name);

        public static AudioClip Sfx(string name)
        {
            string path = $"{SfxFolder}/{name}.ogg";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (clip == null)
            {
                Debug.LogWarning($"[ArtLibrary] Missing audio clip: {path}");
            }

            return clip;
        }

        /// <summary>True when the art folders have been populated.</summary>
        public static bool IsPresent()
        {
            return AssetDatabase.IsValidFolder(CharacterFolder) &&
                   AssetDatabase.IsValidFolder(TileFolder) &&
                   Load(TileFolder, "terrain_grass_block_top") != null;
        }

        private static Sprite Load(string folder, string name)
        {
            string path = $"{folder}/{name}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null)
            {
                Debug.LogError($"[ArtLibrary] Missing sprite: {path}");
            }

            return sprite;
        }

        /// <summary>
        /// Bounds of the visible pixels expressed in world units relative to the
        /// sprite's centre. Kenney's characters sit inside a padded square canvas,
        /// so using the canvas for collision would leave the player hovering.
        /// </summary>
        public static Bounds OpaqueBounds(Sprite sprite)
        {
            string path = AssetDatabase.GetAssetPath(sprite);

            // Read the file rather than the imported texture: imported sprite
            // textures are compressed and not CPU readable.
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));

            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;

            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;

                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a <= 8)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            Object.DestroyImmediate(texture);

            if (maxX < 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            float ppu = sprite.pixelsPerUnit;

            // GetPixels32 is bottom-up, which already matches world orientation.
            float left = (minX - width * 0.5f) / ppu;
            float right = (maxX + 1 - width * 0.5f) / ppu;
            float bottom = (minY - height * 0.5f) / ppu;
            float top = (maxY + 1 - height * 0.5f) / ppu;

            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(left, bottom, 0f), new Vector3(right, top, 0f));
            return bounds;
        }
    }
}
