using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacementManager : MonoBehaviour{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private LayerMask blockedLayer;
    [SerializeField] private LayerMask turretLayer;

    [SerializeField] private GameObject towerPrefab;
    [SerializeField, Min(0f)] private float footprintPadding = 0.08f;
    [SerializeField, Range(4, 16)] private int footprintSamples = 12;
    [SerializeField, Min(0.5f)] private float constructionCellSize = 2f;

    [SerializeField] private Material validPreviewMaterial;
    [SerializeField] private Material invalidPreviewMaterial;
    [SerializeField] private AudioClip placementSound;
    [SerializeField, Range(0f, 1f)] private float placementSoundVolume = 0.35f;

    public bool IsBuildMode => isBuildMode;
    public bool ConsumedPlacementClickThisFrame { get; private set; }

    private GameObject towerPreview;
    private PlayerGold playerGold;

    private bool isBuildMode;
    private bool canPlaceTower;
    private Vector3 placementPosition;

    private Renderer[] previewRenderers;
    private float previewFootprintRadius = 0.5f;
    private ConstructionGrid constructionGrid;
    private LayerMask placementSurfaceLayer;

    private void Start(){
        playerGold = FindFirstObjectByType<PlayerGold>();

        if (playerGold == null){
            Debug.LogWarning("PlayerGold was not found.");
        }
        else{
            playerGold.GoldChanged += OnGoldChanged;
        }

        CreatePreview();

        if (towerPreview != null){
            towerPreview.SetActive(false);
        }

        constructionGrid = new ConstructionGrid(
            transform,
            constructionCellSize,
            buildableLayer,
            blockedLayer,
            turretLayer
        );
        placementSurfaceLayer = buildableLayer |
            blockedLayer |
            LayerMask.GetMask("EnemyPath");
    }

    private void OnDestroy(){
        if (playerGold != null){
            playerGold.GoldChanged -= OnGoldChanged;
        }

        constructionGrid?.Dispose();
    }

    private void Update(){
        if (RunUpgradeState.Current.BlocksInput) return;
        ConsumedPlacementClickThisFrame = false;

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
        constructionGrid?.Tick(Time.deltaTime);

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
        // Keep it hidden until the pointer is over a valid construction cell.
        towerPreview.SetActive(false);
        constructionGrid?.Show();
    }

    public void StartBuildMode(GameObject selectedTowerPrefab){
        if (selectedTowerPrefab == null){
            Debug.LogWarning("A tower prefab was not assigned to the build button.");
            return;
        }

        if (towerPrefab != selectedTowerPrefab){
            towerPrefab = selectedTowerPrefab;
            ReplacePreview();
        }

        StartBuildMode();
    }

    public void CancelBuildMode(){
        isBuildMode = false;
        canPlaceTower = false;

        if (towerPreview != null){
            towerPreview.SetActive(false);
        }

        constructionGrid?.Hide();
    }

    public void RefreshConstructionGrid(){
        constructionGrid?.Refresh();
    }

    private void UpdatePreview(){
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            1000f,
            placementSurfaceLayer
        )){
            canPlaceTower = false;
            UpdatePreviewMaterial();
            return;
        }

        if (
            constructionGrid == null ||
            !constructionGrid.TrySnap(hit.point, out placementPosition)
        ){
            placementPosition = hit.point;
            towerPreview.transform.position = placementPosition;
            towerPreview.SetActive(true);
            canPlaceTower = false;
            UpdatePreviewMaterial();
            return;
        }

        towerPreview.SetActive(true);
        towerPreview.transform.position = placementPosition;

        bool isOnBlockedArea = Physics.CheckSphere(
            placementPosition,
            0.3f,
            blockedLayer
        );

        bool isNearTurret = constructionGrid.IsCellOccupied(placementPosition);

        bool hasEnoughGold = HasEnoughGold();
        bool isFullySupported = IsFootprintFullySupported(placementPosition);

        canPlaceTower =
            !isOnBlockedArea &&
            !isNearTurret &&
            hasEnoughGold &&
            isFullySupported;

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

        previewFootprintRadius = CalculateFootprintRadius(colliders);

        foreach (Collider collider in colliders){
            collider.enabled = false;
        }

        previewRenderers =
            towerPreview.GetComponentsInChildren<Renderer>();
    }

    private float CalculateFootprintRadius(Collider[] colliders){
        float radius = 0.5f;
        Vector3 towerPosition = towerPreview.transform.position;

        foreach (Collider collider in colliders){
                if (collider.gameObject == towerPreview){
                    continue;
                }

                Bounds bounds = collider.bounds;
                Vector3 centerOffset = bounds.center - towerPosition;
                float extentX = Mathf.Abs(centerOffset.x) + bounds.extents.x;
            float extentZ = Mathf.Abs(centerOffset.z) + bounds.extents.z;
            radius = Mathf.Max(radius, extentX, extentZ);
        }

        return radius + footprintPadding;
    }

    private bool IsFootprintFullySupported(Vector3 center){
        int sampleCount = Mathf.Max(4, footprintSamples);

        for (int index = 0; index < sampleCount; index++){
            float angle = index * Mathf.PI * 2f / sampleCount;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle)
            ) * previewFootprintRadius;

            Vector3 rayOrigin = center + offset + Vector3.up * 1.5f;

            if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit supportHit,
                3f,
                buildableLayer
            )){
                return false;
            }

            if (Mathf.Abs(supportHit.point.y - center.y) > 0.08f){
                return false;
            }
        }

        return true;
    }

    private void ReplacePreview(){
        if (towerPreview != null){
            towerPreview.SetActive(false);
            Destroy(towerPreview);
        }

        CreatePreview();

        if (towerPreview != null){
            towerPreview.SetActive(false);
        }

    }

    private void OnGoldChanged(int currentGold){
        if (isBuildMode && !HasEnoughGold()){
            CancelBuildMode();
        }
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
            targeting.TowerData.Cost
        );
    }

    private void UpdatePreviewMaterial(){
        if (towerPreview == null){
            return;
        }

        // Invalid cells are already communicated by the construction grid.
        // Hiding the ghost also prevents a red preview from covering the tower
        // and its placement animation immediately after construction.
        towerPreview.SetActive(canPlaceTower);

        if (!canPlaceTower){
            return;
        }

        Material materialToUse = validPreviewMaterial;

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

        int cost = targeting.TowerData.Cost;

        if (!playerGold.SpendGold(cost)){
            return;
        }

        ConsumedPlacementClickThisFrame = true;

        GameObject placedTower = Instantiate(
            towerPrefab,
            placementPosition,
            towerPrefab.transform.rotation
        );

        // The occupied cell becomes invalid after Refresh(), but hide the
        // preview in this same frame so the creation feedback stays visible.
        towerPreview.SetActive(false);

        PlayPlacementFeedback(placedTower);
        TowerAttackAudio.PlayPlacement(
            placementSound,
            placedTower.transform.position,
            placementSoundVolume
        );
        constructionGrid?.Refresh();
    }

    private static void PlayPlacementFeedback(GameObject placedTower){
        // Imported tower Animators may write transforms on the prefab root.
        // Animate an external container so FEEL never competes with those curves.
        GameObject animationRoot = new GameObject($"{placedTower.name} Placement Root");
        animationRoot.AddComponent<PlacedTowerRoot>();
        animationRoot.transform.SetPositionAndRotation(
            placedTower.transform.position,
            Quaternion.identity
        );
        placedTower.transform.SetParent(animationRoot.transform, true);

        GameObject feedbackObject = new GameObject("Placement Feedbacks");
        feedbackObject.transform.SetParent(animationRoot.transform, false);
        MMF_Player feedbacks = feedbackObject.AddComponent<MMF_Player>();
        MMF_Position rise = new MMF_Position{
            Mode = MMF_Position.Modes.AtoB,
            Space = MMF_Position.Spaces.World,
            AnimatePositionTarget = animationRoot,
            AnimatePositionDuration = 0.28f,
            RelativePosition = true,
            DeterminePositionsOnPlay = false,
            InitialPosition = Vector3.down * 0.65f,
            DestinationPosition = Vector3.zero
        };
        MMF_Scale scale = new MMF_Scale{
            Mode = MMF_Scale.Modes.Absolute,
            AnimateScaleTarget = animationRoot.transform,
            AnimateScaleDuration = 0.3f,
            RemapCurveZero = 0.86f,
            RemapCurveOne = 1f,
            UniformScaling = true,
            AllowAdditivePlays = false,
            DetermineScaleOnPlay = false
        };

        feedbacks.AddFeedback(rise);
        feedbacks.AddFeedback(scale);
        feedbacks.Initialization();
        feedbacks.PlayFeedbacks(animationRoot.transform.position);
    }
}

public sealed class PlacedTowerRoot : MonoBehaviour{}
