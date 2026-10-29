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
        [Header("Starter Bundle")]
        [SerializeField] private TMP_Text starterContents;
        [SerializeField] private TMP_Text starterPrice;
        [Header("Coin Packs (top to bottom)")]
        [SerializeField] private TMP_Text[] coinPackAmounts = new TMP_Text[0];
        [SerializeField] private TMP_Text[] coinPackPrices = new TMP_Text[0];
        private float clearAt;
        private PowerUpShopConfig Config => GameSystem.Instance != null ? GameSystem.Instance.PowerUpShop : PowerUpShopConfig.Load();
        private void OnEnable()
        {
            if (statusLabel != null) statusLabel.text = string.Empty;
            var config = Config;
            if (config == null) return;
            var offer = config.GetOffer(PowerUpKind.Hint);
            if (hintPrice != null) hintPrice.text = offer.coinPrice + " coins";
            if (hintQuantity != null) hintQuantity.text = "x" + offer.quantity + " Hint";
            offer = config.GetOffer(PowerUpKind.Freeze);
            if (freezePrice != null) freezePrice.text = offer.coinPrice + " coins";
            if (freezeQuantity != null) freezeQuantity.text = "x" + offer.quantity + " Freeze";
            RefreshBundleAndPacks(config);
        }

        private void RefreshBundleAndPacks(PowerUpShopConfig config)
        {
            var bundle = config.StarterBundle;
            if (starterContents != null) starterContents.text = Describe(bundle);
            if (starterPrice != null) starterPrice.text = bundle.coinPrice + " coins";
            for (int i = 0; i < coinPackAmounts.Length; i++)
            {
                var pack = config.GetCoinPack(i);
                if (coinPackAmounts[i] != null) coinPackAmounts[i].text = pack.coins > 0 ? pack.coins.ToString() : string.Empty;
                if (i < coinPackPrices.Length && coinPackPrices[i] != null)
                    coinPackPrices[i].text = pack.priceLabel;
            }
        }

        private static string Describe(PowerUpShopConfig.Bundle bundle)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (bundle.hints > 0) parts.Add(bundle.hints + " Hint" + (bundle.hints == 1 ? "" : "s"));
            if (bundle.freezes > 0) parts.Add(bundle.freezes + " Freeze" + (bundle.freezes == 1 ? "" : "s"));
            if (bundle.rotates > 0) parts.Add(bundle.rotates + " Rotate" + (bundle.rotates == 1 ? "" : "s"));
            if (bundle.bonusCoins > 0) parts.Add("+" + bundle.bonusCoins + " coins");
            return string.Join(" · ", parts);
        }

        public void BuyStarterBundle()
        {
            var config = Config;
            if (config == null || !config.StarterBundle.IsValid) return;
            var system = GameSystem.Instance;
            PowerUpPurchaseStatus result = system != null ? system.PurchaseStarterBundle() : PowerUpPurchaseStatus.Unavailable;
            if (result == PowerUpPurchaseStatus.Success) Message("Bundle added!");
            else if (result == PowerUpPurchaseStatus.InsufficientCoins) Message("Not enough coins");
            else Message("This offer is unavailable");
        }

        public void BuyCoinPack0() => BuyCoinPack(0);
        public void BuyCoinPack1() => BuyCoinPack(1);
        public void BuyCoinPack2() => BuyCoinPack(2);
        private void BuyCoinPack(int index)
        {
            var config = Config;
            var pack = config != null ? config.GetCoinPack(index) : default;
            if (!pack.available || pack.coins <= 0) return; // no store connected yet: the row just shows its values
            var system = GameSystem.Instance;
            if (system != null && system.GrantShopCoins(pack.coins)) Message("+" + pack.coins + " coins!");
            else Message("This offer is unavailable");
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
