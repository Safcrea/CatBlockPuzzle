using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CatBlockPuzzle.KawaiiUI;

namespace CatBlockPuzzle
{
    /// <summary>Gameplay coin purchase. Inventory is granted here; using it requires a separate HUD tap.</summary>
    public sealed class PowerUpPurchasePopup : MonoBehaviour
    {
        private GameplayHudView hud;
        private Text heading;
        private Text message;
        private Text wallet;
        private Text buyLabel;
        private Text closeLabel;
        private Image icon;
        private Button buyButton;
        private Button closeButton;
        private GameObject previousSelection;
        private bool sessionOpen;
        private bool closing;
        private bool purchased;
        private bool resumeOnClose = true;
        private PowerUpShopConfig.Offer displayedOffer;
        public PowerUpKind Kind { get; private set; }
        public bool IsOpen => sessionOpen && gameObject.activeInHierarchy;
        private bool ReducedMotion => PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;

        public static PowerUpPurchasePopup Create(GameplayHudView hud)
        {
            RectTransform root = RuntimeUiFactory.CreateOverlay(hud.Canvas.transform, "Power Up Purchase Popup");
            root.gameObject.SetActive(false);
            var popup = root.gameObject.AddComponent<PowerUpPurchasePopup>();
            popup.hud = hud;
            var panel = RuntimeUiFactory.CreatePanel(root, "Panel", new Vector2(720f, 660f));
            Vector2 available = ((RectTransform)hud.Canvas.transform).rect.size - new Vector2(64f, 64f);
            panel.localScale = Vector3.one * Mathf.Clamp(Mathf.Min(available.x / 720f, available.y / 660f), .1f, 1f);
            Sprite rounded = KawaiiSprites.RoundedRect;
            panel.GetComponent<Image>().sprite = rounded;
            panel.GetComponent<Image>().type = Image.Type.Sliced;
            popup.heading = RuntimeUiFactory.CreateText(panel, "Title", "Out of Hints!", 44, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(popup.heading.rectTransform, new Vector2(0f, 255f), new Vector2(640f, 70f));
            var iconRect = RuntimeUiFactory.CreateRect(panel, "Power Up Icon");
            RuntimeUiFactory.SetRect(iconRect, new Vector2(0f, 140f), new Vector2(130f, 130f));
            popup.icon = iconRect.gameObject.AddComponent<Image>();
            popup.icon.preserveAspect = true;
            popup.icon.raycastTarget = false;
            popup.message = RuntimeUiFactory.CreateText(panel, "Message", "", 30, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(popup.message.rectTransform, new Vector2(0f, 10f), new Vector2(620f, 125f));
            popup.wallet = RuntimeUiFactory.CreateText(panel, "Wallet", "", 28, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(popup.wallet.rectTransform, new Vector2(0f, -100f), new Vector2(620f, 60f));
            popup.buyButton = RuntimeUiFactory.CreateButton(panel, "Buy", "Buy", new Vector2(0f, -185f), new Vector2(520f, 82f), RuntimeUiFactory.Coral);
            popup.buyLabel = popup.buyButton.GetComponentInChildren<Text>();
            popup.closeButton = RuntimeUiFactory.CreateButton(panel, "Close", "Not now", new Vector2(0f, -275f), new Vector2(280f, 68f), new Color(.9f, .85f, .76f));
            popup.closeLabel = popup.closeButton.GetComponentInChildren<Text>();
            foreach (Button button in new[] { popup.buyButton, popup.closeButton })
            {
                var image = button.GetComponent<Image>();
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
            }
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = false;
                if (hud.levelText != null && hud.levelText.font != null) text.font = hud.levelText.font;
            }
            popup.buyButton.onClick.AddListener(popup.Buy);
            popup.closeButton.onClick.AddListener(popup.Close);
            return popup;
        }

        public bool Show(PowerUpKind kind)
        {
            var system = GameSystem.Instance;
            if (IsOpen || hud == null || system == null || !system.CanRequestPowerUp(kind) || system.GetPowerUpCount(kind) > 0) return false;
            Kind = kind;
            purchased = closing = false;
            resumeOnClose = true;
            sessionOpen = true;
            if (EventSystem.current != null) previousSelection = EventSystem.current.currentSelectedGameObject;
            heading.text = kind == PowerUpKind.Hint ? "Out of Hints!" : "Out of Freezes!";
            closeLabel.text = "Not now";
            icon.sprite = hud.GetPowerUpSprite(kind);
            icon.enabled = icon.sprite != null;
            RefreshOffer();
            system.SetPowerUpPurchaseOpen(true);
            if (ReducedMotion)
            {
                gameObject.SetActive(true);
                transform.SetAsLastSibling();
            }
            else MenuTransition.Show(gameObject, true, new MenuTransition.EntranceSettings { duration = .35f, slideDistance = 24f, overshoot = 1.2f });
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(buyButton.interactable ? buyButton.gameObject : closeButton.gameObject);
            return true;
        }

        private void RefreshOffer()
        {
            var system = GameSystem.Instance;
            var config = system != null ? system.PowerUpShop : null;
            PowerUpShopConfig.Offer offer = config != null ? config.GetOffer(Kind) : default;
            displayedOffer = offer;
            int coins = system != null ? system.CurrentCoins : 0;
            wallet.text = $"Your coins: {coins}";
            if (!offer.IsValid)
            {
                message.text = "This power-up is unavailable right now.";
                buyLabel.text = "Unavailable";
                buyButton.interactable = false;
                return;
            }
            string item = Kind == PowerUpKind.Hint ? "Hint" : "Freeze";
            string items = offer.quantity == 1 ? item : item + "s";
            message.text = coins >= offer.coinPrice
                ? $"Get {offer.quantity} {items} for {offer.coinPrice} coins.\nBuy now, then tap {item} to use one."
                : $"Get {offer.quantity} {items} for {offer.coinPrice} coins.\nYou need {offer.coinPrice - coins} more coins.";
            buyLabel.text = $"Buy x{offer.quantity}  •  {offer.coinPrice} coins";
            buyButton.interactable = coins >= offer.coinPrice;
        }

        public void Buy()
        {
            if (!IsOpen || closing || purchased) return;
            var system = GameSystem.Instance;
            var currentOffer = system != null && system.PowerUpShop != null ? system.PowerUpShop.GetOffer(Kind) : default;
            if (currentOffer.coinPrice != displayedOffer.coinPrice || currentOffer.quantity != displayedOffer.quantity)
            {
                RefreshOffer();
                return; // Let the player review an offer changed while the popup was open.
            }
            var status = system != null ? system.PurchasePowerUp(Kind) : PowerUpPurchaseStatus.Unavailable;
            if (status != PowerUpPurchaseStatus.Success)
            {
                RefreshOffer();
                if (status == PowerUpPurchaseStatus.InventoryFull)
                {
                    message.text = "Your power-up bag is full.";
                    buyButton.interactable = false;
                }
                return;
            }
            purchased = true;
            buyButton.interactable = false;
            buyLabel.text = "Purchased!";
            heading.text = Kind == PowerUpKind.Hint ? "Hints ready!" : "Freezes ready!";
            string item = Kind == PowerUpKind.Hint ? "Hint" : "Freeze";
            message.text = $"Added x{displayedOffer.quantity} to your bag!\nTap {item} after closing to use one.";
            wallet.text = $"Your coins: {system.CurrentCoins}";
            closeLabel.text = "Continue";
            SoundManager.Instance?.PlaySfx("Button");
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        public void Close()
        {
            if (!sessionOpen || closing) return;
            closing = true;
            MenuTransition.Hide(gameObject, null, !ReducedMotion);
        }

        public void HideImmediately(bool resumeGameplay = true)
        {
            resumeOnClose = resumeGameplay;
            MenuTransition.Hide(gameObject, null, false);
        }

        private void Update()
        {
            var system = GameSystem.Instance;
            if (sessionOpen && (hud == null || !hud.isActiveAndEnabled ||
                system == null || !system.HasGameplayController || !system.IsGameplayOpen)) HideImmediately(false);
        }

        private void OnDisable()
        {
            if (!sessionOpen) return;
            sessionOpen = false;
            GameSystem.Instance?.SetPowerUpPurchaseOpen(false, resumeOnClose);
            if (resumeOnClose && EventSystem.current != null && previousSelection != null && previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection = null;
        }
    }
}
