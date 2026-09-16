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

    }
}
