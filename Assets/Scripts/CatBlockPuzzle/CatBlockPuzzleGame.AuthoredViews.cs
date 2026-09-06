using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        [Serializable]
        private sealed class AuthoredBoardCell
        {
            public Image Background;
            public Image Preview;
        }

        [Serializable]
        private sealed class AuthoredPiece
        {
            public RectTransform Slot;
            public RectTransform Piece;
            public PieceDragView SlotDrag;
            public PieceDragView PieceDrag;
            public Image[] Cells;
        }

        [SerializeField] private AuthoredBoardCell[] authoredBoard = Array.Empty<AuthoredBoardCell>();
        [SerializeField] private AuthoredPiece[] authoredPieces = Array.Empty<AuthoredPiece>();
        [SerializeField] private Image[] authoredFx = Array.Empty<Image>();

        private void ResetAuthoredViews()
        {
            foreach (AuthoredBoardCell cell in authoredBoard)
            {
                cell.Background.gameObject.SetActive(false);
                cell.Preview.gameObject.SetActive(false);
            }
            foreach (AuthoredPiece view in authoredPieces)
            {
                view.Piece.SetParent(view.Slot, false);
                view.Piece.localScale = Vector3.one;
                view.Piece.localRotation = Quaternion.identity;
                view.Slot.gameObject.SetActive(false);
            }
        }

        private void ConfigureAuthoredBoardCells()
        {
            if (activeLevel.Rows > 8 || activeLevel.Cols > 8 || authoredBoard.Length != 64)
                throw new InvalidOperationException("The level exceeds the scene's authored board capacity.");
            for (int row = 0; row < activeLevel.Rows; row++)
                for (int col = 0; col < activeLevel.Cols; col++)
                {
                    var coord = new Vector2Int(row, col);
                    bool active = activeLevel.ActiveCells.Contains(coord);
                    AuthoredBoardCell view = authoredBoard[row * 8 + col];
                    Image image = view.Background;
                    RectTransform rect = image.rectTransform;
                    rect.gameObject.SetActive(active);
                    image.color = active ? TargetColor : Color.clear;
                    SetTopLeft(rect, CellPosition(row, col), new Vector2(boardCellWidth, boardCellHeight));
                    view.Preview.gameObject.SetActive(false);
                    rect.localScale = active ? Vector3.zero : Vector3.one;
                    if (active) boardRevealCells.Add(new BoardRevealCell(rect, row + col));
                    boardCells[coord] = new CellView(rect, image, active ? view.Preview : null, image.color);
                }
        }

        private void BindAuthoredPiece(PieceState state, int index)
        {
            if (index >= authoredPieces.Length)
                throw new InvalidOperationException("The level exceeds the scene's authored piece capacity.");
            AuthoredPiece view = authoredPieces[index];
            if (state.Definition.Cells.Length > view.Cells.Length)
                throw new InvalidOperationException("The shape exceeds the scene's authored cat capacity.");
            state.Slot = view.Slot;
            state.SlotImage = view.Slot.GetComponent<Image>();
            state.SlotImage.color = CardRestColor;
            state.SlotLayout = view.Slot.GetComponent<LayoutElement>();
            ApplyTraySlotLayout(state.SlotLayout);
            state.Rect = view.Piece;
            state.Rect.gameObject.SetActive(true);
            view.SlotDrag.Bind(this, state, true);
            view.PieceDrag.Bind(this, state, false);
            for (int c = 0; c < view.Cells.Length; c++)
            {
                Image body = view.Cells[c];
                body.gameObject.SetActive(c < state.Definition.Cells.Length);
                if (c >= state.Definition.Cells.Length) continue;
                body.sprite = CatPortrait(CatMood.Neutral, state.AtlasIndex);
                body.color = Color.white;
                state.CellImages.Add(body);
                state.CatViews.Add(new CatCellView(body.rectTransform, body));
            }
        }

