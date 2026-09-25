using System;
using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;

public class WaveManager : MonoBehaviour{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private float timeBetweenWaves = 10f;
    [Header("Infinite waves")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField, Min(1)] private int baseEnemyCount = 32;
    [SerializeField, Min(0f)] private float enemiesAddedPerWave = 4f;
    [SerializeField, Range(0f, 0.4f)] private float enemyCountVariance = 0.14f;
    [SerializeField, Min(1f)] private float swarmCountMultiplier = 1.45f;
    [SerializeField, Min(1f)] private float fastCountMultiplier = 1.15f;
    [SerializeField, Range(0.1f, 1f)] private float specialCountMultiplier = 0.82f;
    [SerializeField, Min(1)] private int baseSpawnBatchSize = 2;
    [SerializeField, Min(0.01f)] private float initialSpawnInterval = 0.55f;
    [SerializeField, Min(0.01f)] private float minimumSpawnInterval = 0.08f;
    [SerializeField, Range(0.8f, 1f)] private float spawnIntervalDecay = 0.96f;
    [SerializeField, Min(0.1f)] private float initialHealthMultiplier = 0.4f;
    [SerializeField, Min(1f)] private float healthGrowthPerWave = 1.085f;
    [SerializeField, Range(0.1f, 1f)] private float swarmHealthMultiplier = 0.85f;
    [SerializeField, Range(0.1f, 1f)] private float fastHealthMultiplier = 0.9f;
    [SerializeField, Min(1f)] private float maximumHealthMultiplier = 1000000f;
    [SerializeField, Min(0f)] private float speedAddedPerWave = 0.012f;
    [SerializeField, Min(0.1f)] private float maximumSpeedMultiplier = 1.75f;
    [SerializeField, Min(0f)] private float initialGoldRewardMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float rewardGrowthEveryFiveWaves = 0.025f;
    [SerializeField, Min(1f)] private float specialRewardMultiplier = 1.1f;
    [Header("Run upgrades")]
    [SerializeField, Min(0f)] private float missionIntroDelay = 1.2f;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private UnityEngine.UI.Button[] upgradeButtons;
    [SerializeField] private TMPro.TMP_Text[] upgradeLabels;
    [SerializeField] private TMPro.TMP_Text upgradeTitle;
    [SerializeField] private GameObject upgradeDecorations;
    [SerializeField] private UnityEngine.UI.Image[] upgradeIcons;
    [SerializeField] private Sprite[] towerUpgradeIcons;
    [SerializeField] private UnityEngine.UI.Image[] upgradeFrames;
    [SerializeField] private UnityEngine.UI.Button rerollButton;
    [SerializeField] private TMPro.TMP_Text rerollLabel;
    [SerializeField] private TMPro.TMP_Text rerollHint;
    [SerializeField] private UnityEngine.UI.Image rerollFlash;
    private PlayerGold upgradeGold;
    private bool rerolling;
    private RunUpgradeState.Choice[] offeredUpgrades;
    private float upgradeRevealTime;
    private bool confirmingUpgrade;
    private bool showingMission;
    private bool gameplayReady;
    private MMF_Player[] cardReveal, cardFocus, cardBlur, cardConfirm;
    private bool[] cardHighlighted;
    private UnityEngine.UI.Outline[] cardBorders;
    private UnityEngine.CanvasGroup[] cardGroups;
    private Color[] cardAccent;
    private Vector2[] cardRestPositions;
    private MMF_Player missionFeedback, rerollFeedback;
    private MMF_Player[] cardReroll;
    private const float CardsReadyTime = .4f;

    private void Awake(){
        RunUpgradeState.Reset();
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (upgradeButtons != null)
            for (int i = 0; i < upgradeButtons.Length; i++){
                int index = i;
                upgradeButtons[i].onClick.AddListener(() => ChooseUpgrade(index));
            }
        ConfigureCardFeedbacks();
        if (rerollButton != null) rerollButton.onClick.AddListener(RerollUpgrades);
    }

