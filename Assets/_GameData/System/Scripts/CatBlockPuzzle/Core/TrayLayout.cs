using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static CatBlockPuzzle.PuzzleStyle;
using static CatBlockPuzzle.PuzzleTuning;
using static CatBlockPuzzle.RectLayout;

namespace CatBlockPuzzle
{
    /// <summary>
    /// The scrolling shelf of piece cards: card sizing for the current piece count, the
    /// grow/settle animations when cards leave and return, idle bobbing, and the tray's
    /// horizontal scroll gesture forwarding.
    /// </summary>
    internal sealed class TrayLayout : HostedService
    {
        private readonly PuzzleSession session;
        private readonly PieceLayout pieceLayout;
        private readonly PlayerSettings settings;
        private readonly CanvasSpace space;

        private float traySlotPreferredWidth = 196f;
        private float traySlotPreferredHeight = 248f;
        private float traySlotMinWidth = 104f;
        private float traySlotMinHeight = 154f;
        private float trayCellMaxSize = 38f;
        private Text queueLabel;

        public TrayLayout(IGameplayContext context, PuzzleSession session, PieceLayout pieceLayout, PlayerSettings settings, CanvasSpace space) : base(context)
        {
            this.session = session;
            this.pieceLayout = pieceLayout;
            this.settings = settings;
            this.space = space;
        }

        private List<PieceState> pieces => session.pieces;
        private LevelDefinition activeLevel => session.activeLevel;
        private DragState drag => session.drag;
        private RectTransform trayRoot => session.trayRoot;
        private RectTransform trayContent => session.trayContent;
        private HorizontalLayoutGroup trayLayout => session.trayLayout;
        private ScrollRect trayScrollRect => session.trayScrollRect;
        private Image trayImage => session.trayImage;
        private RectTransform root => Hud.root;
        private RectTransform levelRoot => Hud.levelRoot;
        private PortraitLayoutProfile layoutProfile => Assets.layoutProfile;
        private bool reducedMotion => settings.ReducedMotion;

        /// <summary>Places every piece back on the shelf for a freshly laid-out level.</summary>
        public void LayoutPieces()
        {
            ConfigureTrayForPieceCount(pieces.Count);
            foreach (var state in pieces)
            {
                state.HasGridLayout = false; state.Row = -1; state.Col = -1;
                ApplyTraySlotLayout(state.SlotLayout);
                AttachPieceToTray(state);
            }
            RefreshTrayLayout(false);
            trayScrollRect.StopMovement();
            trayScrollRect.horizontalNormalizedPosition = 0f;
        }

        /// <summary>Re-applies the drip-fed shelf after a cat comes back to the tray outside a layout refresh.</summary>
        public void ApplyShelfWindow()
        {
            if (activeLevel != null && activeLevel.HasTrayWindow) RefreshTrayLayout(!reducedMotion);
        }

        // Shows the first TrayWindow unplaced cats (in piece order) and hides the rest; a held cat is never hidden.
        private void ApplyDripFeed(List<PieceState> revealed)
        {
            int window = activeLevel != null && activeLevel.HasTrayWindow ? activeLevel.TrayWindow : 0;
            int rank = 0;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state?.Slot == null || state.Placed) continue;
                bool held = drag != null && drag.Piece == state;
                bool show = window <= 0 || rank < window || held;
                rank++;
                if (state.Slot.gameObject.activeSelf == show) continue;
                state.Slot.gameObject.SetActive(show);
                if (show) revealed?.Add(state);
            }

