using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacementManager : MonoBehaviour{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private LayerMask blockedLayer;
    [SerializeField] private GameObject towerPrefab;

    private GameObject towerPreview;
    private PlayerGold playerGold;

    private bool canPlaceTower;
    private Vector3 placementPosition;

    private void Start(){
        playerGold = FindFirstObjectByType<PlayerGold>();

        if (playerGold == null){
            Debug.LogWarning("PlayerGold was not found.");
        }

        CreatePreview();
    }

    private void Update(){
        if (
            Mouse.current == null ||
            mainCamera == null ||
            towerPreview == null
        ){
            return;
        }

        UpdatePreview();

        if (
            canPlaceTower &&
            Mouse.current.leftButton.wasPressedThisFrame
        ){
            PlaceTower();
        }
    }

    private void UpdatePreview(){
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            1000f,
            buildableLayer
        )){
            canPlaceTower = false;
            towerPreview.SetActive(false);
            return;
        }

        bool isBlocked = Physics.CheckSphere(
            hit.point,
            0.3f,
            blockedLayer
        );

        canPlaceTower = !isBlocked;

        towerPreview.SetActive(canPlaceTower);

        if (canPlaceTower){
            placementPosition = hit.point;
            towerPreview.transform.position = placementPosition;
        }
    }

    private void CreatePreview(){
        if (towerPrefab == null){
            return;
        }

        towerPreview = Instantiate(towerPrefab);

        towerPreview.name = $"{towerPrefab.name} (Preview)";

        TowerTargeting targeting =
            towerPreview.GetComponent<TowerTargeting>();

        if (targeting != null){
            targeting.enabled = false;
        }
    }

    private void PlaceTower(){
        if (playerGold == null){
            return;
        }

        TowerTargeting targeting =
            towerPrefab.GetComponent<TowerTargeting>();

        if (targeting == null || targeting.TowerData == null){
            return;
        }

        int cost = targeting.TowerData.cost;

        if (!playerGold.SpendGold(cost)){
            return;
        }

        Instantiate(
            towerPrefab,
            placementPosition,
            towerPrefab.transform.rotation
        );
    }
}