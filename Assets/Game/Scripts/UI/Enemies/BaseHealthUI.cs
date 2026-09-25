using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseHealthUI : MonoBehaviour{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image damageFlash;
    [SerializeField, Range(0f, 1f)] private float flashOpacity = 0.65f;
    [SerializeField, Min(0.05f)] private float flashDuration = 0.35f;

    private BaseHealth baseHealth;
    private MMF_Player hitFeedback;
    private float flashAlpha;
    private float lastImpactTime = -10f;
    private float lastFeedbackTime = -10f;
    private int flashVariant;

    private void Start(){
        baseHealth = FindFirstObjectByType<BaseHealth>();
        if (baseHealth == null){
            Debug.LogWarning("BaseHealth was not found.");
            return;
        }

        baseHealth.HealthChanged += OnHealthChanged;
        RefreshText();
        if (damageFlash != null){
            damageFlash.raycastTarget = false;
            damageFlash.color = new Color(1f, 1f, 1f, 0f);
        }

        if (healthText != null){
            GameObject feedbackObject = new GameObject("Base Hit FEEL");
            feedbackObject.transform.SetParent(healthText.transform, false);
            hitFeedback = feedbackObject.AddComponent<MMF_Player>();
            hitFeedback.AddFeedback(new MMF_Scale{
                AnimateScaleTarget = healthText.transform,
                Mode = MMF_Scale.Modes.Additive,
                AnimateScaleDuration = 0.2f,
                RemapCurveZero = 0f,
                RemapCurveOne = 0.13f,
                UniformScaling = true,
                AllowAdditivePlays = false
            });
            hitFeedback.AddFeedback(new MMF_CameraShake{
                Channel = CameraMovement.BaseDamageShakeChannel,
                CameraShakeProperties = new MMCameraShakeProperties(0.3f, 0.35f, 24f)
            });
            hitFeedback.Initialization();
            healthText.raycastTarget = false;
        }
    }

    private void OnDestroy(){
        if (baseHealth != null) baseHealth.HealthChanged -= OnHealthChanged;
    }

    private void Update(){
        if (flashAlpha <= 0f || damageFlash == null) return;
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f,
            flashOpacity / flashDuration * Time.unscaledDeltaTime);
        float brightness = flashOpacity > 0f ? flashAlpha / flashOpacity : 0f;
        Color tint = Color.Lerp(new Color(0.7f, 0.6f, 0.6f), Color.white, brightness);
        tint.a = flashAlpha;
        damageFlash.color = tint;
    }

    private void OnHealthChanged(float remaining, float lost){
        RefreshText();
        if (lost <= 0f) return;
        float now = Time.unscaledTime;
        bool newBurst = now - lastImpactTime > flashDuration;
        lastImpactTime = now;

        if (newBurst && damageFlash != null){
            // A burst keeps one orientation; rapid hits reinforce it instead of
            // stacking or swapping identical splashes every frame.
            flashVariant = (flashVariant + Random.Range(1, 4)) % 4;
            RectTransform rect = damageFlash.rectTransform;
            rect.localScale = new Vector3((flashVariant & 1) == 0 ? 1f : -1f, 1f, 1f);
            rect.localRotation = Quaternion.Euler(0f, 0f,
                (flashVariant & 2) == 0 ? 0f : 180f);
        }

        flashAlpha = newBurst
            ? Mathf.Max(flashAlpha, flashOpacity * 0.78f)
            : Mathf.Min(flashOpacity, flashAlpha + flashOpacity * 0.3f);
        if (damageFlash != null)
            damageFlash.color = new Color(1f, 1f, 1f, flashAlpha);

        if (now - lastFeedbackTime >= 0.15f){
            lastFeedbackTime = now;
            hitFeedback?.PlayFeedbacks();
        }
    }

    private void RefreshText(){
        if (baseHealth == null || healthText == null) return;
        float ratio = baseHealth.MaxHealth > 0f
            ? baseHealth.CurrentHealth / baseHealth.MaxHealth : 0f;
        string color = ratio > 0.5f ? "#7EE787" : ratio > 0.25f ? "#FFD36A" : "#FF6B6B";
        healthText.text =
            $"<size=11><color=#9BAFC4>INTEGRIDAD</color></size>\n" +
            $"<color={color}><b>{baseHealth.CurrentHealth:0}</b></color> " +
            $"<size=14><color=#9BAFC4>/ {baseHealth.MaxHealth:0}</color></size>";
        healthText.fontSize = 22f;
    }
}
