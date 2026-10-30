using AVNEvents;
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
        public const string SpareCats = "SpareCats";
        public const string Hearts = "Hearts";
        public const string FixedCat = "FixedCat";
        public const string Fog = "Fog";
        public const string Moves = "Moves";

        private readonly PuzzleSession session;
        private GameObject overlay;
        private bool cancelling;

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
                yield return Show(Padlocks, "Padlocks!", CellLockArt.Padlock,
                    "Place more cats to open locked spots.");
            if (level.HasKeyLocks)
                yield return Show(KeyCats, "Key cats!", CellLockArt.Key,
                    "Place the key cat to open its lock.");
            if (level.HasZones)
                yield return Show(ColourZones, "Colour spots!", Assets.catPortraitSprites != null && Assets.catPortraitSprites.Length > 3 ? Assets.catPortraitSprites[3] : null,
                    "Only the matching cat fits a tinted spot.");
            if (level.HasSpares)
                yield return Show(SpareCats, "Spare cat!", Assets.catPortraitSprites != null && Assets.catPortraitSprites.Length > 5 ? Assets.catPortraitSprites[5] : null,
                    "One cat is extra. Fill the board to win.");
            if (level.HasHearts)
                yield return Show(Hearts, "Hearts!", HeartArt.Heart,
                    "A wrong drop costs a heart. You have " + level.Hearts + ".");
            if (level.HasFixedPieces)
                yield return Show(FixedCat, "Napping cat!", BoardMechanicArt.Pin,
                    "This cat stays put. Build around it.");
            if (level.HasFog)
                yield return Show(Fog, "Foggy spots!", BoardMechanicArt.Cloud,
                    "Place a cat nearby to clear the fog.");
            if (level.HasMoves)
                yield return Show(Moves, "Drop limit!", Assets.starSprite,
                    "Win in " + level.Moves + " drops for 3 stars.");
            if (level.HasTrayWindow)
                yield return Show(Shelf, "Busy shelf!", Assets.catPortraitSprites != null && Assets.catPortraitSprites.Length > 0 ? Assets.catPortraitSprites[0] : null,
                    "Only " + level.TrayWindow + " cats show at a time.");
        }

        public void Cancel()
        {
            cancelling = true;
            AvnEvents.TutorialsInterrupted("feature_");
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

            // The same small cat-panel popup the daily puzzle uses: a title, one picture and one short line.
            InfoPopup popup = InfoPopup.Create(Hud.Canvas.transform);
            if (popup == null)
            {
                Debug.LogWarning("InfoPopup prefab missing; skipping the '" + mechanic + "' intro.", Host);
                yield break;
            }

            overlay = popup.gameObject;
            bool finished = false;
            cancelling = false;
            GameAnalytics.MechanicIntroShown(mechanic);
            AvnEvents.TutorialShown(AvnEvents.FeatureTutorial(mechanic));
            popup.Closed += () =>
            {
                if (cancelling) return; // the level changed under the card, so the player has not seen it yet
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                GameAnalytics.MechanicIntroDismissed(mechanic);
                AvnEvents.TutorialCompleted(AvnEvents.FeatureTutorial(mechanic));
                finished = true;
            };
            popup.Show(heading, iconSprite, () => message);
            while (!finished && overlay != null) yield return null;
            Cancel();
        }
    }
}
