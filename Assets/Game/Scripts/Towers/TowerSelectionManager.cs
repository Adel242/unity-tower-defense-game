using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TowerSelectionManager : MonoBehaviour{
    private Camera mainCamera;
    private TowerPlacementManager placementManager;
    private TowerInfoPanel infoPanel;
    private TowerTargeting selectedTower;
    private int turretMask;

    private void Awake(){
        turretMask = LayerMask.GetMask("Turrets");
    }

    private void OnEnable(){
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindReferences();
    }

    private void OnDisable(){
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SelectTower(null);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode){
        FindReferences();
    }

    private void FindReferences(){
        mainCamera = Camera.main;
        placementManager = FindFirstObjectByType<TowerPlacementManager>();
        infoPanel = FindFirstObjectByType<TowerInfoPanel>();
        RefreshPanel();
    }

    // Run after UI events and construction have processed this frame's input.
    private void LateUpdate(){
        if (placementManager != null && placementManager.IsBuildMode){
            SelectTower(null);
            return;
        }

        if (selectedTower == null && infoPanel != null){
            infoPanel.Hide();
        }

        Mouse mouse = Mouse.current;

        if (Time.timeScale == 0f || mouse == null || mainCamera == null){
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame){
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()){
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
        TowerTargeting tower = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, turretMask, QueryTriggerInteraction.Ignore)){
            tower = hit.collider.GetComponentInParent<TowerTargeting>();

            if (tower != null && (!tower.isActiveAndEnabled || tower.TowerData == null)){
                tower = null;
            }
        }

        SelectTower(tower);
    }

    private void SelectTower(TowerTargeting tower){
        selectedTower = tower;
        RefreshPanel();
    }

    private void RefreshPanel(){
        if (infoPanel == null){
            return;
        }

        if (selectedTower != null){
            infoPanel.Show(selectedTower.TowerData);
        }
        else{
            infoPanel.Hide();
        }
    }
}
