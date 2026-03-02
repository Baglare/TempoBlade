using UnityEngine;
using System.Collections;

/// <summary>
/// Her düşmana eklenir (EnemyBase olan objeye).
/// Tempo tier degistiginde:
/// 1. Scale lerp (T2'de küçült, T3'te daha da küçült)
/// 2. Tint degisimi
/// 3. Overlay prefab (T3'te neon çarpı gibi efektler — prefab sürüklenir)
/// </summary>
public class TempoEnemyEffect : MonoBehaviour
{
    [Header("Config (Opsiyonel — null ise global config aranır)")]
    [Tooltip("null bırakılırsa sahnedeki TempoVisualSkin'den config alınır")]
    public TempoTierVisualSO visualConfig;

    [Header("Geçiş Ayarları")]
    public float scaleLerpDuration = 0.3f;
    public float tintLerpDuration = 0.25f;

    private Vector3 originalScale;
    private SpriteRenderer sr;
    private GameObject activeOverlay;
    private Coroutine scaleCoroutine;
    private Coroutine tintCoroutine;

    private void Start()
    {
        originalScale = transform.localScale;
        sr = GetComponentInChildren<SpriteRenderer>();

        // Config atanmamışsa, sahnedeki player'ın TempoVisualSkin'inden al
        if (visualConfig == null)
        {
            TempoVisualSkin playerSkin = FindFirstObjectByType<TempoVisualSkin>();
            if (playerSkin != null) visualConfig = playerSkin.visualConfig;
        }

        if (TempoManager.Instance != null)
        {
            TempoManager.Instance.OnTierChanged += OnTierChanged;
            // Mevcut tier'a göre başlat (geçiş efekti olmadan)
            ApplyInstant(TempoManager.Instance.CurrentTier);
        }
    }

    private void OnDestroy()
    {
        if (TempoManager.Instance != null)
            TempoManager.Instance.OnTierChanged -= OnTierChanged;
    }

    private void OnTierChanged(TempoManager.TempoTier newTier)
    {
        if (visualConfig == null) return;
        var visual = visualConfig.GetVisual(newTier);

        // 1. Scale lerp
        float targetScale = visual.enemyScaleMultiplier;
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(LerpScale(targetScale));

        // 2. Tint lerp
        if (sr != null)
        {
            if (tintCoroutine != null) StopCoroutine(tintCoroutine);
            tintCoroutine = StartCoroutine(LerpTint(visual.enemyTint));
        }

        // 3. Overlay toggle
        HandleOverlay(visual);
    }

    private void ApplyInstant(TempoManager.TempoTier tier)
    {
        if (visualConfig == null) return;
        var visual = visualConfig.GetVisual(tier);

        transform.localScale = originalScale * visual.enemyScaleMultiplier;

        if (sr != null)
            sr.color = visual.enemyTint;

        HandleOverlay(visual);
    }

    private void HandleOverlay(TempoTierVisualSO.TierVisual visual)
    {
        // Overlay aktifse ve prefab varsa → oluştur
        if (visual.enemyOverlayEnabled && visual.enemyOverlayPrefab != null)
        {
            if (activeOverlay == null)
            {
                activeOverlay = Instantiate(visual.enemyOverlayPrefab, transform);
                activeOverlay.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            // Overlay kapalıysa → yok et
            if (activeOverlay != null)
            {
                Destroy(activeOverlay);
                activeOverlay = null;
            }
        }
    }

    private IEnumerator LerpScale(float targetMultiplier)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetMultiplier;
        float elapsed = 0f;

        while (elapsed < scaleLerpDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / scaleLerpDuration);
            yield return null;
        }
        transform.localScale = endScale;
    }

    private IEnumerator LerpTint(Color targetColor)
    {
        Color startColor = sr.color;
        float elapsed = 0f;

        while (elapsed < tintLerpDuration)
        {
            elapsed += Time.deltaTime;
            sr.color = Color.Lerp(startColor, targetColor, elapsed / tintLerpDuration);
            yield return null;
        }
        sr.color = targetColor;
    }
}
