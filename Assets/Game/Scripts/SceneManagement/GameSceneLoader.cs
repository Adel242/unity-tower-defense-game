using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneLoader : MonoBehaviour{
    private readonly string[] additiveScenes ={
        "UI",
        "PauseMenu"
    };

    private void Start(){
        foreach (string sceneName in additiveScenes){
            if (!SceneManager.GetSceneByName(sceneName).isLoaded){
                SceneManager.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Additive
                );
            }
        }
    }
}