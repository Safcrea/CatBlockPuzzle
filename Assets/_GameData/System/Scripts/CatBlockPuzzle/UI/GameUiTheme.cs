using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>The shared visual assets for every game screen and runtime overlay.</summary>
    [CreateAssetMenu(menuName = "Cat Block Puzzle/Game UI Theme")]
    public sealed class GameUiTheme : ScriptableObject
    {
        public Font font;
        public Sprite background, panel, goldPanel, icePanel, board, tile;
        public Sprite primaryButton, secondaryButton, dangerButton, blueButton;
        public Sprite coin, hint, freeze, rotate, pause, clock, close, back, pointer, lockIcon;
        public Sprite star, emptyStar, happyCat, sadCat;
        public Color ink = new Color(.38f, .153f, .035f);
        public Color mutedInk = new Color(.58f, .35f, .18f);
        private static GameUiTheme active;
        public static GameUiTheme Active
        {
            get
            {
                if (active == null) active = Resources.Load<GameUiTheme>("CatBlockPuzzle/GameUiTheme");
                return active;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => active = null;

        public static void Skin(Image image, Sprite sprite, bool sliced = true)
        {
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.color = Color.white;
        }

        public void ApplyLevel(CatPuzzleLevelView view)
        {
            if (view == null) return;
            Skin(view.boardFrameImage, board);
            if (view.boardOutline != null) view.boardOutline.effectColor = Color.clear;
            if (view.boardEars != null) foreach (var ear in view.boardEars) if (ear != null) ear.enabled = false;
            Skin(view.trayImage, panel);
            if (view.cells != null) foreach (var cell in view.cells)
            {
                Skin(cell.image, tile);
                if (cell.image != null && !cell.active) cell.image.color = Color.clear;
                if (cell.preview != null) { cell.preview.sprite = tile; cell.preview.type = Image.Type.Sliced; }
                if (cell.shine != null && cell.shine.TryGetComponent<Image>(out var shine)) shine.enabled = false;
            }
            if (view.pieces != null) foreach (var piece in view.pieces)
                if (piece != null) Skin(piece.slotImage, tile);
        }
    }
}
