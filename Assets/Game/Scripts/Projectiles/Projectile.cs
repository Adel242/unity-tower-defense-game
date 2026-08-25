using UnityEngine;

public class Projectile : MonoBehaviour{
    [SerializeField] private float speed = 10f;

    private Transform target;
    private Vector3 lastTargetPosition;
    private float damage;

    public void SetTarget(Transform newTarget, float newDamage){
        target = newTarget;
        damage = newDamage;

        if (target != null){
            lastTargetPosition = target.position;
        }
    }

    private void Update(){
        if (target != null){
            lastTargetPosition = target.position;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            lastTargetPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, lastTargetPosition) < 0.1f){
            HitTarget();
        }
    }

    private void HitTarget(){
        if (target != null){
            EnemyHealth enemyHealth = target.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null){
                enemyHealth.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
