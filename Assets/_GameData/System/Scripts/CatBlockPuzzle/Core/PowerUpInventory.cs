using UnityEngine;

namespace CatBlockPuzzle
{
    public enum PowerUpKind { Freeze, Hint }

    /// <summary>Persistent inventory for consumable gameplay power-ups.</summary>
    public static class PowerUpInventory
    {
        public static int GetCount(PowerUpKind kind) => DailyRewardProgress.GetPowerUpCount(kind);

        public static void Add(PowerUpKind kind, int amount = 1)
        {
            DailyRewardProgress.AddPowerUp(kind, amount);
        }

        public static bool TryConsume(PowerUpKind kind)
        {
            return DailyRewardProgress.TryConsumePowerUp(kind);
        }

        /// <summary>Spends <paramref name="amount"/> at once; nothing is taken unless the whole amount is available.</summary>
        public static bool TryConsume(PowerUpKind kind, int amount)
        {
            if (amount <= 0 || GetCount(kind) < amount) return false;
            for (int i = 0; i < amount; i++)
            {
                if (!DailyRewardProgress.TryConsumePowerUp(kind)) return false;
            }
            return true;
        }

    }
}
