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
            "Assets/Resources/Words/"
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
            settings.spritePixelsPerUnit = 100f;
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
