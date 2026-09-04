using System.Collections;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaveManager : MonoBehaviour{
    [SerializeField] private WaveData[] waves;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private float timeBetweenWaves = 10f;

    private int currentWaveIndex;
    private int enemiesAlive;

    private float nextWaveTimer;

    private bool waitingForFirstWave;
    private bool waitingForNextWave;
    private bool skipWait;

    public int CurrentWaveNumber => Mathf.Min(
        currentWaveIndex + 1,
        waves.Length
    );

    public float NextWaveTimer => nextWaveTimer;

    public bool WaitingForFirstWave => waitingForFirstWave;
    public bool WaitingForNextWave => waitingForNextWave;

#if UNITY_EDITOR
    private void OnValidate(){
        string[] guids = AssetDatabase.FindAssets(
            "t:WaveData",
            new[] { "Assets/Data/Waves" }
        );

        waves = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<WaveData>)
            .Where(wave => wave != null)
            .OrderBy(wave => wave.name)
            .ToArray();
    }
#endif

    private void OnEnable(){
        if (enemySpawner != null){
            enemySpawner.EnemySpawned += OnEnemySpawned;
        }
    }

    private void OnDisable(){
        if (enemySpawner != null){
            enemySpawner.EnemySpawned -= OnEnemySpawned;
        }
    }

    private void Start(){
        if (waves == null || waves.Length == 0){
            Debug.LogWarning("No waves configured.");
            return;
        }

        if (enemySpawner == null){
            Debug.LogWarning("EnemySpawner is not assigned.");
            return;
        }

        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves(){
        waitingForFirstWave = true;

        while (waitingForFirstWave){
            yield return null;
        }

        for (
            currentWaveIndex = 0;
            currentWaveIndex < waves.Length;
            currentWaveIndex++
        ){
            Debug.Log($"Starting Wave {CurrentWaveNumber}");

            yield return StartCoroutine(
                enemySpawner.SpawnWave(waves[currentWaveIndex])
            );

            while (enemiesAlive > 0){
                yield return null;
            }

            Debug.Log($"Wave {CurrentWaveNumber} completed.");

            if (currentWaveIndex < waves.Length - 1){
                yield return StartCoroutine(WaitForNextWave());
            }
        }

        Debug.Log("All waves completed.");
    }

    private IEnumerator WaitForNextWave(){
        waitingForNextWave = true;
        skipWait = false;
        nextWaveTimer = timeBetweenWaves;

        while (nextWaveTimer > 0f && !skipWait){
            nextWaveTimer -= Time.deltaTime;

            if (nextWaveTimer < 0f){
                nextWaveTimer = 0f;
            }

            yield return null;
        }

        nextWaveTimer = 0f;
        waitingForNextWave = false;
    }

    public void StartNextWaveNow(){
        if (waitingForFirstWave){
            waitingForFirstWave = false;
            return;
        }

        if (waitingForNextWave){
            skipWait = true;
        }
    }

    private void OnEnemySpawned(GameObject enemy){
        enemiesAlive++;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();

        if (health != null){
            health.Died += OnEnemyDied;
        }

        if (movement != null){
            movement.ReachedDestination += OnEnemyReachedDestination;
        }
    }

    private void OnEnemyDied(EnemyHealth enemyHealth){
        enemyHealth.Died -= OnEnemyDied;

        EnemyMovement movement =
            enemyHealth.GetComponent<EnemyMovement>();

        if (movement != null){
            movement.ReachedDestination -=
                OnEnemyReachedDestination;
        }

        EnemyRemoved();
    }

    private void OnEnemyReachedDestination(
        EnemyMovement enemyMovement
    ){
        enemyMovement.ReachedDestination -=
            OnEnemyReachedDestination;

        EnemyHealth health =
            enemyMovement.GetComponent<EnemyHealth>();

        if (health != null){
            health.Died -= OnEnemyDied;
        }

        EnemyRemoved();
    }

    private void EnemyRemoved(){
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
    }
}
