using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance
    {
        get;
        private set;
    }

    public const string SelectedItemKey =
        "Shop_SelectedItem";

    // =======================================================
    // SHOP WINDOW
    // =======================================================

    [Header("Shop Window")]
    [Tooltip("Assign Canvas/ShopWindow.")]
    [SerializeField]
    private GameObject shopWindow;

    // =======================================================
    // GAME DATA
    // =======================================================

    [Header("Game Data")]
    [Tooltip(
        "Assign the scene LevelState. " +
        "If empty, it will be found automatically."
    )]
    [SerializeField]
    private LevelState levelState;

    // =======================================================
    // CURRENCY DISPLAY
    // =======================================================

    [Header("Currency Display")]
    [Tooltip("Shop banana amount text.")]
    [SerializeField]
    private TMP_Text bananaAmountText;

    [Tooltip("Shop coin amount text.")]
    [SerializeField]
    private TMP_Text coinAmountText;

    // =======================================================
    // DEFAULT SHOP ITEM
    // =======================================================

    [Header("Default Selected Item")]
    [Tooltip(
        "Must match the Item ID of your free/default item."
    )]
    [SerializeField]
    private string defaultSelectedItemId =
        "ClassicMonkey";

    // =======================================================
    // DEBUG
    // =======================================================

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = false;

    // =======================================================
    // RUNTIME
    // =======================================================

    private bool isSubscribed;

    // =======================================================
    // UNITY
    // =======================================================

    private void Awake()
    {
        Instance = this;

        FindLevelStateIfMissing();

        /*
         * Give the player a default selected shop item
         * the first time the game runs.
         */
        if (!PlayerPrefs.HasKey(
                SelectedItemKey
            ))
        {
            PlayerPrefs.SetString(
                SelectedItemKey,
                defaultSelectedItemId
            );

            PlayerPrefs.Save();
        }

        if (shopWindow != null)
        {
            shopWindow.SetActive(false);
        }
    }

    private void OnEnable()
    {
        FindLevelStateIfMissing();

        SubscribeToCurrencyEvents();
    }

    private void Start()
    {
        RefreshShop();
    }

    private void OnDisable()
    {
        UnsubscribeFromCurrencyEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromCurrencyEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =======================================================
    // FIND LEVEL STATE
    // =======================================================

    private void FindLevelStateIfMissing()
    {
        if (levelState != null)
        {
            return;
        }

        levelState =
            FindFirstObjectByType<LevelState>();

        if (levelState == null)
        {
            Debug.LogError(
                "[ShopManager] LevelState was not found. " +
                "Assign the LevelState object in the Inspector.",
                this
            );
        }
    }

    // =======================================================
    // EVENTS
    // =======================================================

    private void SubscribeToCurrencyEvents()
    {
        if (levelState == null ||
            isSubscribed)
        {
            return;
        }

        levelState.OnBananaChanged +=
            OnBananaChanged;

        levelState.OnCoinChanged +=
            OnCoinChanged;

        isSubscribed = true;
    }

    private void UnsubscribeFromCurrencyEvents()
    {
        if (levelState == null ||
            !isSubscribed)
        {
            return;
        }

        levelState.OnBananaChanged -=
            OnBananaChanged;

        levelState.OnCoinChanged -=
            OnCoinChanged;

        isSubscribed = false;
    }

    private void OnBananaChanged(
        int newAmount
    )
    {
        UpdateBananaText(
            newAmount
        );
    }

    private void OnCoinChanged(
        int newAmount
    )
    {
        UpdateCoinText(
            newAmount
        );
    }

    // =======================================================
    // OPEN SHOP
    // =======================================================

    public void OpenShop()
    {
        FindLevelStateIfMissing();

        if (shopWindow != null)
        {
            shopWindow.SetActive(true);

            shopWindow.transform
                .SetAsLastSibling();
        }

        Time.timeScale = 0f;

        RefreshShop();

        DebugLog(
            "Shop opened."
        );
    }

    // =======================================================
    // CLOSE SHOP
    // =======================================================

    public void CloseShop()
    {
        if (shopWindow != null)
        {
            shopWindow.SetActive(false);
        }

        /*
         * Leave the game paused because your ShopWindow
         * is being opened from the MenuWindow.
         */
        Time.timeScale = 0f;

        DebugLog(
            "Shop closed."
        );
    }

    // =======================================================
    // GET CURRENCY
    // =======================================================

    public int GetBananas()
    {
        if (levelState == null)
        {
            FindLevelStateIfMissing();
        }

        if (levelState == null)
        {
            return 0;
        }

        return levelState.TotalBananas;
    }

    public int GetCoins()
    {
        if (levelState == null)
        {
            FindLevelStateIfMissing();
        }

        if (levelState == null)
        {
            return 0;
        }

        return levelState.TotalCoins;
    }

    // =======================================================
    // SPEND CURRENCY
    // =======================================================

    public bool TrySpendCurrency(
        ShopItemUI.CurrencyType currencyType,
        int amount
    )
    {
        if (amount < 0)
        {
            return false;
        }

        FindLevelStateIfMissing();

        if (levelState == null)
        {
            Debug.LogError(
                "[ShopManager] Cannot purchase item because " +
                "LevelState is missing.",
                this
            );

            return false;
        }

        bool purchaseSuccessful = false;

        switch (currencyType)
        {
            case ShopItemUI.CurrencyType.Banana:
                {
                    purchaseSuccessful =
                        levelState.SpendBananas(
                            amount
                        );

                    break;
                }

            case ShopItemUI.CurrencyType.Coin:
                {
                    purchaseSuccessful =
                        levelState.SpendCoins(
                            amount
                        );

                    break;
                }
        }

        if (purchaseSuccessful)
        {
            /*
             * LevelState already saves the new amount
             * and fires its currency event.
             */
            RefreshCurrencyDisplay();

            DebugLog(
                $"Spent {amount} {currencyType}."
            );
        }
        else
        {
            DebugLog(
                $"Not enough {currencyType}. " +
                $"Price={amount}."
            );
        }

        return purchaseSuccessful;
    }

    // =======================================================
    // REFRESH SHOP
    // =======================================================

    public void RefreshShop()
    {
        FindLevelStateIfMissing();

        RefreshCurrencyDisplay();

        if (shopWindow == null)
        {
            return;
        }

        /*
         * Include inactive ShopItem objects as well.
         */
        ShopItemUI[] items =
            shopWindow.GetComponentsInChildren
                <ShopItemUI>(true);

        for (int i = 0;
             i < items.Length;
             i++)
        {
            if (items[i] != null)
            {
                items[i].RefreshUI();
            }
        }
    }

    // =======================================================
    // CURRENCY DISPLAY
    // =======================================================

    private void RefreshCurrencyDisplay()
    {
        if (levelState == null)
        {
            return;
        }

        UpdateBananaText(
            levelState.TotalBananas
        );

        UpdateCoinText(
            levelState.TotalCoins
        );

        DebugLog(
            $"Currency refreshed. " +
            $"Bananas={levelState.TotalBananas}, " +
            $"Coins={levelState.TotalCoins}."
        );
    }

    private void UpdateBananaText(
        int value
    )
    {
        if (bananaAmountText != null)
        {
            bananaAmountText.text =
                value.ToString();
        }
    }

    private void UpdateCoinText(
        int value
    )
    {
        if (coinAmountText != null)
        {
            coinAmountText.text =
                value.ToString();
        }
    }

    // =======================================================
    // DEBUG
    // =======================================================

    private void DebugLog(
        string message
    )
    {
        if (!showDebugLogs)
        {
            return;
        }

        Debug.Log(
            "[ShopManager] " +
            message,
            this
        );
    }
}