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
    /// Output: Store/raw/*.png
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

        [UnityTest]
        public IEnumerator CaptureAll()
        {
            SetProgress(gardenWords: 7, farmWords: 4, english: false);

            // 1. Catching a word in the garden.
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World1", counting: false);
            yield return Capture("02_catch_the_word");

            // 2. Goal row full: BRAVO and confetti.
            yield return FillGoal();
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Capture("04_bravo");

            // 2b. The level ends: stars, the five words and the next step.
            float waitedForEnd = 0f;
            while (Core.GameManager.Instance.State != Core.GameState.LevelComplete && waitedForEnd < 5f)
            {
                waitedForEnd += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1.6f);
            yield return Capture("10_level_complete");

            // 3. Counting on the farm.
            LearningProgress.SelectPack("World2");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World2", counting: true);
            yield return Capture("03_counting_farm");

            // 4. Spoken commands at the bazaar.
            SetProgress(gardenWords: 7, farmWords: 6, english: false, homeWords: 6);
            LearningProgress.SelectPack("World4");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World4", counting: false, command: true);
            yield return Capture("08_command_bazaar");

            // 5. Big or small by the river.
            SetProgress(gardenWords: 7, farmWords: 6, english: false, homeWords: 6, bazaarWords: 6);
            LearningProgress.SelectPack("World5");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World5", counting: false, size: true);
            yield return Capture("09_big_small_river");

            // 5b. Letters at school.
            SetProgress(gardenWords: 7, farmWords: 6, english: false, homeWords: 6, bazaarWords: 6);
            LearningProgress.RecordLevelComplete("World6", 2);
            LearningProgress.RecordLevelComplete("World5", 2);
            LearningProgress.SelectPack("World7");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World7", counting: false, kind: WordChallengeDirector.Kind.Letter);
            yield return Capture("11_letters_school");

            // 6. English learners see the same game in English.
            SetProgress(gardenWords: 7, farmWords: 4, english: true);
            LearningProgress.SelectPack("World1");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World1", counting: false);
            yield return Capture("06_learning_english");

            // 5. Main menu, album and world chooser.
            SetProgress(gardenWords: 7, farmWords: 4, english: false);
            yield return LoadScene("MainMenu");
            yield return Capture("01_main_menu");
            // Phones are wider than 16:9; check the layout holds there too (not a store shot).
            yield return Capture("check_main_menu_phone", 2340);

            WordAlbumUi album = Object.FindFirstObjectByType<WordAlbumUi>();
            album.Open();
            yield return null;
            yield return Capture("05_album");
            album.Close();

            WordWorldsUi worlds = Object.FindFirstObjectByType<WordWorldsUi>();
            worlds.Open();
            yield return null;
            yield return Capture("07_worlds");
        }

        private static void SetProgress(int gardenWords, int farmWords, bool english, int homeWords = 0,
            int bazaarWords = 0)
        {
            SaveData data = SaveManager.Data;
            data.wordProgress.Clear();
            data.worldResults.Clear();
            data.wordTutorialDone = true;
            data.learningLanguage = english ? LearningProgress.English : LearningProgress.Bosnian;

            Learn(WordPack.Load("World1"), gardenWords);
            Learn(WordPack.Load("World2"), farmWords);
            Learn(WordPack.Load("World3"), homeWords);
            Learn(WordPack.Load("World4"), bazaarWords);
            data.selectedPack = "World1";
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
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            string folder = Path.Combine(Application.dataPath, "..", "Store", "raw");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            Object.Destroy(image);
            target.Release();
            Debug.Log($"[StoreScreenshots] Saved {name}.png");
        }
    }
}
