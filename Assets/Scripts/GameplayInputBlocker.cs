using UnityEngine;

/// <summary>
/// Shared state used to stop gameplay scripts from reacting
/// while MenuWindow, MapWindow, or another modal UI is open.
/// </summary>
public static class GameplayInputBlocker
{
    private static bool modalWindowOpen;
    private static int blockUntilFrame = -1;

    public static bool IsGameplayInputBlocked
    {
        get
        {
            return modalWindowOpen ||
                   Time.frameCount <= blockUntilFrame;
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

    /// <summary>
    /// Releases gameplay input after the current UI click has
    /// completely finished.
    /// </summary>
    public static void CloseModalAndReleaseInput()
    {
        modalWindowOpen = false;

        // Prevent the level-button click from also becoming
        // the first gameplay click.
        blockUntilFrame = Time.frameCount + 1;
    }

    public static void Reset()
    {
        modalWindowOpen = false;
        blockUntilFrame = -1;
    }
}