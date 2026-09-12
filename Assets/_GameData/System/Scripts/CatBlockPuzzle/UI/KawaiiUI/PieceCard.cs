using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle.KawaiiUI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class PieceCard : MonoBehaviour
    {
        [SerializeField] private RectTransform pieceHost;

        private Image background;

        public RectTransform PieceHost
        {
            get
            {
                EnsureVisual();
                return pieceHost;
            }
        }

        private void Awake()
        {
            EnsureVisual();
        }

        public void EnsureVisual()
        {
            background = KawaiiUIRuntime.EnsureImage(gameObject);
            background.sprite = KawaiiSprites.RoundedRect;
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            KawaiiUIRuntime.EnsureShadow(background, new Vector2(0f, -7f), 0.12f);
            KawaiiUIRuntime.EnsureOutline(background, KawaiiPalette.WithAlpha(KawaiiPalette.SoftBeige, 0.35f), new Vector2(2f, -2f));

            if (pieceHost == null)
            {
                Transform existing = transform.Find("Piece Host");
                if (existing != null)
                {
                    pieceHost = existing.GetComponent<RectTransform>();
                }
                else
                {
                    pieceHost = KawaiiUIRuntime.CreateRect(transform, "Piece Host");
                }
            }

            KawaiiUIRuntime.SetRect(pieceHost, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(-18f, -58f));
            EnsureDots();
        }

        private void EnsureDots()
        {
            RectTransform dotsRoot;
            Transform existing = transform.Find("Drag Dots");
            if (existing != null)
            {
                dotsRoot = existing.GetComponent<RectTransform>();
            }
            else
            {
                dotsRoot = KawaiiUIRuntime.CreateRect(transform, "Drag Dots");
            }

            KawaiiUIRuntime.SetRect(dotsRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(72f, 16f));

            while (dotsRoot.childCount < 3)
            {
                Image dot = KawaiiUIRuntime.CreateImage(dotsRoot, "Dot " + dotsRoot.childCount, KawaiiPalette.WithAlpha(KawaiiPalette.SoftBeige, 0.85f));
                dot.sprite = KawaiiSprites.Circle;
                dot.raycastTarget = false;
            }

            for (int i = 0; i < dotsRoot.childCount; i++)
            {
                RectTransform dotRect = dotsRoot.GetChild(i).GetComponent<RectTransform>();
                KawaiiUIRuntime.SetRect(dotRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 20f, 0f), new Vector2(8f, 8f));
            }
        }
    }
}
