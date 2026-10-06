using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class GameplayHUD : MonoBehaviour
{
    [Header("Game State")]
    [Tooltip("Assign the GameObject containing LevelState.")]
    [SerializeField]
    private LevelState levelState;

    [Header("HUD Text References")]
    [Tooltip("Assign HUD/Score/ScoreText.")]
    [SerializeField]
    private TMP_Text scoreText;

    [Tooltip("Assign HUD/Coin/CoinText.")]
    [SerializeField]
    private TMP_Text coinText;

    [Tooltip("Assign HUD/Banana/BananaText.")]
    [SerializeField]
    private TMP_Text bananaText;

    [Header("Optional Text")]
    [Tooltip("Optional current try counter.")]
    [SerializeField]
    private TMP_Text tryText;

    [Tooltip("Optional earned-star counter.")]
    [SerializeField]
    private TMP_Text starText;

    [Tooltip("Optional level timer text.")]
    [SerializeField]
    private TMP_Text timerText;

    [Header("Formatting")]
    [SerializeField]
    private bool useThousandsSeparator = false;

    [SerializeField]
    private string tryPrefix = "TRIES: ";

    [SerializeField]
    private string starPrefix = "STARS: ";

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = false;

    private bool isSubscribed;
    private LevelTimer levelTimer;

    private void Awake()
    {
        FindLevelStateIfMissing();
    }

    private void OnEnable()
    {
        FindLevelStateIfMissing();
        FindLevelTimerIfMissing();
        SubscribeToEvents();
        RefreshHUD();
    }

    private void Start()
    {
        RefreshHUD();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void FindLevelStateIfMissing()
    {
        if (levelState != null)
            return;

        levelState = FindFirstObjectByType<LevelState>();

        if (levelState == null)
        {
            Debug.LogError(
                "GameplayHUD: LevelState was not found. " +
                "Assign the LevelState object to the Level State field.",
                this
            );
        }
    }

    private void FindLevelTimerIfMissing()
    {
        if (levelTimer == null)
            levelTimer = FindFirstObjectByType<LevelTimer>();
    }

    private void SubscribeToEvents()
    {
        if (levelState == null || isSubscribed)
            return;

        levelState.OnTotalScoreChanged += UpdateScoreText;
        levelState.OnCoinChanged += UpdateCoinText;
        levelState.OnBananaChanged += UpdateBananaText;
        levelState.OnTryChanged += UpdateTryText;
        levelState.OnStarsChanged += UpdateStarText;
        if (levelTimer != null) levelTimer.OnSecondsChanged += UpdateTimerText;

        isSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (levelState == null || !isSubscribed)
            return;

        levelState.OnTotalScoreChanged -= UpdateScoreText;
        levelState.OnCoinChanged -= UpdateCoinText;
        levelState.OnBananaChanged -= UpdateBananaText;
        levelState.OnTryChanged -= UpdateTryText;
        levelState.OnStarsChanged -= UpdateStarText;
        if (levelTimer != null) levelTimer.OnSecondsChanged -= UpdateTimerText;

        isSubscribed = false;
    }

    public void RefreshHUD()
    {
        if (levelState == null)
            return;

        UpdateScoreText(levelState.TotalScore);
        UpdateCoinText(levelState.TotalCoins);
        UpdateBananaText(levelState.TotalBananas);
        UpdateTryText(levelState.TryCount);
        UpdateStarText(levelState.EarnedStars);
        if (levelTimer != null) UpdateTimerText(levelTimer.DisplaySeconds);

        if (showDebugLogs)
        {
            Debug.Log(
                $"HUD refreshed. " +
                $"Score: {levelState.TotalScore}, " +
                $"Coins: {levelState.TotalCoins}, " +
                $"Bananas: {levelState.TotalBananas}, " +
                $"Tries: {levelState.TryCount}, " +
                $"Stars: {levelState.EarnedStars}.",
                this
            );
        }
    }

    private void UpdateScoreText(int value)
    {
        if (scoreText != null)
            scoreText.text = FormatNumber(value);
    }

    private void UpdateCoinText(int value)
    {
        if (coinText != null)
            coinText.text = FormatNumber(value);
    }

    private void UpdateBananaText(int value)
    {
        if (bananaText != null)
            bananaText.text = FormatNumber(value);
    }

    private void UpdateTryText(int value)
    {
        if (tryText != null)
            tryText.text = tryPrefix + value;
    }

    private void UpdateStarText(int value)
    {
        if (starText != null)
            starText.text = starPrefix + value;
    }

    private void UpdateTimerText(int totalSeconds)
    {
        if (timerText != null)
            timerText.text = $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }

    private string FormatNumber(int value)
    {
        return useThousandsSeparator
            ? value.ToString("N0")
            : value.ToString();
    }
}