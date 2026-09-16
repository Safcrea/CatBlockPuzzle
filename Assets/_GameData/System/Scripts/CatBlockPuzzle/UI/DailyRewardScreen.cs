using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
            public Sprite availableCardSprite;
            public Sprite claimedCardSprite;
            public Sprite availableClaimSprite;
            public Sprite claimedClaimSprite;
            public bool claimed;
            public UnityEvent claimRequested = new UnityEvent();
        }

        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;
        [SerializeField] private DayView[] days = Array.Empty<DayView>();
        [Header("Rewards")]
        [SerializeField] private DailyRewardConfig rewardConfig;
        private UnityAction[] claimActions;

        public bool IsClaimAvailable => Config != null && DailyRewardProgress.GetAvailableDay(Config, DateTime.UtcNow) > 0;
        private DailyRewardConfig Config => rewardConfig != null ? rewardConfig : DailyRewardConfig.Load();

        private void OnEnable()
        {
            if (claimActions == null) claimActions = new UnityAction[days.Length];
            for (int i = 0; i < days.Length; i++)
            {
                DayView day = days[i];
                if (day == null) continue;
                int index = i;
                if (claimActions[i] == null) claimActions[i] = () => RequestClaim(days[index].day);
                Bind(day.cardButton, claimActions[i], true);
                if (day.claimButton != day.cardButton) Bind(day.claimButton, claimActions[i], true);
                ApplyClaimedState(day);
            }
            Bind(closeButton, Close, true);
            RefreshPresentation();
        }

        private void OnDisable()
        {
            Bind(closeButton, Close, false);
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

        public void RequestClaim(int dayNumber)
        {
            DailyRewardConfig config = Config;
            int availableDay = config != null ? DailyRewardProgress.GetAvailableDay(config, DateTime.UtcNow) : 0;
            if (availableDay != dayNumber) return;

            foreach (var day in days)
                if (day != null && day.day == dayNumber)
                {
                    if (GameSystem.Instance == null) return;
                    if (config == null || !DailyRewardProgress.TryClaim(config, DateTime.UtcNow, dayNumber, out var reward)) return;
                    if (reward.coins > 0) GameSystem.Instance.AwardCoins(reward.coins);
                    GameSystem.Instance.RefreshPowerUpHud();
                    day.claimRequested?.Invoke();
                    RefreshPresentation();
                    return;
                }
        }

        /// <summary>Called by the reward system after a claim succeeds, or while restoring its saved state.</summary>
        public void SetDayClaimed(int dayNumber, bool claimed)
        {
            foreach (var day in days)
                if (day != null && day.day == dayNumber)
                { day.claimed = claimed; ApplyClaimedState(day); return; }
        }

        private static void ApplyClaimedState(DayView day)
        {
            Sprite card = day.claimed ? day.claimedCardSprite : day.availableCardSprite;
            Sprite claim = day.claimed ? day.claimedClaimSprite : day.availableClaimSprite;
            if (day.cardImage != null && card != null) day.cardImage.sprite = card;
            if (day.claimImage != null && claim != null) day.claimImage.sprite = claim;
            if (day.cardButton != null) day.cardButton.interactable = !day.claimed;
            if (day.claimButton != null) day.claimButton.interactable = !day.claimed;
        }

        public void Show()
        {
            if (root != null) root.SetActive(true);
            RefreshPresentation();
        }
        public void Hide() { if (root != null) root.SetActive(false); }
        private void Close() => GameSystem.Instance?.GoHome();

        private string RewardLabelForDay(int dayNumber)
        {
            DailyRewardConfig.DayReward reward = Config != null ? Config.GetDay(dayNumber) : default;
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
            for (int i = 0; i < days.Length; i++)
            {
                DayView day = days[i];
                if (day == null) continue;
                day.claimed = claimedToday ? day.day <= savedDay : day.day < availableDay;
                if (day.amountLabel != null) day.amountLabel.text = RewardLabelForDay(day.day);
                ApplyClaimedState(day);
                bool canClaim = day.day == availableDay;
                if (day.cardButton != null) day.cardButton.interactable = canClaim;
                if (day.claimButton != null) day.claimButton.interactable = canClaim;
            }
        }
    }
}
