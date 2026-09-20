using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerBuildButton : MonoBehaviour{
    [SerializeField] private GameObject towerPrefab;

    private TowerPlacementManager placementManager;
    private PlayerGold playerGold;
    private Button button;
    private int towerCost;

    private void Start(){
        placementManager =
            FindFirstObjectByType<TowerPlacementManager>();

        if (placementManager == null){
            Debug.LogWarning(
                "TowerPlacementManager was not found."
            );
        }

        playerGold = FindFirstObjectByType<PlayerGold>();
        button = GetComponent<Button>();

        UpdateVisuals();

        if (playerGold != null){
            playerGold.GoldChanged += UpdateAffordability;
            UpdateAffordability(playerGold.CurrentGold);
        }
    }

    private void OnDestroy(){
        if (playerGold != null){
            playerGold.GoldChanged -= UpdateAffordability;
        }
    }

    public void StartBuildMode(){
        if (
            placementManager == null ||
            towerPrefab == null ||
            playerGold == null ||
            !playerGold.CanAfford(towerCost)
        ){
            return;
        }

        placementManager.StartBuildMode(towerPrefab);
    }

    private void UpdateVisuals(){
        if (towerPrefab == null){
            return;
        }

        TowerTargeting targeting =
            towerPrefab.GetComponent<TowerTargeting>();

        if (targeting == null || targeting.TowerData == null){
            return;
        }

        towerCost = targeting.TowerData.cost;

        TMP_Text label = GetComponentInChildren<TMP_Text>();

        if (label != null){
            TowerData data = targeting.TowerData;
            label.text =
                $"<mark=#111827D9><b>{data.towerName}</b></mark>\n" +
                $"<mark=#111827D9><color=#FFD36A>{data.cost} G</color></mark>";
            label.alignment = TextAlignmentOptions.Bottom;
            label.fontSize = 14f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 14f;
            label.margin = new Vector4(4f, 4f, 4f, 5f);
            label.outlineColor = new Color32(10, 12, 18, 255);
            label.outlineWidth = 0.18f;
            label.raycastTarget = false;
        }

        Image icon = GetComponent<Image>();

        if (icon != null){
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        if (button != null){
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(225, 231, 239, 255);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color32(155, 170, 190, 255);
            colors.selectedColor = new Color32(205, 225, 245, 255);
            colors.disabledColor = new Color32(80, 88, 102, 150);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }
    }

    private void UpdateAffordability(int currentGold){
        if (button == null){
            return;
        }

        button.interactable = towerCost > 0 && currentGold >= towerCost;
    }
}
