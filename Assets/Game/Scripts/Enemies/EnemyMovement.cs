using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform targetPoint;
    public event System.Action<EnemyMovement> ReachedDestination;
    private bool hasDestination;

    public Transform TargetPoint => targetPoint;

    private NavMeshAgent agent;
    private BaseHealth playerBase;
    private bool reachedDestination;

private void Awake(){
    agent = GetComponent<NavMeshAgent>();

    if (enemyData != null){
        agent.speed = enemyData.speed;
        agent.angularSpeed = enemyData.rotationSpeed;
    }

    agent.updateRotation = true;
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
            playerBase.TakeDamage(enemyData.baseDamage);
        }
        
        ReachedDestination?.Invoke(this);
        Destroy(gameObject);
    }
}
