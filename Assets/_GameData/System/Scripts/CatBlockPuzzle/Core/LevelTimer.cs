using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.PuzzleTuning;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The level countdown: running/frozen state, the timer label and its warning pulse,
    /// and the three progress stars that drain as time passes. Raises <see cref="Expired"/>
    /// when the countdown reaches zero.
    /// </summary>
    internal sealed class LevelTimer : HostedService
    {
        private readonly PuzzleSession session;
        private readonly IPuzzleConfig config;
        private readonly InteractionGates gates;

        internal bool timerRunning;
        internal float levelRemainingSeconds = LevelDurationSeconds;
        internal int lastTimerSecond = -1;
        internal float freezeRemainingSeconds;
        internal bool freezeUsedThisAttempt;
        private Coroutine timerPulseRoutine;
        private Image[] progressStarBackgrounds;
        private DifficultyProfile profile = DifficultyProfile.Tutorial;

        public event Action Expired;

        private readonly HapticsController haptics;

        public LevelTimer(IGameplayContext context, PuzzleSession session, IPuzzleConfig config, InteractionGates gates,
            HapticsController haptics) : base(context)
        {
            this.haptics = haptics;
            this.session = session;
            this.config = config;
            this.gates = gates;
        }

        public bool Running => timerRunning;
        public float RemainingSeconds => levelRemainingSeconds;
        public bool IsFrozen => freezeRemainingSeconds > 0f;
        public float FreezeRemainingSeconds => freezeRemainingSeconds;
        public bool FreezeUsedThisAttempt => freezeUsedThisAttempt;
        /// <summary>The time budget and star rules of the level the timer was last reset for.</summary>
        public DifficultyProfile Profile => profile;

        private Text timerText => Hud.timerText;
        private Image[] progressStars => Hud.progressStars;
        private bool levelFailed => session.levelFailed;

        public void ResetLevelTimer()
        {
            freezeRemainingSeconds = 0f;
            freezeUsedThisAttempt = false;
            Hud?.SetFreezeAvailable(true);
            timerRunning = false;
            session.levelFailed = false;
            profile = DifficultyProfile.For(session.activeLevel, session.levelIndex);
            levelRemainingSeconds = profile.DurationSeconds;
            lastTimerSecond = Mathf.CeilToInt(levelRemainingSeconds);
            if (timerPulseRoutine != null)
            {
                StopCoroutine(timerPulseRoutine);
                timerPulseRoutine = null;
            }

            UpdateTimerDisplay();
            if (timerText != null)
            {
                timerText.rectTransform.localScale = Vector3.one;
            }
        }

        public void StartLevelTimer()
        {
            if (levelFailed || gates.ResultScreenOpen || gates.MetaUiOpen || gates.PowerUpPurchaseOpen ||
                gates.SystemPaused || gates.GameplayHidden)
            {
                return;
            }

            timerRunning = true;
            UpdateTimerDisplay();
        }

        public void StopLevelTimer()
        {
            timerRunning = false;
            if (timerPulseRoutine != null)
            {
                StopCoroutine(timerPulseRoutine);
                timerPulseRoutine = null;
            }

            if (timerText != null)
            {
                timerText.rectTransform.localScale = Vector3.one;
            }
        }

        /// <summary>Call after the host stops every coroutine; the pulse handle is stale by then.</summary>
        public void ClearPulseHandle()
        {
            timerPulseRoutine = null;
        }

        /// <summary>Advances the countdown for this frame. Onboarding never spends the timer.</summary>
        public void Tick(bool onboardingOpen)
        {
            if (!timerRunning || levelFailed || onboardingOpen)
            {
                return;
            }

            float elapsed = Time.unscaledDeltaTime;
            if (freezeRemainingSeconds > 0f)
            {
                float frozenElapsed = Mathf.Min(freezeRemainingSeconds, elapsed);
                freezeRemainingSeconds = Mathf.Max(0f, freezeRemainingSeconds - frozenElapsed);
                elapsed -= frozenElapsed;
                if (freezeRemainingSeconds > 0f) return;
                UpdateTimerDisplay();
            }
            levelRemainingSeconds -= elapsed;
            if (levelRemainingSeconds <= 0f)
            {
                levelRemainingSeconds = 0f;
                lastTimerSecond = 0;
                UpdateTimerDisplay();
                if (profile.CanFail)
                {
                    Expired?.Invoke();
                }
                else
                {
                    // Easy levels never fail: the stars are gone, but the player can still finish.
                    StopLevelTimer();
                }
                return;
            }

            int currentSecond = Mathf.CeilToInt(levelRemainingSeconds);
            if (currentSecond == lastTimerSecond)
            {
                UpdateStarDisplay();
                return;
            }

            lastTimerSecond = currentSecond;
            UpdateTimerDisplay();
            // A tick for each of the final seconds, only on levels that can actually be lost.
            if (currentSecond <= 5 && profile.CanFail) haptics?.Play(HapticKind.TimerTick);
            if (levelRemainingSeconds <= TimerWarningSeconds)
            {
                StartTimerPulse();
            }
        }

        /// <summary>Freezes the countdown for the configured duration. Eligibility is the caller's job.</summary>
        public void Freeze()
        {
            freezeUsedThisAttempt = true;
            freezeRemainingSeconds = Mathf.Max(.1f, config.FreezeDurationSeconds);
            Hud?.SetFreezeAvailable(false);
            if (timerPulseRoutine != null) { StopCoroutine(timerPulseRoutine); timerPulseRoutine = null; }
            if (timerText != null) timerText.rectTransform.localScale = Vector3.one;
            UpdateTimerDisplay();
        }

        public void UpdateTimerDisplay()
        {
            UpdateStarDisplay();
            TimerFreezeSkin freezeSkin = Hud != null ? Hud.timerFreezeSkin : null;
            if (freezeSkin != null) freezeSkin.SetFrozen(IsFrozen);
            if (timerText == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, levelRemainingSeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = minutes.ToString() + ":" + seconds.ToString("00");
            timerText.color = IsFrozen ? (freezeSkin != null ? freezeSkin.FrozenTextColor : new Color(.08f, .55f, .85f)) :
                levelRemainingSeconds <= TimerWarningSeconds ? TimerWarningColor : TimerNormalColor;
        }

        private void StartTimerPulse()
        {
            if (timerText == null)
            {
                return;
            }

            if (timerPulseRoutine != null)
            {
                StopCoroutine(timerPulseRoutine);
            }

            timerPulseRoutine = StartCoroutine(PulseTimer());
        }

        private IEnumerator PulseTimer()
        {
            RectTransform rect = timerText.rectTransform;
            float elapsed = 0f;
            const float seconds = 0.34f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(Mathf.Clamp01(elapsed / seconds) * Mathf.PI);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, TimerPulseScale, wave);
                yield return null;
            }

            rect.localScale = Vector3.one;
            timerPulseRoutine = null;
        }

        public void UpdateStarDisplay()
        {
            if (progressStars == null) return;
            if (progressStarBackgrounds == null || progressStarBackgrounds.Length != progressStars.Length)
            {
                progressStarBackgrounds = new Image[progressStars.Length];
            }
            for (int i = 0; i < progressStars.Length; i++)
            {
                Image star = progressStars[i];
                if (star == null)
                {
                    continue;
                }

                Image background = progressStarBackgrounds[i];
                if (background == null)
                {
                    background = new GameObject(star.name + " Empty", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    background.transform.SetParent(star.transform.parent, false);
                    background.transform.SetSiblingIndex(star.transform.GetSiblingIndex());
                    RectTransform rect = background.rectTransform;
                    RectTransform source = star.rectTransform;
                    rect.anchorMin = source.anchorMin;
                    rect.anchorMax = source.anchorMax;
                    rect.pivot = source.pivot;
                    rect.sizeDelta = source.sizeDelta;
                    rect.anchoredPosition3D = source.anchoredPosition3D;
                    rect.localRotation = source.localRotation;
                    rect.localScale = source.localScale;
                    background.preserveAspect = star.preserveAspect;
                    background.raycastTarget = false;
                    background.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    progressStarBackgrounds[i] = background;
                }

                background.sprite = Assets.starOutlineSprite;
                background.color = Hud.UsesAuthoredLayout ? Color.white : new Color(0.64f, 0.52f, 0.42f, 0.48f);
                background.gameObject.SetActive(star.gameObject.activeSelf);
                star.sprite = Assets.starSprite;
                star.color = Hud.UsesAuthoredLayout ? Color.white : GoldColor;
                star.type = Image.Type.Filled;
                star.fillMethod = Image.FillMethod.Vertical;
                star.fillOrigin = (int)Image.OriginVertical.Bottom;
                star.fillAmount = profile.StarFill(i, levelRemainingSeconds);
            }
        }
    }
}
