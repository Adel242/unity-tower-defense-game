using TMPro;
using UnityEngine;

public class BaseHealthUI : MonoBehaviour
{
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private TMP_Text healthText;

    private void Update()
    {
        if (baseHealth == null || healthText == null)
        {
            return;
        }

        healthText.text =
            $"BASE HP: {baseHealth.CurrentHealth:0} / {baseHealth.MaxHealth:0}";
    }
}