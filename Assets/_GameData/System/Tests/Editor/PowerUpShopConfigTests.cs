using NUnit.Framework;
using UnityEngine;

namespace CatBlockPuzzle.Tests
{
    public sealed class PowerUpShopConfigTests
    {
        [Test]
        public void ResourceConfig_HasEditableOffersForBothPowerUps()
        {
            var config = PowerUpShopConfig.Load();
            Assert.That(config, Is.Not.Null);
            Assert.That(config.GetOffer(PowerUpKind.Hint).IsValid, Is.True);
            Assert.That(config.GetOffer(PowerUpKind.Freeze).IsValid, Is.True);
        }

        [TestCase(PowerUpKind.Hint, 100, 3, 250, 2, PowerUpPurchaseStatus.Success, 150, 5)]
        [TestCase(PowerUpKind.Freeze, 150, 1, 150, 0, PowerUpPurchaseStatus.Success, 0, 1)]
        [TestCase(PowerUpKind.Hint, 100, 1, 99, 0, PowerUpPurchaseStatus.InsufficientCoins, 99, 0)]
        [TestCase(PowerUpKind.Freeze, 150, 1, -1, 0, PowerUpPurchaseStatus.InsufficientCoins, -1, 0)]
        [TestCase(PowerUpKind.Hint, 0, 1, 100, 0, PowerUpPurchaseStatus.InvalidOffer, 100, 0)]
        [TestCase(PowerUpKind.Hint, -10, 1, 100, 0, PowerUpPurchaseStatus.InvalidOffer, 100, 0)]
        [TestCase(PowerUpKind.Hint, 100, 0, 100, 0, PowerUpPurchaseStatus.InvalidOffer, 100, 0)]
        [TestCase(PowerUpKind.Hint, 100, 2, 100, int.MaxValue - 1, PowerUpPurchaseStatus.InventoryFull, 100, int.MaxValue - 1)]
        [TestCase((PowerUpKind)99, 100, 1, 100, 0, PowerUpPurchaseStatus.InvalidOffer, 100, 0)]
        public void Purchase_ChargesOnlyValidAffordablePacks(PowerUpKind kind, int price, int quantity, int coins, int inventory,
            PowerUpPurchaseStatus expected, int expectedCoins, int expectedInventory)
        {
            var offer = new PowerUpShopConfig.Offer { coinPrice = price, quantity = quantity };
            var status = PowerUpShopConfig.CalculatePurchase(kind, offer, coins, inventory, out int remaining, out int updated);
            Assert.That(status, Is.EqualTo(expected));
            Assert.That(remaining, Is.EqualTo(expectedCoins));
            Assert.That(updated, Is.EqualTo(expectedInventory));
        }
    }
}
