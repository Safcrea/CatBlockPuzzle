using System;
using System.Collections;
using UnityEngine;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class MenuTransition : MonoBehaviour
    {
        private CanvasGroup group;
        private Transform panel;
        private Vector3 restingScale;
        private Coroutine transitionRoutine;
        private bool closing;

        private void Initialize()
        {
            if (group != null) return;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            panel = transform.Find("Overlay/PopUp");
            if (panel == null) panel = transform.Find("Panel");
            if (panel == null) panel = transform;
            restingScale = panel.localScale;
        }

        public static void Show(GameObject root)
        {
            if (root == null) return;
            var transition = root.GetComponent<MenuTransition>();
            if (transition == null) transition = root.AddComponent<MenuTransition>();
            transition.Initialize();
            bool wasVisible = root.activeSelf;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            transition.Play(true, !wasVisible, null);
        }

        public static void Hide(GameObject root, Action completed = null, bool animate = true)
        {
            if (root == null) { completed?.Invoke(); return; }
            var transition = root.GetComponent<MenuTransition>();
            if (!animate || transition == null || !root.activeInHierarchy)
            {
                root.SetActive(false);
                completed?.Invoke();
                return;
            }
            if (!transition.closing) transition.Play(false, false, completed);
        }

        private void Play(bool show, bool fresh, Action completed)
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            closing = !show;
            group.interactable = false;
            group.blocksRaycasts = true;
            if (fresh) { group.alpha = 0f; panel.localScale = restingScale * .94f; }
            transitionRoutine = StartCoroutine(Animate(show, completed));
        }

        private IEnumerator Animate(bool show, Action completed)
        {
            float fromAlpha = group.alpha;
            Vector3 fromScale = panel.localScale;
            float duration = show ? .22f : .16f;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                group.alpha = Mathf.Lerp(fromAlpha, show ? 1f : 0f, eased);
                panel.localScale = Vector3.LerpUnclamped(fromScale, restingScale * (show ? 1f : .96f), eased);
                yield return null;
            }
            group.alpha = show ? 1f : 0f;
            panel.localScale = restingScale;
            group.interactable = show;
            transitionRoutine = null;
            if (!show) gameObject.SetActive(false);
            completed?.Invoke();
        }

        private void OnDisable()
        {
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            closing = false;
            if (group == null) return;
            panel.localScale = restingScale;
            group.alpha = 1f;
            group.interactable = true;
        }
    }
}
