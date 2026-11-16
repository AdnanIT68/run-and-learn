using System;
using System.Collections.Generic;
using System.IO;
using FootballRunner.SaveSystem;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// A diploma for every world the child has finished (all five levels, in the language
    /// being learned): a picture with the child's name, the world, the stars and the date,
    /// drawn off screen and saved as a PNG that a parent can share or print.
    /// </summary>
    public static class Diplomas
    {
        public const int Width = 1600;
        public const int Height = 1131;

        /// <summary>Worlds finished in the current language, in play order.</summary>
        public static List<string> Earned()
        {
            var earned = new List<string>();

            foreach (string pack in WordPack.PackOrder)
            {
                if (LearningProgress.LevelsDone(pack) >= LearningProgress.LevelsPerWorld)
                {
                    earned.Add(pack);
                }
            }

            return earned;
        }

        public static string Folder => Path.Combine(Application.persistentDataPath, "Diplomas");

        /// <summary>The saved picture for a world, e.g. .../Diplomas/Amina_World1_bs.png.</summary>
        public static string FilePath(string pack, string childName)
        {
            string safe = string.IsNullOrEmpty(childName) ? "dijete" : childName;

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(c, '_');
            }

            return Path.Combine(Folder, $"{safe}_{pack}_{LearningProgress.Language}.png");
        }

        /// <summary>Draws the diploma off screen. The caller destroys the texture.</summary>
        public static Texture2D Render(string pack, string childName)
        {
            WordPack words = WordPack.Load(pack);
            int stars = LearningProgress.TotalStars(pack);
            string title = words != null ? words.Title(LearningProgress.HomeLanguage) : pack;
            string date = DailyRewards.Clock().ToString("dd. MM. yyyy.");

            var root = new GameObject("DiplomaRender");
            root.transform.position = new Vector3(0f, -5000f, 0f);
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Texture2D result = null;

            try
            {
                var cameraGo = new GameObject("DiplomaCamera");
                cameraGo.transform.SetParent(root.transform, false);
                var camera = cameraGo.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(1f, 0.97f, 0.88f, 1f);
                camera.cullingMask = 1 << 5;
                camera.targetTexture = target;
                camera.enabled = false;

                var canvasGo = new GameObject("DiplomaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                canvasGo.layer = 5;
                canvasGo.transform.SetParent(root.transform, false);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(Width, Height);
                scaler.matchWidthOrHeight = 0.5f;

                Compose(canvasGo.transform, childName, title, words, stars, date);
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                result = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                result.Apply();
                RenderTexture.active = previous;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }

            return result;
        }

        /// <summary>Draws and saves the diploma; returns the file, or null if it could not be written.</summary>
        public static string Save(string pack, string childName)
        {
            Texture2D picture = Render(pack, childName);

            try
            {
                Directory.CreateDirectory(Folder);
                string path = FilePath(pack, childName);
                File.WriteAllBytes(path, picture.EncodeToPNG());
                return path;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Diplomas] Could not save the diploma. {e.Message}");
                return null;
            }
            finally
            {
                UnityEngine.Object.Destroy(picture);
            }
        }

        /// <summary>
        /// Opens Android's share sheet with the picture (print, messages, mail...). On
        /// Android 10 and later the picture goes through the shared Pictures folder, which
        /// needs no permission. Returns false where sharing is not possible (the editor,
        /// older Android); the picture stays saved in the game's folder.
        /// </summary>
        public static bool Share(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                if (version.GetStatic<int>("SDK_INT") < 29)
                {
                    return false;
                }

                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject resolver = activity.Call<AndroidJavaObject>("getContentResolver");
                using var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media");
                string uriText = media.CallStatic<string>("insertImage", resolver, path, Path.GetFileNameWithoutExtension(path),
                    "Run and Learn");

                if (string.IsNullOrEmpty(uriText))
                {
                    return false;
                }

                using var uriClass = new AndroidJavaClass("android.net.Uri");
                using AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", uriText);
                using var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND");
                intent.Call<AndroidJavaObject>("setType", "image/png");
                intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri);
                intent.Call<AndroidJavaObject>("addFlags", 1);
                using var chooserClass = new AndroidJavaClass("android.content.Intent");
                using AndroidJavaObject chooser = chooserClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Run and Learn");
                activity.Call("startActivity", chooser);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Diplomas] Could not share the diploma. {e.Message}");
                return false;
            }
#else
            return false;
#endif
        }

        private static void Compose(Transform canvas, string childName, string title, WordPack words, int stars, string date)
        {
            Image frame = Panel(canvas, Vector2.zero, new Vector2(Width - 60f, Height - 60f), new Color(0.98f, 0.78f, 0.25f, 1f));
            Panel(frame.transform, Vector2.zero, new Vector2(Width - 110f, Height - 110f), new Color(1f, 0.98f, 0.92f, 1f));

            Label(canvas, "DIPLOMA", new Vector2(0f, 400f), new Vector2(1200f, 160f), 140f, new Color(0.95f, 0.6f, 0.1f, 1f));
            Label(canvas, "Run and Learn", new Vector2(0f, 290f), new Vector2(1000f, 70f), 56f, new Color(0.2f, 0.45f, 0.75f, 1f));
            Label(canvas, LearningProgress.Ui("za", "for", "für"), new Vector2(0f, 200f), new Vector2(600f, 60f), 46f,
                new Color(0.4f, 0.44f, 0.5f, 1f));
            Label(canvas, childName ?? "", new Vector2(0f, 100f), new Vector2(1200f, 150f),
                130f, new Color(0.16f, 0.2f, 0.3f, 1f));
            Label(canvas, LearningProgress.Ui("za završen svijet", "for finishing the world", "für die geschaffte Welt"),
                new Vector2(0f, -20f), new Vector2(1100f, 60f), 46f, new Color(0.4f, 0.44f, 0.5f, 1f));
            Label(canvas, title, new Vector2(0f, -110f), new Vector2(1200f, 110f), 90f, new Color(0.27f, 0.6f, 0.3f, 1f));
            Label(canvas, LearningProgress.Ui($"Zvjezdice: {stars} / 15", $"Stars: {stars} / 15", $"Sterne: {stars} / 15"),
                new Vector2(0f, -220f), new Vector2(900f, 70f), 52f, new Color(0.95f, 0.6f, 0.1f, 1f));
            Label(canvas, date, new Vector2(0f, -320f), new Vector2(700f, 60f), 42f, new Color(0.4f, 0.44f, 0.5f, 1f));

            Picture(canvas, Resources.Load<Sprite>("Menu/rukajja_wave"), new Vector2(-560f, -180f), new Vector2(300f, 420f));
            Picture(canvas, WordPack.Icon("parrot"), new Vector2(580f, -220f), new Vector2(220f, 220f));
            Sprite cover = words != null ? words.Picture(WordPack.CoverWordId(words.Name)) : null;
            Picture(canvas, cover, new Vector2(-580f, 330f), new Vector2(180f, 180f));
            Picture(canvas, WordPack.Icon("star"), new Vector2(580f, 330f), new Vector2(180f, 180f));
        }

        private static Image Panel(Transform parent, Vector2 position, Vector2 size, Color colour)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.6f;
            image.color = colour;
            return image;
        }

        private static void Picture(Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            if (sprite == null)
            {
                return;
            }

            var go = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
        }

        private static void Label(Transform parent, string content, Vector2 position, Vector2 size, float fontSize, Color colour)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            GameFonts.UseDisplay(text);
            text.text = content;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.4f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = colour;
        }
    }
}
