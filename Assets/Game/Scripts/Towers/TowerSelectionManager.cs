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
    private Mesh selectionMesh;

    private void OnDestroy(){
        if (selectionMesh != null) Destroy(selectionMesh);
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
        TowerTargeting nearbyTower = null;
        float nearestScreenDistance = float.PositiveInfinity;
        Vector2 pointer = mouse.position.ReadValue();

        float closestDistance = 1000f;
        // Only run on a click. Broad selection capsules overlap adjacent towers;
        // test each model part in its own local space instead.
        foreach (TowerTargeting candidate in FindObjectsByType<TowerTargeting>(
            FindObjectsSortMode.None)){
            if (!candidate.isActiveAndEnabled || candidate.TowerData == null){
                continue;
            }

            bool hasCandidateScreenBounds = false;
            Rect candidateScreenBounds = default;

            foreach (Renderer part in candidate.GetComponentsInChildren<Renderer>()){
                // Ignore particles, trails and the selection ring itself.
                if (!(part is MeshRenderer) && !(part is SkinnedMeshRenderer)){
                    continue;
                }

                if (!part.enabled || part.forceRenderingOff ||
                    (mainCamera.cullingMask & (1 << part.gameObject.layer)) == 0){
                    continue;
                }

                // Skinned import bounds may describe the bind pose rather than
                // the weapon after its bones have moved. Sample its current pose.
                if (part is SkinnedMeshRenderer skinned){
                    if (skinned.sharedMesh == null) continue;
                    if (selectionMesh == null) selectionMesh = new Mesh { name = "Tower picking pose" };
                    skinned.BakeMesh(selectionMesh, false);
                    Matrix4x4 inverse = skinned.transform.worldToLocalMatrix;
                    Ray poseRay = new Ray(inverse.MultiplyPoint3x4(ray.origin),
                        inverse.MultiplyVector(ray.direction).normalized);
                    selectionMesh.RecalculateBounds();
                    Bounds poseBounds = selectionMesh.bounds;
                    if (poseBounds.IntersectRay(poseRay, out float localHit)){
                        Vector3 worldPoint = skinned.transform.TransformPoint(
                            poseRay.GetPoint(localHit));
                        float hitDistance = Vector3.Dot(
                            worldPoint - ray.origin,
                            ray.direction
                        );
                        if (hitDistance >= 0f && hitDistance < closestDistance){
                            closestDistance = hitDistance;
                            tower = candidate;
                        }
                    }

                    if (TryGetScreenBounds(
                        skinned.transform,
                        poseBounds,
                        mainCamera,
                        out Rect screenBounds
                    )){
                        EncapsulateScreenBounds(
                            ref candidateScreenBounds,
                            ref hasCandidateScreenBounds,
                            screenBounds
                        );
                    }
                    continue;
                }

                Matrix4x4 toLocal = part.worldToLocalMatrix;
                Ray localRay = new Ray(
                    toLocal.MultiplyPoint3x4(ray.origin),
                    toLocal.MultiplyVector(ray.direction)
                );

                // Rigid decorative pieces (such as the flame tower nozzle)
                // also need a small screen-space tolerance. Their exact AABB
                // can leave visually solid gaps that are frustrating to click.
                if (TryGetScreenBounds(
                    part.transform,
                    part.localBounds,
                    mainCamera,
                    out Rect rigidScreenBounds
                )){
                    EncapsulateScreenBounds(
                        ref candidateScreenBounds,
                        ref hasCandidateScreenBounds,
                        rigidScreenBounds
                    );
                }

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

            // Treat the complete visible silhouette as one selectable region.
            // This includes gaps between the arcane tower's animated pieces,
            // while nearest-centre resolution keeps adjacent towers distinct.
            if (hasCandidateScreenBounds){
                const float selectionPadding = 12f;
                Rect paddedBounds = new Rect(
                    candidateScreenBounds.xMin - selectionPadding,
                    candidateScreenBounds.yMin - selectionPadding,
                    candidateScreenBounds.width + selectionPadding * 2f,
                    candidateScreenBounds.height + selectionPadding * 2f
                );
                if (paddedBounds.Contains(pointer)){
                    float screenDistance =
                        (pointer - candidateScreenBounds.center).sqrMagnitude;
                    if (screenDistance < nearestScreenDistance){
                        nearestScreenDistance = screenDistance;
                        nearbyTower = candidate;
                    }
                }
            }
        }

        SelectTower(tower != null ? tower : nearbyTower);
    }

    private static bool TryGetScreenBounds(
        Transform target,
        Bounds localBounds,
        Camera camera,
        out Rect result
    ){
        Vector3 center = localBounds.center;
        Vector3 extents = localBounds.extents;
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

        for (int x = -1; x <= 1; x += 2){
            for (int y = -1; y <= 1; y += 2){
                for (int z = -1; z <= 1; z += 2){
                    Vector3 localCorner = center + Vector3.Scale(
                        extents,
                        new Vector3(x, y, z)
                    );
                    Vector3 screen = camera.WorldToScreenPoint(
                        target.TransformPoint(localCorner)
                    );
                    if (screen.z <= camera.nearClipPlane){
                        result = default;
                        return false;
                    }

                    minX = Mathf.Min(minX, screen.x);
                    minY = Mathf.Min(minY, screen.y);
                    maxX = Mathf.Max(maxX, screen.x);
                    maxY = Mathf.Max(maxY, screen.y);
                }
            }
        }

        result = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    private static void EncapsulateScreenBounds(
        ref Rect aggregate,
        ref bool hasAggregate,
        Rect addition
    ){
        if (!hasAggregate){
            aggregate = addition;
            hasAggregate = true;
            return;
        }

        aggregate = Rect.MinMaxRect(
            Mathf.Min(aggregate.xMin, addition.xMin),
            Mathf.Min(aggregate.yMin, addition.yMin),
            Mathf.Max(aggregate.xMax, addition.xMax),
            Mathf.Max(aggregate.yMax, addition.yMax)
        );
    }

    private void SelectTower(TowerTargeting tower){
        if (selectedTower == tower){
            return;
        }

        selectedTower = tower;
        UpdateSelectionIndicator();

        if (infoPanel == null){
            infoPanel = FindFirstObjectByType<TowerInfoPanel>();
        }

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
