using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float moveSpeed = 1.5f;

    private float remainingLifetime;

    private void Awake(){
        remainingLifetime = lifetime;
    }

    private void Update(){
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        remainingLifetime -= Time.deltaTime;

        float alpha = remainingLifetime / lifetime;

        Color textColor = damageText.color;
        textColor.a = alpha;
        damageText.color = textColor;

        if (remainingLifetime <= 0f){
            Destroy(gameObject);
        }
    }

    public void Setup(float damage){
        if (damageText == null){
            return;
        }

        damageText.text = Mathf.RoundToInt(damage).ToString();
    }
}
