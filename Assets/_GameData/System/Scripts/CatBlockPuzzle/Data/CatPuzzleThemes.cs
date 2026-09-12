using System;
using UnityEngine;

namespace CatBlockPuzzle
{
    public readonly struct CatPuzzleTheme
    {
        public readonly int Index;
        public readonly int AtlasIndex;
        public readonly string Id;
        public readonly string DisplayName;
        public readonly Color HeaderColor;
        public readonly Color ObjectiveColor;
        public readonly Color BoardColor;
        public readonly Color BoardOutlineColor;
        public readonly Color TrayColor;
        public readonly Color TrayHoverColor;

        public CatPuzzleTheme(
            int index,
            int atlasIndex,
            string id,
            string displayName,
            Color headerColor,
            Color objectiveColor,
            Color boardColor,
            Color boardOutlineColor,
            Color trayColor,
            Color trayHoverColor)
        {
            Index = index;
            AtlasIndex = atlasIndex;
            Id = id;
            DisplayName = displayName;
            HeaderColor = headerColor;
            ObjectiveColor = objectiveColor;
            BoardColor = boardColor;
            BoardOutlineColor = boardOutlineColor;
            TrayColor = trayColor;
            TrayHoverColor = trayHoverColor;
        }
    }

    public static class CatPuzzleThemeCatalog
    {
        public const int ThemeCount = 5;
        public const int LevelsPerTheme = 5;
        public const int SupportedLevelCount = 100;

        private const int BlocksPerCycle = ThemeCount;
        private const int TotalBlocks = SupportedLevelCount / LevelsPerTheme;
        private const uint SequenceSeed = 0xCA71C05Fu;

        private static readonly CatPuzzleTheme[] Themes =
        {
            new CatPuzzleTheme(
                0,
                0,
                "glasshouse",
                "Sunlit Glasshouse",
                C(245, 250, 225, 238),
                C(79, 139, 105, 248),
                C(239, 247, 219),
                C(153, 181, 126, 232),
                C(188, 220, 169, 250),
                C(216, 240, 187)),
            new CatPuzzleTheme(
                1,
                1,
                "patisserie",
                "Sugar Patisserie",
                C(255, 236, 222, 238),
                C(191, 100, 108, 248),
                C(255, 239, 220),
                C(221, 166, 141, 232),
                C(247, 181, 170, 250),
                C(255, 208, 185)),
            new CatPuzzleTheme(
                2,
                2,
                "seaside",
                "Seaside Picnic",
                C(232, 248, 246, 238),
                C(54, 136, 157, 248),
                C(244, 241, 213),
                C(132, 187, 190, 232),
                C(151, 211, 211, 250),
                C(185, 233, 221)),
            new CatPuzzleTheme(
                3,
                3,
                "storybook",
                "Storybook Library",
                C(243, 236, 211, 238),
                C(105, 123, 75, 248),
                C(236, 227, 193),
                C(154, 133, 91, 232),
                C(184, 195, 140, 250),
                C(215, 212, 160)),
            new CatPuzzleTheme(
                4,
                4,
                "stargazer",
                "Stargazer Room",
                C(230, 226, 248, 238),
                C(76, 81, 149, 248),
                C(230, 225, 246),
                C(135, 122, 188, 232),
                C(168, 163, 221, 250),
                C(198, 189, 240))
        };

        private static readonly int[] ThemeBlockSequence = BuildThemeBlockSequence();

        public static CatPuzzleTheme GetTheme(int themeIndex)
        {
            return Themes[Mathf.Clamp(themeIndex, 0, Themes.Length - 1)];
        }

        public static CatPuzzleTheme GetThemeForLevel(int zeroBasedLevelIndex)
        {
            return GetTheme(GetThemeIndexForLevel(zeroBasedLevelIndex));
        }

        public static int GetThemeIndexForLevel(int zeroBasedLevelIndex)
        {
            int level = Mathf.Clamp(zeroBasedLevelIndex, 0, SupportedLevelCount - 1);
            return ThemeBlockSequence[level / LevelsPerTheme];
        }

        public static int[] GetThemeBlockSequence()
        {
            int[] copy = new int[ThemeBlockSequence.Length];
            Array.Copy(ThemeBlockSequence, copy, ThemeBlockSequence.Length);
            return copy;
        }

        private static int[] BuildThemeBlockSequence()
        {
            int[] sequence = new int[TotalBlocks];
            for (int theme = 0; theme < ThemeCount; theme++)
            {
                sequence[theme] = theme;
            }

            uint randomState = SequenceSeed;
            int writeIndex = BlocksPerCycle;
            while (writeIndex < sequence.Length)
            {
                int[] cycle = { 0, 1, 2, 3, 4 };
                for (int i = cycle.Length - 1; i > 0; i--)
                {
                    int swapIndex = (int)(NextRandom(ref randomState) % (uint)(i + 1));
                    int value = cycle[i];
                    cycle[i] = cycle[swapIndex];
                    cycle[swapIndex] = value;
                }

                if (cycle[0] == sequence[writeIndex - 1])
                {
                    int value = cycle[0];
                    cycle[0] = cycle[1];
                    cycle[1] = value;
                }

                for (int i = 0; i < cycle.Length && writeIndex < sequence.Length; i++)
                {
                    sequence[writeIndex++] = cycle[i];
                }
            }

            return sequence;
        }

        private static uint NextRandom(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        private static Color C(byte red, byte green, byte blue, byte alpha = 255)
        {
            return new Color32(red, green, blue, alpha);
        }
    }
}
