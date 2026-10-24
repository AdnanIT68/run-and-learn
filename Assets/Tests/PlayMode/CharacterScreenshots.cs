using System.Collections;
using System.IO;
using System.Linq;
using FootballRunner.Learning;
using FootballRunner.Player;
using FootballRunner.SaveSystem;
using FootballRunner.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>
    /// Photographs the menu, the player chooser and a few kids running, for checking
    /// the team by eye. Explicit; run with graphics:
    /// -testFilter FootballRunner.Tests.CharacterScreenshots. Output: Store/tour/Players_*.png
    /// </summary>
    [Explicit("Visual check of the players; run on demand with graphics enabled.")]
    [Timeout(300000)]
    public class CharacterScreenshots
    {
        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            FootballRunner.UI.DailyUi.AutoOpen = false;
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
        public IEnumerator PhotographThePlayers()
        {
            SaveData data = SaveManager.Data;
            data.unlockedItems.Clear();
            data.totalCoins = 75;
            data.wordTutorialDone = true;
            LearningProgress.SelectPack("World1");
            CharacterRoster.Select("amina");

            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("Players_Menu");

            // Settings carry the picture credits; check they fit.
            GameObject settings = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(go => go.name == "SettingsPanel" && go.scene.IsValid());
            if (settings != null)
            {
                settings.SetActive(true);
                settings.transform.SetAsLastSibling();
                yield return Capture("Menu_Settings");
                settings.SetActive(false);
            }

            // TODAY with one task half done.
            FootballRunner.Learning.DailyRewards.Report(FootballRunner.Learning.DailyRewards.Kind.CatchWords, 3);
            DailyUi daily = Object.FindFirstObjectByType<DailyUi>();
            daily.Open();
            yield return null;
            yield return Capture("Menu_Today");
            daily.Close();

            // WORLDS with the garden's first two levels done and level 3 chosen.
            data.worldResults.Clear();
            LearningProgress.RecordLevelComplete("World1", 0, 3);
            LearningProgress.RecordLevelComplete("World1", 1, 2);
            LearningProgress.SelectPack("World1");
            WordWorldsUi worlds = Object.FindFirstObjectByType<WordWorldsUi>();
            worlds.Open();
            yield return null;
            yield return Capture("Menu_Worlds");
            worlds.Close();

            CharacterChooserUi chooser = Object.FindFirstObjectByType<CharacterChooserUi>();
            chooser.Open();
            yield return null;
            yield return Capture("Players_Chooser");
            chooser.Close();

            foreach (string id in new[] { "emir", "tarik", "selma" })
            {
                data.unlockedItems.Add("character_" + id);
                CharacterRoster.Select(id);
                yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
                yield return new WaitForSecondsRealtime(1.3f);
                yield return Capture("Players_Run_" + id);
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

            foreach (Canvas canvas in overlays)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            string folder = Path.Combine(Application.dataPath, "..", "Store", "tour");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            Object.Destroy(image);
            target.Release();
        }
    }
}
