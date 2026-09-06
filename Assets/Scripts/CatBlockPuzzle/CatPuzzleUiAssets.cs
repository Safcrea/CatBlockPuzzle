using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Visuals baked in the editor. Loading these assets does not generate sprites or audio.</summary>
    public sealed class CatPuzzleUiAssets : ScriptableObject
    {
        public Sprite White;
        public Sprite RoundedBox;
        public Sprite Circle;
        public Sprite Coin;
        public Sprite CatHead;
        public Sprite Mouth;
        public Sprite Tail;
        public Sprite Paw;
        public Sprite Star;
        public Sprite StarOutline;
        public Sprite Background;
        public Sprite[] Icons = new Sprite[6];
        public Sprite[] Portraits = new Sprite[24];
        public Sprite[] ThemeBackgrounds = new Sprite[CatPuzzleThemeCatalog.ThemeCount];
        public AudioClip[] Sounds = new AudioClip[4];
    }
}
