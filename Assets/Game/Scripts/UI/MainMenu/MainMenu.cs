using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenu : MonoBehaviour
{
    [SerializeField] private OptionsMenuController optionsMenu;
    [SerializeField] private ResolutionController resolutionMenu;

    private void Start()
    {
        optionsMenu.ResetPanels();
        resolutionMenu.ResetPanel();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        bool escapePressed = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (escapePressed && !resolutionMenu.HandleEscape()) optionsMenu.HandleEscape();
    }

    // Botón "JUGAR"
    public void PlayGame()
    {
        Debug.Log("Cargando escena Game...");
        SceneManager.LoadScene("Game");
    }

    // Botón "OPCIONES"
    public void OpenOptions()
    {
        Debug.Log("Abriendo menú de opciones...");
        optionsMenu.OpenOptions();
    }

    // Botón "SALIR"
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
