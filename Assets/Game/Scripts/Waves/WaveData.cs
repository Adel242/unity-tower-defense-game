using UnityEngine;

[CreateAssetMenu(
    fileName = "WaveData",
    menuName = "Tower Defense/Wave Data"
)]
public class WaveData : ScriptableObject{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int enemyCount = 5;
    [SerializeField] private float timeBetweenEnemies = 1f;

    public GameObject EnemyPrefab => enemyPrefab;
    public int EnemyCount => enemyCount;
    public float TimeBetweenEnemies => timeBetweenEnemies;
}
