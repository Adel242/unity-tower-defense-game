using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFlameAttackData", menuName = "Tower Defense/Attacks/Flame Attack")]
public class FlameAttackData : TowerAttackData
{
    [SerializeField] private float coneRange = 6f;
    [SerializeField, Range(1f, 180f)] private float coneAngle = 55f;
    [SerializeField] private LayerMask enemyLayer = 1 << 7;

    public override bool ApplyImpact(Vector3 attackOrigin, Vector3 impactPosition,
        EnemyHealth directTarget, float directDamage,
        HashSet<EnemyHealth> affectedEnemies, out Transform nextTarget)
    {
        nextTarget = null;
        Vector3 forward = impactPosition - attackOrigin;
        forward.y = 0f;

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if ((enemyLayer.value & (1 << enemy.layer)) == 0) continue;
            Vector3 direction = enemy.transform.position - attackOrigin;
            direction.y = 0f;
            EnemyHealth health = enemy.GetComponentInParent<EnemyHealth>();

            if (health != null && direction.magnitude <= coneRange &&
                Vector3.Angle(forward, direction) <= coneAngle * 0.5f &&
                affectedEnemies.Add(health))
            {
                health.TakeDamage(directDamage);
            }
        }

        TowerAttackVfx.PlayFlameCone(
            attackOrigin,
            forward,
            coneRange,
            coneAngle
        );
        return false;
    }
}
