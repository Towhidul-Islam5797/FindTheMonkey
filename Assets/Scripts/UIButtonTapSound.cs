using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonTapSound :
    MonoBehaviour,
    IPointerClickHandler
{
    private Button button;

    private void Awake()
    {
        button =
            GetComponent<Button>();
    }

    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        /*
         * Do not play sounds for locked/disabled buttons.
         */
        if (button != null &&
            !button.interactable)
        {
            return;
        }

        if (GameFeedbackManager.Instance != null)
        {
            GameFeedbackManager.Instance
                .PlayButtonTap();
        }
    }
}