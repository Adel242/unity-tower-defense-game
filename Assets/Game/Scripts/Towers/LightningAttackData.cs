using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewLightningAttackData",
    menuName = "Tower Defense/Attacks/Lightning Attack"
)]
public class LightningAttackData : TowerAttackData
{
    [SerializeField] private int maxBounces = 3;
    [SerializeField] private float bounceRange = 4f;
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

        if (directTarget != null && affectedEnemies.Add(directTarget))
        {
            directTarget.TakeDamage(directDamage);
        }

        if (affectedEnemies.Count > maxBounces + Mathf.RoundToInt(RunUpgradeState.Current.Bonus("bounces", this)))
        {
            return false;
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            bool isEnemyLayer =
                (enemyLayer.value & (1 << enemy.layer)) != 0;

            if (!isEnemyLayer)
            {
                continue;
            }

            EnemyHealth enemyHealth =
                enemy.GetComponentInParent<EnemyHealth>();

            if (
                enemyHealth == null ||
                affectedEnemies.Contains(enemyHealth)
            )
            {
                continue;
            }

            float distance = Vector3.Distance(
                impactPosition,
                enemy.transform.position
            );

            if (distance < closestDistance && distance <= bounceRange * RunUpgradeState.Current.Multiplier("chain", this))
            {
                EnemyMovement movement =
                    enemy.GetComponent<EnemyMovement>();

                if (movement != null && movement.TargetPoint != null)
                {
                    closestDistance = distance;
                    nextTarget = movement.TargetPoint;
                }
            }
        }

        if (nextTarget != null)
        {
            TowerAttackVfx.PlayLightningArc(
                impactPosition,
                nextTarget.position
            );
        }

        return nextTarget != null;
    }
}
