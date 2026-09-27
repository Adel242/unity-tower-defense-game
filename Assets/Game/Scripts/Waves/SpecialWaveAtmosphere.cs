using UnityEngine;

// Visual-only atmosphere for milestone waves. Never changes wave or enemy rules.
public sealed class SpecialWaveAtmosphere : MonoBehaviour{
    [SerializeField] private Light sun;
    [SerializeField, Min(0.1f)] private float transitionSeconds = 1.8f;
    [SerializeField, Range(0f, 1f)] private float sunMultiplier = 0.45f;
    [SerializeField] private Color specialSunColor = new Color(0.62f, 0.72f, 0.9f);
    [Header("Testing")]
    [SerializeField, Tooltip("Activa la iluminación especial en Play Mode sin esperar la oleada 5.")]
    private bool previewInPlayMode;

    private Color originalSunColor;
    private float originalSunIntensity;
    private float blend;
    private float targetBlend;
    private bool waveActive;
    private float appliedSunMultiplier = float.NaN;
    private Color appliedSunColor;

    private void Awake(){
        if (sun != null){
            originalSunColor = sun.color;
            originalSunIntensity = sun.intensity;
        }
    }

    public void Begin(){ waveActive = true; }

    public void End(){ waveActive = false; }

    private void Update(){
        targetBlend = waveActive || previewInPlayMode ? 1f : 0f;
        bool transitioning = !Mathf.Approximately(blend, targetBlend);
        if (transitioning)
            blend = Mathf.MoveTowards(blend, targetBlend, Time.deltaTime / transitionSeconds);

        // Inspector edits should be visible even after the transition has finished.
        if (transitioning || (blend > 0f &&
            (!Mathf.Approximately(appliedSunMultiplier, sunMultiplier) || appliedSunColor != specialSunColor)))
            Apply(blend);
    }

    private void Apply(float amount){
        if (amount <= 0f){
            Restore();
            return;
        }

        if (sun != null){
            sun.color = Color.Lerp(originalSunColor, specialSunColor, amount);
            sun.intensity = Mathf.Lerp(originalSunIntensity, originalSunIntensity * sunMultiplier, amount);
        }
        appliedSunMultiplier = sunMultiplier;
        appliedSunColor = specialSunColor;
    }

    private void OnDisable(){ Restore(); }

    private void Restore(){
        blend = targetBlend = 0f;
        appliedSunMultiplier = float.NaN;
        if (sun != null){
            sun.color = originalSunColor;
            sun.intensity = originalSunIntensity;
        }
    }
}
