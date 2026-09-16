using System;
using System.Globalization;
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
        private UnityAction[] claimActions;

        private const string LastClaimDateKey = "CatBlockPuzzle.DailyReward.LastClaimUtc";
        private const string LastClaimDayKey = "CatBlockPuzzle.DailyReward.LastClaimDay";
        private const string DateFormat = "yyyy-MM-dd";

        public bool IsClaimAvailable => GetAvailableDay() > 0;

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
            int availableDay = GetAvailableDay();
            if (availableDay != dayNumber) return;

            foreach (var day in days)
                if (day != null && day.day == dayNumber)
                {
                    int coins = CoinsForDay(dayNumber);
                    if (GameSystem.Instance == null || (coins > 0 && !GameSystem.Instance.AwardCoins(coins))) return;
                    GrantPowerUps(dayNumber);
                    PlayerPrefs.SetString(LastClaimDateKey, DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture));
                    PlayerPrefs.SetInt(LastClaimDayKey, dayNumber);
                    PlayerPrefs.Save();
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

        private int GetAvailableDay()
        {
            int dayCount = days == null ? 0 : days.Length;
            if (dayCount == 0) return 0;
            string saved = PlayerPrefs.GetString(LastClaimDateKey, string.Empty);
            if (!DateTime.TryParseExact(saved, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastClaim)) return 1;
            int elapsedDays = (DateTime.UtcNow.Date - lastClaim.Date).Days;
            if (elapsedDays <= 0) return 0;
            if (elapsedDays == 1) return (Mathf.Clamp(PlayerPrefs.GetInt(LastClaimDayKey, 0), 0, dayCount - 1) % dayCount) + 1;
            return 1;
        }

        private static int CoinsForDay(int dayNumber)
        {
            switch (dayNumber)
            {
                case 1: return 100;
                case 3: return 250;
                case 5: return 500;
                case 6: return 750;
                case 7: return 1000;
                default: return 0;
            }
        }

        private static void GrantPowerUps(int dayNumber)
        {
            switch (dayNumber)
            {
                case 2: PowerUpInventory.Add(PowerUpKind.Hint); break;
                case 4: PowerUpInventory.Add(PowerUpKind.Freeze); break;
                case 7:
                    PowerUpInventory.Add(PowerUpKind.Freeze);
                    PowerUpInventory.Add(PowerUpKind.Hint);
                    break;
            }
        }

        private static string RewardLabelForDay(int dayNumber)
        {
            int coins = CoinsForDay(dayNumber);
            if (coins > 0 && dayNumber != 7) return "x" + coins;
            if (dayNumber == 2) return "1 Hint";
            if (dayNumber == 4) return "1 Freeze";
            return string.Empty;
        }

        private void RefreshPresentation()
        {
            if (days == null) return;
            int availableDay = GetAvailableDay();
            int savedDay = Mathf.Max(0, PlayerPrefs.GetInt(LastClaimDayKey, 0));
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
