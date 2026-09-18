using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class MenuTransition : MonoBehaviour
    {
        [Serializable]
        public sealed class EntranceSettings
        {
            public bool enabled = true;
            [Min(0.1f)] public float duration = 0.42f;
            [Range(0.7f, 1f)] public float startingScale = 0.92f;
            [Min(0f)] public float slideDistance = 40f;
            [Range(0f, 3f)] public float overshoot = 1.8f;
            [Min(0f)] public float cardDelay = 0.08f;
            [Min(0f)] public float cardStagger = 0.035f;
            [Min(0.1f)] public float cardDuration = 0.32f;
            [Range(0.5f, 1f)] public float cardStartingScale = 0.78f;
            [Tooltip("How far below its resting spot each card starts. 0 keeps the plain pop.")]
            [Min(0f)] public float cardSlideDistance = 42f;
            [Tooltip("Degrees each card is rotated at the start, alternating side to side.")]
            [Range(0f, 20f)] public float cardTilt = 5f;
            [Tooltip("Swings each card open around its own top edge. Safe inside a masked scroll view, unlike Unfold.")]
            [Range(0f, 89f)] public float cardUnfoldAngle;
            [Header("Unfold")]
            [Tooltip("Swings the panel open around its top edge, like a chapter opening.")]
            public bool unfold;
            [Range(0f, 89f)] public float unfoldAngle = 82f;
            [Tooltip("Off pivots the swing around the panel centre instead of its top edge.")]
            public bool unfoldFromTop = true;
        }

        private sealed class ItemState
        {
            public Transform target;
            public RectTransform rect;
            public Vector3 scale;
            public Vector2 position;
            public Quaternion rotation;
            public float hinge;
            public CanvasGroup group;
            public float alpha;
        }

        [Serializable]
        public sealed class ExitSettings
        {
            public bool enabled = true;
            [Min(0.1f)] public float duration = 0.3f;
            [Range(0.6f, 1f)] public float endingScale = 0.84f;
            [Min(0f)] public float slideDistance = 65f;
            [Range(0f, 3f)] public float anticipation = 1.4f;
        }

        /// <summary>Shared with the screens that branch on reduced motion before they animate anything.</summary>
        public const string ReducedMotionKey = "CatBlockPuzzle.Settings.ReducedMotion";
        public static bool ReducedMotion => PlayerPrefs.GetInt(ReducedMotionKey, 0) != 0;

        private EntranceSettings entrance = new EntranceSettings();
        private ExitSettings exit;
        private readonly List<ItemState> items = new List<ItemState>();
        private CanvasGroup group;
        private Transform panel;
        private Vector3 restingScale;
        private Coroutine transitionRoutine;
        private bool closing;
        private bool springOpening;
        private RectTransform panelRect;
        private Vector2 restingPosition;
        private Quaternion restingRotation;

        private void Initialize(bool spring, Transform overridePanel)
        {
            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            }
            Transform resolved = overridePanel;
            if (resolved == null && panel != null) return; // Keep the panel resolved on the first open.
            if (resolved == null) resolved = transform.Find("Overlay/PopUp");
            if (resolved == null) resolved = transform.Find("Panel");
            if (resolved == null && spring) resolved = transform.Find("BG/Board");
            if (resolved == null && spring) resolved = transform.Find("BG/Middle");
            if (resolved == null) resolved = transform;
            if (panel == resolved) return;
            panel = resolved;
            restingScale = panel.localScale;
            restingRotation = panel.localRotation;
            panelRect = panel as RectTransform;
            if (panelRect != null) restingPosition = panelRect.anchoredPosition;
        }

        /// <summary>Builds a card list for Show, dropping the slots a screen left unassigned.</summary>
        public static Transform[] Cards(params Component[] parts)
        {
            if (parts == null) return null;
            var cards = new List<Transform>(parts.Length);
            foreach (Component part in parts)
                if (part != null) cards.Add(part.transform);
            return cards.Count > 0 ? cards.ToArray() : null;
        }

        public static void Show(GameObject root, bool spring = false, EntranceSettings settings = null, Transform[] cards = null, Transform panelOverride = null)
        {
            if (root == null) return;
            var transition = root.GetComponent<MenuTransition>();
            if (transition == null) transition = root.AddComponent<MenuTransition>();
            // One accessibility switch for every screen: plain fade, no springs, no staggered cards.
            if (ReducedMotion) { spring = false; cards = null; }
            transition.Initialize(spring, panelOverride);
            // Restore interrupted entrances before capturing the authored card scales.
            if (transition.transitionRoutine != null) transition.StopCoroutine(transition.transitionRoutine);
            transition.RestoreItems();
            transition.entrance = settings ?? new EntranceSettings();
            transition.springOpening = spring && transition.entrance.enabled;
            transition.CaptureItems(transition.springOpening ? cards : null);
            bool wasVisible = root.activeSelf;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            transition.Play(true, !wasVisible, null);
        }

        private void CaptureItems(Transform[] cards)
        {
            items.Clear();
            if (cards == null) return;
            foreach (Transform card in cards)
            {
                if (card == null || card == panel || !card.IsChildOf(transform)) continue;
                if (items.Exists(item => item.target == card)) continue;
                CanvasGroup cardGroup = card.GetComponent<CanvasGroup>();
                if (cardGroup == null) cardGroup = card.gameObject.AddComponent<CanvasGroup>();
                var cardRect = card as RectTransform;
                items.Add(new ItemState
                {
                    target = card,
                    rect = cardRect,
                    scale = card.localScale,
                    position = cardRect != null ? cardRect.anchoredPosition : Vector2.zero,
                    rotation = card.localRotation,
                    hinge = cardRect != null ? cardRect.rect.height * .5f * cardRect.localScale.y : 0f,
                    group = cardGroup,
                    alpha = cardGroup.alpha
                });
            }
        }

        private void RestoreItems()
        {
            foreach (ItemState item in items)
            {
                if (item.target != null)
                {
                    item.target.localScale = item.scale;
                    item.target.localRotation = item.rotation;
                }
                if (item.rect != null) item.rect.anchoredPosition = item.position;
                if (item.group != null) item.group.alpha = item.alpha;
            }
        }

        /// <summary>Places one card between its packed pose (motion 0) and its resting pose (motion 1).</summary>
        private void ApplyCard(ItemState item, int index, float motion)
        {
            if (item.target != null)
            {
                item.target.localScale = item.scale * Mathf.LerpUnclamped(entrance.cardStartingScale, 1f, motion);
                // Cards alternate their lean so a row of them unpacks instead of marching in.
                float tilt = entrance.cardTilt > 0f
                    ? Mathf.LerpUnclamped(index % 2 == 0 ? entrance.cardTilt : -entrance.cardTilt, 0f, motion)
                    : 0f;
                float unfoldX = entrance.cardUnfoldAngle > 0f
                    ? Mathf.LerpUnclamped(-entrance.cardUnfoldAngle, 0f, motion)
                    : 0f;
                if (tilt != 0f || unfoldX != 0f)
                    item.target.localRotation = item.rotation * Quaternion.Euler(unfoldX, 0f, tilt);
                else item.target.localRotation = item.rotation;
                if (item.rect != null)
                {
                    Vector2 position = entrance.cardSlideDistance > 0f
                        ? Vector2.LerpUnclamped(item.position + Vector2.down * entrance.cardSlideDistance, item.position, motion)
                        : item.position;
                    // Pin the top edge so an unfolding card swings down rather than tipping about its middle.
                    if (unfoldX != 0f && entrance.unfoldFromTop)
                        position.y += item.hinge * (1f - Mathf.Cos(unfoldX * Mathf.Deg2Rad));
                    item.rect.anchoredPosition = position;
                }
            }
        }

        public static void Hide(GameObject root, Action completed = null, bool animate = true, ExitSettings settings = null)
        {
            if (root == null) { completed?.Invoke(); return; }
            var transition = root.GetComponent<MenuTransition>();
            if (!animate || transition == null || !root.activeInHierarchy)
            {
                root.SetActive(false);
                completed?.Invoke();
                return;
            }
            if (!transition.closing)
            {
                transition.exit = settings;
                transition.Play(false, false, completed);
            }
        }

        private void Play(bool show, bool fresh, Action completed)
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            closing = !show;
            group.interactable = false;
            group.blocksRaycasts = true;
            if (fresh)
            {
                group.alpha = 0f;
                panel.localScale = restingScale * (springOpening ? entrance.startingScale : .94f);
                if (springOpening && panelRect != null)
                    panelRect.anchoredPosition = restingPosition + Vector2.down * entrance.slideDistance;
                for (int i = 0; i < items.Count; i++)
                {
                    ApplyCard(items[i], i, 0f);
                    if (items[i].group != null) items[i].group.alpha = 0f;
                }
            }
            transitionRoutine = StartCoroutine(Animate(show, completed));
        }

        private IEnumerator Animate(bool show, Action completed)
        {
            float fromAlpha = group.alpha;
            Vector3 fromScale = panel.localScale;
            Vector2 fromPosition = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;
            bool juicyClose = !show && exit != null && exit.enabled;
            bool unfolding = show && springOpening && entrance.unfold;
            float hingeOffset = unfolding && panelRect != null ? panelRect.rect.height * .5f * restingScale.y : 0f;
            float duration = show ? (springOpening ? Mathf.Max(.1f, entrance.duration) : .22f)
                : (juicyClose ? Mathf.Max(.1f, exit.duration) : .16f);
            float totalDuration = show && springOpening && items.Count > 0
                ? Mathf.Max(duration, Mathf.Max(0f, entrance.cardDelay) + (items.Count - 1) * Mathf.Max(0f, entrance.cardStagger) + Mathf.Max(.1f, entrance.cardDuration))
                : duration;
            for (float elapsed = 0f; elapsed < totalDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float motion = show && springOpening ? EaseOutBack(t, entrance.overshoot) : eased;
                if (juicyClose) motion = (exit.anticipation + 1f) * t * t * t - exit.anticipation * t * t;
                float fade = show && springOpening ? Mathf.Clamp01(t * 2.5f) : eased;
                if (juicyClose) fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.15f, 1f, t));
                group.alpha = Mathf.Lerp(fromAlpha, show ? 1f : 0f, fade);
                panel.localScale = Vector3.LerpUnclamped(fromScale, restingScale * (show ? 1f : (juicyClose ? exit.endingScale : .96f)), motion);
                Vector2 panelPosition = juicyClose
                    ? Vector2.Lerp(fromPosition, restingPosition + Vector2.down * exit.slideDistance, t * t * t)
                    : Vector2.LerpUnclamped(fromPosition, restingPosition, motion);
                if (unfolding)
                {
                    float angle = Mathf.LerpUnclamped(-entrance.unfoldAngle, 0f, motion);
                    panel.localRotation = restingRotation * Quaternion.Euler(angle, 0f, 0f);
                    // Pin the top edge so the body swings down into place instead of tipping about its middle.
                    if (entrance.unfoldFromTop)
                        panelPosition.y += hingeOffset * (1f - Mathf.Cos(angle * Mathf.Deg2Rad));
                }
                if (panelRect != null) panelRect.anchoredPosition = panelPosition;
                if (juicyClose)
                    foreach (ItemState item in items)
                        if (item.target != null) item.target.localScale = item.scale * (1f - .12f * t * t);
                if (show && springOpening)
                    for (int i = 0; i < items.Count; i++)
                    {
                        ItemState item = items[i];
                        float cardTime = Mathf.Clamp01((elapsed - Mathf.Max(0f, entrance.cardDelay) - i * Mathf.Max(0f, entrance.cardStagger)) / Mathf.Max(.1f, entrance.cardDuration));
                        ApplyCard(item, i, EaseOutBack(cardTime, entrance.overshoot));
                        if (item.group != null) item.group.alpha = item.alpha * Mathf.Clamp01(cardTime * 3f);
                    }
                yield return null;
            }
            group.alpha = show ? 1f : 0f;
            panel.localScale = restingScale;
            panel.localRotation = restingRotation;
            if (panelRect != null) panelRect.anchoredPosition = restingPosition;
            group.interactable = show;
            RestoreItems();
            transitionRoutine = null;
            if (!show) gameObject.SetActive(false);
            completed?.Invoke();
        }

        private static float EaseOutBack(float t, float overshoot)
        {
            float x = t - 1f;
            return 1f + (1f + overshoot) * x * x * x + overshoot * x * x;
        }

        private void OnDisable()
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            closing = false;
            RestoreItems();
            if (group == null || panel == null) return;
            panel.localScale = restingScale;
            panel.localRotation = restingRotation;
            if (panelRect != null) panelRect.anchoredPosition = restingPosition;
            group.alpha = 1f;
            group.interactable = true;
        }
    }
}
