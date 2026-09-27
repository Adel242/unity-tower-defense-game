using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuController : MonoBehaviour{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject soundPanel;

    public void OpenOptions(){
        pausePanel.SetActive(false);
        soundPanel.SetActive(false);
        optionsPanel.SetActive(true);
        FocusPanel(optionsPanel);
    }

    public void CloseOptions(){
        optionsPanel.SetActive(false);
        soundPanel.SetActive(false);
        pausePanel.SetActive(true);
        FocusPanel(pausePanel);
    }

    public void OpenSound(){
        optionsPanel.SetActive(false);
        soundPanel.SetActive(true);
        FocusPanel(soundPanel);
    }

    public void CloseSound(){
        soundPanel.SetActive(false);
        optionsPanel.SetActive(true);
        FocusPanel(optionsPanel);
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

    internal static void FocusPanel(GameObject panel){
        foreach (Selectable control in panel.GetComponentsInChildren<Selectable>()){
            if (control.IsInteractable()){
                control.Select();
                return;
            }
        }
    }
}
