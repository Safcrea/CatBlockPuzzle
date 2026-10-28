using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>A short message pill over the gameplay board ("Here's a free hint!"); it pops in, waits and fades.</summary>
    internal sealed class GameplayToast : HostedService
    {
        private const string ToastName = "Gameplay Toast";
        private RectTransform toast;
        private Text label;
        private Coroutine routine;

        public GameplayToast(IGameplayContext context) : base(context) { }

        public bool IsShowing => toast != null && toast.gameObject.activeSelf;
        public string Message => label != null ? label.text : string.Empty;

        public void Show(string message, float seconds = 1.8f)
        {
            if (Hud == null || Hud.Canvas == null) return;
            EnsureToast();
            label.text = message;
            toast.SetAsLastSibling();
            toast.gameObject.SetActive(true);
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(seconds));
        }

        public void Hide()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (toast != null) toast.gameObject.SetActive(false);
        }

        private IEnumerator Run(float seconds)
        {
            CanvasGroup group = toast.GetComponent<CanvasGroup>();
            float elapsed = 0f;
            const float popSeconds = 0.2f, fadeSeconds = 0.3f;
            while (elapsed < seconds + fadeSeconds && toast != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float pop = Mathf.Clamp01(elapsed / popSeconds);
                toast.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, 1f - (1f - pop) * (1f - pop));
                group.alpha = elapsed <= seconds ? 1f : 1f - Mathf.Clamp01((elapsed - seconds) / fadeSeconds);
                yield return null;
            }

            if (toast != null) toast.gameObject.SetActive(false);
            routine = null;
        }

        private void EnsureToast()
        {
            if (toast != null) return;
            RectTransform parent = Hud.GameplayScreen != null ? Hud.GameplayScreen : (RectTransform)Hud.Canvas.transform;
            Transform existing = parent.Find(ToastName);
            if (existing != null)
            {
                toast = (RectTransform)existing;
                label = toast.GetComponentInChildren<Text>(true);
                return;
            }

            toast = new GameObject(ToastName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup)).GetComponent<RectTransform>();
            toast.SetParent(parent, false);
            toast.anchorMin = toast.anchorMax = new Vector2(0.5f, 0.5f);
            toast.pivot = new Vector2(0.5f, 0.5f);
            toast.sizeDelta = new Vector2(640f, 112f);
            // Just above the board area, clear of the HUD at the top.
            toast.anchoredPosition = Hud.boardArea != null
                ? (Vector2)parent.InverseTransformPoint(Hud.boardArea.TransformPoint(new Vector3(0f, Hud.boardArea.rect.yMax, 0f))) + new Vector2(0f, -40f)
                : new Vector2(0f, 420f);
            Image background = toast.GetComponent<Image>();
            background.sprite = GameUiTheme.Active != null ? GameUiTheme.Active.panel : Assets.roundedBoxSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(1f, 0.97f, 0.88f, 0.97f);
            background.raycastTarget = false;
            Outline outline = toast.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.36f, 0.19f, 0.10f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            CanvasGroup group = toast.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            label = RuntimeUiFactory.CreateText(toast, "Message", "", 44, TextAnchor.MiddleCenter);
            Text sample = Hud.levelText;
            if (sample != null) label.font = sample.font;
            else if (Assets.defaultFont != null) label.font = Assets.defaultFont;
            label.color = new Color(0.36f, 0.19f, 0.10f, 1f);
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 20;
            label.resizeTextMaxSize = 44;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 10f);
            rect.offsetMax = new Vector2(-24f, -10f);
        }
    }
}
