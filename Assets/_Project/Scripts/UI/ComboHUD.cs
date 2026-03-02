using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Kombo adımlarını nokta göstergesiyle (● ○) ekranda gösterir.
/// PlayerCombat.OnComboChanged eventine abone olur.
/// Canvas'ta bir TextMeshProUGUI elementine ihtiyaç duyar.
/// </summary>
public class ComboHUD : MonoBehaviour
{
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private float fadeOutDelay = 1.2f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (comboText != null)
        {
            comboText.text  = "";
            comboText.alpha = 0f;
        }
    }

    private void Start()
    {
        // Her zaman sahnedeki instance'ı bul (Inspector'a prefab atanmış olabilir)
        playerCombat = FindObjectOfType<PlayerCombat>();

        if (playerCombat != null)
            playerCombat.OnComboChanged += HandleComboChanged;
        else
            Debug.LogWarning("ComboHUD: PlayerCombat bulunamadı!");
    }

    private void OnDestroy()
    {
        if (playerCombat != null)
            playerCombat.OnComboChanged -= HandleComboChanged;
    }

    private void HandleComboChanged(int current, int total)
    {
        Debug.Log($"[ComboHUD] HandleComboChanged çağrıldı: {current}/{total}, comboText null? {comboText == null}");
        if (comboText == null) return;

        if (total == 0)
        {
            comboText.alpha = 0f;
            return;
        }

        if (current == 0) // Sıfırlama / whiff
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeOut());
            return;
        }

        // Nokta dizisi oluştur: tamamlananlar beyaz, bekleyenler gri
        string dots = "";
        for (int i = 0; i < total; i++)
            dots += (i < current) ? "<color=white>●</color> " : "<color=#666666>○</color> ";

        comboText.text  = dots.Trim();
        comboText.alpha = 1f;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        // Son adım tamamlanınca fade başlat
        if (current == total)
            fadeRoutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(fadeOutDelay);

        float elapsed = 0f;
        const float fadeDuration = 0.3f;
        while (elapsed < fadeDuration)
        {
            comboText.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        comboText.alpha = 0f;
    }
}
