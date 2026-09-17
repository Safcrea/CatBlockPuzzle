using System;
using System.Globalization;
using UnityEngine;

namespace CatBlockPuzzle
{
    [Serializable]
    internal sealed class DailyRewardSave
    {
        public int version = 1;
        public string lastClaimUtcDate;
        public int lastClaimDay;
        public int freezeCount;
        public int hintCount;
    }

    internal static class DailyRewardProgress
    {
        private const string SaveKey = "CatBlockPuzzle.DailyReward.State.v1";
        private const string DateFormat = "yyyy-MM-dd";
        // The Home badge reminds players to claim, even after viewing and dismissing the screen.
        internal static bool HasUnreadReward(DailyRewardConfig config, DateTime utcNow) =>
            GetAvailableDay(config, utcNow) > 0;

        internal static int GetPowerUpCount(PowerUpKind kind)
        {
            DailyRewardSave save = Load();
            return kind == PowerUpKind.Freeze ? save.freezeCount : save.hintCount;
        }

        internal static void AddPowerUp(PowerUpKind kind, int amount)
        {
            if (amount <= 0) return;
            DailyRewardSave save = Load();
            if (kind == PowerUpKind.Freeze) save.freezeCount = AddClamped(save.freezeCount, amount);
            else save.hintCount = AddClamped(save.hintCount, amount);
            Save(save);
        }

        internal static bool TryConsumePowerUp(PowerUpKind kind)
        {
            DailyRewardSave save = Load();
            if (kind == PowerUpKind.Freeze)
            {
                if (save.freezeCount <= 0) return false;
                save.freezeCount--;
            }
            else
            {
                if (save.hintCount <= 0) return false;
                save.hintCount--;
            }
            Save(save); return true;
        }

        internal static PowerUpPurchaseStatus TryPurchasePowerUp(PowerUpKind kind, PowerUpShopConfig.Offer offer,
            int coins, string coinsKey, out int remainingCoins)
        {
            DailyRewardSave save = Load();
            int inventory = kind == PowerUpKind.Freeze ? save.freezeCount : save.hintCount;
            var status = PowerUpShopConfig.CalculatePurchase(kind, offer, coins, inventory, out remainingCoins, out int updatedInventory);
            if (status != PowerUpPurchaseStatus.Success) return status;
            if (kind == PowerUpKind.Freeze) save.freezeCount = updatedInventory;
            else save.hintCount = updatedInventory;
            // Persist the wallet and inventory together, then refresh their live views.
            PlayerPrefs.SetInt(coinsKey, remainingCoins);
            Save(save);
            return status;
        }

        internal static int GetAvailableDay(DailyRewardConfig config, DateTime utcNow)
        {
            if (config == null || config.DayCount == 0) return 0;
            DailyRewardSave save = Load();
            if (!DateTime.TryParseExact(save.lastClaimUtcDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime last)) return 1;
            int elapsed = (utcNow.Date - last.Date).Days;
            if (elapsed <= 0) return 0;
            return elapsed == 1 ? (Mathf.Clamp(save.lastClaimDay, 0, config.DayCount - 1) % config.DayCount) + 1 : 1;
        }

        internal static int GetLastClaimDay() => Mathf.Max(0, Load().lastClaimDay);

        internal static bool TryClaim(DailyRewardConfig config, DateTime utcNow, int day, out DailyRewardConfig.DayReward reward)
        {
            reward = default;
            if (GetAvailableDay(config, utcNow) != day) return false;
            reward = config.GetDay(day);
            DailyRewardSave save = Load();
            save.lastClaimUtcDate = utcNow.ToString(DateFormat, CultureInfo.InvariantCulture);
            save.lastClaimDay = day;
            save.freezeCount = AddClamped(save.freezeCount, reward.freezeCount);
            save.hintCount = AddClamped(save.hintCount, reward.hintCount);
            Save(save);
            return true;
        }

        private static DailyRewardSave Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                var startingSave = new DailyRewardSave { hintCount = 5, freezeCount = 5 };
                Save(startingSave);
                return startingSave;
            }
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            try { return string.IsNullOrEmpty(json) ? new DailyRewardSave() : JsonUtility.FromJson<DailyRewardSave>(json) ?? new DailyRewardSave(); }
            catch { return new DailyRewardSave(); }
        }
        private static void Save(DailyRewardSave save) { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save)); PlayerPrefs.Save(); }
        private static int AddClamped(int value, int amount) => (long)value + amount > int.MaxValue ? int.MaxValue : value + amount;
    }
}
