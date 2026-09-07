using TMPro;
using UnityEngine;

public class TowerInfoPanel : MonoBehaviour{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text towerNameText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text fireRateText;
    [SerializeField] private TMP_Text costText;

    private void Awake(){
        Hide();
    }

    public void Show(TowerData data){
        if (data == null){
            Hide();
            return;
        }

        if (towerNameText != null){
            towerNameText.text = data.towerName;
        }

        if (damageText != null){
            damageText.text = $"Daño: {data.damage}";
        }

        if (rangeText != null){
            rangeText.text = $"Rango: {data.range}";
        }

        if (fireRateText != null){
            fireRateText.text = $"Disparos/s: {data.fireRate}";
        }

        if (costText != null){
            costText.text = $"Coste: {data.cost}";
        }

        if (panelRoot != null){
            panelRoot.SetActive(true);
        }
    }

    public void Hide(){
        if (panelRoot != null && panelRoot.activeSelf){
            panelRoot.SetActive(false);
        }
    }
}
