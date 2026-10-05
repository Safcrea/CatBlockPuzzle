using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Colour zones: board cells tinted in one cat's colour, with a faint print of its face.
    /// Only that cat can land on them; every other cat is refused there.
    /// </summary>
    internal sealed class CellZoneController : HostedService
    {
        // Matches the order of the nine portraits in Art/.../Gameplay/NewCats (AtlasIndex 0..8).
        private static readonly Color[] PortraitColors =
        {
            new Color(1f, 0.64f, 0.45f, 1f),     // peach tabby
            new Color(0.70f, 0.48f, 0.95f, 1f),  // purple tabby
            new Color(0.12f, 0.80f, 0.72f, 1f),  // teal tabby
            new Color(1f, 0.18f, 0.66f, 1f),     // pink tabby
            new Color(0.12f, 0.36f, 0.95f, 1f),  // blue tabby
            new Color(0.25f, 0.76f, 1f, 1f),     // cyan tabby
            new Color(0.58f, 0.88f, 0.14f, 1f),  // green tabby
            new Color(1f, 0.52f, 0.08f, 1f),     // orange tabby
            new Color(0.95f, 0.14f, 0.16f, 1f)   // red tabby
        };

        private readonly PuzzleSession session;
        private readonly CatMoodPresenter mood;
        private readonly Dictionary<Vector2Int, Color> tints = new Dictionary<Vector2Int, Color>();

        public CellZoneController(IGameplayContext context, PuzzleSession session, CatMoodPresenter mood) : base(context)
        {
            this.session = session;
            this.mood = mood;
        }

        /// <summary>True when the cell is a zone that belongs to a different cat.</summary>
        public bool Refuses(Vector2Int cell, PieceState piece)
        {
            LevelDefinition level = session.activeLevel;
            return level != null && piece != null && level.ZoneCells.TryGetValue(cell, out string owner) && owner != piece.Definition.Id;
        }

        public bool TryGetTint(Vector2Int cell, out Color tint) => tints.TryGetValue(cell, out tint);

        public static Color PortraitColor(int atlasIndex) => PortraitColors[Mathf.Abs(atlasIndex) % PortraitColors.Length];

        /// <summary>Rebuilds tints and face prints for the active level. Call after the board cells are laid out.</summary>
        public void Layout()
        {
            tints.Clear();
            foreach (KeyValuePair<Vector2Int, CellView> cell in session.boardCells)
            {
                Transform stale = cell.Value.Rect.Find("Zone");
                if (stale != null) stale.gameObject.SetActive(false);
            }

            LevelDefinition level = session.activeLevel;
            if (level == null || level.ZoneCells.Count == 0) return;
            float size = Mathf.Min(session.boardCellWidth, session.boardCellHeight);
            foreach (KeyValuePair<Vector2Int, string> zone in level.ZoneCells)
            {
                PieceState owner = FindPiece(zone.Value);
                if (owner == null || !session.boardCells.TryGetValue(zone.Key, out CellView view)) continue;
                Color tint = Color.Lerp(TargetColor, PortraitColor(owner.AtlasIndex), 0.5f);
                tints[zone.Key] = tint;

                Transform existing = view.Rect.Find("Zone");
                Image face = existing != null ? existing.GetComponent<Image>() : null;
                if (face == null)
                {
                    face = new GameObject("Zone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    face.rectTransform.SetParent(view.Rect, false);
                    face.raycastTarget = false;
                    face.preserveAspect = true;
                }

                RectTransform rect = face.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.one * size * 0.7f;
                rect.localScale = Vector3.one;
                rect.SetAsLastSibling();
                face.sprite = mood.Portrait(CatMood.Neutral, owner.AtlasIndex);
                face.color = new Color(1f, 1f, 1f, 0.5f);
                face.gameObject.SetActive(true);
            }

            RestoreColors();
        }

        /// <summary>Re-applies the zone tints after other code reset the board colours.</summary>
        public void RestoreColors()
        {
            foreach (KeyValuePair<Vector2Int, Color> tint in tints)
            {
                if (session.boardCells.TryGetValue(tint.Key, out CellView view)) view.Image.color = tint.Value;
            }
        }

        private PieceState FindPiece(string pieceId)
        {
            for (int i = 0; i < session.pieces.Count; i++)
            {
                if (session.pieces[i].Definition.Id == pieceId) return session.pieces[i];
            }
            return null;
        }
    }
}
