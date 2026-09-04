using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacementManager : MonoBehaviour{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private LayerMask blockedLayer;
    [SerializeField] private LayerMask turretLayer;

    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private float minimumTurretDistance = 1f;

    [SerializeField] private Material validPreviewMaterial;
    [SerializeField] private Material invalidPreviewMaterial;

    public bool IsBuildMode => isBuildMode;

    private GameObject towerPreview;
    private PlayerGold playerGold;

    private bool isBuildMode;
    private bool canPlaceTower;
    private Vector3 placementPosition;

    private Renderer[] previewRenderers;

    private void Start(){
        playerGold = FindFirstObjectByType<PlayerGold>();

        if (playerGold == null){
            Debug.LogWarning("PlayerGold was not found.");
        }

        CreatePreview();

        if (towerPreview != null){
            towerPreview.SetActive(false);
        }
    }

    private void Update(){
        if (
            Mouse.current == null ||
            mainCamera == null ||
            towerPreview == null
        ){
            return;
        }

        if (!isBuildMode){
            return;
        }

        UpdatePreview();

        if (
            canPlaceTower &&
            Mouse.current.leftButton.wasPressedThisFrame
        ){
            PlaceTower();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame){
            CancelBuildMode();
        }
    }

    public void StartBuildMode(){
        if (towerPreview == null){
            return;
        }

        isBuildMode = true;
        towerPreview.SetActive(true);
    }

    public void CancelBuildMode(){
        isBuildMode = false;
        canPlaceTower = false;

        if (towerPreview != null){
            towerPreview.SetActive(false);
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
            towerPreview.SetActive(false);
            canPlaceTower = false;
            return;
        }

        towerPreview.SetActive(true);

        placementPosition = hit.point;
        towerPreview.transform.position = placementPosition;

        bool isOnBlockedArea = Physics.CheckSphere(
            hit.point,
            0.3f,
            blockedLayer
        );

        bool isNearTurret = Physics.CheckSphere(
            hit.point,
            minimumTurretDistance,
            turretLayer
        );

        bool hasEnoughGold = HasEnoughGold();

        canPlaceTower =
            !isOnBlockedArea &&
            !isNearTurret &&
            hasEnoughGold;

        UpdatePreviewMaterial();
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

        Collider[] colliders =
            towerPreview.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders){
            collider.enabled = false;
        }

        previewRenderers =
            towerPreview.GetComponentsInChildren<Renderer>();
    }

    private bool HasEnoughGold(){
        if (playerGold == null){
            return false;
        }

        TowerTargeting targeting =
            towerPrefab.GetComponent<TowerTargeting>();

        if (targeting == null || targeting.TowerData == null){
            return false;
        }

        return playerGold.CanAfford(
            targeting.TowerData.cost
        );
    }

    private void UpdatePreviewMaterial(){
        Material materialToUse =
            canPlaceTower
                ? validPreviewMaterial
                : invalidPreviewMaterial;

        if (materialToUse == null){
            return;
        }

        foreach (Renderer previewRenderer in previewRenderers){
            previewRenderer.material = materialToUse;
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