using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ShopItemUI : MonoBehaviour
{
    public enum CurrencyType
    {
        Banana,
        Coin
    }

    // =======================================================
    // ITEM SETTINGS
    // =======================================================

    [Header("Item Settings")]

    [Tooltip("Unique ID. Example: RedMonkey")]
    [SerializeField]
    private string itemId = "RedMonkey";

    [Tooltip("Name shown in the shop.")]
    [SerializeField]
    private string itemName = "Red Monkey";

    [Tooltip("Image shown for this item.")]
    [SerializeField]
    private Sprite itemSprite;

    [Tooltip("Choose Banana or Coin.")]
    [SerializeField]
    private CurrencyType currencyType =
        CurrencyType.Coin;

    [Min(0)]
    [SerializeField]
    private int itemPrice = 100;

    [Tooltip(
        "Turn ON if this item should already be owned."
    )]
    [SerializeField]
    private bool ownedByDefault = false;

    // =======================================================
    // CURRENCY LOGOS
    // =======================================================

    [Header("Currency Logos")]

    [SerializeField]
    private Sprite bananaLogo;

    [SerializeField]
    private Sprite coinLogo;

    // =======================================================
    // UI REFERENCES
    // =======================================================

    [Header("UI References")]

    [SerializeField]
    private Image itemImage;

    [SerializeField]
    private TMP_Text itemNameText;

    [SerializeField]
    private TMP_Text priceText;

    [SerializeField]
    private Image priceLogo;

    [SerializeField]
    private Button buyButton;

    [Tooltip(
        "Assign ShopItem/Selected."
    )]
    [SerializeField]
    private GameObject selectedObject;

    [Tooltip(
        "Assign ShopItem/Selected/SelectedText."
    )]
    [SerializeField]
    private TMP_Text selectedText;

    // =======================================================
    // RUNTIME
    // =======================================================

    private TMP_Text buyButtonText;

    // =======================================================
    // UNITY
    // =======================================================

    private void Awake()
    {
        if (buyButton != null)
        {
            buyButtonText =
                buyButton.GetComponentInChildren<TMP_Text>(
                    true
                );

            buyButton.onClick.AddListener(
                OnBuyOrSelectClicked
            );
        }
    }

    private void Start()
    {
        SetupItem();

        EnsureDefaultOwnership();

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(
                OnBuyOrSelectClicked
            );
        }
    }

    // =======================================================
    // SETUP ITEM
    // =======================================================

    private void SetupItem()
    {
        if (itemImage != null)
        {
            itemImage.sprite =
                itemSprite;
        }

        if (itemNameText != null)
        {
            itemNameText.text =
                itemName;
        }

        if (priceText != null)
        {
            priceText.text =
                itemPrice.ToString();
        }

        if (priceLogo != null)
        {
            priceLogo.sprite =
                currencyType == CurrencyType.Banana
                    ? bananaLogo
                    : coinLogo;
        }

        if (selectedText != null)
        {
            selectedText.text =
                "SELECTED";
        }
    }

    // =======================================================
    // DEFAULT OWNERSHIP
    // =======================================================

    private void EnsureDefaultOwnership()
    {
        if (!ownedByDefault)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        string key =
            GetOwnedKey();

        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(
                key,
                1
            );

            PlayerPrefs.Save();
        }
    }

    // =======================================================
    // BUTTON
    // =======================================================

    private void OnBuyOrSelectClicked()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogError(
                "[ShopItemUI] Item ID is empty.",
                this
            );

            return;
        }

        if (IsOwned())
        {
            SelectItem();

            return;
        }

        BuyItem();
    }

    // =======================================================
    // BUY ITEM
    // =======================================================

    private void BuyItem()
    {
        if (ShopManager.Instance == null)
        {
            Debug.LogError(
                "[ShopItemUI] ShopManager was not found.",
                this
            );

            return;
        }

        bool purchased =
            ShopManager.Instance.TrySpendCurrency(
                currencyType,
                itemPrice
            );

        if (!purchased)
        {
            Debug.Log(
                $"[ShopItemUI] Not enough " +
                $"{currencyType} for {itemName}.",
                this
            );

            return;
        }

        PlayerPrefs.SetInt(
            GetOwnedKey(),
            1
        );

        PlayerPrefs.Save();

        /*
         * Newly purchased item becomes selected.
         */
        SelectItem();

        Debug.Log(
            $"[ShopItemUI] Purchased: {itemName}",
            this
        );
    }

    // =======================================================
    // SELECT ITEM
    // =======================================================

    private void SelectItem()
    {
        PlayerPrefs.SetString(
            ShopManager.SelectedItemKey,
            itemId
        );

        PlayerPrefs.Save();

        // Immediately update the currently active player skin.
        PlayerSkinController playerSkinController =
            FindFirstObjectByType<PlayerSkinController>();

        if (playerSkinController != null)
        {
            playerSkinController.ApplySelectedSkin();
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshShop();
        }
        else
        {
            RefreshUI();
        }

        Debug.Log(
            $"[ShopItemUI] Selected: {itemName}",
            this
        );
    }

    // =======================================================
    // REFRESH UI
    // =======================================================

    public void RefreshUI()
    {
        bool owned =
            IsOwned();

        bool selected =
            IsSelected();

        // ---------------------------------------------------
        // BUY / SELECT BUTTON
        // ---------------------------------------------------

        if (buyButton != null)
        {
            /*
             * If currently selected:
             * hide BuyButton.
             *
             * Otherwise:
             * show BuyButton.
             */
            buyButton.gameObject.SetActive(
                !selected
            );

            buyButton.interactable =
                !selected;
        }

        // ---------------------------------------------------
        // SELECTED BACKGROUND
        // ---------------------------------------------------

        /*
         * Selected background is visible ONLY
         * when this item is selected.
         */
        if (selectedObject != null)
        {
            selectedObject.SetActive(
                selected
            );
        }

        // ---------------------------------------------------
        // SELECTED TEXT
        // ---------------------------------------------------

        if (selectedText != null)
        {
            selectedText.gameObject.SetActive(
                selected
            );
        }

        // ---------------------------------------------------
        // BUY BUTTON TEXT
        // ---------------------------------------------------

        if (buyButtonText != null)
        {
            if (!owned)
            {
                buyButtonText.text =
                    "BUY";
            }
            else
            {
                buyButtonText.text =
                    "SELECT";
            }
        }
    }

    // =======================================================
    // OWNERSHIP
    // =======================================================

    public bool IsOwned()
    {
        /*
         * Default/free item is always owned.
         */
        if (ownedByDefault)
        {
            return true;
        }

        return PlayerPrefs.GetInt(
            GetOwnedKey(),
            0
        ) == 1;
    }

    // =======================================================
    // SELECTED STATE
    // =======================================================

    public bool IsSelected()
    {
        string selectedItem =
            PlayerPrefs.GetString(
                ShopManager.SelectedItemKey,
                ""
            );

        return selectedItem ==
               itemId;
    }

    // =======================================================
    // PLAYERPREF KEY
    // =======================================================

    private string GetOwnedKey()
    {
        return "ShopItem_Owned_" +
               itemId;
    }

    // =======================================================
    // PUBLIC INFO
    // =======================================================

    public string ItemId
    {
        get
        {
            return itemId;
        }
    }
}