using InstantDestruction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TowerSelectionManager : MonoBehaviour{
    [Header("Tower sale destruction")]
    [SerializeField] private ComputeShader destructionComputeShader;
    [SerializeField] private Shader destructionShader;
    [SerializeField] private ParticleSystem destructionParticles;
    [SerializeField] private AudioClip destructionSound;
    [SerializeField, Range(0f, 1f)] private float enemyDestructionChance = 0.22f;
    [SerializeField, Min(1)] private int maxConcurrentEnemyDestructions = 3;
    [SerializeField, Min(1f)] private float enemyDestructionMaxDistance = 32f;

    private Camera mainCamera;
    private TowerPlacementManager placementManager;
    private TowerInfoPanel infoPanel;
    private TowerSellPanel sellPanel;
    private PlayerGold playerGold;
    private TowerTargeting selectedTower;
    private LineRenderer selectionIndicator;
    private Mesh selectionMesh;

    private void Awake(){
        TowerDestructionEffect.ConfigureSharedResources(
            destructionComputeShader,
            destructionShader,
            destructionParticles,
            destructionSound,
            enemyDestructionChance,
            maxConcurrentEnemyDestructions,
            enemyDestructionMaxDistance
        );
    }

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
        placementManager?.RefreshConstructionGrid();

        GameObject soldTowerRoot = placementRoot != null
            ? placementRoot.gameObject
            : towerToSell.gameObject;

        towerToSell.enabled = false;
        foreach (Collider towerCollider in
                 soldTowerRoot.GetComponentsInChildren<Collider>()){
            towerCollider.enabled = false;
        }

        bool destructionStarted = TowerDestructionEffect.Play(
            soldTowerRoot,
            destructionComputeShader,
            destructionShader,
            destructionParticles,
            destructionSound
        );

        if (!destructionStarted){
            Destroy(soldTowerRoot);
        }
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

public sealed class TowerDestructionEffect : BaseDestruction{
    private const float EffectLifetime = 1.5f;

    private static ComputeShader sharedComputeShader;
    private static Shader sharedDestructionShader;
    private static ParticleSystem sharedParticlePrefab;
    private static AudioClip sharedDestructionClip;
    private static int activeEnemyEffects;
    private static float enemyEffectChance = 0.22f;
    private static int maxConcurrentEnemyEffects = 3;
    private static float enemyEffectMaxDistance = 32f;

    protected override string GuidComputeShader => string.Empty;
    protected override string GuidShader => string.Empty;
    protected override string GuidParticle => string.Empty;
    protected override string GuidAudioClip => string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState(){
        sharedComputeShader = null;
        sharedDestructionShader = null;
        sharedParticlePrefab = null;
        sharedDestructionClip = null;
        activeEnemyEffects = 0;
    }

    public static void ConfigureSharedResources(
        ComputeShader computeShader,
        Shader destructionShader,
        ParticleSystem particlePrefab,
        AudioClip destructionClip,
        float enemyChance,
        int maxEnemyEffects,
        float enemyMaxDistance
    ){
        sharedComputeShader = computeShader;
        sharedDestructionShader = destructionShader;
        sharedParticlePrefab = particlePrefab;
        sharedDestructionClip = destructionClip;
        enemyEffectChance = Mathf.Clamp01(enemyChance);
        maxConcurrentEnemyEffects = Mathf.Max(1, maxEnemyEffects);
        enemyEffectMaxDistance = Mathf.Max(1f, enemyMaxDistance);
    }

    protected override void Start(){
        base.Start();
        if (destructionAudioClip == null){
            audioSource = null;
        }
    }

    public void Configure(
        ComputeShader computeShader,
        Shader destructionShader,
        ParticleSystem particlePrefab,
        AudioClip destructionClip
    ){
        instantDestructionCS = computeShader;
        instantDestructionSG = destructionShader;
        dustParticlePrefab = particlePrefab;
        destructionAudioClip = destructionClip;
        destructionAudioVolume = 0.18f;
        useGravity = true;
        afterDestructionMode = AfterDestructionMode.Nothing;
        afterDestructionTime = EffectLifetime;
    }

    public static bool Play(
        GameObject towerRoot,
        ComputeShader computeShader,
        Shader destructionShader,
        ParticleSystem particlePrefab,
        AudioClip destructionClip
    ){
        if (towerRoot == null || computeShader == null ||
            destructionShader == null){
            return false;
        }

        List<TowerDestructionEffect> destructibles = new();
        foreach (MeshFilter meshFilter in
                 towerRoot.GetComponentsInChildren<MeshFilter>()){
            Mesh mesh = meshFilter.sharedMesh;
            Renderer meshRenderer = meshFilter.GetComponent<Renderer>();
            if (mesh == null || meshRenderer == null || !mesh.isReadable){
                continue;
            }

            TowerDestructionEffect effect =
                meshFilter.gameObject.AddComponent<TowerDestructionEffect>();
            effect.Configure(
                computeShader,
                destructionShader,
                particlePrefab,
                destructionClip
            );
            destructibles.Add(effect);
        }

        if (destructibles.Count == 0){
            return false;
        }

        TowerDestructionSequence sequence =
            towerRoot.AddComponent<TowerDestructionSequence>();
        sequence.Begin(destructibles, EffectLifetime);
        return true;
    }

    public static bool PlayEnemyDeath(GameObject enemyRoot, Vector3 center){
        if (enemyRoot == null || sharedComputeShader == null ||
            sharedDestructionShader == null ||
            activeEnemyEffects >= maxConcurrentEnemyEffects ||
            Random.value > enemyEffectChance){
            return false;
        }

        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null || Vector3.Distance(
                gameplayCamera.transform.position,
                center
            ) > enemyEffectMaxDistance){
            return false;
        }

        SkinnedMeshRenderer[] skinnedRenderers =
            enemyRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (skinnedRenderers.Length == 0){
            return false;
        }

        GameObject effectRoot = new GameObject("Enemy Instant Destruction VFX");
        List<TowerDestructionEffect> destructibles = new();
        List<Mesh> bakedMeshes = new();

        foreach (SkinnedMeshRenderer sourceRenderer in skinnedRenderers){
            if (sourceRenderer == null || !sourceRenderer.enabled ||
                sourceRenderer.sharedMesh == null){
                continue;
            }

            Mesh bakedMesh = new Mesh{
                name = $"{sourceRenderer.sharedMesh.name} Death Pose"
            };
            sourceRenderer.BakeMesh(bakedMesh, false);

            GameObject meshObject = new GameObject(sourceRenderer.name);
            meshObject.transform.SetParent(effectRoot.transform, false);
            meshObject.transform.SetPositionAndRotation(
                sourceRenderer.transform.position,
                sourceRenderer.transform.rotation
            );
            meshObject.transform.localScale = sourceRenderer.transform.lossyScale;

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = bakedMesh;
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            meshRenderer.receiveShadows = sourceRenderer.receiveShadows;

            TowerDestructionEffect effect =
                meshObject.AddComponent<TowerDestructionEffect>();
            effect.Configure(
                sharedComputeShader,
                sharedDestructionShader,
                destructibles.Count == 0 ? sharedParticlePrefab : null,
                sharedDestructionClip
            );
            destructibles.Add(effect);
            bakedMeshes.Add(bakedMesh);
        }

        if (destructibles.Count == 0){
            Destroy(effectRoot);
            return false;
        }

        activeEnemyEffects++;
        TowerDestructionSequence sequence =
            effectRoot.AddComponent<TowerDestructionSequence>();
        sequence.Begin(
            destructibles,
            EffectLifetime,
            bakedMeshes,
            OnEnemyEffectFinished,
            center + Vector3.up * 0.8f
        );
        return true;
    }

    private static void OnEnemyEffectFinished(){
        activeEnemyEffects = Mathf.Max(0, activeEnemyEffects - 1);
    }
}

