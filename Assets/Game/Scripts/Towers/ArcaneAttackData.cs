using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewArcaneAttackData", menuName = "Tower Defense/Attacks/Arcane Attack")]
public class ArcaneAttackData : TowerAttackData
{
    public override bool ApplyImpact(Vector3 attackOrigin, Vector3 impactPosition,
        EnemyHealth directTarget, float directDamage,
        HashSet<EnemyHealth> affectedEnemies, out Transform nextTarget)
    {
        nextTarget = null;
        if (directTarget != null && affectedEnemies.Add(directTarget))
        {
            directTarget.TakeDamage(directDamage);
        }
        return false;
    }
}
