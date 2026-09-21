using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A scene-authored, five-wave experience rail with independently anchored labels.</summary>
public class WaveProgressBar : MaskableGraphic{
    public const int WavesPerPage = 5;
    private const float HorizontalPadding = 18f;

    [SerializeField] private TMP_Text[] segmentLabels;
    [SerializeField, Min(1f)] private float segmentGap = 12f;
    [SerializeField, Min(0f)] private float borderSize = 2f;
    [SerializeField] private Color borderColor = new Color32(57, 79, 100, 255);
    [SerializeField] private Color pendingColor = new Color32(17, 28, 41, 255);
    [SerializeField] private Color completedColor = new Color32(56, 205, 176, 255);
    [SerializeField] private Color currentColor = new Color32(78, 195, 247, 255);
    [SerializeField] private Color specialColor = new Color32(245, 202, 112, 255);

    private int totalWaves = 12;
    private int currentWave = 1;
    private int completedWaves;
    private bool waveActive;
    private float targetProgress;
    private float displayedProgress;
    private float progressVelocity;
    private float progressGlow;

    // Keep the completed block visible during the break, then switch when the
    // next wave actually starts. The last page contains only real waves.
    public int FirstVisibleWave => ((Mathf.Max(1, currentWave) - 1) / WavesPerPage) * WavesPerPage + 1;
    public int VisibleWaveCount => Mathf.Clamp(totalWaves - FirstVisibleWave + 1, 0, WavesPerPage);

    protected override void OnEnable(){
        base.OnEnable();
        displayedProgress = targetProgress;
        RefreshLabels();
    }

    protected override void OnRectTransformDimensionsChange(){
        base.OnRectTransformDimensionsChange();
        RefreshLabels();
    }

    public void SetProgress(int total, int current, int completed, bool active, float progress){
        total = Mathf.Max(0, total);
        current = Mathf.Clamp(current, 1, Mathf.Max(1, total));
        completed = Mathf.Clamp(completed, 0, total);
        int oldFirstWave = FirstVisibleWave;
        bool labelsChanged = totalWaves != total || currentWave != current ||
            completedWaves != completed || waveActive != active;

        totalWaves = total;
        currentWave = current;
        completedWaves = completed;
        waveActive = active;
        float previousProgress = targetProgress;
        targetProgress = Mathf.Clamp(completed - FirstVisibleWave + 1, 0, VisibleWaveCount);

        if (active && completed < current){
            targetProgress += Mathf.Clamp01(progress);
        }

        targetProgress = Mathf.Clamp(targetProgress, 0f, VisibleWaveCount);
        if (targetProgress > previousProgress){
            progressGlow = 1f;
        }
        if (oldFirstWave != FirstVisibleWave || displayedProgress > targetProgress){
            displayedProgress = targetProgress;
            progressVelocity = 0f;
            progressGlow = 0f;
            SetVerticesDirty();
        }

        if (labelsChanged){
            RefreshLabels();
            SetVerticesDirty();
        }
    }

    private void Update(){
        if (!Application.isPlaying ||
            (Mathf.Approximately(displayedProgress, targetProgress) && progressGlow <= 0f)){
            return;
        }

        displayedProgress = Mathf.SmoothDamp(displayedProgress, targetProgress,
            ref progressVelocity, 0.22f, Mathf.Infinity, Time.unscaledDeltaTime);
        if (Mathf.Abs(displayedProgress - targetProgress) < 0.0001f){
            displayedProgress = targetProgress;
            progressVelocity = 0f;
        }
        progressGlow = Mathf.MoveTowards(progressGlow, 0f, Time.unscaledDeltaTime * 2.5f);
        SetVerticesDirty();
    }

