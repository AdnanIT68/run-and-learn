using System.Collections.Generic;
using System.IO;
using FootballRunner.Collectibles;
using FootballRunner.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Renders the generated prefabs to PNG files so the art can be inspected
    /// without launching the game. Automated tests prove the logic runs; only a
    /// picture shows whether the runner's feet actually meet the ground.
    /// </summary>
    internal static class ArtPreview
    {
        private const string OutputFolder = "_preview";

        [MenuItem("Tools/Football Runner/3 - Render Art Preview")]
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AddGlobalLight();
            AddBackground();

            var chunks = new List<GameObject>
            {
                PlaceChunk("Chunk_Flat", 0f),
                PlaceChunk("Chunk_Platform", 20f),
                PlaceChunk("Chunk_Gap", 40f)
            };

            foreach (GameObject chunk in chunks)
            {
                PopulateChunk(chunk);
            }

            PlacePlayer(3f);
            AddCharacterLineup(60f);

            Directory.CreateDirectory(OutputFolder);

            Shoot("01_flat_and_player", new Vector2(8f, 3f), 6f);
            Shoot("02_platform", new Vector2(28f, 3.5f), 6f);
            Shoot("03_gap", new Vector2(50f, 3f), 6f);
            Shoot("04_closeup_player", new Vector2(3.2f, 1.2f), 2.2f);
            Shoot("05_characters", new Vector2(63f, 1.6f), 3.2f);

            Debug.Log("[ArtPreview] Done.");
        }

        private static void AddGlobalLight()
        {
            // The URP 2D renderer draws lit sprites black without a global light.
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        /// <summary>
        /// Mirrors what ParallaxLayer does at runtime. Its Awake never fires in
        /// edit mode, so the sizing is reproduced here to make the backdrop visible.
        /// </summary>
        private static void AddBackground()
        {
            AddBackgroundLayer("background_color_hills", -40);
        }

        private static void AddBackgroundLayer(string spriteName, int order)
        {
            Sprite sprite = ArtLibrary.Background(spriteName);

            if (sprite == null)
            {
                return;
            }

            var go = new GameObject($"BG_{spriteName}");

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.sortingOrder = order;

            float spriteHeight = sprite.rect.height / sprite.pixelsPerUnit;
            float scale = 12f * 1.4f / spriteHeight;

            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.transform.position = new Vector3(0f, 3f, 0f);

            renderer.size = new Vector2(200f / scale, spriteHeight);
        }

        /// <summary>The five available runner colours, side by side, for choosing one.</summary>
        private static void AddCharacterLineup(float startX)
        {
            string[] colours = { "beige", "green", "pink", "purple", "yellow" };

            for (int i = 0; i < colours.Length; i++)
            {
                Sprite sprite = ArtLibrary.Character($"character_{colours[i]}_idle");

                if (sprite == null)
                {
                    continue;
                }

                Bounds art = ArtLibrary.OpaqueBounds(sprite);
                float scale = art.size.y > 0f ? 1.5f / art.size.y : 1f;

                var go = new GameObject(colours[i]);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                go.transform.position = new Vector3(
                    startX + i * 1.5f - art.center.x * scale,
                    -art.min.y * scale,
                    0f);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 10;
            }

            // Ground for the lineup to stand on.
            var ground = new GameObject("LineupGround");
            var groundRenderer = ground.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = ArtLibrary.Tile("terrain_grass_block_top");
            groundRenderer.drawMode = SpriteDrawMode.Tiled;
            groundRenderer.tileMode = SpriteTileMode.Continuous;
            groundRenderer.size = new Vector2(12f, 1f);
            groundRenderer.sortingOrder = 0;
            ground.transform.position = new Vector3(startX + 3f, -0.5f, 0f);
        }

        private static GameObject PlaceChunk(string name, float x)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Environment/{name}.prefab");

            if (prefab == null)
            {
                Debug.LogError($"[ArtPreview] Missing chunk prefab {name}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(x, 0f, 0f);
            return instance;
        }

        /// <summary>
        /// Fills a chunk's slots the way LevelGenerator would at runtime, so the
        /// preview shows real spacing rather than empty ground.
        /// </summary>
        private static void PopulateChunk(GameObject chunkObject)
        {
            if (chunkObject == null)
            {
                return;
            }

            var chunk = chunkObject.GetComponent<Chunk>();

            if (chunk == null)
            {
                return;
            }

            string[] obstacleNames = { "Obstacle_Spikes", "Obstacle_Saw", "Obstacle_Crate" };
            IReadOnlyList<Transform> obstacleSlots = chunk.ObstacleSlots;

            for (int i = 0; obstacleSlots != null && i < obstacleSlots.Count; i++)
            {
                if (obstacleSlots[i] == null || i % 2 == 1)
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"Assets/Prefabs/Obstacles/{obstacleNames[i % obstacleNames.Length]}.prefab");

                if (prefab == null)
                {
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = obstacleSlots[i].position;
            }

            var collectiblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Collectibles/Collectible.prefab");

            string[] dataNames = { "CoinBronze", "CoinSilver", "CoinGold", "GemBlue", "Star" };
            IReadOnlyList<Transform> collectibleSlots = chunk.CollectibleSlots;

            for (int i = 0; collectiblePrefab != null && collectibleSlots != null && i < collectibleSlots.Count; i++)
            {
                if (collectibleSlots[i] == null)
                {
                    continue;
                }

                var data = AssetDatabase.LoadAssetAtPath<CollectibleData>(
                    $"Assets/ScriptableObjects/Collectibles/{dataNames[i % dataNames.Length]}.asset");

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(collectiblePrefab);
                instance.transform.position = collectibleSlots[i].position;
                instance.GetComponent<Collectible>()?.Configure(data);
            }
        }

        private static void PlacePlayer(float x)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");

            if (prefab == null)
            {
                Debug.LogError("[ArtPreview] Missing player prefab.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var capsule = instance.GetComponent<CapsuleCollider2D>();

            // Rest the capsule exactly on ground level so any gap between the
            // collider and the drawn feet is obvious in the render.
            float halfHeight = capsule != null ? capsule.size.y * 0.5f : 0.75f;
            instance.transform.position = new Vector3(x, halfHeight, 0f);

            Debug.Log($"[ArtPreview] Player capsule size={capsule?.size}, resting centre y={halfHeight}");
        }

        private static void Shoot(string name, Vector2 centre, float orthographicSize)
        {
            var cameraObject = new GameObject("PreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.45f, 0.75f, 0.95f);
            camera.transform.position = new Vector3(centre.x, centre.y, -10f);

            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2
            };

            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;

            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;

            string path = Path.Combine(OutputFolder, name + ".png");
            File.WriteAllBytes(path, image.EncodeToPNG());

            Object.DestroyImmediate(image);
            Object.DestroyImmediate(cameraObject);
            target.Release();
            Object.DestroyImmediate(target);

            Debug.Log($"[ArtPreview] Wrote {path}");
        }
    }
}
