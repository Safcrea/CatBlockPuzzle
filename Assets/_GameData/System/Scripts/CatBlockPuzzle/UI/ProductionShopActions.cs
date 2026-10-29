using System.Collections;
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
        [Header("Message toast")]
        [Tooltip("Centred pill that holds the status label. Shown for Toast Seconds, then faded out.")]
        [SerializeField] private CanvasGroup toast;
        [SerializeField, Min(0.5f)] private float toastSeconds = 2f;
        private Coroutine toastRoutine;
        private PowerUpShopConfig Config => GameSystem.Instance != null ? GameSystem.Instance.PowerUpShop : PowerUpShopConfig.Load();
        private void OnEnable()
        {
            if (statusLabel != null) statusLabel.text = string.Empty;
            toastRoutine = null;
            if (toast != null) toast.gameObject.SetActive(false);
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
            var system = GameSystem.Instance;
            PowerUpPurchaseStatus result = config == null || !config.StarterBundle.IsValid ? PowerUpPurchaseStatus.InvalidOffer
                : system != null ? system.PurchaseStarterBundle() : PowerUpPurchaseStatus.Unavailable;
            AVNEvents.AvnEvents.PurchaseOutcome("starter_bundle", "shop", result,
                config != null ? config.StarterBundle.coinPrice : 0, 1, system != null ? system.CurrentCoins : 0);
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
            var system = GameSystem.Instance;
            bool granted = pack.available && pack.coins > 0 && system != null && system.GrantShopCoins(pack.coins);
            AVNEvents.AvnEvents.PurchaseOutcome("coin_pack_" + index, "shop", granted ? PowerUpPurchaseStatus.Success
                : PowerUpPurchaseStatus.Unavailable, 0, pack.coins, system != null ? system.CurrentCoins : 0);
            if (granted) Message("+" + pack.coins + " coins!");
            else Message("This offer is unavailable");
        }
        public void BuyHint() => Buy(PowerUpKind.Hint, "Hints");
        public void BuyFreeze() => Buy(PowerUpKind.Freeze, "Freezes");
        private void Buy(PowerUpKind kind, string plural)
        {
            var system = GameSystem.Instance;
            PowerUpPurchaseStatus result = system != null ? system.PurchasePowerUpFromShop(kind) : PowerUpPurchaseStatus.Unavailable;
            var offer = Config != null ? Config.GetOffer(kind) : default;
            AVNEvents.AvnEvents.PurchaseOutcome(AVNEvents.AvnEvents.PowerupName(kind), "shop", result,
                offer.coinPrice, offer.quantity, system != null ? system.CurrentCoins : 0);
            if (result == PowerUpPurchaseStatus.Success) Message(plural + " added!");
            else if (result == PowerUpPurchaseStatus.InsufficientCoins) Message("Not enough coins");
            else if (result == PowerUpPurchaseStatus.InventoryFull) Message("Your " + plural.ToLowerInvariant() + " are full");
            else Message("This offer is unavailable");
        }
        // A big centred toast: it pops in, stays for a couple of seconds, then fades away.
        private void Message(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
            if (toast == null) return;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ShowToast());
        }

        private IEnumerator ShowToast()
        {
            RectTransform rect = (RectTransform)toast.transform;
            bool reduced = PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;
            toast.gameObject.SetActive(true);
            toast.transform.SetAsLastSibling();
            toast.alpha = 1f;
            const float popTime = .28f, fadeTime = .4f;
            for (float t = 0f; t < popTime && !reduced; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / popTime);
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(.6f, 1f, EaseOutBack(k));
                yield return null;
            }
            rect.localScale = Vector3.one;
            for (float t = 0f; t < toastSeconds; t += Time.unscaledDeltaTime) yield return null;
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                toast.alpha = 1f - Mathf.Clamp01(t / fadeTime);
                if (!reduced) rect.localScale = Vector3.one * (1f + .06f * Mathf.Clamp01(t / fadeTime));
                yield return null;
            }
            toast.alpha = 0f;
            toast.gameObject.SetActive(false);
            if (statusLabel != null) statusLabel.text = string.Empty;
            toastRoutine = null;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
