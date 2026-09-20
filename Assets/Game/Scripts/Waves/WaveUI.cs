using System.Collections;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveUI : MonoBehaviour{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text nextWaveText;
    [SerializeField] private Button startWaveButton;

    private WaveManager waveManager;
    private TMP_Text startWaveButtonText;
    private MMF_Player waveRequestedFeedbacks;
    private MMF_Player waveStartedFeedbacks;
    private TMP_Text waveAnnouncement;
    private Coroutine announcementAnimation;
    private Vector2 announcementStartPosition;

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

    private void Update(){
        if (waveManager == null){
            return;
        }

        waveText.text =
            $"<mark=#111827E6><color=#8BD5FF><b>  OLEADA " +
            $"{waveManager.CurrentWaveNumber}  </b></color></mark>";

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

            nextWaveText.text =
                $"<mark=#111827D9>  Siguiente oleada en " +
                $"<color=#FFD36A><b>{seconds}s</b></color>  </mark>";

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
        waveRequestedFeedbacks?.StopFeedbacks();

        if (announcementAnimation != null){
            StopCoroutine(announcementAnimation);
        }

        waveAnnouncement.gameObject.SetActive(true);
        waveAnnouncement.text =
            $"<mark=#111827F2><color=#8BD5FF><b>  OLEADA {waveNumber}  </b></color></mark>";
        waveAnnouncement.color = Color.white;
        waveAnnouncement.rectTransform.anchoredPosition =
            announcementStartPosition;

        waveStartedFeedbacks?.PlayFeedbacks(
            waveAnnouncement.transform.position
        );
        announcementAnimation = StartCoroutine(AnimateWaveAnnouncement());
    }

    private void ConfigureFeelFeedbacks(){
        waveAnnouncement = Instantiate(waveText, waveText.transform.parent);
        waveAnnouncement.name = "Wave Announcement";
        waveAnnouncement.fontSize = 52f;
        waveAnnouncement.alignment = TextAlignmentOptions.Center;
        waveAnnouncement.raycastTarget = false;

        RectTransform announcementRect = waveAnnouncement.rectTransform;
        announcementRect.anchorMin = new Vector2(0.5f, 0.5f);
        announcementRect.anchorMax = new Vector2(0.5f, 0.5f);
        announcementRect.pivot = new Vector2(0.5f, 0.5f);
        announcementRect.anchoredPosition = new Vector2(0f, 150f);
        announcementRect.sizeDelta = new Vector2(700f, 100f);
        announcementStartPosition = announcementRect.anchoredPosition;

        waveRequestedFeedbacks = CreateScaleFeedback(
            "Wave Requested Feedbacks",
            waveText.transform,
            0.14f,
            0.07f
        );
        waveStartedFeedbacks = CreateScaleFeedback(
            "Wave Started Feedbacks",
            waveAnnouncement.transform,
            0.38f,
            0.28f
        );
        waveAnnouncement.gameObject.SetActive(false);
    }

    private IEnumerator AnimateWaveAnnouncement(){
        const float holdDuration = 0.8f;
        const float fadeDuration = 0.45f;

        yield return new WaitForSecondsRealtime(holdDuration);

        float elapsed = 0f;
        Color baseColor = waveAnnouncement.color;

        while (elapsed < fadeDuration){
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            waveAnnouncement.rectTransform.anchoredPosition =
                announcementStartPosition + Vector2.up * (progress * 35f);

            Color color = baseColor;
            color.a = 1f - progress;
            waveAnnouncement.color = color;
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
            DetermineScaleOnPlay = false
        });
        feedbacks.Initialization();
        return feedbacks;
    }

    private void StyleHud(){
        waveText.fontSize = 24f;
        waveText.raycastTarget = false;
        nextWaveText.fontSize = 18f;
        nextWaveText.raycastTarget = false;

        if (startWaveButtonText != null){
            startWaveButtonText.fontSize = 18f;
            startWaveButtonText.fontStyle = FontStyles.Bold;
            startWaveButtonText.color = Color.white;
            startWaveButtonText.raycastTarget = false;
        }

        ColorBlock colors = startWaveButton.colors;
        colors.normalColor = new Color32(31, 157, 106, 255);
        colors.highlightedColor = new Color32(44, 190, 130, 255);
        colors.pressedColor = new Color32(20, 112, 76, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color32(45, 60, 70, 180);
        colors.fadeDuration = 0.08f;
        startWaveButton.colors = colors;
    }
}
