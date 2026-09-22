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
    private readonly System.Collections.Generic.List<Vector3> selectionVertices = new System.Collections.Generic.List<Vector3>();
    private readonly System.Collections.Generic.List<int> selectionTriangles = new System.Collections.Generic.List<int>();

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
                    bool intersectsBounds = selectionMesh.bounds.IntersectRay(poseRay);
                    selectionMesh.GetVertices(selectionVertices);
                    bool candidateHit = false;
                    for (int submesh = 0; submesh < selectionMesh.subMeshCount; submesh++){
                        if (selectionMesh.GetTopology(submesh) != MeshTopology.Triangles) continue;
                        selectionMesh.GetTriangles(selectionTriangles, submesh);
                        for (int i = 0; i < selectionTriangles.Count; i += 3){
                            Vector3 a = selectionVertices[selectionTriangles[i]];
                            Vector3 b = selectionVertices[selectionTriangles[i + 1]];
                            Vector3 c = selectionVertices[selectionTriangles[i + 2]];
                            if (!intersectsBounds || !IntersectTriangle(poseRay, a, b, c, out float localHit)) continue;
                            Vector3 worldPoint = skinned.transform.TransformPoint(poseRay.GetPoint(localHit));
                            float hitDistance = Vector3.Dot(worldPoint - ray.origin, ray.direction);
                            if (hitDistance >= 0f && hitDistance < closestDistance){
                                closestDistance = hitDistance;
                                tower = candidate;
                                candidateHit = true;
                            }
                        }
                    }

                    // Project only the eight bounds corners for the small click
                    // tolerance. Projecting all three vertices of every triangle
                    // made repeated clicks stall enemy movement for a frame.
                    if (!candidateHit && TryGetScreenBounds(
                        skinned.transform,
                        selectionMesh.bounds,
                        mainCamera,
                        out Rect screenBounds
                    )){
                        const float padding = 7f;
                        Rect paddedBounds = new Rect(
                            screenBounds.xMin - padding,
                            screenBounds.yMin - padding,
                            screenBounds.width + padding * 2f,
                            screenBounds.height + padding * 2f
                        );
                        if (paddedBounds.Contains(pointer)){
                            float screenDistance = (pointer - screenBounds.center).sqrMagnitude;
                            if (screenDistance < nearestScreenDistance){
                                nearestScreenDistance = screenDistance;
                                nearbyTower = candidate;
                            }
                        }
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
                    const float rigidPadding = 9f;
                    Rect paddedBounds = new Rect(
                        rigidScreenBounds.xMin - rigidPadding,
                        rigidScreenBounds.yMin - rigidPadding,
                        rigidScreenBounds.width + rigidPadding * 2f,
                        rigidScreenBounds.height + rigidPadding * 2f
                    );
                    if (paddedBounds.Contains(pointer)){
                        float screenDistance =
                            (pointer - rigidScreenBounds.center).sqrMagnitude;
                        if (screenDistance < nearestScreenDistance){
                            nearestScreenDistance = screenDistance;
                            nearbyTower = candidate;
                        }
                    }
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

    private static bool IntersectTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance){
        distance = 0f;
        Vector3 edge1 = b - a, edge2 = c - a;
        Vector3 p = Vector3.Cross(ray.direction, edge2);
        float determinant = Vector3.Dot(edge1, p);
        if (Mathf.Abs(determinant) < 0.00000001f) return false;
        float inverse = 1f / determinant;
        Vector3 offset = ray.origin - a;
        float u = Vector3.Dot(offset, p) * inverse;
        if (u < 0f || u > 1f) return false;
        Vector3 q = Vector3.Cross(offset, edge1);
        float v = Vector3.Dot(ray.direction, q) * inverse;
        if (v < 0f || u + v > 1f) return false;
        distance = Vector3.Dot(edge2, q) * inverse;
        return distance >= 0f;
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
