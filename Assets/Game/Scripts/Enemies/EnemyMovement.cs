using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour{
    private static readonly List<EnemyMovement> activeEnemies = new();

    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform targetPoint;
    public event System.Action<EnemyMovement> ReachedDestination;
    private bool hasDestination;

    public Transform TargetPoint => targetPoint;
    public static IReadOnlyList<EnemyMovement> ActiveEnemies => activeEnemies;

    private NavMeshAgent agent;
    private BaseHealth playerBase;
    private bool reachedDestination;
    private float speedMultiplier = 1f;
    private Vector3[] laneWaypoints;
    private int laneWaypointIndex;

    public Vector3 Velocity => agent != null ? agent.velocity : Vector3.zero;

private void Awake(){
    agent = GetComponent<NavMeshAgent>();

    if (enemyData != null){
        agent.speed = enemyData.speed;
        agent.angularSpeed = enemyData.rotationSpeed;
    }

    agent.updateRotation = true;
    agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
}

    private void OnEnable(){
        if (!activeEnemies.Contains(this)){
            activeEnemies.Add(this);
        }
    }

    private void OnDisable(){
        activeEnemies.Remove(this);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveEnemies(){
        activeEnemies.Clear();
    }

    private void Start(){
        playerBase = FindFirstObjectByType<BaseHealth>();
    }

    private void Update(){
        if (enemyData == null || reachedDestination || !hasDestination){
            return;
        }

        CheckDestinationReached();
    }

    public void SetDestination(Transform destination, float laneOffset = 0f){
        if (destination == null){
            return;
        }

        if (!agent.isOnNavMesh){
            Debug.LogWarning($"{name} is not on the NavMesh.");
            return;
        }

        laneWaypoints = null;
        laneWaypointIndex = 0;

        if (Mathf.Abs(laneOffset) > 0.01f){
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, destination.position,
                    NavMesh.AllAreas, path) &&
                path.status == NavMeshPathStatus.PathComplete &&
                path.corners.Length > 1){
                Vector3[] corners = path.corners;
                laneWaypoints = new Vector3[corners.Length - 1];
                for (int index = 1; index < corners.Length; index++){
                    Vector3 tangent = corners[Mathf.Min(index + 1, corners.Length - 1)]
                        - corners[index - 1];
                    tangent.y = 0f;
                    Vector3 right = new Vector3(tangent.z, 0f, -tangent.x).normalized;
                    Vector3 candidate = corners[index] + right * laneOffset;
                    laneWaypoints[index - 1] = NavMesh.SamplePosition(candidate,
                        out NavMeshHit hit, 0.35f, NavMesh.AllAreas)
                        ? hit.position : corners[index];
                }
            }
        }

        agent.SetDestination(laneWaypoints != null
            ? laneWaypoints[0] : destination.position);
        hasDestination = true;
    }

    public void ApplyWaveSpeed(float multiplier){
        speedMultiplier = Mathf.Max(0.1f, multiplier);

        if (agent != null && enemyData != null){
            agent.speed = enemyData.speed * speedMultiplier;
        }
    }

    private void CheckDestinationReached(){
        if (agent.pathPending){
            return;
        }

        if (agent.remainingDistance > agent.stoppingDistance + 0.1f){
            return;
        }

        if (agent.hasPath && agent.velocity.sqrMagnitude > 0.01f){
            return;
        }

        if (laneWaypoints != null && laneWaypointIndex < laneWaypoints.Length - 1){
            laneWaypointIndex++;
            agent.SetDestination(laneWaypoints[laneWaypointIndex]);
            return;
        }

        reachedDestination = true;

        if (playerBase != null){
            playerBase.TakeDamage(1f);
        }
        
        ReachedDestination?.Invoke(this);
        Destroy(gameObject);
    }
}
