using UnityEngine;
using static CatBlockPuzzle.RectLayout;

namespace CatBlockPuzzle
{
    /// <summary>Sizes a piece's cell grid and lays out each cat inside its cell.</summary>
    internal sealed class PieceLayout
    {
        private readonly PuzzleSession session;

        public PieceLayout(PuzzleSession session)
        {
            this.session = session;
        }

        public void SetPieceGrid(PieceState state, float cellWidth, float gapX, float cellHeight, float gapY)
        {
            bool resting = state.Placed && !session.IsDragging(state);
            if (state.HasGridLayout &&
                state.RestingLayout == resting &&
                Mathf.Approximately(state.CellWidth, cellWidth) &&
                Mathf.Approximately(state.CellHeight, cellHeight) &&
                Mathf.Approximately(state.GapX, gapX) &&
                Mathf.Approximately(state.GapY, gapY))
            {
                return;
            }

            state.HasGridLayout = true;
            state.RestingLayout = resting;
            state.CellWidth = cellWidth;
            state.CellHeight = cellHeight;
            state.GapX = gapX;
            state.GapY = gapY;
            float width = (state.Definition.Cols * cellWidth) + ((state.Definition.Cols - 1) * gapX);
            float height = (state.Definition.Rows * cellHeight) + ((state.Definition.Rows - 1) * gapY);
            state.Rect.sizeDelta = new Vector2(width, height);

            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                RectTransform cellRect = state.CellImages[i].rectTransform;
                cellRect.anchorMin = new Vector2(0f, 1f);
                cellRect.anchorMax = new Vector2(0f, 1f);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.sizeDelta = new Vector2(cellWidth, cellHeight);
                cellRect.anchoredPosition = new Vector2(
                    (cell.Col * (cellWidth + gapX)) + (cellWidth * 0.5f),
                    -((cell.Row * (cellHeight + gapY)) + (cellHeight * 0.5f)));
                LayoutCatCell(state.CatViews[i], cellWidth, cellHeight, resting);
            }
        }

        public void LayoutCatCell(CatCellView view, float width, float height, bool resting)
        {
            if (view.Authored)
            {
                view.Portrait.preserveAspect = true;
                return;
            }

            float min = Mathf.Min(width, height);
            SetCenteredChild(view.LeftEar, new Vector2(width * -0.26f, height * 0.43f), new Vector2(min * 0.32f, min * 0.32f));
            SetCenteredChild(view.RightEar, new Vector2(width * 0.26f, height * 0.43f), new Vector2(min * 0.32f, min * 0.32f));
            view.LeftEar.localEulerAngles = new Vector3(0f, 0f, 45f);
            view.RightEar.localEulerAngles = new Vector3(0f, 0f, 45f);
            SetCenteredChild(view.LeftInnerEar, new Vector2(width * -0.26f, height * 0.43f), new Vector2(min * 0.16f, min * 0.16f));
            SetCenteredChild(view.RightInnerEar, new Vector2(width * 0.26f, height * 0.43f), new Vector2(min * 0.16f, min * 0.16f));
            view.LeftInnerEar.localEulerAngles = new Vector3(0f, 0f, 45f);
            view.RightInnerEar.localEulerAngles = new Vector3(0f, 0f, 45f);
            SetCenteredChild(view.Highlight, new Vector2(width * -0.08f, height * 0.08f), new Vector2(width * 0.72f, height * 0.72f));
            view.Highlight.localEulerAngles = Vector3.zero;

            float eyeHeight = resting ? Mathf.Max(2f, height * 0.04f) : height * 0.14f;
            SetCenteredChild(view.LeftEye, new Vector2(width * -0.12f, height * 0.05f), new Vector2(width * 0.09f, eyeHeight));
            SetCenteredChild(view.RightEye, new Vector2(width * 0.12f, height * 0.05f), new Vector2(width * 0.09f, eyeHeight));
            SetCenteredChild(view.Nose, new Vector2(0f, height * -0.06f), new Vector2(width * 0.1f, height * 0.08f));
            SetCenteredChild(view.Mouth, new Vector2(0f, height * -0.2f), new Vector2(width * 0.28f, height * 0.14f));
            SetCenteredChild(view.LeftCheek, new Vector2(width * -0.23f, height * -0.12f), new Vector2(width * 0.13f, height * 0.08f));
            SetCenteredChild(view.RightCheek, new Vector2(width * 0.23f, height * -0.12f), new Vector2(width * 0.13f, height * 0.08f));
            SetCenteredChild(view.LeftWhiskerTop, new Vector2(width * -0.31f, height * -0.08f), new Vector2(width * 0.22f, Mathf.Max(2f, min * 0.025f)));
            SetCenteredChild(view.LeftWhiskerBottom, new Vector2(width * -0.31f, height * -0.17f), new Vector2(width * 0.2f, Mathf.Max(2f, min * 0.022f)));
            SetCenteredChild(view.RightWhiskerTop, new Vector2(width * 0.31f, height * -0.08f), new Vector2(width * 0.22f, Mathf.Max(2f, min * 0.025f)));
            SetCenteredChild(view.RightWhiskerBottom, new Vector2(width * 0.31f, height * -0.17f), new Vector2(width * 0.2f, Mathf.Max(2f, min * 0.022f)));
            view.LeftWhiskerTop.localEulerAngles = new Vector3(0f, 0f, 8f);
            view.LeftWhiskerBottom.localEulerAngles = new Vector3(0f, 0f, -8f);
            view.RightWhiskerTop.localEulerAngles = new Vector3(0f, 0f, -8f);
            view.RightWhiskerBottom.localEulerAngles = new Vector3(0f, 0f, 8f);
            SetCenteredChild(view.ForeheadStripe, new Vector2(0f, height * 0.25f), new Vector2(width * 0.18f, Mathf.Max(2f, min * 0.03f)));
            view.ForeheadStripe.localEulerAngles = new Vector3(0f, 0f, 90f);
            SetCenteredChild(view.Tail, new Vector2(width * 0.55f, height * -0.27f), new Vector2(width * 0.34f, height * 0.22f));
            view.Tail.localEulerAngles = new Vector3(0f, 0f, -13f);
        }
    }
}
