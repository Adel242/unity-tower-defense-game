using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BasicAttackData",
    menuName = "Tower Defense/Attacks/Basic Attack"
)]
public class BasicAttackData : TowerAttackData
{
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

        if (directTarget != null)
        {
            directTarget.TakeDamage(directDamage);
        }

        return false;
    }
}