    private void RefreshLabels(){
        if (segmentLabels == null){
            return;
        }

        int count = VisibleWaveCount;
        for (int slot = 0; slot < segmentLabels.Length; slot++){
            TMP_Text label = segmentLabels[slot];
            if (label == null){
                continue;
            }

            bool visible = slot < count;
            label.gameObject.SetActive(visible);
            if (!visible){
                continue;
            }

            int wave = FirstVisibleWave + slot;
            bool special = wave % WavesPerPage == 0;
            bool revealed = wave <= completedWaves || wave == currentWave;
            label.text = revealed ? wave.ToString() : "?";
            label.color = special ? specialColor :
                wave == currentWave ? new Color32(229, 246, 255, 255) :
                wave <= completedWaves ? completedColor : new Color32(122, 145, 166, 255);
            label.alpha = 1f;
            label.raycastTarget = false;
            label.transform.SetAsLastSibling();

            float center = (slot + 0.5f) / count;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(center, 1f);
            labelRect.anchoredPosition = new Vector2(HorizontalPadding * (1f - 2f * center), -22f);
            labelRect.sizeDelta = new Vector2(
                Mathf.Max(1f, (rectTransform.rect.width - HorizontalPadding * 2f) / count - 8f), 28f
            );
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh){
        vh.Clear();
        Rect panel = GetPixelAdjustedRect();
        if (panel.width <= HorizontalPadding * 2f || panel.height < 64f){
            return;
        }

        Rect shadow = panel;
        shadow.y -= 3f;
        AddBeveled(vh, shadow, 9f, new Color(0f, 0f, 0f, 0.32f));
        AddBeveled(vh, panel, 8f, borderColor);
        AddBeveled(vh, Inset(panel, 1f), 7f, new Color32(11, 18, 29, 246));
        AddQuad(vh, new Rect(panel.xMin + 12f, panel.yMax - 2f, panel.width - 24f, 1f),
            new Color32(125, 185, 214, 65));

        int count = VisibleWaveCount;
        if (count == 0){
            return;
        }

        Rect rail = new Rect(panel.xMin + HorizontalPadding, panel.yMin + 28f,
            panel.width - HorizontalPadding * 2f, 18f);
        float cellWidth = rail.width / count;
        for (int slot = 0; slot < count; slot++){
            int wave = FirstVisibleWave + slot;
            bool special = wave % WavesPerPage == 0;
            Rect cell = new Rect(rail.x + slot * cellWidth, rail.y, cellWidth, rail.height);
            Rect surround = new Rect(cell.x + segmentGap * 0.5f, cell.y,
                cell.width - segmentGap, cell.height);
            AddBeveled(vh, surround, 2f, special ? specialColor : borderColor);
            Rect inside = Inset(surround, borderSize * 0.5f);
            AddQuad(vh, inside, pendingColor);
            Color accent = special ? specialColor :
                Color.Lerp(currentColor, completedColor, slot / 4f);

            if (special){
                AddQuad(vh, inside, new Color32(57, 44, 25, 255));
                AddDiamond(vh, new Vector2(cell.center.x, panel.yMax - 7f), 3f, specialColor);
            }

            float fill = Mathf.Clamp01(displayedProgress - slot);
            if (fill > 0f){
                Rect filled = inside;
                filled.width *= fill;
                Color dark = Color.Lerp(accent, Color.black, 0.35f);
                Color litAccent = Color.Lerp(accent, specialColor, progressGlow * 0.25f);
                AddQuad(vh, filled, dark, litAccent);
                AddQuad(vh, new Rect(filled.x, filled.yMax - 2f, filled.width, 1f),
                    Color.Lerp(accent, Color.white, 0.55f));

                if (fill < 1f){
                    AddQuad(vh, new Rect(filled.xMax - Mathf.Min(2f, filled.width), filled.y,
                        Mathf.Min(2f, filled.width), filled.height), new Color32(208, 243, 255, 220));
                }
            }

        }
    }

    private static Rect Inset(Rect rect, float amount){
        return new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);
    }

    private void AddQuad(VertexHelper vh, Rect rect, Color shade){
        AddQuad(vh, rect, shade, shade);
    }

    private void AddQuad(VertexHelper vh, Rect rect, Color bottom, Color top){
        if (rect.width <= 0f || rect.height <= 0f){
            return;
        }

        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(rect.xMin, rect.yMin), bottom * color, Vector2.zero);
        vh.AddVert(new Vector3(rect.xMin, rect.yMax), top * color, Vector2.zero);
        vh.AddVert(new Vector3(rect.xMax, rect.yMax), top * color, Vector2.zero);
        vh.AddVert(new Vector3(rect.xMax, rect.yMin), bottom * color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }

    private void AddBeveled(VertexHelper vh, Rect rect, float bevel, Color shade){
        AddQuad(vh, new Rect(rect.x + bevel, rect.y, rect.width - 2f * bevel, rect.height), shade);
        int start = vh.currentVertCount;
        // Four vertices per side make a chamfer without a material or texture.
        for (int side = 0; side < 2; side++){
            float outer = side == 0 ? rect.xMin : rect.xMax;
            float inner = side == 0 ? rect.xMin + bevel : rect.xMax - bevel;
            vh.AddVert(new Vector3(inner, rect.yMin), shade * color, Vector2.zero);
            vh.AddVert(new Vector3(outer, rect.yMin + bevel), shade * color, Vector2.zero);
            vh.AddVert(new Vector3(outer, rect.yMax - bevel), shade * color, Vector2.zero);
            vh.AddVert(new Vector3(inner, rect.yMax), shade * color, Vector2.zero);
            int i = start + side * 4;
            if (side == 0){
                vh.AddTriangle(i, i + 1, i + 2);
                vh.AddTriangle(i + 2, i + 3, i);
            }
            else{
                vh.AddTriangle(i + 2, i + 1, i);
                vh.AddTriangle(i, i + 3, i + 2);
            }
        }
    }

    private void AddDiamond(VertexHelper vh, Vector2 center, float radius, Color shade){
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(center.x - radius, center.y), shade * color, Vector2.zero);
        vh.AddVert(new Vector3(center.x, center.y + radius), shade * color, Vector2.zero);
        vh.AddVert(new Vector3(center.x + radius, center.y), shade * color, Vector2.zero);
        vh.AddVert(new Vector3(center.x, center.y - radius), shade * color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start + 2, start + 3, start);
    }
}
