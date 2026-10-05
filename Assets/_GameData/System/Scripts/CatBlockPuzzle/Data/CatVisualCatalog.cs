using UnityEngine;

namespace CatBlockPuzzle
{
    [CreateAssetMenu(fileName = "CatVisualCatalog", menuName = "Cat Block Puzzle/Visual Catalog")]
    public sealed class CatVisualCatalog : ScriptableObject
    {
        public Texture2D CozyRoomBackground;
        public Texture2D ThemeAtlas;

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

            return catalog;
        }
    }

}
