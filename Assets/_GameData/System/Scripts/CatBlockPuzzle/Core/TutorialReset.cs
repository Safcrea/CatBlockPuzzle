using UnityEngine;

namespace CatBlockPuzzle
{
    /// <summary>Forgets which tutorials and intro cards were seen, so they all show again as the player meets them.</summary>
    public static class TutorialReset
    {
        private static readonly string[] Mechanics =
        {
            MechanicIntroPresenter.Padlocks, MechanicIntroPresenter.KeyCats, MechanicIntroPresenter.Shelf, MechanicIntroPresenter.ColourZones,
            MechanicIntroPresenter.SpareCats, MechanicIntroPresenter.Hearts, MechanicIntroPresenter.FixedCat, MechanicIntroPresenter.Fog,
            MechanicIntroPresenter.Moves, MechanicIntroPresenter.Blocked
        };

        /// <summary>Every key that marks a tutorial as seen.</summary>
        internal static string[] SeenKeys()
        {
            var keys = new System.Collections.Generic.List<string>
            {
                PuzzleSaveKeys.LevelOneTutorialSeen, PuzzleSaveKeys.MovePieceTipSeen,
                PowerUpTutorialManager.HintSeenKey, PowerUpTutorialManager.FreezeSeenKey, PowerUpTutorialManager.RotateSeenKey,
                PowerUpTutorialManager.HammerSeenKey, PowerUpTutorialManager.TimerSeenKey
            };
            foreach (string mechanic in Mechanics) keys.Add(MechanicIntroPresenter.SeenKey(mechanic));
            return keys.ToArray();
        }

        /// <summary>Level 1's walkthrough shows on the next visit to level 1; the rest show when their level comes up.</summary>
        public static void ResetAll()
        {
            foreach (string key in SeenKeys()) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
