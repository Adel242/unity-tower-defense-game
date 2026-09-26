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

        Vector3 finalDestination = destination.position;

        if (Mathf.Abs(laneOffset) > 0.01f){
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, destination.position,
                    NavMesh.AllAreas, path) &&
                path.status == NavMeshPathStatus.PathComplete &&
                path.corners.Length > 1){
                Vector3[] corners = path.corners;
                Vector3 lastSegment = corners[corners.Length - 1]
                    - corners[corners.Length - 2];
                lastSegment.y = 0f;
                if (lastSegment.sqrMagnitude > 0.0001f){
                    Vector3 right = new Vector3(lastSegment.z, 0f,
                        -lastSegment.x).normalized;
                    Vector3 candidate = destination.position + right * laneOffset;
                    if (NavMesh.SamplePosition(candidate, out NavMeshHit hit,
                            0.35f, NavMesh.AllAreas)){
                        finalDestination = hit.position;
                    }
                }
            }
        }

        // A single destination lets NavMesh handle every bend. Chaining
        // laterally shifted corners could make agents double back at turns.
        agent.SetDestination(finalDestination);
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

        reachedDestination = true;

        if (playerBase != null){
            playerBase.TakeDamage(1f);
        }
        
        ReachedDestination?.Invoke(this);
        Destroy(gameObject);
    }
}
