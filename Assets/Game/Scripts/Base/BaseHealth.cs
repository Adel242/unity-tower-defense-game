using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log($"Base HP: {currentHealth}");

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("GAME OVER");
    }
}