            UpdateQueueLabel(window > 0 ? Mathf.Max(0, rank - window) : 0);
        }

        private void UpdateQueueLabel(int waiting)
        {
            if (trayRoot == null) return;
            if (queueLabel == null || queueLabel.transform.parent != trayRoot)
            {
                Transform existing = trayRoot.Find("Waiting Cats");
                queueLabel = existing != null ? existing.GetComponent<Text>() : null;
            }
            if (waiting <= 0)
            {
                if (queueLabel != null) queueLabel.gameObject.SetActive(false);
                return;
            }

            if (queueLabel == null)
            {
                queueLabel = RuntimeUiFactory.CreateText(trayRoot, "Waiting Cats", "", 30, TextAnchor.MiddleRight);
                if (Assets.defaultFont != null) queueLabel.font = Assets.defaultFont;
                queueLabel.raycastTarget = false;
                queueLabel.color = new Color(0.47f, 0.23f, 0.11f, 1f);
                RectTransform rect = queueLabel.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(360f, 44f);
                rect.anchoredPosition = new Vector2(-24f, 4f);
            }

            queueLabel.text = "+" + waiting + (waiting == 1 ? " cat waiting" : " cats waiting");
            queueLabel.gameObject.SetActive(true);
            queueLabel.transform.SetAsLastSibling();
        }

        private IEnumerator PopIn(RectTransform slot)
        {
            const float seconds = 0.28f;
            float elapsed = 0f;
            while (elapsed < seconds && slot != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                slot.localScale = Vector3.one * (EaseOutCubic(t) * (1f + 0.12f * Mathf.Sin(t * Mathf.PI)));
                yield return null;
            }
            if (slot != null) slot.localScale = Vector3.one;
        }

        public void ConfigureTrayForPieceCount(int visiblePieceCount)
        {
            int visibleCount = Mathf.Max(0, visiblePieceCount);
            int layoutCount = Mathf.Max(1, visibleCount);
            int totalCount = activeLevel != null ? Mathf.Max(1, activeLevel.Pieces.Length) : layoutCount;
            int hiddenCount = Mathf.Max(0, totalCount - visibleCount);
            int horizontalPadding = 24;
            int verticalPadding = 24;
            float densityScale = totalCount >= 7 ? 0.83f : totalCount >= 5 ? 0.9f : 1f;
            float spacing = totalCount >= 7 ? 14f : totalCount >= 5 ? 16f : 18f;
            float growthPerCard = layoutProfile != null ? layoutProfile.RemainingCardGrowth : 0.08f;
            float maximumGrowth = layoutProfile != null ? layoutProfile.MaximumCardGrowth : 0.16f;
            float growth = 1f + Mathf.Min(maximumGrowth, hiddenCount * growthPerCard);
            float baseWidth = layoutProfile != null ? layoutProfile.TrayCardWidth : 212f;
            float baseHeight = layoutProfile != null ? layoutProfile.TrayCardHeight : 246f;
            float baseMinWidth = layoutProfile != null ? layoutProfile.TrayCardMinWidth : 104f;
            float baseMinHeight = layoutProfile != null ? layoutProfile.TrayCardMinHeight : 154f;
            float baseCellSize = layoutProfile != null ? layoutProfile.TrayCatCellSize : 46f;

            traySlotPreferredWidth = baseWidth * densityScale * growth;
            traySlotPreferredHeight = baseHeight * densityScale;
            traySlotMinWidth = Mathf.Min(baseMinWidth * growth, traySlotPreferredWidth);
            traySlotMinHeight = Mathf.Min(baseMinHeight, traySlotPreferredHeight);
            trayCellMaxSize = Mathf.Min(64f, baseCellSize * densityScale * growth);

            float profileTrayHeight = layoutProfile != null ? layoutProfile.TrayHeight : 292f;
            float profileActionHeight = layoutProfile != null ? layoutProfile.ActionBarHeight : 96f;
            traySlotPreferredHeight = Mathf.Min(traySlotPreferredHeight, Mathf.Max(150f, profileTrayHeight - (verticalPadding * 2f)));
            float trayWidth = Mathf.Min(TrayMaxWidth, Mathf.Max(560f, root.rect.width - 64f));
            float trayHeight = Mathf.Clamp(traySlotPreferredHeight + (verticalPadding * 2f), 270f, profileTrayHeight);
            if (Hud.UsesAuthoredLayout)
            {
                trayWidth = Hud.trayArea.rect.width;
                trayHeight = Hud.trayArea.rect.height;
            }
            float viewportWidth = Mathf.Max(1f, trayWidth - 36f);
            float desiredContentWidth = (traySlotPreferredWidth * layoutCount) + (spacing * (layoutCount - 1)) + (horizontalPadding * 2f);

            if (trayLayout != null)
            {
                trayLayout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
                trayLayout.spacing = spacing;
            }

            float trayBottom = profileActionHeight + 40f;
            SetRect(trayRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, trayBottom), new Vector2(trayWidth, trayHeight));
            if (Hud.UsesAuthoredLayout)
            {
                Vector2 center = levelRoot.InverseTransformPoint(Hud.trayArea.TransformPoint(Hud.trayArea.rect.center));
                SetRect(trayRoot, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), center, new Vector2(trayWidth, trayHeight));
                if (trayImage != null) trayImage.enabled = false;
            }
            if (trayContent != null)
            {
                SetRect(trayContent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(Mathf.Max(viewportWidth, desiredContentWidth), 0f));
            }
        }

        public void ApplyTraySlotLayout(LayoutElement layout)
        {
            if (layout == null)
            {
                return;
            }

            layout.preferredWidth = traySlotPreferredWidth;
            layout.minWidth = traySlotMinWidth;
            layout.preferredHeight = traySlotPreferredHeight;
            layout.minHeight = traySlotMinHeight;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        public int VisibleTrayCardCount()
        {
            int count = 0;
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i]?.Slot != null && pieces[i].Slot.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        public void SetPieceSlotVisible(PieceState state, bool visible, bool refreshLayout)
        {
            if (state?.Slot == null)
            {
                return;
            }

            if (state.Slot.gameObject.activeSelf != visible)
            {
                state.Slot.gameObject.SetActive(visible);
            }

            if (state.SlotImage != null)
            {
                state.SlotImage.color = visible ? TraySlotSkin.RestColor : TraySlotSkin.DimColor;
            }

            if (refreshLayout)
            {
                RefreshTrayLayout(!reducedMotion);
            }
        }

        public void RefreshTrayLayout(bool animateCatSizes)
        {
            if (trayContent == null || trayRoot == null)
            {
                return;
            }

            var revealed = new List<PieceState>();
            ApplyDripFeed(revealed);
            float previousScroll = trayScrollRect != null ? trayScrollRect.horizontalNormalizedPosition : 0f;
            var previousSlotPositions = new Dictionary<PieceState, Vector2>();
            if (animateCatSizes)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    PieceState state = pieces[i];
                    if (state?.Slot != null && state.Slot.gameObject.activeSelf)
                    {
                        previousSlotPositions[state] = state.Slot.anchoredPosition;
                    }
                }
            }
            ConfigureTrayForPieceCount(VisibleTrayCardCount());
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state?.Slot == null || !state.Slot.gameObject.activeSelf)
                {
                    continue;
                }

                ApplyTraySlotLayout(state.SlotLayout);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(trayContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(trayRoot);
            Canvas.ForceUpdateCanvases();
            if (trayScrollRect != null)
            {
                trayScrollRect.horizontalNormalizedPosition = Mathf.Clamp01(previousScroll);
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state?.Rect == null || state.Placed || state.Slot == null || !state.Slot.gameObject.activeSelf ||
                    (drag != null && drag.Piece == state))
                {
                    continue;
                }

                float targetCellSize = TrayCellSize(state);
                if (state.SizeRoutine != null)
                {
                    StopCoroutine(state.SizeRoutine);
                    state.SizeRoutine = null;
                }

                if (animateCatSizes && Host.gameObject.activeInHierarchy && !Mathf.Approximately(state.CellWidth, targetCellSize))
                {
                    state.SizeRoutine = StartCoroutine(AnimateTrayPieceSize(state, targetCellSize));
                }
                else
                {
                    pieceLayout.SetPieceGrid(state, targetCellSize, TrayGap, targetCellSize, TrayGap);
                }

                if (state.SlotMoveRoutine != null)
                {
                    StopCoroutine(state.SlotMoveRoutine);
                    state.SlotMoveRoutine = null;
                }

                if (animateCatSizes && previousSlotPositions.TryGetValue(state, out Vector2 startPosition) &&
                    (!Mathf.Approximately(startPosition.x, state.Slot.anchoredPosition.x) ||
                     !Mathf.Approximately(startPosition.y, state.Slot.anchoredPosition.y)))
                {
                    Vector2 targetPosition = state.Slot.anchoredPosition;
                    state.Slot.anchoredPosition = startPosition;
                    state.SlotMoveRoutine = StartCoroutine(AnimateTraySlotPosition(state, startPosition, targetPosition));
                }
            }

            if (animateCatSizes && !reducedMotion && Host.gameObject.activeInHierarchy)
            {
                for (int i = 0; i < revealed.Count; i++) StartCoroutine(PopIn(revealed[i].Slot));
            }
        }

        private IEnumerator AnimateTraySlotPosition(PieceState state, Vector2 start, Vector2 end)
        {
            if (state?.Slot == null) yield break;
            const float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration && state != null && state.Slot != null && state.Slot.gameObject.activeSelf)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                state.Slot.anchoredPosition = Vector2.Lerp(start, end, t);
                yield return null;
            }

            if (state?.Slot != null && state.Slot.gameObject.activeSelf)
            {
                state.Slot.anchoredPosition = end;
            }
            if (state != null) state.SlotMoveRoutine = null;
        }

        private IEnumerator AnimateTrayPieceSize(PieceState state, float targetCellSize)
        {
            float startWidth = state.CellWidth;
            float startHeight = state.CellHeight;
            float startGapX = state.GapX;
            float startGapY = state.GapY;
            float duration = layoutProfile != null ? layoutProfile.PieceSizeTransitionSeconds : 0.2f;
            float elapsed = 0f;
            while (elapsed < duration && state != null && state.Rect != null && !state.Placed &&
                   (drag == null || drag.Piece != state))
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutCubic(Mathf.Clamp01(elapsed / duration));
                pieceLayout.SetPieceGrid(
                    state,
                    Mathf.Lerp(startWidth, targetCellSize, t),
                    Mathf.Lerp(startGapX, TrayGap, t),
                    Mathf.Lerp(startHeight, targetCellSize, t),
                    Mathf.Lerp(startGapY, TrayGap, t));
                yield return null;
            }

            if (state != null && state.Rect != null && !state.Placed && (drag == null || drag.Piece != state))
            {
                pieceLayout.SetPieceGrid(state, targetCellSize, TrayGap, targetCellSize, TrayGap);
                state.SizeRoutine = null;
            }
        }

        public void AttachPieceToTray(PieceState state)
        {
            state.Placed = false;
            if (state.Slot != null && !state.Slot.gameObject.activeSelf)
            {
                state.Slot.gameObject.SetActive(true);
            }

            state.Rect.SetParent(state.Slot, false);
            state.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            state.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            state.Rect.pivot = new Vector2(0.5f, 0.5f);
            state.Rect.anchoredPosition = Vector2.zero;
            float trayCellSize = TrayCellSize(state);
            pieceLayout.SetPieceGrid(state, trayCellSize, TrayGap, trayCellSize, TrayGap);
            state.SlotImage.color = TraySlotSkin.RestColor;
            state.Rect.localScale = Vector3.one;
            state.Rect.localEulerAngles = Vector3.zero;
        }

        public void UpdateIdleMotion()
        {
            if (pieces.Count == 0 || reducedMotion)
            {
                return;
            }

            float time = Time.unscaledTime * TrayFloatSpeed;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (state == null || state.Rect == null || state.Placed || (drag != null && drag.Piece == state))
                {
                    continue;
                }

                float wave = Mathf.Sin(time + state.FloatPhase);
                state.Rect.anchoredPosition = new Vector2(0f, wave * TrayFloatAmplitude);
                state.Rect.localEulerAngles = new Vector3(0f, 0f, wave * TrayFloatRotation);
            }
        }

        public float TrayCellSize(PieceState state)
        {
            int rows = Mathf.Max(1, state.Definition.Rows);
            int cols = Mathf.Max(1, state.Definition.Cols);
            int maxCells = Mathf.Max(rows, cols);
            float horizontalInset = Mathf.Clamp(traySlotPreferredWidth * 0.16f, 14f, 30f);
            float verticalInset = Mathf.Clamp(traySlotPreferredHeight * 0.22f, 42f, 58f);
            float widthFit = (traySlotPreferredWidth - horizontalInset - ((cols - 1) * TrayGap)) / cols;
            float heightFit = (traySlotPreferredHeight - verticalInset - ((rows - 1) * TrayGap)) / rows;
            float shapeScale = maxCells >= 5 ? 0.82f : maxCells >= 4 ? 0.9f : 1f;
            float shapeCap = trayCellMaxSize * shapeScale;
            float cellSize = Mathf.Min(trayCellMaxSize, shapeCap, widthFit, heightFit);
            return Mathf.Clamp(cellSize, 20f, 64f);
        }

        public Vector2 SlotCenterInRoot(PieceState state)
        {
            return space.ScreenToRootLocal(space.WorldToScreen(state.Slot.position));
        }

        public void SetScrollEnabled(bool enabled)
        {
            if (trayScrollRect == null)
            {
                return;
            }

            if (!enabled)
            {
                trayScrollRect.StopMovement();
            }

            trayScrollRect.enabled = enabled;
        }

        public void PrepareGesture(PointerEventData eventData)
        {
            if (trayScrollRect == null || eventData == null)
            {
                return;
            }

            trayScrollRect.OnInitializePotentialDrag(eventData);
            trayScrollRect.StopMovement();
        }

        public float PieceDragThresholdPixels()
        {
            float configured = layoutProfile != null ? layoutProfile.PiecePickDragThreshold : PiecePickDragThreshold;
            float canvasScale = space.ScaleFactor;
            int eventThreshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 0;
            return Mathf.Max(configured * canvasScale, eventThreshold);
        }

        public float PiecePickVerticalBias()
        {
            return layoutProfile != null ? layoutProfile.PiecePickVerticalBias : PuzzleTuning.PiecePickVerticalBias;
        }

        public float TrayScrollHorizontalBias()
        {
            return layoutProfile != null ? layoutProfile.TrayScrollHorizontalBias : PuzzleTuning.TrayScrollHorizontalBias;
        }

        public void BeginForwardedScroll(PointerEventData eventData)
        {
            if (trayScrollRect == null || eventData == null)
            {
                return;
            }

            trayScrollRect.enabled = true;
            trayScrollRect.OnInitializePotentialDrag(eventData);
            trayScrollRect.OnBeginDrag(eventData);
        }

        public void ForwardScroll(PointerEventData eventData)
        {
            if (trayScrollRect == null || eventData == null)
            {
                return;
            }

            trayScrollRect.OnDrag(eventData);
        }

        public void EndForwardedScroll(PointerEventData eventData)
        {
            if (trayScrollRect == null || eventData == null)
            {
                return;
            }

            trayScrollRect.OnEndDrag(eventData);
        }
    }
}
