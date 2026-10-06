using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelCompletePanelController : MonoBehaviour
{
    // -------------------------------------------------------
    // REQUIRED REFERENCES
    // -------------------------------------------------------

    [Header("Required References")]
    [Tooltip("Assign the shared root LevelState.")]
    [SerializeField]
    private LevelState levelState;

    [Tooltip("Assign the root MapLevelManager.")]
    [SerializeField]
    private MapLevelManager mapLevelManager;

    [Tooltip("Assign Canvas/LevelCompletePanel.")]
    [SerializeField]
    private RectTransform levelCompletePanel;

    // -------------------------------------------------------
    // COMPLETION TEXT
    // -------------------------------------------------------

    [Header("Completion Text")]
    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text scoreRewardText;

    [SerializeField]
    private TMP_Text bananaRewardText;

    [SerializeField]
    private TMP_Text coinRewardText;

    [SerializeField]
    private TMP_Text triesText;

    [SerializeField]
    private TMP_Text timeText;

    // -------------------------------------------------------
    // STARS
    // -------------------------------------------------------

    [Header("Stars")]
    [Tooltip("Assign Star 1, Star 2 and Star 3 in order.")]
    [SerializeField]
    private GameObject[] starObjects;

    // -------------------------------------------------------
    // BUTTONS
    // -------------------------------------------------------

    [Header("Buttons")]
    [Tooltip("Assign the Restart button.")]
    [SerializeField]
    private Button restartButton;

    [Tooltip("Assign the Next button.")]
    [SerializeField]
    private Button nextButton;

    // -------------------------------------------------------
    // CUMULATIVE COIN TOTALS
    // -------------------------------------------------------

    [Header("Cumulative Coin Reward Totals")]
    [Tooltip(
        "The total coin reward for reaching 1 star. " +
        "This is not an additional reward."
    )]
    [SerializeField, Min(0)]
    private int oneStarTotalCoinReward = 2;

    [Tooltip(
        "The total coin reward for reaching 2 stars. " +
        "Improving from 1 to 2 stars gives the difference."
    )]
    [SerializeField, Min(0)]
    private int twoStarTotalCoinReward = 3;

    [Tooltip(
        "The total coin reward for reaching 3 stars. " +
        "Improving from a lower star result gives the difference."
    )]
    [SerializeField, Min(0)]
    private int threeStarTotalCoinReward = 5;

    // -------------------------------------------------------
    // BEHAVIOUR
    // -------------------------------------------------------

    [Header("Behaviour")]
    [SerializeField]
    private bool pauseGameWhenOpened = true;

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private bool panelShown;
    private LevelTimer levelTimer;

    // -------------------------------------------------------
    // UNITY
    // -------------------------------------------------------

    private void Awake()
    {
        ResolveReferences();

        panelShown = false;

        if (levelCompletePanel != null)
        {
            levelCompletePanel.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToLevelState();
    }

    private void OnDisable()
    {
        UnsubscribeFromLevelState();
    }

    private void Update()
    {
        /*
         * Safety fallback:
         * If the monkey was found before the event subscription,
         * the panel will still open.
         */
        if (!panelShown &&
            levelState != null &&
            levelState.IsMonkeyFound)
        {
            ShowPanel();
        }
    }

    // -------------------------------------------------------
    // REFERENCES
    // -------------------------------------------------------

    private void ResolveReferences()
    {
        if (levelState == null)
        {
            levelState =
                FindFirstObjectByType<LevelState>();
        }

        if (mapLevelManager == null)
        {
            mapLevelManager =
                FindFirstObjectByType<MapLevelManager>();
        }

        if (levelTimer == null)
        {
            levelTimer =
                FindFirstObjectByType<LevelTimer>();
        }
    }

    private void SubscribeToLevelState()
    {
        if (levelState == null)
            return;

        levelState.OnMonkeyFound -= HandleMonkeyFound;
        levelState.OnMonkeyFound += HandleMonkeyFound;
    }

    private void UnsubscribeFromLevelState()
    {
        if (levelState == null)
            return;

        levelState.OnMonkeyFound -= HandleMonkeyFound;
    }

    // -------------------------------------------------------
    // COMPLETION
    // -------------------------------------------------------

    private void HandleMonkeyFound()
    {
        ShowPanel();
    }

    public void ShowPanel()
    {
        if (panelShown)
            return;

        ResolveReferences();

        if (!ValidateRequiredReferences())
            return;

        panelShown = true;

        int currentLevel =
            mapLevelManager.CurrentLevel;

        int earnedStars =
            Mathf.Clamp(
                levelState.EarnedStars,
                1,
                3
            );

        /*
         * LevelState saved this before the new best result
         * was written to PlayerPrefs.
         */
        int previousBestStars =
            Mathf.Clamp(
                levelState.PreviousBestStarsThisRun,
                0,
                3
            );

        /*
         * Calculate the coin difference before saving
         * the new best stars.
         *
         * Examples:
         *
         * Previous best 0, earn 2 stars:
         * 3 - 0 = 3 coins
         *
         * Previous best 1, earn 2 stars:
         * 3 - 2 = 1 coin
         *
         * Previous best 2, earn 3 stars:
         * 5 - 3 = 2 coins
         *
         * Previous best 3, earn anything:
         * 0 coins
         */
        int coinReward =
            mapLevelManager.CalculateCumulativeReward(
                previousBestStars,
                earnedStars,
                oneStarTotalCoinReward,
                twoStarTotalCoinReward,
                threeStarTotalCoinReward
            );

        /*
         * Save:
         * - best stars;
         * - completed state;
         * - next-level unlock.
         */
        bool isFirstCompletion =
            mapLevelManager.CompleteCurrentLevel(
                earnedStars
            );

        int savedBestStars =
            mapLevelManager.GetSavedStars(
                currentLevel
            );

        if (coinReward > 0)
        {
            levelState.AddCoins(
                coinReward
            );
        }

        UpdatePanelUI(
            earnedStars,
            coinReward
        );

        OpenPanel();
        UpdateButtonStates();

        if (pauseGameWhenOpened)
        {
            Time.timeScale = 0f;
        }

        DebugLog(
            $"Level {currentLevel} completed. " +
            $"Tries={levelState.TryCount}, " +
            $"EarnedStars={earnedStars}, " +
            $"PreviousBestStars={previousBestStars}, " +
            $"SavedBestStars={savedBestStars}, " +
            $"FirstCompletion={isFirstCompletion}, " +
            $"ScoreReward={levelState.LastScoreReward}, " +
            $"BananaReward={levelState.LastBananaReward}, " +
            $"CoinReward={coinReward}, " +
            $"TotalScore={levelState.TotalScore}, " +
            $"TotalBananas={levelState.TotalBananas}, " +
            $"TotalCoins={levelState.TotalCoins}."
        );
    }

    private bool ValidateRequiredReferences()
    {
        if (levelState == null)
        {
            Debug.LogError(
                "[LevelCompletePanelController] " +
                "LevelState is missing.",
                this
            );

            return false;
        }

        if (mapLevelManager == null)
        {
            Debug.LogError(
                "[LevelCompletePanelController] " +
                "MapLevelManager is missing.",
                this
            );

            return false;
        }

        if (levelCompletePanel == null)
        {
            Debug.LogError(
                "[LevelCompletePanelController] " +
                "LevelCompletePanel is missing.",
                this
            );

            return false;
        }

        return true;
    }

    // -------------------------------------------------------
    // PANEL UI
    // -------------------------------------------------------

    private void UpdatePanelUI(
        int earnedStars,
        int coinReward
    )
    {
        if (titleText != null)
        {
            titleText.text =
                $"Level {mapLevelManager.CurrentLevel} Complete!";
        }

        if (scoreRewardText != null)
        {
            scoreRewardText.text =
                $"+{levelState.LastScoreReward}";
        }

        if (bananaRewardText != null)
        {
            bananaRewardText.text =
                $"+{levelState.LastBananaReward}";
        }

        if (coinRewardText != null)
        {
            coinRewardText.text =
                $"+{coinReward}";
        }

        if (triesText != null)
        {
            triesText.text =
                $"Tries: {levelState.TryCount}";
        }

        if (timeText != null && levelTimer != null)
        {
            int seconds = levelTimer.ElapsedWholeSeconds;

            timeText.text =
                $"Time: {seconds / 60}:{seconds % 60:00}";
        }

        UpdateStars(
            earnedStars
        );
    }

    private void UpdateStars(
        int earnedStars
    )
    {
        if (starObjects == null)
            return;

        for (int i = 0;
             i < starObjects.Length;
             i++)
        {
            if (starObjects[i] == null)
                continue;

            starObjects[i].SetActive(
                i < earnedStars
            );
        }
    }

    private void OpenPanel()
    {
        levelCompletePanel.gameObject.SetActive(true);
        levelCompletePanel.SetAsLastSibling();

        if (levelCompletePanel.localScale ==
            Vector3.zero)
        {
            levelCompletePanel.localScale =
                Vector3.one;
        }

        CanvasGroup canvasGroup =
            levelCompletePanel.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void UpdateButtonStates()
    {
        if (restartButton != null)
        {
            restartButton.interactable = true;
        }

        if (nextButton != null)
        {
            nextButton.interactable =
                mapLevelManager.HasNextLevel();
        }
    }

    // -------------------------------------------------------
    // BUTTON ACTIONS
    // -------------------------------------------------------

    public void RestartCurrentLevel()
    {
        ResolveReferences();

        if (mapLevelManager == null)
        {
            Debug.LogError(
                "[LevelCompletePanelController] " +
                "Cannot restart because MapLevelManager is missing.",
                this
            );

            return;
        }

        Time.timeScale = 1f;

        mapLevelManager.RestartCurrentLevel();
    }

    public void LoadNextLevel()
    {
        ResolveReferences();

        if (mapLevelManager == null)
        {
            Debug.LogError(
                "[LevelCompletePanelController] " +
                "Cannot load the next level because " +
                "MapLevelManager is missing.",
                this
            );

            return;
        }

        Time.timeScale = 1f;

        mapLevelManager.LoadNextLevel();
    }

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    [ContextMenu("DEBUG - Print Reward Calculation")]
    private void DebugPrintRewardCalculation()
    {
        ResolveReferences();

        if (levelState == null ||
            mapLevelManager == null)
        {
            Debug.LogWarning(
                "[LevelCompletePanelController] " +
                "Required references are missing.",
                this
            );

            return;
        }

        int previousBest =
            levelState.PreviousBestStarsThisRun;

        int earnedStars =
            levelState.EarnedStars;

        int reward =
            mapLevelManager.CalculateCumulativeReward(
                previousBest,
                earnedStars,
                oneStarTotalCoinReward,
                twoStarTotalCoinReward,
                threeStarTotalCoinReward
            );

        Debug.Log(
            "[LevelCompletePanelController] " +
            $"PreviousBestStars={previousBest}, " +
            $"EarnedStars={earnedStars}, " +
            $"CoinReward={reward}.",
            this
        );
    }

    private void DebugLog(string message)
    {
        if (!showDebugLogs)
            return;

        Debug.Log(
            "[LevelCompletePanelController] " +
            message,
            this
        );
    }
}