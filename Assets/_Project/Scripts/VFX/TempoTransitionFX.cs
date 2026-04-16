using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Main Camera veya Canvas üstüne eklenir.
/// Tier degisiminde ekran flash + kamera sarsintisi uygular.
/// </summary>
public class TempoTransitionFX : MonoBehaviour
{
    [Header("Config")]
    public TempoTierVisualSO visualConfig;

    [Header("Flash Overlay")]
    [Tooltip("Canvas altinda tum ekrani kaplayan bir Image (alpha 0 ile baslar)")]
    public Image flashOverlay;

    [Header("Kamera Sarsıntısı")]
    [Tooltip("Sarsıntı süresi (saniye)")]
    public float shakeDuration = 0.15f;
    [Tooltip("Ardışık tier geçişlerinde aynı anda birden fazla flash/shake spam'ini önler.")]
    public float minTransitionInterval = 0.12f;

    private Transform camTransform;
    private Vector3 originalCamPos;
    private Coroutine flashCoroutine;
    private Coroutine shakeCoroutine;

    private TempoManager.TempoTier lastTier;
    private float lastTransitionTime = -999f;

    private void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
            originalCamPos = camTransform.localPosition;
        }

        if (flashOverlay != null)
        {
            Color c = flashOverlay.color;
            c.a = 0f;
            flashOverlay.color = c;
        }

        if (TempoManager.Instance != null)
        {
            lastTier = TempoManager.Instance.CurrentTier;
            TempoManager.Instance.OnTierChanged += OnTierChanged;
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

        if (Time.unscaledTime - lastTransitionTime < minTransitionInterval)
        {
            lastTier = newTier;
            return;
        }

        lastTransitionTime = Time.unscaledTime;

        var visual = visualConfig.GetVisual(newTier);
        bool isUpgrade = (int)newTier > (int)lastTier;
        lastTier = newTier;

        // Flash efekti
        if (flashOverlay != null && visual.transitionFlashDuration > 0f)
        {
            Color flashColor = visual.transitionFlashColor;
            // Düşüşlerde kırmızımsı flash
            if (!isUpgrade) flashColor = new Color(1f, 0.2f, 0.1f, flashColor.a);

            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine(flashColor, visual.transitionFlashDuration));
        }

        // Kamera sarsıntısı
        if (camTransform != null && visual.cameraShakeIntensity > 0f)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeRoutine(visual.cameraShakeIntensity, shakeDuration));
        }
    }

    private IEnumerator FlashRoutine(Color flashColor, float duration)
    {
        flashOverlay.color = flashColor;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // timeScale'den bağımsız
            float alpha = Mathf.Lerp(flashColor.a, 0f, elapsed / duration);
            flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            yield return null;
        }

        flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float x = Random.Range(-intensity, intensity);
            float y = Random.Range(-intensity, intensity);
            camTransform.localPosition = originalCamPos + new Vector3(x, y, 0f);
            yield return null;
        }

        camTransform.localPosition = originalCamPos;
    }
}
