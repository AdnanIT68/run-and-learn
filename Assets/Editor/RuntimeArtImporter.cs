using UnityEditor;
using UnityEngine;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Pictures loaded at runtime from Resources (word pictures, icons, menu art)
    /// must import as one whole sprite. The project default is Multiple, which
    /// auto-slices transparent images into islands and Resources.Load then
    /// returns only the first fragment.
    /// </summary>
    internal class RuntimeArtImporter : AssetPostprocessor
    {
        private static readonly string[] Folders =
        {
            "Assets/Resources/Menu/",
            "Assets/Resources/Words/",
            "Assets/Resources/Worlds/",
            "Assets/Resources/Characters/"
        };

        private void OnPreprocessTexture()
        {
            if (!IsRuntimeArt(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            // Ground tiles replace Kenney tiles at 64 px per unit, so they must match it.
            settings.spritePixelsPerUnit = assetPath.Contains("/Worlds/Ground/") ? 64f : 100f;

            // Runner frames stand on their feet like Rukajja's: pivot at the bottom, and a
            // pixel density that makes the drawn kids as tall as she is (her frames are 64 at half the size).
            if (assetPath.StartsWith("Assets/Resources/Characters/"))
            {
                bool rukajja = assetPath.Contains("/rukajja/");
                settings.spritePixelsPerUnit = rukajja ? 64f : 112f;
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            }
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.alphaIsTransparency = true;
            settings.mipmapEnabled = false;
            settings.filterMode = FilterMode.Bilinear;
            settings.wrapMode = TextureWrapMode.Clamp;

            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 2048;
        }

        private static bool IsRuntimeArt(string path)
        {
            foreach (string folder in Folders)
            {
                if (path.StartsWith(folder))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
