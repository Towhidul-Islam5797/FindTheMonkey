using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private const string StartSelectedLevelKey = "StartSelectedLevel";

    public void PlayGame()
    {
        //SceneManager.LoadScene("WorldSelection"); Gameplay
        PlayerPrefs.SetInt(StartSelectedLevelKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("JungleMap");

    }

    public void OpenCollection()
    {
        SceneManager.LoadScene("MonkeyCollection");
    }

    public void OpenRewards()
    {
        SceneManager.LoadScene("Rewards");
    }

    public void OpenSettings()
    {
        SceneManager.LoadScene("Settings");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}