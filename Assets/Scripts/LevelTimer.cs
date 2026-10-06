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
    private CharacterPathMover watchedMover;
    private float elapsedSeconds;

    public bool IsRunning { get; private set; }

    public int DisplaySeconds => Mathf.CeilToInt(remainingSeconds);

    public int ElapsedWholeSeconds => Mathf.FloorToInt(elapsedSeconds);

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
        if (watchedMover != null && watchedMover.IsMovementComplete)
        {
            watchedMover = null;
            StartTimer();
        }

        if (!IsRunning)
            return;

        elapsedSeconds += Time.deltaTime;

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);

        NotifyIfSecondsChanged();
    }

    public void StartTimer()
    {
        remainingSeconds = timeLimitSeconds;
        elapsedSeconds = 0f;
        IsRunning = true;
        NotifyIfSecondsChanged();
    }

    public void StartTimerWhenMovementEnds(CharacterPathMover mover)
    {
        remainingSeconds = timeLimitSeconds;
        IsRunning = false;
        watchedMover = mover;
        NotifyIfSecondsChanged();
    }

    public void StopTimer()
    {
        watchedMover = null;
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
