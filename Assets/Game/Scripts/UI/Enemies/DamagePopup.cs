using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float spawnHorizontalSpread = 0.65f;
    [SerializeField, Min(0f)] private float spawnVerticalSpread = 0.35f;
    [SerializeField] private float horizontalSpread = 0.35f;
    [SerializeField] private float strongHitThreshold = 20f;
    [SerializeField] private Color normalHitColor = new Color(1f, 0.94f, 0.78f, 1f);
    [SerializeField] private Color strongHitColor = new Color(1f, 0.58f, 0.08f, 1f);

    private float remainingLifetime;
    private DamagePopupPool pool;
    private MMF_Player appearanceFeedbacks;
    private Vector3 originalScale;
    private Color originalColor;
    private float originalFontSize;
    private float horizontalSpeed;

    private void Awake(){
        remainingLifetime = lifetime;
        originalScale = transform.localScale;

        if (damageText != null){
            damageText.fontStyle = FontStyles.Bold;
            damageText.outlineWidth = 0.18f;
            damageText.outlineColor = new Color32(20, 13, 9, 230);
            damageText.characterSpacing = -2f;
            originalColor = normalHitColor;
            originalFontSize = Mathf.Min(damageText.fontSize, 48f);
        }

        appearanceFeedbacks = gameObject.AddComponent<MMF_Player>();
        appearanceFeedbacks.AddFeedback(new MMF_Scale{
            Mode = MMF_Scale.Modes.Additive,
            AnimateScaleTarget = transform,
            AnimateScaleDuration = 0.18f,
            RemapCurveZero = 0f,
            RemapCurveOne = originalScale.x * 0.25f,
            UniformScaling = true,
            AllowAdditivePlays = false,
            DetermineScaleOnPlay = false
        });
        appearanceFeedbacks.Initialization();
    }

    private void Update(){
        Vector3 movement = new Vector3(
            horizontalSpeed,
            moveSpeed,
            0f
        );
        transform.position += movement * Time.deltaTime;

        remainingLifetime -= Time.deltaTime;

        float normalizedLifetime = remainingLifetime / lifetime;
        float alpha = Mathf.SmoothStep(0f, 1f, normalizedLifetime * 1.8f);

        Color textColor = damageText.color;
        textColor.a = alpha;
        damageText.color = textColor;

        if (remainingLifetime <= 0f){
            if (pool != null){
                pool.Release(this);
                return;
            }

            Destroy(gameObject);
        }
    }

    public void SetPool(DamagePopupPool newPool){
        pool = newPool;
    }

    public void Setup(float damage){
        if (damageText == null){
            return;
        }

        ApplyRandomSpawnOffset();
        damageText.text = $"{Mathf.RoundToInt(damage)}";
        horizontalSpeed = Random.Range(-horizontalSpread, horizontalSpread);

        bool isStrongHit = damage >= strongHitThreshold;
        damageText.color = isStrongHit
            ? strongHitColor
            : normalHitColor;
        damageText.fontSize = originalFontSize * (isStrongHit ? 1.12f : 0.9f);

        transform.localScale = originalScale;
        appearanceFeedbacks?.PlayFeedbacks(transform.position);
    }

    private void ApplyRandomSpawnOffset(){
        // Spread in camera space so the separation remains visible after the
        // player rotates the battlefield. Vertical offset is always positive:
        // damage numbers stay above the enemy instead of covering its body.
        Camera gameplayCamera = Camera.main;
        Vector3 horizontalDirection = gameplayCamera != null
            ? gameplayCamera.transform.right
            : Vector3.right;
        horizontalDirection.y = 0f;
        horizontalDirection.Normalize();

        float horizontalOffset = Random.Range(
            -spawnHorizontalSpread,
            spawnHorizontalSpread
        );
        float verticalOffset = Random.Range(
            spawnVerticalSpread * 0.2f,
            spawnVerticalSpread
        );
        transform.position += horizontalDirection * horizontalOffset +
                              Vector3.up * verticalOffset;
    }

    public void ResetForPool(){
        appearanceFeedbacks?.StopFeedbacks();
        remainingLifetime = lifetime;
        horizontalSpeed = 0f;
        transform.localScale = originalScale;

        if (damageText == null){
            return;
        }

        damageText.text = string.Empty;
        damageText.color = originalColor;
        damageText.fontSize = originalFontSize;
    }
}
