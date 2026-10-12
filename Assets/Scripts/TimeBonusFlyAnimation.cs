using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class TimeBonusFlyAnimation : MonoBehaviour
{
    // -------------------------------------------------------
    // REFERENCES
    // -------------------------------------------------------

    [Header("References")]
    [Tooltip("The +N text that flies. Hidden until Play is called.")]
    [SerializeField]
    private TMP_Text flyText;

    [Tooltip("Where the text starts, for example next to the time bonus text.")]
    [SerializeField]
    private RectTransform startPoint;

    [Tooltip("Where the text lands, for example on the coin reward text.")]
    [SerializeField]
    private RectTransform endPoint;

    // -------------------------------------------------------
    // TIMING
    // -------------------------------------------------------

    [Header("Timing")]
    [Tooltip("Seconds to wait before the text starts to fly.")]
    [SerializeField, Min(0f)]
    private float startDelaySeconds = 0.4f;

    [Tooltip("Seconds the flight takes.")]
    [SerializeField, Min(0.1f)]
    private float flightSeconds = 0.8f;

    public event Action OnArrived;

    private Coroutine flyRoutine;

    // -------------------------------------------------------
    // UNITY
    // -------------------------------------------------------

    private void Awake()
    {
        HideFlyText();
    }

    private void OnDisable()
    {
        if (flyRoutine != null)
        {
            StopCoroutine(flyRoutine);
            flyRoutine = null;
        }

        HideFlyText();
    }

    // -------------------------------------------------------
    // PLAY
    // -------------------------------------------------------

    public void Play(int amount)
    {
        if (flyText == null ||
            startPoint == null ||
            endPoint == null)
        {
            OnArrived?.Invoke();
            return;
        }

        if (flyRoutine != null)
        {
            StopCoroutine(flyRoutine);
        }

        flyRoutine = StartCoroutine(FlyRoutine(amount));
    }

    private IEnumerator FlyRoutine(int amount)
    {
        flyText.text = $"+{amount}";
        SetAlpha(1f);
        flyText.transform.position = startPoint.position;
        flyText.enabled = true;

        yield return new WaitForSecondsRealtime(startDelaySeconds);

        Vector3 from = startPoint.position;
        Vector3 to = endPoint.position;
        float elapsed = 0f;

        while (elapsed < flightSeconds)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / flightSeconds);

            flyText.transform.position = Vector3.Lerp(from, to, t);
            SetAlpha(1f - t);

            yield return null;
        }

        HideFlyText();
        flyRoutine = null;

        OnArrived?.Invoke();
    }

    // -------------------------------------------------------
    // HELPERS
    // -------------------------------------------------------

    private void SetAlpha(float alpha)
    {
        Color color = flyText.color;
        color.a = alpha;
        flyText.color = color;
    }

    private void HideFlyText()
    {
        if (flyText != null)
        {
            flyText.enabled = false;
        }
    }
}
