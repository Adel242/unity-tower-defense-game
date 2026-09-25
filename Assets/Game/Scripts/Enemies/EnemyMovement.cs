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

    public void SetDestination(Transform destination){
        if (destination == null){
            return;
        }

        if (!agent.isOnNavMesh){
            Debug.LogWarning($"{name} is not on the NavMesh.");
            return;
        }

        agent.SetDestination(destination.position);
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
