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
    /// Output: Store/raw/&lt;en|hr|de&gt;/*.png, eight shots per language for 1.3:
    /// catch the word, Brbljavka's story, Arabic, mini-games, world map, outfits,
    /// parents corner and the main menu; CaptureAllTablet adds a 4:3 tablet set.
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

        /// <summary>Look check for the special hazards and the HUD: Store/raw/check/hazards_*.png.</summary>
        [UnityTest]
        public IEnumerator CaptureHazards()
        {
            SetProgress(LearningProgress.English, LearningProgress.Bosnian);

            foreach (string world in new[] { "World1", "World4", "World6", "World8" })
            {
                LearningProgress.SelectPack(world);
                LearningProgress.SelectLevel(0);
                yield return LoadScene("Game");
                yield return new WaitForSeconds(0.5f);

                Transform runner = FootballRunner.Player.PlayerController.Instance.transform;
                float feet = runner.GetComponent<Collider2D>().bounds.min.y;
                FootballRunner.Obstacles.SpecialHazards.CreateFlyers(new Vector3(runner.position.x + 4.5f, feet, 0f), world);
                FootballRunner.Obstacles.SpecialHazards.CreateBall(new Vector3(runner.position.x + 9.5f, feet, 0f));
                yield return null;
                yield return Capture("check/hazards_" + world);
            }
        }

        [UnityTest]
        public IEnumerator CaptureWorldExtras()
        {
            SetProgress(LearningProgress.English, LearningProgress.Bosnian);

            // Every world open, so each shot shows its own scenery.
            foreach (string pack in WordPack.PackOrder)
            {
                LearningProgress.RecordLevelComplete(pack, LearningProgress.LevelToOpenNextWorld - 1, 1);
            }

            foreach (string world in new[] { "World1", "World2", "World5", "World6", "World7", "World8" })
            {
                LearningProgress.SelectPack(world);
                LearningProgress.SelectLevel(0);
                yield return LoadScene("Game");
                yield return new WaitForSeconds(0.5f);

                Transform runner = FootballRunner.Player.PlayerController.Instance.transform;
                float feet = runner.GetComponent<Collider2D>().bounds.min.y;
                FootballRunner.Obstacles.SpecialHazards.CreateMud(new Vector3(runner.position.x + 3.5f, feet, 0f), world);
                FootballRunner.Obstacles.SpecialHazards.CreateProp(new Vector3(runner.position.x + 7f, feet, 0f), world);
                FootballRunner.Obstacles.SpecialHazards.CreateTrampoline(new Vector3(runner.position.x + 10.5f, feet, 0f));
                yield return null;
                yield return Capture("check/extras_" + world);
            }
        }

        /// <summary>
        /// Look check for Arabic: joined letters, harakat and the Latin line on the word
        /// card, in the album and in the parents corner. Output: Store/raw/check/arabic_*.png.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureArabic()
        {
            SetProgress(LearningProgress.Arabic, LearningProgress.Bosnian);
            foreach (string pack in WordPack.PackOrder)
            {
                LearningProgress.RecordLevelComplete(pack, LearningProgress.LevelToOpenNextWorld - 1, 1);
            }

            (string world, WordChallengeDirector.Kind kind)[] shots =
            {
                ("World1", WordChallengeDirector.Kind.Word),
                ("World2", WordChallengeDirector.Kind.Counting),
                ("World6", WordChallengeDirector.Kind.Sentence),
                ("World7", WordChallengeDirector.Kind.Letter)
            };

            foreach ((string world, WordChallengeDirector.Kind kind) in shots)
            {
                LearningProgress.ShowTransliteration = true;
                LearningProgress.SelectPack(world);
                LearningProgress.SelectLevel(0);
                yield return LoadScene("Game");
                yield return StartChallengeAndWait(world, counting: false, kind: kind);
                yield return Capture($"check/arabic_{world}_{kind}");
            }

            LearningProgress.ShowTransliteration = false;
            LearningProgress.SelectPack("World1");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World1", counting: false, kind: WordChallengeDirector.Kind.Word);
            yield return Capture("check/arabic_World1_no_latin");

            LearningProgress.ShowTransliteration = true;
            yield return LoadScene("MainMenu");
            yield return Capture("check/arabic_menu");

            var album = Object.FindFirstObjectByType<WordAlbumUi>();
            if (album != null)
            {
                album.Open();
                yield return null;
                yield return Capture("check/arabic_album");
                album.Close();
            }

            var corner = Object.FindFirstObjectByType<FootballRunner.UI.ParentsCornerUi>();
            corner.Open();
            corner.PressAnswer(corner.GateAnswer);
            yield return null;
            yield return Capture("check/arabic_parents");
            corner.Close();
        }

        /// <summary>Look check for the children's list in the parents corner: Store/raw/check/profiles.png.</summary>
        [UnityTest]
        public IEnumerator CaptureProfiles()
        {
            SetProgress(LearningProgress.English, LearningProgress.Bosnian);
            yield return LoadScene("MainMenu");
            var corner = Object.FindFirstObjectByType<FootballRunner.UI.ParentsCornerUi>();
            corner.Open();
            corner.PressAnswer(corner.GateAnswer);
            yield return null;
            yield return Capture("check/profiles_corner");

            corner.OpenProfiles();
            bool added = corner.Profiles.AddChild("Amina");
            yield return null;
            yield return Capture("check/profiles");

            if (added)
            {
                FootballRunner.SaveSystem.ProfileStore.Remove(
                    FootballRunner.SaveSystem.ProfileStore.Profiles[FootballRunner.SaveSystem.ProfileStore.Profiles.Count - 1].id);
            }

            corner.Profiles.Close();
            corner.Close();
        }

        /// <summary>Look check for the mini-games: Store/raw/check/mini_*.png.</summary>
        [UnityTest]
        public IEnumerator CaptureMiniGames()
        {
            SetProgress(LearningProgress.English, LearningProgress.Bosnian);
            LearningProgress.SelectPack("World1");
            yield return LoadScene("Game");
            Object.FindFirstObjectByType<LevelResultUi>().OpenMiniGames();
            MiniGameUi ui = MiniGameUi.Instance;
            yield return null;
            yield return Capture("check/mini_picker");

            ui.Play(MiniGames.Kind.Memory);
            ui.OpenCard(0);
            int pair = ui.Memory.Cards.FindIndex(c => c != ui.Memory.Cards[0] && c.Word == ui.Memory.Cards[0].Word);
            ui.OpenCard(pair);
            ui.OpenCard(pair == 1 ? 2 : 1);
            yield return null;
            yield return Capture("check/mini_memory");

            ui.Play(MiniGames.Kind.Hear);
            yield return null;
            yield return Capture("check/mini_hear");

            ui.Play(MiniGames.Kind.Build);
            ui.TapLetter(ui.Build.Tiles.IndexOf(ui.Build.Letters[0]));
            yield return null;
            yield return Capture("check/mini_build");

            // Half a letter traced, with the finger's orange strokes.
            ui.Play(MiniGames.Kind.Trace);
            bool[] mask = TracingLetters.MaskOf(ui.Trace.LetterId);
            int grid = TracingLetters.Grid;
            for (int y = 0; y < grid / 2; y++)
            {
                for (int x = 0; x < grid; x++)
                {
                    if (mask[y * grid + x])
                    {
                        ui.TraceAt(new Vector2((x + 0.5f) / grid, 1f - (y + 0.5f) / grid));
                        ui.Surface.Lift();
                    }
                }
            }

            yield return null;
            yield return Capture("check/mini_trace");
            ui.Close();
        }

        /// <summary>Look check for outfits on the runner and in the wardrobe: Store/raw/check/outfit_*.png.</summary>
        [UnityTest]
        public IEnumerator CaptureOutfits()
        {
            SetProgress(LearningProgress.English, LearningProgress.Bosnian);
            SaveManager.Data.totalCoins = 500;
            FootballRunner.Player.Outfits.Buy("cap");
            FootballRunner.Player.Outfits.Buy("sunglasses");
            FootballRunner.Player.Outfits.Buy("scarf");

            foreach (string kid in new[] { "emir", "rukajja", "hind" })
            {
                SaveManager.Data.unlockedItems.Add("character_" + kid);
                SaveManager.Data.selectedCharacterId = kid;
                LearningProgress.SelectPack("World1");
                yield return LoadScene("Game");
                yield return Capture("check/outfit_run_" + kid);
            }

            SaveManager.Data.selectedCharacterId = "amina";
            yield return LoadScene("MainMenu");
            var shop = FootballRunner.UI.OutfitShopUi.EnsureOnCanvas(
                Object.FindFirstObjectByType<FootballRunner.UI.MainMenuController>().transform);
            shop.Open();
            yield return null;
            yield return Capture("check/outfit_shop");
            shop.Close();
        }

        /// <summary>Look check for a diploma: Store/raw/check/diploma_*.png.</summary>
        [UnityTest]
        public IEnumerator CaptureDiploma()
        {
            SetProgress(LearningProgress.English, LearningProgress.Bosnian);
            for (int level = 0; level < LearningProgress.LevelsPerWorld; level++)
            {
                LearningProgress.RecordLevelComplete("World1", level, 3);
            }

            yield return LoadScene("MainMenu");
            Texture2D diploma = Diplomas.Render("World1", "Amina");
            string folder = Path.Combine(Application.dataPath, "..", "Store", "raw", "check");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "diploma_World1.png"), diploma.EncodeToPNG());
            Object.Destroy(diploma);
        }

        /// <summary>Look check for the worksheets and their screen: Store/raw/check/sheet_*.png.</summary>
        [UnityTest]
        public IEnumerator CaptureWorksheets()
        {
            string folder = Path.Combine(Application.dataPath, "..", "Store", "raw", "check");
            Directory.CreateDirectory(folder);

            foreach ((string learn, string home) in new[]
                     {
                         (LearningProgress.English, LearningProgress.Bosnian),
                         (LearningProgress.Arabic, LearningProgress.Bosnian)
                     })
            {
                SetProgress(learn, home);
                yield return LoadScene("MainMenu");

                foreach (Worksheets.Kind kind in new[] { Worksheets.Kind.Letters, Worksheets.Kind.Match })
                {
                    Texture2D sheet = Worksheets.Render("World1", kind);
                    File.WriteAllBytes(Path.Combine(folder, $"sheet_{learn}_{kind}.png"), sheet.EncodeToPNG());
                    Object.Destroy(sheet);
                }
            }

            SetProgress(LearningProgress.English, LearningProgress.Bosnian);
            yield return LoadScene("MainMenu");
            var corner = Object.FindFirstObjectByType<FootballRunner.UI.ParentsCornerUi>();
            corner.Open();
            corner.PressAnswer(corner.GateAnswer);
            yield return null;
            yield return Capture("check/sheet_corner");
            corner.OpenDiplomas();
            corner.Diplomas.ShowMode(FootballRunner.UI.DiplomasUi.Mode.Worksheets);
            corner.Diplomas.Show("World2");
            yield return null;
            yield return Capture("check/sheet_screen");
            corner.Diplomas.Close();
            corner.Close();
        }

        /// <summary>Look check on a 4:3 tablet: Store/raw/check/tablet_*.png.</summary>
        [UnityTest]
        public IEnumerator CaptureTablet()
        {
            const int tablet = 1440;
            SetProgress(LearningProgress.English, LearningProgress.Bosnian);
            LearningProgress.SelectPack("World1");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World1", counting: false);
            yield return Capture("check/tablet_game", tablet);

            yield return LoadScene("MainMenu");
            yield return Capture("check/tablet_menu", tablet);

            WordWorldsUi worlds = Object.FindFirstObjectByType<WordWorldsUi>();
            worlds.Open();
            yield return null;
            yield return Capture("check/tablet_worlds", tablet);
            worlds.Close();

            var corner = Object.FindFirstObjectByType<FootballRunner.UI.ParentsCornerUi>();
            corner.Open();
            corner.PressAnswer(corner.GateAnswer);
            yield return null;
            yield return Capture("check/tablet_parents", tablet);
            corner.Close();
        }

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

            // 01: catching a word in the garden.
            LearningProgress.SelectPack("World1");
            LearningProgress.SelectLevel(0);
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World1", counting: false);
            yield return Capture(folder + "/01_catch_the_word");

            // 02: Pepi tells Brbljavka's story on the first run in a world (the farm).
            SetProgress(learn, home);
            SaveManager.Data.pepiWelcomed.RemoveAll(key => key.StartsWith("World2:"));
            LearningProgress.SelectPack("World2");
            LearningProgress.SelectLevel(0);
            yield return LoadScene("Game");
            yield return Capture(folder + "/02_story");

            // 03: Arabic, learned with Bosnian menus, with the Latin letters underneath.
            SetProgress(LearningProgress.Arabic, LearningProgress.Bosnian);
            LearningProgress.ShowTransliteration = true;
            LearningProgress.SelectPack("World1");
            yield return LoadScene("Game");
            yield return StartChallengeAndWait("World1", counting: false, kind: WordChallengeDirector.Kind.Word);
            yield return Capture(folder + "/03_arabic");

            // 04: tracing a letter in the mini-games.
            SetProgress(learn, home);
            yield return ShotMiniGames(folder + "/04_mini_games", Width);

            // 08: the main menu, then 05: the world map with all ten worlds.
            SetProgress(learn, home);
            yield return ShotMenuAndWorlds(folder + "/08_main_menu", folder + "/05_worlds", Width);

            // 06: the wardrobe.
            yield return ShotOutfits(folder + "/06_outfits", Width);

            // 07: the parents corner with three children, a diploma, a time limit and the report.
            SetProgress(learn, home);
            yield return ShotParents(folder + "/07_parents", Width);
        }

        /// <summary>
        /// Tablet shots (4:3) for the "tablet" screenshot slots, one set per store language:
        /// Store/raw/&lt;lang&gt;-tablet/*.png.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureAllTablet()
        {
            const int tablet = 1440;

            foreach ((string folder, string learn, string home) in Profiles)
            {
                string dir = folder + "-tablet";
                SetProgress(learn, home);
                LearningProgress.SelectPack("World1");
                LearningProgress.SelectLevel(0);
                yield return LoadScene("Game");
                yield return StartChallengeAndWait("World1", counting: false);
                yield return Capture(dir + "/01_catch_the_word", tablet);

                SetProgress(learn, home);
                yield return ShotMiniGames(dir + "/02_mini_games", tablet);

                SetProgress(learn, home);
                yield return ShotMenuAndWorlds(dir + "/06_main_menu", dir + "/03_worlds", tablet);
                yield return ShotOutfits(dir + "/04_outfits", tablet);

                SetProgress(learn, home);
                yield return ShotParents(dir + "/05_parents", tablet);
            }
        }

        private static IEnumerator ShotMiniGames(string name, int width)
        {
            LearningProgress.SelectPack("World1");
            yield return LoadScene("Game");
            Object.FindFirstObjectByType<LevelResultUi>().OpenMiniGames();
            MiniGameUi ui = MiniGameUi.Instance;
            // A letter that reads well in a small picture (the letters come mixed).
            var clear = new[] { "latin_A", "latin_B", "latin_K", "latin_M" };
            ui.Play(MiniGames.Kind.Trace);
            for (int tries = 0; tries < 30 && System.Array.IndexOf(clear, ui.Trace.LetterId) < 0; tries++)
            {
                ui.Play(MiniGames.Kind.Trace);
            }

            // The left half traced, as if the finger were halfway through.
            bool[] mask = TracingLetters.MaskOf(ui.Trace.LetterId);
            int grid = TracingLetters.Grid;
            for (int y = 0; y < grid; y++)
            {
                for (int x = 0; x < grid / 2; x++)
                {
                    if (mask[y * grid + x])
                    {
                        ui.TraceAt(new Vector2((x + 0.5f) / grid, 1f - (y + 0.5f) / grid));
                        ui.Surface.Lift();
                    }
                }
            }

            yield return null;
            yield return Capture(name, width);
            ui.Close();
        }

        private static IEnumerator ShotMenuAndWorlds(string menuName, string worldsName, int width)
        {
            yield return LoadScene("MainMenu");
            yield return Capture(menuName, width);

            WordWorldsUi worlds = Object.FindFirstObjectByType<WordWorldsUi>();
            worlds.Open();
            yield return null;
            yield return Capture(worldsName, width);
            worlds.Close();
        }

        private static IEnumerator ShotOutfits(string name, int width)
        {
            SaveManager.Data.totalCoins = 230;
            FootballRunner.Player.Outfits.Buy("crown");
            FootballRunner.Player.Outfits.Buy("sunglasses");
            FootballRunner.Player.Outfits.Buy("scarf");
            SaveManager.Data.selectedCharacterId = "amina";

            var shop = FootballRunner.UI.OutfitShopUi.EnsureOnCanvas(
                Object.FindFirstObjectByType<FootballRunner.UI.MainMenuController>().transform);
            shop.Open();
            yield return null;
            yield return Capture(name, width);
            shop.Close();
        }

        private static IEnumerator ShotParents(string name, int width)
        {
            // A finished world (so a diploma), a daily limit, this week's time and the hardest words.
            LearningProgress.RecordLevelComplete("World1", 3, 3);
            LearningProgress.RecordLevelComplete("World1", 4, 2);
            TimeLimit.Minutes = 20;
            PlayTime.Add(12f * 60f);
            LearningProgress.RecordMiss("lemon");
            LearningProgress.RecordMiss("lemon");
            LearningProgress.RecordMiss("cucumber");

            var added = new System.Collections.Generic.List<string>();
            foreach (string child in new[] { "Emir", "Hana" })
            {
                FootballRunner.SaveSystem.ChildProfile profile = FootballRunner.SaveSystem.ProfileStore.Add(child);
                if (profile != null)
                {
                    added.Add(profile.id);
                }
            }

            string nameBefore = FootballRunner.SaveSystem.ProfileStore.Active.name;
            FootballRunner.SaveSystem.ProfileStore.Rename(FootballRunner.SaveSystem.ProfileStore.ActiveId, "Amina");

            try
            {
                yield return LoadScene("MainMenu");
                var corner = Object.FindFirstObjectByType<FootballRunner.UI.ParentsCornerUi>();
                corner.Open();
                corner.PressAnswer(corner.GateAnswer);
                yield return null;
                yield return Capture(name, width);
                corner.Close();
            }
            finally
            {
                foreach (string id in added)
                {
                    FootballRunner.SaveSystem.ProfileStore.Remove(id);
                }

                FootballRunner.SaveSystem.ProfileStore.Rename(FootballRunner.SaveSystem.ProfileStore.ActiveId, nameBefore);
                TimeLimit.Minutes = 0;
            }
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
            data.stickerPages.Clear();
            data.playDays.Clear();
            data.dailyLimitMinutes = 0;
            data.easyMode = false;
            data.outfitHead = data.outfitEyes = data.outfitNeck = "";

            // Pepi has welcomed the child everywhere, so no story card covers the other shots.
            data.pepiWelcomed.Clear();
            foreach (string pack in WordPack.PackOrder)
            {
                data.pepiWelcomed.Add(pack + ":" + learn);
            }
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
            camera.GetComponent<Gameplay.CameraFollow>()?.FitTo(camera.aspect);
            camera.GetComponent<Gameplay.CameraFollow>()?.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
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

            // On a phone an overlay canvas is always drawn last; keep it above coins, gems and
            // the clouds' own sorting layer here too, or a cloud can hide the score.
            int topLayer = SortingLayer.layers[SortingLayer.layers.Length - 1].id;
            var layersBefore = new System.Collections.Generic.Dictionary<Canvas, int>();
            foreach (Canvas canvas in overlays)
            {
                // The shot's shape decides the layout, as a phone or tablet screen would.
                FootballRunner.UI.ScreenFit.FitCanvas(canvas.GetComponent<UnityEngine.UI.CanvasScaler>(), (float)width / Height);
                layersBefore[canvas] = canvas.sortingLayerID;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                canvas.sortingLayerID = topLayer;
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
                canvas.sortingLayerID = layersBefore[canvas];
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
