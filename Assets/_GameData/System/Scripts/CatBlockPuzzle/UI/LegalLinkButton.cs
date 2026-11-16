using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatBlockPuzzle
{
    /// <summary>
    /// Store and legal links the game shows as underlined text: Restore Purchases (Settings) and Privacy Policy (Shop).
    /// Restore goes through <see cref="RestoreProvider"/>, which the in-app purchase plugin sets once it is installed.
    /// </summary>
    public static class StoreLinks
    {
        // Defaults are the studio's other games' documents (Zombie Rush). The real links are set on GameSystem (and in the
        // Setup window, Legal Links), which overwrites these at start-up.
        public static string PrivacyPolicyUrl = "https://drive.google.com/file/d/10PM9SazVO6EmkjYxDqkaCX2td3JIcUmC/view?usp=drive_link";
        public static string TermsUrl = "https://drive.google.com/file/d/1USmexlEsJGMP9AJ2fZV2uVpHQEtt7TuD/view?usp=drive_link";

        /// <summary>Set by the purchase plugin: restores the player's purchases, then calls back with whether anything came back.</summary>
        public static Action<Action<bool>> RestoreProvider;
    }

    /// <summary>A text link: tapping it opens a page or restores purchases. Lives on a Button.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class LegalLinkButton : MonoBehaviour
    {
        public enum Kind { RestorePurchases, PrivacyPolicy, TermsOfUse }

        [SerializeField] private Kind kind;
        private InfoPopup popup;

        private void Awake() => GetComponent<Button>().onClick.AddListener(Click);

        private void Click()
        {
            ButtonSfx.PlayClick();
            switch (kind)
            {
                case Kind.PrivacyPolicy: Application.OpenURL(StoreLinks.PrivacyPolicyUrl); break;
                case Kind.TermsOfUse: Application.OpenURL(StoreLinks.TermsUrl); break;
                case Kind.RestorePurchases: Restore(); break;
            }
        }

        private void Restore()
        {
            if (StoreLinks.RestoreProvider == null) { Say("Restoring purchases will work once the store is connected."); return; }
            StoreLinks.RestoreProvider(found => Say(found ? "Your purchases have been restored." : "There were no purchases to restore."));
        }

        private void Say(string message)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (popup == null && canvas != null) popup = InfoPopup.Create(canvas.rootCanvas.transform);
            if (popup != null) popup.Show("RESTORE PURCHASES", null, () => message);
            else Debug.Log("[Store] " + message);
        }
    }
}
