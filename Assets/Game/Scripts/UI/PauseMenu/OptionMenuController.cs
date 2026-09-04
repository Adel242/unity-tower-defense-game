using UnityEngine;

public class OptionsMenuController : MonoBehaviour{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject soundPanel;

    public void OpenOptions(){
        pausePanel.SetActive(false);
        soundPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions(){
        optionsPanel.SetActive(false);
        soundPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void OpenSound(){
        optionsPanel.SetActive(false);
        soundPanel.SetActive(true);
    }

    public void CloseSound(){
        soundPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public bool HandleEscape(){
        if (soundPanel.activeSelf){
            CloseSound();
            return true;
        }

        if (optionsPanel.activeSelf){
            CloseOptions();
            return true;
        }

        return false;
    }

    public void ResetPanels(){
        optionsPanel.SetActive(false);
        soundPanel.SetActive(false);
        pausePanel.SetActive(true);
    }
}