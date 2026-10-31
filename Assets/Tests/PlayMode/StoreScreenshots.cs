using System.Collections;
using System.IO;
using System.Linq;
using FootballRunner.Adventure;
using FootballRunner.Learning;
using FootballRunner.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FootballRunner.Tests
{
    /// <summary>
    /// Captures raw 1920x1080 frames for the Google Play listing. Explicit, so it
    /// never runs with the normal suite. Run in batch mode WITH graphics:
    /// Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode
    ///   -testFilter FootballRunner.Tests.StoreScreenshots
    /// Output: Store/raw/&lt;en|hr|de&gt;/*.png
    /// </summary>
    [Explicit("Store screenshots; run on demand with graphics enabled.")]
    [Timeout(180000)]
    public class StoreScreenshots
    {
        private const int Width = 1920;
        private const int Height = 1080;

        private string _saveBefore;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            _saveBefore = JsonUtility.ToJson(SaveManager.Data);
            FootballRunner.UI.DailyUi.AutoOpen = false;
            Time.timeScale = 1f;
            AdventureSession.Clear();
            yield return null;
        }

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            JsonUtility.FromJsonOverwrite(_saveBefore, SaveManager.Data);
            SaveManager.Save();
        }

        /// <summary>
        /// One set of eight shots per store language, each in the way that audience plays:
        /// en = learning Bosnian with English menus, hr (Bosnian listing) = learning English
        /// with Bosnian menus, de = learning Bosnian with German menus.
        /// </summary>
        private static readonly (string folder, string learn, string home)[] Profiles =
        {
            ("en", LearningProgress.Bosnian, LearningProgress.English),
            ("hr", LearningProgress.English, LearningProgress.Bosnian),
            ("de", LearningProgress.Bosnian, LearningProgress.German)
        };

        [UnityTest]
        public IEnumerator CaptureAll()
        {
            foreach ((string folder, string learn, string home) in Profiles)
            {
                yield return CaptureProfile(folder, learn, home);
            }
        }

        private static IEnumerator CaptureProfile(string folder, string learn, string home)
        {
            SetProgress(learn, home);

            // Catching a word in the garden.
            LearningProgress.SelectPack("World1");
            LearningProgress.SelectLevel(0);
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World1", counting: false);
            yield return Capture(folder + "/01_catch_the_word");

            // A full goal row ends the level: stars, the words and the next step.
            yield return FillGoal();
            float waitedForEnd = 0f;
            while (Core.GameManager.Instance.State != Core.GameState.LevelComplete && waitedForEnd < 5f)
            {
                waitedForEnd += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1.6f);
            yield return Capture(folder + "/06_level_complete");

            // Counting on the farm.
            SetProgress(learn, home);
            LearningProgress.SelectPack("World2");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World2", counting: true);
            yield return Capture(folder + "/04_counting_farm");

            // Spoken commands at the bazaar.
            LearningProgress.SelectPack("World4");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World4", counting: false, command: true);
            yield return Capture(folder + "/05_command_bazaar");

            // The menu and its screens.
            SetProgress(learn, home);
            yield return LoadScene("MainMenu");
            yield return Capture(folder + "/08_main_menu");

            // The listing shows the whole team as it looks once bought, not rows of silhouettes.
            foreach (var kid in FootballRunner.Player.CharacterRoster.All)
            {
                if (!FootballRunner.Player.CharacterRoster.IsUnlocked(kid.Id))
                {
                    SaveManager.Data.unlockedItems.Add("character_" + kid.Id);
                }
            }

            var players = Object.FindFirstObjectByType<FootballRunner.UI.CharacterChooserUi>();
            players.Open();
            yield return null;
            yield return Capture(folder + "/02_players");
            players.Close();

            WordWorldsUi worlds = Object.FindFirstObjectByType<WordWorldsUi>();
            worlds.Open();
            yield return null;
            yield return Capture(folder + "/03_worlds");
            worlds.Close();

            DailyRewards.Report(DailyRewards.Kind.CatchWords, 5);
            DailyRewards.Report(DailyRewards.Kind.CollectCoins, 50);
            var today = Object.FindFirstObjectByType<FootballRunner.UI.DailyUi>();
            today.Open();
            yield return null;
            yield return Capture(folder + "/07_today");
            today.Close();
        }

        /// <summary>A child some way in: three worlds open, a few levels and words done, coins saved.</summary>
        private static void SetProgress(string learn, string home)
        {
            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.worldResults.Clear();
            data.wordTutorialDone = true;
            LearningProgress.SetLanguage(learn, home);

            Learn(WordPack.Load("World1"), 8);
            Learn(WordPack.Load("World2"), 6);
            Learn(WordPack.Load("World3"), 4);
            Learn(WordPack.Load("World4"), 3);

            int[] stars = { 3, 2, 3 };
            foreach (string pack in new[] { "World1", "World2", "World3" })
            {
                for (int level = 0; level < stars.Length; level++)
                {
                    LearningProgress.RecordLevelComplete(pack, level, stars[level]);
                }
            }

            LearningProgress.RecordLevelComplete("World4", 0, 2);

            data.selectedPack = "World1";
            data.selectedLevel = 0;
            data.totalCoins = 95;
            data.unlockedItems.Clear();
            data.unlockedItems.Add("character_harun");
            data.selectedCharacterId = "rukajja";
            data.dailyGiftDate = "";
            data.dailyGiftStep = 2;
            data.missionDate = "";
            data.missions.Clear();
        }

        private static void Learn(WordPack pack, int count)
        {
            for (int i = 0; i < count && i < pack.Words.Count; i++)
            {
                // A few repeats so the album shows a spread of stars.
                for (int r = 0; r <= i % 4; r++)
                {
                    LearningProgress.RecordCorrect(pack.Words[i].id);
                }
            }
        }

        private static IEnumerator LoadScene(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            // Let the runner land and the track build before anything is captured.
            yield return new WaitForSecondsRealtime(1.5f);
        }

        private static IEnumerator StartChallengeAndWait(string pack, bool counting, bool command = false,
            bool size = false, WordChallengeDirector.Kind? kind = null)
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;
            Assert.AreEqual(pack, director.Pack.Name);
            float waited = 0f;

            while (director.CurrentWord == null && waited < 5f)
            {
                if (kind.HasValue)
                {
                    director.ForceChallenge(kind);
                }
                else
                {
                    director.ForceChallenge(counting, command, size);
                }
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotNull(director.CurrentWord, "No challenge to photograph.");

            // Wait until the bubbles are well inside the frame, ahead of the runner.
            waited = 0f;
            while (waited < 6f)
            {
                float nearest = director.Bubbles.Where(b => b != null)
                    .Select(b => b.transform.position.x).DefaultIfEmpty(0f).Min();
                float playerX = Player.PlayerController.Instance.transform.position.x;

                if (nearest - playerX < 6.5f)
                {
                    break;
                }

                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static IEnumerator FillGoal()
        {
            WordChallengeDirector director = WordChallengeDirector.Instance;

            while (director.GoalCaught < director.GoalSize - 1)
            {
                if (director.CurrentWord == null && !director.ForceChallenge())
                {
                    yield return null;
                    continue;
                }

                director.OnBubbleTouched(director.Bubbles.First(b => b.Word == director.CurrentWord));
                yield return null;
            }

            while (director.CurrentWord == null)
            {
                director.ForceChallenge();
                yield return null;
            }

            director.OnBubbleTouched(director.Bubbles.First(b => b.Word == director.CurrentWord));
        }

        /// <summary>
        /// Renders the main camera and every overlay canvas into one 1920x1080 image.
        /// Overlay canvases are switched to camera space for the shot, then restored.
        /// </summary>
        private static IEnumerator Capture(string name, int width = Width)
        {
            // WaitForEndOfFrame never resumes in batch mode; a frame step is enough here.
            yield return null;

            Camera camera = Camera.main;
            Assert.IsNotNull(camera, "No main camera to capture.");

            Canvas[] overlays = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();

            var target = new RenderTexture(width, Height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            camera.targetTexture = target;

            // Batch mode starts with a 4:3 screen, so backgrounds were fitted to that.
            // Refit them to the 16:9 shot; a phone does this itself at start-up.
            camera.aspect = (float)width / Height;
            foreach (Gameplay.ParallaxLayer layer in
                     Object.FindObjectsByType<Gameplay.ParallaxLayer>(FindObjectsSortMode.None))
            {
                layer.SendMessage("Configure", SendMessageOptions.DontRequireReceiver);
            }

            // The worlds' own backdrop layers were sized for that 4:3 start as well.
            foreach (SpriteRenderer layer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                         .Where(r => r.name == "WorldFar" || r.name == "WorldMid"))
            {
                layer.size = new Vector2(layer.size.x * 2f, layer.size.y);
            }

            foreach (Canvas canvas in overlays)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                // On a phone an overlay canvas is always drawn last; keep it above coins and gems here too.
                canvas.sortingOrder += 1000;
            }

            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = null;

            camera.targetTexture = previousTarget;
            camera.ResetAspect();

            foreach (Canvas canvas in overlays)
            {
                canvas.sortingOrder -= 1000;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            string path = Path.Combine(Application.dataPath, "..", "Store", "raw", name + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
            target.Release();
            Debug.Log($"[StoreScreenshots] Saved {name}.png");
        }
    }
}
