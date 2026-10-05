using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelState : MonoBehaviour
{
    private const string TotalScoreKey = "TotalScore";
    private const string TotalBananasKey = "TotalBananas";
    private const string TotalCoinsKey = "TotalCoins";

    // -------------------------------------------------------
    // CURRENT LEVEL DATA
    // -------------------------------------------------------

    public bool IsMonkeyFound { get; private set; }

    public int TryCount { get; private set; }

    /// <summary>
    /// Score earned during this completion only.
    /// </summary>
    public int LevelScore { get; private set; }

    public int EarnedStars { get; private set; }

    /// <summary>
    /// Best stars saved before the current completion.
    /// The completion panel uses this to calculate coin difference.
    /// </summary>
    public int PreviousBestStarsThisRun { get; private set; }

    /// <summary>
    /// Best-star value after considering this completion.
    /// </summary>
    public int BestStarsAfterThisRun { get; private set; }

    /// <summary>
    /// True when the current result is higher than the saved best result.
    /// </summary>
    public bool IsNewBestStarResultThisRun { get; private set; }

    /// <summary>
    /// Kept for compatibility with the previous completion-panel script.
    /// It now means the player achieved a new best star result.
    /// </summary>
    public bool WasStarRewardClaimedThisRun
    {
        get;
        private set;
    }

    // -------------------------------------------------------
    // SAVED GLOBAL DATA
    // -------------------------------------------------------

    public int TotalScore { get; private set; }

    public int TotalBananas { get; private set; }

    public int TotalCoins { get; private set; }

    // -------------------------------------------------------
    // CURRENT COMPLETION REWARD
    // -------------------------------------------------------

    public int LastScoreReward { get; private set; }

    public int LastBananaReward { get; private set; }

    // -------------------------------------------------------
    // EVENTS
    // -------------------------------------------------------

    public event Action<int> OnScoreChanged;
    public event Action<int> OnTotalScoreChanged;
    public event Action<int> OnBananaChanged;
    public event Action<int> OnCoinChanged;
    public event Action<int> OnTryChanged;
    public event Action<int> OnStarsChanged;

    public event Action<int> OnScoreRewarded;
    public event Action<int> OnBananaRewarded;

    public event Action OnMonkeyFound;

    // -------------------------------------------------------
    // LEVEL PROGRESSION
    // -------------------------------------------------------

    [Header("Level Progression")]
    [Tooltip("Assign the root MapLevelManager.")]
    [SerializeField]
    private MapLevelManager mapLevelManager;

    // -------------------------------------------------------
    // CUMULATIVE SCORE TOTALS
    // -------------------------------------------------------

    [Header("Cumulative Score Reward Totals")]
    [Tooltip(
        "The total score value for reaching 1 star. " +
        "This is not an additional reward."
    )]
    [SerializeField, Min(0)]
    private int oneStarTotalScoreReward = 500;

    [Tooltip(
        "The total score value for reaching 2 stars. " +
        "Improving from 1 to 2 stars gives the difference."
    )]
    [SerializeField, Min(0)]
    private int twoStarTotalScoreReward = 750;

    [Tooltip(
        "The total score value for reaching 3 stars. " +
        "Improving from a lower result gives the difference."
    )]
    [SerializeField, Min(0)]
    private int threeStarTotalScoreReward = 1000;

    // -------------------------------------------------------
    // CUMULATIVE BANANA TOTALS
    // -------------------------------------------------------

    [Header("Cumulative Banana Reward Totals")]
    [Tooltip("The total banana value for reaching 1 star.")]
    [SerializeField, Min(0)]
    private int oneStarTotalBananaReward = 5;

    [Tooltip("The total banana value for reaching 2 stars.")]
    [SerializeField, Min(0)]
    private int twoStarTotalBananaReward = 7;

    [Tooltip("The total banana value for reaching 3 stars.")]
    [SerializeField, Min(0)]
    private int threeStarTotalBananaReward = 10;

    // -------------------------------------------------------
    // STAR CALCULATION
    // -------------------------------------------------------

    [Header("Stars By Try Count")]
    [Tooltip("Maximum tries allowed to receive 3 stars.")]
    [SerializeField, Min(1)]
    private int threeStarMaximumTries = 1;

    [Tooltip("Maximum tries allowed to receive 2 stars.")]
    [SerializeField, Min(1)]
    private int twoStarMaximumTries = 2;

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    // -------------------------------------------------------
    // UNITY
    // -------------------------------------------------------

    private void Awake()
    {
        ResolveMapLevelManager();
        LoadSavedData();
        ResetCurrentLevelData();
    }

    private void Start()
    {
        RefreshAllEvents();
    }

    private void OnValidate()
    {
        if (twoStarMaximumTries < threeStarMaximumTries)
        {
            twoStarMaximumTries =
                threeStarMaximumTries;
        }
    }

    private void ResolveMapLevelManager()
    {
        if (mapLevelManager != null)
            return;

        mapLevelManager =
            FindFirstObjectByType<MapLevelManager>();

        if (mapLevelManager == null)
        {
            Debug.LogError(
                "[LevelState] MapLevelManager was not found. " +
                "Cumulative level rewards cannot be calculated safely.",
                this
            );
        }
    }

    // -------------------------------------------------------
    // TRY SYSTEM
    // -------------------------------------------------------

    public void RegisterTry()
    {
        if (IsMonkeyFound)
            return;

        TryCount++;

        OnTryChanged?.Invoke(TryCount);

        DebugLog(
            $"Try registered. TryCount={TryCount}."
        );
    }

    // -------------------------------------------------------
    // MONKEY FOUND
    // -------------------------------------------------------

    public void MarkMonkeyFound()
    {
        if (IsMonkeyFound)
        {
            DebugLog(
                "MarkMonkeyFound ignored because the monkey " +
                "was already found during this run."
            );

            return;
        }

        ResolveMapLevelManager();

        IsMonkeyFound = true;

        // Safety if RegisterTry was not called.
        if (TryCount <= 0)
        {
            TryCount = 1;
            OnTryChanged?.Invoke(TryCount);
        }

        EarnedStars = CalculateStars();

        PrepareCurrentRunStarData();
        CalculateCurrentRunRewards();
        ApplyCurrentRunRewards();

        NotifyRewardEvents();
        SaveSavedData();

        DebugLog(
            $"Monkey found. " +
            $"Level={GetCurrentLevelNumber()}, " +
            $"Tries={TryCount}, " +
            $"EarnedStars={EarnedStars}, " +
            $"PreviousBestStars={PreviousBestStarsThisRun}, " +
            $"BestStarsAfterRun={BestStarsAfterThisRun}, " +
            $"IsNewBest={IsNewBestStarResultThisRun}, " +
            $"ScoreReward={LastScoreReward}, " +
            $"BananaReward={LastBananaReward}, " +
            $"TotalScore={TotalScore}, " +
            $"TotalBananas={TotalBananas}."
        );

        /*
         * The completion-panel controller receives this event.
         *
         * It must:
         * 1. Calculate coin reward using PreviousBestStarsThisRun.
         * 2. Call MapLevelManager.CompleteCurrentLevel().
         * 3. Open the completion panel.
         *
         * CompleteCurrentLevel must happen after the reward differences
         * are calculated because it saves the new best-star result.
         */
        OnMonkeyFound?.Invoke();
    }

    private void PrepareCurrentRunStarData()
    {
        PreviousBestStarsThisRun = 0;
        BestStarsAfterThisRun = EarnedStars;
        IsNewBestStarResultThisRun = false;
        WasStarRewardClaimedThisRun = false;

        if (mapLevelManager == null)
        {
            Debug.LogError(
                "[LevelState] MapLevelManager is missing. " +
                "No score or banana reward will be granted.",
                this
            );

            return;
        }

        int currentLevel =
            mapLevelManager.CurrentLevel;

        if (currentLevel < 1 ||
            currentLevel > mapLevelManager.LevelCount)
        {
            Debug.LogError(
                "[LevelState] Current level number is invalid. " +
                "No score or banana reward will be granted.",
                this
            );

            return;
        }

        PreviousBestStarsThisRun =
            mapLevelManager.GetSavedStars(
                currentLevel
            );

        BestStarsAfterThisRun =
            Mathf.Max(
                PreviousBestStarsThisRun,
                EarnedStars
            );

        IsNewBestStarResultThisRun =
            EarnedStars >
            PreviousBestStarsThisRun;

        // Compatibility with older panel code.
        WasStarRewardClaimedThisRun =
            IsNewBestStarResultThisRun;
    }

    private void CalculateCurrentRunRewards()
    {
        LastScoreReward = 0;
        LastBananaReward = 0;
        LevelScore = 0;

        if (mapLevelManager == null)
            return;

        if (!IsNewBestStarResultThisRun)
        {
            DebugLog(
                $"No cumulative reward. " +
                $"EarnedStars={EarnedStars} is not higher than " +
                $"PreviousBestStars={PreviousBestStarsThisRun}."
            );

            return;
        }

        LastScoreReward =
            mapLevelManager.CalculateCumulativeReward(
                PreviousBestStarsThisRun,
                EarnedStars,
                oneStarTotalScoreReward,
                twoStarTotalScoreReward,
                threeStarTotalScoreReward
            );

        LastBananaReward =
            mapLevelManager.CalculateCumulativeReward(
                PreviousBestStarsThisRun,
                EarnedStars,
                oneStarTotalBananaReward,
                twoStarTotalBananaReward,
                threeStarTotalBananaReward
            );

        LevelScore =
            LastScoreReward;

        DebugLog(
            $"Cumulative reward calculated. " +
            $"PreviousBestStars={PreviousBestStarsThisRun}, " +
            $"EarnedStars={EarnedStars}, " +
            $"ScoreDifference={LastScoreReward}, " +
            $"BananaDifference={LastBananaReward}."
        );
    }

    private void ApplyCurrentRunRewards()
    {
        if (LastScoreReward > 0)
        {
            TotalScore +=
                LastScoreReward;
        }

        if (LastBananaReward > 0)
        {
            TotalBananas +=
                LastBananaReward;
        }
    }

    private int GetCurrentLevelNumber()
    {
        if (mapLevelManager == null)
            return 0;

        return mapLevelManager.CurrentLevel;
    }

    private void NotifyRewardEvents()
    {
        OnScoreChanged?.Invoke(
            LevelScore
        );

        OnTotalScoreChanged?.Invoke(
            TotalScore
        );

        OnBananaChanged?.Invoke(
            TotalBananas
        );

        OnStarsChanged?.Invoke(
            EarnedStars
        );

        OnScoreRewarded?.Invoke(
            LastScoreReward
        );

        OnBananaRewarded?.Invoke(
            LastBananaReward
        );
    }

    // -------------------------------------------------------
    // STAR CALCULATION
    // -------------------------------------------------------

    private int CalculateStars()
    {
        if (TryCount <= threeStarMaximumTries)
            return 3;

        if (TryCount <= twoStarMaximumTries)
            return 2;

        return 1;
    }

    // -------------------------------------------------------
    // PUBLIC REWARD-TOTAL HELPERS
    // -------------------------------------------------------

    public int GetTotalScoreRewardForStars(
        int stars
    )
    {
        switch (Mathf.Clamp(stars, 0, 3))
        {
            case 3:
                return threeStarTotalScoreReward;

            case 2:
                return twoStarTotalScoreReward;

            case 1:
                return oneStarTotalScoreReward;

            default:
                return 0;
        }
    }

    public int GetTotalBananaRewardForStars(
        int stars
    )
    {
        switch (Mathf.Clamp(stars, 0, 3))
        {
            case 3:
                return threeStarTotalBananaReward;

            case 2:
                return twoStarTotalBananaReward;

            case 1:
                return oneStarTotalBananaReward;

            default:
                return 0;
        }
    }

    // -------------------------------------------------------
    // COINS
    // -------------------------------------------------------

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        TotalCoins += amount;

        SaveSavedData();

        OnCoinChanged?.Invoke(
            TotalCoins
        );

        DebugLog(
            $"Added {amount} coin(s). " +
            $"TotalCoins={TotalCoins}."
        );
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
            return true;

        if (TotalCoins < amount)
        {
            DebugLog(
                $"Cannot spend {amount} coin(s). " +
                $"TotalCoins={TotalCoins}."
            );

            return false;
        }

        TotalCoins -= amount;

        SaveSavedData();

        OnCoinChanged?.Invoke(
            TotalCoins
        );

        DebugLog(
            $"Spent {amount} coin(s). " +
            $"TotalCoins={TotalCoins}."
        );

        return true;
    }

    // -------------------------------------------------------
    // BANANAS
    // -------------------------------------------------------

    public void AddBananas(int amount)
    {
        if (amount <= 0)
            return;

        TotalBananas += amount;

        SaveSavedData();

        OnBananaChanged?.Invoke(
            TotalBananas
        );

        DebugLog(
            $"Added {amount} banana(s). " +
            $"TotalBananas={TotalBananas}."
        );
    }

    public bool SpendBananas(int amount)
    {
        if (amount <= 0)
            return true;

        if (TotalBananas < amount)
        {
            DebugLog(
                $"Cannot spend {amount} banana(s). " +
                $"TotalBananas={TotalBananas}."
            );

            return false;
        }

        TotalBananas -= amount;

        SaveSavedData();

        OnBananaChanged?.Invoke(
            TotalBananas
        );

        DebugLog(
            $"Spent {amount} banana(s). " +
            $"TotalBananas={TotalBananas}."
        );

        return true;
    }

    // -------------------------------------------------------
    // MANUAL SAVED VALUE CONTROL
    // -------------------------------------------------------

    public void SetTotalScore(int value)
    {
        TotalScore =
            Mathf.Max(
                0,
                value
            );

        SaveSavedData();

        OnTotalScoreChanged?.Invoke(
            TotalScore
        );

        DebugLog(
            $"TotalScore manually set to {TotalScore}."
        );
    }

    public void SetTotalCoins(int value)
    {
        TotalCoins =
            Mathf.Max(
                0,
                value
            );

        SaveSavedData();

        OnCoinChanged?.Invoke(
            TotalCoins
        );

        DebugLog(
            $"TotalCoins manually set to {TotalCoins}."
        );
    }

    public void SetTotalBananas(int value)
    {
        TotalBananas =
            Mathf.Max(
                0,
                value
            );

        SaveSavedData();

        OnBananaChanged?.Invoke(
            TotalBananas
        );

        DebugLog(
            $"TotalBananas manually set to {TotalBananas}."
        );
    }

    public void SetAllSavedTotals(
        int score,
        int coins,
        int bananas
    )
    {
        TotalScore =
            Mathf.Max(
                0,
                score
            );

        TotalCoins =
            Mathf.Max(
                0,
                coins
            );

        TotalBananas =
            Mathf.Max(
                0,
                bananas
            );

        SaveSavedData();
        RefreshCurrencyEvents();

        DebugLog(
            $"Manual totals applied. " +
            $"Score={TotalScore}, " +
            $"Coins={TotalCoins}, " +
            $"Bananas={TotalBananas}."
        );
    }

    // -------------------------------------------------------
    // RESET CURRENT LEVEL
    // -------------------------------------------------------

    public void ResetCurrentLevelState()
    {
        ResetCurrentLevelData();

        OnScoreChanged?.Invoke(
            LevelScore
        );

        OnTryChanged?.Invoke(
            TryCount
        );

        OnStarsChanged?.Invoke(
            EarnedStars
        );

        DebugLog(
            "Current-level runtime state reset."
        );
    }

    private void ResetCurrentLevelData()
    {
        IsMonkeyFound = false;
        TryCount = 0;
        LevelScore = 0;
        EarnedStars = 0;

        PreviousBestStarsThisRun = 0;
        BestStarsAfterThisRun = 0;
        IsNewBestStarResultThisRun = false;
        WasStarRewardClaimedThisRun = false;

        LastScoreReward = 0;
        LastBananaReward = 0;
    }

    // -------------------------------------------------------
    // RESET SAVED CURRENCY
    // -------------------------------------------------------

    [ContextMenu("RESET - Score Coins And Bananas")]
    public void ResetAllSavedData()
    {
        TotalScore = 0;
        TotalCoins = 0;
        TotalBananas = 0;

        PlayerPrefs.SetInt(
            TotalScoreKey,
            0
        );

        PlayerPrefs.SetInt(
            TotalCoinsKey,
            0
        );

        PlayerPrefs.SetInt(
            TotalBananasKey,
            0
        );

        PlayerPrefs.Save();

        ResetCurrentLevelData();
        RefreshAllEvents();

        Debug.Log(
            "[LevelState] Score, coins, bananas and " +
            "current-level data reset to zero.",
            this
        );
    }

    // -------------------------------------------------------
    // SAVE / LOAD
    // -------------------------------------------------------

    private void LoadSavedData()
    {
        TotalScore =
            PlayerPrefs.GetInt(
                TotalScoreKey,
                0
            );

        TotalCoins =
            PlayerPrefs.GetInt(
                TotalCoinsKey,
                0
            );

        TotalBananas =
            PlayerPrefs.GetInt(
                TotalBananasKey,
                0
            );

        DebugLog(
            $"Saved data loaded. " +
            $"Score={TotalScore}, " +
            $"Coins={TotalCoins}, " +
            $"Bananas={TotalBananas}."
        );
    }

    private void SaveSavedData()
    {
        PlayerPrefs.SetInt(
            TotalScoreKey,
            TotalScore
        );

        PlayerPrefs.SetInt(
            TotalCoinsKey,
            TotalCoins
        );

        PlayerPrefs.SetInt(
            TotalBananasKey,
            TotalBananas
        );

        PlayerPrefs.Save();
    }

    // -------------------------------------------------------
    // EVENT REFRESH
    // -------------------------------------------------------

    private void RefreshCurrencyEvents()
    {
        OnTotalScoreChanged?.Invoke(
            TotalScore
        );

        OnCoinChanged?.Invoke(
            TotalCoins
        );

        OnBananaChanged?.Invoke(
            TotalBananas
        );
    }

    private void RefreshAllEvents()
    {
        OnScoreChanged?.Invoke(
            LevelScore
        );

        OnTotalScoreChanged?.Invoke(
            TotalScore
        );

        OnCoinChanged?.Invoke(
            TotalCoins
        );

        OnBananaChanged?.Invoke(
            TotalBananas
        );

        OnTryChanged?.Invoke(
            TryCount
        );

        OnStarsChanged?.Invoke(
            EarnedStars
        );
    }

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    [ContextMenu("DEBUG - Print Current Reward")]
    private void DebugPrintCurrentReward()
    {
        Debug.Log(
            $"[LevelState] " +
            $"Level={GetCurrentLevelNumber()}, " +
            $"Tries={TryCount}, " +
            $"EarnedStars={EarnedStars}, " +
            $"PreviousBestStars={PreviousBestStarsThisRun}, " +
            $"BestStarsAfterRun={BestStarsAfterThisRun}, " +
            $"NewBest={IsNewBestStarResultThisRun}, " +
            $"ScoreReward={LastScoreReward}, " +
            $"BananaReward={LastBananaReward}.",
            this
        );
    }

    private void DebugLog(string message)
    {
        if (!showDebugLogs)
            return;

        Debug.Log(
            "[LevelState] " + message,
            this
        );
    }
}