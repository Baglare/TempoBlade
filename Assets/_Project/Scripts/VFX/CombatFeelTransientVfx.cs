using UnityEngine;

public class CombatFeelTransientVfx : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Color baseColor = Color.white;
    private float lifetime = 0.15f;
    private float age;

    public void Initialize(LineRenderer renderer, Color color, float duration)
    {
        lineRenderer = renderer;
        baseColor = color;
        lifetime = Mathf.Max(0.02f, duration);

        if (lineRenderer != null)
        {
            lineRenderer.startColor = baseColor;
            lineRenderer.endColor = baseColor;
        }
    }

    private void Update()
    {
        age += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(age / lifetime);

        if (lineRenderer != null)
        {
            Color color = baseColor;
            color.a *= 1f - t;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        if (age >= lifetime)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (lineRenderer != null && lineRenderer.material != null)
            Destroy(lineRenderer.material);
    }
}
