using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Image healthFill;
    [SerializeField] private DamagePopup damagePopupPrefab;

    public event System.Action<EnemyHealth> Died;

    private float currentHealth;
    private float maximumHealth;
    private int goldReward;
    private PlayerGold playerGold;
    private MMF_Player hitFeedbacks;
    private Image healthDamageTrail;
    private float targetHealthFill = 1f;
    private float displayedHealthFill = 1f;
    private float displayedTrailFill = 1f;
    private bool healthBarInitialized;
    private float burnDamagePerSecond;
    private float burnRemaining;
    private float burnTickTimer;
    private HitFlashTarget[] hitFlashTargets;
    private float hitFlashRemaining;
    private const float BurnTickInterval = 0.5f;
    private const float HitFlashDuration = 0.01f;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static Material sharedHitFlashMaterial;

    private readonly struct HitFlashTarget{
        public readonly Renderer Renderer;
        public readonly Material[] OriginalMaterials;
        public readonly Material[] FlashMaterials;

        public HitFlashTarget(Renderer renderer, Material[] originalMaterials,
            Material[] flashMaterials){
            Renderer = renderer;
            OriginalMaterials = originalMaterials;
            FlashMaterials = flashMaterials;
        }
    }

    private void Awake(){
        if (enemyData == null){
            return;
        }

        maximumHealth = enemyData.maxHealth;
        currentHealth = maximumHealth;
        goldReward = enemyData.goldReward;
        ConfigureHealthBar();
        UpdateHealthBar();
        ConfigureHitFlash();
        hitFeedbacks = CreateScaleFeedback(
            gameObject,
            transform,
            0.1f,
            0.06f
        );
    }

    private void Start(){
        playerGold = FindFirstObjectByType<PlayerGold>();
    }

    private void Update(){
        UpdateBurn();
        UpdateHitFlash();

        if (!healthBarInitialized ||
            (Mathf.Approximately(displayedHealthFill, targetHealthFill) &&
             Mathf.Approximately(displayedTrailFill, targetHealthFill))){
            return;
        }

        displayedHealthFill = Mathf.MoveTowards(
            displayedHealthFill,
            targetHealthFill,
            3.5f * Time.deltaTime
        );
        displayedTrailFill = Mathf.MoveTowards(
            displayedTrailFill,
            targetHealthFill,
            0.85f * Time.deltaTime
        );

        SetBarFill(healthFill, displayedHealthFill);
        SetBarFill(healthDamageTrail, displayedTrailFill);
        healthFill.color = GetHealthColor(displayedHealthFill);
    }

    public void TakeDamage(float damage){
        // Destroy is deferred until the end of the frame. Several projectiles
        // can still hit this instance; award gold and signal death only once.
        if (enemyData == null || currentHealth <= 0f || damage <= 0f){
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        UpdateHealthBar();
        ShowDamagePopup(damage);
        PlayHitFlash();

        if (currentHealth <= 0f){
            Die();
            return;
        }

        hitFeedbacks?.PlayFeedbacks(transform.position);
    }

    private void ConfigureHitFlash(){
        System.Collections.Generic.List<HitFlashTarget> targets = new();
        Material flashMaterial = GetHitFlashMaterial();
        foreach (Renderer targetRenderer in GetComponentsInChildren<Renderer>()){
            if (targetRenderer is ParticleSystemRenderer ||
                targetRenderer is TrailRenderer ||
                targetRenderer is LineRenderer){
                continue;
            }

            Material[] originalMaterials = targetRenderer.sharedMaterials;
            if (originalMaterials.Length == 0){ continue; }

            Material[] flashMaterials = new Material[originalMaterials.Length];
            for (int index = 0; index < flashMaterials.Length; index++){
                flashMaterials[index] = flashMaterial;
            }

            targets.Add(new HitFlashTarget(
                targetRenderer,
                originalMaterials,
                flashMaterials
            ));
        }

        hitFlashTargets = targets.ToArray();
    }

    private static Material GetHitFlashMaterial(){
        if (sharedHitFlashMaterial != null){ return sharedHitFlashMaterial; }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null){ shader = Shader.Find("Sprites/Default"); }
        sharedHitFlashMaterial = new Material(shader){
            name = "Enemy Hit Flash (Runtime)",
            hideFlags = HideFlags.HideAndDontSave
        };
        if (sharedHitFlashMaterial.HasProperty(BaseColorId)){
            sharedHitFlashMaterial.SetColor(BaseColorId, Color.white);
        }
        if (sharedHitFlashMaterial.HasProperty(ColorId)){
            sharedHitFlashMaterial.SetColor(ColorId, Color.white);
        }
        return sharedHitFlashMaterial;
    }

    private void PlayHitFlash(){
        if (hitFlashTargets == null || hitFlashTargets.Length == 0){ return; }
        hitFlashRemaining = HitFlashDuration;
        SetHitFlashVisible(true);
    }

    private void UpdateHitFlash(){
        if (hitFlashRemaining <= 0f){ return; }

        hitFlashRemaining = Mathf.Max(0f, hitFlashRemaining - Time.deltaTime);
        if (hitFlashRemaining <= 0f){ SetHitFlashVisible(false); }
    }

    private void SetHitFlashVisible(bool visible){
        foreach (HitFlashTarget target in hitFlashTargets){
            if (target.Renderer == null){ continue; }
            target.Renderer.sharedMaterials = visible
                ? target.FlashMaterials
                : target.OriginalMaterials;
        }
    }

    private void OnDisable(){
        if (hitFlashTargets != null){
            SetHitFlashVisible(false);
        }
        hitFlashRemaining = 0f;
    }

    public void ApplyWaveScaling(
        float healthMultiplier,
        float goldRewardMultiplier
    ){
        if (enemyData == null){
            return;
        }

        maximumHealth = enemyData.maxHealth * Mathf.Max(0.1f, healthMultiplier);
        currentHealth = maximumHealth;
        goldReward = Mathf.Max(
            1,
            Mathf.RoundToInt(
                enemyData.goldReward * Mathf.Max(0f, goldRewardMultiplier)
            )
        );
        UpdateHealthBar();
    }

    public void ApplyBurn(float damagePerSecond, float duration){
        if (currentHealth <= 0f || damagePerSecond <= 0f || duration <= 0f){
            return;
        }

        // Reapplying fire refreshes the strongest burn instead of producing
        // an unbounded stack of coroutines on enemies inside the cone.
        burnDamagePerSecond = Mathf.Max(burnDamagePerSecond, damagePerSecond);
        burnRemaining = Mathf.Max(burnRemaining, duration);
        if (burnTickTimer <= 0f){ burnTickTimer = BurnTickInterval; }
    }

    private void UpdateBurn(){
        if (burnRemaining <= 0f || currentHealth <= 0f){ return; }

        burnRemaining = Mathf.Max(0f, burnRemaining - Time.deltaTime);
        burnTickTimer -= Time.deltaTime;
        if (burnTickTimer > 0f){ return; }

        burnTickTimer += BurnTickInterval;
        TakeDamage(burnDamagePerSecond * BurnTickInterval);
        if (burnRemaining <= 0f){ burnDamagePerSecond = 0f; }
    }

    private void UpdateHealthBar(){
        if (healthFill == null || enemyData == null){
            return;
        }

        targetHealthFill = currentHealth / maximumHealth;

        if (!healthBarInitialized){
            SetBarFill(healthFill, targetHealthFill);
            return;
        }

        if (Mathf.Approximately(currentHealth, maximumHealth)){
            displayedHealthFill = targetHealthFill;
            displayedTrailFill = targetHealthFill;
            SetBarFill(healthFill, targetHealthFill);
            SetBarFill(healthDamageTrail, targetHealthFill);
        }
    }

    private void ShowDamagePopup(float damage){
        if (damagePopupPrefab == null){
            return;
        }

        Vector3 popupPosition = transform.position + Vector3.up * 2.3f;

        DamagePopupPool popupPool =
            DamagePopupPool.GetShared(damagePopupPrefab);

        DamagePopup popup = popupPool.Get(
            popupPosition,
            Quaternion.identity
        );

        popup.Setup(damage);
    }

    private void Die(){
        if (playerGold != null && enemyData != null){
            playerGold.AddEnemyReward(goldReward);
        }

        EnemyDeathVfx.Play(transform.position);
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    private static MMF_Player CreateScaleFeedback(
        GameObject owner,
        Transform target,
        float duration,
        float scaleAmount
    ){
        MMF_Player player = owner.AddComponent<MMF_Player>();
        MMF_Scale scale = new MMF_Scale{
            Mode = MMF_Scale.Modes.Additive,
            AnimateScaleTarget = target,
            AnimateScaleDuration = duration,
            RemapCurveZero = 0f,
            RemapCurveOne = scaleAmount,
            UniformScaling = true,
            AllowAdditivePlays = false,
            // Keep the scale captured during initialization as the baseline.
            // Re-capturing it during a hit can store an in-progress enlarged
            // scale and make rapid successive hits grow the enemy permanently.
            DetermineScaleOnPlay = false
        };

        player.AddFeedback(scale);
        player.Initialization();
        return player;
    }

    private void ConfigureHealthBar(){
        if (healthFill == null){
            return;
        }

        RectTransform fillRect = healthFill.rectTransform;
        RectTransform backgroundRect = fillRect.parent as RectTransform;
        Image background = backgroundRect != null
            ? backgroundRect.GetComponent<Image>()
            : null;

        if (backgroundRect == null || background == null){
            return;
        }

        backgroundRect.sizeDelta = new Vector2(112f, 16f);
        background.sprite = null;
        background.type = Image.Type.Simple;
        background.color = new Color(0.025f, 0.035f, 0.05f, 0.98f);
        background.raycastTarget = false;

        GameObject trackObject = CreateBarImage(
            "Health Track",
            backgroundRect,
            new Color(0.08f, 0.11f, 0.15f, 1f)
        );
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        ApplyBarPadding(trackRect, 2.5f);

        fillRect.SetParent(trackRect, false);
        ApplyBarPadding(fillRect, 1f);
        // A sprite with rounded/soft edges makes the bar look oval and
        // obscures its exact health at both ends. A plain Image fills squarely.
        healthFill.sprite = null;
        healthFill.type = Image.Type.Simple;
        healthFill.raycastTarget = false;

        GameObject trailObject = new GameObject(
            "Health Damage Trail",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        RectTransform trailRect = trailObject.GetComponent<RectTransform>();
        trailRect.SetParent(trackRect, false);
        ApplyBarPadding(trailRect, 1f);
        trailRect.SetSiblingIndex(0);

        healthDamageTrail = trailObject.GetComponent<Image>();
        healthDamageTrail.sprite = null;
        healthDamageTrail.type = Image.Type.Simple;
        healthDamageTrail.color = new Color(1f, 0.24f, 0.16f, 0.9f);
        healthDamageTrail.raycastTarget = false;

        for (int index = 1; index < 5; index++){
            CreateHealthSegment(trackRect, index / 5f);
        }

        GameObject highlight = CreateBarImage(
            "Top Highlight",
            trackRect,
            new Color(1f, 1f, 1f, 0.14f)
        );
        RectTransform highlightRect = highlight.GetComponent<RectTransform>();
        highlightRect.anchorMin = new Vector2(0f, 1f);
        highlightRect.anchorMax = Vector2.one;
        highlightRect.pivot = new Vector2(0.5f, 1f);
        highlightRect.anchoredPosition = new Vector2(0f, -1f);
        highlightRect.sizeDelta = new Vector2(-2f, 1f);

        healthFill.color = GetHealthColor(1f);
        healthBarInitialized = true;
    }

    private static GameObject CreateBarImage(
        string objectName,
        RectTransform parent,
        Color color
    ){
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private static void CreateHealthSegment(
        RectTransform parent,
        float normalizedPosition
    ){
        GameObject segment = CreateBarImage(
            "Health Segment",
            parent,
            new Color(0.015f, 0.02f, 0.03f, 0.55f)
        );
        RectTransform segmentRect = segment.GetComponent<RectTransform>();
        segmentRect.anchorMin = new Vector2(normalizedPosition, 0f);
        segmentRect.anchorMax = new Vector2(normalizedPosition, 1f);
        segmentRect.pivot = new Vector2(0.5f, 0.5f);
        segmentRect.anchoredPosition = Vector2.zero;
        segmentRect.sizeDelta = new Vector2(1.25f, -2f);
    }

    private static void ApplyBarPadding(RectTransform rect, float padding){
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static void SetBarFill(Image image, float amount){
        RectTransform rect = image.rectTransform;
        rect.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
        rect.offsetMin = new Vector2(0f, 1f);
        rect.offsetMax = new Vector2(0f, -1f);
    }

    private static Color GetHealthColor(float healthPercent){
        Color lowHealth = new Color(1f, 0.18f, 0.14f, 1f);
        Color mediumHealth = new Color(1f, 0.68f, 0.12f, 1f);
        Color highHealth = new Color(0.18f, 0.9f, 0.52f, 1f);

        if (healthPercent < 0.5f){
            return Color.Lerp(lowHealth, mediumHealth, healthPercent * 2f);
        }

        return Color.Lerp(
            mediumHealth,
            highHealth,
            (healthPercent - 0.5f) * 2f
        );
    }
}

public static class EnemyDeathVfx
{
    private const string ParticleShader =
        "Universal Render Pipeline/Particles/Unlit";
    private static Material sharedDeathMaterial;

    public static void Play(Vector3 position)
    {
        GameObject effectObject = new GameObject("Enemy Death VFX");
        effectObject.SetActive(false);
        effectObject.transform.position = position + Vector3.up * 0.75f;

        ParticleSystem particles =
            effectObject.AddComponent<ParticleSystem>();

        ConfigureMain(particles);
        ConfigureEmission(particles);
        ConfigureShape(particles);
        ConfigureColor(particles);
        ConfigureSize(particles);
        ConfigureRenderer(effectObject);

        MMF_Player deathFeedbacks =
            effectObject.AddComponent<MMF_Player>();
        deathFeedbacks.AddFeedback(new MMF_Particles{
            BoundParticleSystem = particles,
            Mode = MMF_Particles.Modes.Play,
            MoveToPosition = false
        });

        effectObject.SetActive(true);
        deathFeedbacks.Initialization();
        deathFeedbacks.PlayFeedbacks(effectObject.transform.position);
    }

    private static void ConfigureMain(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.35f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.46f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.55f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 36;
        main.stopAction = ParticleSystemStopAction.Destroy;
    }

    private static void ConfigureEmission(ParticleSystem particles)
    {
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0f, 32)
        });
    }

    private static void ConfigureShape(ParticleSystem particles)
    {
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.45f;
        shape.radiusThickness = 1f;
    }

    private static void ConfigureColor(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.48f, 0.04f, 0.03f, 1f),
            new Color(0.95f, 0.18f, 0.10f, 1f)
        );

        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 0.45f, 0.35f), 0f),
                new GradientColorKey(new Color(0.30f, 0.05f, 0.04f), 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        ParticleSystem.ColorOverLifetimeModule color =
            particles.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(fade);
    }

    private static void ConfigureSize(ParticleSystem particles)
    {
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)
        );

        ParticleSystem.SizeOverLifetimeModule size =
            particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
    }

    private static void ConfigureRenderer(GameObject effectObject)
    {
        Shader shader = Shader.Find(ParticleShader);

        if (shader == null){
            return;
        }

        if (sharedDeathMaterial == null){
            sharedDeathMaterial = new Material(shader);
        }
        ParticleSystemRenderer particleRenderer =
            effectObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = sharedDeathMaterial;
    }
}
