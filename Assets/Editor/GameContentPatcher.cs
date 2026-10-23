using FootballRunner.Collectibles;
using FootballRunner.Gameplay;
using FootballRunner.Player;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FootballRunner.EditorTools
{
    /// <summary>
    /// Applies gameplay and UI fixes to the existing prefabs and scenes without
    /// wiping them and regenerating from scratch.
    /// </summary>
    internal static class GameContentPatcher
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string MaterialPath = "Assets/Physics/PlayerNoFriction.physicsMaterial2D";

        [MenuItem("Tools/Football Runner/6 - Apply Gameplay Patch")]
        public static void Apply()
        {
            PhysicsMaterial2D material = LoadOrCreateMaterial();
            PatchPlayer(material);
            PatchCollectibleScores();
            PatchChunkSeams();
            PatchGameScene();
            PatchMainMenuScene();
            AndroidSetup.Configure();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[GameContentPatcher] Applied gameplay patch.");
        }

        private static PhysicsMaterial2D LoadOrCreateMaterial()
        {
            BuildUtils.EnsureFolder("Assets/Physics");
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);

            if (material == null)
            {
                material = new PhysicsMaterial2D("PlayerNoFriction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.friction = 0f;
                material.bounciness = 0f;
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static void PatchPlayer(PhysicsMaterial2D material)
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            if (player == null)
            {
                Debug.LogError($"[GameContentPatcher] Missing {PlayerPrefabPath}");
                return;
            }

            var body = player.GetComponent<Rigidbody2D>();
            var capsule = player.GetComponent<CapsuleCollider2D>();
            var controller = player.GetComponent<PlayerController>();

            if (body != null)
            {
                body.sharedMaterial = material;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            }

            if (capsule != null)
            {
                capsule.sharedMaterial = material;
                capsule.size = new Vector2(0.72f, 1.5f);
            }

            if (controller != null)
            {
                new SerializedProps(controller).Num("maxJumps", 1).Apply();

                var so = new SerializedObject(controller);
                SerializedProperty size = so.FindProperty("groundCheckSize");

                if (size != null)
                {
                    size.vector2Value = new Vector2(0.48f, 0.14f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Transform visual = player.transform.Find("Visual_Rukajja");

            if (visual != null)
            {
                Vector3 planted = visual.localPosition;
                planted.y = -0.93f;
                visual.localPosition = planted;
            }

            EditorUtility.SetDirty(player);
            PrefabUtility.SavePrefabAsset(player);
        }

        private static void PatchCollectibleScores()
        {
            const string bronzePath = "Assets/ScriptableObjects/Collectibles/CoinBronze.asset";
            var bronze = AssetDatabase.LoadAssetAtPath<CollectibleData>(bronzePath);

            if (bronze == null)
            {
                return;
            }

            bronze.scorePoints = 10;
            bronze.healAmount = 0;
            EditorUtility.SetDirty(bronze);
        }

        private static void PatchChunkSeams()
        {
            string[] paths =
            {
                "Assets/Prefabs/Environment/Chunk_Flat.prefab",
                "Assets/Prefabs/Environment/Chunk_Gap.prefab",
                "Assets/Prefabs/Environment/Chunk_Platform.prefab"
            };

            foreach (string path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                {
                    continue;
                }

                foreach (BoxCollider2D box in prefab.GetComponentsInChildren<BoxCollider2D>(true))
                {
                    if (box.gameObject.name != "Ground")
                    {
                        continue;
                    }

                    Vector2 size = box.size;

                    if (size.x >= 7f && size.x < 20.05f)
                    {
                        size.x += 0.12f;
                    }
                    else if (Mathf.Approximately(size.x, 20.12f) || size.x > 20.05f)
                    {
                        // Already patched.
                    }
                    else if (size.x < 7f)
                    {
                        size.x += 0.12f;
                    }

                    box.size = size;
                    box.edgeRadius = 0.02f;
                    EditorUtility.SetDirty(box);
                }

                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        private static void PatchGameScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);

            foreach (LevelGenerator generator in UnityEngine.Object.FindObjectsByType<LevelGenerator>(FindObjectsSortMode.None))
            {
                new SerializedProps(generator)
                    .Num("minSecondsBetweenObstacles", 1.1f)
                    .Num("safeStartChunks", 3)
                    .Num("spawnAheadDistance", 80f)
                    .Num("restChunksAfterHard", 1)
                    .Apply();
            }

            foreach (DifficultyManager difficulty in UnityEngine.Object.FindObjectsByType<DifficultyManager>(FindObjectsSortMode.None))
            {
                new SerializedProps(difficulty)
                    .Num("startSpeed", 6.5f)
                    .Num("maxSpeed", 13f)
                    .Num("secondsToMaxSpeed", 180f)
                    .Num("startObstacleChance", 0.18f)
                    .Num("maxObstacleChance", 0.5f)
                    .Num("secondsToMaxObstacleChance", 140f)
                    .Num("warmUpSeconds", 24f)
                    .Apply();
            }

            EnsureSafeArea(GameObject.Find("Canvas"), new[] { "PausePanel", "GameOverPanel" });
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void PatchMainMenuScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            {
                if (text.text != null && text.text.IndexOf("FOOTBALL", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    text.text = "ENDLESS RUNNER";
                    EditorUtility.SetDirty(text);
                }
            }

            EnsureSafeArea(GameObject.Find("Canvas"), new[] { "SettingsPanel", "Scrim", "HowToPlayPanel" });
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Moves HUD/menu widgets into a SafeArea child so notches do not cover
        /// score and buttons. Full-screen dim panels stay on the canvas root.
        /// </summary>
        private static void EnsureSafeArea(GameObject canvasObject, string[] keepOnRoot)
        {
            if (canvasObject == null)
            {
                return;
            }

            Transform canvas = canvasObject.transform;
            Transform safe = canvas.Find("SafeArea");

            if (safe == null)
            {
                var go = new GameObject("SafeArea", typeof(RectTransform));
                go.transform.SetParent(canvas, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                go.AddComponent<UI.SafeAreaFitter>();
                safe = go.transform;
            }
            else if (safe.GetComponent<UI.SafeAreaFitter>() == null)
            {
                safe.gameObject.AddComponent<UI.SafeAreaFitter>();
            }

            for (int i = canvas.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.GetChild(i);

                if (child == safe)
                {
                    continue;
                }

                bool keep = false;

                for (int k = 0; k < keepOnRoot.Length; k++)
                {
                    if (child.name == keepOnRoot[k])
                    {
                        keep = true;
                        break;
                    }
                }

                if (!keep)
                {
                    child.SetParent(safe, true);
                }
            }
        }
    }
}
