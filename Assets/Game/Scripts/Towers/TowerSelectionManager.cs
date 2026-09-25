using TMPro;
using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TowerSelectionManager : MonoBehaviour{
    private Camera mainCamera;
    private TowerPlacementManager placementManager;
    private TowerInfoPanel infoPanel;
    private TowerSellPanel sellPanel;
    private PlayerGold playerGold;
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
        sellPanel = FindFirstObjectByType<TowerSellPanel>();
        playerGold = FindFirstObjectByType<PlayerGold>();
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

    public void SellSelectedTower(float refundRatio){
        if (selectedTower == null || selectedTower.TowerData == null){ return; }
        if (playerGold == null){ playerGold = FindFirstObjectByType<PlayerGold>(); }
        if (playerGold == null){ return; }

        TowerTargeting towerToSell = selectedTower;
        PlacedTowerRoot placementRoot = towerToSell.GetComponentInParent<PlacedTowerRoot>();
        int refund = Mathf.Max(1, Mathf.RoundToInt(
            towerToSell.TowerData.Cost * Mathf.Clamp01(refundRatio)
        ));

        SelectTower(null);
        playerGold.AddGold(refund);


        GameObject soldTowerRoot = placementRoot != null
            ? placementRoot.gameObject
            : towerToSell.gameObject;

        towerToSell.enabled = false;
        foreach (Collider towerCollider in
                 soldTowerRoot.GetComponentsInChildren<Collider>()){
            towerCollider.enabled = false;
        }

        placementManager?.RefreshConstructionGrid();
        TowerSaleEffect.Play(soldTowerRoot, towerToSell.TowerData.AttackData, refund);
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
        // Tower statistics are previews only; selecting is reserved for actions.
        infoPanel?.Hide();

        if (sellPanel == null){ sellPanel = FindFirstObjectByType<TowerSellPanel>(); }
        if (selectedTower != null){ sellPanel?.Show(selectedTower, this); }
        else{ sellPanel?.Hide(); }
    }
}


public sealed class TowerSaleEffect : MonoBehaviour{
    private GameObject visualRoot;
    private Material effectMaterial;

    public static void Play(GameObject tower, TowerAttackData attack, int refund){
        foreach (MMF_Player feedback in tower.GetComponentsInChildren<MMF_Player>()){
            feedback.StopFeedbacks();
            feedback.enabled = false;
        }
        foreach (Animator animator in tower.GetComponentsInChildren<Animator>()){
            animator.enabled = false;
        }
        foreach (ParticleSystem particles in tower.GetComponentsInChildren<ParticleSystem>()){
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        GameObject owner = new GameObject("Tower Sale VFX");
        owner.transform.position = tower.transform.position;
        TowerSaleEffect effect = owner.AddComponent<TowerSaleEffect>();
        effect.visualRoot = tower;
        // External parent prevents sale animation from modifying imported pivots.
        tower.transform.SetParent(owner.transform, true);
        Color tint = attack is FlameAttackData ? new Color(1f, .32f, .07f) :
            attack is ArcaneAttackData ? new Color(.7f, .35f, 1f) :
            attack is LightningAttackData ? new Color(.2f, .85f, 1f) :
            new Color(1f, .74f, .3f);
        effect.StartCoroutine(effect.Animate(tint, refund));
    }

    private IEnumerator Animate(Color tint, int refund){
        Shader shader = Shader.Find("Sprites/Default");
        LineRenderer ring = null;
        if (shader != null){
            effectMaterial = new Material(shader);
            GameObject ringObject = new GameObject("Sale Ring");
            ringObject.transform.SetParent(transform, false);
            ring = ringObject.AddComponent<LineRenderer>();
            ring.sharedMaterial = effectMaterial;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 48;
            ring.widthMultiplier = .055f;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject sparksObject = new GameObject("Sale Motes");
            sparksObject.SetActive(false);
            sparksObject.transform.SetParent(transform, false);
            sparksObject.transform.localPosition = Vector3.up * .65f;
            ParticleSystem sparks = sparksObject.AddComponent<ParticleSystem>();
            var main = sparks.main;
            main.loop = false;
            main.duration = .6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.3f, .65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.7f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(.04f, .1f);
            main.startColor = tint;
            main.maxParticles = 18;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = sparks.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[]{new ParticleSystem.Burst(0, 18)});
            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .65f;
            var color = sparks.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(new[]{new GradientColorKey(Color.white, 0)},
                new[]{new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1)});
            color.color = fade;
            sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = effectMaterial;
            sparksObject.SetActive(true);
            sparks.Play();
        }

        GameObject labelObject = new GameObject("Sale Refund");
        labelObject.transform.SetParent(transform, false);
        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.text = "+" + refund;
        label.fontSize = 4;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, .8f, .25f);
        label.rectTransform.sizeDelta = new Vector2(3f, 1f);
        label.GetComponent<MeshRenderer>().sortingOrder = 25;
        Vector3 initialScale = visualRoot.transform.localScale;
        Vector3 initialPosition = visualRoot.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < .85f){
            elapsed += Time.deltaTime;
            float collapse = Mathf.Clamp01(elapsed / .3f);
            float ease = collapse * collapse * (3f - 2f * collapse);
            if (visualRoot != null){
                visualRoot.transform.localScale = initialScale * (1f - ease);
                visualRoot.transform.localPosition = initialPosition + Vector3.down * (.25f * ease);
                if (collapse >= 1f){
                    Destroy(visualRoot);
                    visualRoot = null;
                }
            }
            float progress = Mathf.Clamp01(elapsed / .65f);
            if (ring != null){
                Color ringColor = tint;
                ringColor.a = 1f - progress;
                ring.startColor = ring.endColor = ringColor;
                float radius = Mathf.Lerp(.35f, 1.5f, 1f - Mathf.Pow(1f - progress, 3));
                for (int i = 0; i < ring.positionCount; i++){
                    float angle = i * Mathf.PI * 2 / ring.positionCount;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .05f, Mathf.Sin(angle) * radius));
                }
            }
            label.transform.localPosition = Vector3.up * (1.8f + elapsed * .6f);
            Camera camera = Camera.main;
            if (camera != null) label.transform.rotation = camera.transform.rotation;
            label.alpha = 1f - Mathf.Clamp01((elapsed - .4f) / .45f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy(){
        if (effectMaterial != null) Destroy(effectMaterial);
    }
}
