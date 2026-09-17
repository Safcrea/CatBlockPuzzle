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
            [Tooltip("Optional artwork displayed in this day's reward-icon Image.")]
            public Sprite rewardIcon;
            [Tooltip("Suppress the amount/reward label for this day.")]
            public bool leaveTextEmpty;
            [Tooltip("Optional replacement for the generated reward label. Leave blank to generate it from the reward amounts.")]
            public string textOverride;
        }

        [SerializeField] private DayReward[] days = Array.Empty<DayReward>();
        public int DayCount => days?.Length ?? 0;
        public DayReward GetDay(int oneBasedDay) => days != null && oneBasedDay > 0 && oneBasedDay <= days.Length
            ? days[oneBasedDay - 1] : default;

        public static DailyRewardConfig Load() => Resources.Load<DailyRewardConfig>(ResourcePath);
    }
}
