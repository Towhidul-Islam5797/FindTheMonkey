using UnityEngine;

public class SplashController : MonoBehaviour
{
    public float waitTime = 2.5f;

    void Start()
    {
        Invoke("GoToNextScene", waitTime);
    }

    void GoToNextScene()
    {
        SceneLoader.Instance.LoadScene("MainMenu");
    }
}