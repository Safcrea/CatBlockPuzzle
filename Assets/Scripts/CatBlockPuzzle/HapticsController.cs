using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CatBlockPuzzle
{
    internal sealed class HapticsController
    {
        private readonly MonoBehaviour runner;
        private Coroutine levelCompleteRoutine;

        public bool Enabled { get; set; } = true;

        public HapticsController(MonoBehaviour runner)
        {
            this.runner = runner;
        }

        public void PlaySnap()
        {
            if (!Enabled)
            {
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            CatBlockPuzzleHapticLight();
#elif UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroidOneShot(22, 90);
#else
            FallbackVibrate();
#endif
        }

        public void PlayWrongMove()
        {
            if (!Enabled)
            {
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            CatBlockPuzzleHapticHeavy();
#elif UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroidOneShot(85, 220);
#else
            FallbackVibrate();
#endif
        }

        public void PlayLevelComplete()
        {
            if (!Enabled)
            {
                return;
            }

            if (runner == null)
            {
                TriggerLevelCompleteStart();
                return;
            }

            if (levelCompleteRoutine != null)
            {
                runner.StopCoroutine(levelCompleteRoutine);
            }

            levelCompleteRoutine = runner.StartCoroutine(LevelCompleteSequence());
        }

        public void CancelLevelComplete()
        {
            if (runner != null && levelCompleteRoutine != null)
            {
                runner.StopCoroutine(levelCompleteRoutine);
            }

            levelCompleteRoutine = null;
        }

        private IEnumerator LevelCompleteSequence()
        {
            TriggerLevelCompleteStart();
            yield return new WaitForSecondsRealtime(0.08f);
            PlaySnap();
            yield return new WaitForSecondsRealtime(0.11f);
            PlaySnap();
            levelCompleteRoutine = null;
        }

        private void TriggerLevelCompleteStart()
        {
#if UNITY_IOS && !UNITY_EDITOR
            CatBlockPuzzleHapticSuccess();
#elif UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroidWaveform(new long[] { 0, 28, 55, 36, 70, 52 }, new int[] { 0, 120, 0, 170, 0, 230 });
#else
            FallbackVibrate();
#endif
        }

        private static void FallbackVibrate()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void CatBlockPuzzleHapticLight();

        [DllImport("__Internal")]
        private static extern void CatBlockPuzzleHapticHeavy();

        [DllImport("__Internal")]
        private static extern void CatBlockPuzzleHapticSuccess();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibrator;

        private static AndroidJavaObject Vibrator
        {
            get
            {
                if (vibrator != null)
                {
                    return vibrator;
                }

                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                return vibrator;
            }
        }

        private static int AndroidApiLevel
        {
            get
            {
                using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    return version.GetStatic<int>("SDK_INT");
                }
            }
        }

        private static void VibrateAndroidOneShot(long milliseconds, int amplitude)
        {
            try
            {
                AndroidJavaObject nativeVibrator = Vibrator;
                if (nativeVibrator == null)
                {
                    FallbackVibrate();
                    return;
                }

                if (AndroidApiLevel >= 26)
                {
                    using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                               "createOneShot",
                               milliseconds,
                               Mathf.Clamp(amplitude, 1, 255)))
                    {
                        nativeVibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    nativeVibrator.Call("vibrate", milliseconds);
                }
            }
            catch
            {
                FallbackVibrate();
            }
        }

        private static void VibrateAndroidWaveform(long[] timings, int[] amplitudes)
        {
            try
            {
                AndroidJavaObject nativeVibrator = Vibrator;
                if (nativeVibrator == null)
                {
                    FallbackVibrate();
                    return;
                }

                if (AndroidApiLevel >= 26)
                {
                    using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                               "createWaveform",
                               timings,
                               amplitudes,
                               -1))
                    {
                        nativeVibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    nativeVibrator.Call("vibrate", timings, -1);
                }
            }
            catch
            {
                FallbackVibrate();
            }
        }
#endif
    }
}
