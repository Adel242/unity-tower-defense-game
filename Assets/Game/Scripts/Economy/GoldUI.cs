using System.Collections;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour{
    [SerializeField] private TMP_Text goldText;

    private PlayerGold playerGold;
    private MMF_Player goldFeedbacks;
    private TMP_Text goldChangeText;
    private Coroutine countAnimation;
    private Coroutine changeAnimation;
    private Vector2 goldChangeStartPosition;
    private int displayedGold;
    private int lastGold;
    private bool hasDisplayedGold;

    private void Awake(){
        if (goldText == null){
            return;
        }

        goldFeedbacks = gameObject.AddComponent<MMF_Player>();
        goldFeedbacks.AddFeedback(new MMF_Scale{
            Mode = MMF_Scale.Modes.Additive,
            AnimateScaleTarget = goldText.transform,
            AnimateScaleDuration = 0.2f,
            RemapCurveZero = 0f,
            RemapCurveOne = goldText.transform.localScale.x * 0.1f,
            UniformScaling = true,
            AllowAdditivePlays = false,
            DetermineScaleOnPlay = false
        });
        goldFeedbacks.Initialization();

        goldChangeText = Instantiate(goldText, goldText.transform.parent);
        goldChangeText.name = "Gold Change Popup";
        goldChangeText.text = string.Empty;
        goldChangeText.fontSize = 18f;
        goldChangeText.alignment = TextAlignmentOptions.Center;
        goldChangeText.raycastTarget = false;

        RectTransform changeRect = goldChangeText.rectTransform;
        changeRect.anchoredPosition =
            goldText.rectTransform.anchoredPosition + new Vector2(100f, 0f);
        changeRect.sizeDelta = new Vector2(120f, 30f);
        goldChangeStartPosition = changeRect.anchoredPosition;
        goldChangeText.gameObject.SetActive(false);
    }

    private void Start(){
        playerGold = FindFirstObjectByType<PlayerGold>();

        if (playerGold == null){
            Debug.LogWarning("PlayerGold was not found.");
            return;
        }

        playerGold.GoldChanged += UpdateGoldText;

        UpdateGoldText(playerGold.CurrentGold);
    }

    private void OnDestroy(){
        if (playerGold != null){
            playerGold.GoldChanged -= UpdateGoldText;
        }
    }

    private void UpdateGoldText(int gold){
        if (goldText == null){
            return;
        }

        goldText.fontSize = 24f;
        goldText.raycastTarget = false;

        if (!hasDisplayedGold){
            displayedGold = gold;
            lastGold = gold;
            SetGoldText(gold);
            hasDisplayedGold = true;
            return;
        }

        int difference = gold - lastGold;
        lastGold = gold;

        if (countAnimation != null){
            StopCoroutine(countAnimation);
        }
        countAnimation = StartCoroutine(AnimateGoldCount(gold));

        goldFeedbacks?.PlayFeedbacks(goldText.transform.position);
        ShowGoldChange(difference);
    }

    private IEnumerator AnimateGoldCount(int targetGold){
        int startGold = displayedGold;
        const float duration = 0.38f;
        float elapsed = 0f;

        while (elapsed < duration){
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            displayedGold = Mathf.RoundToInt(
                Mathf.Lerp(startGold, targetGold, easedProgress)
            );
            SetGoldText(displayedGold);
            yield return null;
        }

        displayedGold = targetGold;
        SetGoldText(displayedGold);
        countAnimation = null;
    }

    private void ShowGoldChange(int difference){
        if (difference == 0 || goldChangeText == null){
            return;
        }

        if (changeAnimation != null){
            StopCoroutine(changeAnimation);
        }

        goldChangeText.gameObject.SetActive(true);
        goldChangeText.rectTransform.anchoredPosition = goldChangeStartPosition;
        goldChangeText.transform.localScale = Vector3.one * 0.72f;
        goldChangeText.color = difference > 0
            ? new Color(1f, 0.83f, 0.42f, 1f)
            : new Color(1f, 0.38f, 0.28f, 1f);
        goldChangeText.text = difference > 0
            ? $"<b>+{difference}</b>"
            : $"<b>{difference}</b>";

        changeAnimation = StartCoroutine(AnimateGoldChange());
    }

    private IEnumerator AnimateGoldChange(){
        const float duration = 0.9f;
        float elapsed = 0f;
        Color baseColor = goldChangeText.color;

        while (elapsed < duration){
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float scaleProgress = 1f - Mathf.Pow(1f - Mathf.Min(progress / 0.25f, 1f), 3f);
            goldChangeText.transform.localScale = Vector3.one * Mathf.Lerp(
                0.72f,
                1f,
                scaleProgress
            );
            goldChangeText.rectTransform.anchoredPosition =
                goldChangeStartPosition + Vector2.up * (progress * 24f);

            Color color = baseColor;
            color.a = 1f - Mathf.InverseLerp(0.45f, 1f, progress);
            goldChangeText.color = color;
            yield return null;
        }

        goldChangeText.gameObject.SetActive(false);
        goldChangeText.transform.localScale = Vector3.one;
        changeAnimation = null;
    }

    private void SetGoldText(int gold){
        goldText.text =
            $"<mark=#111827E6><color=#FFD36A><b>  ORO  {gold}  </b></color></mark>";
    }
}
