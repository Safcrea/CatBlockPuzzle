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
        private UnityAction[] claimActions;

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
            foreach (var day in days)
                if (day != null && day.day == dayNumber && !day.claimed)
                { day.claimRequested?.Invoke(); return; }
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

        public void Show() { if (root != null) root.SetActive(true); }
        public void Hide() { if (root != null) root.SetActive(false); }
        private void Close() => GameSystem.Instance?.GoHome();
    }
}
