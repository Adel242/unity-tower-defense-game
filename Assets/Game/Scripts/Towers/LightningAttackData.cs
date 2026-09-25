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

        float closestDistance = Mathf.Infinity;

        var activeEnemies = EnemyMovement.ActiveEnemies;
        for (int index = activeEnemies.Count - 1; index >= 0; index--)
        {
            EnemyMovement enemy = activeEnemies[index];
            if (enemy == null || !enemy.isActiveAndEnabled) continue;
            bool isEnemyLayer =
                (enemyLayer.value & (1 << enemy.gameObject.layer)) != 0;

            if (!isEnemyLayer)
            {
                continue;
            }

            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

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
                if (enemy.TargetPoint != null)
                {
                    closestDistance = distance;
                    nextTarget = enemy.TargetPoint;
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
