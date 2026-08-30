using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour{
    [SerializeField] private BoxCollider spawnZone;
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private float minimumSpawnDistance = 0.7f;
    public event System.Action<GameObject> EnemySpawned;

    public IEnumerator SpawnWave(WaveData waveData){
        for (int i = 0; i < waveData.EnemyCount; i++){
            Vector3 spawnPosition;

            while (!TryGetRandomSpawnPosition(out spawnPosition)){
                yield return new WaitForSeconds(0.1f);
            }

            GameObject enemy = Instantiate(
                waveData.EnemyPrefab,
                spawnPosition,
                Quaternion.identity
            );

            EnemySpawned?.Invoke(enemy);

            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();

            if (movement != null){
                movement.SetDestination(destinationPoint);
            }

            yield return new WaitForSeconds(waveData.TimeBetweenEnemies);
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

            Collider[] nearbyEnemies = Physics.OverlapSphere(
                hit.position,
                minimumSpawnDistance,
                LayerMask.GetMask("Enemy")
            );

            if (nearbyEnemies.Length > 0){
                continue;
            }

            spawnPosition = hit.position;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }
}
