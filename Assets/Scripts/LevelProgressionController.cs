using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelProgressionController : MonoBehaviour
{
    private const string HighestUnlockedLevelKey = "HighestUnlockedLevel";

    [Header("Level State")]
    [SerializeField] private LevelState levelState;

    [Header("Level Complete UI")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TMP_Text scoreRewardText;
    [SerializeField] private TMP_Text bananaRewardText;
    [SerializeField] private TMP_Text tryCountText;

    [Header("Buttons")]
    [SerializeField] private Button nextLevelButton;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool useNextBuildIndex = true;
    [SerializeField] private string manualNextLevelSceneName;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool levelCompleted;

    private void OnEnable()
    {
        if (levelState != null)
            levelState.OnMonkeyFound += HandleLevelComplete;
    }

    private void OnDisable()
    {
        if (levelState != null)
            levelState.OnMonkeyFound -= HandleLevelComplete;
    }

    private void Start()
    {
        levelCompleted = false;

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
    }

    private void HandleLevelComplete()
    {
        if (levelCompleted)
            return;

        levelCompleted = true;

        UnlockNextLevel();
        UpdateCompletePanel();

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        if (showDebugLogs)
            Debug.Log("Level complete panel opened.");
    }

    private void UpdateCompletePanel()
    {
        if (levelState == null)
            return;

        if (scoreRewardText != null)
            scoreRewardText.text = $"+{levelState.LastScoreReward} SCORE";

        if (bananaRewardText != null)
            bananaRewardText.text = $"+{levelState.LastBananaReward} BANANAS";

        if (tryCountText != null)
            tryCountText.text = $"TRIES: {levelState.TryCount}";

        if (nextLevelButton != null)
            nextLevelButton.interactable = DoesNextLevelExist();
    }

    private void UnlockNextLevel()
    {
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        int nextBuildIndex = currentBuildIndex + 1;

        int highestUnlocked = PlayerPrefs.GetInt(
            HighestUnlockedLevelKey,
            currentBuildIndex
        );

        if (nextBuildIndex > highestUnlocked)
        {
            PlayerPrefs.SetInt(HighestUnlockedLevelKey, nextBuildIndex);
            PlayerPrefs.Save();
        }

        if (showDebugLogs)
            Debug.Log("Highest unlocked level build index: " + nextBuildIndex);
    }

    private bool DoesNextLevelExist()
    {
        if (!useNextBuildIndex)
            return !string.IsNullOrWhiteSpace(manualNextLevelSceneName);

        int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
        return nextBuildIndex < SceneManager.sceneCountInBuildSettings;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;

        if (useNextBuildIndex)
        {
            int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;

            if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextBuildIndex);
            }
            else
            {
                Debug.LogWarning("No next scene exists in Build Settings.");
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(manualNextLevelSceneName))
        {
            SceneManager.LoadScene(manualNextLevelSceneName);
        }
        else
        {
            Debug.LogWarning("Manual next level scene name is empty.");
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public static bool IsLevelUnlocked(int sceneBuildIndex)
    {
        int highestUnlocked = PlayerPrefs.GetInt(
            HighestUnlockedLevelKey,
            0
        );

        return sceneBuildIndex <= highestUnlocked;
    }

    [ContextMenu("Reset Level Progress")]
    public void ResetLevelProgress()
    {
        PlayerPrefs.DeleteKey(HighestUnlockedLevelKey);
        PlayerPrefs.Save();

        Debug.Log("Level progression reset.");
    }
}