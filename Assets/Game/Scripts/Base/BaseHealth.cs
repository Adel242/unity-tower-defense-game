using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    public event System.Action<float, float> HealthChanged;
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || currentHealth <= 0f) return;
        float previousHealth = currentHealth;
        currentHealth -= damage;

        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }

        HealthChanged?.Invoke(currentHealth, previousHealth - currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("GAME OVER");
    }
}
