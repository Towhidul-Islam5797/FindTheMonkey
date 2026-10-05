using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MapLevelLoader : MonoBehaviour
{
    private const string SelectedLevelKey = "SelectedLevel";

    [Header("Map Information")]
    [Tooltip("Example: Jungle, Desert or Snow.")]
    [SerializeField]
    private string mapId = "Jungle";

    [Header("Level Roots")]
    [Tooltip(
        "Add Level_1, Level_2, Level_3 in the correct order."
    )]
    [SerializeField]
    private GameObject[] levelRoots;

    [Header("Scene Navigation")]
    [Tooltip("Scene containing the map and level buttons.")]
    [SerializeField]
    private string selectionSceneName = "MapLevelSelect";

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    public int CurrentLevel { get; private set; }

    public int TotalLevels
    {
        get
        {
            return levelRoots == null
                ? 0
                : levelRoots.Length;
        }
    }

    private void Awake()
    {
        LoadSelectedLevel();
    }

    private void LoadSelectedLevel()
    {
        if (levelRoots == null || levelRoots.Length == 0)
        {
            Debug.LogError(
                $"[MapLevelLoader:{mapId}] No level roots assigned.",
                this
            );

            return;
        }

        int selectedLevel =
            PlayerPrefs.GetInt(SelectedLevelKey, 1);

        selectedLevel = Mathf.Clamp(
            selectedLevel,
            1,
            levelRoots.Length
        );

        CurrentLevel = selectedLevel;

        for (int i = 0; i < levelRoots.Length; i++)
        {
            if (levelRoots[i] == null)
                continue;

            bool shouldBeActive =
                i == CurrentLevel - 1;

            levelRoots[i].SetActive(shouldBeActive);
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"[MapLevelLoader:{mapId}] " +
                $"Loaded Level {CurrentLevel}.",
                this
            );
        }
    }

    public void RestartCurrentLevel()
    {
        Time.timeScale = 1f;

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.name);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;

        int nextLevel = CurrentLevel + 1;

        if (nextLevel <= TotalLevels)
        {
            PlayerPrefs.SetInt(
                SelectedLevelKey,
                nextLevel
            );

            PlayerPrefs.Save();

            Scene currentScene =
                SceneManager.GetActiveScene();

            SceneManager.LoadScene(currentScene.name);
        }
        else
        {
            LoadSelectionScene();
        }
    }

    public void LoadSelectionScene()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(selectionSceneName))
        {
            Debug.LogError(
                $"[MapLevelLoader:{mapId}] " +
                "Selection Scene Name is empty.",
                this
            );

            return;
        }

        SceneManager.LoadScene(selectionSceneName);
    }

    [ContextMenu("DEBUG - Print Current Level")]
    private void DebugPrintCurrentLevel()
    {
        Debug.Log(
            $"[MapLevelLoader:{mapId}] " +
            $"CurrentLevel={CurrentLevel}, " +
            $"TotalLevels={TotalLevels}.",
            this
        );
    }
}