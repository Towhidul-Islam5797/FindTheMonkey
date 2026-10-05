using UnityEngine;

/// <summary>
/// Shared input state for MenuWindow, MapWindow and gameplay.
/// This script is static and must not be attached to a GameObject.
/// </summary>
public static class GameplayInputGuard
{
    private static bool modalWindowOpen;
    private static int blockedThroughFrame = -1;

    public static bool IsGameplayInputBlocked
    {
        get
        {
            return modalWindowOpen ||
                   Time.frameCount <= blockedThroughFrame;
        }
    }

    public static void OpenModalWindow()
    {
        modalWindowOpen = true;
    }

    public static void KeepModalWindowOpen()
    {
        modalWindowOpen = true;
    }

    public static void CloseModalAndBlockFrames(
        int additionalFrames = 2
    )
    {
        modalWindowOpen = false;

        additionalFrames =
            Mathf.Max(1, additionalFrames);

        blockedThroughFrame =
            Time.frameCount + additionalFrames;
    }

    public static void Reset()
    {
        modalWindowOpen = false;
        blockedThroughFrame = -1;
    }
}