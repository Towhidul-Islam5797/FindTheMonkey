using System;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelTimer : MonoBehaviour
{
    [Header("Game State")]
    [Tooltip("Optional. Found automatically if empty.")]
    [SerializeField]
    private LevelState levelState;

    [Header("Timer")]
    [Tooltip("Time given for each level, in seconds.")]
    [SerializeField, Min(1f)]
    private float timeLimitSeconds = 60f;

    private float remainingSeconds;
    private int lastShownSeconds = -1;

    public bool IsRunning { get; private set; }

    public int DisplaySeconds => Mathf.CeilToInt(remainingSeconds);

    public event Action<int> OnSecondsChanged;

    private void Awake()
    {
        if (levelState == null)
            levelState = FindFirstObjectByType<LevelState>();

        remainingSeconds = timeLimitSeconds;
    }

    private void OnEnable()
    {
        if (levelState != null)
            levelState.OnMonkeyFound += StopTimer;
    }

    private void OnDisable()
    {
        if (levelState != null)
            levelState.OnMonkeyFound -= StopTimer;
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);

        NotifyIfSecondsChanged();

        if (remainingSeconds <= 0f)
            IsRunning = false;
    }

    public void StartTimer()
    {
        remainingSeconds = timeLimitSeconds;
        IsRunning = true;
        NotifyIfSecondsChanged();
    }

    public void StopTimer()
    {
        IsRunning = false;
    }

    private void NotifyIfSecondsChanged()
    {
        if (DisplaySeconds == lastShownSeconds)
            return;

        lastShownSeconds = DisplaySeconds;
        OnSecondsChanged?.Invoke(lastShownSeconds);
    }
}
