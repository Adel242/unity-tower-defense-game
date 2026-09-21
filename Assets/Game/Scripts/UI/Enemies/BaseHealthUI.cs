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
            $"<size=11><color=#9BAFC4>INTEGRIDAD</color></size>\n" +
            $"<color={healthColor}><b>{baseHealth.CurrentHealth:0}</b></color> " +
            $"<size=14><color=#9BAFC4>/ {baseHealth.MaxHealth:0}</color></size>";
        healthText.fontSize = 22f;
        healthText.raycastTarget = false;
    }
}
