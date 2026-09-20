using TMPro;
using UnityEngine;

public class BaseHealthUI : MonoBehaviour{
    [SerializeField] private TMP_Text healthText;

    private BaseHealth baseHealth;

    private void Start(){
        baseHealth = FindFirstObjectByType<BaseHealth>();

        if (baseHealth == null){
            Debug.LogWarning("BaseHealth was not found.");
        }
    }

    private void Update(){
        if (baseHealth == null || healthText == null){
            return;
        }

        float healthRatio = baseHealth.MaxHealth > 0f
            ? baseHealth.CurrentHealth / baseHealth.MaxHealth
            : 0f;
        string healthColor = healthRatio > 0.5f
            ? "#7EE787"
            : healthRatio > 0.25f ? "#FFD36A" : "#FF6B6B";

        healthText.text =
            $"<mark=#111827E6><color={healthColor}><b>  BASE  " +
            $"{baseHealth.CurrentHealth:0} / {baseHealth.MaxHealth:0}  " +
            "</b></color></mark>";
        healthText.fontSize = 22f;
        healthText.raycastTarget = false;
    }
}
