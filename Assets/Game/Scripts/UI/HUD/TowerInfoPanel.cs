using System.Collections;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

public class TowerInfoPanel : MonoBehaviour{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text towerNameText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text fireRateText;
    [SerializeField] private TMP_Text costText;
    private CanvasGroup panelGroup;
    private RectTransform panelRect;
    private Vector2 restingPosition;
    private Coroutine reveal;
    private MMF_Player selectionFeedbacks;
    private TowerData displayedData;
    private TowerData selectedData;
    private TowerData previewData;

    private void Awake(){
        if (panelRoot == null){ return; }
        panelRect = panelRoot.GetComponent<RectTransform>();
        restingPosition = panelRect.anchoredPosition;
        panelGroup = panelRoot.GetComponent<CanvasGroup>();
        if (panelGroup == null){ panelGroup = panelRoot.AddComponent<CanvasGroup>(); }
        selectionFeedbacks = gameObject.AddComponent<MMF_Player>();
        selectionFeedbacks.AddFeedback(new MMF_Scale{
            AnimateScaleTarget = panelRect,
            Mode = MMF_Scale.Modes.Additive,
            AnimateScaleDuration = 0.24f,
            RemapCurveZero = 0f,
            RemapCurveOne = 0.015f,
            UniformScaling = true,
            AllowAdditivePlays = false,
            DetermineScaleOnPlay = false,
            Timing = new MMFeedbackTiming{ TimescaleMode = TimescaleModes.Unscaled }
        });
        selectionFeedbacks.Initialization();
        HidePanel();
    }

public void Show(TowerData data){
    selectedData = data;
    RefreshDisplay();
}

public void ShowPreview(TowerData data){
    if (data == null){ return; }
    previewData = data;
    RefreshDisplay();
}

public void ClearPreview(TowerData data){
    if (previewData != data){ return; }
    previewData = null;
    RefreshDisplay();
}

private void RefreshDisplay(){
    TowerData data = previewData != null ? previewData : selectedData;

    if (data == null || panelRoot == null){
        HidePanel();
        return;
    }

    bool changed = displayedData != data || !panelRoot.activeSelf;
    displayedData = data;

    SetText(towerNameText, $"<size=10><color=#7994AC>TORRE</color></size>\n<b>{data.towerName}</b>", 21f);
    SetText(damageText, $"<size=10><color=#9BAFC4>DAÑO</color></size>\n<b>{data.Damage:0.#}</b>", 24f);
    SetText(rangeText, $"<size=10><color=#9BAFC4>ALCANCE</color></size>\n<b>{data.Range:0.#}</b>", 24f);
    SetText(fireRateText, $"<size=10><color=#9BAFC4>CADENCIA</color></size>\n<b>{data.FireRate:0.##}</b><size=13> /s</size>", 24f);
    SetText(costText, $"<size=10><color=#9BAFC4>COSTE</color></size>\n<color=#F5CA70><b>{data.Cost}</b><size=13> G</size></color>", 24f);

    panelRoot.SetActive(true);

    if (!changed){
        return;
    }

    if (reveal != null){
        StopCoroutine(reveal);
    }

    selectionFeedbacks.StopFeedbacks();
    panelRect.localScale = Vector3.one;
    selectionFeedbacks.PlayFeedbacks();
    reveal = StartCoroutine(Reveal());
}
    private IEnumerator Reveal(){
        float elapsed = 0f;
        panelGroup.alpha = 0f;
        while (elapsed < 0.2f){
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.2f);
            panelGroup.alpha = t;
            panelRect.anchoredPosition = restingPosition + Vector2.left * (10f * (1f - t));
            yield return null;
        }
        panelGroup.alpha = 1f;
        panelRect.anchoredPosition = restingPosition;
        reveal = null;
    }

    public void Hide(){
        if (selectedData == null){ return; }
        selectedData = null;
        RefreshDisplay();
    }

    private void HidePanel(){
        if (reveal != null){ StopCoroutine(reveal); reveal = null; }
        if (panelRoot != null && panelRoot.activeSelf){
            selectionFeedbacks?.StopFeedbacks();
            if (panelRect != null){
                panelRect.localScale = Vector3.one;
                panelRect.anchoredPosition = restingPosition;
            }
            panelRoot.SetActive(false);
        }
        displayedData = null;
    }

    private void OnDisable(){
        selectedData = null;
        previewData = null;
        HidePanel();
    }

    private static void SetText(TMP_Text label, string value, float size){
        if (label == null){ return; }
        label.text = value;
        label.fontSize = size;
        label.fontStyle = FontStyles.Normal;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color32(226, 239, 250, 255);
        label.margin = Vector4.zero;
        label.raycastTarget = false;
    }
}
