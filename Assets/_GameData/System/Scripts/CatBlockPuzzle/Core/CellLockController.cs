using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Locked board cells: solution cells that start padlocked and open during the level.
    /// A counter lock opens once enough cats are on the board; a neighbour lock opens once
    /// every free cell beside it (belonging to other pieces) is filled. Opening is permanent.
    /// </summary>
    internal sealed class CellLockController : HostedService
    {
        private sealed class Lock
        {
            public Vector2Int Cell;
            public int After;                      // > 0: counter rule, else neighbour rule
            public List<Vector2Int> Unlockers;     // neighbour rule: cells that must be occupied
            public bool Open;
            public RectTransform Overlay;
            public Text Counter;
        }

        private readonly PuzzleSession session;
        private readonly PlayerSettings settings;
        private readonly List<Lock> locks = new List<Lock>();
        private readonly Dictionary<Vector2Int, Lock> byCell = new Dictionary<Vector2Int, Lock>();

        public CellLockController(IGameplayContext context, PuzzleSession session, PlayerSettings settings) : base(context)
        {
            this.session = session;
            this.settings = settings;
        }

        public bool HasLocks => locks.Count > 0;

        public bool IsLocked(Vector2Int cell) => byCell.TryGetValue(cell, out Lock l) && !l.Open;

        /// <summary>The resting colour of an active cell, dimmed while it is locked.</summary>
        public Color BaseColor(Vector2Int cell) => IsLocked(cell) ? LockedColor : TargetColor;

        /// <summary>True when any of the piece's solution cells is still locked.</summary>
        public bool BlocksSolution(PieceDefinition piece)
        {
            for (int i = 0; i < piece.Cells.Length; i++)
            {
                if (IsLocked(new Vector2Int(piece.SolutionRow + piece.Cells[i].Row, piece.SolutionCol + piece.Cells[i].Col))) return true;
            }
            return false;
        }

        /// <summary>Rebuilds the lock state and overlays for the active level. Call after the board cells are laid out.</summary>
        public void Layout()
        {
            locks.Clear();
            byCell.Clear();
            LevelDefinition level = session.activeLevel;
            foreach (KeyValuePair<Vector2Int, CellView> cell in session.boardCells)
            {
                Transform stale = cell.Value.Rect.Find("Lock");
                if (stale != null) stale.gameObject.SetActive(false);
            }
            if (level == null || level.LockedCells.Count == 0) return;

            Dictionary<Vector2Int, string> owner = OwnerMap(level);
            foreach (KeyValuePair<Vector2Int, int> entry in level.LockedCells)
            {
                if (!session.boardCells.TryGetValue(entry.Key, out CellView view)) continue;
                Lock l = new Lock { Cell = entry.Key, After = entry.Value, Unlockers = new List<Vector2Int>() };
                if (l.After <= 0)
                {
                    string self = owner[entry.Key];
                    foreach (Vector2Int n in Neighbours(entry.Key))
                    {
                        if (level.ActiveCells.Contains(n) && !level.LockedCells.ContainsKey(n) && owner[n] != self) l.Unlockers.Add(n);
                    }
                }
                BuildOverlay(l, view);
                locks.Add(l);
                byCell[entry.Key] = l;
            }
            RestoreColors();
        }

        /// <summary>Re-applies the dimmed colour to locked cells after other code reset the board colours.</summary>
        public void RestoreColors()
        {
            for (int i = 0; i < locks.Count; i++)
            {
                if (!locks[i].Open && session.boardCells.TryGetValue(locks[i].Cell, out CellView view)) view.Image.color = LockedColor;
            }
        }

        /// <summary>Re-evaluates every lock against the current board; call after each placement.</summary>
        public void OnBoardChanged()
        {
            if (locks.Count == 0) return;
            int placed = 0;
            for (int i = 0; i < session.pieces.Count; i++) if (session.pieces[i].Placed) placed++;

            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < locks.Count; i++)
                {
                    Lock l = locks[i];
                    if (l.Open) continue;
                    if (l.After > 0)
                    {
                        if (l.Counter != null) l.Counter.text = Mathf.Max(0, l.After - placed).ToString();
                        if (placed < l.After) continue;
                    }
                    else if (!AllOccupied(l.Unlockers)) continue;
                    Open(l);
                    changed = true;
                }
            }
        }

        private bool AllOccupied(List<Vector2Int> cells)
        {
            for (int i = 0; i < cells.Count; i++) if (!session.occupancy.ContainsKey(cells[i])) return false;
            return true;
        }

        private void Open(Lock l)
        {
            l.Open = true;
            if (session.boardCells.TryGetValue(l.Cell, out CellView view)) view.Image.color = TargetColor;
            PuzzleAudio.Play("Snap");
            if (l.Overlay == null) return;
            if (settings.ReducedMotion) { l.Overlay.gameObject.SetActive(false); return; }
            StartCoroutine(Dismiss(l.Overlay, view.Rect));
        }

        private IEnumerator Dismiss(RectTransform overlay, RectTransform cell)
        {
            const float seconds = 0.32f;
            float elapsed = 0f;
            Image icon = overlay.GetComponent<Image>();
            Color start = icon != null ? icon.color : Color.white;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float pop = 1f + 0.35f * Mathf.Sin(t * Mathf.PI);
                overlay.localScale = Vector3.one * pop * (1f - t * 0.6f);
                overlay.anchoredPosition = Vector2.up * (t * t * 40f);
                if (icon != null) icon.color = new Color(start.r, start.g, start.b, 1f - t);
                if (cell != null) cell.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            if (cell != null) cell.localScale = Vector3.one;
            overlay.gameObject.SetActive(false);
        }

        private void BuildOverlay(Lock l, CellView view)
        {
            Transform existing = view.Rect.Find("Lock");
            RectTransform overlay;
            Image icon;
            Text counter;
            if (existing != null)
            {
                overlay = (RectTransform)existing;
                icon = overlay.GetComponent<Image>();
                counter = overlay.GetComponentInChildren<Text>(true);
            }
            else
            {
                overlay = new GameObject("Lock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                overlay.SetParent(view.Rect, false);
                icon = overlay.GetComponent<Image>();
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                Text label = new GameObject("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
                label.rectTransform.SetParent(overlay, false);
                label.font = Assets.defaultFont;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                Outline outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.36f, 0.19f, 0.10f, 1f);
                outline.effectDistance = new Vector2(2f, -2f);
                counter = label;
            }

            float size = Mathf.Min(session.boardCellWidth, session.boardCellHeight);
            overlay.anchorMin = overlay.anchorMax = new Vector2(0.5f, 0.5f);
            overlay.pivot = new Vector2(0.5f, 0.5f);
            overlay.anchoredPosition = Vector2.zero;
            overlay.sizeDelta = Vector2.one * size * 0.62f;
            overlay.localScale = Vector3.one;
            overlay.SetAsLastSibling();
            icon.sprite = CellLockArt.Padlock;
            icon.color = Color.white;
            overlay.gameObject.SetActive(true);

            // Counter locks show how many more cats must land before they open.
            counter.rectTransform.anchorMin = counter.rectTransform.anchorMax = new Vector2(0.5f, 0.32f);
            counter.rectTransform.anchoredPosition = Vector2.zero;
            counter.rectTransform.sizeDelta = Vector2.one * size * 0.4f;
            counter.fontSize = Mathf.Max(12, Mathf.RoundToInt(size * 0.26f));
            counter.text = l.After > 0 ? l.After.ToString() : string.Empty;
            counter.gameObject.SetActive(l.After > 0);
            l.Overlay = overlay;
            l.Counter = l.After > 0 ? counter : null;
        }

        private static IEnumerable<Vector2Int> Neighbours(Vector2Int c)
        {
            yield return new Vector2Int(c.x - 1, c.y);
            yield return new Vector2Int(c.x + 1, c.y);
            yield return new Vector2Int(c.x, c.y - 1);
            yield return new Vector2Int(c.x, c.y + 1);
        }

        private static Dictionary<Vector2Int, string> OwnerMap(LevelDefinition level)
        {
            var owner = new Dictionary<Vector2Int, string>();
            foreach (PieceDefinition piece in level.Pieces)
                foreach (CellOffset cell in piece.Cells)
                    owner[new Vector2Int(piece.SolutionRow + cell.Row, piece.SolutionCol + cell.Col)] = piece.Id;
            return owner;
        }
    }
}
