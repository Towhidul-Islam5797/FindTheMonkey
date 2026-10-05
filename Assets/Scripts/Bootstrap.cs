using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }

    void Start()
    {
        LoadFirstScene();
    }

    void InitializeGame()
    {
        Debug.Log("Game Initialized");

        // Example setups
        Application.targetFrameRate = 60;

        // Later you can add:
        // AudioManager.Init();
        // SaveManager.Load();
        // SettingsManager.Load();
    }

    void LoadFirstScene()
    {
        // Decide where game starts
        SceneManager.LoadScene("Splash");
    }
}