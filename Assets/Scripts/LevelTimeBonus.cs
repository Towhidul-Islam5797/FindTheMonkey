using UnityEngine;

[DisallowMultipleComponent]
public class LevelTimeBonus : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("Optional. Found automatically if empty.")]
    [SerializeField]
    private MapLevelManager mapLevelManager;

    [Header("Time Tier Limits (seconds)")]
    [Tooltip("Finish within this time to earn tier 3.")]
    [SerializeField, Min(1)]
    private int threeTierMaxSeconds = 20;

    [Tooltip("Finish within this time to earn tier 2.")]
    [SerializeField, Min(1)]
    private int twoTierMaxSeconds = 40;

    [Tooltip("Finish within this time to earn tier 1.")]
    [SerializeField, Min(1)]
    private int oneTierMaxSeconds = 60;

    [Header("Tiers From Level Time")]
    [Tooltip("When the active level has a time limit in LevelSettings, tier 3 is this fraction of that limit.")]
    [SerializeField, Range(0.05f, 1f)]
    private float threeTierFraction = 0.33f;

    [Tooltip("When the active level has a time limit in LevelSettings, tier 2 is this fraction of that limit.")]
    [SerializeField, Range(0.05f, 1f)]
    private float twoTierFraction = 0.66f;

    [Header("Cumulative Coin Bonus Totals")]
    [Tooltip("Total bonus coins for reaching tier 1.")]
    [SerializeField, Min(0)]
    private int oneTierTotalBonus = 1;

    [Tooltip("Total bonus coins for reaching tier 2.")]
    [SerializeField, Min(0)]
    private int twoTierTotalBonus = 2;

    [Tooltip("Total bonus coins for reaching tier 3.")]
    [SerializeField, Min(0)]
    private int threeTierTotalBonus = 3;

    private void OnValidate()
    {
        twoTierFraction = Mathf.Max(twoTierFraction, threeTierFraction);
    }

    public int ClaimBonus(int elapsedSeconds)
    {
        if (mapLevelManager == null)
            mapLevelManager = FindFirstObjectByType<MapLevelManager>();

        if (mapLevelManager == null)
            return 0;

        string key = GetBestTierKey();
        int previousBestTier = PlayerPrefs.GetInt(key, 0);
        int earnedTier = GetTier(elapsedSeconds);

        int oneTotal = oneTierTotalBonus;
        int twoTotal = twoTierTotalBonus;
        int threeTotal = threeTierTotalBonus;

        if (LevelSettings.Current != null &&
            LevelSettings.Current.OverrideBonusCoins)
        {
            oneTotal = LevelSettings.Current.OneTierTotalBonus;
            twoTotal = LevelSettings.Current.TwoTierTotalBonus;
            threeTotal = LevelSettings.Current.ThreeTierTotalBonus;
        }

        int bonus = mapLevelManager.CalculateCumulativeReward(
            previousBestTier,
            earnedTier,
            oneTotal,
            twoTotal,
            threeTotal
        );

        if (earnedTier > previousBestTier)
        {
            PlayerPrefs.SetInt(key, earnedTier);
            PlayerPrefs.Save();
        }

        return bonus;
    }

    private int GetTier(int elapsedSeconds)
    {
        int threeLimit = threeTierMaxSeconds;
        int twoLimit = twoTierMaxSeconds;
        int oneLimit = oneTierMaxSeconds;

        if (LevelSettings.Current != null &&
            LevelSettings.Current.TimeLimitSeconds > 0f)
        {
            float limit = LevelSettings.Current.TimeLimitSeconds;

            threeLimit = Mathf.RoundToInt(limit * threeTierFraction);
            twoLimit = Mathf.RoundToInt(limit * twoTierFraction);
            oneLimit = Mathf.RoundToInt(limit);
        }

        if (elapsedSeconds <= threeLimit)
            return 3;

        if (elapsedSeconds <= twoLimit)
            return 2;

        if (elapsedSeconds <= oneLimit)
            return 1;

        return 0;
    }

    private string GetBestTierKey()
    {
        return mapLevelManager.MapId +
               "_LevelBestTimeTier_" +
               mapLevelManager.CurrentLevel;
    }
}
