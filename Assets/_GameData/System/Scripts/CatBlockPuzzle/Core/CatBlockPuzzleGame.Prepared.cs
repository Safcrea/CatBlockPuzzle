using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        [Header("Prepared scene — assigned in Edit Mode")]
        [SerializeField] private CatPuzzleContentCatalog contentCatalog;
#if UNITY_EDITOR
        // Migration/authoring only. Cleared after exporting; never included in a player build.
        [SerializeField, HideInInspector] private CatPuzzleLevelView[] preparedLevels;
#endif
        private PreparedLevelState preparedState;
        private CatPuzzleLevelView loadedLevel;
        private CatPuzzleLevelView loadedLevelPrefab;
        private bool pendingUnusedLevelRelease;
        private AsyncOperation unusedLevelRelease;
        private int preparedLevelIndex = -1;

        private sealed class PreparedLevelState
        {
            public PieceState[] Pieces;
            public PieceDragGesture[] PieceGestures;
            public PieceDragGesture[] SlotGestures;
        }

        public CatPuzzleLevelView LoadedLevel => loadedLevel;
        public string[] LevelPrefabResources => contentCatalog.levelPrefabResources;
        public Canvas PreparedCanvas => canvas;

        public bool ValidatePreparedScene(out string error)
        {
            error = null;
            // Reference UI replaces the legacy room-decoration flow. Its unused room
            // bindings must not block the authored main-menu and level-selection flow.
            bool usesReferenceUi = FindFirstObjectByType<ReferenceUiNavigation>(FindObjectsInactive.Include) != null;
            if (contentCatalog == null || contentCatalog.LevelCount != 100)
                error = "the 100-level content catalog is missing";
            else if (gameplayHud == null) error = "the Gameplay HUD View is missing";
            else if (!gameplayHud.Validate(out error)) { }
            else if (presentationAssets == null) error = "the Gameplay Presentation Assets component is missing";
            else if (!presentationAssets.Validate(out error)) { }
            else if (!usesReferenceUi && metaView == null) error = "the Meta Progression View is missing";
            else if (!usesReferenceUi && !metaView.Validate(out error)) { }
            else if (levelCompleteScreen == null || !levelCompleteScreen.IsConfigured ||
                levelFailScreen == null || !levelFailScreen.IsConfigured)
                error = "level result screen controllers are missing";
            else if (contentCatalog.levelPrefabResources == null || contentCatalog.levelPrefabResources.Length != contentCatalog.LevelCount)
                error = "level prefab addresses do not match the catalog; convert or prepare the scene";
            if (error != null) return false;
            foreach (var effect in preparedEffects) if (effect == null) { error = "an effect slot is missing"; return false; }
            foreach (var address in contentCatalog.levelPrefabResources)
                if (string.IsNullOrWhiteSpace(address)) { error = "a level prefab address is missing"; return false; }
            return usesReferenceUi || ValidatePreparedMeta(out error);
        }

        public bool ValidateLevelPrefab(CatPuzzleLevelView view, int i, out string error)
        {
            error = null;
            {
                var data = contentCatalog.levelPack.levels[i];
                if (view == null || view.levelIndex != i || view.board == null || view.boardFrame == null ||
                    view.boardFrameImage == null || view.boardOutline == null || view.boardEars == null || view.boardEars.Length != 2 ||
                    view.tray == null || view.trayImage == null || view.viewport == null || view.content == null ||
                    view.layout == null || view.scroll == null || view.pieceLayer == null ||
                    view.cells == null || view.cells.Length != data.rows * data.cols ||
                    view.pieces == null || view.pieces.Length != data.pieces.Length)
                { error = "level " + (i + 1) + " has incomplete bindings"; return false; }
                var expectedCells = new System.Collections.Generic.HashSet<Vector2Int>();
                foreach (var sourcePiece in data.pieces)
                {
                    if (!ShapeLibrary.TryGetShape(sourcePiece.shape, out CellOffset[] offsets))
                    { error = "level " + (i + 1) + " has an unknown shape: " + sourcePiece.shape; return false; }
                    foreach (var offset in offsets)
                        expectedCells.Add(new Vector2Int(sourcePiece.row + offset.Row, sourcePiece.col + offset.Col));
                }
                var seenCells = new System.Collections.Generic.HashSet<Vector2Int>();
                foreach (var cell in view.cells)
                {
                    var coordinate = new Vector2Int(cell.row, cell.col);
                    if (cell.row < 0 || cell.row >= data.rows || cell.col < 0 || cell.col >= data.cols ||
                        !seenCells.Add(coordinate) || cell.active != expectedCells.Contains(coordinate))
                    { error = "level " + (i + 1) + " has stale board geometry; regenerate its level prefab"; return false; }
                    if (cell.image == null || (cell.active && (cell.preview == null || cell.shine == null || cell.paw == null)))
                    { error = "level " + (i + 1) + " has a missing board-cell reference"; return false; }
                }
                for (int p = 0; p < view.pieces.Length; p++)
                {
                    var piece = view.pieces[p];
                    if (piece == null || piece.pieceIndex != p || piece.slot == null || piece.slotImage == null ||
                        piece.slotLayout == null || piece.visual == null || piece.slotInput == null || piece.pieceInput == null ||
                        piece.slotInput.levelIndex != i || piece.pieceInput.levelIndex != i ||
                        piece.slotInput.pieceIndex != p || piece.pieceInput.pieceIndex != p ||
                        !piece.slotInput.slotProxy || piece.pieceInput.slotProxy || piece.cats == null || piece.cats.Length == 0)
                    { error = "level " + (i + 1) + ", piece " + p + " has incomplete bindings"; return false; }
                    ShapeLibrary.TryGetShape(data.pieces[p].shape, out CellOffset[] shapeCells);
                    if (piece.cats.Length != shapeCells.Length)
                    { error = "level " + (i + 1) + ", piece " + p + " has a stale shape; regenerate its level prefab"; return false; }
                    foreach (var cat in piece.cats) if (cat == null || cat.sprite == null)
                    { error = "level " + (i + 1) + " has a missing cat image"; return false; }
                }
            }
            return true;
        }

        private void InitializeLoadedLevel()
        {
            {
                var view = loadedLevel;
                var definition = levelManager.GetLevel(view.levelIndex);
                var saved = new PreparedLevelState {
                    Pieces = new PieceState[view.pieces.Length],
                    PieceGestures = new PieceDragGesture[view.pieces.Length],
                    SlotGestures = new PieceDragGesture[view.pieces.Length]
                };
                for (int p = 0; p < view.pieces.Length; p++)
                {
                    var pv = view.pieces[p];
                    pv.slotInput.controller = this;
                    pv.pieceInput.controller = this;
                    var state = new PieceState(definition.Pieces[p], PieceColors[p % PieceColors.Length]) {
                        AtlasIndex = p % 8, FloatPhase = p * 0.83f,
                        Slot = pv.slot, SlotImage = pv.slotImage, SlotLayout = pv.slotLayout, Rect = pv.visual
                    };
                    foreach (var cat in pv.cats)
                    {
                        state.CellImages.Add(cat);
                        state.CatViews.Add(new CatCellView(cat.rectTransform, cat));
                    }
                    saved.Pieces[p] = state;
                    saved.PieceGestures[p] = new PieceDragGesture();
                    saved.PieceGestures[p].Bind(this, state, false);
                    saved.SlotGestures[p] = new PieceDragGesture();
                    saved.SlotGestures[p].Bind(this, state, true);
                }
                preparedState = saved;
            }
        }

        private void ApplyLevelBindings(CatPuzzleLevelView view)
        {
            boardRoot = view.board; boardBackdrop = view.boardFrame;
            trayRoot = view.tray; trayImage = view.trayImage;
            trayViewport = view.viewport; trayContent = view.content;
            trayLayout = view.layout; trayScrollRect = view.scroll; pieceLayer = view.pieceLayer;
        }

        private void ResetPreparedLevel()
        {
            if (backgroundCrossfade != null) backgroundCrossfade.gameObject.SetActive(false);
            if (loadedLevel == null || preparedState == null) return;
            var old = preparedState;
            foreach (var state in old.Pieces)
            {
                state.SizeRoutine = null;
                state.Row = -1; state.Col = -1; state.HasGridLayout = false;
                AttachPieceToTray(state);
                SetCatMood(state, CatMood.Neutral);
            }
            foreach (var gesture in old.PieceGestures) gesture.ResetGesture();
            foreach (var gesture in old.SlotGestures) gesture.ResetGesture();
            foreach (var cell in loadedLevel.cells)
            {
                if (cell.preview != null) cell.preview.gameObject.SetActive(false);
                cell.image.rectTransform.localScale = Vector3.one;
            }
            loadedLevel.gameObject.SetActive(false);
        }

        private CatPuzzleLevelView LoadLevelPrefab(int index)
        {
            if (loadedLevel != null && preparedLevelIndex == index) return loadedLevelPrefab;
            var prefab = Resources.Load<CatPuzzleLevelView>(contentCatalog.levelPrefabResources[index]);
            if (!ValidateLevelPrefab(prefab, index, out string error))
            {
                Debug.LogError("Cannot load level " + (index + 1) + ": " + error, this);
                return null;
            }
            return prefab;
        }

        private void ActivatePreparedLevel(int index, CatPuzzleLevelView prefab)
        {
            if (loadedLevel == null || preparedLevelIndex != index)
            {
                ReleaseLoadedLevel();
                loadedLevelPrefab = prefab;
                loadedLevel = Instantiate(prefab, levelRoot, false);
                loadedLevel.name = prefab.name;
                InitializeLoadedLevel();
            }
            preparedLevelIndex = index;
            ApplyLevelBindings(loadedLevel);
            loadedLevel.gameObject.SetActive(true);
            pieces.AddRange(preparedState.Pieces);
        }

        private void ReleaseLoadedLevel()
        {
            if (loadedLevel != null)
            {
                loadedLevel.gameObject.SetActive(false);
                // Detached immediately; Unity destroys the previous instance at the end of this frame.
                loadedLevel.transform.SetParent(null, false);
                if (Application.isPlaying) Destroy(loadedLevel.gameObject);
#if UNITY_EDITOR
                else DestroyImmediate(loadedLevel.gameObject);
#endif
                pendingUnusedLevelRelease = true;
            }
            loadedLevel = null; loadedLevelPrefab = null; preparedState = null; preparedLevelIndex = -1;
            boardRoot = null; boardBackdrop = null; trayRoot = null; trayImage = null;
            trayViewport = null; trayContent = null; trayLayout = null; trayScrollRect = null; pieceLayer = null;
            boardCells.Clear(); boardRevealCells.Clear(); pieces.Clear(); previewCells.Clear();
        }

        private System.Collections.IEnumerator ReleaseUnusedLevelAssets()
        {
            // Destruction must complete before the unused prefab asset can be reclaimed.
            yield return null;
            while (unusedLevelRelease != null && !unusedLevelRelease.isDone) yield return null;
            if (!pendingUnusedLevelRelease) yield break;
            pendingUnusedLevelRelease = false;
            unusedLevelRelease = Resources.UnloadUnusedAssets();
            yield return unusedLevelRelease;
        }

        private void LayoutPreparedLevel()
        {
            boardCells.Clear(); boardRevealCells.Clear();
            Vector2 target = GetBoardTargetMaxSize();
            Vector2 gridSize = CatPuzzleBoardLayout.Fit(activeLevel.Rows, activeLevel.Cols, target, BoardGap);
            boardWidth = gridSize.x;
            boardHeight = gridSize.y;
            ApplyGameplayLayout();
            boardCellWidth = (boardWidth - BoardGap * (activeLevel.Cols - 1)) / activeLevel.Cols;
            boardCellHeight = (boardHeight - BoardGap * (activeLevel.Rows - 1)) / activeLevel.Rows;
            placementAvailability = new bool[activeLevel.Rows, activeLevel.Cols];
            foreach (var cell in loadedLevel.cells)
            {
                var rect = cell.image.rectTransform;
                SetTopLeft(rect, CellPosition(cell.row, cell.col), new Vector2(boardCellWidth, boardCellHeight));
                cell.image.color = cell.active ? TargetColor : Color.clear;
                rect.localScale = cell.active ? Vector3.zero : Vector3.one;
                if (cell.active)
                {
                    cell.preview.gameObject.SetActive(false);
                    SetCenteredChild(cell.shine, new Vector2(-boardCellWidth * .08f, boardCellHeight * .08f), new Vector2(boardCellWidth * .72f, boardCellHeight * .72f));
                    SetCenteredChild(cell.paw, Vector2.zero, new Vector2(boardCellWidth * .48f, boardCellHeight * .48f));
                    boardRevealCells.Add(new BoardRevealCell(rect, cell.row + cell.col));
                }
                boardCells[new Vector2Int(cell.row, cell.col)] = new CellView(rect, cell.image, cell.preview, cell.image.color);
            }
            ConfigureTrayForPieceCount(pieces.Count);
            foreach (var state in pieces)
            {
                state.HasGridLayout = false; state.Row = -1; state.Col = -1;
                ApplyTraySlotLayout(state.SlotLayout);
                AttachPieceToTray(state);
                SetCatMood(state, CatMood.Neutral);
            }
            RefreshTrayLayout(false);
            trayScrollRect.StopMovement();
            trayScrollRect.horizontalNormalizedPosition = 0f;
        }

        public void HandlePreparedGesture(int level, int piece, bool slotProxy,
            CatPuzzlePieceDragView.GestureEvent action, BaseEventData data)
        {
            if (preparedState == null || level != preparedLevelIndex || piece < 0 || piece >= pieces.Count) return;
            var saved = preparedState;
            var gesture = slotProxy ? saved.SlotGestures[piece] : saved.PieceGestures[piece];
            var pointer = data as PointerEventData;
            switch (action)
            {
                case CatPuzzlePieceDragView.GestureEvent.Initialize: gesture.OnInitializePotentialDrag(pointer); break;
                case CatPuzzlePieceDragView.GestureEvent.Begin: gesture.OnBeginDrag(pointer); break;
                case CatPuzzlePieceDragView.GestureEvent.Drag: gesture.OnDrag(pointer); break;
                case CatPuzzlePieceDragView.GestureEvent.End: gesture.OnEndDrag(pointer); break;
                case CatPuzzlePieceDragView.GestureEvent.Down: gesture.OnPointerDown(pointer); break;
                case CatPuzzlePieceDragView.GestureEvent.Up: gesture.OnPointerUp(pointer); break;
                case CatPuzzlePieceDragView.GestureEvent.Cancel: gesture.OnCancel(data); break;
            }
        }
    }
}
