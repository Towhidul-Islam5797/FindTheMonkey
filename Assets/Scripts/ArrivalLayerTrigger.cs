using UnityEngine;

public class ArrivalLayerTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterPathMover characterPathMover;
    [SerializeField] private Transform relatedMovePoint;

    [Header("Options")]
    [SerializeField] private bool spawnSmokeOnTrigger = true;
    [SerializeField] private bool showDebugLog = false;

    private Transform lastTriggeredTarget;

    private void Awake()
    {
        if (relatedMovePoint == null && transform.parent != null)
        {
            relatedMovePoint = transform.parent;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyArrivedVisual(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyArrivedVisual(other);
    }

    private void TryApplyArrivedVisual(Collider2D other)
    {
        if (characterPathMover == null) return;
        if (relatedMovePoint == null) return;

        if (!characterPathMover.IsCurrentlyMoving)
            return;

        if (characterPathMover.CurrentTargetPoint != relatedMovePoint)
            return;

        bool isPlayer =
            other.transform == characterPathMover.transform ||
            other.transform.IsChildOf(characterPathMover.transform);

        if (!isPlayer) return;

        characterPathMover.ApplyArrivedVisualState();

        if (spawnSmokeOnTrigger && lastTriggeredTarget != relatedMovePoint)
        {
            characterPathMover.SpawnArrivalSmoke();
            lastTriggeredTarget = relatedMovePoint;
        }

        if (showDebugLog)
            Debug.Log($"{name}: Arrival trigger applied for {relatedMovePoint.name}");
    }
}