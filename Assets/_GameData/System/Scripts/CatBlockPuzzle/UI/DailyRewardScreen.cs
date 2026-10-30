using AVNEvents;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class DailyRewardScreen : MonoBehaviour
    {
        [Serializable] public sealed class DayView
        {
            public int day;
            public TMP_Text dayLabel;
            public TMP_Text amountLabel;
            public Image rewardIcon;
            public Button claimButton;
            public Button cardButton;
            public Image cardImage;
            public Image claimImage;
            [FormerlySerializedAs("availableCardSprite")] public Sprite claimDaySprite;
            [FormerlySerializedAs("claimedCardSprite")] public Sprite backgroundDaySprite;
            public Sprite availableClaimSprite;
            public Sprite claimedClaimSprite;
            public bool claimed;
            public Transform motionRoot;
            public GameObject claimedBadge;
            public Sprite claimedCardSprite;
            public Sprite lockedCardSprite;
            public UnityEvent claimRequested = new UnityEvent();
        }

        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button okayButton;
        [SerializeField] private DayView[] days = Array.Empty<DayView>();
        [SerializeField] private bool useAuthoredRewardIcons;
        [SerializeField] private Button claimCurrentButton;
        [SerializeField] private Button laterButton;
        [SerializeField] private TMP_Text bonusCoinsLabel;
        [SerializeField] private TMP_Text bonusHintLabel;
        [SerializeField] private TMP_Text bonusFreezeLabel;
        [Header("Rewards")]
        [SerializeField] private DailyRewardConfig rewardConfig;
        [Header("Opening Animation")]
        [SerializeField] private MenuTransition.EntranceSettings openingAnimation = new MenuTransition.EntranceSettings();
        [Header("Closing Animation")]
        [SerializeField] private MenuTransition.ExitSettings closingAnimation = new MenuTransition.ExitSettings();
        [Header("Next Reward")]
        [Tooltip("Alpha of every day card except today's claim or the next reward that is counting down.")]
        [SerializeField, Range(0f, 1f)] private float inactiveCardAlpha = 0.55f;
        private UnityAction[] claimActions;
        private DayView countdownDay;
        private TMP_Text countdownLabel;
        private int countdownSecondsShown = -1;

        public bool IsClaimAvailable => AvailableDay > 0;
        /// <summary>The day that can be claimed right now, or 0 when today's reward is taken.</summary>
        public int AvailableDay => Config != null ? DailyRewardProgress.GetAvailableDay(Config, DateTime.UtcNow) : 0;
        public bool IsOpen => root != null && root.activeInHierarchy;
        public bool HasUnreadReward => DailyRewardProgress.HasUnreadReward(Config, DateTime.UtcNow);
        private DailyRewardConfig Config => rewardConfig != null ? rewardConfig : DailyRewardConfig.Load();

        private void OnEnable()
        {
            if (okayButton == null && root != null)
            {
                Transform okay = root.transform.Find("BG/Okay");
                if (okay != null) okayButton = okay.GetComponent<Button>();
            }
            if (claimActions == null) claimActions = new UnityAction[days.Length];
            for (int i = 0; i < days.Length; i++)
            {
                DayView day = days[i];
                if (day == null) continue;
                int index = i;
                if (claimActions[i] == null) claimActions[i] = () => RequestClaim(days[index].day);
                Bind(day.cardButton, claimActions[i], true);
                if (day.claimButton != day.cardButton) Bind(day.claimButton, claimActions[i], true);
                LockedButtonTapSfx.Ensure(day.cardButton);
                LockedButtonTapSfx.Ensure(day.claimButton);
                ApplyClaimedState(day);
            }
            Bind(claimCurrentButton, ClaimCurrent, true);
            Bind(laterButton, CloseButtonClicked, true);
            Bind(closeButton, CloseButtonClicked, true);
            Bind(okayButton, CloseButtonClicked, true);
            RefreshPresentation();
        }

        private void OnDisable()
        {
            Bind(claimCurrentButton, ClaimCurrent, false);
            Bind(laterButton, CloseButtonClicked, false);
            Bind(closeButton, CloseButtonClicked, false);
            Bind(okayButton, CloseButtonClicked, false);
            if (claimActions == null) return;
            for (int i = 0; i < days.Length; i++)
            {
                if (days[i] == null || claimActions[i] == null) continue;
                Bind(days[i].cardButton, claimActions[i], false);
                Bind(days[i].claimButton, claimActions[i], false);
            }
        }

        private static void Bind(Button button, UnityAction action, bool bind)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (bind) button.onClick.AddListener(action);
        }

        private void ClaimCurrent() => RequestClaim(AvailableDay);

        public void RequestClaim(int dayNumber)
        {
            ButtonSfx.PlayClick();
            DailyRewardConfig config = Config;
            int availableDay = config != null ? DailyRewardProgress.GetAvailableDay(config, DateTime.UtcNow) : 0;
            if (availableDay != dayNumber) return;

            foreach (var day in days)
                if (day != null && day.day == dayNumber)
                {
                    if (GameSystem.Instance == null) return;
                    int previousDay = DailyRewardProgress.GetLastClaimDay();
                    if (config == null || !DailyRewardProgress.TryClaim(config, DateTime.UtcNow, dayNumber, out var reward)) return;
                    if (reward.coins > 0) GameSystem.Instance.AwardCoins(reward.coins);
                    ReportClaim(config, dayNumber, previousDay, reward);
                    GameSystem.Instance.RefreshPowerUpHud();
                    day.claimRequested?.Invoke();
                    RefreshPresentation();
                    Close();
                    return;
                }
        }

        private static void ReportClaim(DailyRewardConfig config, int dayNumber, int previousDay, DailyRewardConfig.DayReward reward)
        {
            GameAnalytics.DailyRewardClaimed(dayNumber, previousDay, config.DayCount, reward.coins, reward.hintCount, reward.freezeCount);
            GameAnalytics.CurrencyEarned(reward.coins, GameAnalytics.EarnDailyReward, GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0);
            GameAnalytics.ItemEarned(PowerUpKind.Hint, reward.hintCount, GameAnalytics.EarnDailyReward);
            GameAnalytics.ItemEarned(PowerUpKind.Freeze, reward.freezeCount, GameAnalytics.EarnDailyReward);
            AvnEvents.ScreenInteraction(AvnScreen.daily_gift, ScreenAction.claim, "day_" + dayNumber);
            AvnEvents.EconomyEarned(reward.coins, GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0, EconomySource.gift, EconomyDetail.daily_reward);
            AvnEvents.PowerupObtained(PowerUpKind.Hint, reward.hintCount, PowerupSource.Gift);
            AvnEvents.PowerupObtained(PowerUpKind.Freeze, reward.freezeCount, PowerupSource.Gift);
        }

        /// <summary>Called by the reward system after a claim succeeds, or while restoring its saved state.</summary>
        public void SetDayClaimed(int dayNumber, bool claimed)
        {
            foreach (var day in days)
                if (day != null && day.day == dayNumber)
                { day.claimed = claimed; ApplyClaimedState(day, false); return; }
        }

        private static void ApplyClaimedState(DayView day, bool canClaim = false)
        {
            // claim_day is reserved for today's available reward; all other standard
            // day cards use bg_day, whether they are future or already claimed.
            Sprite card = canClaim ? day.claimDaySprite : day.backgroundDaySprite;
            if (day.claimed && day.claimedCardSprite != null) card = day.claimedCardSprite;
            else if (!canClaim && !day.claimed && day.lockedCardSprite != null) card = day.lockedCardSprite;
            if (day.claimedBadge != null) day.claimedBadge.SetActive(day.claimed);
            Sprite claim = day.claimed ? day.claimedClaimSprite : day.availableClaimSprite;
            if (day.cardImage != null && card != null) day.cardImage.sprite = card;
            if (day.claimImage != null && claim != null) day.claimImage.sprite = claim;
            if (day.cardButton != null) day.cardButton.interactable = !day.claimed;
            if (day.claimButton != null) day.claimButton.interactable = !day.claimed;
        }

        public void Show()
        {
            if (root == null) return;
            // The entrance captures each card's resting alpha before enabling the root, so dim first.
            RefreshPresentation();
            var cards = new Transform[days.Length];
            for (int i = 0; i < days.Length; i++)
                if (days[i]?.cardImage != null) cards[i] = days[i].motionRoot != null ? days[i].motionRoot : days[i].cardImage.transform;
            MenuTransition.Show(root, true, openingAnimation, cards);
        }
        public void Hide()
        {
            StopCountdown();
            MenuTransition.Hide(root, null, false);
        }
        public void HideAnimated(Action completed)
        {
            StopCountdown();
            MenuTransition.Hide(root, completed, true, closingAnimation);
        }
        private void Close() => GameSystem.Instance?.GoHome();
        private void CloseButtonClicked()
        {
            ButtonSfx.PlayClick();
            GameAnalytics.DailyRewardDismissed(AvailableDay);
            AvnEvents.ScreenInteraction(AvnScreen.daily_gift, ScreenAction.later);
            Close();
        }

        private string RewardLabelFor(DailyRewardConfig.DayReward reward, int dayNumber)
        {
            if (reward.leaveTextEmpty) return string.Empty;
            if (!string.IsNullOrWhiteSpace(reward.textOverride)) return reward.textOverride;
            int coins = reward.coins;
            if (coins > 0 && dayNumber != 7) return "x" + coins;
            if (reward.hintCount > 0 && reward.freezeCount == 0) return "x" + reward.hintCount + " Hint";
            if (reward.freezeCount > 0 && reward.hintCount == 0) return "x" + reward.freezeCount + " Freeze";
            return string.Empty;
        }

        private void RefreshPresentation()
        {
            if (days == null) return;
            DailyRewardConfig config = Config;
            int availableDay = config != null ? DailyRewardProgress.GetAvailableDay(config, DateTime.UtcNow) : 0;
            int savedDay = DailyRewardProgress.GetLastClaimDay();
            bool claimedToday = availableDay == 0;
            // After today's claim the following day is the one to wait for; it wraps after the last day.
            int dayCount = config != null ? config.DayCount : days.Length;
            int nextDay = claimedToday && dayCount > 0 ? (Mathf.Max(0, savedDay) % dayCount) + 1 : availableDay;
            DayView nextView = null;
            for (int i = 0; i < days.Length; i++)
            {
                DayView day = days[i];
                if (day == null) continue;
                if (day.dayLabel != null) day.dayLabel.text = "Day " + day.day;
                day.claimed = claimedToday ? day.day <= savedDay : day.day < availableDay;
                bool highlighted = day.day == nextDay;
                SetCardDimmed(day, !highlighted);
                if (highlighted) nextView = day;
                DailyRewardConfig.DayReward reward = config != null ? config.GetDay(day.day) : default;
                if (!useAuthoredRewardIcons && day.rewardIcon != null && reward.rewardIcon != null) day.rewardIcon.sprite = reward.rewardIcon;
                if (day.amountLabel != null) day.amountLabel.text = RewardLabelFor(reward, day.day);
                bool canClaim = day.day == availableDay;
                ApplyClaimedState(day, canClaim);
                if (day.cardButton != null) day.cardButton.interactable = canClaim;
                if (day.claimButton != null) day.claimButton.interactable = canClaim;
            }

            if (claimCurrentButton != null) claimCurrentButton.interactable = availableDay > 0;
            DailyRewardConfig.DayReward bonus = config != null ? config.GetDay(7) : default;
            if (bonusCoinsLabel != null) bonusCoinsLabel.text = bonus.coins.ToString();
            if (bonusHintLabel != null) bonusHintLabel.text = "x" + bonus.hintCount;
            if (bonusFreezeLabel != null) bonusFreezeLabel.text = "x" + bonus.freezeCount;
            if (claimedToday && nextView != null) StartCountdown(nextView);
            else StopCountdown();
        }

        private void SetCardDimmed(DayView day, bool dimmed)
        {
            if (day.cardImage == null) return;
            GameObject card = day.motionRoot != null ? day.motionRoot.gameObject : day.cardImage.gameObject;
            CanvasGroup group = card.GetComponent<CanvasGroup>();
            if (group == null) group = card.AddComponent<CanvasGroup>();
            group.alpha = dimmed ? inactiveCardAlpha : 1f;
            // Dimmed cards still take taps so a locked tap can answer with a sound.
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        // The next card shows a clock above its icon, in place of its day label, counting down to the next UTC day.
        private void StartCountdown(DayView day)
        {
            if (countdownDay != day)
            {
                StopCountdown();
                countdownDay = day;
                countdownLabel = CreateCountdownLabel(day);
                if (countdownLabel != null && day.dayLabel != null) day.dayLabel.gameObject.SetActive(false);
            }
            countdownSecondsShown = -1;
            UpdateCountdown();
        }

        private void StopCountdown()
        {
            if (countdownDay != null && countdownDay.dayLabel != null) countdownDay.dayLabel.gameObject.SetActive(true);
            if (countdownLabel != null)
            {
                // Editor authoring can hide this screen outside Play Mode, where Destroy is not allowed.
                if (Application.isPlaying) Destroy(countdownLabel.gameObject);
                else DestroyImmediate(countdownLabel.gameObject);
            }
            countdownLabel = null;
            countdownDay = null;
            countdownSecondsShown = -1;
        }

        private static TMP_Text CreateCountdownLabel(DayView day)
        {
            TMP_Text template = day.dayLabel != null ? day.dayLabel : day.amountLabel;
            if (template == null || day.cardImage == null) return null;

            // Sits in the band above the reward icon, where the day label normally is, centred on the card.
            TMP_Text label = Instantiate(template, day.cardImage.transform);
            label.name = "Next Reward Countdown";
            label.gameObject.SetActive(true);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -14f);
            rect.sizeDelta = new Vector2(-16f, 44f);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 32f;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }

        private void Update()
        {
            if (countdownLabel != null && IsOpen) UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (countdownLabel == null) return;
            DateTime now = DateTime.UtcNow;
            int secondsLeft = Mathf.CeilToInt((float)(now.Date.AddDays(1) - now).TotalSeconds);
            if (secondsLeft <= 0)
            {
                // The day rolled over while the screen was open: the next reward is claimable now.
                RefreshPresentation();
                return;
            }
            if (secondsLeft == countdownSecondsShown) return;
            countdownSecondsShown = secondsLeft;
            TimeSpan left = TimeSpan.FromSeconds(secondsLeft);
            countdownLabel.text = ((int)left.TotalHours).ToString("00") + ":" + left.Minutes.ToString("00") + ":" + left.Seconds.ToString("00");
        }
    }
}
