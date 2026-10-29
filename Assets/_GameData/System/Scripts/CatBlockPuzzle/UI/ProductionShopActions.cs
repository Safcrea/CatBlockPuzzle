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
        [SerializeField] private TMP_Text freezePrice;
        [SerializeField] private TMP_Text freezeQuantity;
        private float clearAt;
        private void OnEnable()
        {
            if (statusLabel != null) statusLabel.text = string.Empty;
            var config = GameSystem.Instance != null ? GameSystem.Instance.PowerUpShop : PowerUpShopConfig.Load();
            if (config == null) return;
            var offer = config.GetOffer(PowerUpKind.Hint);
            if (hintPrice != null) hintPrice.text = offer.coinPrice + " coins";
            if (hintQuantity != null) hintQuantity.text = "x" + offer.quantity + " Hint";
            offer = config.GetOffer(PowerUpKind.Freeze);
            if (freezePrice != null) freezePrice.text = offer.coinPrice + " coins";
            if (freezeQuantity != null) freezeQuantity.text = "x" + offer.quantity + " Freeze";
        }
        public void BuyHint() => Buy(PowerUpKind.Hint, "Hints");
        public void BuyFreeze() => Buy(PowerUpKind.Freeze, "Freezes");
        private void Buy(PowerUpKind kind, string plural)
        {
            var system = GameSystem.Instance;
            PowerUpPurchaseStatus result = system != null ? system.PurchasePowerUpFromShop(kind) : PowerUpPurchaseStatus.Unavailable;
            if (result == PowerUpPurchaseStatus.Success) Message(plural + " added!");
            else if (result == PowerUpPurchaseStatus.InsufficientCoins) Message("Not enough coins");
            else if (result == PowerUpPurchaseStatus.InventoryFull) Message("Your " + plural.ToLowerInvariant() + " are full");
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
