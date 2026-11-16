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
            "Assets/Resources/Characters/",
            "Assets/Resources/Story/",
            "Assets/Resources/Tracing/",
            "Assets/Resources/Outfits/"
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

            // The children and the menu art are seen up close, so they get the finer
            // compression (ASTC 4x4); scenery and word pictures use the normal one.
            // The full-screen menu background is soft scenery: normal compression is enough
            // there and halves its memory (2 MB to about 0.9 MB).
            bool closeUp = (assetPath.StartsWith("Assets/Resources/Characters/") ||
                            assetPath.StartsWith("Assets/Resources/Menu/") ||
                            assetPath.StartsWith("Assets/Resources/Story/")) &&
                           !assetPath.EndsWith("/Menu/background.png");
            importer.textureCompression = closeUp
                ? TextureImporterCompression.CompressedHQ
                : TextureImporterCompression.Compressed;

            // Word pictures are drawn at 256 px; the cap stops a larger source from bloating the build.
            importer.maxTextureSize = assetPath.StartsWith("Assets/Resources/Words/") ? 512 : 2048;
        }

        /// <summary>
        /// Spoken words and phrases: short mono speech, so 60 % Vorbis is clear enough,
        /// and keeping them compressed in memory saves RAM because only the clips of the
        /// current world are ever loaded, one at a time.
        /// </summary>
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Words/") && !assetPath.StartsWith("Assets/Resources/Tracing/"))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings sample = importer.defaultSampleSettings;
            sample.loadType = AudioClipLoadType.CompressedInMemory;
            sample.compressionFormat = AudioCompressionFormat.Vorbis;
            sample.quality = 0.6f;
            sample.preloadAudioData = false;
            importer.defaultSampleSettings = sample;
            importer.forceToMono = true;
            importer.loadInBackground = false;
        }

        /// <summary>Raised whenever the rules above change, so Unity reimports the affected files.</summary>
        public override uint GetVersion()
        {
            return 6;
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
