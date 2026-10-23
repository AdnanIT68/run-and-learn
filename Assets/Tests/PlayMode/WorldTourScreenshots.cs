using System.Collections;
using System.IO;
using System.Linq;
using FootballRunner.Gameplay;
using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>
    /// Photographs a run in every world, with one dressed obstacle and a word
    /// challenge in view, for checking the scenery by eye. Explicit; run with
    /// graphics: -testFilter FootballRunner.Tests.WorldTourScreenshots
    /// Output: Store/tour/&lt;World&gt;.png
    /// </summary>
    [Explicit("Visual check of every world; run on demand with graphics enabled.")]
    [Timeout(300000)]
    public class WorldTourScreenshots
    {
        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            yield return null;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        [UnityTest]
        public IEnumerator PhotographEveryWorld()
        {
            SaveManager.Data.wordProgress.Clear();
            SaveManager.Data.worldResults.Clear();
            SaveManager.Data.wordTutorialDone = true;

            foreach (string world in WordPack.PackOrder)
            {
                WordPack pack = WordPack.Load(world);
                for (int i = 0; i < LearningProgress.WordsToUnlockNext; i++)
                {
                    LearningProgress.RecordCorrect(pack.Words[i].id);
                }
            }

            foreach (string world in WordPack.PackOrder)
            {
                Assert.IsTrue(LearningProgress.SelectPack(world), $"{world} could not be chosen.");
                yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(1.5f);

                // The warm-up keeps hazards away, so place one by hand to show its new look.
                Obstacles.Obstacle obstacle = Object.FindObjectsByType<Obstacles.Obstacle>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
                if (obstacle != null)
                {
                    Vector3 feet = PlayerController.Instance.transform.position;
                    obstacle.gameObject.SetActive(true);
                    obstacle.transform.position = new Vector3(feet.x + 14f, obstacle.transform.position.y, 0f);
                    obstacle.GetComponent<Collider2D>().enabled = false;
                    WorldScenery.Instance.SkinObstacle(obstacle.gameObject, obstacle.name);
                }

                WordChallengeDirector.Instance.ForceChallenge();
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Capture(world);
            }
        }

        private static IEnumerator Capture(string name)
        {
            yield return null;
            const int width = 2340;
            const int height = 1080;
            Camera camera = Camera.main;
            Canvas[] overlays = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.aspect = (float)width / height;

            foreach (Canvas canvas in overlays)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }

            // Backdrop layers were sized for batch mode's 4:3 screen; size them for the shot.
            foreach (SpriteRenderer layer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                         .Where(r => r.name == "WorldFar" || r.name == "WorldMid"))
            {
                layer.size = new Vector2(layer.size.x * 2f, layer.size.y);
            }

            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            camera.ResetAspect();

            string folder = Path.Combine(Application.dataPath, "..", "Store", "tour");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            Object.Destroy(image);
            target.Release();
        }
    }
}
