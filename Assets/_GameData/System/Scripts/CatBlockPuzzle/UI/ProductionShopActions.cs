using TMPro;
using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Connects the supplied hint card to the existing coin shop.</summary>
    public sealed class ProductionShopActions : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text hintPrice;
        [SerializeField] private TMP_Text hintQuantity;
        private float clearAt;
        private void OnEnable()
        {
            if (statusLabel != null) statusLabel.text = string.Empty;
            var config = GameSystem.Instance != null ? GameSystem.Instance.PowerUpShop : PowerUpShopConfig.Load();
            if (config == null) return;
            var offer = config.GetOffer(PowerUpKind.Hint);
            if (hintPrice != null) hintPrice.text = offer.coinPrice + " coins";
            if (hintQuantity != null) hintQuantity.text = "x" + offer.quantity + " Hint";
        }
        public void BuyHint()
        {
            var system = GameSystem.Instance;
            PowerUpPurchaseStatus result = system != null ? system.PurchasePowerUpFromShop(PowerUpKind.Hint) : PowerUpPurchaseStatus.Unavailable;
            if (result == PowerUpPurchaseStatus.Success) Message("Hints added!");
            else if (result == PowerUpPurchaseStatus.InsufficientCoins) Message("Not enough coins");
            else if (result == PowerUpPurchaseStatus.InventoryFull) Message("Your hints are full");
            else Message("This offer is unavailable");
        }
        public void ComingSoon() => Message("Coming soon");
        private void Message(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
            clearAt = Time.unscaledTime + 2.5f;
        }
        private void Update()
        {
            if (statusLabel != null && clearAt > 0f && Time.unscaledTime >= clearAt)
            {
                statusLabel.text = string.Empty;
                clearAt = 0f;
            }
        }
    }
}
