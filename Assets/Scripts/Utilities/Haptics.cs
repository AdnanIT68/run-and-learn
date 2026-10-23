using FootballRunner.SaveSystem;
using UnityEngine;

namespace FootballRunner.Utilities
{
    /// <summary>
    /// Short phone vibrations for moments that matter: a caught word, a hit, a
    /// full goal row. Handheld.Vibrate is one long buzz, so Android's Vibrator
    /// service is called directly for short, soft taps. Does nothing in the Editor.
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled
        {
            get => SaveManager.Data.vibrationEnabled;
            set
            {
                SaveManager.Data.vibrationEnabled = value;
                SaveManager.Save();
            }
        }

        /// <summary>A caught word or a double jump.</summary>
        public static void Light() => Vibrate(18, 70);

        /// <summary>A completed goal row.</summary>
        public static void Medium() => Vibrate(40, 140);

        /// <summary>Taking damage.</summary>
        public static void Strong() => Vibrate(70, 230);

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static int _sdkLevel;
        private static bool _unavailable;

        private static void Vibrate(long milliseconds, int amplitude)
        {
            if (!Enabled || _unavailable)
            {
                return;
            }

            try
            {
                if (_vibrator == null)
                {
                    AndroidJavaObject activity = UnityEngine.Android.AndroidApplication.currentActivity;
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                    {
                        _sdkLevel = version.GetStatic<int>("SDK_INT");
                    }
                }

                if (_vibrator == null || !_vibrator.Call<bool>("hasVibrator"))
                {
                    _unavailable = true;
                    return;
                }

                if (_sdkLevel >= 26)
                {
                    using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = effects.CallStatic<AndroidJavaObject>(
                               "createOneShot", milliseconds, Mathf.Clamp(amplitude, 1, 255)))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", milliseconds);
                }
            }
            catch (System.Exception e)
            {
                // Some devices block the service. One long buzz is better than none,
                // and this call is also what makes Unity add the VIBRATE permission.
                Debug.LogWarning($"[Haptics] Vibrator unavailable, using Handheld.Vibrate. {e.Message}");
                _unavailable = true;
                Handheld.Vibrate();
            }
        }
#else
        private static void Vibrate(long milliseconds, int amplitude)
        {
        }
#endif
    }
}
