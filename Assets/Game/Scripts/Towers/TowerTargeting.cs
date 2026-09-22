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

    private void Awake()
    {
        ConfigureSelectionCollider();
        ConfigureCannonRecoil();
        ConfigureLightningSurge();
    }

    private void ConfigureCannonRecoil()
    {
        if (towerData == null ||
            !(towerData.AttackData is CannonAttackData) ||
            turretHead == null)
        {
            return;
        }

        cannonRecoilFeedbacks = gameObject.AddComponent<MMF_Player>();
        cannonRecoilFeedbacks.AddFeedback(new MMF_PositionSpring
        {
            AnimatePositionTarget = turretHead,
            DeclaredDuration = 0.18f,
            Space = MMF_PositionSpring.Spaces.Local,
            Mode = MMF_PositionSpring.Modes.Bump,
            DampingX = 0.28f,
            DampingY = 0.28f,
            DampingZ = 0.24f,
            FrequencyX = 13f,
            FrequencyY = 13f,
            FrequencyZ = 15f,
            BumpPositionMin = new Vector3(0f, 0f, -0.1f),
            BumpPositionMax = new Vector3(0f, 0f, -0.1f)
        });
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

    private void ConfigureLightningSurge()
    {
        if (towerData == null ||
            !(towerData.AttackData is LightningAttackData) ||
            turretHead == null)
        {
            return;
        }

        lightningSurgeFeedbacks = gameObject.AddComponent<MMF_Player>();
        lightningSurgeFeedbacks.AddFeedback(new MMF_PositionSpring
        {
            AnimatePositionTarget = turretHead,
            DeclaredDuration = 0.2f,
            Space = MMF_PositionSpring.Spaces.Local,
            Mode = MMF_PositionSpring.Modes.Bump,
            DampingX = 0.32f,
            DampingY = 0.3f,
            DampingZ = 0.32f,
            FrequencyX = 16f,
            FrequencyY = 18f,
            FrequencyZ = 16f,
            BumpPositionMin = new Vector3(0f, 0.035f, 0f),
            BumpPositionMax = new Vector3(0f, 0.035f, 0f)
        });
        lightningSurgeFeedbacks.Initialization();
    }

    private void ConfigureSelectionCollider()
    {
        int turretLayer = LayerMask.NameToLayer("Turrets");

        if (turretLayer >= 0)
        {
            gameObject.layer = turretLayer;
        }

        if (TryGetComponent(out CapsuleCollider capsuleCollider))
        {
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

    private void OnEnable()
    {
        if (turretHead != null)
        {
            currentYaw = turretHead.eulerAngles.y;
        }
    }

    // Apply aiming after Animator updates the imported tower bones.
    private void LateUpdate()
    {
        if (towerData == null || turretHead == null || firePoint == null || projectilePrefab == null)
        {
            return;
        }

        UpdateTarget();

        if (target != null)
        {
            if (rotateTurret)
            {
                RotateTowardsTarget();
            }

            if (!rotateTurret || IsAimingAtTarget())
            {
                HandleShooting();
            }
        }
        else if (rotateTurret)
        {
            SearchForEnemies();
        }
    }

    private void UpdateTarget()
    {
        if (target != null && targetRoot != null)
        {
            float currentTargetDistance = HorizontalDistance(targetRoot.position);

            if (currentTargetDistance <= towerData.Range)
            {
                return;
            }

            target = null;
            targetRoot = null;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float closestDistance = Mathf.Infinity;
        GameObject closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = HorizontalDistance(enemy.transform.position);

            if (distance < closestDistance && distance <= towerData.Range)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            EnemyMovement enemyMovement =
                closestEnemy.GetComponent<EnemyMovement>();

            if (enemyMovement != null && enemyMovement.TargetPoint != null)
            {
                target = enemyMovement.TargetPoint;
                targetRoot = closestEnemy.transform;
                fireCooldown = towerData.firstShotDelay;
            }
        }
    }

    private void RotateTowardsTarget()
    {
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

    private float HorizontalDistance(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private bool IsAimingAtTarget()
    {
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

    private void HandleShooting()
    {
        fireCooldown -= Time.deltaTime;

        if (fireCooldown <= 0f)
        {
            Shoot();
            fireCooldown = 1f / towerData.FireRate;
        }
    }

    private void Shoot(){
        if (towerData.AttackData is FlameAttackData flame && targetRoot != null){
            // A flame pulse is an area attack, not a delayed homing projectile.
            flame.FireCone(transform.position, firePoint.position,
                targetRoot.position, towerData.Range, towerData.Damage);
            TowerAttackAudio.PlayShot(flame, firePoint.position);
            return;
        }

        ProjectilePool projectilePool =
            ProjectilePool.GetShared(projectilePrefab);

        Projectile projectile = projectilePool.Get(
            firePoint.position,
            firePoint.rotation
        );

        projectile.SetTarget(
            target,
            towerData.Damage,
            towerData.AttackData
        );

        if (towerData.AttackData is CannonAttackData)
        {
            cannonRecoilFeedbacks?.PlayFeedbacks(firePoint.position);
            TowerAttackVfx.PlayCannonMuzzleFlash(
                firePoint.position,
                firePoint.forward
            );
        }

        if (towerData.AttackData is LightningAttackData)
        {
            lightningSurgeFeedbacks?.PlayFeedbacks(firePoint.position);
            TowerAttackVfx.PlayLightningMuzzleFlash(
                firePoint.position,
                firePoint.forward
            );
        }

        TowerAttackAudio.PlayShot(towerData.AttackData, firePoint.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (towerData == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(transform.position, towerData.Range);
    }
}