#if UNITY_EDITOR
        public void AuthorGameplayViews()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Author views in Edit Mode.");
            if (authoredBoard.Length > 0 || authoredPieces.Length > 0 || authoredFx.Length > 0)
                throw new InvalidOperationException("Gameplay views are already authored.");
            LoadBakedUiAssets();
            authoredBoard = new AuthoredBoardCell[64];
            for (int i = 0; i < authoredBoard.Length; i++)
            {
                Image cell = CreateImage(boardRoot, "Cell " + i / 8 + "," + i % 8, TargetColor);
                UseRoundedSprite(cell);
                cell.raycastTarget = false;
                AddSoftShadow(cell, new Vector2(0f, -5f), .1f);
                AddSoftOutline(cell, BoardTileEdgeColor, new Vector2(2f, -2f));
                Image shine = CreateImage(cell.rectTransform, "Cell Shine", new Color(1, 1, 1, .28f));
                UseRoundedSprite(shine);
                shine.raycastTarget = false;
                Stretch(shine.rectTransform);
                shine.rectTransform.anchorMin = new Vector2(.06f, .22f);
                shine.rectTransform.anchorMax = new Vector2(.78f, .94f);
                Image paw = CreateImage(cell.rectTransform, "Paw Print", new Color(.945f, .945f, .957f, .58f));
                paw.sprite = pawSprite;
                paw.raycastTarget = false;
                Stretch(paw.rectTransform);
                paw.rectTransform.anchorMin = new Vector2(.26f, .26f);
                paw.rectTransform.anchorMax = new Vector2(.74f, .74f);
                Image preview = CreateImage(cell.rectTransform, "Cat Landing Preview", Color.white);
                preview.raycastTarget = false;
                preview.preserveAspect = true;
                Stretch(preview.rectTransform);
                preview.rectTransform.offsetMin = new Vector2(4, 4);
                preview.rectTransform.offsetMax = new Vector2(-4, -4);
                authoredBoard[i] = new AuthoredBoardCell { Background = cell, Preview = preview };
                UnityEditor.Undo.RegisterCreatedObjectUndo(cell.gameObject, "Author board cell");
            }
            authoredPieces = new AuthoredPiece[8];
            for (int i = 0; i < authoredPieces.Length; i++)
            {
                RectTransform slot = CreatePanel(trayContent, "Piece Slot " + (i + 1), CardRestColor);
                StyleCreamPanel(slot.GetComponent<Image>(), .11f);
                slot.gameObject.AddComponent<LayoutElement>();
                AddDragDots(slot);
                RectTransform piece = CreatePanel(slot, "Piece " + (i + 1), Color.clear);
                var view = new AuthoredPiece { Slot = slot, Piece = piece,
                    SlotDrag = slot.gameObject.AddComponent<PieceDragView>(),
                    PieceDrag = piece.gameObject.AddComponent<PieceDragView>(), Cells = new Image[5] };
                for (int c = 0; c < view.Cells.Length; c++)
                {
                    Image body = CreateImage(piece, "Cat Cell", Color.white);
                    body.sprite = CatPortrait(CatMood.Neutral, i);
                    body.preserveAspect = true;
                    body.raycastTarget = false;
                    AddSoftShadow(body, new Vector2(0, -5), .22f);
                    view.Cells[c] = body;
                }
                authoredPieces[i] = view;
                UnityEditor.Undo.RegisterCreatedObjectUndo(slot.gameObject, "Author piece slot");
            }
            authoredFx = new Image[128];
            for (int i = 0; i < authoredFx.Length; i++)
            {
                Image fx = CreateImage(fxLayer, "Effect " + (i + 1), Color.white);
                fx.raycastTarget = false;
                authoredFx[i] = fx;
                UnityEditor.Undo.RegisterCreatedObjectUndo(fx.gameObject, "Author effect");
            }
            ResetAuthoredViews();
            ResetFxPool();
            foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("UI");
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(gameObject.scene);
        }
#endif
    }
}
