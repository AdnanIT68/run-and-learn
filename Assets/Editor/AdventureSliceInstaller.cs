using FootballRunner.Adventure;
using FootballRunner.Collectibles;
using UnityEditor;
using UnityEngine;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Wires Green Hills adventure art into Resources so device builds can load it.
    /// Safe to re-run.
    /// </summary>
    internal static class AdventureSliceInstaller
    {
        private const string Folder = "Assets/Resources/Adventure";
        private const string VisualsPath = Folder + "/AdventureVisuals.asset";
        private const string StarPath = Folder + "/LostStar.asset";

        [MenuItem("Tools/Football Runner/7 - Install Green Hills Adventure Slice")]
        public static void Apply()
        {
            BuildUtils.EnsureFolder(Folder);
            CreateVisuals();
            CreateLostStar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AdventureSliceInstaller] Green Hills adventure art installed.");
        }

        private static void CreateVisuals()
        {
            var visuals = AssetDatabase.LoadAssetAtPath<AdventureVisuals>(VisualsPath);
            if (visuals == null)
            {
                visuals = ScriptableObject.CreateInstance<AdventureVisuals>();
                AssetDatabase.CreateAsset(visuals, VisualsPath);
            }

            visuals.rukajjaPortrait = LoadSprite("Assets/Art/Rukajja/Frames/Rukajja_Idle.png");
            visuals.pine = LoadSprite("Assets/ThirdParty/Ansimuz/SunnyLand/environment/Props/pine.png");
            visuals.palm = LoadSprite("Assets/ThirdParty/Ansimuz/SunnyLand/environment/Props/palm.png");
            visuals.bush = LoadSprite("Assets/ThirdParty/Ansimuz/SunnyLand/environment/Props/bush.png");
            visuals.rock = LoadSprite("Assets/ThirdParty/Ansimuz/SunnyLand/environment/Props/rock-2.png");
            visuals.plantHouse = LoadSprite("Assets/ThirdParty/Ansimuz/SunnyLand/environment/Props/plant-house.png");
            visuals.grassTuft = LoadSprite("Assets/Art/Tiles/grass.png");
            visuals.kenneyBush = LoadSprite("Assets/Art/Tiles/bush.png");
            visuals.kenneyRock = LoadSprite("Assets/Art/Tiles/rock.png");
            visuals.flagGreen = LoadSprite("Assets/Art/Tiles/flag_green_a.png");
            visuals.flagYellow = LoadSprite("Assets/Art/Tiles/flag_yellow_a.png");
            visuals.signExit = LoadSprite("Assets/Art/Tiles/sign_exit.png");
            visuals.sign = LoadSprite("Assets/Art/Tiles/sign.png");
            visuals.cloud = LoadSprite("Assets/Art/Tiles/terrain_grass_cloud.png");
            visuals.smallPlatform = LoadSprite("Assets/ThirdParty/Ansimuz/SunnyLand/environment/Props/small-platform.png");
            visuals.groundTop = LoadSprite("Assets/Art/Tiles/terrain_grass_block_top.png");
            EditorUtility.SetDirty(visuals);
        }

        private static void CreateLostStar()
        {
            var data = AssetDatabase.LoadAssetAtPath<CollectibleData>(StarPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CollectibleData>();
                AssetDatabase.CreateAsset(data, StarPath);
            }

            data.displayName = "Lost Star";
            data.sprite = LoadSprite("Assets/Art/Tiles/star.png");
            if (data.sprite == null)
            {
                var existing = AssetDatabase.LoadAssetAtPath<CollectibleData>("Assets/ScriptableObjects/Collectibles/Star.asset");
                data.sprite = existing != null ? existing.sprite : null;
            }

            data.tint = Color.white;
            data.scale = 1.55f;
            data.scorePoints = 50;
            data.coinValue = 0;
            data.healAmount = 0;
            data.spawnWeight = 0f;
            EditorUtility.SetDirty(data);
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[AdventureSliceInstaller] Missing sprite: {path}");
            }

            return sprite;
        }
    }
}
