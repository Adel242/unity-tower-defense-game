using UnityEngine;

[CreateAssetMenu(
    fileName = "NewEnemyData",
    menuName = "Tower Defense/Enemy"
)]
public class EnemyData : ScriptableObject{
    public string enemyName;

    public float maxHealth = 100f;
    public float speed = 3f;
    public float rotationSpeed = 270f;
    public float baseDamage = 10f;
}