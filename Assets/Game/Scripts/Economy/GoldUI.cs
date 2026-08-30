using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour{
    [SerializeField] private TMP_Text goldText;

    private PlayerGold playerGold;

    private void Start(){
        playerGold = FindFirstObjectByType<PlayerGold>();

        if (playerGold == null){
            Debug.LogWarning("PlayerGold was not found.");
            return;
        }

        playerGold.GoldChanged += UpdateGoldText;

        UpdateGoldText(playerGold.CurrentGold);
    }

    private void OnDestroy(){
        if (playerGold != null){
            playerGold.GoldChanged -= UpdateGoldText;
        }
    }

    private void UpdateGoldText(int gold){
        if (goldText == null){
            return;
        }

        goldText.text = $"GOLD: {gold}";
    }
}