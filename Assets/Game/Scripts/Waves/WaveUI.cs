using System.Collections;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveUI : MonoBehaviour{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text nextWaveText;
    [SerializeField] private Button startWaveButton;
    [SerializeField] private WaveProgressBar waveProgressBar;
    [SerializeField] private TMP_Text waveAnnouncement;

    private WaveManager waveManager;
    private TMP_Text startWaveButtonText;
    private MMF_Player waveRequestedFeedbacks;
    private MMF_Player waveStartedFeedbacks;
    private Coroutine announcementAnimation;
    private Vector2 announcementStartPosition;
    private int lastStatusWave = -1;
    private int lastStatusTotal = -1;
    private int lastStatusPercent = -1;
    private int lastStatusState = -1;
    private MMF_Player progressFeedbacks;
    private int lastProgressMilestone;
    private int lastFeedbackWave;
    private const float AnnouncementDuration = 2.6f;

    private void Start(){
        waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager == null){
            Debug.LogWarning("WaveManager was not found.");
            return;
        }

        startWaveButtonText =
            startWaveButton.GetComponentInChildren<TMP_Text>();

        StyleHud();
        ConfigureFeelFeedbacks();
        waveManager.WaveStarted += OnWaveStarted;
    }

    private void OnDestroy(){
        if (waveManager != null){
            waveManager.WaveStarted -= OnWaveStarted;
        }
    }

    private void OnDisable(){
        waveRequestedFeedbacks?.StopFeedbacks();
        waveStartedFeedbacks?.StopFeedbacks();
        progressFeedbacks?.StopFeedbacks();
        if (announcementAnimation != null){
            StopCoroutine(announcementAnimation);
            announcementAnimation = null;
        }
        if (waveAnnouncement != null){
            waveAnnouncement.gameObject.SetActive(false);
        }
        if (waveText != null){
            waveText.transform.localScale = Vector3.one;
        }
    }

    private void Update(){
        if (waveManager == null){
            return;
        }

        UpdateWaveProgress();

        bool waitingForFirstWave =
            waveManager.WaitingForFirstWave;

        bool waitingForNextWave =
            waveManager.WaitingForNextWave;

        if (waitingForFirstWave){
            nextWaveText.gameObject.SetActive(false);
            startWaveButton.gameObject.SetActive(true);

            if (startWaveButtonText != null){
                startWaveButtonText.text = "INICIAR OLEADA";
            }

            return;
        }

        if (waitingForNextWave){
            nextWaveText.gameObject.SetActive(true);
            startWaveButton.gameObject.SetActive(true);

            int seconds =
                Mathf.CeilToInt(waveManager.NextWaveTimer);

            nextWaveText.text = $"<color=#9BAFC4>Siguiente oleada</color>  <b>{seconds}s</b>";

            if (startWaveButtonText != null){
                startWaveButtonText.text = "INICIAR AHORA";
            }

            return;
        }

        nextWaveText.gameObject.SetActive(false);
        startWaveButton.gameObject.SetActive(false);
    }

    public void StartNextWaveNow(){
        if (waveManager == null){
            return;
        }

        waveRequestedFeedbacks?.PlayFeedbacks(waveText.transform.position);
        waveManager.StartNextWaveNow();
    }

    private void OnWaveStarted(int waveNumber){
        if (waveAnnouncement == null){
            return;
        }
        waveRequestedFeedbacks?.StopFeedbacks();

        if (announcementAnimation != null){
            StopCoroutine(announcementAnimation);
        }

        waveStartedFeedbacks?.StopFeedbacks();
        waveAnnouncement.transform.localScale = Vector3.one;
        waveAnnouncement.gameObject.SetActive(true);
        waveAnnouncement.text = $"<color=#E3F5FF>OLEADA <b>{waveNumber:00}</b></color>";
        waveAnnouncement.color = Color.white;
        waveAnnouncement.alpha = 0f;
        waveAnnouncement.rectTransform.anchoredPosition =
            announcementStartPosition;

        waveStartedFeedbacks?.PlayFeedbacks(
            waveAnnouncement.transform.position
        );
        announcementAnimation = StartCoroutine(AnimateWaveAnnouncement());
    }

    private void ConfigureFeelFeedbacks(){
        if (waveAnnouncement == null){
            return;
        }
        waveAnnouncement.fontSize = 46f;
        waveAnnouncement.fontStyle = FontStyles.Normal;
        waveAnnouncement.characterSpacing = 3f;
        waveAnnouncement.lineSpacing = 6f;
        waveAnnouncement.outlineColor = new Color32(8, 6, 8, 235);
        waveAnnouncement.outlineWidth = 0.15f;
        waveAnnouncement.alignment = TextAlignmentOptions.Center;
        waveAnnouncement.raycastTarget = false;

        RectTransform announcementRect = waveAnnouncement.rectTransform;
        announcementRect.anchorMin = new Vector2(0.5f, 0.5f);
        announcementRect.anchorMax = new Vector2(0.5f, 0.5f);
        announcementRect.pivot = new Vector2(0.5f, 0.5f);
        announcementRect.anchoredPosition = new Vector2(0f, 150f);
        announcementRect.sizeDelta = new Vector2(600f, 100f);
        announcementStartPosition = announcementRect.anchoredPosition;

        waveRequestedFeedbacks = CreateScaleFeedback(
            "Wave Requested Feedbacks",
            waveText.transform,
            0.3f,
            0.025f
        );
        waveStartedFeedbacks = CreateScaleFeedback(
            "Wave Started Feedbacks",
            waveAnnouncement.transform,
            AnnouncementDuration,
            0.035f
        );
        waveStartedFeedbacks.AddFeedback(new MMF_TMPAlpha{
            TargetTMPText = waveAnnouncement,
            AlphaMode = MMF_TMPAlpha.AlphaModes.Interpolate,
            Duration = AnnouncementDuration,
            Curve = new MMTweenType(new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.2f, 1f),
                new Keyframe(0.65f, 1f), new Keyframe(1f, 0f))),
            CurveRemapZero = 0f,
            CurveRemapOne = 1f,
            AllowAdditivePlays = false,
            Timing = new MMFeedbackTiming{ TimescaleMode = TimescaleModes.Unscaled }
        });
        waveStartedFeedbacks.Initialization();
        progressFeedbacks = CreateScaleFeedback(
            "Wave Progress Feedbacks", waveText.transform, 0.32f, 0.018f);
        waveAnnouncement.gameObject.SetActive(false);
    }

    private IEnumerator AnimateWaveAnnouncement(){
        float elapsed = 0f;
        while (elapsed < AnnouncementDuration){
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / AnnouncementDuration);
            float entry = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.25f));
            waveAnnouncement.rectTransform.anchoredPosition =
                announcementStartPosition + Vector2.up * Mathf.Lerp(-14f, 0f, entry);
            yield return null;
        }

        waveAnnouncement.gameObject.SetActive(false);
        announcementAnimation = null;
    }

    private MMF_Player CreateScaleFeedback(
        string objectName,
        Transform target,
        float duration,
        float scaleAmount
    ){
        GameObject feedbackObject = new GameObject(objectName);
        feedbackObject.transform.SetParent(transform, false);

        MMF_Player feedbacks = feedbackObject.AddComponent<MMF_Player>();
        feedbacks.AddFeedback(new MMF_Scale{
            Mode = MMF_Scale.Modes.Additive,
            AnimateScaleTarget = target,
            AnimateScaleDuration = duration,
            RemapCurveZero = 0f,
            RemapCurveOne = scaleAmount,
            UniformScaling = true,
            AllowAdditivePlays = false,
            DetermineScaleOnPlay = false,
            Timing = new MMFeedbackTiming{ TimescaleMode = TimescaleModes.Unscaled },
            AnimateScaleTweenX = new MMTweenType(new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0f)))
        });
        feedbacks.Initialization();
        return feedbacks;
    }

    private void StyleHud(){
        waveText.fontSize = 13f;
        waveText.alignment = TextAlignmentOptions.Center;
        waveText.margin = Vector4.zero;
        waveText.textWrappingMode = TextWrappingModes.NoWrap;
        waveText.raycastTarget = false;
        nextWaveText.fontSize = 14f;
        nextWaveText.alignment = TextAlignmentOptions.Center;
        nextWaveText.margin = Vector4.zero;
        nextWaveText.textWrappingMode = TextWrappingModes.NoWrap;
        nextWaveText.raycastTarget = false;

        if (startWaveButtonText != null){
            startWaveButtonText.fontSize = 18f;
            startWaveButtonText.fontStyle = FontStyles.Bold;
            startWaveButtonText.color = Color.white;
            startWaveButtonText.raycastTarget = false;
        }

        ColorBlock colors = startWaveButton.colors;
        colors.normalColor = new Color32(24, 99, 132, 255);
        colors.highlightedColor = new Color32(36, 136, 170, 255);
        colors.pressedColor = new Color32(20, 75, 108, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color32(45, 60, 70, 180);
        colors.fadeDuration = 0.08f;
        startWaveButton.colors = colors;
    }

    private void UpdateWaveProgress(){
        int totalWaves = waveManager.TotalWaveCount;

        if (waveProgressBar != null){
            waveProgressBar.SetProgress(
                totalWaves,
                waveManager.CurrentWaveNumber,
                waveManager.CompletedWaveCount,
                waveManager.WaveActive,
                waveManager.CurrentWaveProgress
            );
        }

        int wave = waveManager.CurrentWaveNumber;
        int percent = Mathf.FloorToInt(waveManager.CurrentWaveProgress * 100f);
        int state = waveManager.CompletedWaveCount >= totalWaves ? 3 :
            waveManager.WaitingForFirstWave ? 0 : waveManager.WaveActive ? 1 : 2;
        if (lastStatusWave == wave && lastStatusTotal == totalWaves &&
            lastStatusPercent == percent && lastStatusState == state){
            return;
        }

        waveText.text = totalWaves == 0 ? "SIN OLEADAS" :
            $"<color=#9BAFC4>OLEADA</color>  <color=#E3F5FF><b>{wave:00}</b></color>";
        int milestone = percent / 10;
        if (wave != lastFeedbackWave){
            lastProgressMilestone = 0;
            lastFeedbackWave = wave;
        }
        if (milestone > lastProgressMilestone){
            // One quiet pulse per 10%, not one animation per enemy hit.
            progressFeedbacks?.PlayFeedbacks(waveText.transform.position);
            lastProgressMilestone = milestone;
        }
        lastStatusWave = wave;
        lastStatusTotal = totalWaves;
        lastStatusPercent = percent;
        lastStatusState = state;
    }
}
