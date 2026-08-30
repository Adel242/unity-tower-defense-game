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

        healthText.text =
            $"BASE HP: {baseHealth.CurrentHealth:0} / {baseHealth.MaxHealth:0}";
    }
}