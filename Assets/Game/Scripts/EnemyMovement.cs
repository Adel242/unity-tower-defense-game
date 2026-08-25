using UnityEngine;

public class EnemyMovement : MonoBehaviour{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform targetPoint;

    public Transform TargetPoint => targetPoint;

    private int currentWaypointIndex = 0;
    private BaseHealth playerBase;

    private void Start(){
        playerBase = FindFirstObjectByType<BaseHealth>();
    }

    public void SetWaypoints(Transform[] newWaypoints){
        waypoints = newWaypoints;
    }

    private void Update(){
        if (enemyData == null){
            return;
        }

        if (waypoints == null || waypoints.Length == 0){
            return;
        }

        Transform target = waypoints[currentWaypointIndex];

        RotateVisualTowards(target);

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            enemyData.speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < 0.1f){
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length){
                if (playerBase != null){
                    playerBase.TakeDamage(enemyData.baseDamage);
                }

                Destroy(gameObject);
            }
        }
    }

    private void RotateVisualTowards(Transform target){
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f){
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        visualRoot.rotation = Quaternion.RotateTowards(
            visualRoot.rotation,
            targetRotation,
            enemyData.rotationSpeed * Time.deltaTime
        );
    }
}
