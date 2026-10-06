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
    /// Pooled particle-style effects (sparks, rings, paw bursts, flying coins) and the
    /// small transform animations (pop, squish, shake, cell flash) that decorate gameplay.
    /// The pool is the fixed set of effect images baked into the scene; nothing is created at runtime.
    /// </summary>
    internal sealed class FxSpawner : HostedService
    {
        private readonly CanvasSpace space;
        private readonly PlayerSettings settings;
        private readonly Stack<Image> fxImagePool = new Stack<Image>(64);
        private float nextDragTrailTime;

        public FxSpawner(IGameplayContext context, CanvasSpace space, PlayerSettings settings) : base(context)
        {
            this.space = space;
            this.settings = settings;
        }

        private RectTransform fxLayer => Hud.fxLayer;
        private Image[] preparedEffects => Hud.preparedEffects;
        private Sprite circleSprite => Assets.circleSprite;
        private Sprite pawSprite => Assets.pawSprite;
        private Sprite coinSprite => Assets.coinSprite;
        private bool reducedMotion => settings.ReducedMotion;
        private Vector2 ScreenCenterToRootLocal(Vector2 screen) => space.ScreenToRootLocal(screen);

        public void ResetDragTrailTimer()
        {
            nextDragTrailTime = 0f;
        }

        public void SpawnDragTrail(Vector2 screenPosition, Color color)
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

        public void SpawnSnapRing(Vector2 screenPosition, Color color)
        {
            Image ring = AcquireFxImage("Snap Ring", fxLayer, color);
            if (ring == null) return;
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

        /// <summary>Radial paw sparks inside a board-space parent (the board root during play).</summary>
        public void SpawnPawBurst(RectTransform parent, Vector2 localPosition, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / Mathf.Max(1, count);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpawnSpark(parent, localPosition, direction * (42f + ((i % 4) * 16f)), i % 2 == 0 ? GoldColor : color, 16f + ((i % 3) * 3f), pawSprite);
            }
        }

        public void SpawnBoardBurst(RectTransform boardRoot)
        {
            SpawnPawBurst(boardRoot, Vector2.zero, reducedMotion ? 8 : 42, TargetColor);
        }

        public void SpawnFixedBurst(Vector2 screenPosition, int count, Color color)
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
            if (spark == null) return;
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

        /// <summary>Arcs a handful of coins from a screen point to the HUD wallet. Returns the total flight time.</summary>
        public float FlyCoins(int reward, Vector2 start)
        {
            Vector2 end = space.WorldToScreen(Hud.coinText.rectTransform.position);
            int count = Mathf.Clamp(Mathf.RoundToInt(reward / 7f), 4, 9);
            const float flightSeconds = 0.62f;
            const float staggerSeconds = 0.055f;
            Vector2 rootEnd = ScreenCenterToRootLocal(end);
            for (int i = 0; i < count; i++)
            {
                Image coin = AcquireFxImage("Flying Coin", fxLayer, Color.white);
                if (coin == null) break;
                coin.sprite = coinSprite;
                coin.raycastTarget = false;
                Vector2 rootStart = ScreenCenterToRootLocal(start + new Vector2((i - ((count - 1) * 0.5f)) * 12f, (i % 2) * 14f));
                float arcSide = (i - ((count - 1) * 0.5f)) * 26f;
                Vector2 arc = Vector2.Lerp(rootStart, rootEnd, 0.45f) + new Vector2(arcSide, 160f + ((i % 3) * 18f));
                SetRect(coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rootStart, new Vector2(28f, 28f));
                StartCoroutine(AnimateCoin(coin, rootStart, arc, rootEnd, i * staggerSeconds, flightSeconds, i % 2 == 0 ? 1f : -1f, i == 0));
            }

            return ((count - 1) * staggerSeconds) + flightSeconds;
        }

        private IEnumerator AnimateCoin(Image coin, Vector2 start, Vector2 arc, Vector2 end, float delay, float duration, float spinDirection, bool collectSound)
        {
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);
                float inverse = 1f - eased;
                coin.rectTransform.anchoredPosition = (inverse * inverse * start) + (2f * inverse * eased * arc) + (eased * eased * end);
                coin.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.38f, eased);
                coin.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spinDirection * Mathf.Lerp(0f, 310f, eased));
                yield return null;
            }

            // One "collected" chime as the first coin reaches the wallet.
            if (collectSound) ButtonSfx.PlayCoin();
            ReleaseFxImage(coin);
        }

        public IEnumerator PopTransform(RectTransform rect, float peakScale)
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

        public IEnumerator SquishLandTransform(RectTransform rect)
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

        public IEnumerator ShakeTransform(RectTransform rect)
        {
            if (reducedMotion || rect == null)
            {
                yield break;
            }

            Quaternion startRotation = rect.localRotation;
            float elapsed = 0f;
            const float duration = 0.2f;
            while (elapsed < duration && rect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float falloff = 1f - t;
                float angle = Mathf.Sin(t * Mathf.PI * 5f) * 4.5f * falloff;
                rect.localRotation = startRotation * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            if (rect != null) rect.localRotation = startRotation;
        }

        public IEnumerator FlashCell(Image image, float delay)
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

        private Image AcquireFxImage(string name, RectTransform parent, Color color)
        {
            Image image = null;
            while (fxImagePool.Count > 0 && image == null)
            {
                image = fxImagePool.Pop();
            }

            if (image == null)
            {
                return null;
            }

            image.gameObject.SetActive(true);
            image.transform.SetParent(parent, false);
            image.gameObject.name = name;
            image.color = color;
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

        public void ResetPool()
        {
            fxImagePool.Clear();
            if (fxLayer == null || preparedEffects == null)
            {
                return;
            }

            foreach (Image image in preparedEffects)
            {
                if (image == null) continue;
                image.gameObject.SetActive(false);
                image.transform.SetParent(fxLayer, false);
                fxImagePool.Push(image);
            }
        }
    }
}
