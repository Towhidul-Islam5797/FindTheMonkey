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

    private RectTransform pulseTarget;
    private Vector3 pulseOriginalScale;
    private Coroutine pulseRoutine;

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

        StopPulse();
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

        if (hintsUsed >= hintsPerLevel)
            return false;

        if (pulseRoutine != null)
            return false;

        return levelState.TotalCoins >= hintCost;
    }

    private void RefreshButton()
    {
        if (hintButton == null)
            return;

        hintButton.interactable = CanUseHint();
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
