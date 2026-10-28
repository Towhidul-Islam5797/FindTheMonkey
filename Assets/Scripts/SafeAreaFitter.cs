using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Vector2 originalPosition;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private float lastScaleFactor;

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        originalPosition =
            rectTransform.anchoredPosition;

        Canvas parentCanvas =
            GetComponentInParent<Canvas>(true);

        if (parentCanvas != null)
        {
            rootCanvas =
                parentCanvas.rootCanvas;
        }
    }

    private void Update()
    {
        if (rootCanvas == null ||
            rootCanvas.scaleFactor <= 0f)
        {
            return;
        }

        Vector2Int screenSize =
            new Vector2Int(Screen.width, Screen.height);

        if (Screen.safeArea == lastSafeArea &&
            screenSize == lastScreenSize &&
            Mathf.Approximately(rootCanvas.scaleFactor, lastScaleFactor))
        {
            return;
        }

        lastSafeArea = Screen.safeArea;
        lastScreenSize = screenSize;
        lastScaleFactor = rootCanvas.scaleFactor;

        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        /*
         * Moves this element away from the screen edge it is anchored to,
         * by that edge's safe area inset (notch, camera hole, home bar).
         * Centered or stretched axes are not moved.
         */
        Rect safeArea = Screen.safeArea;
        float scale = rootCanvas.scaleFactor;

        float leftInset = safeArea.xMin / scale;
        float rightInset = (Screen.width - safeArea.xMax) / scale;
        float bottomInset = safeArea.yMin / scale;
        float topInset = (Screen.height - safeArea.yMax) / scale;

        Vector2 anchorMin = rectTransform.anchorMin;
        Vector2 anchorMax = rectTransform.anchorMax;

        Vector2 offset = Vector2.zero;

        if (anchorMin.x == anchorMax.x)
        {
            if (anchorMin.x == 0f)
            {
                offset.x = leftInset;
            }
            else if (anchorMin.x == 1f)
            {
                offset.x = -rightInset;
            }
        }

        if (anchorMin.y == anchorMax.y)
        {
            if (anchorMin.y == 0f)
            {
                offset.y = bottomInset;
            }
            else if (anchorMin.y == 1f)
            {
                offset.y = -topInset;
            }
        }

        rectTransform.anchoredPosition =
            originalPosition + offset;
    }
}
