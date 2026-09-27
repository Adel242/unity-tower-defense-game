using System;
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private float timeBetweenWaves = 10f;
    [Header("Infinite waves")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField, Min(1)] private int baseEnemyCount = 24;
    [SerializeField, Min(0f)] private float enemiesAddedPerWave = 2.5f;
    [SerializeField, Range(0f, 0.4f)] private float enemyCountVariance = 0.14f;
    [SerializeField, Min(1f)] private float swarmCountMultiplier = 1.45f;
    [SerializeField, Min(1f)] private float fastCountMultiplier = 1.15f;
    [SerializeField, Range(0.1f, 1f)] private float specialCountMultiplier = 0.82f;
    [SerializeField, Min(1)] private int baseSpawnBatchSize = 1;
    [SerializeField, Min(0.01f)] private float initialSpawnInterval = 0.65f;
    [SerializeField, Min(0.01f)] private float minimumSpawnInterval = 0.3f;
    [SerializeField, Range(0.8f, 1f)] private float spawnIntervalDecay = 0.985f;
    [SerializeField, Min(0.1f)] private float initialHealthMultiplier = 0.55f;
    [SerializeField, Min(1f)] private float healthGrowthPerWave = 2.08f;
    [SerializeField, Range(0.1f, 1f)] private float swarmHealthMultiplier = 0.85f;
    [SerializeField, Range(0.1f, 1f)] private float fastHealthMultiplier = 0.9f;
    [SerializeField, Min(1f)] private float maximumHealthMultiplier = 1000000f;
    [SerializeField, Min(0f)] private float speedAddedPerWave = 0.015f;
    [SerializeField, Min(0.1f)] private float maximumSpeedMultiplier = 1.7f;
    [SerializeField, Min(0f)] private float initialGoldRewardMultiplier = 0.35f;
    [SerializeField, Min(0f)] private float rewardGrowthEveryFiveWaves = 0.025f;
    [SerializeField, Min(1f)] private float specialRewardMultiplier = 1.1f;
    [SerializeField] private UpgradeSelectionPanel upgradeSelectionPanel;
    [SerializeField] private SpecialWaveAtmosphere specialWaveAtmosphere;
    private bool gameplayReady;

    private int currentWaveIndex;
    private int enemiesAlive;
    private int currentWaveEnemyCount;
    private int currentWaveEnemiesRemoved;
    private int completedWaveCount;
    private bool waveActive;

    private float nextWaveTimer;

    private bool waitingForFirstWave;
    private bool waitingForNextWave;
    private bool skipWait;

    public int CurrentWaveNumber => currentWaveIndex + 1;

    public float NextWaveTimer => nextWaveTimer;

    // The progress rail only needs an upper bound to keep five future slots visible.
    public int TotalWaveCount => int.MaxValue;
    public int CompletedWaveCount => completedWaveCount;
    public bool WaveActive => waveActive;
    public float CurrentWaveProgress => currentWaveEnemyCount > 0
        ? Mathf.Clamp01(
            (float)currentWaveEnemiesRemoved / currentWaveEnemyCount
        )
        : 0f;

    public bool WaitingForFirstWave => waitingForFirstWave;
    public bool WaitingForNextWave => waitingForNextWave;
    public bool GameplayReady => gameplayReady;

    public event Action<int> WaveStarted;

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
        if (upgradeSelectionPanel == null){
            Debug.LogError("UpgradeSelectionPanel is not assigned.", this);
            return;
        }
        if (enemyPrefab == null){
            Debug.LogWarning("No enemy prefab is configured for infinite waves.");
            return;
        }

        if (enemySpawner == null){
            Debug.LogWarning("EnemySpawner is not assigned.");
            return;
        }

        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves(){
        gameplayReady = false;
        yield return upgradeSelectionPanel.ShowMission();
        gameplayReady = true;
        waitingForFirstWave = true;

        while (waitingForFirstWave){
            yield return null;
        }
        yield return upgradeSelectionPanel.OfferUpgrades(1);

        for (currentWaveIndex = 0; ; currentWaveIndex++){
            InfiniteWave wave = GenerateWave(CurrentWaveNumber);
            currentWaveEnemyCount = wave.EnemyCount;
            currentWaveEnemiesRemoved = 0;
            waveActive = true;
            if (CurrentWaveNumber % 5 == 0) specialWaveAtmosphere?.Begin();
            Debug.Log($"Starting Wave {CurrentWaveNumber}");
            WaveStarted?.Invoke(CurrentWaveNumber);

            yield return StartCoroutine(
                enemySpawner.SpawnWave(
                    enemyPrefab,
                    wave.EnemyCount,
                    wave.SpawnBatchSize,
                    wave.SpawnInterval,
                    wave.HealthMultiplier,
                    wave.SpeedMultiplier,
                    wave.GoldRewardMultiplier
                )
            );

            while (enemiesAlive > 0){
                yield return null;
            }

            currentWaveEnemiesRemoved = currentWaveEnemyCount;
            completedWaveCount = currentWaveIndex + 1;
            waveActive = false;
            if (CurrentWaveNumber % 5 == 0) specialWaveAtmosphere?.End();
            Debug.Log($"Wave {CurrentWaveNumber} completed.");
            if (completedWaveCount % 5 == 0) yield return upgradeSelectionPanel.OfferUpgrades(completedWaveCount);
            // One-shot audio owns its short lifetime and fade, including the
            // final impact. Ending the wave must not cut that tail off.

            yield return StartCoroutine(WaitForNextWave());
        }
    }

    private InfiniteWave GenerateWave(int waveNumber){
        int step = Mathf.Max(0, waveNumber - 1);
        bool specialWave = waveNumber % 5 == 0;
        bool swarmWave = !specialWave && waveNumber % 3 == 0;
        bool fastWave = !specialWave && waveNumber % 4 == 0;

        float countVariation = 1f + Mathf.Sin(
            waveNumber * 1.37f + 1.77f
        ) * enemyCountVariance;
        float typeCountMultiplier = specialWave
            ? specialCountMultiplier
            : swarmWave
                ? swarmCountMultiplier
                : fastWave
                    ? fastCountMultiplier
                    : 1f;
        int enemyCount = Mathf.RoundToInt(
            (baseEnemyCount + step * enemiesAddedPerWave) *
            countVariation * typeCountMultiplier
        );
        enemyCount = Mathf.Clamp(enemyCount, 1, 400);

        int batchSize = Mathf.Max(1, baseSpawnBatchSize);

        float health = initialHealthMultiplier * Mathf.Pow(healthGrowthPerWave, step);
        if (swarmWave){ health *= swarmHealthMultiplier; }
        if (fastWave && !swarmWave){ health *= fastHealthMultiplier; }
        if (specialWave){ health *= 1.4f; }
        health = Mathf.Clamp(health, 0.1f, maximumHealthMultiplier);

        float speed = 1f + step * speedAddedPerWave;
        if (fastWave){ speed *= 1.12f; }
        if (specialWave){ speed *= 0.92f; }
        speed = Mathf.Clamp(speed, 0.75f, maximumSpeedMultiplier);

        float interval = initialSpawnInterval * Mathf.Pow(spawnIntervalDecay, step);
        if (swarmWave){ interval *= 0.8f; }
        if (specialWave){ interval *= 1.1f; }
        interval = Mathf.Max(minimumSpawnInterval, interval);

        float reward = initialGoldRewardMultiplier +
                       Mathf.Floor(step / 5f) * rewardGrowthEveryFiveWaves;
        if (specialWave){ reward *= specialRewardMultiplier; }
        reward = Mathf.Min(1.5f, reward);

        return new InfiniteWave(
            enemyCount,
            batchSize,
            interval,
            health,
            speed,
            reward
        );
    }

    private readonly struct InfiniteWave{
        public readonly int EnemyCount;
        public readonly int SpawnBatchSize;
        public readonly float SpawnInterval;
        public readonly float HealthMultiplier;
        public readonly float SpeedMultiplier;
        public readonly float GoldRewardMultiplier;

        public InfiniteWave(int count, int batchSize, float interval,
            float health, float speed, float reward){
            EnemyCount = count;
            SpawnBatchSize = batchSize;
            SpawnInterval = interval;
            HealthMultiplier = health;
            SpeedMultiplier = speed;
            GoldRewardMultiplier = reward;
        }
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
        if (RunUpgradeState.Current.BlocksInput) return;
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
        currentWaveEnemiesRemoved = Mathf.Min(
            currentWaveEnemyCount,
            currentWaveEnemiesRemoved + 1
        );
    }
}
