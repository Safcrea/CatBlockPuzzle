using UnityEngine;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class GameplayPresentationAssets : MonoBehaviour
    {
        /// <summary>Distinct cat portraits; the sprite array holds this many per mood.</summary>
        internal const int CatCount = 9;

        [Header("Shared UI")]
        [SerializeField] internal Font defaultFont;
        [SerializeField] internal Sprite whiteSprite;
        [SerializeField] internal Sprite roundedBoxSprite;
        [SerializeField] internal Sprite circleSprite;
        [SerializeField] internal Sprite coinSprite;
        [SerializeField] internal Sprite catHeadSprite;
        [SerializeField] internal Sprite mouthSprite;
        [SerializeField] internal Sprite tailSprite;
        [SerializeField] internal Sprite pawSprite;
        [SerializeField] internal Sprite starSprite;
        [SerializeField] internal Sprite starOutlineSprite;

        [Header("Icons")]
        [SerializeField] internal Sprite backIconSprite;
        [SerializeField] internal Sprite pauseIconSprite;
        [SerializeField] internal Sprite settingsIconSprite;
        [SerializeField] internal Sprite hintIconSprite;
        [SerializeField] internal Sprite resetIconSprite;
        [SerializeField] internal Sprite closeIconSprite;

        [Header("Cats and Themes")]
        [Tooltip("The head tile of each cat: the rounded block with its face, one per cat per mood.")]
        [SerializeField] internal Sprite[] catPortraitSprites = new Sprite[CatCount * 3];
        [Tooltip("The plain block every cell of a piece other than the head draws, one per cat.")]
        [SerializeField] internal Sprite[] catBodySprites = new Sprite[CatCount];
        [SerializeField] internal Sprite[] themeBackgroundSprites = new Sprite[CatPuzzleThemeCatalog.ThemeCount];
        [SerializeField] internal CatVisualCatalog visualCatalog;
        [SerializeField] internal PortraitLayoutProfile layoutProfile;

        [Header("Tray")]
        [Tooltip("Skin copied onto every tray slot when a level loads (background, shadow, border, drag dots). Leave empty for the original cream cards.")]
        [SerializeField] internal GameObject traySlotSkin;

        public bool Validate(out string error)
        {
            error = null;
            if (layoutProfile == null || defaultFont == null)
                error = "the layout profile or default font is missing";
            else if (catPortraitSprites == null || catPortraitSprites.Length != CatCount * 3 ||
                catBodySprites == null || catBodySprites.Length != CatCount ||
                themeBackgroundSprites == null || themeBackgroundSprites.Length != CatPuzzleThemeCatalog.ThemeCount ||
                whiteSprite == null || roundedBoxSprite == null || circleSprite == null || coinSprite == null ||
                pawSprite == null || starSprite == null || starOutlineSprite == null)
                error = "the baked presentation artwork is incomplete";
            if (error != null) return false;
            foreach (Sprite sprite in catPortraitSprites) if (sprite == null) { error = "a cat portrait is missing"; return false; }
            foreach (Sprite sprite in catBodySprites) if (sprite == null) { error = "a cat body tile is missing"; return false; }
            foreach (Sprite sprite in themeBackgroundSprites) if (sprite == null) { error = "a theme sprite is missing"; return false; }
            return true;
        }
    }
}
