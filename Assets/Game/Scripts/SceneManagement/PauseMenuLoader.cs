using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuLoader : MonoBehaviour
{
    private const string PauseMenuSceneName = "PauseMenu";

    private void Start()
    {
        if (!SceneManager.GetSceneByName(PauseMenuSceneName).isLoaded)
        {
            SceneManager.LoadSceneAsync(
                PauseMenuSceneName,
                LoadSceneMode.Additive
            );
        }
    }
}