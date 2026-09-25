using MoreMountains.Feedbacks;
using UnityEngine;

public class TowerTargeting : MonoBehaviour {
    [SerializeField] private TowerData towerData;
    [SerializeField] private Transform turretHead;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private bool rotateTurret = true;
    public TowerData TowerData => towerData;

    private Transform target;
    private Transform targetRoot;
    private float fireCooldown = 0f;
    private float currentYaw;
    private MMF_Player cannonRecoilFeedbacks;
    private MMF_Player lightningSurgeFeedbacks;

    private void Awake(){
        ConfigureSelectionCollider();
        ConfigureCannonRecoil();
        ConfigureLightningSurge();
    }

    private void ConfigureCannonRecoil(){
        if (towerData == null ||
            !(towerData.AttackData is CannonAttackData) ||
            turretHead == null)
        {
            return;
        }

        cannonRecoilFeedbacks = gameObject.AddComponent<MMF_Player>();
        cannonRecoilFeedbacks.AddFeedback(new MMF_Scale
        {
            AnimateScaleTarget = turretHead,
            Mode = MMF_Scale.Modes.Additive,
            AnimateScaleDuration = 0.14f,
            RemapCurveZero = 0f,
            RemapCurveOne = 0.08f,
            UniformScaling = true,
            AllowAdditivePlays = false,
            DetermineScaleOnPlay = false
        });
        cannonRecoilFeedbacks.Initialization();
    }

    private void ConfigureLightningSurge(){
        if (towerData == null ||
            !(towerData.AttackData is LightningAttackData) ||
            turretHead == null)
        {
            return;
        }

        lightningSurgeFeedbacks = gameObject.AddComponent<MMF_Player>();
        lightningSurgeFeedbacks.AddFeedback(new MMF_Scale
        {
            AnimateScaleTarget = turretHead,
            Mode = MMF_Scale.Modes.Additive,
            AnimateScaleDuration = 0.16f,
            RemapCurveZero = 0f,
            RemapCurveOne = 0.045f,
            UniformScaling = true,
            AllowAdditivePlays = false,
            DetermineScaleOnPlay = false
        });
        lightningSurgeFeedbacks.Initialization();
    }

    private void ConfigureSelectionCollider(){
        int turretLayer = LayerMask.NameToLayer("Turrets");

        if (turretLayer >= 0){
            gameObject.layer = turretLayer;
        }

        if (TryGetComponent(out CapsuleCollider capsuleCollider)){
            capsuleCollider.center = new Vector3(0f, 1.8f, 0f);
            capsuleCollider.radius = 1.5f;
            capsuleCollider.height = 4.8f;
            return;
        }

        CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.8f, 0f);
        collider.radius = 1.5f;
        collider.height = 4.8f;
    }

    private void OnEnable(){
        if (turretHead != null){
            currentYaw = turretHead.eulerAngles.y;
        }
    }

    private void LateUpdate(){
        if (towerData == null || turretHead == null || firePoint == null || projectilePrefab == null){
            return;
        }

        fireCooldown = Mathf.Max(0f, fireCooldown - Time.deltaTime);
        UpdateTarget();

        if (target != null){
            if (rotateTurret){
                RotateTowardsTarget();
            }

            if (!rotateTurret || IsAimingAtTarget()){
                HandleShooting();
            }
        }
        else if (rotateTurret){
            SearchForEnemies();
        }
    }

    private void UpdateTarget(){
        if (target != null && targetRoot != null){
            float currentTargetDistance = HorizontalDistance(targetRoot.position);

            if (currentTargetDistance <= towerData.Range){
                return;
            }

            target = null;
            targetRoot = null;
        }

        float closestDistance = Mathf.Infinity;
        EnemyMovement closestEnemy = null;
        var activeEnemies = EnemyMovement.ActiveEnemies;
        for (int index = activeEnemies.Count - 1; index >= 0; index--){
            EnemyMovement enemy = activeEnemies[index];
            if (enemy == null || !enemy.isActiveAndEnabled){ continue; }
            float distance = HorizontalDistance(enemy.transform.position);

            if (distance < closestDistance && distance <= towerData.Range)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null){
            if (closestEnemy.TargetPoint != null){
                target = closestEnemy.TargetPoint;
                targetRoot = closestEnemy.transform;
                fireCooldown = Mathf.Max(
                    fireCooldown,
                    towerData.firstShotDelay
                );
            }
        }
    }

    private void RotateTowardsTarget(){
        Vector3 direction = target.position - turretHead.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        float targetY = Quaternion.LookRotation(direction).eulerAngles.y;

        currentYaw = Mathf.MoveTowardsAngle(
            currentYaw,
            targetY,
            towerData.rotationSpeed * Time.deltaTime
        );

        turretHead.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    private float HorizontalDistance(Vector3 position){
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private bool IsAimingAtTarget(){
        Vector3 directionToTarget = target.position - turretHead.position;
        directionToTarget.y = 0f;

        float angle = Vector3.Angle(
            turretHead.forward,
            directionToTarget
        );

        return angle <= towerData.aimTolerance;
    }

    private void SearchForEnemies(){
        currentYaw = Mathf.Repeat(currentYaw + towerData.searchRotationSpeed * Time.deltaTime, 360f);
        turretHead.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    private void HandleShooting(){
        if (fireCooldown <= 0f){
            Shoot();
            fireCooldown = 1f / towerData.FireRate;
        }
    }

    private void Shoot(){
        Vector3 shotPosition = firePoint.position;
        Quaternion shotRotation = firePoint.rotation;
        if (!IsFinite(shotPosition) || !IsFinite(shotRotation)){
            target = null;
            targetRoot = null;
            return;
        }

        float shotDamage = RunUpgradeState.Current.RollDamage(
            towerData.Damage,
            towerData.AttackData
        );

        if (towerData.AttackData is FlameAttackData flame && targetRoot != null){
            flame.FireCone(transform.position, shotPosition,
                targetRoot.position, towerData.Range, shotDamage);
            TowerAttackAudio.PlayShot(flame, shotPosition);
            return;
        }

        ProjectilePool projectilePool =
            ProjectilePool.GetShared(projectilePrefab);

        Projectile projectile = projectilePool.Get(
            shotPosition,
            shotRotation
        );

        projectile.SetTarget(
            target,
            shotDamage,
            towerData.AttackData
        );

        if (towerData.AttackData is CannonAttackData){
            cannonRecoilFeedbacks?.PlayFeedbacks(shotPosition);
            TowerAttackVfx.PlayCannonMuzzleFlash(
                shotPosition,
                shotRotation * Vector3.forward
            );
        }

        if (towerData.AttackData is LightningAttackData){
            lightningSurgeFeedbacks?.PlayFeedbacks(shotPosition);
            TowerAttackVfx.PlayLightningMuzzleFlash(
                shotPosition,
                shotRotation * Vector3.forward
            );
        }

        TowerAttackAudio.PlayShot(towerData.AttackData, shotPosition);
    }

    private static bool IsFinite(Vector3 value) =>
        IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

    private static bool IsFinite(Quaternion value) =>
        IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private void OnDrawGizmosSelected(){
        if (towerData == null){
            return;
        }

        Gizmos.DrawWireSphere(transform.position, towerData.Range);
    }
}
