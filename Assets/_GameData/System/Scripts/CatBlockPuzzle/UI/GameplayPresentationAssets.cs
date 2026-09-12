using UnityEngine;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class GameplayPresentationAssets : MonoBehaviour
    {
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
        [SerializeField] internal Sprite[] catPortraitSprites = new Sprite[24];
        [SerializeField] internal Sprite[] themeBackgroundSprites = new Sprite[CatPuzzleThemeCatalog.ThemeCount];
        [SerializeField] internal CatVisualCatalog visualCatalog;
        [SerializeField] internal PortraitLayoutProfile layoutProfile;

        public bool Validate(out string error)
        {
            error = null;
            if (layoutProfile == null || defaultFont == null)
                error = "the layout profile or default font is missing";
            else if (catPortraitSprites == null || catPortraitSprites.Length != 24 ||
                themeBackgroundSprites == null || themeBackgroundSprites.Length != CatPuzzleThemeCatalog.ThemeCount ||
                whiteSprite == null || roundedBoxSprite == null || circleSprite == null || coinSprite == null ||
                pawSprite == null || starSprite == null || starOutlineSprite == null)
                error = "the baked presentation artwork is incomplete";
            if (error != null) return false;
            foreach (Sprite sprite in catPortraitSprites) if (sprite == null) { error = "a cat portrait is missing"; return false; }
            foreach (Sprite sprite in themeBackgroundSprites) if (sprite == null) { error = "a theme sprite is missing"; return false; }
            return true;
        }
    }
}
