using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFlameAttackData", menuName = "Tower Defense/Attacks/Flame Attack")]
public class FlameAttackData : TowerAttackData
{
    [SerializeField] private float coneRange = 6f;
    [SerializeField, Range(1f, 180f)] private float coneAngle = 55f;
    [SerializeField] private LayerMask enemyLayer = 1 << 7;

    public void FireCone(Vector3 towerOrigin, Vector3 muzzlePosition,
        Vector3 targetPosition, float range, float damage)
    {
        ApplyCone(towerOrigin, muzzlePosition, targetPosition, range, damage,
            new HashSet<EnemyHealth>());
    }

    public override bool ApplyImpact(Vector3 attackOrigin, Vector3 impactPosition,
        EnemyHealth directTarget, float directDamage,
        HashSet<EnemyHealth> affectedEnemies, out Transform nextTarget)
    {
        nextTarget = null;
        ApplyCone(attackOrigin, attackOrigin, impactPosition, coneRange,
            directDamage, affectedEnemies);
        return false;
    }

    private void ApplyCone(Vector3 attackOrigin, Vector3 muzzlePosition,
        Vector3 impactPosition, float range, float directDamage,
        HashSet<EnemyHealth> affectedEnemies)
    {
        Vector3 forward = impactPosition - attackOrigin;
        float effectiveAngle = Mathf.Min(150f, coneAngle * RunUpgradeState.Current.Multiplier("area", this));
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f || range <= 0f) return;

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            if ((enemyLayer.value & (1 << enemy.layer)) == 0) continue;
            Vector3 direction = enemy.transform.position - attackOrigin;
            direction.y = 0f;
            EnemyHealth health = enemy.GetComponentInParent<EnemyHealth>();

            if (health != null && direction.magnitude <= range &&
                Vector3.Angle(forward, direction) <= effectiveAngle * 0.5f &&
                affectedEnemies.Add(health))
            {
                health.TakeDamage(directDamage);
            }
        }

        // Keep the visual's tip within the same radius measured from the base.
        Vector3 muzzleOffset = muzzlePosition - attackOrigin;
        muzzleOffset.y = 0f;
        float visualRange = Mathf.Max(0f, range - muzzleOffset.magnitude);
        TowerAttackVfx.PlayFlameCone(
            muzzlePosition,
            forward,
            visualRange,
            effectiveAngle
        );
    }
}
