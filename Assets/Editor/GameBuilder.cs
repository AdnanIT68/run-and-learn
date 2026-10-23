using System.Collections.Generic;
using System.IO;
using FootballRunner.Audio;
using FootballRunner.Collectibles;
using FootballRunner.Core;
using FootballRunner.Gameplay;
using FootballRunner.Obstacles;
using FootballRunner.Player;
using FootballRunner.UI;
using FootballRunner.Utilities;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Generates the prefabs, data assets and both scenes from the imported art.
    /// Re-runnable: it clears what it previously created before rebuilding.
    /// </summary>
    internal static class GameBuilder
    {
        private const string DataFolder = "Assets/ScriptableObjects/Collectibles";
        private const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        private const float GroundTopY = 0f;
        private const float ChunkLength = 20f;
        private const float GroundThickness = 3f;

        /// <summary>Height of the runner in world units. One ground tile is 1 unit.</summary>
        private const float PlayerHeight = 1.5f;

        // ------------------------------------------------------------------
        // Step 1
        // ------------------------------------------------------------------

        [MenuItem("Tools/Football Runner/1 - Prepare Project")]
        public static void Prepare()
        {
            BuildUtils.AddLayer("Ground");
            ImportTmpEssentials();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameBuilder] Prepare complete.");
        }

        /// <summary>
        /// AssetDatabase.ImportPackage is asynchronous, which means a batch-mode run
        /// quits before the fonts land. The internal immediate variant blocks, so we
        /// reach for it via reflection and fall back to the public call if it moves.
        /// </summary>
        private static void ImportTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0)
            {
                Debug.Log("[GameBuilder] TMP essentials already present.");
                return;
            }

            System.Reflection.MethodInfo immediate = typeof(AssetDatabase).GetMethod(
                "ImportPackageImmediately",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (immediate != null)
            {
                Debug.Log("[GameBuilder] Importing TMP Essential Resources (immediate).");
                immediate.Invoke(null, new object[] { TmpPackage });
            }
            else
            {
                Debug.Log("[GameBuilder] Importing TMP Essential Resources (async fallback).");
                AssetDatabase.ImportPackage(TmpPackage, false);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log(AssetDatabase.FindAssets("t:TMP_Settings").Length > 0
                ? "[GameBuilder] TMP essentials imported."
                : "[GameBuilder] TMP essentials STILL MISSING after import attempt.");
        }

        // ------------------------------------------------------------------
        // Art import
        // ------------------------------------------------------------------

        [MenuItem("Tools/Football Runner/1b - Reimport Art")]
        public static void ReimportArt()
        {
            const ImportAssetOptions options =
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate;

            AssetDatabase.ImportAsset("Assets/Art", options);
            AssetDatabase.ImportAsset("Assets/Audio", options);
            AssetDatabase.Refresh();

            Debug.Log("[GameBuilder] Art and audio reimported.");
        }

        /// <summary>
        /// Art copied into the project before the import rules were compiled will
        /// have Unity's defaults, which use the wrong pixels-per-unit and a tight
        /// sprite mesh that breaks tiling. Detect that and correct it once.
        /// </summary>
        private static bool EnsureArtImported()
        {
            if (!ArtLibrary.IsPresent())
            {
                Debug.LogError("[GameBuilder] Art is missing from Assets/Art. " +
                               "Import the Kenney pack before building.");
                return false;
            }

            Sprite probe = ArtLibrary.Tile("terrain_grass_block_top");

            if (probe != null && !Mathf.Approximately(probe.pixelsPerUnit, ArtImportSettings.PixelsPerUnit))
            {
                Debug.Log($"[GameBuilder] Art was imported at {probe.pixelsPerUnit} pixels per unit. Reimporting.");
                ReimportArt();
            }

            return true;
        }

        // ------------------------------------------------------------------
        // Step 2
        // ------------------------------------------------------------------

        [MenuItem("Tools/Football Runner/2 - Build Game")]
        public static void BuildAll()
        {
            if (!EnsureArtImported())
            {
                return;
            }

            // A scene must be open for temporary objects while prefabs are saved.
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);

            string[] dataPaths = CreateCollectibleData();
            AssetDatabase.SaveAssets();

            GameObject playerPrefab = CreatePlayerPrefab();
            GameObject collectiblePrefab = CreateCollectiblePrefab();
            GameObject floatingTextPrefab = CreateFloatingTextPrefab();
            GameObject persistentPrefab = CreatePersistentSystemsPrefab();

            GameObject[] obstaclePrefabs =
            {
                CreateObstaclePrefab("Obstacle_Spikes", ArtLibrary.Tile("spikes"), 0.5f, 0.80f),
                CreateObstaclePrefab("Obstacle_Saw", ArtLibrary.Tile("saw"), 1.0f, 0.75f),
                CreateObstaclePrefab("Obstacle_Crate", ArtLibrary.Tile("block_planks"), 1.0f, 0.90f)
            };

            GameObject chunkFlat = CreateFlatChunk();
            GameObject chunkGap = CreateGapChunk();
            GameObject chunkPlatform = CreatePlatformChunk();

            GameObject[] chunkPrefabs = { chunkFlat, chunkGap, chunkPlatform };

            BuildGameScene(playerPrefab, chunkFlat, chunkPrefabs, obstaclePrefabs, collectiblePrefab,
                dataPaths, floatingTextPrefab);

            BuildMainMenuScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GameBuilder] Build complete. Persistent systems prefab: {persistentPrefab.name}");
        }

        // ------------------------------------------------------------------
        // Data assets
        // ------------------------------------------------------------------

        private static string[] CreateCollectibleData()
        {
            BuildUtils.EnsureFolder(DataFolder);

            // Weights are relative, not percentages: bronze coins are roughly five
            // times as common as stars.
            var definitions = new (string name, string sprite, int score, int coin, int heal, float scale, float weight)[]
            {
                // Scales are generous: the drawn coin fills only about half of its
                // 64px canvas, so a scale of 1 would read as a speck on a phone.
                ("CoinBronze", "coin_bronze", 10, 1, 0, 1.15f, 34f),
                ("CoinSilver", "coin_silver", 10, 2, 0, 1.15f, 16f),
                ("CoinGold", "coin_gold", 20, 5, 0, 1.20f, 8f),
                ("GemBlue", "gem_blue", 15, 0, 0, 1.25f, 12f),
                ("GemGreen", "gem_green", 15, 0, 0, 1.25f, 10f),
                ("GemRed", "gem_red", 25, 0, 0, 1.30f, 7f),
                ("GemYellow", "gem_yellow", 25, 0, 0, 1.30f, 7f),
                ("Star", "star", 50, 0, 0, 1.45f, 4f),

                // Rare, and still worth taking at full health thanks to the score.
                ("Heart", "heart", 25, 0, 1, 1.15f, 3f)
            };

            // Anything created by an earlier build that is no longer in the list
            // would otherwise linger as an orphaned asset.
            var keep = new HashSet<string>();

            foreach (var definition in definitions)
            {
                keep.Add($"{DataFolder}/{definition.name}.asset");
            }

            foreach (string guid in AssetDatabase.FindAssets("t:CollectibleData", new[] { DataFolder }))
            {
                string existing = AssetDatabase.GUIDToAssetPath(guid);

                if (!keep.Contains(existing))
                {
                    AssetDatabase.DeleteAsset(existing);
                }
            }

            var results = new List<string>();

            foreach ((string name, string sprite, int score, int coin, int heal, float scale, float weight) in definitions)
            {
                string path = $"{DataFolder}/{name}.asset";
                var data = AssetDatabase.LoadAssetAtPath<CollectibleData>(path);

                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<CollectibleData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                data.displayName = name;
                data.sprite = ArtLibrary.Tile(sprite);
                data.tint = Color.white;
                data.scale = scale;
                data.scorePoints = score;
                data.coinValue = coin;
                data.healAmount = heal;
                data.spawnWeight = weight;

                EditorUtility.SetDirty(data);
                results.Add(path);
            }

            AssetDatabase.SaveAssets();
            return results.ToArray();
        }

        // ------------------------------------------------------------------
        // Sprite helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// A sprite renderer that repeats its texture to fill the given size rather
        /// than stretching it. Used for every stretch of ground.
        /// </summary>
        private static GameObject CreateTiledSprite(string name, Transform parent, Sprite sprite,
            Vector3 localPosition, Vector2 size, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingOrder = sortingOrder;

            return go;
        }

        /// <summary>
        /// Adds a sprite scaled to <paramref name="targetHeight"/> and positioned so
        /// its drawn pixels sit centred horizontally with their base on the parent
        /// origin. Kenney sprites are padded inside square canvases, so using the
        /// canvas directly would leave objects floating above the ground.
        /// </summary>
        private static SpriteRenderer AddGroundedVisual(Transform parent, Sprite sprite, float targetHeight,
            int sortingOrder, out float drawnWidth)
        {
            Bounds art = ArtLibrary.OpaqueBounds(sprite);
            float scale = art.size.y > 0f ? targetHeight / art.size.y : 1f;
            drawnWidth = art.size.x * scale;

            var go = new GameObject("Visual");
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.transform.localPosition = new Vector3(-art.center.x * scale, -art.min.y * scale, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            return renderer;
        }

        // ------------------------------------------------------------------
        // Prefabs
        // ------------------------------------------------------------------

        private static GameObject SavePrefab(GameObject root, string path)
        {
            BuildUtils.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static PhysicsMaterial2D LoadOrCreatePlayerMaterial()
        {
            const string path = "Assets/Physics/PlayerNoFriction.physicsMaterial2D";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);

            if (material == null)
            {
                BuildUtils.EnsureFolder("Assets/Physics");
                material = new PhysicsMaterial2D("PlayerNoFriction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.friction = 0f;
                material.bounciness = 0f;
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static GameObject CreatePlayerPrefab()
        {
            Sprite idle = ArtLibrary.Character("character_green_idle");
            Sprite walkA = ArtLibrary.Character("character_green_walk_a");
            Sprite walkB = ArtLibrary.Character("character_green_walk_b");
            Sprite jumpFrame = ArtLibrary.Character("character_green_jump");
            Sprite hitFrame = ArtLibrary.Character("character_green_hit");

            var root = new GameObject("Player");

            Bounds art = ArtLibrary.OpaqueBounds(idle);
            float scale = art.size.y > 0f ? PlayerHeight / art.size.y : 1f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(scale, scale, 1f);

            // Feet on the bottom of the collider, art centred on the collider axis.
            visual.transform.localPosition = new Vector3(
                -art.center.x * scale,
                -PlayerHeight * 0.5f - art.min.y * scale,
                0f);

            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = idle;
            renderer.sortingOrder = 10;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 4f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            body.sharedMaterial = LoadOrCreatePlayerMaterial();

            var capsule = root.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(0.72f, PlayerHeight);
            capsule.sharedMaterial = body.sharedMaterial;

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(root.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, -PlayerHeight * 0.5f - 0.02f, 0f);

            var controller = root.AddComponent<PlayerController>();
            var health = root.AddComponent<PlayerHealth>();
            var animator = root.AddComponent<PlayerAnimator>();

            int groundMask = 1 << LayerMask.NameToLayer("Ground");

            new SerializedProps(controller)
                .Obj("body", body)
                .Obj("groundCheck", groundCheck.transform)
                .Mask("groundLayers", groundMask)
                .Num("maxJumps", 1)
                .Apply();

            new SerializedProps(health)
                .Obj("spriteRenderer", renderer)
                .Apply();

            new SerializedProps(animator)
                .Obj("spriteRenderer", renderer)
                .Obj("idleSprite", idle)
                .Array("runSprites", new Object[] { walkA, walkB })
                .Obj("jumpSprite", jumpFrame)
                .Obj("hitSprite", hitFrame)
                .Apply();

            return SavePrefab(root, "Assets/Prefabs/Player/Player.prefab");
        }

        private static GameObject CreateCollectiblePrefab()
        {
            var root = new GameObject("Collectible");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtLibrary.Tile("coin_bronze");
            renderer.sortingOrder = 6;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;

            var collectible = root.AddComponent<Collectible>();
            root.AddComponent<PooledObject>();

            new SerializedProps(collectible).Obj("spriteRenderer", renderer).Apply();

            return SavePrefab(root, "Assets/Prefabs/Collectibles/Collectible.prefab");
        }

        private static GameObject CreateObstaclePrefab(string name, Sprite sprite, float height, float colliderInset)
        {
            var root = new GameObject(name);

            AddGroundedVisual(root.transform, sprite, height, 5, out float width);

            var box = root.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(width * colliderInset, height * colliderInset);
            box.offset = new Vector2(0f, height * 0.5f);

            var obstacle = root.AddComponent<Obstacle>();
            root.AddComponent<PooledObject>();

            new SerializedProps(obstacle).Num("damage", 1).Apply();

            return SavePrefab(root, $"Assets/Prefabs/Obstacles/{name}.prefab");
        }

        private static GameObject CreateFloatingTextPrefab()
        {
            var root = new GameObject("FloatingText", typeof(RectTransform));

            var label = root.AddComponent<TextMeshPro>();
            label.text = "+10";
            label.fontSize = 6f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = BuildUtils.Accent;

            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4f, 1.5f);

            var meshRenderer = root.GetComponent<MeshRenderer>();

            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 30;
            }

            var floating = root.AddComponent<FloatingText>();
            root.AddComponent<PooledObject>();

            new SerializedProps(floating).Obj("label", label).Apply();

            return SavePrefab(root, "Assets/Prefabs/UI/FloatingText.prefab");
        }

        private static GameObject CreatePersistentSystemsPrefab()
        {
            var root = new GameObject("PersistentSystems");

            // The game ships without music by design, but the source is kept so a
            // track can be dropped in later without touching any code.
            var musicObject = new GameObject("MusicSource");
            musicObject.transform.SetParent(root.transform, false);
            var musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.5f;

            var sfxObject = new GameObject("SfxSource");
            sfxObject.transform.SetParent(root.transform, false);
            var sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.8f;

            var audioManager = root.AddComponent<AudioManager>();

            new SerializedProps(audioManager)
                .Obj("musicSource", musicSource)
                .Obj("sfxSource", sfxSource)
                .Obj("jumpClip", ArtLibrary.Sfx("sfx_jump"))
                .Obj("collectClip", ArtLibrary.Sfx("sfx_gem"))
                .Obj("coinClip", ArtLibrary.Sfx("sfx_coin"))
                .Obj("hurtClip", ArtLibrary.Sfx("sfx_hurt"))
                .Obj("powerUpClip", ArtLibrary.Sfx("sfx_magic"))
                .Obj("buttonClip", ArtLibrary.Sfx("sfx_select"))
                .Obj("gameOverClip", ArtLibrary.Sfx("sfx_disappear"))
                .Apply();

            return SavePrefab(root, "Assets/Resources/PersistentSystems.prefab");
        }

        // ------------------------------------------------------------------
        // Chunks
        // ------------------------------------------------------------------

        private static GameObject BeginChunk(string name, out Chunk chunk)
        {
            var root = new GameObject(name);
            chunk = root.AddComponent<Chunk>();
            root.AddComponent<PooledObject>();
            return root;
        }

        /// <summary>
        /// A run of ground: one row of grass-topped tiles with dirt beneath, and a
        /// single box collider spanning the whole piece rather than one per tile.
        /// </summary>
        private static void AddGroundPiece(Transform parent, float centreX, float width, float topY, float thickness)
        {
            const float SurfaceHeight = 1f;

            var root = new GameObject("Ground");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(centreX, 0f, 0f);
            root.layer = LayerMask.NameToLayer("Ground");

            CreateTiledSprite("Surface", root.transform, ArtLibrary.Tile("terrain_grass_block_top"),
                new Vector3(0f, topY - SurfaceHeight * 0.5f, 0f), new Vector2(width, SurfaceHeight), 0);

            float fillHeight = thickness - SurfaceHeight;

            if (fillHeight > 0f)
            {
                CreateTiledSprite("Fill", root.transform, ArtLibrary.Tile("terrain_grass_block_center"),
                    new Vector3(0f, topY - SurfaceHeight - fillHeight * 0.5f, 0f),
                    new Vector2(width, fillHeight), -1);
            }

            var box = root.AddComponent<BoxCollider2D>();
            // Extra width hides the vertical lip where two chunks meet, which is
            // what catches a capsule and makes the runner look stuck.
            box.size = new Vector2(width + 0.12f, thickness);
            box.offset = new Vector2(0f, topY - thickness * 0.5f);
            box.edgeRadius = 0.02f;
        }

        /// <summary>A floating one-tile-thick ledge whose walkable surface is at <paramref name="topY"/>.</summary>
        private static void AddPlatform(Transform parent, float centreX, float width, float topY)
        {
            var root = new GameObject("Platform");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(centreX, topY, 0f);
            root.layer = LayerMask.NameToLayer("Ground");

            float middleWidth = Mathf.Max(0f, width - 2f);

            if (middleWidth > 0f)
            {
                CreateTiledSprite("Middle", root.transform, ArtLibrary.Tile("terrain_grass_horizontal_middle"),
                    new Vector3(0f, -0.5f, 0f), new Vector2(middleWidth, 1f), 1);
            }

            float capX = middleWidth * 0.5f + 0.5f;

            BuildUtils.CreateSpriteObject("Left", root.transform, ArtLibrary.Tile("terrain_grass_horizontal_left"),
                Color.white, new Vector3(-capX, -0.5f, 0f), Vector2.one, 1);

            BuildUtils.CreateSpriteObject("Right", root.transform, ArtLibrary.Tile("terrain_grass_horizontal_right"),
                Color.white, new Vector3(capX, -0.5f, 0f), Vector2.one, 1);

            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 1f);
            box.offset = new Vector2(0f, -0.5f);
        }

        private static GameObject FinishChunk(GameObject root, Chunk chunk, Vector2[] obstacleSlots,
            Vector2[] collectibleSlots, string path)
        {
            Transform[] obstacles = BuildUtils.CreateSlots("ObstacleSlots", root.transform, obstacleSlots);
            Transform[] collectibles = BuildUtils.CreateSlots("CollectibleSlots", root.transform, collectibleSlots);

            new SerializedProps(chunk)
                .Num("length", ChunkLength)
                .Array("obstacleSlots", obstacles)
                .Array("collectibleSlots", collectibles)
                .Apply();

            return SavePrefab(root, path);
        }

        private static GameObject CreateFlatChunk()
        {
            GameObject root = BeginChunk("Chunk_Flat", out Chunk chunk);
            AddGroundPiece(root.transform, ChunkLength * 0.5f, ChunkLength, GroundTopY, GroundThickness);

            var obstacles = new[]
            {
                new Vector2(5f, GroundTopY), new Vector2(9f, GroundTopY),
                new Vector2(13f, GroundTopY), new Vector2(17f, GroundTopY)
            };

            var collectibles = new[]
            {
                new Vector2(3f, 1.4f), new Vector2(7f, 1.4f), new Vector2(11f, 2.6f),
                new Vector2(15f, 1.4f), new Vector2(19f, 1.4f)
            };

            return FinishChunk(root, chunk, obstacles, collectibles, "Assets/Prefabs/Environment/Chunk_Flat.prefab");
        }

        private static GameObject CreateGapChunk()
        {
            GameObject root = BeginChunk("Chunk_Gap", out Chunk chunk);

            // Two platforms with a jumpable hole between x=8 and x=12.
            AddGroundPiece(root.transform, 4f, 8f, GroundTopY, GroundThickness);
            AddGroundPiece(root.transform, 16f, 8f, GroundTopY, GroundThickness);

            var obstacles = new[]
            {
                new Vector2(4f, GroundTopY), new Vector2(17f, GroundTopY)
            };

            var collectibles = new[]
            {
                new Vector2(2f, 1.4f), new Vector2(9f, 2.2f), new Vector2(10f, 2.8f),
                new Vector2(11f, 2.2f), new Vector2(19f, 1.4f)
            };

            return FinishChunk(root, chunk, obstacles, collectibles, "Assets/Prefabs/Environment/Chunk_Gap.prefab");
        }

        private static GameObject CreatePlatformChunk()
        {
            GameObject root = BeginChunk("Chunk_Platform", out Chunk chunk);
            AddGroundPiece(root.transform, ChunkLength * 0.5f, ChunkLength, GroundTopY, GroundThickness);
            AddPlatform(root.transform, 10f, 6f, 2.9f);

            var obstacles = new[]
            {
                new Vector2(4f, GroundTopY), new Vector2(17f, GroundTopY)
            };

            var collectibles = new[]
            {
                new Vector2(2f, 1.4f), new Vector2(8.5f, 3.8f), new Vector2(10f, 3.8f),
                new Vector2(11.5f, 3.8f), new Vector2(19f, 1.4f)
            };

            return FinishChunk(root, chunk, obstacles, collectibles, "Assets/Prefabs/Environment/Chunk_Platform.prefab");
        }

        // ------------------------------------------------------------------
        // Scenes
        // ------------------------------------------------------------------

        private static void ClearGeneratedRoots(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Main Camera" || root.name == "Global Light 2D")
                {
                    continue;
                }

                Object.DestroyImmediate(root);
            }
        }

        private static Camera ConfigureCamera(Scene scene, bool addFollow, out CameraFollow follow)
        {
            Camera camera = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                camera = root.GetComponentInChildren<Camera>();

                if (camera != null)
                {
                    break;
                }
            }

            follow = null;

            if (camera == null)
            {
                Debug.LogError("[GameBuilder] No camera found in scene.");
                return null;
            }

            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BuildUtils.Sky;
            camera.transform.position = new Vector3(6f, 2.5f, -10f);

            var existing = camera.GetComponent<CameraFollow>();

            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            if (addFollow)
            {
                follow = camera.gameObject.AddComponent<CameraFollow>();
            }

            return camera;
        }

        /// <summary>
        /// Scrolling backdrop. Exactly one layer: every Kenney background paints its
        /// own opaque sky, so stacking them just hides everything but the front one.
        /// </summary>
        private static void CreateParallaxBackground()
        {
            var root = new GameObject("Background");

            AddParallaxLayer(root.transform, "Hills", ArtLibrary.Background("background_color_hills"), 0.20f, -40);
        }

        private static void AddParallaxLayer(Transform parent, string name, Sprite sprite, float factor, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.sortingOrder = order;

            // ParallaxLayer recalculates this from the camera on Awake. Seeding a
            // sensible value keeps the Scene view readable before entering play mode.
            renderer.size = new Vector2(40f, 12f);

            var layer = go.AddComponent<ParallaxLayer>();

            new SerializedProps(layer)
                .Num("parallaxFactor", factor)
                .Apply();
        }

        private static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");

            if (actions != null)
            {
                module.actionsAsset = actions;
            }
        }

        private static void BuildGameScene(GameObject playerPrefab, GameObject startChunk, GameObject[] chunkPrefabs,
            GameObject[] obstaclePrefabs, GameObject collectiblePrefab, string[] collectibleDataPaths,
            GameObject floatingTextPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);

            // Must be loaded *after* the scene opens. Opening a scene in Single mode
            // unloads assets nothing references, which turns any ScriptableObject
            // reference held only by this method into a null.
            var collectibleTypes = new Object[collectibleDataPaths.Length];

            for (int i = 0; i < collectibleDataPaths.Length; i++)
            {
                collectibleTypes[i] = AssetDatabase.LoadAssetAtPath<CollectibleData>(collectibleDataPaths[i]);

                if (collectibleTypes[i] == null)
                {
                    Debug.LogError($"[GameBuilder] Could not load {collectibleDataPaths[i]}");
                }
            }

            ClearGeneratedRoots(scene);
            ConfigureCamera(scene, true, out CameraFollow follow);
            CreateParallaxBackground();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 1.6f, 0f);

            if (follow != null)
            {
                follow.SetTarget(player.transform);
            }

            var systems = new GameObject("Systems");
            systems.AddComponent<PoolManager>();
            systems.AddComponent<GameManager>();
            systems.AddComponent<ScoreManager>();
            systems.AddComponent<DifficultyManager>();
            var floatingSpawner = systems.AddComponent<FloatingTextSpawner>();

            new SerializedProps(floatingSpawner).Obj("floatingTextPrefab", floatingTextPrefab).Apply();

            var generatorObject = new GameObject("LevelGenerator");
            var generator = generatorObject.AddComponent<LevelGenerator>();

            new SerializedProps(generator)
                .Obj("startChunkPrefab", startChunk)
                .Array("chunkPrefabs", chunkPrefabs)
                .Array("obstaclePrefabs", obstaclePrefabs)
                .Obj("collectiblePrefab", collectiblePrefab)
                .Array("collectibleTypes", collectibleTypes)
                .Obj("player", player.transform)
                .Apply();

            VerifyArrayAssigned(generator, "collectibleTypes");
            VerifyArrayAssigned(generator, "chunkPrefabs");
            VerifyArrayAssigned(generator, "obstaclePrefabs");

            Canvas canvas = CreateCanvas("Canvas");
            BuildHud(canvas.transform);
            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameBuilder] Game scene built.");
        }

        /// <summary>Reads the field back after assignment and reports any null entries.</summary>
        private static void VerifyArrayAssigned(Object target, string field)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError($"[GameBuilder] Verify: no field '{field}'.");
                return;
            }

            int nulls = 0;

            for (int i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    nulls++;
                }
            }

            if (nulls > 0)
            {
                Debug.LogError($"[GameBuilder] Verify FAILED: '{field}' has {nulls}/{property.arraySize} null entries.");
            }
            else
            {
                Debug.Log($"[GameBuilder] Verify OK: '{field}' = {property.arraySize} entries.");
            }
        }

        /// <summary>
        /// A stretched child of the canvas that shrinks itself to the device safe
        /// area at runtime. HUD elements go inside it; full-screen dim overlays
        /// deliberately stay outside so they still cover the whole display.
        /// </summary>
        private static Transform CreateSafeArea(Transform canvas)
        {
            GameObject go = BuildUtils.CreateUIObject("SafeArea", canvas);
            BuildUtils.Place(go, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            go.AddComponent<SafeAreaFitter>();
            return go.transform;
        }

        private static void BuildHud(Transform canvasRoot)
        {
            Transform canvas = CreateSafeArea(canvasRoot);

            // --- Score, top left ---
            TextMeshProUGUI scoreCaption = BuildUtils.CreateText("ScoreCaption", canvas, "SCORE", 30f,
                TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.75f));
            BuildUtils.Place(scoreCaption.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(70f, -50f), new Vector2(300f, 36f));

            TextMeshProUGUI scoreValue = BuildUtils.CreateText("ScoreValue", canvas, "0", 72f,
                TextAlignmentOptions.Left, Color.white);
            BuildUtils.Place(scoreValue.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(70f, -84f), new Vector2(400f, 84f));

            // --- Coins, under the score ---
            GameObject coinIcon = BuildUtils.CreateUIObject("CoinIcon", canvas);
            var coinImage = coinIcon.AddComponent<Image>();
            coinImage.color = Color.white;
            coinImage.sprite = ArtLibrary.Tile("hud_coin");
            coinImage.raycastTarget = false;
            BuildUtils.Place(coinIcon, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(74f, -180f), new Vector2(48f, 48f));

            TextMeshProUGUI coinValue = BuildUtils.CreateText("CoinValue", canvas, "0", 44f,
                TextAlignmentOptions.Left, Color.white);
            BuildUtils.Place(coinValue.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(136f, -178f), new Vector2(300f, 50f));

            // --- Hearts, top centre ---
            GameObject heartRow = BuildUtils.CreateUIObject("Hearts", canvas);
            BuildUtils.Place(heartRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(320f, 70f));

            var layout = heartRow.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Sprite heartFull = ArtLibrary.Tile("hud_heart");
            Sprite heartEmpty = ArtLibrary.Tile("hud_heart_empty");

            var hearts = new Object[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject heart = BuildUtils.CreateUIObject($"Heart{i}", heartRow.transform);
                var image = heart.AddComponent<Image>();
                image.color = Color.white;
                image.sprite = heartFull;
                image.raycastTarget = false;

                var element = heart.AddComponent<LayoutElement>();
                element.preferredWidth = 62f;
                element.preferredHeight = 62f;

                hearts[i] = image;
            }

            // --- Pause button, top right ---
            Button pauseButton = BuildUtils.CreateButton("PauseButton", canvas, "II", new Vector2(110f, 110f),
                new Color(0f, 0f, 0f, 0.35f), 46f);
            BuildUtils.Place(pauseButton.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-60f, -50f), new Vector2(110f, 110f));

            // Controllers live on the canvas root, not the safe area child, so all
            // the UI scripts stay together on one object as before.
            var hud = canvasRoot.gameObject.AddComponent<HUDController>();

            new SerializedProps(hud)
                .Obj("scoreText", scoreValue)
                .Obj("coinText", coinValue)
                .Array("hearts", hearts)
                .Obj("heartFullSprite", heartFull)
                .Obj("heartEmptySprite", heartEmpty)
                .Obj("pauseButton", pauseButton)
                .Apply();

            // Deliberately parented to the canvas root: the dim overlay must cover
            // the notch area too, or it looks like a rendering bug.
            BuildPausePanel(canvasRoot);
            BuildGameOverPanel(canvasRoot);
        }

        private static GameObject CreateModalPanel(Transform canvas, string name, string title,
            out Transform box)
        {
            GameObject panel = BuildUtils.CreateUIObject(name, canvas);
            var dim = panel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            BuildUtils.Place(panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            GameObject boxObject = BuildUtils.CreateUIObject("Box", panel.transform);
            var boxImage = boxObject.AddComponent<Image>();
            boxImage.color = BuildUtils.PanelBackground;
            boxImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            boxImage.type = Image.Type.Sliced;
            BuildUtils.Place(boxObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 620f));

            TextMeshProUGUI titleText = BuildUtils.CreateText("Title", boxObject.transform, title, 64f,
                TextAlignmentOptions.Center, BuildUtils.Accent);
            BuildUtils.Place(titleText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(700f, 80f));

            box = boxObject.transform;
            return panel;
        }

        private static void BuildPausePanel(Transform canvas)
        {
            GameObject panel = CreateModalPanel(canvas, "PausePanel", "PAUSED", out Transform box);

            Button resume = BuildUtils.CreateButton("ResumeButton", box, "RESUME", new Vector2(460f, 100f),
                BuildUtils.ButtonGreen);
            BuildUtils.Place(resume.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(460f, 100f));

            Button restart = BuildUtils.CreateButton("RestartButton", box, "RESTART", new Vector2(460f, 100f),
                BuildUtils.ButtonBlue);
            BuildUtils.Place(restart.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(460f, 100f));

            Button menu = BuildUtils.CreateButton("MainMenuButton", box, "MAIN MENU", new Vector2(460f, 100f),
                BuildUtils.ButtonRed);
            BuildUtils.Place(menu.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(460f, 100f));

            var pauseMenu = canvas.gameObject.AddComponent<PauseMenu>();

            new SerializedProps(pauseMenu)
                .Obj("panel", panel)
                .Obj("resumeButton", resume)
                .Obj("restartButton", restart)
                .Obj("mainMenuButton", menu)
                .Apply();

            panel.SetActive(false);
        }

        private static void BuildGameOverPanel(Transform canvas)
        {
            GameObject panel = CreateModalPanel(canvas, "GameOverPanel", "GAME OVER", out Transform box);

            TextMeshProUGUI newBest = BuildUtils.CreateText("NewBest", box, "NEW BEST!", 34f,
                TextAlignmentOptions.Center, BuildUtils.ButtonGreen);
            BuildUtils.Place(newBest.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(500f, 44f));

            TextMeshProUGUI scoreValue = CreateStatRow(box, "Score", "SCORE", -180f, out _);
            TextMeshProUGUI bestValue = CreateStatRow(box, "Best", "BEST", -240f, out _);
            TextMeshProUGUI coinsValue = CreateStatRow(box, "Coins", "COINS", -300f, out _);

            Button restart = BuildUtils.CreateButton("RestartButton", box, "RESTART", new Vector2(460f, 100f),
                BuildUtils.ButtonGreen);
            BuildUtils.Place(restart.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 172f), new Vector2(460f, 100f));

            Button menu = BuildUtils.CreateButton("MainMenuButton", box, "MAIN MENU", new Vector2(460f, 100f),
                BuildUtils.ButtonBlue);
            BuildUtils.Place(menu.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(460f, 100f));

            var gameOver = canvas.gameObject.AddComponent<GameOverPanel>();

            new SerializedProps(gameOver)
                .Obj("panel", panel)
                .Obj("scoreText", scoreValue)
                .Obj("bestText", bestValue)
                .Obj("coinsText", coinsValue)
                .Obj("newBestLabel", newBest)
                .Obj("restartButton", restart)
                .Obj("mainMenuButton", menu)
                .Apply();

            panel.SetActive(false);
        }

        private static TextMeshProUGUI CreateStatRow(Transform parent, string name, string caption, float y,
            out TextMeshProUGUI captionText)
        {
            captionText = BuildUtils.CreateText($"{name}Caption", parent, caption, 36f,
                TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.75f));
            BuildUtils.Place(captionText.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 1f), new Vector2(-280f, y), new Vector2(300f, 48f));

            TextMeshProUGUI value = BuildUtils.CreateText($"{name}Value", parent, "0", 40f,
                TextAlignmentOptions.Right, Color.white);
            BuildUtils.Place(value.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(1f, 1f), new Vector2(280f, y), new Vector2(300f, 48f));

            return value;
        }

        private static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            ClearGeneratedRoots(scene);
            ConfigureCamera(scene, false, out _);
            CreateParallaxBackground();

            Canvas canvas = CreateCanvas("Canvas");
            Transform canvasRoot = canvas.transform;

            // A dark scrim over the scrolling art keeps the text readable. It stays
            // outside the safe area so it covers the entire screen.
            BuildUtils.CreatePanel("Scrim", canvasRoot, new Color(0f, 0f, 0f, 0.35f));

            Transform root = CreateSafeArea(canvasRoot);

            TextMeshProUGUI title = BuildUtils.CreateText("Title", root, "ENDLESS RUNNER", 110f,
                TextAlignmentOptions.Center, BuildUtils.Accent);
            BuildUtils.Place(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1500f, 140f));

            Button play = BuildUtils.CreateButton("PlayButton", root, "PLAY", new Vector2(520f, 130f),
                BuildUtils.ButtonGreen, 56f);
            BuildUtils.Place(play.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(520f, 130f));

            Button settings = BuildUtils.CreateButton("SettingsButton", root, "SETTINGS", new Vector2(520f, 100f),
                BuildUtils.ButtonBlue);
            BuildUtils.Place(settings.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(520f, 100f));

            Button quit = BuildUtils.CreateButton("QuitButton", root, "EXIT", new Vector2(520f, 100f),
                BuildUtils.ButtonRed);
            BuildUtils.Place(quit.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(520f, 100f));

            TextMeshProUGUI bestCaption = BuildUtils.CreateText("BestCaption", root, "BEST", 32f,
                TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.8f));
            BuildUtils.Place(bestCaption.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(80f, 130f), new Vector2(300f, 40f));

            TextMeshProUGUI bestValue = BuildUtils.CreateText("BestValue", root, "0", 56f,
                TextAlignmentOptions.Left, Color.white);
            BuildUtils.Place(bestValue.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(80f, 70f), new Vector2(400f, 64f));

            TextMeshProUGUI coinsCaption = BuildUtils.CreateText("CoinsCaption", root, "COINS", 32f,
                TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.8f));
            BuildUtils.Place(coinsCaption.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-80f, 130f), new Vector2(300f, 40f));

            TextMeshProUGUI coinsValue = BuildUtils.CreateText("CoinsValue", root, "0", 56f,
                TextAlignmentOptions.Right, BuildUtils.Accent);
            BuildUtils.Place(coinsValue.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-80f, 70f), new Vector2(400f, 64f));

            // --- Settings panel ---
            // Sound only: the game intentionally ships without music, so a music
            // toggle would control nothing.
            GameObject settingsPanel = CreateModalPanel(canvasRoot, "SettingsPanel", "SETTINGS", out Transform box);

            Toggle soundToggle = BuildUtils.CreateToggle("SoundToggle", box, "SOUND", new Vector2(420f, 60f));
            BuildUtils.Place(soundToggle.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(420f, 60f));

            Button close = BuildUtils.CreateButton("CloseButton", box, "CLOSE", new Vector2(420f, 100f),
                BuildUtils.ButtonGreen);
            BuildUtils.Place(close.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(420f, 100f));

            var controller = canvas.gameObject.AddComponent<MainMenuController>();

            new SerializedProps(controller)
                .Obj("playButton", play)
                .Obj("settingsButton", settings)
                .Obj("quitButton", quit)
                .Obj("highScoreText", bestValue)
                .Obj("totalCoinsText", coinsValue)
                .Obj("settingsPanel", settingsPanel)
                .Obj("closeSettingsButton", close)
                .Obj("soundToggle", soundToggle)
                .Apply();

            settingsPanel.SetActive(false);

            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameBuilder] Main menu scene built.");
        }
    }
}
