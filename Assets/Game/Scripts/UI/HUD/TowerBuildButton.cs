using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TowerBuildButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler{
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private Image towerIcon;

    private TowerPlacementManager placementManager;
    private PlayerGold playerGold;
    private Button button;
    private int towerCost;
    private TMP_Text label;
    private bool hovered;
    private bool focused;
    private Vector3 restingScale;
    private RunUpgradeState upgrades;
    private TowerInfoPanel infoPanel;
    private TowerData towerData;
    private int shortcutNumber;

    private void Awake(){
        restingScale = transform.localScale;
        shortcutNumber = transform.GetSiblingIndex() + 1;
    }

    private void Update(){
        if (IsShortcutPressed()){
            StartBuildMode();
        }

        bool highlighted = button != null && button.interactable && (hovered || focused);
        Vector3 targetScale = restingScale * (highlighted ? 1.035f : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale,
            1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData){
        hovered = true;
        if (infoPanel == null){ infoPanel = FindFirstObjectByType<TowerInfoPanel>(); }
        infoPanel?.ShowPreview(towerData);
    }
    public void OnPointerExit(PointerEventData eventData){
        hovered = false;
        infoPanel?.ClearPreview(towerData);
    }
    public void OnSelect(BaseEventData eventData){ focused = true; }
    public void OnDeselect(BaseEventData eventData){ focused = false; }

    private void OnDisable(){
        infoPanel?.ClearPreview(towerData);
        hovered = focused = false;
        transform.localScale = restingScale;
    }

    private void Start(){
        upgrades = RunUpgradeState.Current;
        upgrades.Changed += OnUpgradeChanged;
        placementManager =
            FindFirstObjectByType<TowerPlacementManager>();

        if (placementManager == null){
            Debug.LogWarning(
                "TowerPlacementManager was not found."
            );
        }

        playerGold = FindFirstObjectByType<PlayerGold>();
        infoPanel = FindFirstObjectByType<TowerInfoPanel>();
        button = GetComponent<Button>();

        UpdateVisuals();

        if (playerGold != null){
            playerGold.GoldChanged += UpdateAffordability;
            UpdateAffordability(playerGold.CurrentGold);
        }
    }

    private void OnDestroy(){
        if (upgrades != null) upgrades.Changed -= OnUpgradeChanged;
        if (playerGold != null){
            playerGold.GoldChanged -= UpdateAffordability;
        }
    }

    public void StartBuildMode(){
        if (RunUpgradeState.Current.BlocksInput) return;
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
        // Called again when session discounts change.
        if (towerPrefab == null){
            return;
        }

        TowerTargeting targeting =
            towerPrefab.GetComponent<TowerTargeting>();

        if (targeting == null || targeting.TowerData == null){
            return;
        }

        towerData = targeting.TowerData;
        towerCost = towerData.Cost;

        label = GetComponentInChildren<TMP_Text>();

        if (label != null){
            TowerData data = towerData;
            label.text =
                $"<color=#69D7F0><b>[{shortcutNumber}]</b></color> " +
                $"<b>{data.towerName}</b>\n" +
                $"<color=#F5CA70>{data.Cost} G</color>";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 13f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 13f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.margin = Vector4.zero;
            label.outlineColor = new Color32(10, 12, 18, 255);
            label.outlineWidth = 0f;
            label.raycastTarget = false;
        }

        Image icon = towerIcon != null ? towerIcon : GetComponent<Image>();

        if (icon != null){
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        if (button != null){
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(27, 41, 57, 255);
            colors.highlightedColor = new Color32(43, 80, 102, 255);
            colors.pressedColor = new Color32(22, 61, 82, 255);
            colors.selectedColor = new Color32(35, 66, 87, 255);
            colors.disabledColor = new Color32(20, 26, 36, 230);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }
    }

    private void UpdateAffordability(int currentGold){
        if (button == null){
            return;
        }

        button.interactable = towerCost > 0 && currentGold >= towerCost;
        if (towerIcon != null){
            towerIcon.color = button.interactable ? Color.white : new Color(0.4f, 0.45f, 0.5f, 0.6f);
        }
        if (label != null){
            label.alpha = button.interactable ? 1f : 0.4f;
        }
    }

    private void OnUpgradeChanged(){
        UpdateVisuals();
        if (playerGold != null) UpdateAffordability(playerGold.CurrentGold);
    }

    private bool IsShortcutPressed(){
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || shortcutNumber < 1 || shortcutNumber > 5){
            return false;
        }

        return shortcutNumber switch{
            1 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
            2 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
            3 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
            4 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
            5 => keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame,
            _ => false
        };
    }
}
