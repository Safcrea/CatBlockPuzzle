using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatBlockPuzzle
{
    [Serializable]
    internal sealed class CatMetaCatalogData
    {
        public int version;
        public CatMetaChapterData[] chapters;
    }

    [Serializable]
    internal sealed class CatMetaChapterData
    {
        public int index;
        public string id;
        public string title;
        public string catName;
        public string startStory;
        public string completionStory;
        public int firstLevelIndex;
        public int lastLevelIndex;
        public string backgroundResourcePath;
        public string thumbnailResourcePath;
        public int catPortraitIndex;
        public CatMetaDecorationData[] decorations;
    }

    [Serializable]
    internal sealed class CatMetaDecorationData
    {
        public string id;
        public string displayName;
        public int cost;
        public int unlockAfterChapterLevel;
        public string spriteResourcePath;
        public Vector2 hotspot;
        public Vector2 size;
        public int sortOrder;
    }

    internal sealed class CatMetaChapterDefinition
    {
        private readonly CatMetaDecorationDefinition[] decorations;

        public int Index { get; }
        public string Id { get; }
        public string Title { get; }
        public string CatName { get; }
        public string StartStory { get; }
        public string CompletionStory { get; }
        public int FirstLevelIndex { get; }
        public int LastLevelIndex { get; }
        public string BackgroundResourcePath { get; }
        public string ThumbnailResourcePath { get; }
        public int CatPortraitIndex { get; }
        public IReadOnlyList<CatMetaDecorationDefinition> Decorations => decorations;

        public int LevelCount => (LastLevelIndex - FirstLevelIndex) + 1;

        internal CatMetaChapterDefinition(CatMetaChapterData data)
        {
            Index = data.index;
            Id = data.id;
            Title = data.title;
            CatName = data.catName;
            StartStory = data.startStory;
            CompletionStory = data.completionStory;
            FirstLevelIndex = data.firstLevelIndex;
            LastLevelIndex = data.lastLevelIndex;
            BackgroundResourcePath = data.backgroundResourcePath;
            ThumbnailResourcePath = data.thumbnailResourcePath;
            CatPortraitIndex = data.catPortraitIndex;

            CatMetaDecorationData[] source = data.decorations ?? Array.Empty<CatMetaDecorationData>();
            decorations = new CatMetaDecorationDefinition[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                decorations[i] = new CatMetaDecorationDefinition(Index, source[i]);
            }
        }
    }

    internal sealed class CatMetaDecorationDefinition
    {
        public int ChapterIndex { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public int Cost { get; }
        public int UnlockAfterChapterLevel { get; }
        public string SpriteResourcePath { get; }
        public Vector2 Hotspot { get; }
        public Vector2 Size { get; }
        public int SortOrder { get; }

        internal CatMetaDecorationDefinition(int chapterIndex, CatMetaDecorationData data)
        {
            ChapterIndex = chapterIndex;
            Id = data.id;
            DisplayName = data.displayName;
            Cost = data.cost;
            UnlockAfterChapterLevel = data.unlockAfterChapterLevel;
            SpriteResourcePath = data.spriteResourcePath;
            Hotspot = data.hotspot;
            Size = data.size;
            SortOrder = data.sortOrder;
        }
    }

    internal static class CatMetaStoryIds
    {
        public static string Intro(string chapterId)
        {
            return chapterId + ".intro";
        }

        public static string TrustMilestone(string chapterId)
        {
            return chapterId + ".trust";
        }

        public static string Completion(string chapterId)
        {
            return chapterId + ".complete";
        }
    }
}
