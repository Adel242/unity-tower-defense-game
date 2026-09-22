using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewArcaneAttackData", menuName = "Tower Defense/Attacks/Arcane Attack")]
public class ArcaneAttackData : TowerAttackData
{
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField, Min(0.1f)] private float impactVfxLifetime = 2.5f;
    [SerializeField, Min(0.01f)] private float impactVfxScale = 0.7f;

    public override bool ApplyImpact(Vector3 attackOrigin, Vector3 impactPosition,
        EnemyHealth directTarget, float directDamage,
        HashSet<EnemyHealth> affectedEnemies, out Transform nextTarget)
    {
        nextTarget = null;
        if (directTarget != null && affectedEnemies.Add(directTarget))
        {
            directTarget.TakeDamage(directDamage);
        }

        TowerAttackVfx.PlayPrefab(
            impactVfxPrefab,
            impactPosition,
            Quaternion.identity,
            impactVfxScale,
            impactVfxLifetime
        );
        return false;
    }
}
