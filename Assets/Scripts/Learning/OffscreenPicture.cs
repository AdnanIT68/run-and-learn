using System;
using System.Collections.Generic;
using System.IO;
using FootballRunner.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FootballRunner.Learning
{
    /// <summary>
    /// Pictures drawn off screen with the game's UI pieces (diplomas, worksheets), saved
    /// as PNG files and shared through Android's share sheet (print, messages, mail).
    /// </summary>
    public static class OffscreenPicture
    {
        private const int UiLayer = 5;

        /// <summary>Draws what <paramref name="compose"/> builds on a canvas of this size. The caller destroys the texture.</summary>
        public static Texture2D Render(int width, int height, Color background, Action<Transform> compose)
        {
            var root = new GameObject("OffscreenRender");
            root.transform.position = new Vector3(0f, -5000f, 0f);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D result = null;

            try
            {
                var cameraGo = new GameObject("OffscreenCamera");
                cameraGo.transform.SetParent(root.transform, false);
                var camera = cameraGo.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = background;
                camera.cullingMask = 1 << UiLayer;
                camera.targetTexture = target;
                camera.enabled = false;

                var canvasGo = new GameObject("OffscreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                canvasGo.layer = UiLayer;
                canvasGo.transform.SetParent(root.transform, false);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(width, height);
                scaler.matchWidthOrHeight = 0.5f;

                compose(canvasGo.transform);
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                result = new Texture2D(width, height, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
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

        /// <summary>Writes the picture as a PNG and destroys it; returns the file, or null if it could not be written.</summary>
        public static string SavePng(Texture2D picture, string path)
        {
            try
            {
                return WritePng(picture, path);
            }
            finally
            {
                UnityEngine.Object.Destroy(picture);
            }
        }

        /// <summary>Writes the picture as a PNG and keeps it (e.g. the one on screen); returns the file, or null.</summary>
        public static string WritePng(Texture2D picture, string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, picture.EncodeToPNG());
                return path;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OffscreenPicture] Could not save {path}. {e.Message}");
                return null;
            }
        }

        /// <summary>A file-name-safe version of a child's name or other text.</summary>
        public static string SafeName(string text, string fallback)
        {
            string safe = string.IsNullOrEmpty(text) ? fallback : text;

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(c, '_');
            }

            return safe;
        }

        /// <summary>The gallery copy made for each file shared in this session.</summary>
        private static readonly Dictionary<string, string> SharedCopies = new Dictionary<string, string>();

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
                using var uriClass = new AndroidJavaClass("android.net.Uri");

                // Sharing the same picture again replaces the copy in the gallery instead of adding another one.
                if (SharedCopies.TryGetValue(path, out string earlier))
                {
                    try
                    {
                        using AndroidJavaObject old = uriClass.CallStatic<AndroidJavaObject>("parse", earlier);
                        resolver.Call<int>("delete", old, null, null);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[OffscreenPicture] Could not remove the earlier copy. {e.Message}");
                    }

                    SharedCopies.Remove(path);
                }

                using var media = new AndroidJavaClass("android.provider.MediaStore$Images$Media");
                string uriText = media.CallStatic<string>("insertImage", resolver, path, Path.GetFileNameWithoutExtension(path),
                    "Run and Learn");

                if (string.IsNullOrEmpty(uriText))
                {
                    return false;
                }

                SharedCopies[path] = uriText;
                using AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", uriText);
                using var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND");
                using (intent.Call<AndroidJavaObject>("setType", "image/png"))
                using (intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri))
                using (intent.Call<AndroidJavaObject>("addFlags", 1))
                {
                }

                using var chooserClass = new AndroidJavaClass("android.content.Intent");
                using AndroidJavaObject chooser = chooserClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Run and Learn");
                activity.Call("startActivity", chooser);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OffscreenPicture] Could not share {path}. {e.Message}");
                return false;
            }
#else
            return false;
#endif
        }

        // ------------------------------------------------------------ pieces

        public static Image Panel(Transform parent, Vector2 position, Vector2 size, Color colour, float roundness = 0.6f)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = RuntimeSprites.RoundedRect();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = roundness;
            image.color = colour;
            return image;
        }

        /// <summary>A plain rectangle or line.</summary>
        public static Image Bar(Transform parent, Vector2 position, Vector2 size, Color colour)
        {
            var go = new GameObject("Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = colour;
            return image;
        }

        public static Image Picture(Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            if (sprite == null)
            {
                return null;
            }

            var go = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            return image;
        }

        public static TMP_Text Label(Transform parent, string content, Vector2 position, Vector2 size, float fontSize,
            Color colour)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.layer = UiLayer;
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
            return text;
        }
    }
}
