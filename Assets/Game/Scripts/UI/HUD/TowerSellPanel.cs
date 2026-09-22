using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TowerSellPanel : MonoBehaviour{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellLabel;
    [SerializeField, Range(0f, 1f)] private float refundRatio = 0.7f;

    private TowerSelectionManager selectionManager;

    private void Awake(){
        if (sellButton != null){ sellButton.onClick.AddListener(SellSelectedTower); }
        Hide();
    }

    private void OnDestroy(){
        if (sellButton != null){ sellButton.onClick.RemoveListener(SellSelectedTower); }
    }

    public void Show(TowerTargeting tower, TowerSelectionManager manager){
        if (tower == null || tower.TowerData == null || panelRoot == null){
            Hide();
            return;
        }

        selectionManager = manager;
        int refund = Mathf.Max(1, Mathf.RoundToInt(tower.TowerData.Cost * refundRatio));
        if (sellLabel != null){
            sellLabel.text = $"<b>VENDER TORRE</b>  <color=#F5CA70>+{refund} G</color>";
        }
        panelRoot.SetActive(true);
    }

    public void Hide(){
        selectionManager = null;
        if (panelRoot != null){ panelRoot.SetActive(false); }
    }

    private void SellSelectedTower(){
        selectionManager?.SellSelectedTower(refundRatio);
    }
}
