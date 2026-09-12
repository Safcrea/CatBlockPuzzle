using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatBlockPuzzle
{
    internal sealed class CatMetaCatalog
    {
        public const int CurrentVersion = 1;
        public const int ChapterCount = 10;
        public const int DecorationsPerChapter = 5;
        public const int LevelsPerChapter = 10;
        public const int SupportedLevelCount = ChapterCount * LevelsPerChapter;
        public const string ResourcePath = "CatBlockPuzzle/meta_chapters";

        private readonly CatMetaChapterDefinition[] chapters;
        private readonly Dictionary<string, CatMetaChapterDefinition> chaptersById;
        private readonly Dictionary<string, CatMetaDecorationDefinition> decorationsById;

        public IReadOnlyList<CatMetaChapterDefinition> Chapters => chapters;

        private CatMetaCatalog(CatMetaCatalogData data)
        {
            chapters = new CatMetaChapterDefinition[data.chapters.Length];
            chaptersById = new Dictionary<string, CatMetaChapterDefinition>(StringComparer.Ordinal);
            decorationsById = new Dictionary<string, CatMetaDecorationDefinition>(StringComparer.Ordinal);

            for (int i = 0; i < data.chapters.Length; i++)
            {
                CatMetaChapterDefinition chapter = new CatMetaChapterDefinition(data.chapters[i]);
                chapters[i] = chapter;
                chaptersById.Add(chapter.Id, chapter);
                for (int decorationIndex = 0; decorationIndex < chapter.Decorations.Count; decorationIndex++)
                {
                    CatMetaDecorationDefinition decoration = chapter.Decorations[decorationIndex];
                    decorationsById.Add(decoration.Id, decoration);
                }
            }
        }

        public static CatMetaCatalog Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing meta catalog at Resources/" + ResourcePath + ".json");
            }

            int[] rewards = CatMetaCatalogValidator.LoadConfiguredLevelRewards();
            return FromJson(asset.text, rewards);
        }

        internal static CatMetaCatalog FromData(CatMetaCatalogData data, IReadOnlyList<int> rewards)
        {
            if (!CatMetaCatalogValidator.TryValidate(data, rewards, out string error))
                throw new InvalidOperationException("Invalid prepared meta catalog: " + error);
            return new CatMetaCatalog(data);
        }

        internal static CatMetaCatalog FromJson(string json, IReadOnlyList<int> levelRewards = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Meta catalog JSON is empty.", nameof(json));
            }

            CatMetaCatalogData data;
            try
            {
                data = JsonUtility.FromJson<CatMetaCatalogData>(json);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Meta catalog JSON could not be parsed.", exception);
            }

            if (!CatMetaCatalogValidator.TryValidate(data, levelRewards, out string error))
            {
                throw new InvalidOperationException("Invalid Cat Block Puzzle meta catalog: " + error);
            }

            return new CatMetaCatalog(data);
        }

        public CatMetaChapterDefinition GetChapter(int chapterIndex)
        {
            if (chapterIndex < 0 || chapterIndex >= chapters.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(chapterIndex));
            }

            return chapters[chapterIndex];
        }

        public CatMetaChapterDefinition GetChapterForLevel(int zeroBasedLevelIndex)
        {
            if (zeroBasedLevelIndex < 0 || zeroBasedLevelIndex >= SupportedLevelCount)
            {
                throw new ArgumentOutOfRangeException(nameof(zeroBasedLevelIndex));
            }

            return chapters[zeroBasedLevelIndex / LevelsPerChapter];
        }

        public bool TryGetChapter(string chapterId, out CatMetaChapterDefinition chapter)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                chapter = null;
                return false;
            }

            return chaptersById.TryGetValue(chapterId, out chapter);
        }

        public bool TryGetDecoration(string decorationId, out CatMetaDecorationDefinition decoration)
        {
            if (string.IsNullOrEmpty(decorationId))
            {
                decoration = null;
                return false;
            }

            return decorationsById.TryGetValue(decorationId, out decoration);
        }

        public CatMetaDecorationDefinition GetDecoration(string decorationId)
        {
            if (!TryGetDecoration(decorationId, out CatMetaDecorationDefinition decoration))
            {
                throw new KeyNotFoundException("Unknown decoration id: " + decorationId);
            }

            return decoration;
        }
    }

    internal static class CatMetaCatalogValidator
    {
        private const string LevelResourcePath = "CatBlockPuzzle/levels_100";
        private static readonly int[] RequiredMilestones = { 2, 4, 6, 8, 10 };

        public static bool TryValidate(
            CatMetaCatalogData data,
            IReadOnlyList<int> levelRewards,
            out string error)
        {
            if (!TryValidateStructure(data, out error))
            {
                return false;
            }

            if (levelRewards != null && !TryValidateAffordability(data, levelRewards, out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        public static bool TryValidateAffordability(
            CatMetaCatalogData data,
            IReadOnlyList<int> levelRewards,
            out string error)
        {
            if (data == null || data.chapters == null)
            {
                error = "Catalog structure must be valid before checking affordability.";
                return false;
            }

            if (levelRewards == null || levelRewards.Count != CatMetaCatalog.SupportedLevelCount)
            {
                error = "Affordability requires exactly " + CatMetaCatalog.SupportedLevelCount + " level rewards.";
                return false;
            }

            for (int level = 0; level < levelRewards.Count; level++)
            {
                if (levelRewards[level] < 0)
                {
                    error = "Level " + (level + 1) + " has a negative reward.";
                    return false;
                }
            }

            for (int chapterIndex = 0; chapterIndex < data.chapters.Length; chapterIndex++)
            {
                CatMetaChapterData chapter = data.chapters[chapterIndex];
                int chapterCost = 0;
                int chapterReward = 0;
                for (int localLevel = 0; localLevel < CatMetaCatalog.LevelsPerChapter; localLevel++)
                {
                    chapterReward += levelRewards[chapter.firstLevelIndex + localLevel];
                }

                for (int decorationIndex = 0; decorationIndex < chapter.decorations.Length; decorationIndex++)
                {
                    CatMetaDecorationData decoration = chapter.decorations[decorationIndex];
                    int segmentEnd = chapter.firstLevelIndex + decoration.unlockAfterChapterLevel - 1;
                    int availableFromSegment = levelRewards[segmentEnd - 1] + levelRewards[segmentEnd];
                    if (decoration.cost > availableFromSegment)
                    {
                        error = decoration.id + " costs " + decoration.cost
                            + " but its two-level reward segment grants only " + availableFromSegment + ".";
                        return false;
                    }

                    chapterCost += decoration.cost;
                }

                if (chapterCost > chapterReward)
                {
                    error = chapter.id + " costs " + chapterCost
                        + " but its ten first-clear rewards grant only " + chapterReward + ".";
                    return false;
                }
            }

            error = null;
            return true;
        }

        internal static int[] LoadConfiguredLevelRewards()
        {
            TextAsset asset = Resources.Load<TextAsset>(LevelResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing level rewards at Resources/" + LevelResourcePath + ".json");
            }

            RewardPackData pack;
            try
            {
                pack = JsonUtility.FromJson<RewardPackData>(asset.text);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Level reward JSON could not be parsed.", exception);
            }

            if (pack == null || pack.levels == null || pack.levels.Length != CatMetaCatalog.SupportedLevelCount)
            {
                throw new InvalidOperationException(
                    "Level reward JSON must contain exactly " + CatMetaCatalog.SupportedLevelCount + " levels.");
            }

            int[] rewards = new int[pack.levels.Length];
            for (int i = 0; i < pack.levels.Length; i++)
            {
                rewards[i] = pack.levels[i].reward;
            }

            return rewards;
        }

        private static bool TryValidateStructure(CatMetaCatalogData data, out string error)
        {
            if (data == null)
            {
                error = "JSON could not be parsed.";
                return false;
            }

            if (data.version != CatMetaCatalog.CurrentVersion)
            {
                error = "Expected version " + CatMetaCatalog.CurrentVersion + " but found " + data.version + ".";
                return false;
            }

            if (data.chapters == null || data.chapters.Length != CatMetaCatalog.ChapterCount)
            {
                error = "Catalog must contain exactly " + CatMetaCatalog.ChapterCount + " chapters.";
                return false;
            }

            HashSet<string> chapterIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> backgroundPaths = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> decorationIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> decorationPaths = new HashSet<string>(StringComparer.Ordinal);

            for (int chapterIndex = 0; chapterIndex < data.chapters.Length; chapterIndex++)
            {
                CatMetaChapterData chapter = data.chapters[chapterIndex];
                if (chapter == null)
                {
                    error = "Chapter " + (chapterIndex + 1) + " is null.";
                    return false;
                }

                if (chapter.index != chapterIndex)
                {
                    error = "Chapter at array index " + chapterIndex + " declares index " + chapter.index + ".";
                    return false;
                }

                int expectedFirstLevel = chapterIndex * CatMetaCatalog.LevelsPerChapter;
                int expectedLastLevel = expectedFirstLevel + CatMetaCatalog.LevelsPerChapter - 1;
                if (chapter.firstLevelIndex != expectedFirstLevel || chapter.lastLevelIndex != expectedLastLevel)
                {
                    error = "Chapter " + (chapterIndex + 1) + " must map levels "
                        + (expectedFirstLevel + 1) + "-" + (expectedLastLevel + 1) + ".";
                    return false;
                }

                if (!RequireUniqueText(chapter.id, chapterIds, "chapter id", out error)
                    || !RequireText(chapter.title, "Chapter " + (chapterIndex + 1) + " title", out error)
                    || !RequireText(chapter.catName, "Chapter " + (chapterIndex + 1) + " cat name", out error)
                    || !RequireText(chapter.startStory, chapter.id + " start story", out error)
                    || !RequireText(chapter.completionStory, chapter.id + " completion story", out error)
                    || !RequireUniqueText(chapter.backgroundResourcePath, backgroundPaths, "background path", out error))
                {
                    return false;
                }

                string expectedBackgroundSuffix = "/room_" + (chapterIndex + 1).ToString("00");
                if (!chapter.backgroundResourcePath.EndsWith(expectedBackgroundSuffix, StringComparison.Ordinal))
                {
                    error = chapter.id + " background must end with " + expectedBackgroundSuffix + ".";
                    return false;
                }

                if (!RequireText(chapter.thumbnailResourcePath, chapter.id + " thumbnail path", out error))
                {
                    return false;
                }

                if (chapter.catPortraitIndex < 0)
                {
                    error = chapter.id + " cat portrait index cannot be negative.";
                    return false;
                }

                if (chapter.decorations == null || chapter.decorations.Length != CatMetaCatalog.DecorationsPerChapter)
                {
                    error = chapter.id + " must contain exactly " + CatMetaCatalog.DecorationsPerChapter + " decorations.";
                    return false;
                }

                for (int decorationIndex = 0; decorationIndex < chapter.decorations.Length; decorationIndex++)
                {
                    CatMetaDecorationData decoration = chapter.decorations[decorationIndex];
                    if (decoration == null)
                    {
                        error = chapter.id + " decoration " + (decorationIndex + 1) + " is null.";
                        return false;
                    }

                    if (!RequireUniqueText(decoration.id, decorationIds, "decoration id", out error)
                        || !RequireText(decoration.displayName, decoration.id + " display name", out error)
                        || !RequireUniqueText(decoration.spriteResourcePath, decorationPaths, "decoration sprite path", out error))
                    {
                        return false;
                    }

                    if (!decoration.spriteResourcePath.StartsWith("CatBlockPuzzle/Art/Decorations/", StringComparison.Ordinal))
                    {
                        error = decoration.id + " must use the flat CatBlockPuzzle decoration resource folder.";
                        return false;
                    }

                    if (decoration.cost < 0)
                    {
                        error = decoration.id + " cost cannot be negative.";
                        return false;
                    }

                    if (decoration.unlockAfterChapterLevel != RequiredMilestones[decorationIndex])
                    {
                        error = decoration.id + " must unlock after chapter level "
                            + RequiredMilestones[decorationIndex] + ".";
                        return false;
                    }

                    if (!IsNormalized(decoration.hotspot))
                    {
                        error = decoration.id + " hotspot must be normalized to 0-1.";
                        return false;
                    }

                    if (decoration.size.x <= 0f || decoration.size.x > 1f
                        || decoration.size.y <= 0f || decoration.size.y > 1f)
                    {
                        error = decoration.id + " size must be normalized and greater than zero.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        private static bool RequireText(string value, string label, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = label + " is missing.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool RequireUniqueText(
            string value,
            HashSet<string> values,
            string label,
            out string error)
        {
            if (!RequireText(value, label, out error))
            {
                return false;
            }

            if (!values.Add(value))
            {
                error = "Duplicate " + label + ": " + value + ".";
                return false;
            }

            return true;
        }

        private static bool IsNormalized(Vector2 value)
        {
            return value.x >= 0f && value.x <= 1f && value.y >= 0f && value.y <= 1f;
        }

        [Serializable]
        private sealed class RewardPackData
        {
            public RewardLevelData[] levels;
        }

        [Serializable]
        private sealed class RewardLevelData
        {
            public int reward;
        }
    }
}
