using UnityEngine;

[DisallowMultipleComponent]
public class EnemyCastCircleTelegraph : MonoBehaviour
{
    [SerializeField] private float radius = 0.42f;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private float heightOffset = 1.35f;
    [SerializeField] private int segments = 48;
    [SerializeField] private Color baseColor = new Color(0.9f, 0.25f, 1f, 0.9f);
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    private LineRenderer lineRenderer;
    private float flashTimer;
    private bool visible;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.positionCount = 0;
        lineRenderer.numCapVertices = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.sortingOrder = 140;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            lineRenderer.material = new Material(shader);
        lineRenderer.enabled = false;
    }

    public void Configure(Color color, float newRadius, float newWidth, float newHeightOffset = 1.35f, int newSegments = 48)
    {
        baseColor = color;
        radius = Mathf.Max(0.08f, newRadius);
        lineWidth = Mathf.Max(0.01f, newWidth);
        heightOffset = newHeightOffset;
        segments = Mathf.Max(12, newSegments);
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    public void SetProgress(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);
        int visibleSegments = Mathf.Max(2, Mathf.RoundToInt(segments * progress01) + 1);
        lineRenderer.positionCount = visibleSegments;
        Vector3 center = transform.position + Vector3.up * heightOffset;
        float startAngle = -90f;
        float sweep = 360f * progress01;
        for (int i = 0; i < visibleSegments; i++)
        {
            float t = visibleSegments == 1 ? 0f : i / (float)(visibleSegments - 1);
            float angle = (startAngle + sweep * t) * Mathf.Deg2Rad;
            lineRenderer.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }

        visible = progress01 > 0f;
        lineRenderer.enabled = visible;
        lineRenderer.startColor = baseColor;
        lineRenderer.endColor = baseColor;
    }

    public void FlashComplete()
    {
        flashTimer = flashDuration;
        lineRenderer.startColor = flashColor;
        lineRenderer.endColor = flashColor;
    }

    public void Hide()
    {
        visible = false;
        flashTimer = 0f;
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        if (!visible || flashTimer <= 0f)
            return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            lineRenderer.startColor = baseColor;
            lineRenderer.endColor = baseColor;
        }
    }
}
