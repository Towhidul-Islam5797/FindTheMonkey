using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MapLevelManager : MonoBehaviour
{
    // -------------------------------------------------------
    // MAP
    // -------------------------------------------------------

    [Header("Map")]
    [Tooltip("Use Jungle now. Later use Desert or Snow.")]
    [SerializeField]
    private string mapId = "Jungle";

    // -------------------------------------------------------
    // LEVEL ROOTS
    // -------------------------------------------------------

    [Header("Level Roots")]
    [Tooltip(
        "Assign Level_1, Level_2, etc. in the correct order."
    )]
    [SerializeField]
    private GameObject[] levelRoots;

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    // -------------------------------------------------------
    // PUBLIC PROPERTIES
    // -------------------------------------------------------

    public string MapId => mapId;

    public int CurrentLevel { get; private set; }

    public int LevelCount
    {
        get
        {
            return levelRoots == null
                ? 0
                : levelRoots.Length;
        }
    }

    public int UnlockedLevel
    {
        get
        {
            return PlayerPrefs.GetInt(
                UnlockedLevelKey,
                1
            );
        }
    }

    public int CurrentLevelBestStars
    {
        get
        {
            return GetSavedStars(CurrentLevel);
        }
    }

    // -------------------------------------------------------
    // SAVE KEYS
    // -------------------------------------------------------

    private string SelectedLevelKey =>
        mapId + "_SelectedLevel";

    private string UnlockedLevelKey =>
        mapId + "_UnlockedLevel";

    private string GetStarsKey(int levelNumber)
    {
        return mapId +
               "_LevelStars_" +
               levelNumber;
    }

    private string GetCompletedKey(int levelNumber)
    {
        return mapId +
               "_LevelCompleted_" +
               levelNumber;
    }

    // -------------------------------------------------------
    // UNITY
    // -------------------------------------------------------

    private void Awake()
    {
        Time.timeScale = 1f;

        ValidateMapId();
        EnsureSaveDataExists();
        LoadSelectedLevel();
    }

    private void OnValidate()
    {
        ValidateMapId();
    }

    private void ValidateMapId()
    {
        if (string.IsNullOrWhiteSpace(mapId))
        {
            mapId = "Jungle";
        }
        else
        {
            mapId = mapId.Trim();
        }
    }

    private void EnsureSaveDataExists()
    {
        if (!PlayerPrefs.HasKey(UnlockedLevelKey))
        {
            PlayerPrefs.SetInt(
                UnlockedLevelKey,
                1
            );
        }

        if (!PlayerPrefs.HasKey(SelectedLevelKey))
        {
            PlayerPrefs.SetInt(
                SelectedLevelKey,
                1
            );
        }

        PlayerPrefs.Save();
    }

    // -------------------------------------------------------
    // LEVEL ACTIVATION
    // -------------------------------------------------------

    private void LoadSelectedLevel()
    {
        if (!HasValidLevelRoots())
            return;

        int selectedLevel =
            PlayerPrefs.GetInt(
                SelectedLevelKey,
                1
            );

        int highestAvailableLevel =
            Mathf.Clamp(
                UnlockedLevel,
                1,
                LevelCount
            );

        selectedLevel =
            Mathf.Clamp(
                selectedLevel,
                1,
                highestAvailableLevel
            );

        ActivateLevel(selectedLevel);
    }

    private bool HasValidLevelRoots()
    {
        if (levelRoots == null ||
            levelRoots.Length == 0)
        {
            Debug.LogError(
                $"[MapLevelManager:{mapId}] " +
                "No level roots were assigned.",
                this
            );

            return false;
        }

        return true;
    }

    private void ActivateLevel(int levelNumber)
    {
        if (!HasValidLevelRoots())
            return;

        levelNumber =
            Mathf.Clamp(
                levelNumber,
                1,
                LevelCount
            );

        CurrentLevel = levelNumber;

        for (int i = 0;
             i < levelRoots.Length;
             i++)
        {
            GameObject levelRoot =
                levelRoots[i];

            if (levelRoot == null)
            {
                Debug.LogWarning(
                    $"[MapLevelManager:{mapId}] " +
                    $"Level Roots Element {i} is empty.",
                    this
                );

                continue;
            }

            bool shouldBeActive =
                i == CurrentLevel - 1;

            levelRoot.SetActive(
                shouldBeActive
            );
        }

        PlayerPrefs.SetInt(
            SelectedLevelKey,
            CurrentLevel
        );

        PlayerPrefs.Save();

        DebugLog(
            $"Level {CurrentLevel} activated. " +
            $"UnlockedLevel={UnlockedLevel}, " +
            $"BestStars={CurrentLevelBestStars}, " +
            $"LevelCount={LevelCount}."
        );
    }

    // -------------------------------------------------------
    // LEVEL INFORMATION
    // -------------------------------------------------------

    public bool IsLevelUnlocked(int levelNumber)
    {
        return levelNumber >= 1 &&
               levelNumber <= LevelCount &&
               levelNumber <= UnlockedLevel;
    }

    public bool IsLevelCompleted(int levelNumber)
    {
        if (!IsValidLevelNumber(levelNumber))
            return false;

        return PlayerPrefs.GetInt(
            GetCompletedKey(levelNumber),
            0
        ) == 1;
    }

    public int GetSavedStars(int levelNumber)
    {
        if (!IsValidLevelNumber(levelNumber))
            return 0;

        return Mathf.Clamp(
            PlayerPrefs.GetInt(
                GetStarsKey(levelNumber),
                0
            ),
            0,
            3
        );
    }

    public bool IsNewBestStarResult(
        int levelNumber,
        int earnedStars
    )
    {
        if (!IsValidLevelNumber(levelNumber))
            return false;

        earnedStars =
            Mathf.Clamp(
                earnedStars,
                0,
                3
            );

        return earnedStars >
               GetSavedStars(levelNumber);
    }

    private bool IsValidLevelNumber(
        int levelNumber
    )
    {
        return levelNumber >= 1 &&
               levelNumber <= LevelCount;
    }

    // -------------------------------------------------------
    // CUMULATIVE REWARD CALCULATION
    // -------------------------------------------------------

    /// <summary>
    /// Calculates the missing reward between the previous best
    /// star result and the current result.
    ///
    /// Example totals:
    /// 1 star = 2
    /// 2 stars = 3
    /// 3 stars = 5
    ///
    /// Previous best 1 star, current result 3 stars:
    /// reward = 5 - 2 = 3.
    /// </summary>
    public int CalculateCumulativeReward(
        int previousBestStars,
        int earnedStars,
        int oneStarTotalReward,
        int twoStarTotalReward,
        int threeStarTotalReward
    )
    {
        previousBestStars =
            Mathf.Clamp(
                previousBestStars,
                0,
                3
            );

        earnedStars =
            Mathf.Clamp(
                earnedStars,
                0,
                3
            );

        oneStarTotalReward =
            Mathf.Max(
                0,
                oneStarTotalReward
            );

        twoStarTotalReward =
            Mathf.Max(
                0,
                twoStarTotalReward
            );

        threeStarTotalReward =
            Mathf.Max(
                0,
                threeStarTotalReward
            );

        /*
         * A lower or equal star result never gives another reward.
         */
        if (earnedStars <= previousBestStars)
            return 0;

        int previousTotalReward =
            GetTotalRewardForStars(
                previousBestStars,
                oneStarTotalReward,
                twoStarTotalReward,
                threeStarTotalReward
            );

        int currentTotalReward =
            GetTotalRewardForStars(
                earnedStars,
                oneStarTotalReward,
                twoStarTotalReward,
                threeStarTotalReward
            );

        return Mathf.Max(
            0,
            currentTotalReward -
            previousTotalReward
        );
    }

    private int GetTotalRewardForStars(
        int stars,
        int oneStarTotalReward,
        int twoStarTotalReward,
        int threeStarTotalReward
    )
    {
        switch (stars)
        {
            case 3:
                return threeStarTotalReward;

            case 2:
                return twoStarTotalReward;

            case 1:
                return oneStarTotalReward;

            default:
                return 0;
        }
    }

    // -------------------------------------------------------
    // LEVEL COMPLETION
    // -------------------------------------------------------

    /// <summary>
    /// Saves the best star result, marks the level completed
    /// and unlocks the next level.
    ///
    /// Important:
    /// Calculate score, banana and coin differences before
    /// calling this method because this method saves the new
    /// best star result.
    ///
    /// Returns true only on the first completion.
    /// </summary>
    public bool CompleteCurrentLevel(
        int earnedStars
    )
    {
        if (!IsValidLevelNumber(CurrentLevel))
        {
            Debug.LogError(
                $"[MapLevelManager:{mapId}] " +
                "Cannot complete the current level because " +
                "CurrentLevel is invalid.",
                this
            );

            return false;
        }

        earnedStars =
            Mathf.Clamp(
                earnedStars,
                1,
                3
            );

        int previousBestStars =
            GetSavedStars(CurrentLevel);

        bool isFirstCompletion =
            !IsLevelCompleted(CurrentLevel);

        /*
         * Save only an improved star result.
         */
        if (earnedStars > previousBestStars)
        {
            PlayerPrefs.SetInt(
                GetStarsKey(CurrentLevel),
                earnedStars
            );
        }

        PlayerPrefs.SetInt(
            GetCompletedKey(CurrentLevel),
            1
        );

        /*
         * Completing a level unlocks the next level.
         */
        if (CurrentLevel < LevelCount)
        {
            int nextLevel =
                CurrentLevel + 1;

            if (nextLevel > UnlockedLevel)
            {
                PlayerPrefs.SetInt(
                    UnlockedLevelKey,
                    nextLevel
                );
            }
        }

        PlayerPrefs.Save();

        DebugLog(
            $"Level {CurrentLevel} completed. " +
            $"EarnedStars={earnedStars}, " +
            $"PreviousBestStars={previousBestStars}, " +
            $"SavedBestStars={GetSavedStars(CurrentLevel)}, " +
            $"FirstCompletion={isFirstCompletion}, " +
            $"UnlockedLevel={UnlockedLevel}."
        );

        return isFirstCompletion;
    }

    // -------------------------------------------------------
    // LEGACY COMPATIBILITY
    // -------------------------------------------------------

    /*
     * These methods keep older LevelState versions compiling.
     *
     * The new cumulative LevelState should not use them.
     * It should use:
     *
     * GetSavedStars(...)
     * CalculateCumulativeReward(...)
     */

    public bool IsStarRewardClaimed(
        int levelNumber,
        int stars
    )
    {
        if (!IsValidLevelNumber(levelNumber))
            return false;

        stars =
            Mathf.Clamp(
                stars,
                1,
                3
            );

        return GetSavedStars(levelNumber) >= stars;
    }

    public bool IsOneStarRewardClaimed(
        int levelNumber
    )
    {
        return IsStarRewardClaimed(
            levelNumber,
            1
        );
    }

    public bool IsTwoStarRewardClaimed(
        int levelNumber
    )
    {
        return IsStarRewardClaimed(
            levelNumber,
            2
        );
    }

    public bool IsThreeStarRewardClaimed(
        int levelNumber
    )
    {
        return IsStarRewardClaimed(
            levelNumber,
            3
        );
    }

    public bool CanClaimStarReward(
        int levelNumber,
        int stars
    )
    {
        if (!IsValidLevelNumber(levelNumber))
            return false;

        stars =
            Mathf.Clamp(
                stars,
                1,
                3
            );

        return stars >
               GetSavedStars(levelNumber);
    }

    public bool TryClaimStarReward(
        int levelNumber,
        int stars
    )
    {
        /*
         * This method no longer writes PlayerPrefs.
         * Saving the new best happens in CompleteCurrentLevel().
         */
        return CanClaimStarReward(
            levelNumber,
            stars
        );
    }

    // -------------------------------------------------------
    // LEVEL SELECTION
    // -------------------------------------------------------

    public void SelectLevel(int levelNumber)
    {
        if (!IsValidLevelNumber(levelNumber))
        {
            Debug.LogError(
                $"[MapLevelManager:{mapId}] " +
                $"Level {levelNumber} does not exist.",
                this
            );

            return;
        }

        if (!IsLevelUnlocked(levelNumber))
        {
            Debug.LogWarning(
                $"[MapLevelManager:{mapId}] " +
                $"Level {levelNumber} is locked. " +
                $"UnlockedLevel={UnlockedLevel}.",
                this
            );

            return;
        }

        PlayerPrefs.SetInt(
            SelectedLevelKey,
            levelNumber
        );

        PlayerPrefs.Save();

        DebugLog(
            $"Level {levelNumber} selected."
        );

        if (Application.isPlaying)
        {
            ReloadCurrentScene();
        }
        else
        {
            Debug.Log(
                $"[MapLevelManager:{mapId}] " +
                $"Level {levelNumber} was saved as the selected level. " +
                "Enter Play Mode to load it.",
                this
            );
        }
    }

    public void RestartCurrentLevel()
    {
        Time.timeScale = 1f;

        DebugLog(
            $"Restarting Level {CurrentLevel}."
        );

        ReloadCurrentScene();
    }

    public void LoadNextLevel()
    {
        int nextLevel =
            CurrentLevel + 1;

        if (nextLevel > LevelCount)
        {
            DebugLog(
                "There is no next level."
            );

            return;
        }

        if (!IsLevelUnlocked(nextLevel))
        {
            Debug.LogWarning(
                $"[MapLevelManager:{mapId}] " +
                $"Level {nextLevel} is still locked.",
                this
            );

            return;
        }

        PlayerPrefs.SetInt(
            SelectedLevelKey,
            nextLevel
        );

        PlayerPrefs.Save();

        DebugLog(
            $"Loading Level {nextLevel}."
        );

        ReloadCurrentScene();
    }

    public bool HasNextLevel()
    {
        return CurrentLevel < LevelCount;
    }

    // -------------------------------------------------------
    // BUTTON HELPERS
    // -------------------------------------------------------

    public void SelectLevel1()
    {
        SelectLevel(1);
    }

    public void SelectLevel2()
    {
        SelectLevel(2);
    }

    public void SelectLevel3()
    {
        SelectLevel(3);
    }

    public void SelectLevel4()
    {
        SelectLevel(4);
    }

    public void SelectLevel5()
    {
        SelectLevel(5);
    }

    public void SelectLevel6()
    {
        SelectLevel(6);
    }

    public void SelectLevel7()
    {
        SelectLevel(7);
    }

    public void SelectLevel8()
    {
        SelectLevel(8);
    }

    public void SelectLevel9()
    {
        SelectLevel(9);
    }

    public void SelectLevel10()
    {
        SelectLevel(10);
    }

    // -------------------------------------------------------
    // SCENE RELOAD
    // -------------------------------------------------------

    private void ReloadCurrentScene()
    {
        Time.timeScale = 1f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        if (currentScene.buildIndex < 0)
        {
            Debug.LogError(
                $"[MapLevelManager:{mapId}] " +
                "The current scene is not included in the " +
                "Build Profile Scene List.",
                this
            );

            return;
        }

        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    [ContextMenu("DEBUG - Print Progress")]
    private void DebugPrintProgress()
    {
        Debug.Log(
            $"[MapLevelManager:{mapId}] " +
            $"CurrentLevel={CurrentLevel}, " +
            $"UnlockedLevel={UnlockedLevel}, " +
            $"LevelCount={LevelCount}.",
            this
        );

        for (int levelNumber = 1;
             levelNumber <= LevelCount;
             levelNumber++)
        {
            Debug.Log(
                $"[MapLevelManager:{mapId}] " +
                $"Level={levelNumber}, " +
                $"Unlocked={IsLevelUnlocked(levelNumber)}, " +
                $"Completed={IsLevelCompleted(levelNumber)}, " +
                $"BestStars={GetSavedStars(levelNumber)}.",
                this
            );
        }
    }

    [ContextMenu("DEBUG - Test Reward Calculation")]
    private void DebugTestRewardCalculation()
    {
        Debug.Log(
            "[MapLevelManager] Example reward totals: " +
            "1 Star=2, 2 Stars=3, 3 Stars=5.",
            this
        );

        for (int previousStars = 0;
             previousStars <= 3;
             previousStars++)
        {
            for (int earnedStars = 1;
                 earnedStars <= 3;
                 earnedStars++)
            {
                int reward =
                    CalculateCumulativeReward(
                        previousStars,
                        earnedStars,
                        2,
                        3,
                        5
                    );

                Debug.Log(
                    $"PreviousBest={previousStars}, " +
                    $"EarnedStars={earnedStars}, " +
                    $"Reward={reward}.",
                    this
                );
            }
        }
    }

    private void DebugLog(string message)
    {
        if (!showDebugLogs)
            return;

        Debug.Log(
            $"[MapLevelManager:{mapId}] {message}",
            this
        );
    }
}