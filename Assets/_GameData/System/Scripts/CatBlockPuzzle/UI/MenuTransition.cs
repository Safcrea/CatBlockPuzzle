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
        }

        private sealed class ItemState
        {
            public Transform target;
            public Vector3 scale;
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

        private void Initialize(bool spring)
        {
            if (group != null) return;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            panel = transform.Find("Overlay/PopUp");
            if (panel == null) panel = transform.Find("Panel");
            if (panel == null && spring) panel = transform.Find("BG/Board");
            if (panel == null && spring) panel = transform.Find("BG/Middle");
            if (panel == null) panel = transform;
            restingScale = panel.localScale;
            panelRect = panel as RectTransform;
            if (panelRect != null) restingPosition = panelRect.anchoredPosition;
        }

        public static void Show(GameObject root, bool spring = false, EntranceSettings settings = null, Transform[] cards = null)
        {
            if (root == null) return;
            var transition = root.GetComponent<MenuTransition>();
            if (transition == null) transition = root.AddComponent<MenuTransition>();
            transition.Initialize(spring);
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
                items.Add(new ItemState { target = card, scale = card.localScale, group = cardGroup, alpha = cardGroup.alpha });
            }
        }

        private void RestoreItems()
        {
            foreach (ItemState item in items)
            {
                if (item.target != null) item.target.localScale = item.scale;
                if (item.group != null) item.group.alpha = item.alpha;
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
                foreach (ItemState item in items)
                {
                    if (item.target != null) item.target.localScale = item.scale * entrance.cardStartingScale;
                    if (item.group != null) item.group.alpha = 0f;
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
                if (panelRect != null)
                    panelRect.anchoredPosition = juicyClose
                        ? Vector2.Lerp(fromPosition, restingPosition + Vector2.down * exit.slideDistance, t * t * t)
                        : Vector2.LerpUnclamped(fromPosition, restingPosition, motion);
                if (juicyClose)
                    foreach (ItemState item in items)
                        if (item.target != null) item.target.localScale = item.scale * (1f - .12f * t * t);
                if (show && springOpening)
                    for (int i = 0; i < items.Count; i++)
                    {
                        ItemState item = items[i];
                        float cardTime = Mathf.Clamp01((elapsed - Mathf.Max(0f, entrance.cardDelay) - i * Mathf.Max(0f, entrance.cardStagger)) / Mathf.Max(.1f, entrance.cardDuration));
                        float cardScale = Mathf.LerpUnclamped(entrance.cardStartingScale, 1f, EaseOutBack(cardTime, entrance.overshoot));
                        if (item.target != null) item.target.localScale = item.scale * cardScale;
                        if (item.group != null) item.group.alpha = item.alpha * Mathf.Clamp01(cardTime * 3f);
                    }
                yield return null;
            }
            group.alpha = show ? 1f : 0f;
            panel.localScale = restingScale;
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
            if (group == null) return;
            panel.localScale = restingScale;
            if (panelRect != null) panelRect.anchoredPosition = restingPosition;
            group.alpha = 1f;
            group.interactable = true;
        }
    }
}
