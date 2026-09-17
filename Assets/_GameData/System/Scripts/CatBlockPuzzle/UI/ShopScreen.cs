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
        [Header("Opening Animation")]
        [SerializeField] private MenuTransition.EntranceSettings openingAnimation = new MenuTransition.EntranceSettings();
        [Header("Closing Animation")]
        [SerializeField] private MenuTransition.ExitSettings closingAnimation = new MenuTransition.ExitSettings();
        private UnityAction[] purchaseActions;
        private int displayedBalance = -1;
        public bool IsOpen => root != null && root.activeInHierarchy;

        private void Awake()
        {
            EnsureCoinLabel();
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
            EnsureCoinLabel();
            SetCoinBalance(GameSystem.Instance != null ? GameSystem.Instance.CurrentCoins : 0);
            var cards = new Transform[offers.Length];
            for (int i = 0; i < offers.Length; i++)
                if (offers[i]?.button != null) cards[i] = offers[i].button.transform;
            MenuTransition.Show(root, true, openingAnimation, cards);
        }
        private void LateUpdate()
        {
            if (root != null && root.activeInHierarchy && GameSystem.Instance != null)
                SetCoinBalance(GameSystem.Instance.CurrentCoins);
        }

        private void EnsureCoinLabel()
        {
            if (coinLabel != null || root == null) return;
            Transform hud = root.transform.Find("BG/Header/CoinsHud");
            if (hud == null) return;
            coinLabel = hud.GetComponentInChildren<TMP_Text>(true);
            if (coinLabel != null) return;
            TMP_Text sample = root.GetComponentInChildren<TMP_Text>(true);
            var label = new GameObject("Coin Balance", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.layer = hud.gameObject.layer;
            label.transform.SetParent(hud, false);
            coinLabel = label.GetComponent<TMP_Text>();
            // Reuse the shop's authored font so this also works without TMP default resources.
            if (sample != null && sample != coinLabel) coinLabel.font = sample.font;
            coinLabel.fontSize = 34f;
            coinLabel.enableAutoSizing = true;
            coinLabel.fontSizeMin = 18f;
            coinLabel.fontSizeMax = 34f;
            coinLabel.fontStyle = FontStyles.Bold;
            coinLabel.alignment = TextAlignmentOptions.Center;
            coinLabel.color = RuntimeUiFactory.Ink;
            coinLabel.raycastTarget = false;
            RectTransform rect = coinLabel.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(80f, 8f);
            rect.offsetMax = new Vector2(-18f, -8f);
        }
        public void Hide() => MenuTransition.Hide(root, null, false);
        public void HideAnimated(Action completed) => MenuTransition.Hide(root, completed, true, closingAnimation);
        public void SetCoinBalance(int balance)
        {
            balance = Mathf.Max(0, balance);
            if (coinLabel == null || displayedBalance == balance) return;
            displayedBalance = balance;
            coinLabel.text = balance.ToString();
        }
        private void Back() => GameSystem.Instance?.GoHome();
    }
}
