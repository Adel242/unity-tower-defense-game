using System.Collections.Generic;
using UnityEngine;

public enum ProjectileVisualStyle{
    Basic,
    Cannon,
    Lightning,
    Flame,
    Arcane
}

public class Projectile : MonoBehaviour{
    [SerializeField] private float speed = 10f;
    [SerializeField] private ProjectileVisualStyle visualStyle;

    private Transform target;
    private Vector3 lastTargetPosition;
    private Vector3 attackOrigin;
    private float damage;
    private float movementSpeed;
    private bool ballisticMovement;
    private float ballisticElapsed;
    private float ballisticDuration;
    private Vector3 ballisticVelocity;
    private Vector3 ballisticGravity;
    private Vector3 ballisticImpactPosition;
    private TowerAttackData attackData;
    private HashSet<EnemyHealth> affectedEnemies;
    private ProjectilePool pool;
    private Renderer[] projectileRenderers;
    private TrailRenderer energyTrail;
    private Material energyTrailMaterial;
    private Vector3 defaultScale;
    private float visualPulseOffset;

    private void Awake(){
        defaultScale = transform.localScale;
        projectileRenderers = GetComponentsInChildren<Renderer>();
        CreateEnergyTrail();    
        visualPulseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    public void SetPool(ProjectilePool newPool){
        pool = newPool;
    }

    public void SetTarget(Transform newTarget, float newDamage){
        SetTarget(newTarget, newDamage, null);
    }

    public void SetTarget(
        Transform newTarget,
        float newDamage,
        TowerAttackData newAttackData
    ){
        target = newTarget;
        attackOrigin = transform.position;
        damage = newDamage;
        attackData = newAttackData;
        movementSpeed = attackData != null
            ? attackData.ProjectileSpeed
            : speed;
        affectedEnemies = new HashSet<EnemyHealth>();
        ConfigureVisuals();

        if (target != null){
            if (attackData is CannonAttackData cannonAttack)
            {
                ConfigureBallisticMovement(cannonAttack);
            }
            else
            {
                lastTargetPosition = target.position;
            }
        }
    }

    private void Update(){
        UpdateArcaneVisual();

        if (ballisticMovement)
        {
            ballisticElapsed += Time.deltaTime;
            transform.position = attackOrigin +
                ballisticVelocity * ballisticElapsed +
                0.5f * ballisticGravity * ballisticElapsed * ballisticElapsed;

            if (ballisticElapsed >= ballisticDuration)
            {
                transform.position = ballisticImpactPosition;
                lastTargetPosition = ballisticImpactPosition;
                HitTarget();
            }

            return;
        }

        if (target != null){
            lastTargetPosition = target.position;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            lastTargetPosition,
            movementSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, lastTargetPosition) < 0.1f){
            HitTarget();
        }
    }

    private void HitTarget(){
        EnemyHealth enemyHealth = null;

        if (target != null){
            enemyHealth = target.GetComponentInParent<EnemyHealth>();
        }

        if (attackData != null){
            TowerAttackAudio.PlayImpact(attackData, lastTargetPosition);

            bool hasNextTarget = attackData.ApplyImpact(
                attackOrigin,
                lastTargetPosition,
                enemyHealth,
                damage,
                affectedEnemies,
                out Transform nextTarget
            );

            if (hasNextTarget){
                SetNextTarget(nextTarget);
                return;
            }
        }
        else if (enemyHealth != null){
            enemyHealth.TakeDamage(damage);
        }

        if (pool != null){
            pool.Release(this);
            return;
        }

        Destroy(gameObject);
    }

    public void ResetForPool(){
        if (!IsFinite(transform.position) || !IsFinite(transform.rotation) ||
            !IsFinite(transform.localScale)){
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = IsFinite(defaultScale) ? defaultScale : Vector3.one;
        }

        target = null;
        lastTargetPosition = Vector3.zero;
        attackOrigin = Vector3.zero;
        damage = 0f;
        movementSpeed = speed;
        ballisticMovement = false;
        ballisticElapsed = 0f;
        ballisticDuration = 0f;
        ballisticVelocity = Vector3.zero;
        ballisticGravity = Vector3.zero;
        ballisticImpactPosition = Vector3.zero;
        attackData = null;
        affectedEnemies = null;
        RestoreDefaultVisuals();
    }

    private static bool IsFinite(Vector3 value) =>
        IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

    private static bool IsFinite(Quaternion value) =>
        IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private void SetNextTarget(Transform nextTarget){
        target = nextTarget;
        lastTargetPosition = nextTarget.position;
    }