public sealed class TowerDestructionSequence : MonoBehaviour{
    private IReadOnlyList<TowerDestructionEffect> destructibles;
    private IReadOnlyList<Mesh> runtimeMeshes;
    private System.Action onFinished;
    private Vector3 explosionCenter;
    private float lifetime;
    private bool cleanedUp;

    public void Begin(
        IReadOnlyList<TowerDestructionEffect> effects,
        float effectLifetime,
        IReadOnlyList<Mesh> generatedMeshes = null,
        System.Action completion = null,
        Vector3? customExplosionCenter = null
    ){
        destructibles = effects;
        runtimeMeshes = generatedMeshes;
        onFinished = completion;
        explosionCenter = customExplosionCenter ??
            (transform.position + Vector3.up * 0.8f);
        lifetime = effectLifetime;
        StartCoroutine(PlayAfterInitialization());
    }

    private IEnumerator PlayAfterInitialization(){
        // BaseDestruction prepares its GPU buffers in Start. Waiting one frame
        // keeps runtime-created components compatible with that lifecycle.
        yield return null;

        foreach (TowerDestructionEffect destructible in destructibles){
            if (destructible != null){
                destructible.OnTrigger(explosionCenter);
            }
        }

        yield return new WaitForSeconds(lifetime);

        CleanUpGeneratedResources();
        Destroy(gameObject);
    }

    private void OnDestroy(){
        CleanUpGeneratedResources();
    }

    private void CleanUpGeneratedResources(){
        if (cleanedUp){
            return;
        }

        cleanedUp = true;
        if (runtimeMeshes != null){
            foreach (Mesh runtimeMesh in runtimeMeshes){
                if (runtimeMesh != null){
                    Destroy(runtimeMesh);
                }
            }
        }

        onFinished?.Invoke();
        onFinished = null;
    }
}
