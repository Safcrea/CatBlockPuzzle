using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;
using CatBlockPuzzle.KawaiiUI;

namespace CatBlockPuzzle
{
    public sealed partial class CatBlockPuzzleGame
    {
        private void ResetLevelTimer()
        {
            timerRunning = false;
            levelFailed = false;
            levelRemainingSeconds = LevelDurationSeconds;
            lastTimerSecond = Mathf.CeilToInt(levelRemainingSeconds);
            if (timerPulseRoutine != null)
            {
                StopCoroutine(timerPulseRoutine);
                timerPulseRoutine = null;
            }

            UpdateTimerDisplay();
            if (timerText != null)
            {
                timerText.rectTransform.localScale = Vector3.one;
            }
        }

        private void StartLevelTimer()
        {
            if (levelFailed || winOverlay.gameObject.activeSelf || failOverlay.gameObject.activeSelf)
            {
                return;
            }

            timerRunning = true;
            UpdateTimerDisplay();
        }

        private void StopLevelTimer()
        {
            timerRunning = false;
            if (timerPulseRoutine != null)
            {
                StopCoroutine(timerPulseRoutine);
                timerPulseRoutine = null;
            }

            if (timerText != null)
            {
                timerText.rectTransform.localScale = Vector3.one;
            }
        }

