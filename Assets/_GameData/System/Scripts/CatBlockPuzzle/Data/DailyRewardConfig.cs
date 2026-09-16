using System;
using UnityEngine;

namespace CatBlockPuzzle
{
    [CreateAssetMenu(fileName = "DailyRewardConfig", menuName = "Cat Block Puzzle/Daily Reward Config")]
    public sealed class DailyRewardConfig : ScriptableObject
    {
        public const string ResourcePath = "CatBlockPuzzle/DailyRewardConfig";

        [Serializable]
        public struct DayReward
        {
            [Min(0)] public int coins;
            [Min(0)] public int freezeCount;
            [Min(0)] public int hintCount;
        }

        [SerializeField] private DayReward[] days = Array.Empty<DayReward>();
        public int DayCount => days?.Length ?? 0;
        public DayReward GetDay(int oneBasedDay) => days != null && oneBasedDay > 0 && oneBasedDay <= days.Length
            ? days[oneBasedDay - 1] : default;

        public static DailyRewardConfig Load() => Resources.Load<DailyRewardConfig>(ResourcePath);
    }
}
