using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
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
        SceneManager.LoadScene("PauseMenu");
    }

    // Botón "SALIR"
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}