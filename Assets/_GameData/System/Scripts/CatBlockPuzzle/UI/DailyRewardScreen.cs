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
            public UnityEvent claimRequested = new UnityEvent();
        }

        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;
        [SerializeField] private DayView[] days = Array.Empty<DayView>();
        private UnityAction[] claimActions;

        private void Awake()
        {
            claimActions = new UnityAction[days.Length];
            for (int i = 0; i < days.Length; i++)
            {
                DayView day = days[i];
                if (day == null || day.claimButton == null) continue;
                claimActions[i] = day.claimRequested.Invoke;
                day.claimButton.onClick.AddListener(claimActions[i]);
            }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (claimActions == null) return;
            for (int i = 0; i < days.Length; i++)
                if (days[i]?.claimButton != null && claimActions[i] != null)
                    days[i].claimButton.onClick.RemoveListener(claimActions[i]);
        }

        public void Show() { if (root != null) root.SetActive(true); }
        public void Hide() { if (root != null) root.SetActive(false); }
        private void Close() => GameSystem.Instance?.GoHome();
    }
}
