using System;
using UnityEngine;

namespace CatBlockPuzzle
{
    public enum PowerUpPurchaseStatus { Success, InsufficientCoins, InvalidOffer, InventoryFull, Unavailable }

    [CreateAssetMenu(fileName = "PowerUpShopConfig", menuName = "Cat Block Puzzle/Power Up Shop Config")]
    public sealed class PowerUpShopConfig : ScriptableObject
    {
        public const string ResourcePath = "CatBlockPuzzle/PowerUpShopConfig";

        [Serializable]
        public struct Offer
        {
            [Min(1), Tooltip("Coins charged for this entire pack.")] public int coinPrice;
            [Min(1), Tooltip("Power-ups added to inventory when this pack is purchased.")] public int quantity;
            public bool IsValid => coinPrice > 0 && quantity > 0;
        }

        [SerializeField] private Offer hint = new Offer { coinPrice = 100, quantity = 1 };
        [SerializeField] private Offer freeze = new Offer { coinPrice = 150, quantity = 1 };
        public Offer GetOffer(PowerUpKind kind) => kind == PowerUpKind.Hint ? hint : kind == PowerUpKind.Freeze ? freeze : default;
        public static PowerUpShopConfig Load() => Resources.Load<PowerUpShopConfig>(ResourcePath);

        public static PowerUpPurchaseStatus CalculatePurchase(PowerUpKind kind, Offer offer, int coins, int inventory,
            out int remainingCoins, out int updatedInventory)
        {
            remainingCoins = coins;
            updatedInventory = inventory;
            if ((kind != PowerUpKind.Hint && kind != PowerUpKind.Freeze) || !offer.IsValid || inventory < 0)
                return PowerUpPurchaseStatus.InvalidOffer;
            if (coins < offer.coinPrice) return PowerUpPurchaseStatus.InsufficientCoins;
            if ((long)inventory + offer.quantity > int.MaxValue) return PowerUpPurchaseStatus.InventoryFull;
            remainingCoins = coins - offer.coinPrice;
            updatedInventory = inventory + offer.quantity;
            return PowerUpPurchaseStatus.Success;
        }
    }
}
