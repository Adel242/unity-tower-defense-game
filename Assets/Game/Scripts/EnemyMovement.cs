using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotationSpeed = 270f;
    [SerializeField] private Transform targetPoint;
    [SerializeField] private float baseDamage = 10f;
    private BaseHealth playerBase;

    public Transform TargetPoint => targetPoint;

    private int currentWaypointIndex = 0;

    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        Transform target = waypoints[currentWaypointIndex];

        RotateVisualTowards(target);

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length){
                if (playerBase != null){
                    playerBase.TakeDamage(baseDamage);
                }
                Destroy(gameObject);
            }
        }
    }

    private void RotateVisualTowards(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        visualRoot.rotation = Quaternion.RotateTowards(
            visualRoot.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void Start()
    {
        playerBase = FindFirstObjectByType<BaseHealth>();
    }
}