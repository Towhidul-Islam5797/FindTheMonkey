using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelHint : MonoBehaviour
{
    // -------------------------------------------------------
    // REFERENCES
    // -------------------------------------------------------

    [Header("References")]
    [Tooltip("Assign the shared root LevelState. Found automatically if empty.")]
    [SerializeField]
    private LevelState levelState;

    [Tooltip("Assign the Hint button. Its click is connected by code; do not add an OnClick entry.")]
    [SerializeField]
    private Button hintButton;

    [Tooltip("Optional text that shows the hint cost.")]
    [SerializeField]
    private TMP_Text costText;

    // -------------------------------------------------------
    // HINT RULES
    // -------------------------------------------------------

    [Header("Hint Rules")]
    [Tooltip("Coins spent for one hint.")]
    [SerializeField, Min(0)]
    private int hintCost = 3;

    [Tooltip("How many hints the player can use in one level.")]
    [SerializeField, Min(1)]
    private int hintsPerLevel = 1;

    // -------------------------------------------------------
    // AD HINT
    // -------------------------------------------------------

    [Header("Ad Hint")]
    [Tooltip("Optional button that shows a rewarded ad for a free hint. Its click is connected by code; do not add an OnClick entry.")]
    [SerializeField]
    private Button adHintButton;

    [Tooltip("How many ad hints the player can use in one level. Counted separately from coin hints.")]
    [SerializeField, Min(1)]
    private int adHintsPerLevel = 1;

    [Tooltip("When ticked, the ad hint button appears only when the player has fewer coins than the hint cost.")]
    [SerializeField]
    private bool showAdHintOnlyWithoutCoins = false;

    // -------------------------------------------------------
    // PULSE
    // -------------------------------------------------------

    [Header("Pulse")]
    [Tooltip("How long the correct object pulses, in seconds.")]
    [SerializeField, Min(0.1f)]
    private float pulseSeconds = 1.5f;

    [Tooltip("Largest scale of the pulse. 1.15 means 15 percent bigger.")]
    [SerializeField, Min(1f)]
    private float pulseScale = 1.15f;

    [Tooltip("How many times the object grows and shrinks during the pulse.")]
    [SerializeField, Min(1)]
    private int pulseCount = 3;

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private CharacterPathMover characterPathMover;
    private LevelTimer levelTimer;
    private bool lastMovementComplete;
    private int hintsUsed;
    private int adHintsUsed;

    private RectTransform pulseTarget;
    private Vector3 pulseOriginalScale;
    private Coroutine pulseRoutine;

    private bool waitingForAd;
    private float timeScaleBeforeAd = 1f;

    // -------------------------------------------------------
    // UNITY
    // -------------------------------------------------------

    private void OnEnable()
    {
        ResolveReferences();

        if (levelState != null)
        {
            levelState.OnCoinChanged -= HandleCoinChanged;
            levelState.OnCoinChanged += HandleCoinChanged;

            levelState.OnMonkeyFound -= HandleStateChanged;
            levelState.OnMonkeyFound += HandleStateChanged;
        }

        if (levelTimer != null)
        {
            levelTimer.OnTimeUp -= HandleStateChanged;
            levelTimer.OnTimeUp += HandleStateChanged;
        }

        if (hintButton != null)
        {
            hintButton.onClick.RemoveListener(UseHint);
            hintButton.onClick.AddListener(UseHint);
        }

        if (adHintButton != null)
        {
            adHintButton.onClick.RemoveListener(UseAdHint);
            adHintButton.onClick.AddListener(UseAdHint);
        }

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnRewardedReadyChanged -= RefreshButton;
            AdsManager.Instance.OnRewardedReadyChanged += RefreshButton;
        }

        if (costText != null)
        {
            costText.text = hintCost.ToString();
        }

        RefreshButton();
    }

    private void OnDisable()
    {
        if (levelState != null)
        {
            levelState.OnCoinChanged -= HandleCoinChanged;
            levelState.OnMonkeyFound -= HandleStateChanged;
        }

        if (levelTimer != null)
        {
            levelTimer.OnTimeUp -= HandleStateChanged;
        }

        if (hintButton != null)
        {
            hintButton.onClick.RemoveListener(UseHint);
        }

        if (adHintButton != null)
        {
            adHintButton.onClick.RemoveListener(UseAdHint);
        }

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnRewardedReadyChanged -= RefreshButton;
        }

        if (waitingForAd)
        {
            waitingForAd = false;
            Time.timeScale = timeScaleBeforeAd;
        }

        StopPulse();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            RefreshButton();
        }
    }

    private void Update()
    {
        /*
         * Only the selected level root is active, so a new active
         * CharacterPathMover means a new level: the hint count resets.
         */
        if (characterPathMover == null ||
            !characterPathMover.gameObject.activeInHierarchy)
        {
            characterPathMover =
                FindFirstObjectByType<CharacterPathMover>();

            hintsUsed = 0;
            adHintsUsed = 0;
            lastMovementComplete = false;

            RefreshButton();
        }

        if (characterPathMover == null)
            return;

        if (characterPathMover.IsMovementComplete !=
            lastMovementComplete)
        {
            lastMovementComplete =
                characterPathMover.IsMovementComplete;

            RefreshButton();
        }
    }

    // -------------------------------------------------------
    // REFERENCES
    // -------------------------------------------------------

    private void ResolveReferences()
    {
        if (levelState == null)
        {
            levelState =
                FindFirstObjectByType<LevelState>();
        }

        if (levelTimer == null)
        {
            levelTimer =
                FindFirstObjectByType<LevelTimer>();
        }
    }

    // -------------------------------------------------------
    // RULES
    // -------------------------------------------------------

    private bool CanUseHint()
    {
        return CanGiveHint() &&
               hintsUsed < hintsPerLevel &&
               levelState.TotalCoins >= hintCost;
    }

    private bool CanUseAdHint()
    {
        if (!CanGiveHint())
            return false;

        if (adHintsUsed >= adHintsPerLevel)
            return false;

        if (AdsManager.Instance == null ||
            !AdsManager.Instance.IsRewardedReady)
            return false;

        return !showAdHintOnlyWithoutCoins ||
               levelState.TotalCoins < hintCost;
    }

    private bool CanGiveHint()
    {
        if (levelState == null ||
            characterPathMover == null)
            return false;

        if (!characterPathMover.IsMovementComplete)
            return false;

        if (levelState.IsMonkeyFound)
            return false;

        if (levelTimer != null &&
            levelTimer.IsTimeUp)
            return false;

        if (pulseRoutine != null)
            return false;

        return !waitingForAd;
    }

    private void RefreshButton()
    {
        if (hintButton != null)
        {
            hintButton.interactable = CanUseHint();
        }

        if (adHintButton != null)
        {
            adHintButton.gameObject.SetActive(CanUseAdHint());
        }
    }

    private void HandleCoinChanged(int totalCoins)
    {
        RefreshButton();
    }

    private void HandleStateChanged()
    {
        StopPulse();
        RefreshButton();
    }

    // -------------------------------------------------------
    // HINT
    // -------------------------------------------------------

    public void UseHint()
    {
        if (!CanUseHint())
            return;

        HidingObject correctObject = FindCorrectObject();

        if (correctObject == null)
        {
            DebugLog(
                "No hiding object matches the monkey's point. " +
                "No coins spent."
            );

            return;
        }

        if (!levelState.SpendCoins(hintCost))
            return;

        hintsUsed++;

        DebugLog(
            $"Hint used on '{correctObject.name}'. " +
            $"Cost={hintCost}, HintsUsed={hintsUsed}."
        );

        pulseRoutine = StartCoroutine(
            PulseRoutine(
                correctObject.transform as RectTransform
            )
        );

        RefreshButton();
    }

    public void UseAdHint()
    {
        if (!CanUseAdHint())
            return;

        if (FindCorrectObject() == null)
        {
            DebugLog(
                "No hiding object matches the monkey's point. " +
                "No ad shown."
            );

            return;
        }

        /*
         * On iOS the game keeps running under a full-screen ad,
         * so gameplay is paused until the ad closes.
         */
        timeScaleBeforeAd = Time.timeScale;
        Time.timeScale = 0f;
        waitingForAd = true;

        DebugLog("Ad hint requested.");

        RefreshButton();

        AdsManager.Instance.ShowRewardedThen(HandleAdFinished);
    }

    private void HandleAdFinished(bool rewardEarned)
    {
        if (this == null || !waitingForAd)
            return;

        waitingForAd = false;
        Time.timeScale = timeScaleBeforeAd;

        HidingObject correctObject =
            rewardEarned && CanGiveHint()
                ? FindCorrectObject()
                : null;

        if (correctObject == null)
        {
            DebugLog("Ad hint not rewarded.");

            RefreshButton();
            return;
        }

        adHintsUsed++;

        DebugLog(
            $"Ad hint rewarded on '{correctObject.name}'. " +
            $"AdHintsUsed={adHintsUsed}."
        );

        pulseRoutine = StartCoroutine(
            PulseRoutine(
                correctObject.transform as RectTransform
            )
        );

        RefreshButton();
    }

    private HidingObject FindCorrectObject()
    {
        RectTransform monkeyPoint =
            characterPathMover.CurrentArrivedPoint;

        if (monkeyPoint == null)
            return null;

        HidingObject[] hidingObjects =
            FindObjectsByType<HidingObject>(
                FindObjectsSortMode.None
            );

        for (int i = 0; i < hidingObjects.Length; i++)
        {
            if (hidingObjects[i].RelatedMovePoint == monkeyPoint)
                return hidingObjects[i];
        }

        return null;
    }

    // -------------------------------------------------------
    // PULSE
    // -------------------------------------------------------

    private IEnumerator PulseRoutine(RectTransform target)
    {
        if (target == null)
        {
            pulseRoutine = null;
            yield break;
        }

        pulseTarget = target;
        pulseOriginalScale = target.localScale;

        float elapsed = 0f;

        while (elapsed < pulseSeconds)
        {
            elapsed += Time.deltaTime;

            float wave = Mathf.Abs(
                Mathf.Sin(
                    elapsed / pulseSeconds *
                    pulseCount *
                    Mathf.PI
                )
            );

            target.localScale =
                pulseOriginalScale *
                Mathf.Lerp(1f, pulseScale, wave);

            yield return null;
        }

        target.localScale = pulseOriginalScale;

        pulseTarget = null;
        pulseRoutine = null;

        RefreshButton();
    }

    private void StopPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (pulseTarget != null)
        {
            pulseTarget.localScale = pulseOriginalScale;
            pulseTarget = null;
        }
    }

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    private void DebugLog(string message)
    {
        if (!showDebugLogs)
            return;

        Debug.Log(
            "[LevelHint] " +
            message,
            this
        );
    }
}
