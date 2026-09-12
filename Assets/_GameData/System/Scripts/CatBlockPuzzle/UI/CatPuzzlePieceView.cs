using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed class CatPuzzlePieceView : MonoBehaviour
    {
        public int pieceIndex;
        public RectTransform slot;
        public Image slotImage;
        public LayoutElement slotLayout;
        public RectTransform visual;
        public Image[] cats;
        public CatPuzzlePieceDragView slotInput;
        public CatPuzzlePieceDragView pieceInput;
    }
}
