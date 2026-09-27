using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuController : MonoBehaviour{
    public GameObject pauseMenuUI;
    public GameObject pauseBackground;
    [SerializeField] private GameObject actionConfirmation;
    [SerializeField] private GameObject cancelConfirmationButton;
    [SerializeField] private CanvasGroup[] menuControls;
    [SerializeField] private TMP_Text confirmationText;
    private GameObject previousSelection;
    private bool changingScene;
    private string pendingScene;

    private TowerPlacementManager towerPlacementManager;
    private OptionsMenuController optionsMenuController;
    private ResolutionController resolutionController;

    private bool gameIsPaused;

    private void Start(){
        actionConfirmation.SetActive(false);
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

        if (actionConfirmation.activeSelf){
            CancelConfirmation();
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
        if (actionConfirmation.activeSelf) return;
        pauseMenuUI.SetActive(false);

        if (pauseBackground != null){
            pauseBackground.SetActive(false);
        }

        Time.timeScale = 1f;
        gameIsPaused = false;
    }

    public void RestartLevel(){
        ShowConfirmation("Game", "¿Seguro que quieres reiniciar la partida?");
    }

    public void GoToMainMenu(){
        ShowConfirmation("MainMenu", "¿Seguro que quieres salir al menú principal?");
    }

    private void ShowConfirmation(string sceneName, string question){
        if (!gameIsPaused || actionConfirmation.activeSelf) return;

        pendingScene = sceneName;
        confirmationText.text = question;
        previousSelection = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject : null;
        foreach (CanvasGroup controls in menuControls){
            controls.interactable = false;
            controls.blocksRaycasts = false;
        }
        actionConfirmation.SetActive(true);
        if (EventSystem.current != null){
            EventSystem.current.SetSelectedGameObject(cancelConfirmationButton);
        }
    }

    public void CancelConfirmation(){
        if (changingScene) return;
        pendingScene = null;
        actionConfirmation.SetActive(false);
        foreach (CanvasGroup controls in menuControls){
            controls.interactable = true;
            controls.blocksRaycasts = true;
        }
        if (EventSystem.current != null){
            EventSystem.current.SetSelectedGameObject(previousSelection);
        }
    }

    public void ConfirmAction(){
        if (!actionConfirmation.activeSelf || changingScene || string.IsNullOrEmpty(pendingScene)) return;
        changingScene = true;
        Time.timeScale = 1f;
        // Unload gameplay and its additive UI together for either destination.
        SceneManager.LoadScene(pendingScene, LoadSceneMode.Single);
    }

    public void QuitGame(){
        Time.timeScale = 1f;
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
