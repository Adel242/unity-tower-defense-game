using UnityEngine;

public class TowerTargeting : MonoBehaviour {
    [SerializeField] private TowerData towerData;
    [SerializeField] private Transform turretHead;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private Transform target;
    private float fireCooldown = 0f;

    private void Update()
    {
        if (towerData == null)
        {
            return;
        }

        UpdateTarget();

        if (target != null)
        {
            RotateTowardsTarget();

            if (IsAimingAtTarget())
            {
                HandleShooting();
            }
        }
        else
        {
            SearchForEnemies();
        }
    }

    private void UpdateTarget()
    {
        if (target != null)
        {
            float currentTargetDistance = Vector3.Distance(
                transform.position,
                target.position
            );

            if (currentTargetDistance <= towerData.range)
            {
                return;
            }

            target = null;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        float closestDistance = Mathf.Infinity;
        GameObject closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distance < closestDistance && distance <= towerData.range)
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

        float newY = Mathf.MoveTowardsAngle(
            turretHead.eulerAngles.y,
            targetY,
            towerData.rotationSpeed * Time.deltaTime
        );

        turretHead.rotation = Quaternion.Euler(0f, newY, 0f);
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
        turretHead.Rotate(
            Vector3.up,
            towerData.searchRotationSpeed * Time.deltaTime
        );
    }

    private void HandleShooting()
    {
        fireCooldown -= Time.deltaTime;

        if (fireCooldown <= 0f)
        {
            Shoot();
            fireCooldown = 1f / towerData.fireRate;
        }
    }

    private void Shoot()
    {
        GameObject projectileObject = Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();

        projectile.SetTarget(target, towerData.damage);
    }

    private void OnDrawGizmosSelected()
    {
        if (towerData == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(transform.position, towerData.range);
    }
}