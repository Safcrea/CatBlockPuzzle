using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CatBlockPuzzle.Tests
{
    public sealed class PowerUpPurchasePlayModeTests : PreparedScenePlayModeFixture
    {
        protected override string SceneName => "GameScene";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private Component SystemScreen => UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Single(g => g.GetType().Name == "GameSystem");
        private static object Read(object target, string field) => target.GetType().GetField(field, Flags).GetValue(target);
        private static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Flags).Invoke(target, args);
        private static object Kind(Component system, string name) => Enum.Parse(system.GetType().Assembly.GetType("CatBlockPuzzle.PowerUpKind"), name);
        private Component Hud => (Component)Field("gameplayHud");
        private Component Popup => (Component)Read(Hud, "purchasePopup");

        private IEnumerator PrepareEmptyInventory()
        {
            PlayerPrefs.SetString("CatBlockPuzzle.DailyReward.State.v1", "{\"version\":1,\"hintCount\":0,\"freezeCount\":0}");
            Invoke(SystemScreen, "ShowGameplayPage");
            Call("PreviewLevelForTesting", 4);
            yield return new WaitForSecondsRealtime(1.2f);
            Invoke(Hud, "RefreshPowerUpCounts");
        }

        [UnityTest]
        public IEnumerator EmptyHudPurchases_ChargeConfiguredPacksPersistAndRequireSeparateUse()
        {
            yield return PrepareEmptyInventory();
            var system = SystemScreen;
            var config = system.GetType().GetProperty("PowerUpShop").GetValue(system);
            Assert.That(config, Is.Not.Null);
            foreach (string name in new[] { "Hint", "Freeze" })
            {
                object kind = Kind(system, name);
                var offer = Invoke(config, "GetOffer", kind);
                int cost = (int)Read(offer, "coinPrice");
                int quantity = (int)Read(offer, "quantity");
                Invoke(system, "AwardCoins", cost);
                int before = (int)system.GetType().GetProperty("CurrentCoins").GetValue(system);
                var button = (Button)Read(Hud, name == "Hint" ? "hintButton" : "freezeButton");
                Assert.That(button.interactable, Is.True);
                button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(.45f);
                Assert.That(Field("timerRunning"), Is.EqualTo(false));
                Assert.That(Field("inputLocked"), Is.EqualTo(true));
                float timer = (float)Field("levelRemainingSeconds");
                yield return new WaitForSecondsRealtime(.1f);
                Assert.That((float)Field("levelRemainingSeconds"), Is.EqualTo(timer));
                var buy = (Button)Read(Popup, "buyButton");
                Assert.That(buy.interactable, Is.True);
                buy.onClick.Invoke();
                buy.onClick.Invoke();
                Assert.That(Invoke(system, "GetPowerUpCount", kind), Is.EqualTo(quantity));
                Assert.That(system.GetType().GetProperty("CurrentCoins").GetValue(system), Is.EqualTo(before - cost));
                Assert.That(PlayerPrefs.GetInt("CatBlockPuzzle.Coins"), Is.EqualTo(before - cost));
                Assert.That(PlayerPrefs.GetString("CatBlockPuzzle.DailyReward.State.v1"), Does.Contain($"\"{name.ToLowerInvariant()}Count\":{quantity}"));
                Assert.That((float)Field("freezeRemainingSeconds"), Is.Zero, "Buying must not activate a power-up.");
                Assert.That(Read(Popup, "purchased"), Is.EqualTo(true));
                Invoke(Popup, "Close");
                yield return new WaitForSecondsRealtime(.35f);
                Assert.That(Field("timerRunning"), Is.EqualTo(true));
                button.onClick.Invoke();
                Assert.That(Invoke(system, "GetPowerUpCount", kind), Is.EqualTo(quantity - 1));
                if (name == "Freeze")
                {
                    Assert.That((float)Field("freezeRemainingSeconds"), Is.GreaterThan(0f));
                    Assert.That(button.interactable, Is.False);
                    Assert.That(Invoke(system, "TryUseFreezePowerUp"), Is.EqualTo(false));
                    Assert.That(Game.GetType().GetProperty("IsPowerUpPurchaseOpen").GetValue(Game), Is.EqualTo(false));
                }
            }
            Invoke(system, "GoHome");
            Assert.That(Game.GetType().GetProperty("IsPowerUpPurchaseOpen").GetValue(Game), Is.EqualTo(false));
        }

        [UnityTest]
        public IEnumerator InsufficientCoins_CancelAndNavigationPreserveWalletAndInventory()
        {
            yield return PrepareEmptyInventory();
            var system = SystemScreen;
            object kind = Kind(system, "Hint");
            var button = (Button)Read(Hud, "hintButton");
            int coins = (int)system.GetType().GetProperty("CurrentCoins").GetValue(system);
            Assert.That(coins, Is.Zero);
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(((Button)Read(Popup, "buyButton")).interactable, Is.False);
            Invoke(Popup, "Buy");
            Assert.That(system.GetType().GetProperty("CurrentCoins").GetValue(system), Is.EqualTo(coins));
            Assert.That(Invoke(system, "GetPowerUpCount", kind), Is.EqualTo(0));
            Invoke(Popup, "Close");
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(Field("timerRunning"), Is.EqualTo(true));
            button.onClick.Invoke();
            Invoke(system, "GoHome");
            Assert.That(Game.GetType().GetProperty("IsPowerUpPurchaseOpen").GetValue(Game), Is.EqualTo(false));
            Assert.That(Field("timerRunning"), Is.EqualTo(false));
            Call("PreviewLevelForTesting", 4);
            Invoke(system, "ShowGameplayPage");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(Field("timerRunning"), Is.EqualTo(true));
            Assert.That(Invoke(system, "GetPowerUpCount", kind), Is.EqualTo(0));
        }
    }
}
