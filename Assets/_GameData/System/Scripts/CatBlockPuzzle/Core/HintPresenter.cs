using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>Pulses the next misplaced piece and its solution cells for a moment.</summary>
    internal sealed class HintPresenter : HostedService
    {
        private readonly PuzzleSession session;
        private readonly BoardController board;
        private readonly MovePieceTip tip;
        private readonly CellLockController locks;
        private Coroutine hintRoutine;

        public HintPresenter(IGameplayContext context, PuzzleSession session, BoardController board, MovePieceTip tip, CellLockController locks) : base(context)
        {
            this.locks = locks;
            this.session = session;
            this.board = board;
            this.tip = tip;
        }

        private List<PieceState> pieces => session.pieces;

        public void ShowHint() => ShowHint(HintReveal.FullShape);

        public void ShowHint(HintReveal reveal)
        {
            if (session.inputLocked)
            {
                return;
            }

            StopHint();
            PieceState hinted = null;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state.Definition.IsSpare) continue;
                if (locks.BlocksSolution(state.Definition)) continue;
                if (!state.Placed && state.Slot != null && !state.Slot.gameObject.activeSelf) continue;
                if (!state.Placed || state.Row != state.Definition.SolutionRow || state.Col != state.Definition.SolutionCol)
                {
                    hinted = state;
                    break;
                }
            }

            if (hinted != null)
            {
                hintRoutine = StartCoroutine(HintRoutine(hinted, reveal));
            }
        }

        private IEnumerator HintRoutine(PieceState state, HintReveal reveal)
        {
            List<Image> hintedCells = new List<Image>();
            CellOffset[] cells = state.Definition.Cells;
            int revealed = reveal == HintReveal.AnchorOnly ? Mathf.Min(1, cells.Length) : cells.Length;
            for (int c = 0; c < revealed; c++)
            {
                CellOffset cell = cells[c];
                Vector2Int coord = new Vector2Int(state.Definition.SolutionRow + cell.Row, state.Definition.SolutionCol + cell.Col);
                if (session.boardCells.TryGetValue(coord, out CellView cellView))
                {
                    hintedCells.Add(cellView.Image);
                }
            }

            float elapsed = 0f;
            while (elapsed < 1.7f)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(elapsed * 11f) * 0.5f + 0.5f;
                state.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, wave);
                state.SlotImage.color = Color.Lerp(CardRestColor, new Color(1f, 0.92f, 0.55f, 0.92f), wave);
                for (int i = 0; i < hintedCells.Count; i++)
                {
                    hintedCells[i].color = Color.Lerp(TargetColor, GoldColor, wave);
                }

                yield return null;
            }

            state.Rect.localScale = Vector3.one;
            state.SlotImage.color = state.Placed ? CardDimColor : CardRestColor;
            for (int i = 0; i < hintedCells.Count; i++)
            {
                hintedCells[i].color = TargetColor;
            }

            hintRoutine = null;
        }

        /// <summary>Ends the hint pulse and the move-piece tip, restoring every piece and cell.</summary>
        public void StopHint()
        {
            tip.Stop();
            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].Rect.localScale = Vector3.one;
                pieces[i].SlotImage.color = pieces[i].Placed ? CardDimColor : CardRestColor;
            }

            board.ResetCellColors();
        }
    }
}
