using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuController : MonoBehaviour{
    public GameObject pauseMenuUI;
    public GameObject pauseBackground;

    private TowerPlacementManager towerPlacementManager;
    private OptionsMenuController optionsMenuController;
    private ResolutionController resolutionController;

    private bool gameIsPaused;

    private void Start(){
        pauseMenuUI.SetActive(false);

        if (pauseBackground != null){
            pauseBackground.SetActive(false);
        }

        optionsMenuController =
            GetComponent<OptionsMenuController>();

        resolutionController =
            GetComponent<ResolutionController>();

        if (optionsMenuController != null){
            optionsMenuController.ResetPanels();
            pauseMenuUI.SetActive(false);
        }

        if (resolutionController != null){
            resolutionController.ResetPanel();
        }

        Time.timeScale = 1f;
        gameIsPaused = false;

        towerPlacementManager =
            FindFirstObjectByType<TowerPlacementManager>();
    }

    private void Update(){
        bool escapePressed;

#if ENABLE_INPUT_SYSTEM
        escapePressed =
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        escapePressed = Input.GetKeyDown(KeyCode.Escape);
#endif

        if (!escapePressed){
            return;
        }

        if (
            towerPlacementManager != null &&
            towerPlacementManager.IsBuildMode
        ){
            towerPlacementManager.CancelBuildMode();
            return;
        }

        if (gameIsPaused){
            if (
                resolutionController != null &&
                resolutionController.HandleEscape()
            ){
                return;
            }

            if (
                optionsMenuController != null &&
                optionsMenuController.HandleEscape()
            ){
                return;
            }

            ResumeGame();
        }
        else{
            PauseGame();
        }
    }

    public void PauseGame(){
        if (resolutionController != null){
            resolutionController.ResetPanel();
        }

        if (optionsMenuController != null){
            optionsMenuController.ResetPanels();
        }
        else{
            pauseMenuUI.SetActive(true);
        }

        if (pauseBackground != null){
            pauseBackground.SetActive(true);
        }

        Time.timeScale = 0f;
        gameIsPaused = true;
    }

    public void ResumeGame(){
        pauseMenuUI.SetActive(false);

        if (pauseBackground != null){
            pauseBackground.SetActive(false);
        }

        Time.timeScale = 1f;
        gameIsPaused = false;
    }

    public void RestartLevel(){
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void GoToMainMenu(){
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame(){
        Time.timeScale = 1f;
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
