using UnityEngine;

namespace CatBlockPuzzle
{
    [CreateAssetMenu(fileName = "CatVisualCatalog", menuName = "Cat Block Puzzle/Visual Catalog")]
    public sealed class CatVisualCatalog : ScriptableObject
    {
        public Texture2D CozyRoomBackground;
        public Texture2D ThemeAtlas;
        public Texture2D NeutralCatAtlas;
        public Texture2D HappyCatAtlas;
        public Texture2D WorriedCatAtlas;

        internal static CatVisualCatalog LoadOrCreate()
        {
            CatVisualCatalog catalog = Resources.Load<CatVisualCatalog>("CatBlockPuzzle/cat_visual_catalog");
            if (catalog == null)
            {
                catalog = CreateInstance<CatVisualCatalog>();
            }

            if (catalog.CozyRoomBackground == null)
            {
                catalog.CozyRoomBackground = Resources.Load<Texture2D>("CatBlockPuzzle/Art/cozy_room_background");
            }

            if (catalog.ThemeAtlas == null)
            {
                catalog.ThemeAtlas = Resources.Load<Texture2D>("CatBlockPuzzle/Art/Themes/theme_atlas");
            }

            if (catalog.NeutralCatAtlas == null)
            {
                catalog.NeutralCatAtlas = Resources.Load<Texture2D>("CatBlockPuzzle/Art/cat_portraits");
            }

            if (catalog.HappyCatAtlas == null)
            {
                catalog.HappyCatAtlas = Resources.Load<Texture2D>("CatBlockPuzzle/Art/cat_portraits_happy");
            }

            if (catalog.WorriedCatAtlas == null)
            {
                catalog.WorriedCatAtlas = Resources.Load<Texture2D>("CatBlockPuzzle/Art/cat_portraits_worried");
            }

            return catalog;
        }
    }

}
