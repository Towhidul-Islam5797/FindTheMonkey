using UnityEngine;

[DisallowMultipleComponent]
public class LevelSettings : MonoBehaviour
{
    [Header("Level Time")]
    [Tooltip("Time for this level in seconds. 0 means use the LevelTimer value in the scene.")]
    [SerializeField, Min(0f)]
    private float timeLimitSeconds = 0f;

    public float TimeLimitSeconds => timeLimitSeconds;

    [Header("Time Bonus Coins")]
    [Tooltip("Turn on to use the coin totals below for this level instead of the LevelTimeBonus values in the scene.")]
    [SerializeField]
    private bool overrideBonusCoins = false;

    [Tooltip("Total bonus coins for reaching tier 1.")]
    [SerializeField, Min(0)]
    private int oneTierTotalBonus = 1;

    [Tooltip("Total bonus coins for reaching tier 2.")]
    [SerializeField, Min(0)]
    private int twoTierTotalBonus = 2;

    [Tooltip("Total bonus coins for reaching tier 3.")]
    [SerializeField, Min(0)]
    private int threeTierTotalBonus = 3;

    public bool OverrideBonusCoins => overrideBonusCoins;

    public int OneTierTotalBonus => oneTierTotalBonus;

    public int TwoTierTotalBonus => twoTierTotalBonus;

    public int ThreeTierTotalBonus => threeTierTotalBonus;

    [Header("Time Limit Rule")]
    [Tooltip("Turn on to lose this level when the timer reaches 0:00.")]
    [SerializeField]
    private bool endLevelWhenTimeRunsOut = false;

    public bool EndLevelWhenTimeRunsOut => endLevelWhenTimeRunsOut;

    private void OnValidate()
    {
        twoTierTotalBonus = Mathf.Max(twoTierTotalBonus, oneTierTotalBonus);
        threeTierTotalBonus = Mathf.Max(threeTierTotalBonus, twoTierTotalBonus);
    }

    public static LevelSettings Current { get; private set; }

    private void OnEnable()
    {
        if (Current != null && Current != this)
        {
            Debug.LogWarning(
                "[LevelSettings] Two levels are active at once. " +
                "The last enabled one is used.",
                this
            );
        }

        Current = this;
    }

    private void OnDisable()
    {
        if (Current == this)
            Current = null;
    }
}
