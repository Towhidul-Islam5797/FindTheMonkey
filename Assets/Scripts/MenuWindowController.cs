using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MenuWindowController : MonoBehaviour
{
    /*
     * MapWindowController checks this after a scene loads.
     *
     * 1 = run countdown and start the active level
     * 0 = do not automatically start
     */
    private const string StartSelectedLevelKey =
        "StartSelectedLevel";

    // =======================================================
    // MENU WINDOW
    // =======================================================

    [Header("Menu Window")]
    [Tooltip("Assign Canvas/MenuWindow.")]
    [SerializeField]
    private GameObject menuWindow;

    // =======================================================
    // PAUSE
    // =======================================================

    [Header("Pause Settings")]
    [SerializeField]
    private bool pauseGameWhenOpen = true;

    // =======================================================
    // UNITY
    // =======================================================

    private void Start()
    {
        if (menuWindow != null)
        {
            menuWindow.SetActive(false);
        }
    }

    // =======================================================
    // OPEN MENU
    // =======================================================

    public void OpenMenu()
    {
        if (menuWindow != null)
        {
            menuWindow.SetActive(true);
            menuWindow.transform.SetAsLastSibling();
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }
    }

    // =======================================================
    // CLOSE MENU / RESUME
    // =======================================================

    public void CloseMenu()
    {
        if (menuWindow != null)
        {
            menuWindow.SetActive(false);
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }

    // =======================================================
    // START / RESTART CURRENT LEVEL
    // =======================================================

    /// <summary>
    /// Reloads the current map scene and starts the currently
    /// selected level from the beginning.
    ///
    /// After loading:
    /// MapLevelManager activates the saved current level.
    /// MapWindowController shows 3, 2, 1, GO!
    /// Then CharacterPathMover.StartGame() is called.
    /// </summary>
    public void StartCurrentLevel()
    {
        /*
         * Hide the menu before reloading.
         */
        if (menuWindow != null)
        {
            menuWindow.SetActive(false);
        }

        /*
         * Important:
         * Time.timeScale survives scene changes, so restore it
         * before loading the scene.
         */
        Time.timeScale = 1f;

        /*
         * Tell MapWindowController in the newly loaded scene
         * that this level should start.
         */
        PlayerPrefs.SetInt(
            StartSelectedLevelKey,
            1
        );

        PlayerPrefs.Save();

        Scene currentScene =
            SceneManager.GetActiveScene();

        if (currentScene.buildIndex < 0)
        {
            Debug.LogError(
                "[MenuWindowController] Current scene is not " +
                "available in the Build Profile Scene List.",
                this
            );

            PlayerPrefs.SetInt(
                StartSelectedLevelKey,
                0
            );

            PlayerPrefs.Save();

            return;
        }

        Debug.Log(
            $"[MenuWindowController] Restarting current " +
            $"level in scene '{currentScene.name}'.",
            this
        );

        /*
         * Reload exactly the current map scene.
         *
         * Example:
         * JungleMap -> JungleMap
         * BeachMap  -> BeachMap
         * SnowMap   -> SnowMap
         */
        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }

    // =======================================================
    // OLD RESTART METHOD
    // =======================================================

    /// <summary>
    /// Kept so an old Button OnClick assignment using
    /// RestartLevel() does not break.
    /// </summary>
    public void RestartLevel()
    {
        StartCurrentLevel();
    }

    // =======================================================
    // MAIN MENU
    // =======================================================

    public void GoToMainMenu()
    {
        /*
         * MainMenu should not accidentally trigger a level
         * countdown later.
         */
        PlayerPrefs.SetInt(
            StartSelectedLevelKey,
            0
        );

        PlayerPrefs.Save();

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            "MainMenu"
        );
    }
}