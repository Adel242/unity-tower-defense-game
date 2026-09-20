using UnityEngine;

[CreateAssetMenu(
    fileName = "WaveData",
    menuName = "Tower Defense/Wave Data"
)]
public class WaveData : ScriptableObject{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int enemyCount = 5;
    [SerializeField] private float timeBetweenEnemies = 1f;
    [SerializeField, Min(0.1f)] private float healthMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float speedMultiplier = 1f;
    [SerializeField, Min(0f)] private float goldRewardMultiplier = 1f;

    public GameObject EnemyPrefab => enemyPrefab;
    public int EnemyCount => enemyCount;
    public float TimeBetweenEnemies => timeBetweenEnemies;
    public float HealthMultiplier => healthMultiplier;
    public float SpeedMultiplier => speedMultiplier;
    public float GoldRewardMultiplier => goldRewardMultiplier;
}
