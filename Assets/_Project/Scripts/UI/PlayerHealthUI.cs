using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public Slider healthSlider;
    public Image fillImage;
    public Color highHealthColor = Color.green;
    public Color lowHealthColor  = Color.red;

    [Header("Dinamik Genişlik")]
    [Tooltip("Her 1 can puanı için slider kaç pixel genişlesin? (0 = sabit genişlik)")]
    public float pixelsPerHealth = 1f;

    private PlayerCombat playerCombat;
    private RectTransform sliderRect;
    private float baseWidth;
    private const float BASE_MAX_HEALTH = 100f;

    private void Start()
    {
        if (healthSlider != null)
        {
            sliderRect = healthSlider.GetComponent<RectTransform>();
            if (sliderRect != null)
                baseWidth = sliderRect.sizeDelta.x; // Başlangıç genişliğini kaydet
        }

        playerCombat = FindFirstObjectByType<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(playerCombat.currentHealth, playerCombat.maxHealth);
        }
        else
        {
            Debug.LogWarning("PlayerHealthUI: PlayerCombat not found!");
        }
    }

    private void OnDestroy()
    {
        if (playerCombat != null)
            playerCombat.OnHealthChanged -= UpdateHealthUI;
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (healthSlider == null) return;

        healthSlider.maxValue = max;
        healthSlider.value    = current;

        // Fiziksel genişliği max health'e orantılı büyüt
        if (sliderRect != null && pixelsPerHealth > 0f)
        {
            float extraHealth = Mathf.Max(0f, max - BASE_MAX_HEALTH);
            float newWidth    = baseWidth + extraHealth * pixelsPerHealth;
            sliderRect.sizeDelta = new Vector2(newWidth, sliderRect.sizeDelta.y);
        }

        if (fillImage != null)
            fillImage.color = Color.Lerp(lowHealthColor, highHealthColor, current / max);
    }
}
