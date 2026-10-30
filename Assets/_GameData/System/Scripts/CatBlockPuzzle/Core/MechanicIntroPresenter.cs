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
                    "Locked spots can't take a cat yet.\nA number: place that many cats first.\nNo number: fill the spots beside it.");
            if (level.HasKeyLocks)
                yield return Show(KeyCats, "New: Key cats!", CellLockArt.Key,
                    "A padlock with a cat face opens\nwhen that cat is placed.\nPlace the cat with the key first!");
            if (level.HasZones)
                yield return Show(ColourZones, "New: Colour spots!", Assets.catPortraitSprites != null && Assets.catPortraitSprites.Length > 3 ? Assets.catPortraitSprites[3] : null,
                    "A tinted spot shows a cat's face.\nOnly that cat can sit there.");
            if (level.HasSpares)
                yield return Show(SpareCats, "New: A spare cat!", Assets.catPortraitSprites != null && Assets.catPortraitSprites.Length > 5 ? Assets.catPortraitSprites[5] : null,
                    "One cat on the shelf doesn't belong.\nFill the board to win, even with\na cat left over!");
            if (level.HasHearts)
                yield return Show(Hearts, "New: Mistake hearts!", HeartArt.Heart,
                    "You have " + level.Hearts + " hearts.\nA wrong drop costs one heart.\nLose them all and the level ends.");
            if (level.HasFixedPieces)
                yield return Show(FixedCat, "New: A napping cat!", BoardMechanicArt.Pin,
                    "This cat is already napping in its spot.\nBuild around it!");
            if (level.HasFog)
                yield return Show(Fog, "New: Foggy spots!", BoardMechanicArt.Cloud,
                    "Foggy spots hide part of the board.\nPlace a cat next to them\nto clear the fog.");
            if (level.HasMoves)
                yield return Show(Moves, "New: Drop limit!", Assets.starSprite,
                    "Fewer drops, more stars!\nSolve it in " + level.Moves + " drops for 3 stars.");
            if (level.HasTrayWindow)
                yield return Show(Shelf, "New: Busy shelf!", Assets.catPortraitSprites != null && Assets.catPortraitSprites.Length > 0 ? Assets.catPortraitSprites[0] : null,
                    "Only " + level.TrayWindow + " cats fit on the shelf.\nPlace one and the next arrives,\nso plan the order!");
        }

        public void Cancel()
        {
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

            // The card is the MechanicIntroPopup prefab, styled like the power-up purchase popup.
            MechanicIntroPopup popup = MechanicIntroPopup.Create((RectTransform)Hud.Canvas.transform, Hud.levelText != null ? Hud.levelText.font : null);
            if (popup == null)
            {
                Debug.LogWarning("MechanicIntroPopup prefab missing; skipping the '" + mechanic + "' intro.", Host);
                yield break;
            }

            overlay = popup.gameObject;
            bool finished = false;
            GameAnalytics.MechanicIntroShown(mechanic);
            AvnEvents.TutorialShown(AvnEvents.FeatureTutorial(mechanic));
            popup.Show(heading, message, iconSprite, () =>
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                GameAnalytics.MechanicIntroDismissed(mechanic);
                AvnEvents.TutorialCompleted(AvnEvents.FeatureTutorial(mechanic));
                finished = true;
            });
            while (!finished && overlay != null) yield return null;
            Cancel();
        }
    }
}
