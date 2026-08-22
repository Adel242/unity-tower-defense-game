using UnityEngine;

[CreateAssetMenu(
    fileName = "NewTowerData",
    menuName = "Tower Defense/Tower Data"
)]
public class TowerData : ScriptableObject
{
    public string towerName;

    public float damage = 5f;
    public float range = 8f;
    public float fireRate = 2f;

    public float rotationSpeed = 250f;
    public float searchRotationSpeed = 100f;
    public float aimTolerance = 20f;

    public float firstShotDelay = 0.25f;
}