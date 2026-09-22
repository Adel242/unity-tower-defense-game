using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TowerSelectionManager : MonoBehaviour{
    private Camera mainCamera;
    private TowerPlacementManager placementManager;
    private TowerInfoPanel infoPanel;
    private TowerTargeting selectedTower;
    private LineRenderer selectionIndicator;

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
        if (RunUpgradeState.Current.BlocksInput){ SelectTower(null); return; }
        if (
            placementManager != null &&
            placementManager.ConsumedPlacementClickThisFrame
        ){
            SelectTower(null);
            return;
        }

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

        float closestDistance = 1000f;
        // Only run on a click. Broad selection capsules overlap adjacent towers;
        // test each model part in its own local space instead.
        foreach (TowerTargeting candidate in FindObjectsByType<TowerTargeting>(
            FindObjectsSortMode.None)){
            if (!candidate.isActiveAndEnabled || candidate.TowerData == null){
                continue;
            }

            foreach (Renderer part in candidate.GetComponentsInChildren<Renderer>()){
                // Ignore particles, trails and the selection ring itself.
                if (!(part is MeshRenderer) && !(part is SkinnedMeshRenderer)){
                    continue;
                }

                if (!part.enabled || part.forceRenderingOff ||
                    (mainCamera.cullingMask & (1 << part.gameObject.layer)) == 0){
                    continue;
                }

                Matrix4x4 toLocal = part.worldToLocalMatrix;
                Ray localRay = new Ray(
                    toLocal.MultiplyPoint3x4(ray.origin),
                    toLocal.MultiplyVector(ray.direction)
                );
                if (!part.localBounds.IntersectRay(localRay, out float localDistance)){
                    continue;
                }

                // Local ray distances cannot be compared across differently scaled parts.
                Vector3 worldHit = part.localToWorldMatrix.MultiplyPoint3x4(
                    localRay.GetPoint(localDistance));
                float distance = Vector3.Dot(worldHit - ray.origin, ray.direction);
                if (distance >= 0f && distance < closestDistance){
                    closestDistance = distance;
                    tower = candidate;
                }
            }
        }

        SelectTower(tower);
    }

    private void SelectTower(TowerTargeting tower){
        selectedTower = tower;
        UpdateSelectionIndicator();
        RefreshPanel();
    }

    private void UpdateSelectionIndicator(){
        if (selectionIndicator != null){
            Destroy(selectionIndicator.gameObject);
            selectionIndicator = null;
        }

        if (selectedTower == null){
            return;
        }

        GameObject indicatorObject = new GameObject("Tower Selection Indicator");
        indicatorObject.transform.SetParent(selectedTower.transform, false);
        indicatorObject.transform.localPosition = new Vector3(0f, 0.035f, 0f);

        selectionIndicator = indicatorObject.AddComponent<LineRenderer>();
        selectionIndicator.useWorldSpace = false;
        selectionIndicator.loop = true;
        selectionIndicator.positionCount = 40;
        selectionIndicator.startWidth = 0.045f;
        selectionIndicator.endWidth = 0.045f;
        selectionIndicator.material = new Material(Shader.Find("Sprites/Default"));
        selectionIndicator.startColor = new Color(0.35f, 0.9f, 1f, 0.95f);
        selectionIndicator.endColor = selectionIndicator.startColor;
        selectionIndicator.sortingOrder = 20;

        for (int index = 0; index < selectionIndicator.positionCount; index++){
            float angle = index * Mathf.PI * 2f /
                selectionIndicator.positionCount;
            selectionIndicator.SetPosition(index, new Vector3(
                Mathf.Cos(angle) * 1.25f,
                0f,
                Mathf.Sin(angle) * 1.25f
            ));
        }
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
