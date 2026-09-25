using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseHealthUI : MonoBehaviour{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image damageFlash;
    [SerializeField, Range(0f, 0.5f)] private float flashOpacity = 0.22f;
    [SerializeField, Min(0.05f)] private float flashDuration = 0.35f;

    private BaseHealth baseHealth;
    private MMF_Player hitFeedback;
    private float flashRemaining;

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
            damageFlash.color = new Color(0.65f, 0.025f, 0.035f, 0f);
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
            hitFeedback.Initialization();
            healthText.raycastTarget = false;
        }
    }

    private void OnDestroy(){
        if (baseHealth != null) baseHealth.HealthChanged -= OnHealthChanged;
    }

    private void Update(){
        if (flashRemaining <= 0f || damageFlash == null) return;
        flashRemaining = Mathf.Max(0f, flashRemaining - Time.unscaledDeltaTime);
        float pulse = flashRemaining / flashDuration;
        damageFlash.color = new Color(0.65f, 0.025f, 0.035f, flashOpacity * pulse * pulse);
    }

    private void OnHealthChanged(float remaining, float lost){
        RefreshText();
        if (lost <= 0f) return;
        flashRemaining = flashDuration;
        if (damageFlash != null)
            damageFlash.color = new Color(0.65f, 0.025f, 0.035f, flashOpacity);
        hitFeedback?.PlayFeedbacks();
        CameraMovement cameraMovement = Camera.main != null
            ? Camera.main.GetComponent<CameraMovement>() : null;
        cameraMovement?.PlayBaseDamageShake();
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
