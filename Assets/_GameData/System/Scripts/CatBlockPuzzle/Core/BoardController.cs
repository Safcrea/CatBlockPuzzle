using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.PuzzleTuning;
using static CatBlockPuzzle.RectLayout;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The puzzle board: sizing the grid inside the authored area, cell coordinates,
    /// occupancy and placement validity, drop previews, the level-start reveal, and
    /// parenting a landed piece into the board.
    /// </summary>
    internal sealed class BoardController : HostedService
    {
        private readonly PuzzleSession session;
        private readonly CanvasSpace space;
        private readonly PieceLayout pieceLayout;
        private readonly TrayLayout tray;
        private readonly PlayerSettings settings;
        private readonly CatMoodPresenter mood;
        private readonly CellLockController locks;

        public BoardController(IGameplayContext context, PuzzleSession session, CanvasSpace space, PieceLayout pieceLayout, TrayLayout tray, PlayerSettings settings, CatMoodPresenter mood, CellLockController locks) : base(context)
        {
            this.locks = locks;
            this.session = session;
            this.space = space;
            this.pieceLayout = pieceLayout;
            this.tray = tray;
            this.settings = settings;
            this.mood = mood;
        }

        private LevelDefinition activeLevel => session.activeLevel;
        private RectTransform boardRoot => session.boardRoot;
        private RectTransform boardBackdrop => session.boardBackdrop;
        private Dictionary<Vector2Int, CellView> boardCells => session.boardCells;
        private Dictionary<Vector2Int, string> occupancy => session.occupancy;
        private List<CellView> previewCells => session.previewCells;
        private List<BoardRevealCell> boardRevealCells => session.boardRevealCells;
        private float boardWidth { get => session.boardWidth; set => session.boardWidth = value; }
        private float boardHeight { get => session.boardHeight; set => session.boardHeight = value; }
        private float boardCellWidth { get => session.boardCellWidth; set => session.boardCellWidth = value; }
        private float boardCellHeight { get => session.boardCellHeight; set => session.boardCellHeight = value; }
        private float boardCenterX { get => session.boardCenterX; set => session.boardCenterX = value; }
        private float boardCenterY { get => session.boardCenterY; set => session.boardCenterY = value; }
        private RectTransform root => Hud.root;
        private RectTransform levelRoot => Hud.levelRoot;
        private PortraitLayoutProfile layoutProfile => Assets.layoutProfile;
        private bool reducedMotion => settings.ReducedMotion;

        public Vector2 GetBoardTargetMaxSize()
        {
            if (Hud.UsesAuthoredLayout)
            {
                var area = Hud.boardArea;
                Vector2 center = levelRoot.InverseTransformPoint(area.TransformPoint(area.rect.center));
                boardCenterX = center.x;
                boardCenterY = center.y;
                // Measure in the same coordinate space as the instantiated puzzle.
                Vector2 min = levelRoot.InverseTransformPoint(area.TransformPoint(area.rect.min));
                Vector2 max = levelRoot.InverseTransformPoint(area.TransformPoint(area.rect.max));
                Vector2 size = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
                // A canvas that has never been laid out (editor preview scenes) collapses the
                // world-space measurement; the authored rect is the intended size in that case.
                if (size.x < 1f || size.y < 1f) size = area.rect.size;
                Vector2 padding = Hud.boardContentPadding;
                return new Vector2(Mathf.Max(1f, size.x - Mathf.Clamp(padding.x, 0f, size.x * .4f) * 2f),
                    Mathf.Max(1f, size.y - Mathf.Clamp(padding.y, 0f, size.y * .4f) * 2f));
            }
            boardCenterX = 0f;
            int maxDimension = Mathf.Max(activeLevel.Rows, activeLevel.Cols);
            Vector2 desired;
            if (maxDimension <= 3)
            {
                desired = new Vector2(520f, 520f);
            }
            else if (maxDimension <= 4)
            {
                desired = new Vector2(620f, 620f);
            }
            else if (maxDimension <= 5)
            {
                desired = new Vector2(690f, 690f);
            }
            else
            {
                desired = new Vector2(MaxBoardWidth, MaxBoardHeight);
            }

            float sideMargin = layoutProfile != null ? layoutProfile.SideMargin : 108f;
            float headerHeight = layoutProfile != null ? layoutProfile.HeaderHeight : 184f;
            float trayHeight = layoutProfile != null ? layoutProfile.TrayHeight : 292f;
            float actionHeight = layoutProfile != null ? layoutProfile.ActionBarHeight : 96f;
            float sectionGap = layoutProfile != null ? layoutProfile.SectionGap : 34f;
            Rect rootRect = root.rect;
            float boardAreaBottom = rootRect.yMin + actionHeight + trayHeight + (sectionGap * 3f);
            float boardAreaTop = rootRect.yMax - headerHeight - 118f;
            boardCenterY = (boardAreaBottom + boardAreaTop) * 0.5f;

            float availableWidth = Mathf.Max(360f, rootRect.width - (sideMargin * 2f));
            float availableHeight = Mathf.Max(420f, boardAreaTop - boardAreaBottom);
            return new Vector2(
                Mathf.Min(desired.x, MaxBoardWidth, availableWidth),
                Mathf.Min(desired.y, MaxBoardHeight, availableHeight));
        }

        public void ApplyGameplayLayout()
        {
            SetRect(boardRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(boardCenterX, boardCenterY), new Vector2(boardWidth, boardHeight));
            if (boardBackdrop != null)
            {
                SetRect(boardBackdrop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(boardCenterX, boardCenterY), new Vector2(boardWidth + 64f, boardHeight + 64f));
            }

            if (Hud.UsesAuthoredLayout)
            {
                if (boardBackdrop != null) boardBackdrop.gameObject.SetActive(false);
                return;
            }
            Text timerText = Hud.timerText;
            if (timerText != null)
            {
                float timerY = boardCenterY + (boardHeight * 0.5f) + TimerBoardGap + (TimerHeight * 0.5f);
                RectTransform timerTarget = Hud.timerPanel != null ? Hud.timerPanel : timerText.rectTransform;
                SetRect(timerTarget, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-132f, timerY), new Vector2(230f, TimerHeight + 8f));
                if (Hud.starPanel != null)
                {
                    SetRect(Hud.starPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(132f, timerY), new Vector2(246f, TimerHeight + 8f));
                }

                if (Hud.comboBadge != null)
                {
                    float comboY = boardCenterY + (boardHeight * 0.5f) - 36f;
                    SetRect(Hud.comboBadge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, comboY), new Vector2(310f, 58f));
                }
            }
        }

        /// <summary>Fits the grid to the level and positions every baked cell of the loaded prefab.</summary>
        public void LayoutCells(CatPuzzleLevelView view)
        {
            boardCells.Clear(); boardRevealCells.Clear();
            Vector2 target = GetBoardTargetMaxSize();
            Vector2 gridSize = CatPuzzleBoardLayout.Fit(activeLevel.Rows, activeLevel.Cols, target, BoardGap);
            boardWidth = gridSize.x;
            boardHeight = gridSize.y;
            ApplyGameplayLayout();
            boardCellWidth = (boardWidth - BoardGap * (activeLevel.Cols - 1)) / activeLevel.Cols;
            boardCellHeight = (boardHeight - BoardGap * (activeLevel.Rows - 1)) / activeLevel.Rows;
            session.placementAvailability = new bool[activeLevel.Rows, activeLevel.Cols];
            foreach (var cell in view.cells)
            {
                var rect = cell.image.rectTransform;
                SetTopLeft(rect, CellPosition(cell.row, cell.col), new Vector2(boardCellWidth, boardCellHeight));
                bool blocked = !cell.active && activeLevel.BlockedCells.Contains(new Vector2Int(cell.row, cell.col));
                if (blocked)
                {
                    // A sleeping cat already owns this cell: it pops in with the board but never accepts a piece.
                    cell.image.sprite = mood.Portrait(CatMood.Neutral, cell.row * activeLevel.Cols + cell.col);
                    cell.image.type = Image.Type.Simple;
                    cell.image.preserveAspect = true;
                    cell.image.color = Color.white;
                    rect.localScale = Vector3.zero;
                    boardRevealCells.Add(new BoardRevealCell(rect, cell.row + cell.col));
                }
                else
                {
                    // Level prefabs are reused between loads, so undo any earlier cat dressing.
                    cell.image.sprite = Assets.roundedBoxSprite;
                    cell.image.type = Image.Type.Sliced;
                    cell.image.preserveAspect = false;
                    cell.image.color = cell.active ? TargetColor : Color.clear;
                    rect.localScale = cell.active ? Vector3.zero : Vector3.one;
                }
                if (cell.active)
                {
                    cell.preview.gameObject.SetActive(false);
                    SetCenteredChild(cell.shine, new Vector2(-boardCellWidth * .08f, boardCellHeight * .08f), new Vector2(boardCellWidth * .72f, boardCellHeight * .72f));
                    SetCenteredChild(cell.paw, Vector2.zero, new Vector2(boardCellWidth * .48f, boardCellHeight * .48f));
                    boardRevealCells.Add(new BoardRevealCell(rect, cell.row + cell.col));
                }
                boardCells[new Vector2Int(cell.row, cell.col)] = new CellView(rect, cell.image, cell.preview, cell.image.color);
            }
            locks.Layout();
        }

        // Diagonal "pop-in" reveal: every cell starts at scale 0 and springs to full
        // size, staggered by its (row+col) so the fill sweeps across the board. The
        // caller waits for this before handing the level to the player.
        public IEnumerator RevealCells()
        {
            if (reducedMotion || boardRevealCells.Count == 0)
            {
                SnapRevealScales();
                yield break;
            }

            int maxDiagonal = 0;
            for (int i = 0; i < boardRevealCells.Count; i++)
            {
                maxDiagonal = Mathf.Max(maxDiagonal, boardRevealCells[i].Diagonal);
            }

            for (int diagonal = 0; diagonal <= maxDiagonal; diagonal++)
            {
                for (int i = 0; i < boardRevealCells.Count; i++)
                {
                    BoardRevealCell cell = boardRevealCells[i];
                    if (cell.Diagonal == diagonal && cell.Rect != null)
                    {
                        StartCoroutine(SpringBoardCell(cell.Rect));
                    }
                }

                yield return new WaitForSecondsRealtime(BoardRevealStaggerSeconds);
            }

            yield return new WaitForSecondsRealtime(BoardRevealCellSeconds);
            SnapRevealScales();
        }

        /// <summary>Shows every board cell at full size, ending any reveal that was in progress.</summary>
        public void SnapRevealScales()
        {
            for (int i = 0; i < boardRevealCells.Count; i++)
            {
                if (boardRevealCells[i].Rect != null)
                {
                    boardRevealCells[i].Rect.localScale = Vector3.one;
                }
            }
        }

        private IEnumerator SpringBoardCell(RectTransform rect)
        {
            float elapsed = 0f;
            while (elapsed < BoardRevealCellSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / BoardRevealCellSeconds);
                float scale = t < 0.72f
                    ? Mathf.Lerp(0f, BoardRevealOvershoot, EaseOutCubic(t / 0.72f))
                    : Mathf.Lerp(BoardRevealOvershoot, 1f, EaseOutCubic((t - 0.72f) / 0.28f));
                rect.localScale = Vector3.one * scale;
                yield return null;
            }

            rect.localScale = Vector3.one;
        }

        public bool CanPlace(PieceState state, int row, int col)
        {
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                Vector2Int coord = new Vector2Int(row + cell.Row, col + cell.Col);
                if (coord.x < 0 || coord.x >= activeLevel.Rows || coord.y < 0 || coord.y >= activeLevel.Cols)
                {
                    return false;
                }

                if (!activeLevel.ActiveCells.Contains(coord) || occupancy.ContainsKey(coord) || locks.IsLocked(coord))
                {
                    return false;
                }
            }

            return true;
        }

        public void ShowPreview(PieceState state, int row, int col, bool valid)
        {
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                Vector2Int coord = new Vector2Int(row + cell.Row, col + cell.Col);
                if (boardCells.TryGetValue(coord, out CellView cellView) && activeLevel.ActiveCells.Contains(coord))
                {
                    cellView.Image.color = Color.Lerp(locks.BaseColor(coord), valid ? ValidColor : InvalidColor, 0.42f);
                    cellView.Rect.localScale = Vector3.one * (valid ? 1.045f : 1.03f);
                    previewCells.Add(cellView);
                }
            }
        }

        public void ClearPreview()
        {
            for (int i = 0; i < previewCells.Count; i++)
            {
                CellView cell = previewCells[i];
                if (cell.Image != null)
                {
                    cell.Image.color = TargetColor;
                    cell.Rect.localScale = Vector3.one;
                }
                locks.RestoreColors();

                if (cell.Preview != null)
                {
                    cell.Preview.gameObject.SetActive(false);
                }
            }

            previewCells.Clear();
        }

        public void OccupyCells(PieceState state, int row, int col)
        {
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                occupancy[new Vector2Int(row + cell.Row, col + cell.Col)] = state.Definition.Id;
            }
        }

        public void ClearOccupancyForPiece(string pieceId)
        {
            List<Vector2Int> occupancyRemovalBuffer = session.occupancyRemovalBuffer;
            occupancyRemovalBuffer.Clear();
            foreach (KeyValuePair<Vector2Int, string> entry in occupancy)
            {
                if (entry.Value == pieceId)
                {
                    occupancyRemovalBuffer.Add(entry.Key);
                }
            }

            for (int i = 0; i < occupancyRemovalBuffer.Count; i++)
            {
                occupancy.Remove(occupancyRemovalBuffer[i]);
            }

            occupancyRemovalBuffer.Clear();
        }

        /// <summary>Restores every active cell to its resting colour.</summary>
        public void ResetCellColors()
        {
            foreach (KeyValuePair<Vector2Int, CellView> cell in boardCells)
            {
                if (activeLevel != null && activeLevel.ActiveCells.Contains(cell.Key))
                {
                    cell.Value.Image.color = locks.BaseColor(cell.Key);
                }
            }
        }

        public Vector2 CellPosition(int row, int col)
        {
            return new Vector2((col * (boardCellWidth + BoardGap)) + (boardCellWidth * 0.5f), -((row * (boardCellHeight + BoardGap)) + (boardCellHeight * 0.5f)));
        }

        public Vector2 BoardPieceCenter(int row, int col, PieceState state)
        {
            float width = state.Rect.rect.width;
            float height = state.Rect.rect.height;
            return new Vector2((col * (boardCellWidth + BoardGap)) + (width * 0.5f), -((row * (boardCellHeight + BoardGap)) + (height * 0.5f)));
        }

        public Vector2 BoardPieceCenterInBoard(PieceState state, int row, int col)
        {
            return BoardAnchoredToLocal(BoardPieceCenter(row, col, state));
        }

        public Vector2 BoardPieceCenterInRoot(PieceState state, int row, int col)
        {
            Vector3 world = boardRoot.TransformPoint(BoardPieceCenterInBoard(state, row, col));
            return space.ScreenToRootLocal(space.WorldToScreen(world));
        }

        public Vector2 BoardPieceCenterScreen(PieceState state, int row, int col)
        {
            Vector3 world = boardRoot.TransformPoint(BoardPieceCenterInBoard(state, row, col));
            return space.WorldToScreen(world);
        }

        public Vector2 BoardAnchoredToLocal(Vector2 topLeftAnchored)
        {
            return new Vector2(topLeftAnchored.x - (boardWidth * 0.5f), topLeftAnchored.y + (boardHeight * 0.5f));
        }

        public Vector2 BoardCenterScreen()
        {
            return space.WorldToScreen(boardRoot.position);
        }

        public void AttachPieceToBoard(PieceState state)
        {
            state.Rect.SetParent(boardRoot, false);
            state.Rect.anchorMin = new Vector2(0f, 1f);
            state.Rect.anchorMax = new Vector2(0f, 1f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            pieceLayout.SetPieceGrid(state, boardCellWidth, BoardGap, boardCellHeight, BoardGap);
            state.Rect.anchoredPosition = BoardPieceCenter(state.Row, state.Col, state);
            state.Rect.localScale = Vector3.one;
            state.Rect.localEulerAngles = Vector3.zero;
            tray.SetPieceSlotVisible(state, false, true);
        }
    }
}
