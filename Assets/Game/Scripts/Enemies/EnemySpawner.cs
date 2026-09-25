using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour{
    [SerializeField] private BoxCollider spawnZone;
    [SerializeField] private Transform destinationPoint;
    [SerializeField, Min(0f)] private float minimumSpawnDistance = 0.3f;
    public event System.Action<GameObject> EnemySpawned;

    public IEnumerator SpawnWave(
        GameObject enemyPrefab,
        int enemyCount,
        int spawnBatchSize,
        float timeBetweenEnemies,
        float healthMultiplier,
        float speedMultiplier,
        float goldRewardMultiplier
    ){
        if (enemyPrefab == null){ yield break; }

        spawnBatchSize = Mathf.Max(1, spawnBatchSize);
        for (int i = 0; i < enemyCount; i++){
            Vector3 spawnPosition;

            while (!TryGetRandomSpawnPosition(out spawnPosition)){
                yield return new WaitForSeconds(0.1f);
            }

            GameObject enemy = Instantiate(
                enemyPrefab,
                spawnPosition,
                Quaternion.identity
            );

            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();

            health?.ApplyWaveScaling(
                healthMultiplier,
                goldRewardMultiplier
            );
            movement?.ApplyWaveSpeed(speedMultiplier);

            EnemySpawned?.Invoke(enemy);

            if (movement != null){
                movement.SetDestination(destinationPoint);
            }

            bool batchCompleted = (i + 1) % spawnBatchSize == 0;
            if (i < enemyCount - 1 && batchCompleted){
                yield return new WaitForSeconds(timeBetweenEnemies);
            }
        }
    }

    private bool TryGetRandomSpawnPosition(out Vector3 spawnPosition){
        Bounds bounds = spawnZone.bounds;

        for (int i = 0; i < 50; i++){
            Vector3 randomPosition = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (!NavMesh.SamplePosition(
                randomPosition,
                out NavMeshHit hit,
                0.2f,
                NavMesh.AllAreas
            )){
                continue;
            }

            bool spawnBlocked = Physics.CheckSphere(
                hit.position,
                minimumSpawnDistance,
                LayerMask.GetMask("Enemy"),
                QueryTriggerInteraction.Ignore
            );

            if (spawnBlocked){
                continue;
            }

            spawnPosition = hit.position;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }
}
