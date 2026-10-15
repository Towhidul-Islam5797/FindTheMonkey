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
    [Tooltip("Default time for every level, in seconds. A LevelSettings on the active level overrides it.")]
    [SerializeField, Min(1f)]
    private float timeLimitSeconds = 60f;

    private float remainingSeconds;
    private int lastShownSeconds = -1;
    private CharacterPathMover watchedMover;
    private float elapsedSeconds;

    public bool IsRunning { get; private set; }

    public bool IsTimeUp { get; private set; }

    public int DisplaySeconds => Mathf.CeilToInt(remainingSeconds);

    public int ElapsedWholeSeconds => Mathf.FloorToInt(elapsedSeconds);

    public event Action<int> OnSecondsChanged;

    public event Action OnTimeUp;

    private float CurrentTimeLimit
    {
        get
        {
            if (LevelSettings.Current != null &&
                LevelSettings.Current.TimeLimitSeconds > 0f)
            {
                return LevelSettings.Current.TimeLimitSeconds;
            }

            return timeLimitSeconds;
        }
    }

    private bool ShouldEndLevelAtZero =>
        LevelSettings.Current != null &&
        LevelSettings.Current.EndLevelWhenTimeRunsOut;

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

    private void Start()
    {
        remainingSeconds = CurrentTimeLimit;
        NotifyIfSecondsChanged();
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

        if (remainingSeconds <= 0f &&
            !IsTimeUp &&
            ShouldEndLevelAtZero)
        {
            IsTimeUp = true;
            IsRunning = false;
            OnTimeUp?.Invoke();
        }
    }

    public void StartTimer()
    {
        remainingSeconds = CurrentTimeLimit;
        elapsedSeconds = 0f;
        IsRunning = true;
        IsTimeUp = false;
        NotifyIfSecondsChanged();
    }

    public void StartTimerWhenMovementEnds(CharacterPathMover mover)
    {
        remainingSeconds = CurrentTimeLimit;
        IsRunning = false;
        IsTimeUp = false;
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
