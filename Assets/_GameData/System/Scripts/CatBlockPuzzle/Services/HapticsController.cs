using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>The gameplay moments that have their own haptic feel.</summary>
    internal enum HapticKind
    {
        /// <summary>A dragged cat snaps onto a new valid spot.</summary>
        Selection,
        /// <summary>Any UI button.</summary>
        UiTap,
        Pickup,
        Snap,
        Combo,
        Wrong,
        LockOpen,
        HeartLost,
        /// <summary>One of the last seconds on the timer.</summary>
        TimerTick,
        LevelFail,
        LevelComplete
    }

    /// <summary>
    /// Plays haptics for gameplay moments. On Android it uses the richest API the phone offers:
    /// composed primitives (API 30+, crisp on phones such as the Galaxy S22), then predefined
    /// click/tick effects (API 29+), then plain amplitude pulses. iOS uses UIKit feedback generators.
    /// All effects go out as game vibrations and follow the player's Haptics setting.
    /// </summary>
    internal sealed class HapticsController
    {
        private readonly MonoBehaviour runner;
        private Coroutine levelCompleteRoutine;
        private float lastSelectionTime = -1f;
        private float lastUiTapTime = -1f;

        /// <summary>The live controller, for static callers such as the shared button click.</summary>
        public static HapticsController Current { get; private set; }

        public bool Enabled { get; set; } = true;

        public HapticsController(MonoBehaviour runner)
        {
            this.runner = runner;
            Current = this;
        }

        // Kept for existing callers.
        public void PlaySnap() => Play(HapticKind.Snap);
        public void PlayPickup() => Play(HapticKind.Pickup);
        public void PlayWrongMove() => Play(HapticKind.Wrong);

        public void Play(HapticKind kind)
        {
            if (!Enabled) return;
            float now = Time.unscaledTime;
            // Rapid events must not blur into one long buzz.
            if (kind == HapticKind.Selection)
            {
                if (now - lastSelectionTime < 0.05f) return;
                lastSelectionTime = now;
            }
            else if (kind == HapticKind.UiTap)
            {
                if (now - lastUiTapTime < 0.04f) return;
                lastUiTapTime = now;
            }

            if (kind == HapticKind.LevelComplete)
            {
                PlayLevelComplete();
                return;
            }

#if UNITY_IOS && !UNITY_EDITOR
            PlayIos(kind);
#elif UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroid(kind);
#endif
        }

        public void PlayLevelComplete()
        {
            if (!Enabled) return;
            if (runner == null || !runner.isActiveAndEnabled)
            {
                TriggerLevelCompleteStart();
                return;
            }

            if (levelCompleteRoutine != null) runner.StopCoroutine(levelCompleteRoutine);
            levelCompleteRoutine = runner.StartCoroutine(LevelCompleteSequence());
        }

        public void CancelLevelComplete()
        {
            if (runner != null && levelCompleteRoutine != null) runner.StopCoroutine(levelCompleteRoutine);
            levelCompleteRoutine = null;
        }

        // A rising "ta-da": a bright start, then two crisp hits.
        private IEnumerator LevelCompleteSequence()
        {
            TriggerLevelCompleteStart();
            yield return new WaitForSecondsRealtime(0.32f);
            if (Enabled) PlayPlatform(HapticKind.Snap);
            yield return new WaitForSecondsRealtime(0.12f);
            if (Enabled) PlayPlatform(HapticKind.Combo);
            levelCompleteRoutine = null;
        }

        private void TriggerLevelCompleteStart()
        {
#if UNITY_IOS && !UNITY_EDITOR
            CatBlockPuzzleHapticSuccess();
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (!TryCompose(new[] { QuickRise, Click, Click }, new[] { 0.7f, 1f, 0.8f }, new[] { 0, 60, 90 }) &&
                !TryPredefined(EffectHeavyClick))
                VibrateWaveform(new long[] { 0, 30, 60, 40, 60, 60 }, new[] { 0, 170, 0, 220, 0, 255 });
#endif
        }

        private void PlayPlatform(HapticKind kind)
        {
#if UNITY_IOS && !UNITY_EDITOR
            PlayIos(kind);
#elif UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroid(kind);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private static void PlayIos(HapticKind kind)
        {
            switch (kind)
            {
                case HapticKind.Selection:
                case HapticKind.TimerTick: CatBlockPuzzleHapticSelection(); break;
                case HapticKind.UiTap: CatBlockPuzzleHapticLight(); break;
                case HapticKind.Pickup: CatBlockPuzzleHapticSoft(); break;
                case HapticKind.Snap: CatBlockPuzzleHapticMedium(); break;
                case HapticKind.Combo:
                case HapticKind.LockOpen: CatBlockPuzzleHapticRigid(); break;
                case HapticKind.Wrong: CatBlockPuzzleHapticError(); break;
                case HapticKind.HeartLost: CatBlockPuzzleHapticHeavy(); break;
                case HapticKind.LevelFail: CatBlockPuzzleHapticWarning(); break;
            }
        }

        [DllImport("__Internal")] private static extern void CatBlockPuzzleHapticLight();
        [DllImport("__Internal")] private static extern void CatBlockPuzzleHapticHeavy();
        [DllImport("__Internal")] private static extern void CatBlockPuzzleHapticSuccess();
        [DllImport("__Internal")] private static extern void CatBlockPuzzleHapticSelection();
        [DllImport("__Internal")] private static extern void CatBlockPuzzleHapticSoft();
        [DllImport("__Internal")] private static extern void CatBlockPuzzleHapticMedium();
        [DllImport("__Internal")] private static extern void CatBlockPuzzleHapticRigid();
        [DllImport("__Internal")] private static extern void CatBlockPuzzleHapticWarning();
        [DllImport("__Internal")] private static extern void CatBlockPuzzleHapticError();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        // android.os.VibrationEffect.Composition primitives and predefined effect ids.
        private const int Click = 1, Thud = 2, QuickRise = 4, QuickFall = 6, Tick = 7;
        private const int EffectClick = 0, EffectDoubleClick = 1, EffectTick = 2, EffectHeavyClick = 5;
        private const int UsageGame = 14;

        private static AndroidJavaObject vibrator;
        private static AndroidJavaObject attributes;
        private static int apiLevel = -1;
        private static readonly int[] primitiveSupport = new int[9]; // 0 unknown, 1 yes, 2 no

        private static void PlayAndroid(HapticKind kind)
        {
            switch (kind)
            {
                case HapticKind.Selection:
                    if (!TryCompose(new[] { Tick }, new[] { 0.7f }, new[] { 0 }) && !TryPredefined(EffectTick)) VibrateOneShot(10, 140);
                    break;
                case HapticKind.UiTap:
                    if (!TryCompose(new[] { Click }, new[] { 0.45f }, new[] { 0 }) && !TryPredefined(EffectTick)) VibrateOneShot(14, 150);
                    break;
                case HapticKind.TimerTick:
                    if (!TryCompose(new[] { Tick }, new[] { 1f }, new[] { 0 }) && !TryPredefined(EffectTick)) VibrateOneShot(14, 170);
                    break;
                case HapticKind.Pickup:
                    if (!TryCompose(new[] { Click }, new[] { 0.7f }, new[] { 0 }) && !TryPredefined(EffectClick)) VibrateOneShot(20, 180);
                    break;
                case HapticKind.Snap:
                    if (!TryCompose(new[] { Click, Tick }, new[] { 1f, 0.5f }, new[] { 0, 35 }) && !TryPredefined(EffectHeavyClick)) VibrateOneShot(30, 235);
                    break;
                case HapticKind.Combo:
                    if (!TryCompose(new[] { QuickRise, Click }, new[] { 0.8f, 1f }, new[] { 0, 20 }) && !TryPredefined(EffectDoubleClick))
                        VibrateWaveform(new long[] { 0, 25, 40, 35 }, new[] { 0, 200, 0, 255 });
                    break;
                case HapticKind.LockOpen:
                    if (!TryCompose(new[] { QuickRise, Click }, new[] { 0.6f, 0.9f }, new[] { 0, 30 }) && !TryPredefined(EffectClick)) VibrateOneShot(28, 200);
                    break;
                case HapticKind.Wrong:
                    if (!TryCompose(new[] { Thud, Thud }, new[] { 1f, 0.75f }, new[] { 0, 70 }) && !TryPredefined(EffectDoubleClick))
                        VibrateWaveform(new long[] { 0, 40, 60, 40 }, new[] { 0, 255, 0, 200 });
                    break;
                case HapticKind.HeartLost:
                    if (!TryCompose(new[] { Thud, QuickFall }, new[] { 1f, 0.8f }, new[] { 0, 40 }) && !TryPredefined(EffectHeavyClick)) VibrateOneShot(70, 255);
                    break;
                case HapticKind.LevelFail:
                    if (!TryCompose(new[] { QuickFall, Thud }, new[] { 1f, 0.9f }, new[] { 0, 100 }) && !TryPredefined(EffectDoubleClick))
                        VibrateWaveform(new long[] { 0, 60, 90, 120 }, new[] { 0, 220, 0, 255 });
                    break;
            }
        }

        private static int ApiLevel
        {
            get
            {
                if (apiLevel < 0)
                {
                    using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                        apiLevel = version.GetStatic<int>("SDK_INT");
                }
                return apiLevel;
            }
        }

        private static AndroidJavaObject Vibrator
        {
            get
            {
                if (vibrator != null) return vibrator;
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                return vibrator;
            }
        }

        // Game usage, so the system routes it like other in-game vibration rather than notification buzz.
        private static AndroidJavaObject Attributes
        {
            get
            {
                if (attributes != null) return attributes;
                try
                {
                    using (AndroidJavaObject builder = new AndroidJavaObject("android.media.AudioAttributes$Builder"))
                    using (AndroidJavaObject withUsage = builder.Call<AndroidJavaObject>("setUsage", UsageGame))
                        attributes = withUsage.Call<AndroidJavaObject>("build");
                }
                catch { attributes = null; }
                return attributes;
            }
        }

        private static void Vibrate(AndroidJavaObject effect)
        {
            AndroidJavaObject attrs = Attributes;
            if (attrs != null)
            {
                try { Vibrator.Call("vibrate", effect, attrs); return; }
                catch { }
            }
            Vibrator.Call("vibrate", effect);
        }

        private static bool PrimitiveSupported(int primitive)
        {
            if (primitiveSupport[primitive] == 0)
            {
                bool supported = false;
                try { supported = Vibrator.Call<bool>("areAllPrimitivesSupported", new[] { primitive }); }
                catch { supported = false; }
                primitiveSupport[primitive] = supported ? 1 : 2;
            }
            return primitiveSupport[primitive] == 1;
        }

        private static bool TryCompose(int[] primitives, float[] scales, int[] delays)
        {
            if (ApiLevel < 30 || Vibrator == null) return false;
            for (int i = 0; i < primitives.Length; i++) if (!PrimitiveSupported(primitives[i])) return false;
            try
            {
                using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (AndroidJavaObject composition = effectClass.CallStatic<AndroidJavaObject>("startComposition"))
                {
                    for (int i = 0; i < primitives.Length; i++)
                        composition.Call<AndroidJavaObject>("addPrimitive", primitives[i], scales[i], delays[i]).Dispose();
                    using (AndroidJavaObject effect = composition.Call<AndroidJavaObject>("compose")) Vibrate(effect);
                }
                return true;
            }
            catch { return false; }
        }

        private static bool TryPredefined(int effectId)
        {
            if (ApiLevel < 29 || Vibrator == null) return false;
            try
            {
                using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createPredefined", effectId))
                    Vibrate(effect);
                return true;
            }
            catch { return false; }
        }

        private static void VibrateOneShot(long milliseconds, int amplitude)
        {
            try
            {
                if (Vibrator == null) return;
                if (ApiLevel >= 26)
                {
                    using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, Mathf.Clamp(amplitude, 1, 255)))
                        Vibrate(effect);
                }
                else Vibrator.Call("vibrate", milliseconds);
            }
            catch { }
        }

        private static void VibrateWaveform(long[] timings, int[] amplitudes)
        {
            try
            {
                if (Vibrator == null) return;
                if (ApiLevel >= 26)
                {
                    using (AndroidJavaClass effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                        Vibrate(effect);
                }
                else Vibrator.Call("vibrate", timings, -1);
            }
            catch { }
        }
#endif
    }
}
