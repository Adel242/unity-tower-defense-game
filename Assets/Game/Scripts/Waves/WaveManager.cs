using System;
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
    [Header("Run upgrades")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private UnityEngine.UI.Button[] upgradeButtons;
    [SerializeField] private TMPro.TMP_Text[] upgradeLabels;
    [SerializeField] private TMPro.TMP_Text upgradeTitle;
    private RunUpgradeState.Choice[] offeredUpgrades;

    private void Awake(){
        RunUpgradeState.Reset();
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (upgradeButtons != null)
            for (int i = 0; i < upgradeButtons.Length; i++){
                int index = i;
                upgradeButtons[i].onClick.AddListener(() => ChooseUpgrade(index));
            }
    }

    private void OnDestroy(){ RunUpgradeState.Reset(); }

    private void Update(){
        // Let the existing pause menu remain visible and usable above the draft.
        if (RunUpgradeState.Current.Choosing && upgradePanel != null)
            upgradePanel.SetActive(Time.timeScale > 0f);
    }

    private IEnumerator OfferUpgrades(int milestone){
        if (upgradePanel == null || upgradeButtons == null || upgradeButtons.Length != 3 ||
            upgradeLabels == null || upgradeLabels.Length != 3){
            Debug.LogError("Upgrade cards are not configured on WaveManager.");
            yield break;
        }
        offeredUpgrades = RunUpgradeState.Current.Draw();
        if (offeredUpgrades.Length == 0) yield break;
        FindFirstObjectByType<TowerPlacementManager>()?.CancelBuildMode();
        RunUpgradeState.Current.Choosing = true;
        upgradeTitle.text = milestone == 1 ? "ELIGE TU PRIMERA MEJORA" : $"OLEADA {milestone} COMPLETADA · ELIGE UNA MEJORA";
        for (int i = 0; i < 3; i++){
            upgradeButtons[i].gameObject.SetActive(i < offeredUpgrades.Length);
            if (i >= offeredUpgrades.Length) continue;
            var choice = offeredUpgrades[i];
            upgradeLabels[i].text = $"<size=26><b>{choice.Title}</b></size>\n\n{choice.Description}\n\n<size=16>Nivel {choice.Stacks + 1}/{choice.Limit}\n\nELEGIR</size>";
        }
        upgradePanel.SetActive(true);
        upgradeButtons[0].Select();
        while (RunUpgradeState.Current.Choosing) yield return null;
        upgradePanel.SetActive(false);
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
    }

    private void ChooseUpgrade(int index){
        if (!RunUpgradeState.Current.Choosing || Time.timeScale == 0f ||
            offeredUpgrades == null || index < 0 || index >= offeredUpgrades.Length) return;
        RunUpgradeState.Current.Apply(offeredUpgrades[index]);
        upgradePanel.SetActive(false);
    }

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

    public int CurrentWaveNumber => Mathf.Min(
        currentWaveIndex + 1,
        TotalWaveCount
    );

    public float NextWaveTimer => nextWaveTimer;

    public int TotalWaveCount => waves != null ? waves.Length : 0;
    public int CompletedWaveCount => completedWaveCount;
    public bool WaveActive => waveActive;
    public float CurrentWaveProgress => currentWaveEnemyCount > 0
        ? Mathf.Clamp01(
            (float)currentWaveEnemiesRemoved / currentWaveEnemyCount
        )
        : 0f;

    public bool WaitingForFirstWave => waitingForFirstWave;
    public bool WaitingForNextWave => waitingForNextWave;

    public event Action<int> WaveStarted;

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
        yield return OfferUpgrades(1);

        while (waitingForFirstWave){
            yield return null;
        }

        for (
            currentWaveIndex = 0;
            currentWaveIndex < waves.Length;
            currentWaveIndex++
        ){
            currentWaveEnemyCount = waves[currentWaveIndex].EnemyCount;
            currentWaveEnemiesRemoved = 0;
            waveActive = true;
            Debug.Log($"Starting Wave {CurrentWaveNumber}");
            WaveStarted?.Invoke(CurrentWaveNumber);

            yield return StartCoroutine(
                enemySpawner.SpawnWave(waves[currentWaveIndex])
            );

            while (enemiesAlive > 0){
                yield return null;
            }

            currentWaveEnemiesRemoved = currentWaveEnemyCount;
            completedWaveCount = currentWaveIndex + 1;
            waveActive = false;
            Debug.Log($"Wave {CurrentWaveNumber} completed.");
            if (completedWaveCount % 5 == 0) yield return OfferUpgrades(completedWaveCount);
            // One-shot audio owns its short lifetime and fade, including the
            // final impact. Ending the wave must not cut that tail off.

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
