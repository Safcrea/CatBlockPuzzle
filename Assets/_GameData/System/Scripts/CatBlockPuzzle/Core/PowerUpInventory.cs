using UnityEngine;

namespace CatBlockPuzzle
{
    public enum PowerUpKind { Freeze, Hint }

    /// <summary>Persistent inventory for consumable gameplay power-ups.</summary>
    public static class PowerUpInventory
    {
        private const string FreezeKey = "CatBlockPuzzle.PowerUps.Freeze";
        private const string HintKey = "CatBlockPuzzle.PowerUps.Hint";

        public static int GetCount(PowerUpKind kind) => PlayerPrefs.GetInt(KeyFor(kind), 0);

        public static void Add(PowerUpKind kind, int amount = 1)
        {
            if (amount <= 0) return;
            long total = (long)GetCount(kind) + amount;
            PlayerPrefs.SetInt(KeyFor(kind), total > int.MaxValue ? int.MaxValue : (int)total);
            PlayerPrefs.Save();
        }

        public static bool TryConsume(PowerUpKind kind)
        {
            int count = GetCount(kind);
            if (count <= 0) return false;
            PlayerPrefs.SetInt(KeyFor(kind), count - 1);
            PlayerPrefs.Save();
            return true;
        }

        private static string KeyFor(PowerUpKind kind) => kind == PowerUpKind.Freeze ? FreezeKey : HintKey;
    }
}
