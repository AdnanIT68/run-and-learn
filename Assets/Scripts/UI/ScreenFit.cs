using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FootballRunner.UI
{
    /// <summary>
    /// Fits the game to tablets (screens squarer than 16:9, such as 4:3 or 16:10) without
    /// changing anything on phones. Canvases switch to "expand", so the whole 1920x1080
    /// layout always fits and the spare room goes above and below instead of cutting the
    /// sides. The camera zooms out a little and keeps the runner at the same share of the
    /// screen as on a phone, so the child still sees almost as far ahead.
    /// </summary>
    public static class ScreenFit
    {
        public const float PhoneAspect = 16f / 9f;

        /// <summary>Screens squarer than this are treated as tablets.</summary>
        public const float TabletBelow = 1.7f;

        /// <summary>The most the camera zooms out on a tablet.</summary>
        public const float MaxZoomOut = 1.2f;

        public static bool IsTablet(float aspect)
        {
            return aspect < TabletBelow;
        }

        public static float ScreenAspect => Screen.height > 0 ? (float)Screen.width / Screen.height : PhoneAspect;

        /// <summary>How much larger the camera's view is on this screen (1 on phones).</summary>
        public static float ZoomOut(float aspect)
        {
            return IsTablet(aspect) ? Mathf.Clamp(PhoneAspect / Mathf.Max(0.5f, aspect), 1f, MaxZoomOut) : 1f;
        }

        /// <summary>
        /// The camera's lead ahead of the runner on this screen: the same share of the view
        /// width as <paramref name="phoneOffset"/> gives on a 16:9 phone. Phones keep it as is.
        /// </summary>
        public static float RunnerOffset(float baseOrtho, float phoneOffset, float aspect)
        {
            if (!IsTablet(aspect))
            {
                return phoneOffset;
            }

            float phoneHalf = baseOrtho * PhoneAspect;
            float share = (phoneHalf - phoneOffset) / (2f * phoneHalf);
            float half = baseOrtho * ZoomOut(aspect) * aspect;
            return half * (1f - 2f * share);
        }

        public static void FitCanvas(CanvasScaler scaler, float aspect)
        {
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                return;
            }

            scaler.screenMatchMode = IsTablet(aspect)
                ? CanvasScaler.ScreenMatchMode.Expand
                : CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        public static void FitAllCanvases(float aspect)
        {
            foreach (CanvasScaler scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
            {
                FitCanvas(scaler, aspect);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded += (scene, mode) => FitAllCanvases(ScreenAspect);
        }
    }
}
