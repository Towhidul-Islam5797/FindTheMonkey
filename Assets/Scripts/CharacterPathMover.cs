using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class CharacterPathMover : MonoBehaviour
{
    [System.Serializable]
    public class MovePointData
    {
        public RectTransform movePoint;
        public RectTransform[] arrivalTriggers;
    }

    public bool IsMovementComplete { get; private set; }
    public bool IsCurrentlyMoving { get; private set; }
    public RectTransform CurrentArrivedPoint { get; private set; }
    public RectTransform CurrentTargetPoint { get; private set; }

    [Header("Player UI References")]
    [SerializeField] private RectTransform playerRect;
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private Image targetImage;

    [Header("Path Points")]
    [SerializeField] private MovePointData[] movePoints;

    [Header("Movement")]
    [SerializeField] private float startSpeed = 1000f;
    [SerializeField] private float speedIncreasePerCycle = 0f;
    [SerializeField] private int totalCycles = 1;
    [SerializeField] private float stopDistance = 8f;
    [SerializeField] private float waitAtPoint = 0.3f;

    [Header("Arrival Layer Detection")]
    [SerializeField] private float arrivalTriggerDistance = 120f;
    [SerializeField] private bool applyArrivalStateOnTrigger = true;
    [SerializeField] private bool alsoCheckMovePointDistance = true;

    [Header("Movement Order")]
    [SerializeField] private bool randomizeMovePoints = false;

    [Header("Stop Point")]
    [SerializeField] private bool stopAtSpecificPoint = false;
    [SerializeField] private RectTransform stopMovePoint;
    [SerializeField] private bool stopOnlyAfterAllCycles = true;

    [Header("Scale")]
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 movingScale = Vector3.one;
    [SerializeField] private Vector3 arrivedScale = new Vector3(0.85f, 0.85f, 1f);

    [Header("UI Layering")]
    [SerializeField] private RectTransform hideObjectGroup;

    [Header("UI Click Blocking")]
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private bool ignoreClicksOnUI = true;

    [Header("Arrival Smoke VFX")]
    [SerializeField] private GameObject smokePrefab;
    [SerializeField] private RectTransform smokeParent;
    [SerializeField] private Vector3 smokeOffset = Vector3.zero;
    [SerializeField] private float smokeDestroyDelay = 1f;
    [SerializeField] private bool spawnSmokeOnPointArrival = false;
    [SerializeField] private bool spawnSmokeOnTrigger = false;

    [Header("Hide Character")]
    [SerializeField] private bool hideCharacterBriefly = false;
    [SerializeField] private float hideDuration = 0.08f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleBoolName = "IsIdle";
    [SerializeField] private string movingBoolName = "IsMoving";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool hasStarted;
    private float currentSpeed;
    private bool arrivalStateAppliedForCurrentPoint;

    private void Awake()
    {
        if (playerRect == null)
            playerRect = GetComponent<RectTransform>();

        if (scaleTarget == null)
            scaleTarget = playerRect;

        if (graphicRaycaster == null)
            graphicRaycaster = GetComponentInParent<Canvas>()?.GetComponent<GraphicRaycaster>();
    }

    private void Start()
    {
        IsMovementComplete = false;
        IsCurrentlyMoving = false;
        CurrentArrivedPoint = null;
        CurrentTargetPoint = null;

        currentSpeed = startSpeed;

        ApplyStartState();
        PlayIdle();
    }

    /// <summary>
    /// Starts this Player's movement.
    /// Called automatically only for the Player inside the active level.
    /// </summary>
    public void StartGame()
    {
        if (hasStarted)
        {
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning(
                $"[CharacterPathMover] Cannot start inactive player: " +
                $"{gameObject.name}",
                this
            );

            return;
        }

        hasStarted = true;

        IsMovementComplete = false;
        IsCurrentlyMoving = false;
        CurrentArrivedPoint = null;
        CurrentTargetPoint = null;

        currentSpeed = startSpeed;

        ApplyStartState();
        PlayIdle();

        if (showDebugLogs)
        {
            Debug.Log(
                $"[CharacterPathMover] StartGame called for: " +
                $"{gameObject.name}",
                this
            );
        }

        StartCoroutine(MoveRoutine());
    }

    private Vector2 GetPointerScreenPosition()
    {
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Vector2.zero;
    }

    private bool IsPointerOverBlockingUI(Vector2 screenPosition)
    {
        if (graphicRaycaster == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas != null)
                graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        }

        if (graphicRaycaster == null || EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);

        if (results.Count == 0)
            return false;

        for (int i = 0; i < results.Count; i++)
        {
            GameObject hitObject = results[i].gameObject;

            if (hitObject == null)
                continue;

            if (hitObject.transform == playerRect || hitObject.transform.IsChildOf(playerRect))
                continue;

            if (hideObjectGroup != null &&
                (hitObject.transform == hideObjectGroup || hitObject.transform.IsChildOf(hideObjectGroup)))
                continue;

            if (hitObject.GetComponentInParent<Button>() != null)
                return true;

            if (hitObject.GetComponentInParent<MenuWindowController>() != null)
                return true;

            if (hitObject.name.Contains("Menu") ||
                hitObject.name.Contains("HUD") ||
                hitObject.name.Contains("Coin") ||
                hitObject.name.Contains("Banana"))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator MoveRoutine()
    {
        if (showDebugLogs)
            Debug.Log("MoveRoutine started.");

        if (playerRect == null)
        {
            Debug.LogError("Player Rect is NULL.");
            yield break;
        }

        if (movePoints == null || movePoints.Length == 0)
        {
            Debug.LogError("MovePoints array is empty.");
            IsMovementComplete = true;
            IsCurrentlyMoving = false;
            CurrentTargetPoint = null;
            yield break;
        }

        for (int cycle = 0; cycle < totalCycles; cycle++)
        {
            currentSpeed = startSpeed + (speedIncreasePerCycle * cycle);

            List<MovePointData> currentCyclePoints = GetCurrentCyclePoints();

            for (int i = 0; i < currentCyclePoints.Count; i++)
            {
                MovePointData pointData = currentCyclePoints[i];

                if (pointData == null || pointData.movePoint == null)
                {
                    Debug.LogError("Move point is missing in Move Points array.");
                    continue;
                }

                yield return MoveToPoint(pointData);

                if (spawnSmokeOnPointArrival)
                    SpawnArrivalSmoke();

                if (hideCharacterBriefly)
                    StartCoroutine(HideCharacterBriefly());

                ApplyArrivedVisualState();
                PlayIdle();

                bool isLastCycle = cycle == totalCycles - 1;

                if (stopAtSpecificPoint && pointData.movePoint == stopMovePoint)
                {
                    if (!stopOnlyAfterAllCycles || isLastCycle)
                    {
                        ApplyFinalCompleteState();

                        IsMovementComplete = true;
                        IsCurrentlyMoving = false;
                        CurrentTargetPoint = null;

                        if (showDebugLogs)
                            Debug.Log("Stopped at specific UI point: " + pointData.movePoint.name);

                        yield break;
                    }
                }

                yield return new WaitForSeconds(waitAtPoint);
            }
        }

        ApplyFinalCompleteState();

        IsCurrentlyMoving = false;
        IsMovementComplete = true;
        CurrentTargetPoint = null;

        if (showDebugLogs)
            Debug.Log("Movement complete. Player locked behind final hide object.");
    }

    private List<MovePointData> GetCurrentCyclePoints()
    {
        List<MovePointData> points = new List<MovePointData>(movePoints);

        if (randomizeMovePoints)
        {
            for (int i = 0; i < points.Count; i++)
            {
                int randomIndex = Random.Range(i, points.Count);

                MovePointData temp = points[i];
                points[i] = points[randomIndex];
                points[randomIndex] = temp;
            }
        }

        return points;
    }

    private IEnumerator MoveToPoint(MovePointData pointData)
    {
        RectTransform targetPoint = pointData.movePoint;

        if (showDebugLogs)
        {
            Debug.Log("Moving to: " + targetPoint.name);
            Debug.Log("Player anchored position before move: " + playerRect.anchoredPosition);
            Debug.Log("Target anchored position: " + targetPoint.anchoredPosition);
            Debug.Log("Distance: " + Vector2.Distance(playerRect.anchoredPosition, targetPoint.anchoredPosition));
        }

        CurrentTargetPoint = targetPoint;
        IsCurrentlyMoving = true;
        arrivalStateAppliedForCurrentPoint = false;

        ApplyMovingState();
        PlayMove();

        while (Vector2.Distance(playerRect.anchoredPosition, targetPoint.anchoredPosition) > stopDistance)
        {
            Vector2 direction = targetPoint.anchoredPosition - playerRect.anchoredPosition;

            if (Mathf.Abs(direction.x) > 0.01f && scaleTarget != null)
            {
                Vector3 scale = scaleTarget.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
                scaleTarget.localScale = scale;
            }

            playerRect.anchoredPosition = Vector2.MoveTowards(
                playerRect.anchoredPosition,
                targetPoint.anchoredPosition,
                currentSpeed * Time.deltaTime
            );

            if (applyArrivalStateOnTrigger && !arrivalStateAppliedForCurrentPoint)
            {
                if (ShouldApplyArrivalLayer(pointData))
                {
                    ApplyArrivedVisualState();

                    if (spawnSmokeOnTrigger)
                        SpawnArrivalSmoke();

                    arrivalStateAppliedForCurrentPoint = true;

                    if (showDebugLogs)
                        Debug.Log("Arrival layer applied for: " + targetPoint.name);
                }
            }

            yield return null;
        }

        playerRect.anchoredPosition = targetPoint.anchoredPosition;
        CurrentArrivedPoint = targetPoint;

        IsCurrentlyMoving = false;
        CurrentTargetPoint = null;

        ApplyArrivedVisualState();

        if (showDebugLogs)
            Debug.Log("Arrived at: " + targetPoint.name);
    }

    private bool ShouldApplyArrivalLayer(MovePointData pointData)
    {
        if (playerRect == null || pointData == null || pointData.movePoint == null)
            return false;

        if (alsoCheckMovePointDistance)
        {
            float movePointDistance = Vector2.Distance(
                playerRect.anchoredPosition,
                pointData.movePoint.anchoredPosition
            );

            if (showDebugLogs)
            {
                Debug.Log("Checking target MovePoint: "
                          + pointData.movePoint.name
                          + " Distance: "
                          + movePointDistance);
            }

            if (movePointDistance <= arrivalTriggerDistance)
            {
                if (showDebugLogs)
                    Debug.Log("ARRIVAL LAYER HIT BY MOVEPOINT: " + pointData.movePoint.name);

                return true;
            }
        }

        if (pointData.arrivalTriggers == null || pointData.arrivalTriggers.Length == 0)
            return false;

        for (int i = 0; i < pointData.arrivalTriggers.Length; i++)
        {
            RectTransform trigger = pointData.arrivalTriggers[i];

            if (trigger == null)
                continue;

            float triggerDistance = Vector3.Distance(playerRect.position, trigger.position);

            if (showDebugLogs)
            {
                Debug.Log("Checking trigger: "
                          + trigger.name
                          + " Distance: "
                          + triggerDistance);
            }

            if (triggerDistance <= arrivalTriggerDistance)
            {
                if (showDebugLogs)
                    Debug.Log("ARRIVAL LAYER HIT BY TRIGGER: " + trigger.name);

                return true;
            }
        }

        return false;
    }

    public void SpawnArrivalSmoke()
    {
        if (smokePrefab == null || playerRect == null) return;

        Transform parent = smokeParent != null ? smokeParent : playerRect.parent;

        GameObject smoke = Instantiate(smokePrefab, parent);

        RectTransform smokeRect = smoke.GetComponent<RectTransform>();

        if (smokeRect != null)
        {
            smokeRect.position = playerRect.position + smokeOffset;
            smokeRect.localScale = Vector3.one;
            smokeRect.rotation = Quaternion.identity;
        }
        else
        {
            smoke.transform.position = playerRect.position + smokeOffset;
        }

        Destroy(smoke, smokeDestroyDelay);
    }

    private IEnumerator HideCharacterBriefly()
    {
        if (targetImage == null) yield break;

        targetImage.enabled = false;
        yield return new WaitForSeconds(hideDuration);
        targetImage.enabled = true;
    }

    private void ApplyStartState()
    {
        if (scaleTarget != null)
            scaleTarget.localScale = startScale;

        PutPlayerInFrontOfHideObjects();
    }

    private void ApplyMovingState()
    {
        if (scaleTarget != null)
            scaleTarget.localScale = movingScale;

        PutPlayerInFrontOfHideObjects();
    }

    public void ApplyArrivedVisualState()
    {
        if (scaleTarget != null)
            scaleTarget.localScale = arrivedScale;

        PutPlayerBehindHideObjects();
    }

    private void ApplyFinalCompleteState()
    {
        if (scaleTarget != null)
            scaleTarget.localScale = arrivedScale;

        PutPlayerBehindHideObjects();
        PlayIdle();

        if (showDebugLogs)
            Debug.Log("Final complete state applied. Player should be behind Hide_Object.");
    }

    private void PutPlayerInFrontOfHideObjects()
    {
        if (playerRect == null || hideObjectGroup == null)
            return;

        if (playerRect.parent != hideObjectGroup.parent)
        {
            Debug.LogWarning("Player and Hide_Object must have the same parent Canvas.");
            return;
        }

        int playerIndex = playerRect.GetSiblingIndex();
        int hideIndex = hideObjectGroup.GetSiblingIndex();

        if (playerIndex > hideIndex)
        {
            if (showDebugLogs)
            {
                Debug.Log("Player already IN FRONT. Player index: "
                          + playerRect.GetSiblingIndex()
                          + " Hide_Object index: "
                          + hideObjectGroup.GetSiblingIndex());
            }

            return;
        }

        playerRect.SetSiblingIndex(hideIndex);

        if (showDebugLogs)
        {
            Debug.Log("Player placed IN FRONT of Hide_Object. Player index: "
                      + playerRect.GetSiblingIndex()
                      + " Hide_Object index: "
                      + hideObjectGroup.GetSiblingIndex());
        }
    }

    private void PutPlayerBehindHideObjects()
    {
        if (playerRect == null || hideObjectGroup == null)
            return;

        if (playerRect.parent != hideObjectGroup.parent)
        {
            Debug.LogWarning("Player and Hide_Object must have the same parent Canvas.");
            return;
        }

        int playerIndex = playerRect.GetSiblingIndex();
        int hideIndex = hideObjectGroup.GetSiblingIndex();

        if (playerIndex < hideIndex)
        {
            if (showDebugLogs)
            {
                Debug.Log("Player already BEHIND. Player index: "
                          + playerRect.GetSiblingIndex()
                          + " Hide_Object index: "
                          + hideObjectGroup.GetSiblingIndex());
            }

            return;
        }

        playerRect.SetSiblingIndex(hideIndex);

        if (showDebugLogs)
        {
            Debug.Log("Player placed BEHIND Hide_Object. Player index: "
                      + playerRect.GetSiblingIndex()
                      + " Hide_Object index: "
                      + hideObjectGroup.GetSiblingIndex());
        }
    }

    public void ForceRevealScale(Vector3 revealScale)
    {
        if (scaleTarget != null)
            scaleTarget.localScale = revealScale;

        if (playerRect != null)
            playerRect.localScale = Vector3.one;
    }

    private void PlayIdle()
    {
        if (animator == null) return;

        animator.SetBool(idleBoolName, true);
        animator.SetBool(movingBoolName, false);
    }

    private void PlayMove()
    {
        if (animator == null) return;

        animator.SetBool(idleBoolName, false);
        animator.SetBool(movingBoolName, true);
    }
}