using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Image healthFill;
    [SerializeField] private DamagePopup damagePopupPrefab;

    private float currentHealth;

    private void Awake(){
        if (enemyData == null){
            return;
        }

        currentHealth = enemyData.maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(float damage){
        if (enemyData == null){
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"Enemy HP: {currentHealth}");

        UpdateHealthBar();
        ShowDamagePopup(damage);

        if (currentHealth <= 0f){
            Die();
        }
    }

    private void UpdateHealthBar(){
        if (healthFill == null || enemyData == null){
            return;
        }

        healthFill.fillAmount = currentHealth / enemyData.maxHealth;
    }

    private void ShowDamagePopup(float damage){
        if (damagePopupPrefab == null){
            return;
        }

        Vector3 popupPosition = transform.position + Vector3.up * 2.3f;

        DamagePopup popup = Instantiate(
            damagePopupPrefab,
            popupPosition,
            Quaternion.identity,
            null
        );

        popup.Setup(damage);
    }

    private void Die(){
        Destroy(gameObject);
    }
}
