using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    [DisallowMultipleComponent]
    public sealed class PowerUpTutorialManager : MonoBehaviour
    {
        public const string HintSeenKey = "CatBlockPuzzle.Tutorial.Hint";
        public const string FreezeSeenKey = "CatBlockPuzzle.Tutorial.Freeze";
        private GameObject overlay;
        public bool IsOpen => overlay != null && overlay.activeSelf;

        public IEnumerator ShowUnlockedTutorials(GameplayHudView hud, float freezeSeconds)
        {
            while (GameSystem.Instance != null && (GameSystem.Instance.IsPaused || GameSystem.Instance.IsSettingsOpen))
                yield return null;
            if (GameSystem.Instance != null && !GameSystem.Instance.IsGameplayOpen) yield break;
            if (hud.IsPowerUpUnlocked(PowerUpKind.Hint) && PlayerPrefs.GetInt(HintSeenKey, 0) == 0)
                yield return Show(hud, PowerUpKind.Hint, "Hint unlocked!",
                    "Stuck? Tap Hint to highlight a cat and show where it fits.\n\nEach tap uses one Hint.", HintSeenKey);
            if (hud.IsPowerUpUnlocked(PowerUpKind.Freeze) && PlayerPrefs.GetInt(FreezeSeenKey, 0) == 0)
                yield return Show(hud, PowerUpKind.Freeze, "Freeze unlocked!",
                    $"Tap Freeze to stop the timer for {freezeSeconds:0.#} seconds. You can keep moving cats!\n\nUses one Freeze. Available once per attempt.", FreezeSeenKey);
        }

        private IEnumerator Show(GameplayHudView hud, PowerUpKind kind, string heading, string message, string key)
        {
            if (hud.Canvas == null) yield break;
            var root = RuntimeUiFactory.CreateOverlay(hud.Canvas.transform, "Power Up Tutorial");
            overlay = root.gameObject;
            float width = Mathf.Min(760f, ((RectTransform)hud.Canvas.transform).rect.width - 48f);
            var panel = RuntimeUiFactory.CreatePanel(root, "Panel", new Vector2(width, 620f));
            var title = RuntimeUiFactory.CreateText(panel, "Title", heading, 44, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0f, 235f), new Vector2(width - 40f, 75f));
            var iconRect = RuntimeUiFactory.CreateRect(panel, "Power Up Icon");
            RuntimeUiFactory.SetRect(iconRect, new Vector2(0f, 125f), new Vector2(110f, 110f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = hud.GetPowerUpSprite(kind);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = icon.sprite != null;
            var body = RuntimeUiFactory.CreateText(panel, "Instructions", message, 30, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(body.rectTransform, new Vector2(0f, -35f), new Vector2(width - 64f, 210f));
            var okay = RuntimeUiFactory.CreateButton(panel, "Continue", "Got it!", new Vector2(0f, -225f), new Vector2(280f, 78f), RuntimeUiFactory.Coral);
            bool finished = false;
            okay.onClick.AddListener(() =>
            {
                okay.interactable = false;
                if (PowerUpInventory.GetCount(kind) == 0) PowerUpInventory.Add(kind);
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                hud.RefreshPowerUpCounts();
                MenuTransition.Hide(root.gameObject, () => finished = true);
            });
            root.gameObject.SetActive(false);
            MenuTransition.Show(root.gameObject);
            while (!finished && overlay != null) yield return null;
            Cancel();
        }

        public void Cancel()
        {
            if (overlay != null) { overlay.SetActive(false); Destroy(overlay); }
            overlay = null;
        }
        private void OnDisable() => Cancel();
    }
}
