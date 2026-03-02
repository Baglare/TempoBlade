using UnityEngine;
using System.Collections;

/// <summary>
/// Player objesine eklenir. Tempo tier degistiginde:
/// 1. Animator Controller swap (animasyon seti)
/// 2. SpriteRenderer tint gecisi (Color.Lerp)
/// 3. Kısa scale punch efekti (juice)
/// </summary>
public class TempoVisualSkin : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("TempoTierVisualSO asset'ini sürükle")]
    public TempoTierVisualSO visualConfig;

    [Header("Geçiş Ayarları")]
    [Tooltip("Renk tint geçiş süresi (saniye)")]
    public float tintLerpDuration = 0.25f;

    [Tooltip("Tier değişiminde scale punch büyüklüğü (0 = kapalı)")]
    public float scalePunchAmount = 0.15f;

    private Animator animator;
    private SpriteRenderer sr;
    private Coroutine tintCoroutine;
    private Coroutine punchCoroutine;
    private RuntimeAnimatorController originalController;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();

        if (animator != null)
            originalController = animator.runtimeAnimatorController;

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

        // 1. Animator Controller swap
        if (animator != null && visual.animatorController != null)
            animator.runtimeAnimatorController = visual.animatorController;
        else if (animator != null && visual.animatorController == null)
            animator.runtimeAnimatorController = originalController; // Fallback

        // 2. Tint geçişi (yumuşak)
        if (sr != null)
        {
            if (tintCoroutine != null) StopCoroutine(tintCoroutine);
            tintCoroutine = StartCoroutine(LerpTint(visual.playerTint));
        }

        // 3. Scale punch (juice)
        if (scalePunchAmount > 0f)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            punchCoroutine = StartCoroutine(ScalePunch());
        }
    }

    /// <summary>
    /// Gecis efekti olmadan aninda uygula (baslangic icin).
    /// </summary>
    private void ApplyInstant(TempoManager.TempoTier tier)
    {
        if (visualConfig == null) return;
        var visual = visualConfig.GetVisual(tier);

        if (animator != null && visual.animatorController != null)
            animator.runtimeAnimatorController = visual.animatorController;

        if (sr != null)
            sr.color = visual.playerTint;
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

    private IEnumerator ScalePunch()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 punchedScale = originalScale * (1f + scalePunchAmount);

        // Büyü (0.08s)
        float t = 0f;
        while (t < 0.08f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, punchedScale, t / 0.08f);
            yield return null;
        }

        // Küçül (0.15s)
        t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(punchedScale, originalScale, t / 0.15f);
            yield return null;
        }
        transform.localScale = originalScale;
    }
}
