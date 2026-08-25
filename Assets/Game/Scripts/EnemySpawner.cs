using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour {
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float timeBetweenEnemies = 1f;
    [SerializeField] private int enemyCount = 5;

    private IEnumerator Start(){
        for (int i = 0; i < enemyCount; i++){
            GameObject enemy = Instantiate(
                enemyPrefab,
                spawnPoint.position,
                Quaternion.identity
            );

            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
            movement.SetWaypoints(waypoints);

            yield return new WaitForSeconds(timeBetweenEnemies);
        }
    }
}