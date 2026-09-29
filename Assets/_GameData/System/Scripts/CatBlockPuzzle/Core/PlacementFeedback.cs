using System.Collections;
using UnityEngine;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>Sound, haptics, mood and particle reactions to picking up, dropping and placing pieces.</summary>
    internal sealed class PlacementFeedback : HostedService
    {
        private readonly PuzzleSession session;
        private readonly FxSpawner fx;
        private readonly HapticsController haptics;
        private readonly CanvasSpace space;
        private readonly BoardController board;
        private readonly CatMoodPresenter mood;
        private readonly ComboTracker combo;
        private readonly PlayerSettings settings;

        public PlacementFeedback(IGameplayContext context, PuzzleSession session, FxSpawner fx, HapticsController haptics,
            CanvasSpace space, BoardController board, CatMoodPresenter mood, ComboTracker combo, PlayerSettings settings) : base(context)
        {
            this.session = session;
            this.fx = fx;
            this.haptics = haptics;
            this.space = space;
            this.board = board;
            this.mood = mood;
            this.combo = combo;
            this.settings = settings;
        }

        private bool reducedMotion => settings.ReducedMotion;

        public void PlayPickup(PieceState state, Vector2 screenPosition)
        {
            PuzzleAudio.Play("Button");
            haptics?.PlayPickup();
            Color accent = Color.Lerp(state.Color, Color.white, 0.28f);
            fx.SpawnSnapRing(screenPosition, accent);
            fx.SpawnFixedBurst(screenPosition, reducedMotion ? 2 : 7, accent);
            if (state.Slot != null)
            {
                StartCoroutine(fx.PopTransform(state.Slot, 1.025f));
            }
        }

        public void PlaySnap(PieceState state, int row, int col)
        {
            PuzzleAudio.Play("Snap");
            haptics.PlaySnap();
            Vector2 centerScreen = board.BoardPieceCenterScreen(state, row, col);
            fx.SpawnSnapRing(centerScreen, state.Color);
            StartCoroutine(SpawnPlacementEcho(centerScreen, state.Color));
            FlashPlacedCells(state, row, col);
            fx.SpawnPawBurst(session.boardRoot, board.BoardPieceCenterInBoard(state, row, col), reducedMotion ? 4 : 13, state.Color);
            StartCoroutine(fx.PopTransform(session.boardRoot, 1.015f));
        }

        private IEnumerator SpawnPlacementEcho(Vector2 screenPosition, Color color)
        {
            if (reducedMotion)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.065f);
            fx.SpawnSnapRing(screenPosition, Color.Lerp(color, Color.white, 0.32f));
        }

        public void PlayWrong(PieceState state)
        {
            combo.Reset();
            mood.ShowWorried(state);
            PuzzleAudio.Play("InvalidPlacement");
            haptics.PlayWrongMove();
            fx.SpawnFixedBurst(space.WorldToScreen(state.Rect.position), 12, InvalidColor);
            StartCoroutine(fx.ShakeTransform(state.Rect));
        }

        public void PlayShelfReturn()
        {
            PuzzleAudio.Play("Button");
            StartCoroutine(fx.PopTransform(session.trayRoot, 1.02f));
        }

        /// <summary>The landed cat looks happy for a beat; first-try placements also extend the combo.</summary>
        public void RegisterValidPlacement(PieceState state, bool countForCombo)
        {
            mood.ShowHappy(state);
            if (!countForCombo)
            {
                return;
            }

            combo.Register();
        }

        private void FlashPlacedCells(PieceState state, int row, int col)
        {
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                Vector2Int coord = new Vector2Int(row + cell.Row, col + cell.Col);
                if (session.boardCells.TryGetValue(coord, out CellView cellView))
                {
                    StartCoroutine(fx.FlashCell(cellView.Image, i * 0.025f));
                }
            }
        }
    }
}
