using UnityEngine;

public class TowerTargeting : MonoBehaviour
{
    [SerializeField] private float range = 8f;
    [SerializeField] private Transform turretHead;

    [SerializeField] private float rotationSpeed = 250f;
    [SerializeField] private float searchRotationSpeed = 100f;
    [SerializeField] private float aimTolerance = 20f;

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float firstShotDelay = 0.25f;

    private Transform target;
    private float fireCooldown = 0f;

    private void Update()
    {
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

        if (currentTargetDistance <= range)
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

        if (distance < closestDistance && distance <= range)
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
            fireCooldown = firstShotDelay;
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
        rotationSpeed * Time.deltaTime
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

        return angle <= aimTolerance;
    }

    private void SearchForEnemies()
    {
        turretHead.Rotate(
            Vector3.up,
            searchRotationSpeed * Time.deltaTime
        );
    }

    private void HandleShooting()
    {
        fireCooldown -= Time.deltaTime;

        if (fireCooldown <= 0f)
        {
            Shoot();
            fireCooldown = 1f / fireRate;
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

        projectile.SetTarget(target);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, range);
    }
}