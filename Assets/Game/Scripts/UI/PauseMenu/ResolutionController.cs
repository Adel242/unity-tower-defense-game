using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ResolutionController : MonoBehaviour{
    [Header("Panel References")]
    [SerializeField] private GameObject resolutionPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Dropdown")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown screenModeDropdown;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    // 10 resoluciones comunes
    private int[,] resolutions = new int[,] {
        { 640, 480 },    // VGA
        { 800, 600 },    // SVGA
        { 1024, 576 },   // WSVGA
        { 1024, 768 },   // XGA
        { 1280, 720 },   // HD (720p)
        { 1366, 768 },   // HD (Laptop común)
        { 1600, 900 },   // HD+
        { 1920, 1080 },  // Full HD (1080p)
        { 2560, 1440 },  // QHD (2K)
        { 3840, 2160 }   // 4K UHD
    };

    void Start(){
        Debug.Log("=== RESOLUTION CONTROLLER START ===");

        if (resolutionPanel != null){
            resolutionPanel.SetActive(false);
            Debug.Log("ResolutionPanel desactivado");
        }

        if (resolutionDropdown != null){
            Debug.Log("Configurando dropdown: " + resolutionDropdown.name);
            SetupDropdown();
        }
        else{
            Debug.LogError("ResolutionDropdown es NULL - Asignalo en el Inspector!");
        }

        SetupScreenModeDropdown();
    }

    void SetupScreenModeDropdown(){
        if (screenModeDropdown == null){
            Debug.LogError("ScreenModeDropdown es NULL - Asignalo en el Inspector!");
            return;
        }

        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new List<string>{
            "Modo ventana",
            "Pantalla completa"
        });
        screenModeDropdown.SetValueWithoutNotify(
            Screen.fullScreenMode == FullScreenMode.Windowed ? 0 : 1
        );
        screenModeDropdown.RefreshShownValue();
        screenModeDropdown.onValueChanged.RemoveAllListeners();
        screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
    }

    void SetupDropdown(){
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.GetLength(0); i++){
            string option = resolutions[i, 0] + " x " + resolutions[i, 1];
            options.Add(option);
            Debug.Log("Opción " + i + ": " + option);

            if (Screen.width == resolutions[i, 0] && Screen.height == resolutions[i, 1]){
                currentIndex = i;
                Debug.Log("Resolución actual encontrada en índice: " + i);
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        Debug.Log("Dropdown configurado correctamente. Opciones: " + options.Count);
    }

    void OnResolutionChanged(int index){
        Debug.Log("=== ON RESOLUTION CHANGED ===");
        Debug.Log("Índice seleccionado: " + index);

        int width = resolutions[index, 0];
        int height = resolutions[index, 1];

        Debug.Log("Cambiando a: " + width + "x" + height);
        FullScreenMode screenMode =
            screenModeDropdown != null && screenModeDropdown.value == 1
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

        Screen.SetResolution(width, height, screenMode);
        Debug.Log("Resolución cambiada a: " + Screen.width + "x" + Screen.height);

        // Actualizar texto de estado
        if (statusText != null){
            statusText.text = "✓ Resolución cambiada a " + width + "x" + height;
            statusText.color = Color.green;
        }
    }

    void OnScreenModeChanged(int modeIndex){
        bool isFullscreen = modeIndex == 1;
        FullScreenMode screenMode = isFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        int index = resolutionDropdown != null ? resolutionDropdown.value : -1;
        int width = index >= 0 && index < resolutions.GetLength(0)
            ? resolutions[index, 0]
            : Screen.width;
        int height = index >= 0 && index < resolutions.GetLength(0)
            ? resolutions[index, 1]
            : Screen.height;

        Screen.SetResolution(width, height, screenMode);

        if (statusText != null){
            statusText.text = isFullscreen
                ? "Pantalla completa activada"
                : "Modo ventana activado";
            statusText.color = Color.white;
        }
    }

    public void OpenResolution(){
        Debug.Log("=== OPEN RESOLUTION ===");

        if (resolutionPanel != null){
            resolutionPanel.SetActive(true);
            Debug.Log("ResolutionPanel activado");
        }

        if (optionsPanel != null){
            optionsPanel.SetActive(false);
            Debug.Log("OptionsPanel desactivado");
        }

        // Mostrar resolución actual
        if (statusText != null){
            statusText.text = "Resolución actual: " + Screen.width + "x" + Screen.height;
            statusText.color = Color.white;
        }
    }

    public void CloseResolution(){
        Debug.Log("=== CLOSE RESOLUTION ===");

        if (resolutionPanel != null){
            resolutionPanel.SetActive(false);
            Debug.Log("ResolutionPanel desactivado");
        }

        if (optionsPanel != null){
            optionsPanel.SetActive(true);
            Debug.Log("OptionsPanel activado");
        }
    }

    public bool HandleEscape(){
        if (resolutionPanel == null || !resolutionPanel.activeSelf){
            return false;
        }

        CloseResolution();
        return true;
    }

    public void ResetPanel(){
        if (resolutionPanel != null){
            resolutionPanel.SetActive(false);
        }
    }
}
