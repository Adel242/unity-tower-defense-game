using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewCannonAttackData",
    menuName = "Tower Defense/Attacks/Cannon Attack"
)]
public class CannonAttackData : TowerAttackData
{
    [SerializeField] private float splashDamage = 3.5f;
    [SerializeField] private float splashRadius = 2f;
    [SerializeField] private LayerMask enemyLayer = 1 << 7;

    public override bool ApplyImpact(
        Vector3 attackOrigin,
        Vector3 impactPosition,
        EnemyHealth directTarget,
        float directDamage,
        HashSet<EnemyHealth> affectedEnemies,
        out Transform nextTarget
    )
    {
        nextTarget = null;
        Vector3 explosionPosition = directTarget != null
            ? directTarget.transform.position
            : impactPosition;

        if (directTarget != null)
        {
            affectedEnemies.Add(directTarget);
            directTarget.TakeDamage(directDamage);
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            EnemyHealth enemyHealth =
                enemy.GetComponentInParent<EnemyHealth>();

            bool isEnemyLayer =
                (enemyLayer.value & (1 << enemy.layer)) != 0;

            bool isInSplashRadius =
                Vector3.Distance(enemy.transform.position, explosionPosition)
                <= splashRadius;

            if (
                enemyHealth == null ||
                !isEnemyLayer ||
                !isInSplashRadius ||
                !affectedEnemies.Add(enemyHealth)
            )
            {
                continue;
            }

            enemyHealth.TakeDamage(splashDamage);
        }

        TowerAttackVfx.PlayCannonExplosion(
            explosionPosition,
            splashRadius
        );

        return false;
    }
}
