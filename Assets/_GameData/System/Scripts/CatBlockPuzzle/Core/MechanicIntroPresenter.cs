using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Introduces each level mechanic (padlocks, key cats, the drip-fed shelf, ...) with a one-time
    /// "New!" card the first time a level uses it. The level start waits for the card, so no
    /// time is spent while the player reads.
    /// </summary>
    internal sealed class MechanicIntroPresenter : HostedService
    {
        private const string SeenKeyPrefix = "CatBlockPuzzle.Mechanic.";

        public const string Padlocks = "Padlocks";
        public const string KeyCats = "KeyCats";
        public const string Shelf = "Shelf";
        public const string ColourZones = "ColourZones";

        private readonly PuzzleSession session;
        private GameObject overlay;

        public MechanicIntroPresenter(IGameplayContext context, PuzzleSession session) : base(context)
        {
            this.session = session;
        }

        public bool IsOpen => overlay != null && overlay.activeSelf;

        public static string SeenKey(string mechanic) => SeenKeyPrefix + mechanic + ".v1";

        /// <summary>Shows, one after another, the cards for this level's mechanics the player has not met yet.</summary>
        public IEnumerator ShowNewMechanics(LevelDefinition level)
        {
            if (level == null || Hud == null || Hud.Canvas == null) yield break;
            if (GameSystem.Instance != null && !GameSystem.Instance.IsGameplayOpen) yield break;

            bool hasCounterOrNeighbourLock = false;
            foreach (CellLockRule rule in level.LockedCells.Values) if (!rule.IsKey) hasCounterOrNeighbourLock = true;

            if (hasCounterOrNeighbourLock)
                yield return Show(Padlocks, "New: Padlocks!", CellLockArt.Padlock,
                    "Padlocked spots can't take a cat yet.\n\nA number means: place that many cats first.\nNo number: fill the spots next to it first.");
            if (level.HasKeyLocks)
                yield return Show(KeyCats, "New: Key cats!", CellLockArt.Key,
                    "A padlock with a cat face opens when THAT cat is placed.\n\nFind the cat with the key on its card and place it first.");
            if (level.HasZones)
                yield return Show(ColourZones, "New: Colour spots!", Assets.catPortraitSprites != null && Assets.catPortraitSprites.Length > 3 ? Assets.catPortraitSprites[3] : null,
                    "Tinted spots show a cat's face.\n\nOnly the cat with that face and colour can sit there, every other cat is turned away.");
            if (level.HasTrayWindow)
                yield return Show(Shelf, "New: Busy shelf!", Assets.catPortraitSprites != null && Assets.catPortraitSprites.Length > 0 ? Assets.catPortraitSprites[0] : null,
                    "Only " + level.TrayWindow + " cats fit on the shelf at once.\n\nPlace one and the next cat arrives, so think about the order.");
        }

        public void Cancel()
        {
            if (overlay != null)
            {
                overlay.SetActive(false);
                Destroy(overlay);
            }
            overlay = null;
        }

        private IEnumerator Show(string mechanic, string heading, Sprite iconSprite, string message)
        {
            string key = SeenKey(mechanic);
            if (PlayerPrefs.GetInt(key, 0) != 0) yield break;
            while (GameSystem.Instance != null && (GameSystem.Instance.IsPaused || GameSystem.Instance.IsSettingsOpen)) yield return null;

            RectTransform root = RuntimeUiFactory.CreateOverlay(Hud.Canvas.transform, "Mechanic Intro");
            float width = Mathf.Min(760f, ((RectTransform)Hud.Canvas.transform).rect.width - 48f);
            RectTransform panel = RuntimeUiFactory.CreatePanel(root, "Panel", new Vector2(width, 640f));
            Text title = RuntimeUiFactory.CreateText(panel, "Title", heading, 44, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0f, 240f), new Vector2(width - 40f, 75f));
            Image icon = RuntimeUiFactory.CreateImage(panel, "Icon", iconSprite, Color.white, new Vector2(0f, 128f), new Vector2(120f, 120f));
            icon.preserveAspect = true;
            icon.enabled = iconSprite != null;
            Text body = RuntimeUiFactory.CreateText(panel, "Instructions", message, 30, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(body.rectTransform, new Vector2(0f, -40f), new Vector2(width - 64f, 230f));
            Button okay = RuntimeUiFactory.CreateButton(panel, "Continue", "Got it!", new Vector2(0f, -235f), new Vector2(280f, 78f), RuntimeUiFactory.Coral);

            overlay = root.gameObject;
            bool finished = false;
            okay.onClick.AddListener(() =>
            {
                ButtonSfx.PlayClick();
                okay.interactable = false;
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                MenuTransition.Hide(root.gameObject, () => finished = true);
            });
            root.gameObject.SetActive(false);
            MenuTransition.Show(root.gameObject);
            while (!finished && overlay != null) yield return null;
            Cancel();
        }
    }
}
