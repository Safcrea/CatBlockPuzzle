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
        // Matches the order of the eight portraits in the cat atlases (AtlasIndex 0..7).
        private static readonly Color[] PortraitColors =
        {
            new Color(0.98f, 0.52f, 0.38f, 1f),  // ginger tabby
            new Color(0.56f, 0.68f, 0.70f, 1f),  // grey-teal tabby
            new Color(0.47f, 0.58f, 0.76f, 1f),  // blue-grey tabby
            new Color(1f, 0.78f, 0.22f, 1f),     // yellow tabby
            new Color(0.76f, 0.60f, 0.90f, 1f),  // lilac tabby
            new Color(0.98f, 0.66f, 0.30f, 1f),  // calico
            new Color(0.62f, 0.76f, 0.34f, 1f),  // green tabby
            new Color(0.34f, 0.78f, 0.80f, 1f)   // teal tabby
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
