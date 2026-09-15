using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class ShopScreen : MonoBehaviour
    {
        [Serializable] public sealed class OfferView
        {
            public Button button;
            public TMP_Text priceLabel;
            public string productId;
            public UnityEvent purchaseRequested = new UnityEvent();
        }

        [SerializeField] private GameObject root;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private OfferView[] offers = Array.Empty<OfferView>();
        private UnityAction[] purchaseActions;

        private void Awake()
        {
            purchaseActions = new UnityAction[offers.Length];
            for (int i = 0; i < offers.Length; i++)
            {
                OfferView offer = offers[i];
                if (offer == null || offer.button == null) continue;
                purchaseActions[i] = offer.purchaseRequested.Invoke;
                offer.button.onClick.AddListener(purchaseActions[i]);
            }
            if (backButton != null) backButton.onClick.AddListener(Back);
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(Back);
            if (purchaseActions == null) return;
            for (int i = 0; i < offers.Length; i++)
                if (offers[i]?.button != null && purchaseActions[i] != null)
                    offers[i].button.onClick.RemoveListener(purchaseActions[i]);
        }

        public void Show()
        {
            if (root != null) root.SetActive(true);
        }
        public void Hide() { if (root != null) root.SetActive(false); }
        public void SetCoinBalance(int balance) { if (coinLabel != null) coinLabel.text = Mathf.Max(0, balance).ToString(); }
        private void Back() => GameSystem.Instance?.GoHome();
    }
}
