using UnityEngine;

[DisallowMultipleComponent]
public class LevelSettings : MonoBehaviour
{
    [Header("Level Time")]
    [Tooltip("Time for this level in seconds. 0 means use the LevelTimer value in the scene.")]
    [SerializeField, Min(0f)]
    private float timeLimitSeconds = 0f;

    public float TimeLimitSeconds => timeLimitSeconds;

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