    private void OnDestroy(){
        if (upgradeGold != null) upgradeGold.GoldChanged -= OnUpgradeGoldChanged;
        RunUpgradeState.Reset();
    }

    private void Update(){
        // Let the existing pause menu remain visible and usable above the draft.
        if (RunUpgradeState.Current.Choosing && upgradePanel != null)
            upgradePanel.SetActive(Time.timeScale > 0f);
        if (!RunUpgradeState.Current.Choosing || Time.timeScale == 0f || confirmingUpgrade || showingMission || rerolling) return;
        upgradeRevealTime += Time.unscaledDeltaTime;
        var events = UnityEngine.EventSystems.EventSystem.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        // Mouse hover must not inherit the EventSystem's persistent selection.
        if (events != null && mouse != null && mouse.delta.ReadValue().sqrMagnitude > .01f)
            events.SetSelectedGameObject(null);
        if (events != null && events.currentSelectedGameObject == null && upgradeRevealTime > CardsReadyTime &&
            keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame ||
            keyboard.rightArrowKey.wasPressedThisFrame)) upgradeButtons[0].Select();
        for (int i = 0; i < upgradeButtons.Length; i++){
            var button = upgradeButtons[i];
            var rect = (RectTransform)button.transform;
            bool hovered = UnityEngine.InputSystem.Mouse.current != null &&
                RectTransformUtility.RectangleContainsScreenPoint(rect,
                    UnityEngine.InputSystem.Mouse.current.position.ReadValue(), null);
            bool focused = UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == button.gameObject;
            button.interactable = upgradeRevealTime > CardsReadyTime;
            bool highlighted = button.interactable && (hovered || focused);
            Color borderColor = cardAccent[i];
            borderColor.a = highlighted ? 1f : .12f;
            cardBorders[i].effectColor = Color.Lerp(cardBorders[i].effectColor, borderColor,
                1f - Mathf.Exp(-40f * Time.unscaledDeltaTime));
            SetFrameColor(i, cardAccent[i], highlighted ? 1f : .55f);
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition,
                cardRestPositions[i] + Vector2.up * (highlighted ? 6f : 0f),
                1f - Mathf.Exp(-32f * Time.unscaledDeltaTime));
            if (highlighted != cardHighlighted[i]){
                cardHighlighted[i] = highlighted;
                if (highlighted) cardBorders[i].effectColor = borderColor;
                cardFocus[i].StopFeedbacks();
                cardBlur[i].StopFeedbacks();
                (highlighted ? cardFocus[i] : cardBlur[i]).PlayFeedbacks();
            }
        }
        if (rerollButton != null) rerollButton.interactable = CanReroll();
    }

    private IEnumerator OfferUpgrades(int milestone){
        if (upgradePanel == null || upgradeButtons == null || upgradeButtons.Length != 3 ||
            upgradeLabels == null || upgradeLabels.Length != 3){
            Debug.LogError("Upgrade cards are not configured on WaveManager.");
            yield break;
        }
        offeredUpgrades = RunUpgradeState.Current.Draw();
        if (offeredUpgrades.Length == 0) yield break;
        FindFirstObjectByType<TowerPlacementManager>()?.CancelBuildMode();
        RunUpgradeState.Current.Choosing = true;
        upgradeTitle.text = $"<size=14><color=#B8A581>{(milestone == 1 ? "ANTES DE LA OLEADA 1" : "OLEADA " + milestone)}</color></size>\n<color=#E8D7AE><b>ELIGE TU BENDICIÓN</b></color>";
        if (upgradeDecorations != null) upgradeDecorations.SetActive(true);
        PresentUpgradeCards();
        while (RunUpgradeState.Current.Choosing) yield return null;
        upgradePanel.SetActive(false);
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
    }

    private void PresentUpgradeCards(){
        upgradeRevealTime = 0f;
        confirmingUpgrade = false;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        for (int i = 0; i < 3; i++){
            upgradeButtons[i].gameObject.SetActive(i < offeredUpgrades.Length);
            if (i >= offeredUpgrades.Length) continue;
            var choice = offeredUpgrades[i];
            string color = UpgradeColor(choice);
            ColorUtility.TryParseHtmlString("#" + color, out cardAccent[i]);
            SetFrameColor(i, cardAccent[i], .55f);
            if (upgradeIcons != null && i < upgradeIcons.Length && towerUpgradeIcons != null && towerUpgradeIcons.Length >= 5){
                int iconIndex = choice.Family == "cannon" ? 1 : choice.Family == "lightning" ? 2 :
                    choice.Family == "flame" ? 3 : choice.Family == "arcane" ? 4 : 0;
                if ((choice.Stat == "gold" || choice.Stat == "discount") && towerUpgradeIcons.Length > 5) iconIndex = 5;
                upgradeIcons[i].sprite = towerUpgradeIcons[iconIndex];
            }
            cardBorders[i].effectDistance = new Vector2(2f, -2f);
            cardBorders[i].effectColor = new Color(cardAccent[i].r, cardAccent[i].g, cardAccent[i].b, .12f);
            cardGroups[i].alpha = 1f;
            cardGroups[i].blocksRaycasts = true;
            ((RectTransform)upgradeButtons[i].transform).anchoredPosition = cardRestPositions[i];
            upgradeButtons[i].transform.localRotation = Quaternion.identity;
            string family = choice.Family == "all" ? "TODAS LAS TORRES" :
                choice.Family == "flame" ? "FUEGO" : choice.Family == "cannon" ? "CAÑÓN" :
                choice.Family == "lightning" ? "RAYOS" : choice.Family == "arcane" ? "ARCANA" : "BÁSICA";
            if (choice.Stat == "gold") family = "ECONOMÍA";
            string amount = choice.Stat == "bounces" ? "+1" :
                choice.Stat == "burn_duration" ? $"+{choice.Amount:0.##} s" :
                (choice.Stat == "discount" ? "−" : "+") + Mathf.RoundToInt(choice.Amount * 100f) + "%";
            upgradeLabels[i].text = $"<size=12><color=#ACA492>{family}</color></size>\n" +
                $"<size=23><color=#{color}><b>{choice.Title}</b></color></size>\n\n" +
                $"<size=38><color=#{color}><b>{amount}</b></color></size>\n" +
                $"<size=18><color=#D4CFC4>{choice.Description}</color></size>\n\n" +
                $"<size=12><color=#ACA492>NIVEL {choice.Stacks + 1} / {choice.Limit}</color></size>\n" +
                $"<size=13><color=#{color}>ELEGIR BENDICIÓN</color></size>";
            StopCardFeedbacks(i);
            cardHighlighted[i] = false;
            upgradeButtons[i].transform.localScale = Vector3.zero;
            upgradeLabels[i].alpha = 1f;
            upgradeButtons[i].interactable = false;
        }
        upgradePanel.SetActive(true);
        for (int i = 0; i < offeredUpgrades.Length; i++) cardReveal[i].PlayFeedbacks();
        RefreshReroll();
    }

    private void SetFrameColor(int index, Color color, float alpha){
        if (upgradeFrames == null) return;
        color.a = alpha;
        for (int j = index * 4; j < index * 4 + 4 && j < upgradeFrames.Length; j++)
            if (upgradeFrames[j] != null) upgradeFrames[j].color = color;
    }

    private bool CanReroll() => !showingMission && !confirmingUpgrade && !rerolling &&
        upgradeRevealTime > CardsReadyTime && Time.timeScale > 0f && upgradeGold != null &&
        RunUpgradeState.Current.CanReroll(offeredUpgrades) && upgradeGold.CanAfford(RunUpgradeState.Current.RerollCost);

    private void OnUpgradeGoldChanged(int amount){ RefreshReroll(); }

    private void RefreshReroll(){
        if (rerollButton == null) return;
        rerollButton.interactable = CanReroll();
        int price = RunUpgradeState.Current.RerollCost;
        bool affordable = upgradeGold != null && upgradeGold.CanAfford(price);
        if (rerollLabel != null) rerollLabel.text = "RENOVAR  ·  <color=" +
            (affordable ? "#F0C878" : "#E18476") + ">" + price + " ORO</color>";
        if (rerollHint != null) rerollHint.text = "ORO DISPONIBLE  " + (upgradeGold != null ? upgradeGold.CurrentGold : 0) +
            "   /   +15 ORO POR RENOVACIÓN";
    }

    private void RerollUpgrades(){
        if (!CanReroll()) return;
        rerolling = true;
        if (!RunUpgradeState.Current.TryReroll(offeredUpgrades, upgradeGold.SpendGold, out var replacement)){
            rerolling = false;
            RefreshReroll();
            return;
        }
        StartCoroutine(AnimateReroll(replacement));
    }

    private IEnumerator AnimateReroll(RunUpgradeState.Choice[] replacement){
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        if (rerollButton != null) rerollButton.interactable = false;
        rerollFeedback?.StopFeedbacks();
        rerollFeedback?.PlayFeedbacks();
        var startingPositions = new Vector2[upgradeButtons.Length];
        for (int i = 0; i < upgradeButtons.Length; i++){
            StopCardFeedbacks(i);
            upgradeButtons[i].interactable = false;
            startingPositions[i] = ((RectTransform)upgradeButtons[i].transform).anchoredPosition;
            cardReroll[i].PlayFeedbacks();
        }
        float elapsed = 0f;
        while (elapsed < .48f){
            elapsed += Time.deltaTime;
            for (int i = 0; i < upgradeButtons.Length; i++){
                float t = Mathf.Clamp01((elapsed - i * .035f) / .4f);
                float gather = Mathf.SmoothStep(0f, 1f, t);
                var rect = (RectTransform)upgradeButtons[i].transform;
                rect.anchoredPosition = Vector2.Lerp(startingPositions[i], new Vector2(0f, -25f), gather);
                rect.localRotation = Quaternion.Euler(0f, 85f * gather, (i - 1) * -14f * Mathf.Sin(t * Mathf.PI));
                cardGroups[i].alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .45f) / .55f));
                SetFrameColor(i, Color.Lerp(cardAccent[i], new Color(1f, .86f, .58f), gather), 1f);
            }
            yield return null;
        }
        offeredUpgrades = replacement;
        PresentUpgradeCards();
        // Lock input through the impact/reveal; use scaled time so pause freezes it.
        elapsed = 0f;
        while (elapsed < .42f){
            elapsed += Time.deltaTime;
            if (rerollFlash != null){
                float t = Mathf.Clamp01(elapsed / .42f);
                rerollFlash.color = new Color(1f, .78f, .42f, .16f * (1f - t) * (1f - t));
            }
            yield return null;
        }
        if (rerollFlash != null) rerollFlash.color = Color.clear;
        rerolling = false;
        upgradeRevealTime = CardsReadyTime;
        RefreshReroll();
    }

    private void ChooseUpgrade(int index){
        if (!RunUpgradeState.Current.Choosing || showingMission || confirmingUpgrade || rerolling || upgradeRevealTime < CardsReadyTime || Time.timeScale == 0f ||
            offeredUpgrades == null || index < 0 || index >= offeredUpgrades.Length) return;
        StartCoroutine(ConfirmUpgrade(index));
    }

    private IEnumerator ConfirmUpgrade(int index){
        confirmingUpgrade = true;
        if (rerollButton != null) rerollButton.interactable = false;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        foreach (var button in upgradeButtons) button.interactable = false;
        for (int i = 0; i < upgradeButtons.Length; i++) StopCardFeedbacks(i);
        Vector2[] exitPositions = new Vector2[upgradeButtons.Length];
        Vector3[] exitScales = new Vector3[upgradeButtons.Length];
        for (int i = 0; i < upgradeButtons.Length; i++){
            exitPositions[i] = ((RectTransform)upgradeButtons[i].transform).anchoredPosition;
            exitScales[i] = upgradeButtons[i].transform.localScale;
            cardGroups[i].blocksRaycasts = false;
        }
        cardConfirm[index].PlayFeedbacks();
        upgradeTitle.text = "<size=15><color=#89A5BE>MEJORA ADQUIRIDA</color></size>\n<b>" + offeredUpgrades[index].Title + "</b>";
        float elapsed = 0f;
        while (elapsed < .6f){
            if (Time.timeScale > 0f){
                elapsed += Time.unscaledDeltaTime;
                float flash = Mathf.Sin(Mathf.Clamp01(elapsed / .3f) * Mathf.PI);
                cardBorders[index].effectColor = Color.Lerp(cardAccent[index], Color.white, flash);
                SetFrameColor(index, Color.Lerp(cardAccent[index], Color.white, flash), 1f);
                cardBorders[index].effectDistance = Vector2.one * (2f + flash * 4f);
                cardGroups[index].alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - .38f) / .22f));
                for (int i = 0; i < upgradeButtons.Length; i++){
                    if (i == index) continue;
                    float t = Mathf.Clamp01((elapsed - .035f * Mathf.Abs(i - index)) / .28f);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    float side = i < index ? -1f : 1f;
                    var rect = (RectTransform)upgradeButtons[i].transform;
                    rect.anchoredPosition = exitPositions[i] + new Vector2(side * 65f, -38f) * eased;
                    rect.localRotation = Quaternion.Euler(0f, 0f, -side * 7f * eased);
                    rect.localScale = Vector3.Lerp(exitScales[i], Vector3.one * .82f, eased);
                    cardGroups[i].alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
                    if (t >= 1f) upgradeButtons[i].gameObject.SetActive(false);
                }
            }
            yield return null;
        }
        RunUpgradeState.Current.Apply(offeredUpgrades[index]);
        upgradePanel.SetActive(false);
        confirmingUpgrade = false;
    }

    private MMF_Player ScaleFeedback(Transform target, float duration, float delay, params Keyframe[] keys){
        // FEEL allows only one MMF_Player per GameObject. Keep each independently
        // controlled animation on its own child, owned by this scene manager.
        GameObject feedbackObject = new GameObject($"Card Feedback - {target.name}");
        feedbackObject.transform.SetParent(transform, false);
        MMF_Player player = feedbackObject.AddComponent<MMF_Player>();
        player.AddFeedback(new MMF_Scale{
            AnimateScaleTarget = target,
            Mode = MMF_Scale.Modes.Absolute,
            AnimateScaleDuration = duration,
            UniformScaling = true,
            RemapCurveZero = 0f,
            RemapCurveOne = 1f,
            AnimateScaleTweenX = new MMTweenType(new AnimationCurve(keys)),
            Timing = new MMFeedbackTiming { InitialDelay = delay, TimescaleMode = TimescaleModes.Scaled }
        });
        player.Initialization();
        return player;
    }

    private void ConfigureCardFeedbacks(){
        if (upgradeButtons == null || upgradeTitle == null) return;
        int count = upgradeButtons.Length;
        cardReveal = new MMF_Player[count]; cardFocus = new MMF_Player[count];
        cardBlur = new MMF_Player[count]; cardConfirm = new MMF_Player[count];
        cardReroll = new MMF_Player[count];
        cardHighlighted = new bool[count];
        cardBorders = new UnityEngine.UI.Outline[count];
        cardGroups = new CanvasGroup[count];
        cardAccent = new Color[count];
        cardRestPositions = new Vector2[count];
        for (int i = 0; i < count; i++){
            Transform target = upgradeButtons[i].transform;
            cardRestPositions[i] = ((RectTransform)target).anchoredPosition;
            cardBorders[i] = target.GetComponent<UnityEngine.UI.Outline>();
            if (cardBorders[i] == null) cardBorders[i] = target.gameObject.AddComponent<UnityEngine.UI.Outline>();
            cardBorders[i].effectDistance = new Vector2(2f, -2f);
            cardBorders[i].useGraphicAlpha = false;
            cardGroups[i] = target.GetComponent<CanvasGroup>();
            if (cardGroups[i] == null) cardGroups[i] = target.gameObject.AddComponent<CanvasGroup>();
            cardReveal[i] = ScaleFeedback(target, .28f, i * .045f,
                new Keyframe(0f, .05f), new Keyframe(.7f, 1.06f), new Keyframe(1f, 1f));
            cardFocus[i] = ScaleFeedback(target, .1f, 0f,
                new Keyframe(0f, 1f), new Keyframe(.55f, 1.045f), new Keyframe(1f, 1.03f));
            cardBlur[i] = ScaleFeedback(target, .09f, 0f,
                new Keyframe(0f, 1.03f), new Keyframe(1f, 1f));
            cardConfirm[i] = ScaleFeedback(target, .6f, 0f,
                new Keyframe(0f, 1.04f), new Keyframe(.1f, .94f),
                new Keyframe(.35f, 1.14f), new Keyframe(.6f, 1.06f), new Keyframe(1f, 1.1f));
            cardReroll[i] = ScaleFeedback(target, .4f, i * .035f,
                new Keyframe(0f, 1f), new Keyframe(.2f, 1.09f),
                new Keyframe(.45f, .98f), new Keyframe(1f, .55f));
        }
        if (rerollButton != null)
            rerollFeedback = ScaleFeedback(rerollButton.transform, .55f, 0f,
                new Keyframe(0f, 1f), new Keyframe(.16f, .9f),
                new Keyframe(.42f, 1.08f), new Keyframe(1f, 1f));
        if (rerollFlash != null) rerollFlash.color = Color.clear;
        missionFeedback = ScaleFeedback(upgradeTitle.transform, .6f, 0f,
            new Keyframe(0f, .85f), new Keyframe(.7f, 1.03f), new Keyframe(1f, 1f));
    }

    private void StopCardFeedbacks(int index){
        cardReveal[index].StopFeedbacks(); cardFocus[index].StopFeedbacks();
        cardBlur[index].StopFeedbacks(); cardConfirm[index].StopFeedbacks();
        cardReroll[index].StopFeedbacks();
    }

    private IEnumerator ShowMission(){
        if (upgradePanel == null || upgradeTitle == null) yield break;
        if (upgradeDecorations != null) upgradeDecorations.SetActive(false);
        // Let the map settle before presenting the objective. Respect pause.
        yield return new WaitForSeconds(missionIntroDelay);
        showingMission = true;
        RunUpgradeState.Current.Choosing = true;
        foreach (var button in upgradeButtons) button.gameObject.SetActive(false);
        var titleRect = upgradeTitle.rectTransform;
        Vector2 restingPosition = titleRect.anchoredPosition;
        Vector2 restingSize = titleRect.sizeDelta;
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = new Vector2(restingSize.x, 200f);
        upgradeTitle.text = "<size=14><color=#91DED2>PREPARA TUS DEFENSAS</color></size>\n" +
            "<size=38><b>PROTEGE LA BASE</b></size>";
        upgradeTitle.alpha = 0f;
        upgradePanel.SetActive(true);
        missionFeedback?.PlayFeedbacks();
        float elapsed = 0f;
        while (elapsed < 1.8f){
            elapsed += Time.deltaTime;
            upgradeTitle.alpha = Mathf.Min(Mathf.Clamp01(elapsed / .25f), Mathf.Clamp01((1.8f - elapsed) / .3f));
            yield return null;
        }
        upgradePanel.SetActive(false);
        upgradeTitle.alpha = 1f;
        titleRect.anchoredPosition = restingPosition;
        titleRect.sizeDelta = restingSize;
        titleRect.localScale = Vector3.one;
        RunUpgradeState.Current.Choosing = false;
        showingMission = false;
    }

    private static string UpgradeColor(RunUpgradeState.Choice choice){
        if (choice.Stat == "discount" || choice.Stat == "gold") return "F0C878";
        return choice.Family == "flame" ? "FFAA73" : choice.Family == "arcane" ? "BEA1FF" :
            choice.Family == "lightning" ? "75DDF4" : choice.Family == "cannon" ? "E8B994" : "91DED2";
    }

    private int currentWaveIndex;
    private int enemiesAlive;
    private int currentWaveEnemyCount;
    private int currentWaveEnemiesRemoved;
    private int completedWaveCount;
    private bool waveActive;

    private float nextWaveTimer;

    private bool waitingForFirstWave;
    private bool waitingForNextWave;
    private bool skipWait;

    public int CurrentWaveNumber => currentWaveIndex + 1;

    public float NextWaveTimer => nextWaveTimer;

    // The progress rail only needs an upper bound to keep five future slots visible.
    public int TotalWaveCount => int.MaxValue;
    public int CompletedWaveCount => completedWaveCount;
    public bool WaveActive => waveActive;
    public float CurrentWaveProgress => currentWaveEnemyCount > 0
        ? Mathf.Clamp01(
            (float)currentWaveEnemiesRemoved / currentWaveEnemyCount
        )
        : 0f;

    public bool WaitingForFirstWave => waitingForFirstWave;
    public bool WaitingForNextWave => waitingForNextWave;
    public bool GameplayReady => gameplayReady;

    public event Action<int> WaveStarted;

    private void OnEnable(){
        if (enemySpawner != null){
            enemySpawner.EnemySpawned += OnEnemySpawned;
        }
    }

    private void OnDisable(){
        if (enemySpawner != null){
            enemySpawner.EnemySpawned -= OnEnemySpawned;
        }
    }

    private void Start(){
        upgradeGold = FindFirstObjectByType<PlayerGold>();
        if (upgradeGold != null) upgradeGold.GoldChanged += OnUpgradeGoldChanged;
        if (enemyPrefab == null){
            Debug.LogWarning("No enemy prefab is configured for infinite waves.");
            return;
        }

        if (enemySpawner == null){
            Debug.LogWarning("EnemySpawner is not assigned.");
            return;
        }

        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves(){
        gameplayReady = false;
        yield return ShowMission();
        gameplayReady = true;
        waitingForFirstWave = true;

        while (waitingForFirstWave){
            yield return null;
        }
        yield return OfferUpgrades(1);

        for (currentWaveIndex = 0; ; currentWaveIndex++){
            InfiniteWave wave = GenerateWave(CurrentWaveNumber);
            currentWaveEnemyCount = wave.EnemyCount;
            currentWaveEnemiesRemoved = 0;
            waveActive = true;
            Debug.Log($"Starting Wave {CurrentWaveNumber}");
            WaveStarted?.Invoke(CurrentWaveNumber);

            yield return StartCoroutine(
                enemySpawner.SpawnWave(
                    enemyPrefab,
                    wave.EnemyCount,
                    wave.SpawnBatchSize,
                    wave.SpawnInterval,
                    wave.HealthMultiplier,
                    wave.SpeedMultiplier,
                    wave.GoldRewardMultiplier
                )
            );

            while (enemiesAlive > 0){
                yield return null;
            }

            currentWaveEnemiesRemoved = currentWaveEnemyCount;
            completedWaveCount = currentWaveIndex + 1;
            waveActive = false;
            Debug.Log($"Wave {CurrentWaveNumber} completed.");
            if (completedWaveCount % 5 == 0) yield return OfferUpgrades(completedWaveCount);
            // One-shot audio owns its short lifetime and fade, including the
            // final impact. Ending the wave must not cut that tail off.

            yield return StartCoroutine(WaitForNextWave());
        }
    }

    private InfiniteWave GenerateWave(int waveNumber){
        int step = Mathf.Max(0, waveNumber - 1);
        bool specialWave = waveNumber % 5 == 0;
        bool swarmWave = !specialWave && waveNumber % 3 == 0;
        bool fastWave = !specialWave && waveNumber % 4 == 0;

        float countVariation = 1f + Mathf.Sin(
            waveNumber * 1.37f + 1.77f
        ) * enemyCountVariance;
        float typeCountMultiplier = specialWave
            ? specialCountMultiplier
            : swarmWave
                ? swarmCountMultiplier
                : fastWave
                    ? fastCountMultiplier
                    : 1f;
        int enemyCount = Mathf.RoundToInt(
            (baseEnemyCount + step * enemiesAddedPerWave) *
            countVariation * typeCountMultiplier
        );
        enemyCount = Mathf.Clamp(enemyCount, 1, 400);

        int batchSize = baseSpawnBatchSize + step / 12;
        if (swarmWave){ batchSize++; }
        batchSize = Mathf.Clamp(batchSize, 1, 5);

        float health = initialHealthMultiplier * Mathf.Pow(healthGrowthPerWave, step);
        if (swarmWave){ health *= swarmHealthMultiplier; }
        if (fastWave && !swarmWave){ health *= fastHealthMultiplier; }
        if (specialWave){ health *= 1.4f; }
        health = Mathf.Clamp(health, 0.1f, maximumHealthMultiplier);

        float speed = 1f + step * speedAddedPerWave;
        if (fastWave){ speed *= 1.12f; }
        if (specialWave){ speed *= 0.92f; }
        speed = Mathf.Clamp(speed, 0.75f, maximumSpeedMultiplier);

        float interval = initialSpawnInterval * Mathf.Pow(spawnIntervalDecay, step);
        if (swarmWave){ interval *= 0.8f; }
        if (specialWave){ interval *= 1.1f; }
        interval = Mathf.Max(minimumSpawnInterval, interval);

        float reward = initialGoldRewardMultiplier +
                       Mathf.Floor(step / 5f) * rewardGrowthEveryFiveWaves;
        if (specialWave){ reward *= specialRewardMultiplier; }
        reward = Mathf.Min(1.5f, reward);

        return new InfiniteWave(
            enemyCount,
            batchSize,
            interval,
            health,
            speed,
            reward
        );
    }

    private readonly struct InfiniteWave{
        public readonly int EnemyCount;
        public readonly int SpawnBatchSize;
        public readonly float SpawnInterval;
        public readonly float HealthMultiplier;
        public readonly float SpeedMultiplier;
        public readonly float GoldRewardMultiplier;

        public InfiniteWave(int count, int batchSize, float interval,
            float health, float speed, float reward){
            EnemyCount = count;
            SpawnBatchSize = batchSize;
            SpawnInterval = interval;
            HealthMultiplier = health;
            SpeedMultiplier = speed;
            GoldRewardMultiplier = reward;
        }
    }

    private IEnumerator WaitForNextWave(){
        waitingForNextWave = true;
        skipWait = false;
        nextWaveTimer = timeBetweenWaves;

        while (nextWaveTimer > 0f && !skipWait){
            nextWaveTimer -= Time.deltaTime;

            if (nextWaveTimer < 0f){
                nextWaveTimer = 0f;
            }

            yield return null;
        }

        nextWaveTimer = 0f;
        waitingForNextWave = false;
    }

    public void StartNextWaveNow(){
        if (RunUpgradeState.Current.BlocksInput) return;
        if (waitingForFirstWave){
            waitingForFirstWave = false;
            return;
        }

        if (waitingForNextWave){
            skipWait = true;
        }
    }

    private void OnEnemySpawned(GameObject enemy){
        enemiesAlive++;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();

        if (health != null){
            health.Died += OnEnemyDied;
        }

        if (movement != null){
            movement.ReachedDestination += OnEnemyReachedDestination;
        }
    }

    private void OnEnemyDied(EnemyHealth enemyHealth){
        enemyHealth.Died -= OnEnemyDied;

        EnemyMovement movement =
            enemyHealth.GetComponent<EnemyMovement>();

        if (movement != null){
            movement.ReachedDestination -=
                OnEnemyReachedDestination;
        }

        EnemyRemoved();
    }

    private void OnEnemyReachedDestination(
        EnemyMovement enemyMovement
    ){
        enemyMovement.ReachedDestination -=
            OnEnemyReachedDestination;

        EnemyHealth health =
            enemyMovement.GetComponent<EnemyHealth>();

        if (health != null){
            health.Died -= OnEnemyDied;
        }

        EnemyRemoved();
    }

    private void EnemyRemoved(){
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        currentWaveEnemiesRemoved = Mathf.Min(
            currentWaveEnemyCount,
            currentWaveEnemiesRemoved + 1
        );
    }
}
