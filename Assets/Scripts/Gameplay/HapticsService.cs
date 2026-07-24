using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    internal static class HapticsService
    {
        private const string VibrationKey = "settings.vibration";
        private static float nextTapAt;
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject androidVibrator;
        private static AndroidJavaClass vibrationEffectClass;
        private static int androidSdk = -1;
        private static bool androidInitialized;
#endif

        public static void Tap()
        {
            if (!IsEnabled() || Time.unscaledTime < nextTapAt)
            {
                return;
            }

            nextTapAt = Time.unscaledTime + 0.055f;
            VibrateAndroid(14L, 34);
        }

        public static void Upgrade()
        {
            if (!IsEnabled())
            {
                return;
            }

            VibrateAndroid(28L, 58);
        }

        public static void StageChange()
        {
            if (!IsEnabled())
            {
                return;
            }

            VibrateAndroid(42L, 82);
        }

        private static bool IsEnabled()
        {
            return PlayerPrefs.GetInt(VibrationKey, 1) == 1 &&
                   (Application.isMobilePlatform || Application.isEditor);
        }

        private static void VibrateAndroid(long milliseconds, int amplitude)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                EnsureAndroidVibrator();
                if (androidSdk >= 26 && vibrationEffectClass != null)
                {
                    using (AndroidJavaObject effect =
                           vibrationEffectClass.CallStatic<AndroidJavaObject>(
                               "createOneShot",
                               milliseconds,
                               Mathf.Clamp(amplitude, 1, 255)))
                    {
                        androidVibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    androidVibrator.Call("vibrate", milliseconds);
                }
            }
            catch
            {
                Handheld.Vibrate();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void EnsureAndroidVibrator()
        {
            if (androidInitialized && androidVibrator != null)
            {
                return;
            }

            using (AndroidJavaClass unityPlayer =
                   new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity =
                   unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass version =
                   new AndroidJavaClass("android.os.Build$VERSION"))
            {
                androidVibrator =
                    activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                androidSdk = version.GetStatic<int>("SDK_INT");
            }
            if (androidSdk >= 26)
            {
                vibrationEffectClass =
                    new AndroidJavaClass("android.os.VibrationEffect");
            }

            androidInitialized = true;
        }
#endif
    }
}
