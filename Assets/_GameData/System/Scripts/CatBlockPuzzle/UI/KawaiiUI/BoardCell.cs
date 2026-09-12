using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle.KawaiiUI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardCell : MonoBehaviour
    {
        [SerializeField] private int row;
        [SerializeField] private int column;
        [SerializeField] private bool emptyTarget;

        private Image background;
        private Image pawIcon;
        private Image glow;

        public int Row
        {
            get { return row; }
        }

        public int Column
        {
            get { return column; }
        }

        public bool EmptyTarget
        {
            get { return emptyTarget; }
        }

        private void Awake()
        {
            ApplyVisual();
        }

        public void Configure(int targetRow, int targetColumn, bool isEmptyTarget)
        {
            row = targetRow;
            column = targetColumn;
            emptyTarget = isEmptyTarget;
            name = "Cell " + row + "," + column;
            ApplyVisual();
        }

        public void SetHover(bool active, bool valid)
        {
            EnsureVisuals();
            glow.gameObject.SetActive(active);
            glow.color = valid ? KawaiiPalette.WithAlpha(KawaiiPalette.ValidGlow, 0.54f) : KawaiiPalette.WithAlpha(KawaiiPalette.InvalidGlow, 0.52f);
        }

        public void SetOccupied(bool occupied)
        {
            EnsureVisuals();
            pawIcon.gameObject.SetActive(!emptyTarget && !occupied);
            background.color = occupied ? KawaiiPalette.WithAlpha(KawaiiPalette.BoardTileGreen, 0.42f) : CellColor();
        }

        public void ApplyVisual()
        {
            EnsureVisuals();
            background.sprite = KawaiiSprites.RoundedRect;
            background.type = Image.Type.Sliced;
            background.color = CellColor();
            background.raycastTarget = false;

            KawaiiUIRuntime.EnsureShadow(background, new Vector2(0f, -4f), emptyTarget ? 0.07f : 0.12f);
            KawaiiUIRuntime.EnsureOutline(background, emptyTarget ? KawaiiPalette.WithAlpha(KawaiiPalette.SoftBeige, 0.62f) : new Color(1f, 1f, 1f, 0.32f), new Vector2(2f, -2f));

            pawIcon.sprite = KawaiiSprites.Paw;
            pawIcon.color = emptyTarget ? KawaiiPalette.WithAlpha(KawaiiPalette.TextDarkBrown, 0.08f) : new Color(1f, 1f, 1f, 0.26f);
            pawIcon.raycastTarget = false;
            pawIcon.gameObject.SetActive(!emptyTarget);

            glow.sprite = KawaiiSprites.RoundedRect;
            glow.type = Image.Type.Sliced;
            glow.raycastTarget = false;
            glow.gameObject.SetActive(false);

            RectTransform pawRect = pawIcon.rectTransform;
            KawaiiUIRuntime.SetRect(pawRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62f, 62f));
            KawaiiUIRuntime.Stretch(glow.rectTransform);
        }

        private void EnsureVisuals()
        {
            if (background == null)
            {
                background = KawaiiUIRuntime.EnsureImage(gameObject);
            }

            if (pawIcon == null)
            {
                pawIcon = FindOrCreateImage("Paw Icon");
            }

            if (glow == null)
            {
                glow = FindOrCreateImage("Hover Glow");
                glow.transform.SetAsFirstSibling();
            }
        }

        private Image FindOrCreateImage(string childName)
        {
            Transform existing = transform.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName, typeof(RectTransform), typeof(Image));
            if (existing == null)
            {
                go.transform.SetParent(transform, false);
            }

            return go.GetComponent<Image>();
        }

        private Color CellColor()
        {
            return emptyTarget ? KawaiiPalette.EmptyCellCream : KawaiiPalette.BoardTileGreen;
        }
    }
}
