using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CatBlockPuzzle.KawaiiUI;

namespace CatBlockPuzzle
{
    /// <summary>Gameplay coin purchase. Inventory is granted here; using it requires a separate HUD tap.</summary>
    public sealed class PowerUpPurchasePopup : MonoBehaviour
    {
        /// <summary>Resources path used when the HUD has no prefab assigned.</summary>
        public const string PrefabResourcePath = "CatBlockPuzzle/PowerUpPurchasePopup";
        internal static readonly Vector2 PanelSize = new Vector2(720f, 660f);

        [Header("Layout")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text heading;
        [SerializeField] private Text message;
        [SerializeField] private Text wallet;
        [SerializeField] private Image icon;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;
        private GameplayHudView hud;
        private Text buyLabel;
        private Text closeLabel;
        private GameObject previousSelection;
        private bool sessionOpen;
        private bool closing;
        private bool purchased;
        private bool resumeOnClose = true;
        private PowerUpShopConfig.Offer displayedOffer;
        public PowerUpKind Kind { get; private set; }
        public bool IsOpen => sessionOpen && gameObject.activeInHierarchy;
        private bool ReducedMotion => PlayerPrefs.GetInt("CatBlockPuzzle.Settings.ReducedMotion", 0) != 0;

        /// <summary>Spawns the popup from <paramref name="prefab"/>, the Resources copy, or the runtime fallback layout.</summary>
        public static PowerUpPurchasePopup Create(GameplayHudView hud, GameObject prefab = null)
        {
            if (hud == null || hud.Canvas == null) return null;
            if (prefab == null) prefab = Resources.Load<GameObject>(PrefabResourcePath);
            PowerUpPurchasePopup popup = null;
            if (prefab != null)
            {
                var instance = Instantiate(prefab, hud.Canvas.transform, false);
                instance.name = "Power Up Purchase Popup";
                popup = instance.GetComponent<PowerUpPurchasePopup>();
                if (popup == null)
                {
                    Debug.LogError($"{prefab.name} needs a PowerUpPurchasePopup component; using the runtime layout instead.", prefab);
                    Destroy(instance);
                }
            }
            if (popup == null) popup = Build(hud.Canvas.transform);
            if (!popup.Bind(hud))
            {
                Destroy(popup.gameObject);
                return null;
            }
            return popup;
        }

        /// <summary>Builds the authored layout in code. Shared by the runtime fallback and the prefab generator.</summary>
        public static PowerUpPurchasePopup Build(Transform parent)
        {
            RectTransform root = RuntimeUiFactory.CreateOverlay(parent, "Power Up Purchase Popup");
            var popup = root.gameObject.AddComponent<PowerUpPurchasePopup>();
            popup.panel = RuntimeUiFactory.CreatePanel(root, "Panel", PanelSize);
            popup.heading = RuntimeUiFactory.CreateText(popup.panel, "Title", "Out of Hints!", 44, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(popup.heading.rectTransform, new Vector2(0f, 255f), new Vector2(640f, 70f));
            var iconRect = RuntimeUiFactory.CreateRect(popup.panel, "Power Up Icon");
            RuntimeUiFactory.SetRect(iconRect, new Vector2(0f, 140f), new Vector2(130f, 130f));
            popup.icon = iconRect.gameObject.AddComponent<Image>();
            popup.icon.preserveAspect = true;
            popup.icon.raycastTarget = false;
            popup.message = RuntimeUiFactory.CreateText(popup.panel, "Message", "", 30, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(popup.message.rectTransform, new Vector2(0f, 10f), new Vector2(620f, 125f));
            popup.wallet = RuntimeUiFactory.CreateText(popup.panel, "Wallet", "", 28, TextAnchor.MiddleCenter);
            RuntimeUiFactory.SetRect(popup.wallet.rectTransform, new Vector2(0f, -100f), new Vector2(620f, 60f));
            popup.buyButton = RuntimeUiFactory.CreateButton(popup.panel, "Buy", "Buy", new Vector2(0f, -185f), new Vector2(520f, 82f), RuntimeUiFactory.Coral);
            popup.closeButton = RuntimeUiFactory.CreateButton(popup.panel, "Close", "Not now", new Vector2(0f, -275f), new Vector2(280f, 68f), new Color(.9f, .85f, .76f));
            popup.gameObject.SetActive(false);
            return popup;
        }

        /// <summary>Resolves authored references, fits the panel to the canvas and wires the buttons.</summary>
        private bool Bind(GameplayHudView hud)
        {
            this.hud = hud;
            if (panel == null) panel = transform.Find("Panel") as RectTransform;
            if (panel == null)
            {
                Debug.LogError("Power up purchase popup needs a 'Panel' child.", this);
                return false;
            }
            if (heading == null) heading = Find<Text>("Title");
            if (message == null) message = Find<Text>("Message");
            if (wallet == null) wallet = Find<Text>("Wallet");
            if (icon == null) icon = Find<Image>("Power Up Icon");
            if (buyButton == null) buyButton = Find<Button>("Buy");
            if (closeButton == null) closeButton = Find<Button>("Close");
            if (heading == null || message == null || wallet == null || icon == null || buyButton == null || closeButton == null)
            {
                Debug.LogError("Power up purchase popup is missing Title, Message, Wallet, Power Up Icon, Buy or Close.", this);
                return false;
            }
            buyLabel = buyButton.GetComponentInChildren<Text>(true);
            closeLabel = closeButton.GetComponentInChildren<Text>(true);
            if (buyLabel == null || closeLabel == null)
            {
                Debug.LogError("Power up purchase popup buttons need a Text label.", this);
                return false;
            }
            Vector2 available = ((RectTransform)hud.Canvas.transform).rect.size - new Vector2(64f, 64f);
            Vector2 size = panel.rect.size.x > 0f && panel.rect.size.y > 0f ? panel.rect.size : PanelSize;
            panel.localScale = Vector3.one * Mathf.Clamp(Mathf.Min(available.x / size.x, available.y / size.y), .1f, 1f);
            ApplyFallbackSprite(panel.GetComponent<Image>());
            ApplyFallbackSprite(buyButton.GetComponent<Image>());
            ApplyFallbackSprite(closeButton.GetComponent<Image>());
            Font hudFont = hud.levelText != null ? hud.levelText.font : null;
            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = false;
                // Authored fonts win; unskinned labels follow the HUD.
                if (hudFont != null && (text.font == null || text.font == builtinFont)) text.font = hudFont;
            }
            buyButton.onClick.RemoveListener(Buy);
            buyButton.onClick.AddListener(Buy);
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
            gameObject.SetActive(false);
            return true;
        }

        /// <summary>Authored art wins; unskinned images fall back to the generated rounded rect.</summary>
        private static void ApplyFallbackSprite(Image image)
        {
            if (image == null || image.sprite != null) return;
            image.sprite = KawaiiSprites.RoundedRect;
            image.type = Image.Type.Sliced;
        }

        private T Find<T>(string childName) where T : Component
        {
            Transform child = panel.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
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
            transform.SetAsLastSibling(); // Authored prefabs are not spawned on top like the runtime overlay.
            if (ReducedMotion) gameObject.SetActive(true);
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