        private void UpdateLevelTimer()
        {
            if (!timerRunning || levelFailed)
            {
                return;
            }

            levelRemainingSeconds -= Time.unscaledDeltaTime;
            if (levelRemainingSeconds <= 0f)
            {
                levelRemainingSeconds = 0f;
                lastTimerSecond = 0;
                UpdateTimerDisplay();
                FailLevel();
                return;
            }

            int currentSecond = Mathf.CeilToInt(levelRemainingSeconds);
            if (currentSecond == lastTimerSecond)
            {
                return;
            }

            lastTimerSecond = currentSecond;
            UpdateTimerDisplay();
            if (levelRemainingSeconds <= TimerWarningSeconds)
            {
                StartTimerPulse();
            }
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, levelRemainingSeconds));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timerText.text = minutes.ToString() + ":" + seconds.ToString("00");
            timerText.color = levelRemainingSeconds <= TimerWarningSeconds ? TimerWarningColor : TimerNormalColor;
            UpdateStarDisplay();
        }

        private void StartTimerPulse()
        {
            if (timerText == null)
            {
                return;
            }

            if (timerPulseRoutine != null)
            {
                StopCoroutine(timerPulseRoutine);
            }

            timerPulseRoutine = StartCoroutine(PulseTimer());
        }

        private IEnumerator PulseTimer()
        {
            RectTransform rect = timerText.rectTransform;
            float elapsed = 0f;
            const float seconds = 0.34f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(Mathf.Clamp01(elapsed / seconds) * Mathf.PI);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, TimerPulseScale, wave);
                yield return null;
            }

            rect.localScale = Vector3.one;
            timerPulseRoutine = null;
        }

        private void FailLevel()
        {
            if (levelFailed || winOverlay.gameObject.activeSelf)
            {
                return;
            }

            levelFailed = true;
            timerRunning = false;
            inputLocked = true;
            StopHint();
            CancelActiveDragToRest();
            PlayClip(wrongClip);
            failOverlay.gameObject.SetActive(true);
            failPanel.localScale = Vector3.one * 0.88f;
            StartCoroutine(PopTransform(failPanel, 1.03f));
        }

        private void CancelActiveDragToRest()
        {
            if (drag == null)
            {
                return;
            }

            DragState cancelled = drag;
            drag = null;
            SetTrayScrollEnabled(true);
            ClearPreview();
            if (trayImage != null)
            {
                trayImage.color = activeTrayColor;
            }

            PieceState state = cancelled.Piece;
            if (state == null)
            {
                return;
            }

            if (cancelled.PreviousPlaced)
            {
                state.Placed = true;
                state.Row = cancelled.PreviousRow;
                state.Col = cancelled.PreviousCol;
                OccupyCells(state, state.Row, state.Col);
                AttachPieceToBoard(state);
            }
            else
            {
                AttachPieceToTray(state);
            }
        }

        private void CheckWin()
        {
            if (occupancy.Count != activeLevel.ActiveCells.Count)
            {
                return;
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                if (!pieces[i].Placed)
                {
                    return;
                }
            }

            inputLocked = true;
            StopLevelTimer();
            StartCoroutine(WinSequence());
        }

        private IEnumerator WinSequence()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                SetCatMood(pieces[i], CatMood.Happy);
                StartCoroutine(PopTransform(pieces[i].Rect, 1.08f));
            }

            SpawnBoardBurst();
            haptics.PlayLevelComplete();
            FlyCoins(activeLevel.Reward);
            PlayWinSound();
            yield return new WaitForSecondsRealtime(0.75f);
            if (!levelNavigationTesting)
            {
                coins += activeLevel.Reward;
            }

            coinText.text = coins.ToString();
            LevelResult result = SaveLevelResult();
            SaveProgress();
            StartCoroutine(PopTransform(coinText.rectTransform.parent as RectTransform, 1.08f));
            winTitleText.text = activeLevel.Title;
            winRewardText.text = "+" + activeLevel.Reward.ToString() + " coins";
            ShowWinResult(result);
            winOverlay.gameObject.SetActive(true);
            winPanel.localScale = Vector3.one * 0.88f;
            StartCoroutine(PopTransform(winPanel, 1.03f));
        }

        private void ShowHint()
        {
            if (inputLocked)
            {
                return;
            }

            StopHint();
            PieceState hinted = null;
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceState state = pieces[i];
                if (!state.Placed || state.Row != state.Definition.SolutionRow || state.Col != state.Definition.SolutionCol)
                {
                    hinted = state;
                    break;
                }
            }

            if (hinted != null)
            {
                hintRoutine = StartCoroutine(HintRoutine(hinted));
            }
        }

        private IEnumerator HintRoutine(PieceState state)
        {
            List<Image> hintedCells = new List<Image>();
            foreach (CellOffset cell in state.Definition.Cells)
            {
                Vector2Int coord = new Vector2Int(state.Definition.SolutionRow + cell.Row, state.Definition.SolutionCol + cell.Col);
                if (boardCells.TryGetValue(coord, out CellView cellView))
                {
                    hintedCells.Add(cellView.Image);
                }
            }

            float elapsed = 0f;
            while (elapsed < 1.7f)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(elapsed * 11f) * 0.5f + 0.5f;
                state.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, wave);
                state.SlotImage.color = Color.Lerp(CardRestColor, new Color(1f, 0.92f, 0.55f, 0.92f), wave);
                for (int i = 0; i < hintedCells.Count; i++)
                {
                    hintedCells[i].color = Color.Lerp(TargetColor, GoldColor, wave);
                }

                yield return null;
            }

            state.Rect.localScale = Vector3.one;
            state.SlotImage.color = state.Placed ? CardDimColor : CardRestColor;
            for (int i = 0; i < hintedCells.Count; i++)
            {
                hintedCells[i].color = TargetColor;
            }

            hintRoutine = null;
        }

        private void StopHint()
        {
            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].Rect.localScale = Vector3.one;
                pieces[i].SlotImage.color = pieces[i].Placed ? CardDimColor : CardRestColor;
            }

            foreach (KeyValuePair<Vector2Int, CellView> cell in boardCells)
            {
                if (activeLevel != null && activeLevel.ActiveCells.Contains(cell.Key))
                {
                    cell.Value.Image.color = TargetColor;
                }
            }
        }

        private void PlayPickupFeedback(PieceState state, Vector2 screenPosition)
        {
            PlayClip(buttonClip);
            haptics?.PlayPickup();
            Color accent = Color.Lerp(state.Color, Color.white, 0.28f);
            SpawnSnapRing(screenPosition, accent);
            SpawnFixedBurst(screenPosition, reducedMotion ? 2 : 7, accent);
            if (state.Slot != null)
            {
                StartCoroutine(PopTransform(state.Slot, 1.025f));
            }
        }

        private void PlaySnapFeedback(PieceState state, int row, int col)
        {
            PlayClip(snapClip);
            haptics.PlaySnap();
            Vector2 centerScreen = BoardPieceCenterScreen(state, row, col);
            SpawnSnapRing(centerScreen, state.Color);
            StartCoroutine(SpawnPlacementEcho(centerScreen, state.Color));
            FlashPlacedCells(state, row, col);
            SpawnPawBurst(BoardPieceCenterInBoard(state, row, col), reducedMotion ? 4 : 13, state.Color);
            StartCoroutine(PopTransform(boardRoot, 1.015f));
        }

        private IEnumerator SpawnPlacementEcho(Vector2 screenPosition, Color color)
        {
            if (reducedMotion)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.065f);
            SpawnSnapRing(screenPosition, Color.Lerp(color, Color.white, 0.32f));
        }

        private void PlayWrongFeedback(PieceState state)
        {
            ShowWorriedMood(state);
            PlayClip(wrongClip);
            haptics.PlayWrongMove();
            SpawnFixedBurst(RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, state.Rect.position), 12, InvalidColor);
            StartCoroutine(ShakeTransform(boardRoot));
        }

        private void PlayShelfReturnFeedback()
        {
            PlayClip(buttonClip);
            StartCoroutine(PopTransform(trayRoot, 1.02f));
        }

        private void PlayWinSound()
        {
            PlayClip(winClip);
        }

        private void FlashPlacedCells(PieceState state, int row, int col)
        {
            for (int i = 0; i < state.Definition.Cells.Length; i++)
            {
                CellOffset cell = state.Definition.Cells[i];
                Vector2Int coord = new Vector2Int(row + cell.Row, col + cell.Col);
                if (boardCells.TryGetValue(coord, out CellView cellView))
                {
                    StartCoroutine(FlashCell(cellView.Image, i * 0.025f));
                }
            }
        }

        private IEnumerator FlashCell(Image image, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            float elapsed = 0f;
            while (elapsed < 0.34f)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(Mathf.Clamp01(elapsed / 0.34f) * Mathf.PI);
                image.color = Color.Lerp(TargetColor, ValidColor, wave);
                image.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, wave);
                yield return null;
            }

            image.color = TargetColor;
            image.rectTransform.localScale = Vector3.one;
        }

        private IEnumerator PopTransform(RectTransform rect, float peakScale)
        {
            if (reducedMotion)
            {
                rect.localScale = Vector3.one;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < PopSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(Mathf.Clamp01(elapsed / PopSeconds) * Mathf.PI);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, peakScale, wave);
                yield return null;
            }

            rect.localScale = Vector3.one;
        }

        private IEnumerator SquishLandTransform(RectTransform rect)
        {
            if (rect == null)
            {
                yield break;
            }

            if (reducedMotion)
            {
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                yield break;
            }

            const float duration = 0.32f;
            Vector2 squash = new Vector2(1.12f, 0.88f);
            Vector2 rebound = new Vector2(0.96f, 1.07f);
            float elapsed = 0f;
            while (elapsed < duration && rect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector2 scale;
                if (t < 0.24f)
                {
                    scale = Vector2.Lerp(Vector2.one, squash, EaseOutCubic(t / 0.24f));
                }
                else if (t < 0.58f)
                {
                    scale = Vector2.Lerp(squash, rebound, EaseOutCubic((t - 0.24f) / 0.34f));
                }
                else
                {
                    scale = Vector2.Lerp(rebound, Vector2.one, EaseOutCubic((t - 0.58f) / 0.42f));
                }

                float wobble = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * 2.2f;
                rect.localScale = new Vector3(scale.x, scale.y, 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, wobble);
                yield return null;
            }

            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }
        }

        private IEnumerator ShakeTransform(RectTransform rect)
        {
            if (reducedMotion)
            {
                yield break;
            }

            Vector2 start = rect.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.unscaledDeltaTime;
                float falloff = 1f - Mathf.Clamp01(elapsed / 0.25f);
                rect.anchoredPosition = start + new Vector2(Mathf.Sin(elapsed * 90f) * 16f * falloff, Mathf.Sin(elapsed * 52f) * 4f * falloff);
                yield return null;
            }

            rect.anchoredPosition = start;
        }

        private void SpawnDragTrail(Vector2 screenPosition, Color color)
        {
            if (reducedMotion)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < nextDragTrailTime)
            {
                return;
            }

            nextDragTrailTime = now + 0.04f;
            SpawnSpark(fxLayer, ScreenCenterToRootLocal(screenPosition), new Vector2(0f, -24f), color, 15f, pawSprite);
        }

        private void SpawnSnapRing(Vector2 screenPosition, Color color)
        {
            Image ring = AcquireFxImage("Snap Ring", fxLayer, color);
            ring.sprite = circleSprite;
            ring.raycastTarget = false;
            SetRect(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ScreenCenterToRootLocal(screenPosition), new Vector2(42f, 42f));
            StartCoroutine(AnimateRing(ring));
        }

        private IEnumerator AnimateRing(Image ring)
        {
            float elapsed = 0f;
            Color startColor = ring.color;
            while (elapsed < 0.48f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.48f);
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.35f, 2.8f, EaseOutCubic(t));
                ring.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(0.55f, 0f, t));
                yield return null;
            }

            ReleaseFxImage(ring);
        }

        private void SpawnPawBurst(Vector2 boardLocalPosition, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / Mathf.Max(1, count);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnSpark(boardRoot, boardLocalPosition, direction * (42f + ((i % 4) * 16f)), i % 2 == 0 ? GoldColor : color, 16f + ((i % 3) * 3f), pawSprite);
            }
        }

        private void SpawnBoardBurst()
        {
            SpawnPawBurst(Vector2.zero, reducedMotion ? 8 : 42, TargetColor);
        }

        private void SpawnFixedBurst(Vector2 screenPosition, int count, Color color)
        {
            Vector2 start = ScreenCenterToRootLocal(screenPosition);
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / Mathf.Max(1, count);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnSpark(fxLayer, start, direction * (26f + ((i % 3) * 11f)), color, count == 1 ? 12f : 14f);
            }
        }

        private void SpawnSpark(RectTransform parent, Vector2 start, Vector2 delta, Color color, float size, Sprite sprite = null)
        {
            Image spark = AcquireFxImage("Spark", parent, color);
            spark.sprite = sprite != null ? sprite : circleSprite;
            spark.raycastTarget = false;
            SetRect(spark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), start, new Vector2(size, size));
            StartCoroutine(AnimateSpark(spark, start, start + delta));
        }

        private IEnumerator AnimateSpark(Image spark, Vector2 start, Vector2 end)
        {
            float elapsed = 0f;
            Color startColor = spark.color;
            while (elapsed < SparkSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / SparkSeconds);
                spark.rectTransform.anchoredPosition = Vector2.Lerp(start, end, EaseOutCubic(t));
                spark.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, t);
                spark.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(0.95f, 0f, t));
                yield return null;
            }

            ReleaseFxImage(spark);
        }

        private void FlyCoins(int reward)
        {
            Vector2 start = BoardCenterScreen();
            Vector2 end = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, coinText.rectTransform.position);
            int count = Mathf.Clamp(Mathf.RoundToInt(reward / 7f), 4, 9);
            for (int i = 0; i < count; i++)
            {
                Image coin = AcquireFxImage("Flying Coin", fxLayer, Color.white);
                coin.sprite = coinSprite;
                coin.raycastTarget = false;
                SetRect(coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ScreenCenterToRootLocal(start + new Vector2((i - count * 0.5f) * 10f, (i % 2) * 12f)), new Vector2(28f, 28f));
                StartCoroutine(AnimateCoin(coin, ScreenCenterToRootLocal(start), ScreenCenterToRootLocal(end), i * 0.045f));
            }
        }

        private IEnumerator AnimateCoin(Image coin, Vector2 start, Vector2 end, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            float elapsed = 0f;
            while (elapsed < 0.52f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.52f);
                coin.rectTransform.anchoredPosition = Vector2.Lerp(start, end, EaseOutCubic(t));
                coin.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, t);
                yield return null;
            }

            ReleaseFxImage(coin);
        }

        private Image AcquireFxImage(string name, RectTransform parent, Color color)
        {
            Image image = null;
            while (fxImagePool.Count > 0 && image == null)
            {
                image = fxImagePool.Pop();
            }

            if (image == null)
            {
                image = CreateImage(parent, name, color);
            }
            else
            {
                image.gameObject.SetActive(true);
                image.transform.SetParent(parent, false);
                image.gameObject.name = name;
                image.color = color;
            }

            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.localRotation = Quaternion.identity;
            return image;
        }

        private void ReleaseFxImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.gameObject.SetActive(false);
            image.transform.SetParent(fxLayer, false);
            fxImagePool.Push(image);
        }

        private void ResetFxPool()
        {
            fxImagePool.Clear();
            if (fxLayer == null)
            {
                return;
            }

            for (int i = fxLayer.childCount - 1; i >= 0; i--)
            {
                Transform child = fxLayer.GetChild(i);
                Image image = child.GetComponent<Image>();
                if (image == null)
                {
                    Destroy(child.gameObject);
                    continue;
                }

                image.gameObject.SetActive(false);
                fxImagePool.Push(image);
            }
        }
    }
}
