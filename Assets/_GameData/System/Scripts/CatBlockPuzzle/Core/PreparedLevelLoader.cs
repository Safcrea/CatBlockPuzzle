using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Loads one baked level prefab at a time from Resources, validates its bindings,
    /// builds the piece states and gesture handlers for it, and releases it (and the
    /// unused prefab asset) when the next level takes its place.
    /// </summary>
    internal sealed class PreparedLevelLoader : HostedService
    {
        private sealed class PreparedLevelState
        {
            public PieceState[] Pieces;
            public PieceDragGesture[] PieceGestures;
            public PieceDragGesture[] SlotGestures;
        }

        private readonly PuzzleSession session;
        private readonly TrayLayout tray;
        private readonly CatMoodPresenter mood;
        private readonly PieceDragController drag;
        private readonly CatBlockPuzzleGame controller;

        private PreparedLevelState preparedState;
        private CatPuzzleLevelView loadedLevel;
        private CatPuzzleLevelView loadedLevelPrefab;
        private bool pendingUnusedLevelRelease;
        private AsyncOperation unusedLevelRelease;
        private int preparedLevelIndex = -1;

        public PreparedLevelLoader(IGameplayContext context, PuzzleSession session, TrayLayout tray, CatMoodPresenter mood,
            PieceDragController drag, CatBlockPuzzleGame controller) : base(context)
        {
            this.session = session;
            this.tray = tray;
            this.mood = mood;
            this.drag = drag;
            this.controller = controller;
        }

        public CatPuzzleLevelView LoadedLevel => loadedLevel;
        public int LoadedLevelIndex => preparedLevelIndex;
        public bool HasPendingUnusedRelease => pendingUnusedLevelRelease;

        private CatPuzzleContentCatalog contentCatalog => Context.Catalog;

        public static bool ValidateLevelPrefab(CatPuzzleContentCatalog contentCatalog, CatPuzzleLevelView view, int i, out string error)
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
            var view = loadedLevel;
            var definition = Context.Levels.GetLevel(view.levelIndex);
            int baked = view.pieces.Length;
            int total = baked + definition.Spares.Length;
            var saved = new PreparedLevelState {
                Pieces = new PieceState[total],
                PieceGestures = new PieceDragGesture[total],
                SlotGestures = new PieceDragGesture[total]
            };
            for (int p = 0; p < view.pieces.Length; p++)
            {
                var pv = view.pieces[p];
                pv.slotInput.controller = controller;
                pv.pieceInput.controller = controller;
                var state = new PieceState(definition.Pieces[p], PieceColors[p % PieceColors.Length]) {
                    AtlasIndex = p % 8, FloatPhase = p * 0.83f,
                    Slot = pv.slot, SlotImage = pv.slotImage, SlotLayout = pv.slotLayout, Rect = pv.visual
                };
                TraySlotSkin.Apply(pv.slot, pv.slotImage);
                foreach (var cat in pv.cats)
                {
                    state.CellImages.Add(cat);
                    state.CatViews.Add(new CatCellView(cat.rectTransform, cat));
                }
                saved.Pieces[p] = state;
                saved.PieceGestures[p] = new PieceDragGesture();
                saved.PieceGestures[p].Bind(drag, tray, state, false);
                saved.SlotGestures[p] = new PieceDragGesture();
                saved.SlotGestures[p].Bind(drag, tray, state, true);
            }

            int built = baked;
            for (int s = 0; s < definition.Spares.Length; s++)
            {
                PieceState spare = BuildSpare(view, definition.Spares[s], built);
                if (spare == null) continue;
                saved.Pieces[built] = spare;
                saved.PieceGestures[built] = new PieceDragGesture();
                saved.PieceGestures[built].Bind(drag, tray, spare, false);
                saved.SlotGestures[built] = new PieceDragGesture();
                saved.SlotGestures[built].Bind(drag, tray, spare, true);
                built++;
            }

            if (built < total)
            {
                System.Array.Resize(ref saved.Pieces, built);
                System.Array.Resize(ref saved.PieceGestures, built);
                System.Array.Resize(ref saved.SlotGestures, built);
            }
            preparedState = saved;
        }

        // Spare cats are not baked into the level prefab: copy the tray card of a cat with the same
        // number of cats (or the smallest larger one), hide the surplus cats and give it its own index.
        private PieceState BuildSpare(CatPuzzleLevelView view, PieceDefinition spare, int index)
        {
            CatPuzzlePieceView donor = null;
            foreach (var pv in view.pieces)
            {
                if (pv.cats.Length == spare.Cells.Length) { donor = pv; break; }
                if (pv.cats.Length > spare.Cells.Length && (donor == null || pv.cats.Length < donor.cats.Length)) donor = pv;
            }
            if (donor == null)
            {
                Debug.LogWarning("Level " + (view.levelIndex + 1) + " spare " + spare.Id + " has no card to copy.", Host);
                return null;
            }

            GameObject copy = Object.Instantiate(donor.slot.gameObject, donor.slot.parent, false);
            copy.name = spare.Name + " Slot";
            // A fixed but level-specific position, so the decoy is not always the last card.
            copy.transform.SetSiblingIndex((view.levelIndex * 7 + index) % (view.pieces.Length + 1));
            var card = copy.GetComponent<CatPuzzlePieceView>();
            card.pieceIndex = index;
            card.slotInput.pieceIndex = index; card.slotInput.controller = controller;
            card.pieceInput.pieceIndex = index; card.pieceInput.controller = controller;
            card.visual.name = spare.Name;

            var state = new PieceState(spare, PieceColors[index % PieceColors.Length]) {
                AtlasIndex = index % 8, FloatPhase = index * 0.83f,
                Slot = card.slot, SlotImage = card.slotImage, SlotLayout = card.slotLayout, Rect = card.visual
            };
            TraySlotSkin.Apply(card.slot, card.slotImage);
            for (int c = 0; c < card.cats.Length; c++)
            {
                bool used = c < spare.Cells.Length;
                card.cats[c].gameObject.SetActive(used);
                if (!used) continue;
                state.CellImages.Add(card.cats[c]);
                state.CatViews.Add(new CatCellView(card.cats[c].rectTransform, card.cats[c]));
            }
            return state;
        }

        /// <summary>Puts every piece of the loaded level back on the shelf and hides the level, ready to be laid out again.</summary>
        public void ResetLoadedLevel()
        {
            if (Hud.backgroundCrossfade != null) Hud.backgroundCrossfade.gameObject.SetActive(false);
            if (loadedLevel == null || preparedState == null) return;
            var old = preparedState;
            foreach (var state in old.Pieces)
            {
                state.SizeRoutine = null;
                state.SlotMoveRoutine = null;
                state.Row = -1; state.Col = -1; state.HasGridLayout = false;
                tray.AttachPieceToTray(state);
                mood.SetMood(state, CatMood.Neutral);
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

        /// <summary>The validated prefab for a level, or null (with an error logged) when it cannot be used.</summary>
        public CatPuzzleLevelView LoadLevelPrefab(int index)
        {
            if (loadedLevel != null && preparedLevelIndex == index) return loadedLevelPrefab;
            var prefab = Resources.Load<CatPuzzleLevelView>(contentCatalog.levelPrefabResources[index]);
            if (!ValidateLevelPrefab(contentCatalog, prefab, index, out string error))
            {
                Debug.LogError("Cannot load level " + (index + 1) + ": " + error, Host);
                return null;
            }
            return prefab;
        }

        /// <summary>Instantiates the prefab (unless it is already the loaded level), binds its views and registers its pieces.</summary>
        public void Activate(int index, CatPuzzleLevelView prefab)
        {
            if (loadedLevel == null || preparedLevelIndex != index)
            {
                Release();
                loadedLevelPrefab = prefab;
                loadedLevel = Object.Instantiate(prefab, Hud.levelRoot, false);
                loadedLevel.name = prefab.name;
                InitializeLoadedLevel();
            }
            preparedLevelIndex = index;
            session.BindLevelView(loadedLevel);
            loadedLevel.gameObject.SetActive(true);
            session.pieces.AddRange(preparedState.Pieces);
        }

        public void Release()
        {
            if (loadedLevel != null)
            {
                loadedLevel.gameObject.SetActive(false);
                // Detached immediately; Unity destroys the previous instance at the end of this frame.
                loadedLevel.transform.SetParent(null, false);
                if (Application.isPlaying) Destroy(loadedLevel.gameObject);
#if UNITY_EDITOR
                else Object.DestroyImmediate(loadedLevel.gameObject);
#endif
                pendingUnusedLevelRelease = true;
            }
            loadedLevel = null; loadedLevelPrefab = null; preparedState = null; preparedLevelIndex = -1;
            session.ClearLevelView();
        }

        public IEnumerator ReleaseUnusedLevelAssets()
        {
            // Destruction must complete before the unused prefab asset can be reclaimed.
            yield return null;
            while (unusedLevelRelease != null && !unusedLevelRelease.isDone) yield return null;
            if (!pendingUnusedLevelRelease) yield break;
            pendingUnusedLevelRelease = false;
            unusedLevelRelease = Resources.UnloadUnusedAssets();
            yield return unusedLevelRelease;
        }

        /// <summary>Routes a pointer event from a baked piece (or its tray card) to that piece's gesture handler.</summary>
        public void HandleGesture(int level, int piece, bool slotProxy,
            CatPuzzlePieceDragView.GestureEvent action, BaseEventData data)
        {
            if (preparedState == null || level != preparedLevelIndex || piece < 0 || piece >= session.pieces.Count) return;
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
