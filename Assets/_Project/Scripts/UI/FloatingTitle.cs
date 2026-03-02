using UnityEngine;

public class FloatingTitle : MonoBehaviour
{
    [Header("Float Settings")]
    public float floatSpeed = 2f;
    public float floatAmplitude = 10f; // Canvas UI biriminde yukari asagi hareket miktari

    [Header("Pulse Settings (Optional)")]
    public float pulseSpeed = 1.5f;
    public float pulseAmount = 0.05f;

    private RectTransform rectTransform;
    private Vector2 startPos;
    private Vector3 startScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            startPos = rectTransform.anchoredPosition;
            startScale = rectTransform.localScale;
        }
    }

    private void Update()
    {
        if (rectTransform == null) return;

        // Yukarı-Aşağı süzülme efekti (Float)
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        rectTransform.anchoredPosition = new Vector2(startPos.x, newY);

        // Nefes alma / Büyüyüp küçülme (Pulse)
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        rectTransform.localScale = startScale * scale;
    }
}
