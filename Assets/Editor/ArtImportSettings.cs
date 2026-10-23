using UnityEditor;
using UnityEngine;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Import rules for the Kenney art and audio. Written as a postprocessor rather
    /// than a one-shot pass so that anything dropped into these folders later is
    /// configured the same way without a manual step.
    /// </summary>
    internal class ArtImportSettings : AssetPostprocessor
    {
        /// <summary>One 64px tile equals one world unit.</summary>
        public const int PixelsPerUnit = 64;

        private const string ArtRoot = "Assets/Art/";
        private const string AudioRoot = "Assets/Audio/";
        private const string PlaceholderFolder = "Assets/Art/Placeholder";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot) || assetPath.StartsWith(PlaceholderFolder))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;

            // Everything that has a counterpart on the settings struct is set there,
            // because SetTextureSettings would otherwise overwrite direct assignments.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = PixelsPerUnit;

            // Tiled draw mode silently falls back to stretching unless the sprite
            // mesh is a full rect, and the ground is built entirely from tiling.
            settings.spriteMeshType = SpriteMeshType.FullRect;

            settings.filterMode = FilterMode.Bilinear;
            settings.alphaIsTransparency = true;
            settings.mipmapEnabled = false;
            settings.wrapMode = TextureWrapMode.Clamp;

            importer.SetTextureSettings(settings);
            importer.maxTextureSize = 512;
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            // These are all short one-shots. Decompressing up front trades a little
            // memory for no decode hitch at the moment they play.
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;

            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
        }
    }
}
