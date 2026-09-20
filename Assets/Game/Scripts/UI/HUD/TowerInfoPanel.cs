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
            towerNameText.text =
                $"<color=#8BD5FF><b>{data.towerName}</b></color>";
            towerNameText.fontSize = 26f;
        }

        if (damageText != null){
            damageText.text = $"DAÑO   <b>{data.damage}</b>";
        }

        if (rangeText != null){
            rangeText.text = $"RANGO   <b>{data.range}</b>";
        }

        if (fireRateText != null){
            fireRateText.text = $"CADENCIA   <b>{data.fireRate}/s</b>";
        }

        if (costText != null){
            costText.text =
                $"<color=#FFD36A>COSTE   <b>{data.cost} G</b></color>";
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
