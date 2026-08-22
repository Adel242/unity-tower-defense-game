using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour {
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Image healthFill;

    private float currentHealth;

    private void Awake(){
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(float damage){
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"Enemy HP: {currentHealth}");

        UpdateHealthBar();

        if (currentHealth <= 0f){
            Die();
        }
    }

    private void UpdateHealthBar(){
        if (healthFill == null){
            return;
        }

        healthFill.fillAmount = currentHealth / maxHealth;
    }

    private void Die(){
        Destroy(gameObject);
    }
}