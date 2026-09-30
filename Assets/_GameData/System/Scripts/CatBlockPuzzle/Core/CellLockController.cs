using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Locked board cells: solution cells that start padlocked and open during the level.
    /// A key lock opens when its named cat is placed; a counter lock opens once enough cats
    /// are on the board; a neighbour lock opens once every free cell beside it (belonging to
    /// other pieces) is filled. Opening is permanent for the rest of the attempt.
    /// </summary>
    internal sealed class CellLockController : HostedService
    {
        private static readonly Color Cocoa = new Color(0.36f, 0.19f, 0.10f, 1f);

        private sealed class Lock
        {
            public Vector2Int Cell;
            public int After;                      // counter rule when > 0
            public string KeyId;                   // key rule when set
            public List<Vector2Int> Unlockers;     // neighbour rule: cells that must be occupied
            public bool Open;
            public RectTransform Overlay;
            public Text Counter;

            public bool IsKey => !string.IsNullOrEmpty(KeyId);
        }

        private readonly PuzzleSession session;
        private readonly PlayerSettings settings;
        private readonly CatMoodPresenter mood;
        private readonly List<Lock> locks = new List<Lock>();
        private readonly Dictionary<Vector2Int, Lock> byCell = new Dictionary<Vector2Int, Lock>();

        public CellLockController(IGameplayContext context, PuzzleSession session, PlayerSettings settings, CatMoodPresenter mood) : base(context)
        {
            this.session = session;
            this.settings = settings;
            this.mood = mood;
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
            RefreshKeyTags();
            foreach (KeyValuePair<Vector2Int, CellView> cell in session.boardCells)
            {
                Transform stale = cell.Value.Rect.Find("Lock");
                if (stale != null) stale.gameObject.SetActive(false);
            }
            if (level == null || level.LockedCells.Count == 0) return;

            Dictionary<Vector2Int, string> owner = OwnerMap(level);
            foreach (KeyValuePair<Vector2Int, CellLockRule> entry in level.LockedCells)
            {
                if (!session.boardCells.TryGetValue(entry.Key, out CellView view)) continue;
                Lock l = new Lock { Cell = entry.Key, After = entry.Value.After, KeyId = entry.Value.KeyPieceId, Unlockers = new List<Vector2Int>() };
                if (!l.IsKey && l.After <= 0)
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
            RefreshKeyTags();
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
                    if (l.IsKey)
                    {
                        if (!IsPlaced(l.KeyId)) continue;
                    }
                    else if (l.After > 0)
                    {
                        if (l.Counter != null) l.Counter.text = Mathf.Max(0, l.After - placed).ToString();
                        if (placed < l.After) continue;
                    }
                    else if (!AllOccupied(l.Unlockers)) continue;
                    Open(l);
                    changed = true;
                }
            }
            RefreshKeyTags();
        }

        /// <summary>
        /// Teaches why a cat was refused: the padlocks over its home wobble, and whatever opens
        /// them (the key cat's tray tag and lock badge, or the counter number) pulses.
        /// </summary>
        public void Explain(PieceDefinition piece)
        {
            if (settings.ReducedMotion) return;
            HashSet<string> keys = new HashSet<string>();
            for (int i = 0; i < piece.Cells.Length; i++)
            {
                Vector2Int cell = new Vector2Int(piece.SolutionRow + piece.Cells[i].Row, piece.SolutionCol + piece.Cells[i].Col);
                if (!byCell.TryGetValue(cell, out Lock l) || l.Open || l.Overlay == null) continue;
                StartCoroutine(Wobble(l.Overlay));
                if (l.IsKey)
                {
                    keys.Add(l.KeyId);
                    Transform badge = l.Overlay.Find("Key");
                    if (badge != null) StartCoroutine(Pulse((RectTransform)badge));
                }
                else if (l.Counter != null) StartCoroutine(Pulse(l.Counter.rectTransform));
            }

            foreach (string keyId in keys)
            {
                PieceState keyPiece = FindPiece(keyId);
                Transform tag = keyPiece != null && keyPiece.Slot != null ? keyPiece.Slot.Find("Key Tag") : null;
                if (tag != null && tag.gameObject.activeSelf) StartCoroutine(Pulse((RectTransform)tag));
            }
        }

        private IEnumerator Wobble(RectTransform target)
        {
            const float seconds = 0.45f;
            float elapsed = 0f;
            while (elapsed < seconds && target != null && target.gameObject.activeInHierarchy)
            {
                elapsed += Time.unscaledDeltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / seconds);
                target.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(elapsed * 40f) * 16f * fade);
                yield return null;
            }
            if (target != null) target.localEulerAngles = Vector3.zero;
        }

        private IEnumerator Pulse(RectTransform target)
        {
            const float seconds = 0.9f;
            float elapsed = 0f;
            Vector3 rest = target != null ? target.localScale : Vector3.one;
            while (elapsed < seconds && target != null && target.gameObject.activeInHierarchy)
            {
                elapsed += Time.unscaledDeltaTime;
                float beat = Mathf.Abs(Mathf.Sin(elapsed / seconds * Mathf.PI * 2f));
                target.localScale = rest * (1f + (0.45f * beat));
                yield return null;
            }
            if (target != null) target.localScale = rest;
        }

        private bool IsPlaced(string pieceId)
        {
            for (int i = 0; i < session.pieces.Count; i++)
            {
                if (session.pieces[i].Placed && session.pieces[i].Definition.Id == pieceId) return true;
            }
            return false;
        }

        private PieceState FindPiece(string pieceId)
        {
            for (int i = 0; i < session.pieces.Count; i++)
            {
                if (session.pieces[i].Definition.Id == pieceId) return session.pieces[i];
            }
            return null;
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
            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                float pop = 1f + 0.35f * Mathf.Sin(t * Mathf.PI);
                overlay.localScale = Vector3.one * pop * (1f - t * 0.6f);
                overlay.anchoredPosition = Vector2.up * (t * t * 40f);
                if (group != null) group.alpha = 1f - t;
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
                outline.effectColor = Cocoa;
                outline.effectDistance = new Vector2(2f, -2f);
                counter = label;
            }

            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            if (group == null) group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

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
            bool showCount = !l.IsKey && l.After > 0;
            counter.rectTransform.anchorMin = counter.rectTransform.anchorMax = new Vector2(0.5f, 0.32f);
            counter.rectTransform.anchoredPosition = Vector2.zero;
            counter.rectTransform.sizeDelta = Vector2.one * size * 0.4f;
            counter.fontSize = Mathf.Max(12, Mathf.RoundToInt(size * 0.26f));
            counter.text = showCount ? l.After.ToString() : string.Empty;
            counter.gameObject.SetActive(showCount);
            l.Overlay = overlay;
            l.Counter = showCount ? counter : null;

            BuildKeyBadge(l, overlay, size * 0.62f);
        }

        // Key locks carry the face of the cat that opens them, matching the face on its tray card.
        private void BuildKeyBadge(Lock l, RectTransform overlay, float overlaySize)
        {
            Transform existing = overlay.Find("Key");
            PieceState keyPiece = l.IsKey ? FindPiece(l.KeyId) : null;
            if (keyPiece == null)
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }

            RectTransform badge;
            Image face;
            if (existing != null)
            {
                badge = (RectTransform)existing;
                face = badge.Find("Face").GetComponent<Image>();
            }
            else
            {
                badge = new GameObject("Key", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                badge.SetParent(overlay, false);
                Image disc = badge.GetComponent<Image>();
                disc.sprite = Assets.circleSprite;
                disc.color = new Color(1f, 0.96f, 0.86f, 1f);
                disc.raycastTarget = false;
                Outline ring = badge.gameObject.AddComponent<Outline>();
                ring.effectColor = Cocoa;
                ring.effectDistance = new Vector2(2f, -2f);
                face = new GameObject("Face", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                face.rectTransform.SetParent(badge, false);
                face.preserveAspect = true;
                face.raycastTarget = false;
                face.rectTransform.anchorMin = new Vector2(0.08f, 0.08f);
                face.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
                face.rectTransform.offsetMin = face.rectTransform.offsetMax = Vector2.zero;
            }

            float badgeSize = overlaySize * 0.66f;
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 0f);
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.sizeDelta = Vector2.one * badgeSize;
            badge.anchoredPosition = new Vector2(-badgeSize * 0.12f, badgeSize * 0.18f);
            badge.localScale = Vector3.one;
            // Same sprite the tray card shows (Neutral), so the two read as the same cat.
            face.sprite = mood.Portrait(CatMood.Neutral, keyPiece.AtlasIndex);

            // A small key on the badge says this cat OPENS the lock, it is not the cat that goes here.
            Transform keyIcon = badge.Find("Key Icon");
            if (keyIcon == null)
            {
                Image glyph = new GameObject("Key Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                glyph.rectTransform.SetParent(badge, false);
                glyph.sprite = CellLockArt.Key;
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;
                glyph.rectTransform.localEulerAngles = new Vector3(0f, 0f, 35f);
                keyIcon = glyph.rectTransform;
            }

            RectTransform keyRect = (RectTransform)keyIcon;
            keyRect.anchorMin = keyRect.anchorMax = new Vector2(0f, 0f);
            keyRect.pivot = new Vector2(0.5f, 0.5f);
            keyRect.sizeDelta = Vector2.one * badgeSize * 0.7f;
            keyRect.anchoredPosition = new Vector2(badgeSize * 0.04f, badgeSize * 0.06f);
            badge.gameObject.SetActive(true);
        }

        // The key cat's tray card wears the same key until every lock it opens is open.
        private void RefreshKeyTags()
        {
            for (int i = 0; i < session.pieces.Count; i++)
            {
                PieceState piece = session.pieces[i];
                if (piece.Slot == null) continue;
                Transform tag = piece.Slot.Find("Key Tag");
                if (!HasClosedLockKeyedTo(piece.Definition.Id))
                {
                    if (tag != null) tag.gameObject.SetActive(false);
                    continue;
                }

                if (tag == null) tag = CreateKeyTag(piece.Slot);
                tag.gameObject.SetActive(true);
            }
        }

        private bool HasClosedLockKeyedTo(string pieceId)
        {
            for (int i = 0; i < locks.Count; i++)
            {
                if (!locks[i].Open && locks[i].IsKey && locks[i].KeyId == pieceId) return true;
            }
            return false;
        }

        private static Transform CreateKeyTag(RectTransform slot)
        {
            Image tag = new GameObject("Key Tag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            tag.rectTransform.SetParent(slot, false);
            tag.sprite = CellLockArt.Key;
            tag.preserveAspect = true;
            tag.raycastTarget = false;
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(1f, 1f);
            tag.rectTransform.pivot = new Vector2(1f, 1f);
            tag.rectTransform.sizeDelta = new Vector2(46f, 46f);
            tag.rectTransform.anchoredPosition = new Vector2(-4f, -4f);
            tag.rectTransform.localEulerAngles = new Vector3(0f, 0f, 35f);
            return tag.rectTransform;
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