    private void ConfigureBallisticMovement(CannonAttackData cannonAttack)
    {
        ballisticMovement = true;
        ballisticElapsed = 0f;

        EnemyHealth enemyHealth =
            target.GetComponentInParent<EnemyHealth>();
        Vector3 targetPosition = enemyHealth != null
            ? enemyHealth.transform.position + Vector3.up * 0.35f
            : target.position;
        EnemyMovement enemyMovement =
            target.GetComponentInParent<EnemyMovement>();
        Vector3 targetVelocity = enemyMovement != null
            ? enemyMovement.Velocity
            : Vector3.zero;
        targetVelocity.y = 0f;

        float gravityMagnitude = cannonAttack.Gravity;
        ballisticDuration = Mathf.Max(
            Mathf.Sqrt(8f * cannonAttack.ArcHeight / gravityMagnitude)
        );

        for (int iteration = 0; iteration < 2; iteration++)
        {
            targetPosition = enemyHealth != null
                ? enemyHealth.transform.position + Vector3.up * 0.35f
                : target.position;
            targetPosition += targetVelocity * ballisticDuration;

            float horizontalDistance = Vector3.Distance(
                new Vector3(attackOrigin.x, 0f, attackOrigin.z),
                new Vector3(targetPosition.x, 0f, targetPosition.z)
            );

            ballisticDuration = Mathf.Max(
                horizontalDistance / movementSpeed,
                Mathf.Sqrt(8f * cannonAttack.ArcHeight / gravityMagnitude)
            );
        }

        ballisticImpactPosition = targetPosition;
        lastTargetPosition = targetPosition;

        Vector3 displacement = targetPosition - attackOrigin;
        Vector3 horizontalDisplacement = new Vector3(
            displacement.x,
            0f,
            displacement.z
        );
        Vector3 horizontalVelocity = horizontalDisplacement / ballisticDuration;
        float verticalVelocity = (
            displacement.y + 0.5f * gravityMagnitude *
            ballisticDuration * ballisticDuration
        ) / ballisticDuration;

        ballisticVelocity = horizontalVelocity +
            Vector3.up * verticalVelocity;
        ballisticGravity = Vector3.down * gravityMagnitude;
    }

    private void ConfigureVisuals(){
        RestoreDefaultVisuals();

        if (visualStyle == ProjectileVisualStyle.Lightning){
            SetProjectileRenderersVisible(false);
            ConfigureEnergyTrail(
                0.18f,
                0.14f,
                0.02f,
                new Color(0.7f, 0.95f, 1f, 1f),
                new Color(0.1f, 0.4f, 1f, 0f)
            );
            return;
        }

        if (visualStyle == ProjectileVisualStyle.Cannon){
            transform.localScale = defaultScale * 1.35f;

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.08f, 1f));
            properties.SetColor("_Color", new Color(0.08f, 0.08f, 0.08f, 1f));

            foreach (Renderer projectileRenderer in projectileRenderers){
                projectileRenderer.SetPropertyBlock(properties);
            }
        }

        if (visualStyle == ProjectileVisualStyle.Flame){
            SetProjectileColor(new Color(1f, 0.18f, 0.02f, 1f));
        }

        if (visualStyle == ProjectileVisualStyle.Arcane){
            transform.localScale = defaultScale * 1.75f;
            SetProjectileColor(new Color(0.65f, 0.12f, 1f, 1f));
            ConfigureEnergyTrail(
                0.32f,
                0.34f,
                0.04f,
                new Color(0.75f, 0.2f, 1f, 0.95f),
                new Color(0.15f, 0.75f, 1f, 0f)
            );
        }
    }

    private void UpdateArcaneVisual(){
        if (visualStyle != ProjectileVisualStyle.Arcane || attackData == null){
            return;
        }

        float pulse = 1.75f + Mathf.Sin(
            Time.time * 12f + visualPulseOffset
        ) * 0.18f;
        transform.localScale = defaultScale * pulse;
    }

    private void SetProjectileColor(Color color){
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        foreach (Renderer projectileRenderer in projectileRenderers){
            projectileRenderer.SetPropertyBlock(properties);
        }
    }

    private void RestoreDefaultVisuals(){
        transform.localScale = defaultScale;
        SetProjectileRenderersVisible(true);

        foreach (Renderer projectileRenderer in projectileRenderers){
            projectileRenderer.SetPropertyBlock(null);
        }

        if (energyTrail != null){
            energyTrail.emitting = false;
            energyTrail.Clear();
            energyTrail.enabled = false;
        }
    }

    private void SetProjectileRenderersVisible(bool isVisible){
        foreach (Renderer projectileRenderer in projectileRenderers){
            if (projectileRenderer != energyTrail){
                projectileRenderer.enabled = isVisible;
            }
        }
    }

    private void CreateEnergyTrail(){
        energyTrail = gameObject.AddComponent<TrailRenderer>();
        energyTrail.enabled = false;
        energyTrail.emitting = false;
        energyTrail.numCapVertices = 4;
        energyTrail.numCornerVertices = 2;
        energyTrail.minVertexDistance = 0.04f;

        Shader shader = Shader.Find(
            "Universal Render Pipeline/Particles/Unlit"
        );

        if (shader == null){
            shader = Shader.Find("Sprites/Default");
        }

        energyTrailMaterial = new Material(shader);
        energyTrailMaterial.color = Color.white;
        energyTrail.material = energyTrailMaterial;
    }

    private void ConfigureEnergyTrail(
        float lifetime,
        float startWidth,
        float endWidth,
        Color startColor,
        Color endColor
    ){
        energyTrail.time = lifetime;
        energyTrail.startWidth = startWidth;
        energyTrail.endWidth = endWidth;
        energyTrail.startColor = startColor;
        energyTrail.endColor = endColor;
        energyTrail.enabled = true;
        energyTrail.emitting = true;
    }

    private void OnDestroy(){
        if (energyTrailMaterial != null){
            Destroy(energyTrailMaterial);
        }
    }
}
