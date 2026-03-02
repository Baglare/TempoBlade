using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TempoUI : MonoBehaviour
{
    [Header("UI Components")]
    public Slider tempoSlider;
    public Image fillImage; // Renk degisimi icin

    [Header("Tempo Görsel Config (Opsiyonel)")]
    [Tooltip("Null bırakılırsa alttaki sabit renkler kullanılır")]
    public TempoTierVisualSO visualConfig;

    [Header("Fallback Colors (visualConfig yoksa)")]
    public Color tier0Color = Color.white;
    public Color tier1Color = Color.cyan;
    public Color tier2Color = Color.red;
    public Color tier3Color = new Color(1f, 0.3f, 1f); // Mor/Magenta

    [Header("Geçiş")]
    public float colorLerpDuration = 0.3f;

    private Coroutine colorCoroutine;

    private void Start()
    {
        // Baslangic degerlerini al
        if (TempoManager.Instance != null)
        {
            // Eventlere abone ol (Start icinde garanti olsun)
            TempoManager.Instance.OnTempoChanged += UpdateSlider;
            TempoManager.Instance.OnTierChanged += UpdateColor;
            
            UpdateSlider(TempoManager.Instance.tempo);
            ApplyColorInstant(TempoManager.Instance.CurrentTier);
        }
    }

    private void OnDestroy()
    {
        if (TempoManager.Instance != null)
        {
            TempoManager.Instance.OnTempoChanged -= UpdateSlider;
            TempoManager.Instance.OnTierChanged -= UpdateColor;
        }
    }

    private void UpdateSlider(float value)
    {
        if (tempoSlider != null)
        {
            tempoSlider.value = value / 100f; // 0-1 arasina normalize et
        }
    }

    private void UpdateColor(TempoManager.TempoTier tier)
    {
        if (fillImage == null) return;

        Color targetColor = GetColorForTier(tier);

        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(LerpColor(targetColor));
    }

    private void ApplyColorInstant(TempoManager.TempoTier tier)
    {
        if (fillImage == null) return;
        fillImage.color = GetColorForTier(tier);
    }

    private Color GetColorForTier(TempoManager.TempoTier tier)
    {
        // Config varsa oradan oku
        if (visualConfig != null)
            return visualConfig.GetVisual(tier).uiBarColor;

        // Fallback: sabit renkler
        switch (tier)
        {
            case TempoManager.TempoTier.T1: return tier1Color;
            case TempoManager.TempoTier.T2: return tier2Color;
            case TempoManager.TempoTier.T3: return tier3Color;
            default: return tier0Color;
        }
    }

    private IEnumerator LerpColor(Color targetColor)
    {
        Color startColor = fillImage.color;
        float elapsed = 0f;

        while (elapsed < colorLerpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fillImage.color = Color.Lerp(startColor, targetColor, elapsed / colorLerpDuration);
            yield return null;
        }
        fillImage.color = targetColor;
    }
}
