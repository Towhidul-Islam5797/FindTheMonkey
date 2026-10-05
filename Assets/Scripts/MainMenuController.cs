using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void PlayGame()
    {
        //SceneManager.LoadScene("WorldSelection"); Gameplay
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