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

    public int ClaimBonus(int elapsedSeconds)
    {
        if (mapLevelManager == null)
            mapLevelManager = FindFirstObjectByType<MapLevelManager>();

        if (mapLevelManager == null)
            return 0;

        string key = GetBestTierKey();
        int previousBestTier = PlayerPrefs.GetInt(key, 0);
        int earnedTier = GetTier(elapsedSeconds);

        int bonus = mapLevelManager.CalculateCumulativeReward(
            previousBestTier,
            earnedTier,
            oneTierTotalBonus,
            twoTierTotalBonus,
            threeTierTotalBonus
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
        if (elapsedSeconds <= threeTierMaxSeconds)
            return 3;

        if (elapsedSeconds <= twoTierMaxSeconds)
            return 2;

        if (elapsedSeconds <= oneTierMaxSeconds)
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
