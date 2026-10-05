using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HidingObject : MonoBehaviour, IPointerClickHandler
{
    // -------------------------------------------------------
    // LEVEL STATE
    // -------------------------------------------------------

    [Header("Level State")]
    [Tooltip(
        "Assign the same LevelState used by GameplayHUD " +
        "and LevelCompletePanelController."
    )]
    [SerializeField]
    private LevelState levelState;

    // -------------------------------------------------------
    // OBJECT MOVEMENT
    // -------------------------------------------------------

    [Header("Object Movement")]
    [Tooltip("Assign this hiding object's RectTransform.")]
    [SerializeField]
    private RectTransform objectRect;

    [Tooltip("Assign Tree_hide, Rock_hide or Box_hide.")]
    [SerializeField]
    private RectTransform moveReferencePoint;

    [SerializeField, Min(1f)]
    private float moveSpeed = 1000f;

    [SerializeField, Min(0f)]
    private float wrongReturnDelay = 1f;

    // -------------------------------------------------------
    // MONKEY CHECK
    // -------------------------------------------------------

    [Header("Monkey Check")]
    [Tooltip("Assign the Player containing CharacterPathMover.")]
    [SerializeField]
    private CharacterPathMover characterPathMover;

    [Tooltip(
        "Assign the MovePoint connected to this hiding object."
    )]
    [SerializeField]
    private RectTransform relatedMovePoint;

    // -------------------------------------------------------
    // FUTURE TRY COST
    // -------------------------------------------------------

    [Header("Try Cost - Future Currency")]
    [SerializeField]
    private bool useTryCost = false;

    [SerializeField, Min(0)]
    private int tryCost = 1;

    // -------------------------------------------------------
    // MONKEY REVEAL
    // -------------------------------------------------------

    [Header("Monkey Reveal")]
    [Tooltip("Assign Player/classic_monkey.")]
    [SerializeField]
    private GameObject monkeyObject;

    [Tooltip("Assign Player/classic_monkey RectTransform.")]
    [SerializeField]
    private RectTransform monkeyScaleTarget;

    [SerializeField]
    private Vector3 monkeyRevealScale = Vector3.one;

    // -------------------------------------------------------
    // UI RAYCAST
    // -------------------------------------------------------

    [Header("UI Raycast Fallback")]
    [SerializeField]
    private GraphicRaycaster graphicRaycaster;

    [SerializeField]
    private EventSystem eventSystem;

    [SerializeField]
    private bool useManualRaycastFallback = true;

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    // -------------------------------------------------------
    // RUNTIME STATE
    // -------------------------------------------------------

    /*
     * The return position is captured when the object is clicked.
     *
     * This is safer than capturing it in Awake because duplicated
     * levels may initially be inactive.
     */
    private Vector3 clickStartLocalPosition;
    private Vector3 clickStartWorldPosition;
    private Transform capturedParent;

    private bool hasCapturedStartPosition;
    private bool isMoving;
    private bool isRevealed;
    private bool clickHandledThisFrame;

    /*
     * GLOBAL MOVEMENT LOCK
     *
     * Every HidingObject instance shares this reference.
     * While one object is moving out / waiting / returning,
     * all other hiding objects ignore clicks.
     */
    private static HidingObject activeMovingObject;

    private bool IsAnotherObjectMoving
    {
        get
        {
            return activeMovingObject != null &&
                   activeMovingObject != this;
        }
    }

    // -------------------------------------------------------
    // UNITY
    // -------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetGlobalMovementLock()
    {
        activeMovingObject = null;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ValidateSetup();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        isMoving = false;
        clickHandledThisFrame = false;

        ReleaseMovementLock();
    }

    private void OnDestroy()
    {
        ReleaseMovementLock();
    }

    private void Update()
    {
        if (!useManualRaycastFallback)
            return;

        if (clickHandledThisFrame)
            return;

        /*
         * Another hiding object is currently moving.
         * Ignore all input until it has completely returned
         * or the monkey has been found.
         */
        if (IsAnotherObjectMoving)
            return;

        if (isMoving || isRevealed)
            return;

        if (levelState != null &&
            levelState.IsMonkeyFound)
        {
            return;
        }

        if (characterPathMover == null)
            return;

        if (!characterPathMover.IsMovementComplete)
            return;

        bool mouseClicked =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        if (mouseClicked)
        {
            Vector2 mousePosition =
                Mouse.current.position.ReadValue();

            CheckManualUIClick(mousePosition);
            return;
        }

        bool screenTouched =
            Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press
                .wasPressedThisFrame;

        if (screenTouched)
        {
            Vector2 touchPosition =
                Touchscreen.current.primaryTouch.position
                    .ReadValue();

            CheckManualUIClick(touchPosition);
        }
    }

    private void LateUpdate()
    {
        clickHandledThisFrame = false;
    }

    // -------------------------------------------------------
    // REFERENCES
    // -------------------------------------------------------

    private void ResolveReferences()
    {
        if (objectRect == null)
        {
            objectRect =
                GetComponent<RectTransform>();
        }

        if (levelState == null)
        {
#if UNITY_2023_1_OR_NEWER
            levelState =
                FindFirstObjectByType<LevelState>();
#else
            levelState =
                FindObjectOfType<LevelState>();
#endif
        }

        if (characterPathMover == null)
        {
#if UNITY_2023_1_OR_NEWER
            characterPathMover =
                FindFirstObjectByType<CharacterPathMover>();
#else
            characterPathMover =
                FindObjectOfType<CharacterPathMover>();
#endif
        }

        if (graphicRaycaster == null)
        {
            Canvas canvas =
                GetComponentInParent<Canvas>();

            if (canvas != null)
            {
                graphicRaycaster =
                    canvas.GetComponent<GraphicRaycaster>();
            }
        }

        if (eventSystem == null)
        {
            eventSystem =
                EventSystem.current;
        }
    }

    // -------------------------------------------------------
    // POINTER CLICK
    // -------------------------------------------------------

    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        if (clickHandledThisFrame)
            return;

        clickHandledThisFrame = true;

        DebugLog(
            "Click detected by IPointerClickHandler."
        );

        TryReveal();
    }

    private void CheckManualUIClick(
        Vector2 screenPosition
    )
    {
        if (clickHandledThisFrame)
            return;

        if (graphicRaycaster == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "GraphicRaycaster is missing.",
                this
            );

            return;
        }

        if (eventSystem == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "EventSystem is missing.",
                this
            );

            return;
        }

        PointerEventData pointerData =
            new PointerEventData(eventSystem)
            {
                position = screenPosition
            };

        List<RaycastResult> results =
            new List<RaycastResult>();

        graphicRaycaster.Raycast(
            pointerData,
            results
        );

        for (int i = 0;
             i < results.Count;
             i++)
        {
            GameObject hitObject =
                results[i].gameObject;

            if (hitObject == null)
                continue;

            bool hitThisObject =
                hitObject == gameObject ||
                hitObject.transform.IsChildOf(transform);

            if (!hitThisObject)
                continue;

            clickHandledThisFrame = true;

            DebugLog(
                $"Click detected by manual raycast. " +
                $"Hit object: {hitObject.name}."
            );

            TryReveal();
            return;
        }
    }

    // -------------------------------------------------------
    // TRY REVEAL
    // -------------------------------------------------------

    private void TryReveal()
    {
        DebugLog(
            $"TryReveal called. " +
            $"isMoving={isMoving}, " +
            $"isRevealed={isRevealed}, " +
            $"anotherObjectMoving={IsAnotherObjectMoving}."
        );

        if (isMoving || isRevealed)
            return;

        /*
         * Do not allow a second hiding object to start
         * while another object is still moving.
         */
        if (IsAnotherObjectMoving)
        {
            DebugLog(
                $"Click ignored because " +
                $"'{activeMovingObject.name}' is still moving."
            );

            return;
        }

        ResolveReferences();

        if (!ValidateBeforeMovement())
            return;

        /*
         * Acquire the shared movement lock BEFORE starting
         * the coroutine. This makes the lock immediate, so
         * another object cannot start during the same frame.
         */
        if (!TryAcquireMovementLock())
        {
            DebugLog(
                "Click ignored because another hiding " +
                "object acquired the movement lock first."
            );

            return;
        }

        if (useTryCost)
        {
            DebugLog(
                $"Try cost is enabled. " +
                $"Future cost={tryCost}."
            );
        }

        /*
         * Capture the current position immediately before moving.
         * This is the exact position the wrong object returns to.
         */
        CaptureClickStartPosition();

        StartCoroutine(
            MoveObjectRoutine()
        );
    }

    private bool TryAcquireMovementLock()
    {
        /*
         * Unity's destroyed-object null handling means this also
         * safely recovers if an old scene object was destroyed.
         */
        if (activeMovingObject != null &&
            activeMovingObject != this)
        {
            return false;
        }

        activeMovingObject = this;

        DebugLog(
            "Global hiding-object movement lock acquired."
        );

        return true;
    }

    private void ReleaseMovementLock()
    {
        if (activeMovingObject != this)
            return;

        activeMovingObject = null;

        DebugLog(
            "Global hiding-object movement lock released."
        );
    }

    private bool ValidateBeforeMovement()
    {
        if (levelState == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "LevelState is missing.",
                this
            );

            return false;
        }

        if (levelState.IsMonkeyFound)
        {
            DebugLog(
                "Click ignored because the monkey " +
                "was already found."
            );

            return false;
        }

        if (characterPathMover == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "CharacterPathMover is missing.",
                this
            );

            return false;
        }

        if (!characterPathMover.IsMovementComplete)
        {
            DebugLog(
                "Cannot reveal yet. Monkey movement " +
                "is not complete."
            );

            return false;
        }

        if (objectRect == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "Object RectTransform is missing.",
                this
            );

            return false;
        }

        if (objectRect.parent == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "The object has no parent Transform.",
                this
            );

            return false;
        }

        if (moveReferencePoint == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "Move Reference Point is missing.",
                this
            );

            return false;
        }

        if (relatedMovePoint == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "Related Move Point is missing.",
                this
            );

            return false;
        }

        return true;
    }

    private void CaptureClickStartPosition()
    {
        capturedParent =
            objectRect.parent;

        clickStartLocalPosition =
            objectRect.localPosition;

        clickStartWorldPosition =
            objectRect.position;

        hasCapturedStartPosition = true;

        DebugLog(
            $"Click start position captured. " +
            $"Local={clickStartLocalPosition}, " +
            $"World={clickStartWorldPosition}, " +
            $"Parent={capturedParent.name}."
        );
    }

    // -------------------------------------------------------
    // MOVEMENT ROUTINE
    // -------------------------------------------------------

    private IEnumerator MoveObjectRoutine()
    {
        isMoving = true;

        DebugLog(
            $"Move routine started. " +
            $"LevelState Instance ID=" +
            $"{gameObject.GetEntityId()}."
        );

        levelState.RegisterTry();

        DebugLog(
            $"Try registered. " +
            $"TryCount={levelState.TryCount}."
        );

        DebugLog(
            $"Moving object to " +
            $"'{moveReferencePoint.name}'."
        );

        DebugLog(
            "Monkey arrived point: " +
            (
                characterPathMover.CurrentArrivedPoint != null
                    ? characterPathMover
                        .CurrentArrivedPoint.name
                    : "NULL"
            )
        );

        DebugLog(
            $"Related move point: " +
            $"{relatedMovePoint.name}."
        );

        yield return MoveToReferencePoint();

        bool isCorrectObject =
            characterPathMover.CurrentArrivedPoint ==
            relatedMovePoint;

        DebugLog(
            $"Correct-object comparison result: " +
            $"{isCorrectObject}."
        );

        if (isCorrectObject)
        {
            RevealMonkey();
        }
        else
        {
            /*
             * The movement lock stays active during:
             *
             * 1. Move out
             * 2. Wrong return delay
             * 3. Full movement back to the start position
             *
             * Only after the object is completely back can
             * another hiding object be clicked.
             */
            yield return ReturnWrongObject();
        }

        isMoving = false;

        ReleaseMovementLock();
    }

    private IEnumerator MoveToReferencePoint()
    {
        if (objectRect == null ||
            moveReferencePoint == null)
        {
            yield break;
        }

        Transform currentParent =
            objectRect.parent;

        if (currentParent == null)
        {
            yield return MoveToWorldPosition(
                moveReferencePoint.position
            );

            yield break;
        }

        /*
         * Convert the reference point's world position into
         * this object's parent-local coordinate system.
         *
         * This avoids UI Canvas and nested-parent position issues.
         */
        Vector3 targetLocalPosition =
            currentParent.InverseTransformPoint(
                moveReferencePoint.position
            );

        DebugLog(
            $"Reference target converted. " +
            $"World={moveReferencePoint.position}, " +
            $"Local={targetLocalPosition}."
        );

        yield return MoveToLocalPosition(
            targetLocalPosition
        );

        objectRect.localPosition =
            targetLocalPosition;
    }

    private IEnumerator ReturnWrongObject()
    {
        if (!hasCapturedStartPosition)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "Cannot return because the click start " +
                "position was not captured.",
                this
            );

            yield break;
        }

        DebugLog(
            $"Wrong object. Waiting " +
            $"{wrongReturnDelay} second(s). " +
            $"ReturnLocal={clickStartLocalPosition}, " +
            $"ReturnWorld={clickStartWorldPosition}."
        );

        if (wrongReturnDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                wrongReturnDelay
            );
        }

        /*
         * Normally the object still has the same parent,
         * so localPosition is the most reliable return value.
         */
        if (objectRect.parent == capturedParent)
        {
            yield return MoveToLocalPosition(
                clickStartLocalPosition
            );

            objectRect.localPosition =
                clickStartLocalPosition;
        }
        else
        {
            /*
             * Fallback in case another script changed the parent.
             */
            Debug.LogWarning(
                $"[HidingObject:{name}] " +
                "Object parent changed while moving. " +
                "Returning using world position.",
                this
            );

            yield return MoveToWorldPosition(
                clickStartWorldPosition
            );

            objectRect.position =
                clickStartWorldPosition;
        }

        /*
         * Force exact final position after the movement loop.
         */
        if (objectRect.parent == capturedParent)
        {
            objectRect.localPosition =
                clickStartLocalPosition;
        }
        else
        {
            objectRect.position =
                clickStartWorldPosition;
        }

        DebugLog(
            $"Wrong object returned successfully. " +
            $"CurrentLocal={objectRect.localPosition}, " +
            $"TargetLocal={clickStartLocalPosition}, " +
            $"CurrentWorld={objectRect.position}."
        );
    }

    // -------------------------------------------------------
    // MOVEMENT HELPERS
    // -------------------------------------------------------

    private IEnumerator MoveToLocalPosition(
        Vector3 targetLocalPosition
    )
    {
        if (objectRect == null)
            yield break;

        while (
            Vector3.Distance(
                objectRect.localPosition,
                targetLocalPosition
            ) > 0.5f
        )
        {
            objectRect.localPosition =
                Vector3.MoveTowards(
                    objectRect.localPosition,
                    targetLocalPosition,
                    moveSpeed *
                    Time.unscaledDeltaTime
                );

            yield return null;
        }

        objectRect.localPosition =
            targetLocalPosition;
    }

    private IEnumerator MoveToWorldPosition(
        Vector3 targetWorldPosition
    )
    {
        if (objectRect == null)
            yield break;

        while (
            Vector3.Distance(
                objectRect.position,
                targetWorldPosition
            ) > 0.5f
        )
        {
            objectRect.position =
                Vector3.MoveTowards(
                    objectRect.position,
                    targetWorldPosition,
                    moveSpeed *
                    Time.unscaledDeltaTime
                );

            yield return null;
        }

        objectRect.position =
            targetWorldPosition;
    }

    // -------------------------------------------------------
    // MONKEY REVEAL
    // -------------------------------------------------------

    private void RevealMonkey()
    {
        DebugLog(
            "RevealMonkey called."
        );

        if (isRevealed)
        {
            Debug.LogWarning(
                $"[HidingObject:{name}] " +
                "RevealMonkey ignored because this object " +
                "was already revealed.",
                this
            );

            return;
        }

        if (levelState == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "Cannot reveal because LevelState is missing.",
                this
            );

            return;
        }

        Debug.Log(
            $"[HidingObject:{name}] " +
            $"Calling MarkMonkeyFound on LevelState " +
            $"'{levelState.gameObject.name}', " +
            $"Instance ID={gameObject.GetEntityId()}.",
            this
        );

        /*
         * This calculates rewards and triggers
         * LevelCompletePanelController.
         */
        levelState.MarkMonkeyFound();

        Debug.Log(
            $"[HidingObject:{name}] " +
            $"MarkMonkeyFound finished. " +
            $"IsMonkeyFound={levelState.IsMonkeyFound}.",
            this
        );

        isRevealed = true;

        if (monkeyObject != null)
        {
            monkeyObject.SetActive(true);
        }

        if (monkeyScaleTarget != null)
        {
            monkeyScaleTarget.localScale =
                monkeyRevealScale;
        }

        if (characterPathMover != null)
        {
            characterPathMover.ForceRevealScale(
                monkeyRevealScale
            );
        }

        StartCoroutine(
            ForceRevealScaleNextFrame()
        );

        DebugLog(
            $"Correct object found. " +
            $"Monkey scale reset to " +
            $"{monkeyRevealScale}."
        );
    }

    private IEnumerator ForceRevealScaleNextFrame()
    {
        yield return null;

        if (monkeyScaleTarget != null)
        {
            monkeyScaleTarget.localScale =
                monkeyRevealScale;
        }

        if (characterPathMover != null)
        {
            characterPathMover.ForceRevealScale(
                monkeyRevealScale
            );
        }
    }

    // -------------------------------------------------------
    // VALIDATION
    // -------------------------------------------------------

    private void ValidateSetup()
    {
        if (!showDebugLogs)
            return;

        Debug.Log(
            $"========== HIDING OBJECT VALIDATION: " +
            $"{name} ==========",
            this
        );

        if (levelState == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "LevelState: MISSING",
                this
            );
        }
        else
        {
            Debug.Log(
                $"[HidingObject:{name}] " +
                $"LevelState: OK. " +
                $"Object='{levelState.gameObject.name}', " +
                $"Instance ID={gameObject.GetEntityId()}.",
                levelState
            );
        }

        if (objectRect == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "Object Rect: MISSING",
                this
            );
        }
        else
        {
            Debug.Log(
                $"[HidingObject:{name}] " +
                $"Object Rect: {objectRect.name}",
                objectRect
            );
        }

        if (moveReferencePoint == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "Move Reference Point: MISSING",
                this
            );
        }
        else
        {
            Debug.Log(
                $"[HidingObject:{name}] " +
                $"Move Reference Point: " +
                $"{moveReferencePoint.name}",
                moveReferencePoint
            );
        }

        if (characterPathMover == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "CharacterPathMover: MISSING",
                this
            );
        }
        else
        {
            Debug.Log(
                $"[HidingObject:{name}] " +
                $"CharacterPathMover: " +
                $"{characterPathMover.name}",
                characterPathMover
            );
        }

        if (relatedMovePoint == null)
        {
            Debug.LogError(
                $"[HidingObject:{name}] " +
                "Related Move Point: MISSING",
                this
            );
        }
        else
        {
            Debug.Log(
                $"[HidingObject:{name}] " +
                $"Related Move Point: " +
                $"{relatedMovePoint.name}",
                relatedMovePoint
            );
        }

        Debug.Log(
            "=====================================================",
            this
        );
    }

    [ContextMenu("DEBUG - Validate Hiding Object")]
    private void DebugValidate()
    {
        ResolveReferences();
        ValidateSetup();
    }

    [ContextMenu("DEBUG - Print Current Position")]
    private void DebugPrintCurrentPosition()
    {
        ResolveReferences();

        if (objectRect == null)
            return;

        Debug.Log(
            $"[HidingObject:{name}] " +
            $"LocalPosition={objectRect.localPosition}, " +
            $"WorldPosition={objectRect.position}, " +
            $"Parent=" +
            $"{(objectRect.parent != null ? objectRect.parent.name : "NULL")}.",
            this
        );
    }

    private void DebugLog(
        string message
    )
    {
        if (!showDebugLogs)
            return;

        Debug.Log(
            $"[HidingObject:{name}] {message}",
            this
        );
    }
}