using System;
using UnityEngine;

namespace CatBlockPuzzle
{
    public sealed class CatPuzzleContentCatalog : ScriptableObject
    {
        [SerializeField] internal LevelPackData levelPack;
        [SerializeField] internal CatMetaCatalogData metaData;
        [SerializeField] public string sourceFingerprint;
        [SerializeField] public Sprite[] roomBackgrounds;
        [SerializeField] public Sprite[] roomThumbnails;
        [SerializeField] public DecorationArt[] decorations;
        // Resource addresses, not direct prefab references: only the requested level asset is loaded.
        [SerializeField] public string[] levelPrefabResources;

        [Serializable]
        public struct DecorationArt
        {
            public string id;
            public Sprite sprite;
        }

        public int LevelCount => levelPack?.levels?.Length ?? 0;
        internal LevelManager CreateLevelManager() => new LevelManager(levelPack.levels);
        internal CatMetaCatalog CreateMetaCatalog()
        {
            int[] rewards = new int[LevelCount];
            for (int i = 0; i < rewards.Length; i++) rewards[i] = levelPack.levels[i].reward;
            return CatMetaCatalog.FromData(metaData, rewards);
        }
    }
}
