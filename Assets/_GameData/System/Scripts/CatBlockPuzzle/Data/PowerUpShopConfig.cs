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

        /// <summary>A coin-priced pack of power-ups, with optional bonus coins.</summary>
        [Serializable]
        public struct Bundle
        {
            [Tooltip("Off = the card shows Soon and a tap says Coming soon.")] public bool available;
            [Min(1), Tooltip("Coins charged for the whole bundle.")] public int coinPrice;
            [Min(0)] public int hints;
            [Min(0)] public int freezes;
            [Min(0)] public int rotates;
            [Min(0), Tooltip("Bonus coins added on top of the power-ups.")] public int bonusCoins;
            public bool IsValid => available && coinPrice > 0 && hints + freezes + rotates + bonusCoins > 0;
        }

        /// <summary>A row on the shop's Coin Pack card. Real-money purchase is not wired up yet.</summary>
        [Serializable]
        public struct CoinPack
        {
            [Tooltip("Testing only: grants the coins for free when tapped. Leave off until a store is connected.")] public bool available;
            [Min(1), Tooltip("Coins the pack contains (shown on the card).")] public int coins;
            [Tooltip("Shown on the buy button once available, e.g. $0.99.")] public string priceLabel;
        }

        [SerializeField] private Offer hint = new Offer { coinPrice = 100, quantity = 1 };
        [SerializeField] private Offer freeze = new Offer { coinPrice = 150, quantity = 1 };
        [Tooltip("Priced like Freeze.")]
        [SerializeField] private Offer rotate = new Offer { coinPrice = 150, quantity = 1 };
        [Header("Starter Bundle")]
        [SerializeField] private Bundle starterBundle = new Bundle { available = true, coinPrice = 250, hints = 3, freezes = 2, rotates = 0, bonusCoins = 0 };
        [Header("Coin Packs (top to bottom on the Coin Pack card)")]
        [SerializeField] private CoinPack[] coinPacks =
        {
            new CoinPack { coins = 500, priceLabel = "$0.99" },
            new CoinPack { coins = 1200, priceLabel = "$1.99" },
            new CoinPack { coins = 3000, priceLabel = "$4.99" }
        };

        public Bundle StarterBundle => starterBundle;
        public int CoinPackCount => coinPacks != null ? coinPacks.Length : 0;
        public CoinPack GetCoinPack(int index) => coinPacks != null && index >= 0 && index < coinPacks.Length ? coinPacks[index] : default;

        public Offer GetOffer(PowerUpKind kind) =>
            kind == PowerUpKind.Hint ? hint : kind == PowerUpKind.Freeze ? freeze : kind == PowerUpKind.Rotate ? rotate : default;
        public static PowerUpShopConfig Load() => Resources.Load<PowerUpShopConfig>(ResourcePath);

        public static PowerUpPurchaseStatus CalculatePurchase(PowerUpKind kind, Offer offer, int coins, int inventory,
            out int remainingCoins, out int updatedInventory)
        {
            remainingCoins = coins;
            updatedInventory = inventory;
            if ((kind != PowerUpKind.Hint && kind != PowerUpKind.Freeze && kind != PowerUpKind.Rotate) || !offer.IsValid || inventory < 0)
                return PowerUpPurchaseStatus.InvalidOffer;
            if (coins < offer.coinPrice) return PowerUpPurchaseStatus.InsufficientCoins;
            if ((long)inventory + offer.quantity > int.MaxValue) return PowerUpPurchaseStatus.InventoryFull;
            remainingCoins = coins - offer.coinPrice;
            updatedInventory = inventory + offer.quantity;
            return PowerUpPurchaseStatus.Success;
        }
    }
}
